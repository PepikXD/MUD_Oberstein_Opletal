using System;
using System.Threading.Tasks;

namespace MUD_Oberstein_Opletal;

class Program
{
    static async Task Main(string[] args)
    {
        // Configurable port (e.g., via arguments or hardcoded for testing)
        int port = 5000;
        if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
        {
            port = parsedPort;
        }

        Console.WriteLine("Starting MUD server...");
        
        var server = new Server(port);
        
        // Handle server shutdown (e.g., via Ctrl+C)
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };

        await server.StartAsync();
    }
}
