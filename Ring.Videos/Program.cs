using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Serilog;
using Serilog.Events;
using Spectre.Console;

using Ring.Api;
using Ring.Api.Interfaces;
using Ring.Api.Clients;
using Ring.Api.Models;
using Ring.Api.Auth;
using Ring.Api.Auth.Implementations;
using Ring.Api.Common;
using Ring.Api.Common.Interfaces;
using Ring.Api.Forensics.Models;
using Ring.Api.Forensics.Models.Reports;

using Ring.Videos.Writers;
using Ring.Videos.Logging;

namespace Ring.Videos
{
    class Program
    {
        private static IPlatformDirectoryService _directoryService;

        public static string logFileBaseName =>
            Path.Combine(GetDirectoryService().GetLogsDirectory(), "ringvideos.log");

        public static string configFileName =>
            Path.Combine(GetDirectoryService().GetConfigDirectory(), "RingVideosConfig.json");
        private static IConfigurationRoot Configuration;

        private static IPlatformDirectoryService GetDirectoryService()
        {
            return _directoryService ??= new PlatformDirectoryService();
        }

        public static void Main(string[] args)
        {
            ShutdownSignal.Register();

            // Extract and strip verbosity flags (-d / --debug / -t / --trace) before
            // they are handed off to System.CommandLine, which doesn't know about them.
            var (filteredArgs, minimumLevel) = ExtractVerbosityFlags(args);

            Configuration = new ConfigurationBuilder()
             .AddJsonFile(configFileName, optional: true, reloadOnChange: true)
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .Build();

            // Configure Serilog
            var loggerConfig = Ring.Videos.Logging.LoggerFactory.GetDefaultConfiguration(minimumLevel ?? LogEventLevel.Information)
                .ReadFrom.Configuration(Configuration);

            Log.Logger = loggerConfig.CreateLogger();

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(filteredArgs).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static (string[] args, LogEventLevel? level) ExtractVerbosityFlags(string[] args)
        {
            LogEventLevel? level = null;
            var filtered = args.Where(a =>
            {
                var lower = a.ToLowerInvariant();
                if (lower == "-d" || lower == "--debug")
                {
                    level = LogEventLevel.Debug;
                    return false;
                }
                if (lower == "-t" || lower == "--trace")
                {
                    level = LogEventLevel.Verbose;
                    return false;
                }
                return true;
            }).ToArray();
            return (filtered, level);
        }

        private static ForensicsConfiguration LoadForensicsConfiguration()
        {
            var configPath = Path.Combine(GetDirectoryService().GetConfigDirectory(), "ForensicsConfig.json");

            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<ForensicsConfiguration>(json) ?? new ForensicsConfiguration();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load forensics config from {path}: {error}", configPath, ex.Message);
            }

            return new ForensicsConfiguration();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var forensicsConfig = LoadForensicsConfiguration();

            var builder = new HostBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<StartArgs>(new StartArgs(args));
                    services.AddSingleton(forensicsConfig);

                    // Platform-agnostic services
                    services.AddSingleton<IPlatformDirectoryService, PlatformDirectoryService>();
                    services.AddSingleton<ICredentialEncryption>(sp =>
                        CredentialEncryptionFactory.CreateDefault());

                    // Ring API Session - underlying client
                    services.AddSingleton<Session>();

                    // Client Facades - implement domain service interfaces
                    services.AddSingleton<IAuthenticationClient, AuthenticationClient>();
                    services.AddSingleton<IVideoDownloadClient, VideoDownloadClient>();
                    services.AddSingleton<IDeviceManagementClient, DeviceManagementClient>();

                    // Application Layer
                    services.AddSingleton<CommandHelper>();
                    //services.AddSingleton<ConsoleFormatter, CustomConsoleFormatter>();
                    services.AddSingleton<IConsole, SystemConsole>();
                    services.AddSingleton<ConsoleWriter>();
                    services.AddSingleton<IDownloadReporter, ConsoleDownloadReporter>();
                    services.AddSingleton<ICredentialStore, CredentialStore>();

                    // Event metadata recording (sidecars)
                    services.AddSingleton(EventRecordingOptions.CreatePrivacySafe());
                    services.AddSingleton<DownloadedEventRecordBuilder>();
                    services.AddSingleton<EventMetadataWriter>();

                    // Raw API response logging
                    services.AddSingleton<ApiRawResponseLogger>();

                    services.AddSingleton(sp =>
                    {
                        var directoryService = sp.GetRequiredService<IPlatformDirectoryService>();
                        var filter = hostContext.Configuration.GetSection("Filter").Get<Filter>();
                        var ringVideoService = new RingVideoService(
                            sp.GetRequiredService<ILogger<RingVideoService>>(),
                            sp.GetRequiredService<IDownloadReporter>(),
                            sp.GetRequiredService<ICredentialStore>(),
                            directoryService.GetApplicationDataDirectory(),
                            hostContext.Configuration.GetSection("LocationNames").Get<Dictionary<string, string>>(),
                            filter);

                        // Wire up metadata writing callback
                        var builder = sp.GetRequiredService<DownloadedEventRecordBuilder>();
                        var writer = sp.GetRequiredService<EventMetadataWriter>();
                        ringVideoService.OnFileDownloadedAsync = async (evt, filePath) =>
                        {
                            var record = builder.Build(evt, filePath, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
                            await writer.WriteEventRecordAsync(record, filePath);
                        };

                        // Configure and subscribe raw API response logger
                        var apiLogger = sp.GetRequiredService<ApiRawResponseLogger>();
                        apiLogger.Configure(
                            filter?.LogRawApiResponses ?? false,
                            filter?.LogEventJsonResponses ?? true);
                        apiLogger.Subscribe();

                        return ringVideoService;
                    });

                    // Give a Ctrl+C-interrupted run enough time to finish in-flight downloads and
                    // write the failure report/settings before the host force-tears-down the process.
                    services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
                });


            return builder;
        }
    }

    public class StartArgs(string[] args)
    {
        public string[] Args { get; set; } = args;
    }
}



