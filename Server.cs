using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MUD_Oberstein_Opletal.Commands;

namespace MUD_Oberstein_Opletal;

public class Server
{
    private readonly int _port;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly World _world;
    private readonly CommandHandler _commandHandler;

    public ConcurrentDictionary<TcpClient, Player> ClientPlayer { get; } = new();

    public Server(int port)
    {
        _port = port;
        _world = new World();
        _commandHandler = new CommandHandler();
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;
        
        Console.WriteLine($"Server listening on port {_port}. Waiting for clients...");

        while (_isRunning)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected from: {tcpClient.Client.RemoteEndPoint}");
                _ = HandleClientAsync(tcpClient);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
        Console.WriteLine("Server stopped.");
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        Player? player = null;
        try
        {
            using (tcpClient)
            using (var networkStream = tcpClient.GetStream())
            using (var reader = new StreamReader(networkStream, Encoding.UTF8))
            using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
            {
                string? playerName = await PromptForPlayerName(writer, reader);
                if (playerName == null) return;

                player = new Player(playerName, _world.StartingRoom, writer);
                ClientPlayer[tcpClient] = player;
                
                player.CurrentRoom.PlayersInRoom[player.Name] = player;

                await writer.WriteLineAsync($"Welcome to the game, {player.Name}!");
                Console.WriteLine($"Player '{player.Name}' logged in.");
                
                await player.CurrentRoom.BroadcastAsync($"[!] {player.Name} se připojil(a) do hry.", player);
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
            Console.WriteLine($"Error with client {player?.Name ?? "Unknown"}: {ex.Message}");
        }
        finally
        {
            if (player != null)
            {
                player.CurrentRoom.PlayersInRoom.TryRemove(player.Name, out _);
                _ = player.CurrentRoom.BroadcastAsync($"[!] {player.Name} se odpojil(a) ze hry.");
                ClientPlayer.TryRemove(tcpClient, out _);
                Console.WriteLine($"Client '{player.Name}' disconnected.");
            }
        }
    }

    private async Task<string?> PromptForPlayerName(StreamWriter writer, StreamReader reader)
    {
        while (true)
        {
            await writer.WriteLineAsync("Enter your name:");
            string? playerName = await reader.ReadLineAsync();
            if (playerName == null) return null;
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                if (ClientPlayer.Values.Any(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                {
                    await writer.WriteLineAsync("Player with that name is already online.");
                    continue;
                }
                return playerName;
            }
            await writer.WriteLineAsync("Invalid name. Please try again.");
        }
    }
}
