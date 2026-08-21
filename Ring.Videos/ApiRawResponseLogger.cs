using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Ring.Api;

namespace Ring.Videos
{
    /// <summary>
    /// Logs raw API HTTP responses and Ring event JSON to disk, subscribed to ApiRawLogger events.
    /// Creates separate files for raw responses and parsed event JSON in the logs directory.
    /// </summary>
    public class ApiRawResponseLogger
    {
        private readonly ILogger<ApiRawResponseLogger> _logger;
        private readonly string _logsDirectory;
        private bool _logRawResponses;
        private bool _logEventJson;
        private static readonly object _fileLock = new object();

        public ApiRawResponseLogger(
            ILogger<ApiRawResponseLogger> logger,
            string logsDirectory)
        {
            _logger = logger;
            _logsDirectory = logsDirectory;
            _logRawResponses = false;
            _logEventJson = true;
        }

        /// <summary>
        /// Update logging flags from configuration.
        /// </summary>
        public void Configure(bool logRawResponses, bool logEventJson)
        {
            _logRawResponses = logRawResponses;
            _logEventJson = logEventJson;
        }

        /// <summary>
        /// Subscribe to ApiRawLogger events to start capturing logs.
        /// </summary>
        public void Subscribe()
        {
            ApiRawLogger.OnRawResponse += HandleRawResponse;
            ApiRawLogger.OnRingEvents += HandleRingEvents;
        }

        /// <summary>
        /// Unsubscribe from ApiRawLogger events.
        /// </summary>
        public void Unsubscribe()
        {
            ApiRawLogger.OnRawResponse -= HandleRawResponse;
            ApiRawLogger.OnRingEvents -= HandleRingEvents;
        }

        private void HandleRawResponse(RawApiCall call)
        {
            if (!_logRawResponses || call == null)
                return;

            try
            {
                string rawLogsDir = Path.Combine(_logsDirectory, "api-raw");
                if (!Directory.Exists(rawLogsDir))
                    Directory.CreateDirectory(rawLogsDir);

                string timestamp = call.Timestamp.ToString("yyyy-MM-dd_HH-mm-ss-ffff");
                string filename = $"{timestamp}_{call.StatusCode}_{CleanUrl(call.Url)}.txt";
                string filepath = Path.Combine(rawLogsDir, filename);

                lock (_fileLock)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Timestamp: {call.Timestamp:O}");
                    sb.AppendLine($"Method: {call.Method}");
                    sb.AppendLine($"Url: {call.Url}");
                    sb.AppendLine($"StatusCode: {call.StatusCode}");
                    sb.AppendLine();
                    sb.AppendLine("Body:");
                    sb.AppendLine(call.Body ?? "(empty)");

                    File.WriteAllText(filepath, sb.ToString(), Encoding.UTF8);
                }

                _logger.LogDebug($"Raw API response logged: {Path.GetFileName(filepath)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log raw API response");
            }
        }

        private void HandleRingEvents(RingEventsBatch batch)
        {
            if (!_logEventJson || batch == null)
                return;

            try
            {
                string eventsLogsDir = Path.Combine(_logsDirectory, "events-json");
                if (!Directory.Exists(eventsLogsDir))
                    Directory.CreateDirectory(eventsLogsDir);

                string timestamp = batch.Timestamp.ToString("yyyy-MM-dd_HH-mm-ss-ffff");
                string filename = $"{timestamp}_{batch.Category}.json";
                string filepath = Path.Combine(eventsLogsDir, filename);

                lock (_fileLock)
                {
                    var wrapper = new
                    {
                        timestamp = batch.Timestamp,
                        category = batch.Category,
                        message = batch.Message,
                        events = JsonSerializer.Deserialize<object>(batch.EventsJson ?? "{}")
                    };

                    string json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true
                    });

                    File.WriteAllText(filepath, json, Encoding.UTF8);
                }

                _logger.LogDebug($"Ring events JSON logged: {Path.GetFileName(filepath)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log Ring events JSON");
            }
        }

        private string CleanUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "unknown";

            // Extract just the endpoint portion and replace slashes with underscores
            try
            {
                var uri = new Uri(url);
                return uri.AbsolutePath
                    .TrimStart('/')
                    .Replace('/', '_')
                    .Replace('.', '_')
                    .Substring(0, Math.Min(50, uri.AbsolutePath.Length - 1));
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
