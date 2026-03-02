using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace K_Envoy
{
    public class GameManager
    {
        private ConfigManager configManager;

        public GameManager()
        {
            configManager = new ConfigManager();
        }

        /// <summary>
        /// Returns the full path where config.json is saved
        /// </summary>
        public string GetGamesDataPath()
        {
            return configManager.GetConfigPath();
        }

        public List<Game> GetAllGames()
        {
            return configManager.GetAllGames();
        }

        public void AddGame(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            configManager.AddGame(game);
        }

        public void UpdateGame(Game game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            configManager.UpdateGame(game);
        }

        public void DeleteGame(string gameId)
        {
            configManager.DeleteGame(gameId);
        }
    }
}
