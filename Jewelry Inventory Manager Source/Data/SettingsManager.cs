using System;
using System.IO;
using Newtonsoft.Json;

namespace Moonglow_DB.Data
{
    public class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MoonglowDB",
            "settings.json");

        public class ConnectionSettings
        {
            public string Server { get; set; } = "localhost";
            public string Port { get; set; } = "3306";
            public string Database { get; set; } = "moonglow_jewelry";
            public string Username { get; set; } = "root";
            public string Password { get; set; } = "";
        }

        public static ConnectionSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonConvert.DeserializeObject<ConnectionSettings>(json) ?? new ConnectionSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new ConnectionSettings();
        }

        public static void SaveSettings(ConnectionSettings settings)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static string BuildConnectionString(ConnectionSettings settings, bool includeDatabase = true)
        {
            if (includeDatabase)
            {
                return $"Server={settings.Server};Port={settings.Port};Database={settings.Database};Uid={settings.Username};Pwd={settings.Password};CharSet=utf8;AllowUserVariables=True;";
            }
            else
            {
                return $"Server={settings.Server};Port={settings.Port};Uid={settings.Username};Pwd={settings.Password};CharSet=utf8;AllowUserVariables=True;";
            }
        }
    }
} 