using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

#nullable enable

namespace RingVideos.Logging;

public static class LoggerFactory
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RingVideosData"
    );

    private static readonly string LogBasePath = Path.Combine(LogDirectory, "ringvideos");
    private static readonly string JsonLogPath = Path.Combine(LogDirectory, "ringvideos-structured.jsonl");

    static LoggerFactory()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    public static LoggerConfiguration GetDefaultConfiguration(LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        // Base configuration - capture everything up to the requested level for file/structured logs
        var fileLevel = minimumLevel <= LogEventLevel.Debug ? LogEventLevel.Debug : LogEventLevel.Information;

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(fileLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "RingVideos");

        // No Serilog Console sink: ConsoleWriter owns the terminal exclusively via absolute
        // cursor positioning for the live download grid/footer. A Serilog console sink would
        // do plain sequential WriteLine calls wherever the cursor currently sits, corrupting
        // that positioning (and, since ConsoleWriter also logs, duplicating every line).
        // Screen output goes through ConsoleWriter; every sink below is for file/AI diagnostics.

        // Daily rolling file sink (human-readable) - capture all messages at file level
        config = config.WriteTo.File(
            LogBasePath,
            restrictedToMinimumLevel: fileLevel,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        // Structured JSON sink (newline-delimited JSON for AI analysis) - capture all messages
        config = config.WriteTo.File(
            new JsonFormatter(),
            JsonLogPath,
            restrictedToMinimumLevel: fileLevel);

        // Named pipe sink (Windows) for real-time monitoring - capture all messages like files
        // TODO: Configure named pipe sink - requires proper API investigation
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        // {
        //     try
        //     {
        //         config = config.WriteTo.NamedPipe(
        //             "RingVideos-Logs",
        //             restrictedToMinimumLevel: fileLevel);
        //     }
        //     catch
        //     {
        //         // Named pipe configuration failed; continue without it
        //     }
        // }

        return config;
    }

    public static Serilog.ILogger CreateLogger<T>()
    {
        return Log.Logger.ForContext<T>();
    }

    public static Serilog.ILogger CreateLogger(string categoryName)
    {
        return Log.Logger.ForContext("SourceContext", categoryName);
    }
}
