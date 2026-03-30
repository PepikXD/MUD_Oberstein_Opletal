using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal;

public class Server
{
    private readonly int _port;
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly World _world;

    public ConcurrentDictionary<TcpClient, Player> ClientPlayer { get; } = new();

    public Server(int port)
    {
        _port = port;
        _world = new World();
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

                player = new Player(playerName, _world.StartingRoom);
                ClientPlayer[tcpClient] = player;
                
                player.CurrentRoom.PlayersInRoom[player.Name] = player;

                await writer.WriteLineAsync($"Welcome to the game, {player.Name}!");
                Console.WriteLine($"Player '{player.Name}' logged in.");
                
                await writer.WriteLineAsync(player.CurrentRoom.GetRoomDescription(player));

                while (tcpClient.Connected)
                {
                    string? input = await reader.ReadLineAsync();
                    if (input == null) break;

                    await ProcessCommand(input, player, writer);
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

    private async Task ProcessCommand(string command, Player player, StreamWriter writer)
    {
        string[] parts = command.ToLower().Split(' ', 2);
        string action = parts[0];
        string? argument = parts.Length > 1 ? parts[1] : null;

        switch (action)
        {
            case "go":
            case "jdi":
                HandleMove(argument, player, writer);
                break;
            case "look":
            case "prozkoumej":
                await writer.WriteLineAsync(player.CurrentRoom.GetRoomDescription(player));
                break;
            case "take":
            case "vezmi":
                HandleTake(argument, player, writer);
                break;
            case "drop":
            case "poloz":
                HandleDrop(argument, player, writer);
                break;
            case "inventory":
            case "inventar":
                await writer.WriteLineAsync(player.GetInventoryDescription());
                break;
            case "talk":
            case "mluv":
                HandleTalk(argument, player, writer);
                break;
            case "help":
            case "pomoc":
                await HandleHelp(writer);
                break;
            default:
                await writer.WriteLineAsync("Unknown command.");
                break;
        }
    }

    private async Task HandleHelp(StreamWriter writer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("--- Help ---");
        sb.AppendLine("Available commands:");
        sb.AppendLine("  go <direction> / jdi <směr>    - Move to a different room (e.g., 'go north').");
        sb.AppendLine("  look / prozkoumej              - See the description of the current room.");
        sb.AppendLine("  take <item> / vezmi <předmět>  - Pick up an item from the room.");
        sb.AppendLine("  drop <item> / poloz <předmět>  - Drop an item from your inventory.");
        sb.AppendLine("  inventory / inventar           - Check your inventory.");
        sb.AppendLine("  talk <npc> / mluv <npc>        - Talk to a character in the room.");
        sb.AppendLine("  help / pomoc                   - Display this help message.");
        await writer.WriteLineAsync(sb.ToString());
    }

    private void HandleMove(string? direction, Player player, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            writer.WriteLine("Go where?");
            return;
        }

        if (player.CurrentRoom.Exits.TryGetValue(direction, out Room? nextRoom))
        {
            player.CurrentRoom.PlayersInRoom.TryRemove(player.Name, out _);
            player.CurrentRoom = nextRoom;
            player.CurrentRoom.PlayersInRoom[player.Name] = player;
            writer.WriteLine($"You go {direction}.");
            writer.WriteLine(player.CurrentRoom.GetRoomDescription(player));
        }
        else
        {
            writer.WriteLine("You can't go that way.");
        }
    }

    private void HandleTake(string? itemName, Player player, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            writer.WriteLine("Take what?");
            return;
        }

        var item = player.CurrentRoom.Items.FirstOrDefault(i => i.name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            if (player.AddToInventory(item))
            {
                player.CurrentRoom.Items.Remove(item);
                writer.WriteLine($"You took the {item.name}.");
            }
            else
            {
                writer.WriteLine("Your inventory is full.");
            }
        }
        else
        {
            writer.WriteLine("That item is not here.");
        }
    }

    private void HandleDrop(string? itemName, Player player, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            writer.WriteLine("Drop what?");
            return;
        }

        var item = player.GetInventory().FirstOrDefault(i => i.name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            player.RemoveFromInventory(item);
            player.CurrentRoom.Items.Add(item);
            writer.WriteLine($"You dropped the {item.name}.");
        }
        else
        {
            writer.WriteLine("You don't have that item.");
        }
    }

    private void HandleTalk(string? npcName, Player player, StreamWriter writer)
    {
        if (string.IsNullOrWhiteSpace(npcName))
        {
            writer.WriteLine("Talk to whom?");
            return;
        }

        var npc = player.CurrentRoom.NPCs.FirstOrDefault(n => n.Name.Equals(npcName, StringComparison.OrdinalIgnoreCase));
        if (npc != null)
        {
            writer.WriteLine($"{npc.Name} says: \"{npc.Text}\"");
        }
        else
        {
            writer.WriteLine("That person is not here.");
        }
    }
}
