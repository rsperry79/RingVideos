using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RingVideos.Models;
using RingVideos.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Threading;
using System.Text;
using KoenZomers.Ring.Api;
using MoreLinq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using System.Text.Json;
using eNt = KoenZomers.Ring.Api.Entities;
using Spectre.Console;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;
using RingVideos.Writers;
using System.Collections.Concurrent;


namespace RingVideos
{
   public class RingVideoApplication
   {

      private readonly ILogger log;
      private Session ringSession;
      private static SemaphoreSlim semaphore = new SemaphoreSlim(10, 10);
      private static int activeDls = 0;
      private static long totalBytesDownloaded = 0;
      private static DateTime downloadStartTime = DateTime.Now;
      private static CancellationTokenSource speedUpdateCancellation;
      public Filter Filter { get; set; } = new();
      public Authentication Auth { get; set; } = new();
      IConfiguration config;
      public readonly string SavedSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RingVideosData");
      public readonly string SavedSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RingVideosData","RingVideosConfig.json");
      private ConsoleWriter cw;
      private ConcurrentBag<FailedDownload> newFailures = new();
      private HashSet<string> loadedEventIds = new();
      private string reportsDirectory;
      private string logsDirectory;
      public static volatile bool IsRunActive = false;
      private static readonly object rawApiLogLock = new object();
      private Dictionary<Guid, string> locationNameCache = new();
      private Dictionary<long, Guid> deviceIdToLocationId = new();
      public RingVideoApplication(ILogger<RingVideoApplication> logger, IConfiguration config, ConsoleWriter consoleWriter)
      {
         this.log = logger;
         this.config = config;
         this.cw = consoleWriter;
         try
         {
            ReadSettings();
         }
         catch(Exception exe)
         {
            log.LogError(exe, "Failed to load saved settings");
            cw.Warning("Failed to load saved settings");
         }
      }

      private void ReadSettings()
      {
         
         try
         {
            var contents = System.IO.File.ReadAllText(SavedSettingsFile);
            var settings = JsonSerializer.Deserialize<Config>(contents);
            this.Auth = settings.Authentication.Decrypt();
            this.Filter = settings.Filter;
         }
         catch(Exception)
         {
            var tmpAuth = config.GetSection("Authentication").Get<Authentication>();
            if (tmpAuth != null)
            {
               this.Auth = tmpAuth.Decrypt();
            }
            else
            {
               this.Auth = new Authentication();
            }
            this.Filter = config.GetSection("Filter").Get<Filter>();
            if (this.Filter == null)
            {
               this.Filter = new Filter();
            }
         }

      }
      private void SaveSettings(DateTime? lastSuccessUtc, DateTime? lastFailureUtc)
      {

         Auth.Encrypt();
         //Set "next dates" on filter
         if (lastFailureUtc.HasValue && !Filter.Snapshots)
         {
            Filter.StartDateTime = lastFailureUtc.Value.AddMinutes(-1).ToLocalTime();
            Filter.EndDateTime = null;
         }
         else if (lastSuccessUtc.HasValue && !Filter.Snapshots)
         {
            Filter.StartDateTime = lastSuccessUtc.Value.ToLocalTime();
            Filter.EndDateTime = null;
         }

         if (!Directory.Exists(this.SavedSettingsFolder))
         {
            Directory.CreateDirectory(this.SavedSettingsFolder);
         }

         var conf = new Config()
         {
            Authentication = this.Auth,
            Filter = this.Filter
         };
         var config = JsonUtil.Serialize(conf, JsonMode.Pretty);

         System.IO.File.WriteAllText(this.SavedSettingsFile, config);
         log.LogInformation("Settings saved to {settingsFile}", this.SavedSettingsFile);
         log.LogInformation($"Saved refresh token (length: {Auth.ClearTextRefreshToken?.Length ?? 0})");
      }

      /// <summary>
      /// Appends every raw Ring API call made this run to a JSONL log file under the reports
      /// directory, so the full request/response traffic can be inspected for missing fields,
      /// undocumented endpoints, or data our entity classes don't currently map.
      /// </summary>
      private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

      private void LogRawApiCall(KoenZomers.Ring.Api.RawApiCall call)
      {
         if (string.IsNullOrEmpty(logsDirectory))
            return;

         try
         {
            var entry = new
            {
               timestamp = call.Timestamp.ToString("o"),
               method = call.Method,
               url = call.Url,
               statusCode = call.StatusCode,
               bodyLength = call.Body?.Length ?? 0,
               body = call.Body
            };
            var line = JsonUtil.Serialize(entry, JsonMode.Raw);

            var logPath = Path.Combine(logsDirectory, "api_raw_responses.jsonl");
            lock (rawApiLogLock)
            {
               System.IO.File.AppendAllText(logPath, line + Environment.NewLine, Utf8NoBom);
               log.LogInformation("RawApiResponse {method} {url} {statusCode}", entry.method, entry.url, entry.statusCode);
            }
         }
         catch (Exception exe)
         {
            log.LogWarning(exe, "Failed to write raw API response log entry");
         }
      }

      private static int ringEventsFileCounter = 0;

      /// <summary>
      /// Appends session/auth lifecycle events (authenticate, refresh, throttle, retry) to a
      /// human-readable debug log, separate from the full HTTP firehose.
      /// </summary>
      private void LogApiLifecycleEvent(KoenZomers.Ring.Api.ApiLifecycleEvent evt)
      {
         if (string.IsNullOrEmpty(logsDirectory))
            return;

         try
         {
            var line = $"{evt.Timestamp:o} [{evt.Category}] {evt.Message}";
            var logPath = Path.Combine(logsDirectory, "session_debug.log");
            lock (rawApiLogLock)
            {
               System.IO.File.AppendAllText(logPath, line + Environment.NewLine, Utf8NoBom);
            }
         }
         catch (Exception exe)
         {
            log.LogWarning(exe, "Failed to write session debug log entry");
         }
      }

      /// <summary>
      /// Writes the parsed Ring event ("ding") list from a history call to its own pretty-printed
      /// JSON file, so it can be diffed against the raw HTTP body to spot anything our entity
      /// classes silently drop or leave null.
      /// </summary>
      private void LogRingEventsBatch(KoenZomers.Ring.Api.RingEventsBatch batch)
      {
         if (string.IsNullOrEmpty(logsDirectory))
            return;

         try
         {
            var localTime = batch.Timestamp.ToLocalTime();
            var seq = Interlocked.Increment(ref ringEventsFileCounter);
            var fileName = $"events-{localTime:yyyy-MM-dd}-T{localTime:HH_mm_ss}-{seq}.json";
            var filePath = Path.Combine(logsDirectory, fileName);

            using var doc = JsonDocument.Parse(batch.EventsJson);
            var pretty = JsonUtil.Serialize(doc, JsonMode.Pretty);

            System.IO.File.WriteAllText(filePath, pretty, Utf8NoBom);
            log.LogInformation("RingEventsBatch {fileName} written", Path.GetFileName(filePath));
         }
         catch (Exception exe)
         {
            log.LogWarning(exe, "Failed to write ring events batch log file");
         }
      }

      public void FilterMessage(string firstLine)
      {
         try
         {
            var expandedPath = Environment.ExpandEnvironmentVariables(Filter.DownloadPath);
            cw.Info("----------------------------");
            cw.Highlight(firstLine);
            StringBuilder message = new StringBuilder();
            if (Filter.StartDateTime.HasValue)
            {
               message.AppendLine($"Start Date:\t{Filter.StartDateTime.Value} [UTC: {Filter.StartDateTimeUtc.Value}]");
            }
            if (Filter.EndDateTime.HasValue)
            {
               message.AppendLine($"End Date:\t{Filter.EndDateTime.Value} [UTC: {Filter.EndDateTimeUtc.Value}]");
            }
            else
            {
               message.AppendLine($"End Date:\tCurrent Time");
            }
            if (Filter.VideoCount != 10000)
            {
               message.AppendLine($"Max downloads:\t{Filter.VideoCount}");
            }
            message.AppendLine($"Only Starred:\t{Filter.OnlyStarred}");
            message.AppendLine($"Only Person:\t{Filter.OnlyPersonDetected}");
            if (!string.IsNullOrWhiteSpace(Filter.Kind))
            {
               message.AppendLine($"Event Kind:\t{Filter.Kind}");
            }
            if (!string.IsNullOrWhiteSpace(Filter.DetectionType))
            {
               message.AppendLine($"Detection:\t{Filter.DetectionType}");
            }
            message.AppendLine($"Snapshots:\t{Filter.Snapshots}");
            if (!string.IsNullOrWhiteSpace(expandedPath))
            {
               message.AppendLine($"Download Path:\t{expandedPath}");
            }
            message.AppendLine("----------------------------");
            cw.Info(message.ToString());

         }
         catch(Exception)
         {
           
         }
      }
      internal async Task<Session> Authenicate()
      {
         Session session = null;
         if (!string.IsNullOrWhiteSpace(this.Auth.ClearTextRefreshToken))
         {
            try
            {
               // Use refresh token from previous session
               cw.Info($"Attempting refresh token auth (token length: {this.Auth.ClearTextRefreshToken.Length})");
               await AnsiConsole.Status()
                   .StartAsync("Authenticating using refresh token from previous session...", async ctx =>
                   {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      session = await Session.GetSessionByRefreshToken(this.Auth.ClearTextRefreshToken);
                      Thread.Sleep(500);
                   });
               cw.Info("Refresh token auth succeeded!");
            }
            catch(Exception ex)
            {
               cw.Warning($"Refresh token auth failed: {ex.Message}");
               log.LogWarning(ex, "Refresh token authentication failed");
               session = null;
            }
         }
         else
         {
            cw.Info("No cached refresh token found");
         }

         if (session == null)
         {
            try
            {
               // Use the username and password provided
               await AnsiConsole.Status()
                   .StartAsync("Authenticating using provided username and password.", async ctx => {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      session = new Session(this.Auth.UserName, this.Auth.ClearTextPassword);
                      await session.Authenticate();
                      Thread.Sleep(500);
                   });
               
            }
            catch (KoenZomers.Ring.Api.Exceptions.TwoFactorAuthenticationRequiredException)
            {
               // Two factor authentication is enabled on the account. The above Authenticate() will trigger a text message to be sent. Ask for the token sent in that message here.
               cw.Info($"Two factor authentication enabled on this account, please enter the token received in the text message on your phone:");
               var token = Console.ReadLine();
               if (!string.IsNullOrEmpty(token))
               {
                  log.LogInformation("2FA token received");
               }

               // Authenticate again using the two factor token
               await session.Authenticate(twoFactorAuthCode: token);
            }
            catch (KoenZomers.Ring.Api.Exceptions.ThrottledException e)
            {
               cw.Error(e.Message);
            }
            catch (KoenZomers.Ring.Api.Exceptions.AuthenticationFailedException e)
            {
               cw.Error($"{e.Message}: Please validate your credentials");
            }
            catch (System.Net.WebException e)
            {
               cw.Error($"{e.Message}: Connection failed, please validate your credentials.");
            }
            catch(Exception exe)
            {
               cw.Error($"{exe.Message}");
            }
         }

         if (session != null && session.OAuthToken != null)
         {
           this.Auth.ClearTextRefreshToken = session.OAuthToken.RefreshToken;
            SaveSettings(null, null);
         }
         return session;
      }

      internal async Task<int> Run()
      {
         try
         {
            log.LogInformation("Starting download run");
         }
         catch (IOException)
         {
            // Output redirected (e.g. running under CI or piped to a file) - nothing to clear
         }

         IsRunActive = true;
         try
         {
            DateTime? lastSuccess = null;
            DateTime? firstFailure = null;
            int failedCount = 0;
            List<(bool success, eNt.DoorbotHistoryEvent ding)> results = new();
            this.ringSession = await Authenicate();
            if (this.ringSession == null || !this.ringSession.IsAuthenticated)
            {
               cw.Error("Authentication failed. Please check your credentials.");
               return 999;
            }
            this.Auth.ClearTextRefreshToken = this.ringSession.OAuthToken.RefreshToken;

            if (Filter.DownloadPath == null)
            {
               cw.Error("A valid download path '--path' argument is required");
               return -1;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(Filter.DownloadPath);
            if (!string.IsNullOrWhiteSpace(expandedPath))
            {
               if (!Directory.Exists(expandedPath))
               {
                  Directory.CreateDirectory(expandedPath);
               }
            }
            else
            {
               cw.Error("A valid download path '--path' argument is required");
               return -1;
            }

            // Initialize reports directory
            reportsDirectory = Path.Combine(expandedPath, "reports");
            if (!Directory.Exists(reportsDirectory))
            {
               Directory.CreateDirectory(reportsDirectory);
            }

            // Initialize logs directory
            logsDirectory = Path.Combine(expandedPath, "logs");
            if (!Directory.Exists(logsDirectory))
            {
               Directory.CreateDirectory(logsDirectory);
            }

            // Capture every raw API response for this run so we can inspect what Ring actually
            // sends back (e.g. to spot fields/devices/locations our entities don't map).
            // Unsubscribe first in case Run() is invoked more than once in this process (interactive mode).
            KoenZomers.Ring.Api.ApiRawLogger.OnRawResponse -= LogRawApiCall;
            KoenZomers.Ring.Api.ApiRawLogger.OnRawResponse += LogRawApiCall;
            KoenZomers.Ring.Api.ApiRawLogger.OnEvent -= LogApiLifecycleEvent;
            KoenZomers.Ring.Api.ApiRawLogger.OnEvent += LogApiLifecycleEvent;
            KoenZomers.Ring.Api.ApiRawLogger.OnRingEvents -= LogRingEventsBatch;
            KoenZomers.Ring.Api.ApiRawLogger.OnRingEvents += LogRingEventsBatch;

            // Load existing failures to check for duplicates
            LoadExistingFailures(reportsDirectory);

            this.FilterMessage("Fetching videos with the following settings:");
            if (!Filter.Snapshots)
            {
               DeviceList deviceList = new();
               if (Filter.DeviceId.HasValue && Filter.DeviceId.Value > 0)
               {
                  deviceList.Devices.Add(new DeviceInfo() { Id = Filter.DeviceId.Value });
               }
               else
               {
                  deviceList = await GetDevicesList();
               }

               // Fetch all devices (across every location this account has been shared into) and use
               // each device's LocationId to organize downloads
               var allDevices = new eNt.Devices
               {
                  Doorbots = new List<eNt.Doorbot>(),
                  AuthorizedDoorbots = new List<eNt.Doorbot>(),
                  Chimes = new List<eNt.Chime>(),
                  StickupCams = new List<eNt.StickupCam>()
               };

               try
               {
                  var allDeviceResponse = await this.ringSession.GetRingDevices();
                  if (allDeviceResponse != null)
                  {
                     if (allDeviceResponse.Doorbots != null)
                        allDevices.Doorbots.AddRange(allDeviceResponse.Doorbots);
                     if (allDeviceResponse.AuthorizedDoorbots != null)
                        allDevices.AuthorizedDoorbots.AddRange(allDeviceResponse.AuthorizedDoorbots);
                     if (allDeviceResponse.Chimes != null)
                        allDevices.Chimes.AddRange(allDeviceResponse.Chimes);
                     if (allDeviceResponse.StickupCams != null)
                        allDevices.StickupCams.AddRange(allDeviceResponse.StickupCams);
                  }
               }
               catch (Exception ex)
               {
                  cw.Error($"Failed to fetch devices: {ex.Message}");
                  log.LogError(ex, "Failed to fetch devices");
               }

               cw.Highlight($"Found {(allDevices.Doorbots?.Count ?? 0) + (allDevices.Chimes?.Count ?? 0) + (allDevices.StickupCams?.Count ?? 0)} total devices across all locations");

               // Build a device-id -> location-id lookup. The doorbot history API (used per-download)
               // does not embed location_id on its nested doorbot object, only the device-list APIs do -
               // so downloads must resolve location via this lookup keyed on device id, not ding.Doorbot.LocationId.
               deviceIdToLocationId.Clear();
               foreach (var d in allDevices.Doorbots.Concat(allDevices.AuthorizedDoorbots).Where(d => d.LocationId.HasValue))
                  deviceIdToLocationId[d.Id] = d.LocationId.Value;
               foreach (var d in allDevices.Chimes.Where(d => d.LocationId.HasValue))
                  deviceIdToLocationId[d.Id] = d.LocationId.Value;
               foreach (var d in allDevices.StickupCams.Where(d => d.LocationId.HasValue))
                  deviceIdToLocationId[d.Id.Value] = d.LocationId.Value;

               // Populate location name cache from the API
               cw.Info("Loading location names...");
               try
               {
                  var locations = await this.ringSession.GetLocations();
                  if (locations != null && locations.Count > 0)
                  {
                     foreach (var loc in locations)
                     {
                        if (loc.Id.HasValue && !string.IsNullOrEmpty(loc.Name))
                        {
                           locationNameCache[loc.Id.Value] = loc.Name;
                           cw.Info($"  {loc.Name} ({loc.Id})");
                        }
                     }
                  }
               }
               catch (Exception ex)
               {
                  cw.Warning($"Failed to fetch locations from API: {ex.Message}");
                  log.LogWarning(ex, "Failed to fetch location list from API");
               }

               // Fall back to config-supplied names for any location the API didn't cover
               var configLocationNames = config.GetSection("LocationNames").Get<Dictionary<string, string>>();
               if (configLocationNames != null)
               {
                  foreach (var kvp in configLocationNames)
                  {
                     if (Guid.TryParse(kvp.Key, out var locId) && !locationNameCache.ContainsKey(locId))
                     {
                        locationNameCache[locId] = kvp.Value;
                        cw.Info($"  {kvp.Value} ({locId}) [from config]");
                     }
                  }
               }

               // Build device list from all locations
               var allDeviceList = new DeviceList().ExtractDevices(allDevices);

               List<eNt.DoorbotHistoryEvent> dings = new();
               try
               {
                  foreach (var dev in allDeviceList.Devices)
                  {
                     await AnsiConsole.Status()
                            .StartAsync($"Querying for videos to download from {dev.Name}...", async ctx =>
                            {
                               ctx.Spinner(Spinner.Known.Dots2); ;
                               ctx.SpinnerStyle(Style.Parse("yellow"));
                               dings.AddRange(await ringSession.GetDoorbotsHistory(Filter.StartDateTimeUtc.Value, Filter.EndDateTimeUtc, dev.Id));
                               Thread.Sleep(500);
                            });
                  }
               }
               catch(Exception exe)
               {
                  cw.Error(exe.Message);
                  log.LogError(exe.ToString());
                  return -1;
               }
             
               if (Filter.OnlyStarred)
               {
                  dings = dings.Where(d => d.Favorite == true).ToList();
               }
               if (Filter.OnlyPersonDetected)
               {
                  dings = dings.Where(d => d.CvProperties != null &&
                                           (d.CvProperties.PersonDetected == true ||
                                            string.Equals(d.CvProperties.DetectionType, "human", StringComparison.OrdinalIgnoreCase))).ToList();
               }
               if (!string.IsNullOrWhiteSpace(Filter.DetectionType))
               {
                  var detType = Filter.DetectionType.Trim();
                  dings = dings.Where(d => d.CvProperties != null &&
                                           (string.Equals(d.CvProperties.DetectionType, detType, StringComparison.OrdinalIgnoreCase) ||
                                            (string.Equals(detType, "human", StringComparison.OrdinalIgnoreCase) && d.CvProperties.PersonDetected == true))).ToList();
               }
               if (!string.IsNullOrWhiteSpace(Filter.Kind))
               {
                  var kind = Filter.Kind.Trim();
                  dings = dings.Where(d => string.Equals(d.Kind, kind, StringComparison.OrdinalIgnoreCase)).ToList();
               }
               // Skip events that do not have a ready recording — snapshot/live-view events and
               // in-progress recordings would otherwise fail when attempting to download.
               dings = dings.Where(d => d.Recording != null &&
                                        string.Equals(d.Recording.Status, "ready", StringComparison.OrdinalIgnoreCase)).ToList();
               dings = dings.OrderBy(d => d.CreatedAtDateTime).ToList();

               int videoCount = 0;
               if(dings.Count >= Filter.VideoCount)
               {
                  videoCount = Filter.VideoCount;
               }
               else
               {
                  videoCount = dings.Count;
               }

               // Print summary BEFORE starting downloads
               string limitmessage = "";
               int totalToDownload = videoCount;
               if(dings.Count() > Filter.VideoCount)
               {
                  limitmessage = $" (Will download {Filter.VideoCount} of {dings.Count()} based on MaxCount setting)";
               }
               cw.Info("");
               cw.Highlight($"📊 Total Media: {dings.Count()} | Downloading: {totalToDownload}{limitmessage}");

               // Print device breakdown
               var byDevice = dings.Take(videoCount).GroupBy(d => d.Doorbot.Id).ToList();
               StringBuilder sb = new();
               if (!Filter.DeviceId.HasValue)
               {
                  foreach (var grp in byDevice)
                  {
                     var name = deviceList.Devices.Where(d => d.Id == grp.FirstOrDefault().Doorbot.Id).FirstOrDefault().Name;
                     var count = grp.Count();
                     var s = "";
                     if (count > 1)
                     {
                        s = "s";
                     }
                     sb.Append($"{grp.Count()} video{s} from {name} and ");
                  }
                  if (sb.Length > 4)
                  {
                     sb.Length = sb.Length - 4;
                     cw.Info($"Will download {sb.ToString()}");
                  }
               }

               cw.Info(""); // Blank line before downloads start

               // Every download gets its own persistent status row - grow the console buffer up front
               // so a large batch never scrolls mid-run and corrupts previously written rows.
               cw.EnsureBufferHeight(videoCount);

               // NOW start the downloads
               List<Task<(bool success, eNt.DoorbotHistoryEvent ding)>> tasks = new();
               for (int i = 0; i < videoCount; i++)
               {
                  if (ShutdownSignal.Cts.IsCancellationRequested)
                  {
                     cw.Warning($"Stopped queuing new downloads at {i}/{videoCount} (shutdown requested). Waiting for in-flight downloads to finish...");
                     break;
                  }
                  tasks.Add(SaveRecordingAsync(i + 1, dings[i], Filter));
               }

               // Start background task to update speed every 5 seconds
               var speedUpdateTask = UpdateSpeedPeriodically();

               results = (await Task.WhenAll(tasks.ToArray())).ToList();

               // Stop the speed update task
               speedUpdateCancellation?.Cancel();
               var success = results.Count(r => r.success == true);
               var successfulCreatedDates = results.Where(r => r.success == true).Select(r => r.ding.CreatedAtDateTime).ToList();
               lastSuccess = successfulCreatedDates.Any() ? successfulCreatedDates.Max() : null;
               failedCount = results.Count(r => r.success == false);
               cw.Highlight($"{Environment.NewLine}Successfully downloaded {success} videos");
              
            }
            else
            {
               await DownloadSnapshots(Filter);
            }
            
            SaveSettings(lastSuccess, firstFailure);
            if(failedCount > 0)
            {
               cw.Error($"{Environment.NewLine}Failed to download {failedCount} videos.");
               var failedCreatedDates = results.Where(r => r.success == false).Select(r => r.ding.CreatedAtDateTime).ToList();
               firstFailure = failedCreatedDates.Any() ? failedCreatedDates.Min() : null;
               if (firstFailure.HasValue)
               {
                  TimeZoneInfo.Local.GetUtcOffset(firstFailure.Value);
                  var est = firstFailure.Value.ToLocalTime();
                  cw.Warning($"Date of first failed download recorded ({est}). Rerun without a --start value to retry the downloads starting at that point");
               }
            }

            // Generate failure report
            var existingFailures = LoadExistingFailuresList(reportsDirectory);
            GenerateFailureReport(reportsDirectory, existingFailures);

            cw.Info($"{Environment.NewLine}Done!");
            cw.ClearLineWriters();
            return 0;
         }
         catch (Exception exe)
         {
            cw.Error(exe.ToString());
            log.LogError(exe.ToString());
            return -1;
         }
         finally
         {
            IsRunActive = false;
         }
      }

      internal async Task<bool> DownloadSnapshots(Filter filter)
      {
         var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);
         DateTime est = DateTime.Now;
         string fileNameFormat = Path.Combine(expandedPath,
             $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}-T{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}" + "--{0}.jpg");

         string fileName = string.Empty;
         var devices = await this.ringSession.GetRingDevices();
         if (devices == null)
            return false;

         if (devices.Doorbots != null)
         {
            foreach (var d in devices.Doorbots)
            {
               if (d?.Id != null && !string.IsNullOrEmpty(d.Description))
               {
                  fileName = string.Format(fileNameFormat, d.Description);
                  await GetSnapshot(d.Id, fileName);
               }
            }
         }

         if (devices.Chimes != null)
         {
            foreach (var d in devices.Chimes)
            {
               if (d?.Id != null && !string.IsNullOrEmpty(d.Description))
               {
                  fileName = string.Format(fileNameFormat, d.Description);
                  await GetSnapshot(d.Id, fileName);
               }
            }
         }

         if (devices.AuthorizedDoorbots != null)
         {
            foreach (var d in devices.AuthorizedDoorbots)
            {
               if (d?.Id != null && !string.IsNullOrEmpty(d.Description))
               {
                  fileName = string.Format(fileNameFormat, d.Description);
                  await GetSnapshot(d.Id, fileName);
               }
            }
         }

         if (devices.StickupCams != null)
         {
            foreach (var d in devices.StickupCams)
            {
               if (d?.Id != null && !string.IsNullOrEmpty(d.Description))
               {
                  fileName = string.Format(fileNameFormat, d.Description);
                  await GetSnapshot((int)d.Id, fileName);
               }
            }
         }

         return true;

      }
      internal async Task<bool> GetSnapshot(int doorbotId, string fileName)
      {
         try
         {
            await this.ringSession.UpdateSnapshot(doorbotId);
            await this.ringSession.GetLatestSnapshot(doorbotId, fileName);
            cw.Info($"Downloaded snapshot {fileName}");
            log.LogInformation($"Downloaded snapshot {fileName}");
            return true;
         }
         catch (Exception exe)
         {
            cw.Warning($"Failed to download snapshot for device {doorbotId}: {exe.Message}");
            log.LogError(exe, $"Failed to download snapshot for device {doorbotId}");
            return false;
         }
      }

      internal async Task<(bool, eNt.DoorbotHistoryEvent ding)> SaveRecordingAsync(int index, KoenZomers.Ring.Api.Entities.DoorbotHistoryEvent ding, Filter filter)
      {

         await semaphore.WaitAsync();
         LineWriter lw = cw.GetLineWriter();
         Interlocked.Increment(ref activeDls);
         try
         {

            string filename = string.Empty;
            var expandedPath = Environment.ExpandEnvironmentVariables(filter.DownloadPath);

            TimeZoneInfo.Local.GetUtcOffset(ding.CreatedAtDateTime.Value);
            var est = ding.CreatedAtDateTime.Value.ToLocalTime();
            var date = $"{est.Year}-{est.Month.ToString().PadLeft(2, '0')}-{est.Day.ToString().PadLeft(2, '0')}";
            var time = $"{est.Hour.ToString().PadLeft(2, '0')}_{est.Minute.ToString().PadLeft(2, '0')}_{est.Second.ToString().PadLeft(2, '0')}";
            var shortFileName = $"{date}-{time}-{ding.Kind}.mp4";

            // Organize by location/camera name
            string locationName = ResolveLocationName(ding.Doorbot.Id);

            var locationDir = Path.Combine(expandedPath, locationName);
            var cameraDir = Path.Combine(locationDir, ding.Doorbot.Description);
            if (!Directory.Exists(cameraDir))
               Directory.CreateDirectory(cameraDir);

            filename = Path.Combine(cameraDir, shortFileName);

            LogEventJson(ding, date, time);

            string msg = $"{index.ToString().PadLeft(3,'0')}) {locationName}/{ding.Doorbot.Description}/{shortFileName} | {ding.CreatedAtDateTime.Value.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt")} | {ding.Kind} :: ";
            cw.Write(lw,msg);
            cw.Update(lw,"Downloading");

            int attempt = 1;
            Exception lastException = null;
            string lastWebResponseBody = null;
            do
            {
               attempt++;

               //log.LogInformation($"{itemCount + 1} - {filename}... ");
               try
               {
                  await this.ringSession.GetDoorbotHistoryRecording(ding, filename);
                  long fileSizeBytes = new FileInfo(filename).Length;
                  Interlocked.Add(ref totalBytesDownloaded, fileSizeBytes);
                  var speed = GetDownloadSpeed();
                  cw.UpdateFinal(lw, $"Complete - ({fileSizeBytes / 1048576} MB)");
                  UpdateFooterStatus();
                  break;
               }
               catch (AggregateException e)
               {
                  lastException = e;
                  if (e.InnerException != null && e.InnerException.GetType() == typeof(System.Net.WebException) && ((System.Net.WebException)e.InnerException).Response != null)
                  {
                     var webException = (System.Net.WebException)e.InnerException;
                     using (var streamReader = new StreamReader(webException.Response.GetResponseStream()))
                     {
                        lastWebResponseBody = streamReader.ReadToEnd();
                     }
                     cw.UpdateError(lw, $"Failed: ({(e.InnerException != null ? e.InnerException.Message : e.Message)} - {lastWebResponseBody})");
                  }
                  else
                  {
                     cw.UpdateError(lw, $"Failed: ({(e.InnerException != null ? e.InnerException.Message : e.Message)})");
                  }
               }
               catch (Exception exe)
               {
                  lastException = exe;
                  cw.UpdateError(lw, $"Failed ({(exe.InnerException != null ? exe.InnerException.Message : exe.Message)})");
               }

               if (attempt >= 10 || ShutdownSignal.Cts.IsCancellationRequested)
               {
                  cw.UpdateWarning(lw, ShutdownSignal.Cts.IsCancellationRequested
                     ? "Giving up - shutdown requested."
                     : $"Giving up after {attempt} tries.");
                  RecordFailedDownload(ding, lastException, lastWebResponseBody);
                  return (false, ding);
               }
               else
               {
                  cw.UpdateWarning(lw, $"Retrying: {attempt + 1}/10.");
               }

            } while (attempt < 10 && !ShutdownSignal.Cts.IsCancellationRequested);

            return (true, ding);
         }
         finally
         {
            cw.ReleaseLineWriter(lw);
            Interlocked.Decrement(ref activeDls);
            semaphore.Release();
         }
      }

      private string GetDownloadSpeed()
      {
         var elapsed = DateTime.Now - downloadStartTime;
         if (elapsed.TotalSeconds < 1)
            return "calculating...";

         double bytesPerSec = totalBytesDownloaded / elapsed.TotalSeconds;
         double mbPerSec = bytesPerSec / (1024 * 1024);
         return $"{mbPerSec:F2} MB/s";
      }

      private void UpdateFooterStatus()
      {
         var speed = GetDownloadSpeed();
         string status = $"▓ Active Downloads: {activeDls} | Speed: {speed} | Total: {totalBytesDownloaded / (1024 * 1024)} MB";
         cw.UpdateFooterStatus(status);
      }

      private async Task UpdateSpeedPeriodically()
      {
         speedUpdateCancellation = new CancellationTokenSource();
         try
         {
            while (!speedUpdateCancellation.Token.IsCancellationRequested)
            {
               await Task.Delay(5000, speedUpdateCancellation.Token);
               if (!speedUpdateCancellation.Token.IsCancellationRequested)
               {
                  UpdateFooterStatus();
               }
            }
         }
         catch (OperationCanceledException)
         {
            // Expected when cancellation is requested
         }
      }

      private void LoadExistingFailures(string reportsDir)
      {
         try
         {
            if (!Directory.Exists(reportsDir))
               return;

            string tsvPath = Path.Combine(reportsDir, "download_failures.tsv");
            if (!System.IO.File.Exists(tsvPath))
               return;

            using (var reader = new StreamReader(tsvPath))
            {
               string line;
               bool isHeader = true;
               while ((line = reader.ReadLine()) != null)
               {
                  if (isHeader)
                  {
                     isHeader = false;
                     continue;
                  }

                  var parts = line.Split('\t');
                  if (parts.Length >= 5)
                  {
                     loadedEventIds.Add(parts[4]); // EventId is at index 4
                  }
               }
            }
            log.LogInformation($"Loaded {loadedEventIds.Count} previously failed event IDs from existing report");
         }
         catch (Exception exe)
         {
            log.LogError(exe, "Failed to load existing failures from report");
         }
      }

      /// <summary>
      /// Resolves a device id to its location name. The doorbot history API does not embed
      /// location_id on its nested doorbot object, so this goes through the device-id -> location-id
      /// lookup (built from the device-list APIs) rather than reading ding.Doorbot.LocationId directly.
      /// </summary>
      private string ResolveLocationName(long deviceId)
      {
         if (deviceIdToLocationId.TryGetValue(deviceId, out var locationId))
         {
            if (locationNameCache.TryGetValue(locationId, out var name))
               return name;
            return locationId.ToString().Substring(0, 8); // Fallback: first 8 chars of GUID
         }
         return "Unknown Location";
      }

      /// <summary>
      /// Writes the full JSON of a single Ring event ("ding") to its own file, named to match the
      /// downloaded video (camera-date-time-kind), so the two can be cross-referenced on disk.
      /// </summary>
      private void LogEventJson(eNt.DoorbotHistoryEvent ding, string date, string time)
      {
         if (string.IsNullOrEmpty(logsDirectory))
            return;

         try
         {
            var safeCameraName = string.Join("_", ding.Doorbot.Description.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeCameraName}-{date}-T{time}-{ding.Kind}.json";
            var filePath = Path.Combine(logsDirectory, fileName);

            var json = JsonUtil.Serialize(ding, JsonMode.Pretty);

            System.IO.File.WriteAllText(filePath, json, Utf8NoBom);
            log.LogInformation("PerEventJson {fileName} written", Path.GetFileName(filePath));
         }
         catch (Exception exe)
         {
            log.LogWarning(exe, "Failed to write per-event JSON log file");
         }
      }

      private void RecordFailedDownload(eNt.DoorbotHistoryEvent ding, Exception exception, string webResponseBody = null)
      {
         try
         {
            string eventId = ding.Id.ToString();

            // Skip if already recorded
            if (loadedEventIds.Contains(eventId))
            {
               log.LogInformation($"Skipping duplicate failure record for EventId {eventId}");
               return;
            }

            string errorDesc = ExtractErrorMessage(exception, webResponseBody);

            string locationName = ResolveLocationName(ding.Doorbot.Id);

            var failure = new FailedDownload
            {
               Timestamp = DateTime.Now,
               LocationName = locationName,
               CameraName = ding.Doorbot.Description,
               CameraId = ding.Doorbot.Id,
               EventId = eventId,
               EventType = ding.Kind,
               CreatedAt = ding.CreatedAtDateTime.Value,
               ErrorDescription = errorDesc
            };

            newFailures.Add(failure);
            loadedEventIds.Add(eventId); // Mark as recorded for this session
            log.LogInformation($"Recorded failed download for EventId {eventId}: {errorDesc}");
         }
         catch (Exception exe)
         {
            log.LogError(exe, "Failed to record download failure");
         }
      }

      private string ExtractErrorMessage(Exception exception, string webResponseBody = null)
      {
         if (webResponseBody != null)
            return webResponseBody.Length > 200 ? webResponseBody.Substring(0, 200) : webResponseBody;

         if (exception?.InnerException != null)
            return exception.InnerException.Message;

         return exception?.Message ?? "Unknown error";
      }

      private void GenerateFailureReport(string reportsDir, List<FailedDownload> existingFailures)
      {
         try
         {
            if (!Directory.Exists(reportsDir))
               Directory.CreateDirectory(reportsDir);

            string tsvPath = Path.Combine(reportsDir, "download_failures.tsv");

            // Merge existing and new failures, sort chronologically
            var allFailures = existingFailures.Concat(newFailures.ToList())
               .OrderBy(f => f.Timestamp)
               .ToList();

            if (allFailures.Count == 0)
               return;

            using (var writer = new StreamWriter(tsvPath, false, Encoding.UTF8))
            {
               // Write header
               writer.WriteLine("Date\tTime\tTimezone\tLocationName\tCameraName\tCameraId\tEventId\tEventType\tErrorDescription");

               // Write rows
               foreach (var failure in allFailures)
               {
                  var eventTime = failure.CreatedAt.ToLocalTime();
                  var tzInfo = TimeZoneInfo.Local.GetUtcOffset(failure.CreatedAt);
                  string tzStr = $"UTC{(tzInfo.TotalHours >= 0 ? "+" : "")}{tzInfo.TotalHours:F0}";
                  string line = $"{eventTime:yyyy-MM-dd}\t{eventTime:HH:mm:ss}\t{tzStr}\t{failure.LocationName}\t{failure.CameraName}\t{failure.CameraId}\t{failure.EventId}\t{failure.EventType}\t{failure.ErrorDescription}";
                  writer.WriteLine(line);
               }
            }

            if (newFailures.Count > 0)
               log.LogInformation($"Generated failure report with {allFailures.Count} total entries ({newFailures.Count} new)");
         }
         catch (Exception exe)
         {
            log.LogError(exe, "Failed to generate failure report");
         }
      }

      private List<FailedDownload> LoadExistingFailuresList(string reportsDir)
      {
         var existing = new List<FailedDownload>();
         try
         {
            if (!Directory.Exists(reportsDir))
               return existing;

            string tsvPath = Path.Combine(reportsDir, "download_failures.tsv");
            if (!System.IO.File.Exists(tsvPath))
               return existing;

            using (var reader = new StreamReader(tsvPath))
            {
               string line;
               bool isHeader = true;
               while ((line = reader.ReadLine()) != null)
               {
                  if (isHeader)
                  {
                     isHeader = false;
                     continue;
                  }

                  var parts = line.Split('\t');
                  if (parts.Length >= 8)
                  {
                     // Handle both old format (8 columns) and new format (9 columns with LocationName)
                     bool isNewFormat = parts.Length >= 9;

                     if (DateTime.TryParse($"{parts[0]} {parts[1]}", out var eventTime))
                     {
                        string locationName = isNewFormat ? parts[3] : "Unknown Location";
                        string cameraName = isNewFormat ? parts[4] : parts[3];
                        string cameraIdStr = isNewFormat ? parts[5] : parts[4];
                        string eventId = isNewFormat ? parts[6] : parts[5];
                        string eventType = isNewFormat ? parts[7] : parts[6];
                        string errorDesc = isNewFormat ? parts[8] : parts[7];

                        if (int.TryParse(cameraIdStr, out var cameraId))
                        {
                           existing.Add(new FailedDownload
                           {
                              Timestamp = eventTime,
                              LocationName = locationName,
                              CameraName = cameraName,
                              CameraId = cameraId,
                              EventId = eventId,
                              EventType = eventType,
                              CreatedAt = eventTime,
                              ErrorDescription = errorDesc
                           });
                        }
                     }
                  }
               }
            }
         }
         catch (Exception exe)
         {
            log.LogError(exe, "Failed to load existing failures list");
         }
         return existing;
      }

      internal async Task<DeviceList> GetDevicesList(string username = "", string password = "")
      {
         if (this.ringSession == null)
         {
            if (!string.IsNullOrEmpty(username))
            {
               this.Auth.UserName = username;
            }
            if (!string.IsNullOrEmpty(password))
            {
               // Keep as a fallback credential only - don't discard a cached refresh token here,
               // see the matching note in Worker.SetFilterAndAuthValues.
               this.Auth.ClearTextPassword = password;
            }
            this.ringSession = await Authenicate();
         }

         eNt.Devices devices = new();
         try
         {
            await AnsiConsole.Status()
                   .StartAsync("Getting list of registered devices...", async ctx =>
                   {
                      ctx.Spinner(Spinner.Known.Dots2); ;
                      ctx.SpinnerStyle(Style.Parse("yellow"));
                      devices = await ringSession.GetRingDevices();
                      Thread.Sleep(500);
                   });
         }
         catch (Exception exe)
         {
            cw.Error(exe.Message);
            log.LogError(exe.ToString());
            return null;
         }

         DeviceList deviceList = new DeviceList().ExtractDevices(devices);
         cw.Highlight("Found registered devices:");
         foreach (var x in deviceList.Devices)
         {
            cw.Info($"{x.Name}\tId: {x.Id}");
         }

         return deviceList;
      }
   }
}
