using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace K_Envoy
{
    /// <summary>
    /// Unified configuration file that manages both application settings and game data
    /// Stores everything in a single config.json file
    /// </summary>
    public class ConfigManager
    {
        private string configPath;
        private ConfigData config;

        [Serializable]
        public class ConfigData
        {
            [JsonPropertyName("appVersion")]
            public string AppVersion { get; set; } = "1.0";

            [JsonPropertyName("lastUpdated")]
            public DateTime LastUpdated { get; set; } = DateTime.Now;

            [JsonPropertyName("settings")]
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

            [JsonPropertyName("exclusions")]
            public List<string> Exclusions { get; set; } = new List<string>();

            [JsonPropertyName("games")]
            public List<Game> Games { get; set; } = new List<Game>();
        }

        public ConfigManager()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KEnvoy");
            Directory.CreateDirectory(appDataPath);
            configPath = Path.Combine(appDataPath, "config.json");
            LoadConfig();
        }

        /// <summary>
        /// Loads the unified config file, creating it if it doesn't exist
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<ConfigData>(json);
                    if (config == null)
                        config = new ConfigData();
                }
                else
                {
                    config = new ConfigData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Error loading config: {ex.Message}");
                config = new ConfigData();
            }
        }

        /// <summary>
        /// Saves the config to file
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                config.LastUpdated = DateTime.Now;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] Error saving config: {ex.Message}");
            }
        }

        // ===== SETTINGS MANAGEMENT =====

        /// <summary>
        /// Set an application setting
        /// </summary>
        public void SetSetting(string key, string value)
        {
            config.Settings[key] = value;
            SaveConfig();
        }

        /// <summary>
        /// Get an application setting
        /// </summary>
        public string GetSetting(string key, string defaultValue = "UNKNOWN")
        {
            return config.Settings.ContainsKey(key) ? config.Settings[key] : defaultValue;
        }

        /// <summary>
        /// Get all settings
        /// </summary>
        public Dictionary<string, string> GetAllSettings()
        {
            return new Dictionary<string, string>(config.Settings);
        }

        // ===== GAMES MANAGEMENT =====

        /// <summary>
        /// Get all games
        /// </summary>
        public List<Game> GetAllGames()
        {
            return new List<Game>(config.Games);
        }

        /// <summary>
        /// Add a new game
        /// </summary>
        public void AddGame(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            game.GenerateIdIfNeeded();
            config.Games.Add(game);
            SaveConfig();
        }

        /// <summary>
        /// Update an existing game
        /// </summary>
        public void UpdateGame(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            var existing = config.Games.Find(g => g.Id == game.Id);
            if (existing != null)
            {
                existing.Name = game.Name;
                existing.GamePath = game.GamePath;
                existing.IconPath = game.IconPath;
                existing.Notes = game.Notes;
                SaveConfig();
            }
        }

        /// <summary>
        /// Delete a game by ID
        /// </summary>
        public void DeleteGame(string gameId)
        {
            config.Games.RemoveAll(g => g.Id == gameId);
            SaveConfig();
        }

        /// <summary>
        /// Add a folder path to exclusions
        /// </summary>
        public void AddExclusion(string folderPath)
        {
            if (!config.Exclusions.Contains(folderPath))
            {
                config.Exclusions.Add(folderPath);
                SaveConfig();
            }
        }

        /// <summary>
        /// Get all exclusion paths
        /// </summary>
        public List<string> GetExclusions()
        {
            return new List<string>(config.Exclusions);
        }

        /// <summary>
        /// Check if a folder path is in exclusions
        /// </summary>
        public bool IsExcluded(string folderPath)
        {
            return config.Exclusions.Contains(folderPath);
        }

        /// <summary>
        /// Get the path to the config file
        /// </summary>
        public string GetConfigPath()
        {
            return configPath;
        }
    }
}
