using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ring.Api.Common.Interfaces;
using Ring.Api.Common;
using VideoForensics.Core.Implementations;
using VideoForensics.Core.Interfaces;

namespace VideoForensics.Core
{
    public static class ForensicsFactory
    {
        public static IForensicsConfiguration LoadConfiguration(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions();
                    return JsonSerializer.Deserialize<ForensicsConfiguration>(json) ?? new ForensicsConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load config from {configPath}: {ex.Message}");
            }

            return new ForensicsConfiguration();
        }

        public static IVideoDownloadService CreateVideoDownloadService()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<VideoDownloadService>();
            var directoryService = new PlatformDirectoryService();
            return new VideoDownloadService(logger, directoryService);
        }
    }
}
