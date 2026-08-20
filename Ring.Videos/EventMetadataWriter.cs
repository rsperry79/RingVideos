using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Ring.Api.Entities;

namespace Ring.Videos
{
    /// <summary>
    /// Writes DownloadedEventRecord metadata files alongside downloaded Ring videos.
    /// Handles JSON serialization, directory creation, and error handling.
    /// </summary>
    public class EventMetadataWriter
    {
        private readonly ILogger<EventMetadataWriter> _logger;
        private readonly EventRecordingOptions _options;
        private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

        public EventMetadataWriter(ILogger<EventMetadataWriter> logger, EventRecordingOptions options = null)
        {
            _logger = logger;
            _options = options ?? EventRecordingOptions.CreatePrivacySafe();
        }

        /// <summary>
        /// Write a DownloadedEventRecord to a JSON metadata file.
        /// </summary>
        public async Task WriteEventRecordAsync(DownloadedEventRecord record, string videoFilePath)
        {
            if (!_options.WriteEventRecords || record == null)
                return;

            try
            {
                string metadataPath = ResolveMetadataPath(videoFilePath);
                string metadataDir = Path.GetDirectoryName(metadataPath);

                if (!Directory.Exists(metadataDir))
                {
                    Directory.CreateDirectory(metadataDir);
                }

                string json = SerializeRecord(record);
                await File.WriteAllTextAsync(metadataPath, json, Encoding.UTF8);

                _logger.LogInformation($"Event record written: {Path.GetFileName(metadataPath)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write event metadata record");
                // Don't throw - metadata failure shouldn't block video download
            }
        }

        /// <summary>
        /// Synchronous version of WriteEventRecordAsync (for compatibility with existing code).
        /// </summary>
        public void WriteEventRecord(DownloadedEventRecord record, string videoFilePath)
        {
            if (!_options.WriteEventRecords || record == null)
                return;

            try
            {
                string metadataPath = ResolveMetadataPath(videoFilePath);
                string metadataDir = Path.GetDirectoryName(metadataPath);

                if (!Directory.Exists(metadataDir))
                {
                    Directory.CreateDirectory(metadataDir);
                }

                string json = SerializeRecord(record);
                File.WriteAllText(metadataPath, json, Encoding.UTF8);

                _logger.LogInformation($"Event record written: {Path.GetFileName(metadataPath)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write event metadata record");
                // Don't throw - metadata failure shouldn't block video download
            }
        }

        /// <summary>
        /// Generate metadata sidecar path: same directory as video with .json extension.
        /// Example: /videos/Front_Door_2026-08-20_10-30.mp4 → /videos/Front_Door_2026-08-20_10-30.mp4.json
        /// </summary>
        private string ResolveMetadataPath(string videoFilePath)
        {
            return videoFilePath + ".json";
        }

        private string SerializeRecord(DownloadedEventRecord record)
        {
            var options = _options.PrettyPrintJson ? _jsonOptions : new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            return JsonSerializer.Serialize(record, options);
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            return options;
        }
    }
}
