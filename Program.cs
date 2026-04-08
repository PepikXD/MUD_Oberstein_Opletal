using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MUD_Oberstein_Opletal;

class Program
{
    static async Task Main(string[] args)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        // zkusíme najít appsettings ve složce běhu (potřeba copy to output!)
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration config = builder.Build();

        int port = config.GetValue<int>("Server:Port", 8080);
        if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
        {
            port = parsedPort;
        }

        string logPath = config.GetValue<string>("Paths:Logs", "Logs/server.log")!;
        Logger.Initialize(logPath);
        Logger.LogInfo("Starting MUD server...");
        
        var server = new Server(port, config);
        
        // Handle server shutdown (e.g., via Ctrl+C)
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };

        await server.StartAsync();
    }
}
