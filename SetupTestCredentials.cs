using System;
using System.IO;
using KoenZomers.Ring.Api;

namespace RingVideos
{
    /// <summary>
    /// Command-line utility to set up Ring API credentials in AppData for testing.
    /// Uses the same CredentialStore the RingVideos application uses.
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
            var authPath = GetConfigPath();

            var creds = new RingCredentials
            {
                UserName = email,
                Password = password,
                RefreshToken = ""  // Empty for now
            };

            CredentialStore.Save(authPath, creds);
            Console.WriteLine($"✅ Wrote credentials: {authPath}");
        }

        static string GetConfigPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RingVideosData",
                "auth.json"
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
