using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Serilog;
using Serilog.Events;

using KoenZomers.Ring.Api;
using KoenZomers.Ring.Api.Interfaces;
using KoenZomers.Ring.Api.Clients;
using KoenZomers.Ring.Api.Models;

using Ring.Videos.Writers;
using Ring.Videos.Logging;

namespace Ring.Videos
{
    class Program
    {
        public static string logFileBaseName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData", "ringvideos.log");
        public static string configFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData", "RingVideosConfig.json");
        private static IConfigurationRoot Configuration;
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {

            var builder = new HostBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<StartArgs>(new StartArgs(args));

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
                    services.AddSingleton(sp => new RingVideoService(
                        sp.GetRequiredService<ILogger<RingVideoService>>(),
                        sp.GetRequiredService<IDownloadReporter>(),
                        sp.GetRequiredService<ICredentialStore>(),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData"),
                        hostContext.Configuration.GetSection("LocationNames").Get<Dictionary<string, string>>(),
                        hostContext.Configuration.GetSection("Filter").Get<Filter>()));

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



