using System;
using System.Collections.Concurrent;
using System.IO;
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

    // Use ConcurrentDictionary for thread safety
    public ConcurrentDictionary<string, Player> NamePlayer { get; } = new();
    public ConcurrentDictionary<TcpClient, Player> ClientPlayer { get; } = new();

    public Server(int port)
    {
        _port = port;
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
                // Accept client asynchronously
                var tcpClient = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected from: {tcpClient.Client.RemoteEndPoint}");
                
                // Handle each client asynchronously in the background
                _ = HandleClientAsync(tcpClient);
            }
            catch (ObjectDisposedException)
            {
                // Server has been stopped
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
        string? playerName = null;
        try
        {
            using (tcpClient)
            using (var networkStream = tcpClient.GetStream())
            using (var reader = new StreamReader(networkStream, Encoding.UTF8))
            using (var writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true })
            {
                // Prompt for name until valid
                while (true)
                {
                    await writer.WriteLineAsync("Enter your name:");
                    playerName = await reader.ReadLineAsync();
                    
                    if (playerName == null)
                    {
                        // Client disconnected during prompt
                        return;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(playerName))
                    {
                        // Check if player is already logged in
                        if (NamePlayer.ContainsKey(playerName) && ClientPlayer.Values.Any(p => p.Name == playerName))
                        {
                            await writer.WriteLineAsync("This player is already logged in. Please choose a different name.");
                            continue;
                        }
                        break;
                    }
                    await writer.WriteLineAsync("Invalid name. Please try again.");
                }

                // Get existing player or create new one
                Player player = NamePlayer.GetOrAdd(playerName, name => new Player(name));
                
                // Track this client connection
                ClientPlayer[tcpClient] = player;

                await writer.WriteLineAsync($"Welcome to the game, {playerName}!");
                Console.WriteLine($"Player '{playerName}' successfully logged in.");
                
                while (tcpClient.Connected)
                {
                    string? input = await reader.ReadLineAsync();
                    
                    if (input == null)
                    {
                        break;
                    }
                    await writer.WriteLineAsync($"You wrote: {input}");
                }
            }
        }
        catch (IOException)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Something went wrong with client {playerName ?? "Unknown"}: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Client {(playerName != null ? "'" + playerName + "'" : "Unknown")} disconnected.");
            // Remove the client connection when they disconnect
            ClientPlayer.TryRemove(tcpClient, out _);
        }
    }
}