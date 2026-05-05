using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MUD_Client;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfiguration config = builder.Build();

        string ip = config["ServerIp"] ?? "127.0.0.1";
        int port = int.TryParse(config["ServerPort"], out var p) ? p : 8080;

        Console.WriteLine($"[Client] Connecting to {ip}:{port}...");

        using TcpClient client = new TcpClient();
        try
        {
            await client.ConnectAsync(ip, port);
            Console.WriteLine("[Client] Connected to MUD Server.");

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        string? line = await reader.ReadLineAsync();
                        if (line == null) break; 
                        
                        // Erase the current console line to print cleanly
                        int currentLeft = Console.CursorLeft;
                        if (currentLeft > 0)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write(new string(' ', currentLeft));
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }

                        Console.WriteLine(line);
                        Console.Write("> ");
                    }
                }
                catch (Exception)
                {
                    // server disconnected
                }
                Console.WriteLine("\n[Client] Disconnected from server.");
                Environment.Exit(0);
            });

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (input == null) break;

                await writer.WriteLineAsync(input);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Error connecting: {ex.Message}");
        }
    }
}
