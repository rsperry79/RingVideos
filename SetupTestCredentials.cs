using System;
using System.IO;
using System.Text.Json;
using RingVideos.Models;

namespace RingVideos
{
    /// <summary>
    /// Command-line utility to set up Ring API credentials in AppData for testing.
    /// Uses the same Authentication class and encryption as the RingVideos application.
    ///
    /// Usage:
    ///   dotnet run SetupTestCredentials.cs -- "your-email@example.com" "your-password"
    ///   or
    ///   dotnet run SetupTestCredentials.cs (interactive)
    /// </summary>
    class SetupTestCredentials
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Ring API Test Credentials Setup Utility           ║");
            Console.WriteLine("║  Uses RingVideos app encryption                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝\n");

            string? email = null;
            string? password = null;

            // Get email
            if (args.Length > 0)
            {
                email = args[0];
                Console.WriteLine($"Email (from command line): {email}");
            }
            else
            {
                Console.Write("Enter Ring API email: ");
                email = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("❌ Error: Email cannot be empty");
                Environment.Exit(1);
            }

            // Get password
            if (args.Length > 1)
            {
                password = args[1];
                Console.WriteLine($"Password (from command line): {'*' * password.Length}");
            }
            else
            {
                Console.Write("Enter Ring API password: ");
                password = ReadPassword();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("❌ Error: Password cannot be empty");
                Environment.Exit(1);
            }

            try
            {
                SaveCredentials(email, password);
                Console.WriteLine("\n✅ Credentials saved successfully!");
                Console.WriteLine($"📁 Config location: {GetConfigPath()}");
                Console.WriteLine("\nNow you can run the real integration tests:");
                Console.WriteLine("  cd external/RingApi");
                Console.WriteLine("  dotnet test \"UnitTest/Unit Test.csproj\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void SaveCredentials(string email, string password)
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RingVideosData"
            );

            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
                Console.WriteLine($"📁 Created directory: {appDataPath}");
            }

            // Create Authentication object and encrypt
            var auth = new Authentication
            {
                UserName = email,
                ClearTextPassword = password,
                ClearTextRefreshToken = ""  // Empty for now
            };

            auth.Encrypt();

            // Create Config object
            var config = new
            {
                Authentication = new
                {
                    UserName = auth.UserName,
                    Password = auth.Password,
                    RefreshToken = auth.RefreshToken,
                    EncryptionIV = auth.EncryptionIV
                },
                Filter = new
                {
                    TakeLatestOnly = false,
                    StartDate = (string?)null,
                    EndDate = (string?)null
                }
            };

            var configPath = Path.Combine(appDataPath, "RingVideosConfig.json");
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(configPath, json);
            Console.WriteLine($"✅ Wrote config: {configPath}");
        }

        static string GetConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RingVideosData",
                "RingVideosConfig.json"
            );
        }

        static string ReadPassword()
        {
            var password = "";
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password.Substring(0, password.Length - 1);
                    }
                }
                else
                {
                    password += key.KeyChar;
                }
            }
            return password;
        }
    }
}
