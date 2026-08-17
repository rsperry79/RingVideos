using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

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
        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "RingVideos");

        // Console sink with colors
        config = config.WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        // Daily rolling file sink (human-readable)
        config = config.WriteTo.File(
            LogBasePath,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");

        // Structured JSON sink (newline-delimited JSON for AI analysis)
        config = config.WriteTo.File(
            new JsonFormatter(),
            JsonLogPath);

        // Named pipe sink (Windows) for real-time monitoring
        // TODO: Configure named pipe sink - requires proper API investigation
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        // {
        //     try
        //     {
        //         config = config.WriteTo.NamedPipe("RingVideos-Logs");
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
