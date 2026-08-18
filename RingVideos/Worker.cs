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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using RingVideos.Writers;
using Serilog;
namespace RingVideos
{
   public class Worker : BackgroundService
   {
      private static string[] StartArgs { get; set; }
  

      private static ILogger<Worker> log;
      private static RingVideoApplication ringApp;
      private static StartArgs sArgs;
      private static IConfiguration config;
      private static CommandHelper cmdHelper;
      private static RootCommand rootCommand;
      private static ConsoleWriter cw;
      private static IHostApplicationLifetime appLifetime;
      private static bool quitRequested = false;

      public Worker(ILogger<Worker> log, IConfiguration config, RingVideoApplication ringApp,  StartArgs sArgs, CommandHelper cmdHelper, ConsoleWriter  consoleWriter, IHostApplicationLifetime appLifetime)
      {
         Worker.log = log;
         Worker.ringApp = ringApp;
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
               ringApp.FilterMessage("Saved filter settings (use command flags to override):");
               log.LogInformation("RingVideos> "); // Log prompt for audit trail
               var line = Console.ReadLine();
               if (!string.IsNullOrEmpty(line))
               {
                  log.LogInformation("UserInput: {command}", line);
               }

               if (line == null)
               {
                  // stdin closed (e.g. Ctrl+Z / piped input ended) — exit gracefully
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
         catch(Exception exe)
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
            return await ringApp.Run();
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
            ringApp.Auth.UserName = username;
         }
         if (!string.IsNullOrEmpty(password))
         {
            // Keep the password as a fallback credential, but don't discard a cached refresh token -
            // Authenicate() already tries the refresh token first and only falls back to
            // username/password (which triggers 2FA) if that fails. Wiping it here forced a full
            // 2FA re-auth on every single run since -u/-p are supplied on every invocation.
            ringApp.Auth.Password = password;
         }
         if (!string.IsNullOrEmpty(path))
         {
            ringApp.Filter.DownloadPath = path;
         }
         if (start != DateTime.MinValue)
         {
            ringApp.Filter.StartDateTime = start;
         }
         if (end != DateTime.MaxValue)
         {
            ringApp.Filter.EndDateTime = end;
         }
         else
         {
            ringApp.Filter.EndDateTime = DateTime.Now;
         }
  
         ringApp.Filter.OnlyStarred = starred;
         ringApp.Filter.OnlyPersonDetected = personOnly;
         ringApp.Filter.Kind = string.IsNullOrWhiteSpace(kind) ? null : kind;
         ringApp.Filter.DetectionType = string.IsNullOrWhiteSpace(detectionType) ? null : detectionType;
         ringApp.Filter.Snapshots = snapshot;
     
         if (maxcount != 0)
         {
            ringApp.Filter.VideoCount = maxcount;
         }
         if (!ringApp.Filter.StartDateTime.HasValue)
         { 
            ringApp.Filter.StartDateTime = DateTime.MinValue;
         }
         if (!ringApp.Filter.EndDateTime.HasValue)
         {
            ringApp.Filter.EndDateTime = DateTime.MaxValue;
         }
         if(deviceId.HasValue && deviceId.Value > 0)
         {
            ringApp.Filter.DeviceId = deviceId;
         }
      }
         
     
      public static void QuitApplication()
      {
         quitRequested = true;
         appLifetime.StopApplication();
      }
      private static bool SetAuthenticationValues()
      {
         var error = ResolveAuthError(ringApp.Auth);
         if (error != null)
         {
            cw.Error(error);
            return false;
         }

         return true;
      }

      /// <summary>
      /// Checks whether <paramref name="auth"/> has enough to attempt authentication with.
      /// A refresh token alone is sufficient - Authenticate() tries it before falling back to
      /// username/password, so username/password are only required when there's no refresh token.
      /// </summary>
      /// <returns>Null if authentication can proceed, otherwise a user-facing error message.</returns>
      internal static string ResolveAuthError(RingCredentials auth)
      {
         if (!string.IsNullOrWhiteSpace(auth.RefreshToken))
         {
            return null;
         }

         if (string.IsNullOrWhiteSpace(auth.UserName))
         {
            return "A Ring username is required";
         }

         if (string.IsNullOrWhiteSpace(auth.Password))
         {
            return "A Ring password is required";
         }

         return null;
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
            using (FileStream fileStream = new FileStream(currentLogFile.FullName,FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
         await ringApp.GetDevicesList(username, password);
      }
   }
}
