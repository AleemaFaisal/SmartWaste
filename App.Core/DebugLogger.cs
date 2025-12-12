using System;
using System.IO;

namespace App.Core;

/// <summary>
/// Simple file-based logger for debugging
/// </summary>
public static class DebugLogger
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "SmartWaste_Debug.log"
    );

    private static readonly object _lock = new object();

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logMessage);
                Console.WriteLine(logMessage); // Also write to console
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    public static void LogSeparator()
    {
        Log("==================================================");
    }

    public static void ClearLog()
    {
        try
        {
            lock (_lock)
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
                Log("=== SmartWaste Debug Log Started ===");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear log file: {ex.Message}");
        }
    }

    public static string GetLogFilePath() => LogFilePath;
}
