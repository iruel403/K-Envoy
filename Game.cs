using System;

namespace K_Envoy
{
    public class Game
    {
        private string _id;
        
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Name { get; set; }
        public string GamePath { get; set; }
        public string IconPath { get; set; }
        public string Notes { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public Game()
        {
            _id = null;
        }

        public Game(string name, string gamePath)
        {
            _id = null;
            Name = name;
            GamePath = gamePath;
        }

        public void GenerateIdIfNeeded()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString();
            }
        }
    }
}
