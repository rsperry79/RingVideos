using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console;
using VideoForensics.Common;
using VideoForensics.Common.Interfaces;

namespace VideoForensics
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoForensics");
            Directory.CreateDirectory(configDir);
            var configPath = Path.Combine(configDir, "ForensicsConfig.json");

            // Run demo mode if launched with --demo flag
            if (args.Length > 0 && args[0] == "--demo")
            {
                DemoMode.RunDemo();
                return;
            }

            // Load configuration and create menu
            var config = ForensicsFactory.LoadConfiguration(configPath);
            var menuManager = ForensicsFactory.CreateMenuManager(config, configPath);

            // Show interactive menu
            await menuManager.ShowMainMenuAsync();
        }
    }
}
