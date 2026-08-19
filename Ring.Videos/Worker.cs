using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using KoenZomers.Ring.Api;
using KoenZomers.Ring.Api.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Ring.Videos.Writers;

using Serilog;
namespace Ring.Videos
{
    public class Worker : BackgroundService
    {
        private static string[] StartArgs { get; set; }


        private static ILogger<Worker> log;
        private static RingVideoService ringVideoService;
        private static StartArgs sArgs;
        private static IConfiguration config;
        private static CommandHelper cmdHelper;
        private static RootCommand rootCommand;
        private static ConsoleWriter cw;
        private static IHostApplicationLifetime appLifetime;
        private static bool quitRequested = false;

        public Worker(ILogger<Worker> log, IConfiguration config, RingVideoService ringVideoService, StartArgs sArgs, CommandHelper cmdHelper, ConsoleWriter consoleWriter, IHostApplicationLifetime appLifetime)
        {
            Worker.log = log;
            Worker.ringVideoService = ringVideoService;
            Worker.sArgs = sArgs;
            Worker.config = config;
            Worker.cmdHelper = cmdHelper;
            Worker.cw = consoleWriter;
            Worker.appLifetime = appLifetime;

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                rootCommand = cmdHelper.SetupCommands();
                if (Worker.sArgs.Args.Length == 0)
                {
                    Worker.sArgs.Args = new string[] { "-h" };
                }

                int val = await rootCommand.Parse(Worker.sArgs.Args).InvokeAsync();
                if (Worker.sArgs.Args.Contains("-x") || Worker.sArgs.Args.Contains("--exit"))
                {
                    appLifetime.StopApplication();
                    return;
                }
                while (true)
                {
                    if (quitRequested || stoppingToken.IsCancellationRequested || ShutdownSignal.Cts.IsCancellationRequested)
                    {
                        break;
                    }
                    ringVideoService.PrintFilterMessage("Saved filter settings (use command flags to override):");
                    log.LogInformation("RingVideos> "); // Log prompt for audit trail
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        log.LogInformation("UserInput: {command}", line);
                    }

                    if (line == null)
                    {
                        // stdin closed (e.g. Ctrl+Z / piped input ended) - exit gracefully
                        break;
                    }

                    if (line.Length == 0)
                    {
                        line = "-h";
                    }

                    try
                    {
                        val = await rootCommand.Parse(line).InvokeAsync();
                    }
                    catch (Exception exe)
                    {
                        if (exe.Message != "Nullable object must have a value.")
                            log.LogError($"❌ Failed to run command: {exe.Message}");
                    }

                    if (quitRequested)
                    {
                        break;
                    }
                }

                appLifetime.StopApplication();

            }
            catch (Exception exe)
            {
                log.LogError(exe.Message);
            }
        }




        public static async Task<int> GetSnapshotImages(string username, string password, string path, DateTime start, DateTime end, long? deviceId)
        {
            return await GetVideos(username, password, path, start, end, false, false, null, null, true, 1000, deviceId);
        }

        public static async Task<int> GetAllVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount, long? deviceId, bool personOnly, string kind, string detectionType)
        {
            return await GetVideos(username, password, path, start, end, false, personOnly, kind, detectionType, false, maxcount, deviceId);
        }
        public static async Task<int> GetStarredVideos(string username, string password, string path, DateTime start, DateTime end, int maxcount, long? deviceId, bool personOnly, string kind, string detectionType)
        {
            return await GetVideos(username, password, path, start, end, true, personOnly, kind, detectionType, false, maxcount, deviceId);
        }
        private static async Task<int> GetVideos(string username, string password, string path, DateTime start, DateTime end, bool starred, bool personOnly, string kind, string detectionType, bool snapshot, int maxcount, long? deviceId)
        {

            SetFilterAndAuthValues(username, password, path, start, end, starred, personOnly, kind, detectionType, snapshot, maxcount, deviceId);

            if (SetAuthenticationValues())
            {
                return await ringVideoService.Run(ShutdownSignal.Cts.Token);
            }
            else
            {
                return -200;
            }


        }

        private static void SetFilterAndAuthValues(string username, string password, string path, DateTime start, DateTime end, bool starred, bool personOnly, string kind, string detectionType, bool snapshot, int maxcount, long? deviceId)
        {
            if (!string.IsNullOrEmpty(username))
            {
                ringVideoService.Auth.UserName = username;
            }
            if (!string.IsNullOrEmpty(password))
            {
                // Keep the password as a fallback credential, but don't discard a cached refresh token -
                // Authenticate() already tries the refresh token first and only falls back to
                // username/password (which triggers 2FA) if that fails. Wiping it here forced a full
                // 2FA re-auth on every single run since -u/-p are supplied on every invocation.
                ringVideoService.Auth.Password = password;
            }
            if (!string.IsNullOrEmpty(path))
            {
                ringVideoService.Filter.DownloadPath = path;
            }
            if (start != DateTime.MinValue)
            {
                ringVideoService.Filter.StartDateTime = start;
            }
            if (end != DateTime.MaxValue)
            {
                ringVideoService.Filter.EndDateTime = end;
            }
            else
            {
                ringVideoService.Filter.EndDateTime = DateTime.Now;
            }

            ringVideoService.Filter.OnlyStarred = starred;
            ringVideoService.Filter.OnlyPersonDetected = personOnly;
            ringVideoService.Filter.Kind = string.IsNullOrWhiteSpace(kind) ? null : kind;
            ringVideoService.Filter.DetectionType = string.IsNullOrWhiteSpace(detectionType) ? null : detectionType;
            ringVideoService.Filter.Snapshots = snapshot;

            if (maxcount != 0)
            {
                ringVideoService.Filter.VideoCount = maxcount;
            }
            if (!ringVideoService.Filter.StartDateTime.HasValue)
            {
                ringVideoService.Filter.StartDateTime = DateTime.MinValue;
            }
            if (!ringVideoService.Filter.EndDateTime.HasValue)
            {
                ringVideoService.Filter.EndDateTime = DateTime.MaxValue;
            }
            if (deviceId.HasValue && deviceId.Value > 0)
            {
                ringVideoService.Filter.DeviceId = deviceId;
            }
        }


        public static void QuitApplication()
        {
            quitRequested = true;
            appLifetime.StopApplication();
        }
        private static bool SetAuthenticationValues()
        {
            var error = RingVideoService.ResolveAuthError(ringVideoService.Auth);
            if (error != null)
            {
                cw.Error(error);
                return false;
            }

            return true;
        }

        internal static void ShowLog()
        {
            var folder = Path.GetDirectoryName(Program.logFileBaseName);
            var fileRoot = Path.GetFileNameWithoutExtension(Program.logFileBaseName);
            var dirInf = new DirectoryInfo(folder);
            var currentLogFile = dirInf.GetFiles($"{fileRoot}*.log").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();


            cw.Warning($"Log file can be found here: {currentLogFile}");
            cw.Info("Last 100 lines from log file:");
            cw.Info("");

            try
            {
                string filecontent;
                using (FileStream fileStream = new FileStream(currentLogFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    filecontent = reader.ReadToEnd();
                }
                var lines = filecontent.Split(Environment.NewLine);
                var last100 = lines.Skip(Math.Max(0, lines.Count()) - 100);
                foreach (var line in last100)
                {
                    cw.Info(line);
                }

            }
            catch (Exception exe)
            {
                cw.Error(exe.Message);
                log.LogError(exe.ToString());
            }

        }

        internal static async Task DeviceList(string username, string password)
        {
            await ringVideoService.GetDevicesList(username, password);
        }
    }
}
