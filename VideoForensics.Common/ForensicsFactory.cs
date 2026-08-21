using System;
using System.IO;
using System.Text.Json;
using VideoForensics.Common.Implementations;
using VideoForensics.Common.Interfaces;

namespace VideoForensics.Common
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

        public static IMenuManager CreateMenuManager(IForensicsConfiguration config, string configPath)
        {
            return new MenuManager(config, configPath);
        }
    }
}
