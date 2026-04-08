using System;
using System.IO;

namespace MUD_Oberstein_Opletal;

public static class Logger
{
    private static string _logFilePath = "server.log";
    private static readonly object _lock = new object();

    public static void Initialize(string logFilePath)
    {
        _logFilePath = logFilePath;
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void LogInfo(string message)
    {
        Log("INFO", message);
    }

    public static void LogError(string message)
    {
        Log("ERROR", message);
    }

    private static void Log(string level, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logLine = $"[{timestamp}] [{level}] {message}";

        // Write to Console
        Console.WriteLine(logLine);

        // Write to File (thread-safe)
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Jesli zapis do souboru selze, vypiseme to aspon do konzole a nedovolime pad serveru
                Console.WriteLine($"[Logger Error] nelze zapsat do souboru: {ex.Message}");
            }
        }
    }
}
