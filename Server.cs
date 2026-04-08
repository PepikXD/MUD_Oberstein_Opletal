using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MUD_Oberstein_Opletal.Commands;

namespace MUD_Oberstein_Opletal;

public class Server
{
    private readonly int _port;
    private readonly IConfiguration _config;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly World _world;
    private readonly CommandHandler _commandHandler;

    private readonly AccountManager _accountManager;

    public ConcurrentDictionary<TcpClient, Player> ClientPlayer { get; } = new();

    public Server(int port, IConfiguration config)
    {
        _port = port;
        _config = config;
        
        string worldDataPath = _config.GetValue<string>("Paths:WorldData", "Data/world.json")!;
        _world = new World(worldDataPath);
        
        string accountsPath = _config.GetValue<string>("Paths:Accounts", "Accounts")!;
        _accountManager = new AccountManager(accountsPath);
        
        _commandHandler = new CommandHandler();
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;
        
        Logger.LogInfo($"Server listening on port {_port}. Waiting for clients...");

        while (_isRunning)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                Logger.LogInfo($"Client connected from: {tcpClient.Client.RemoteEndPoint}");
                _ = HandleClientAsync(tcpClient);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error accepting client: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
        Logger.LogInfo("Server stopped.");
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        Player? player = null;
        AccountData? accountData = null;
        try
        {
            using (tcpClient)
            using (var networkStream = tcpClient.GetStream())
            using (var reader = new StreamReader(networkStream, Encoding.UTF8))
            using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
            {
                accountData = await LoginFlowAsync(writer, reader);
                if (accountData == null) return;

                Room startingLoc = _world.StartingRoom;
                if (!string.IsNullOrEmpty(accountData.LocationId) && _world.Rooms.TryGetValue(accountData.LocationId, out var savedRoom))
                {
                    startingLoc = savedRoom;
                }

                player = new Player(accountData.Name, startingLoc, writer);
                
                // Obnova inventáře
                foreach (var itemId in accountData.InventoryItems)
                {
                    var item = _world.CreateItem(itemId);
                    if (item != null)
                        player.Inventory.Add(item);
                }

                ClientPlayer[tcpClient] = player;
                
                player.CurrentRoom.PlayersInRoom[player.Name] = player;

                await writer.WriteLineAsync(string.Format(Resources.WelcomeMessage, player.Name));
                Logger.LogInfo(string.Format(Resources.LoggedInMessage, player.Name));
                
                await player.CurrentRoom.BroadcastAsync(string.Format(Resources.PlayerJoinedServer, player.Name), player);
                await writer.WriteLineAsync(player.CurrentRoom.GetRoomDescription(player));

                while (tcpClient.Connected)
                {
                    string? input = await reader.ReadLineAsync();
                    if (input == null) break;

                    await _commandHandler.ExecuteCommandAsync(input, player, writer);
                }
            }
        }
        catch (IOException) { }
        catch (Exception ex)
        {
            Logger.LogError($"Error with client {player?.Name ?? "Unknown"}: {ex.Message}");
        }
        finally
        {
            if (player != null && accountData != null)
            {
                player.CurrentRoom.PlayersInRoom.TryRemove(player.Name, out _);
                _ = player.CurrentRoom.BroadcastAsync(string.Format(Resources.PlayerLeftServer, player.Name));
                ClientPlayer.TryRemove(tcpClient, out _);
                
                // Save state
                accountData.LocationId = player.CurrentRoom.Id;
                accountData.InventoryItems = player.Inventory.Select(i => i.Id).ToList();
                await _accountManager.SaveAccountAsync(accountData);

                Logger.LogInfo(string.Format(Resources.LoggedOutMessage, player.Name));
            }
        }
    }

    private async Task<AccountData?> LoginFlowAsync(StreamWriter writer, StreamReader reader)
    {
        while (true)
        {
            await writer.WriteLineAsync(Resources.AskForName);
            string? playerName = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(playerName)) return null;

            if (ClientPlayer.Values.Any(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
            {
                await writer.WriteLineAsync(Resources.NameAlreadyOnline);
                continue;
            }

            if (_accountManager.AccountExists(playerName))
            {
                await writer.WriteLineAsync(Resources.AskForPassword);
                string? password = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(password)) return null;

                if (await _accountManager.VerifyPasswordAsync(playerName, password))
                {
                    return await _accountManager.LoadAccountAsync(playerName);
                }
                else
                {
                    await writer.WriteLineAsync("Invalid password.");
                }
            }
            else
            {
                await writer.WriteLineAsync("Account not found. Create a new password to register:");
                string? password = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(password)) return null;

                return await _accountManager.CreateAccountAsync(playerName, password, _world.StartingRoom.Id);
            }
        }
    }
}
