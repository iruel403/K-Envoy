using System;
using System.Drawing;
using System.Windows.Forms;
using K_Envoy;

namespace K_Envoy
{
    public class GamesUI
    {
        private GameManager gameManager;
        private ConfigManager configManager;
        private FlowLayoutPanel gameLibraryPanel;
        private Button btnAddGame;
        private TabPage tabGames;
        private Color backColor;
        private Color foreColor;
        private Label lblGlobalStatus;

        public GamesUI(TabPage tab, Color backColor, Color foreColor, ConfigManager config)
        {
            tabGames = tab;
            this.backColor = backColor;
            this.foreColor = foreColor;
            configManager = config;
            gameManager = new GameManager();
            CreateUI(backColor, foreColor);
            RefreshGameLibrary();
        }

        private void CreateUI(Color backColor, Color foreColor)
        {
            // Top toolbar
            Panel toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };

            // Global Status Label
            lblGlobalStatus = new Label
            {
                Text = "READY",
                Location = new Point(140, 12),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red
            };
            toolbarPanel.Controls.Add(lblGlobalStatus);

            btnAddGame = new Button
            {
                Text = "+ Add Game",
                Width = 120,
                Height = 30,
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(0, 120, 200),
                ForeColor = foreColor,
                Cursor = Cursors.Hand
            };
            btnAddGame.Click += (s, e) => OpenAddGameDialog();
            toolbarPanel.Controls.Add(btnAddGame);

            tabGames.Controls.Add(toolbarPanel);

            // Game library panel (scrollable)
            gameLibraryPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = backColor,
                ForeColor = foreColor,
                Padding = new Padding(10, 50, 10, 10)
            };
            tabGames.Controls.Add(gameLibraryPanel);
        }

        public void RefreshGameLibrary()
        {
            gameLibraryPanel.Controls.Clear();

            var games = gameManager.GetAllGames();
            System.Diagnostics.Debug.WriteLine($"RefreshGameLibrary: Found {games.Count} games");

            if (games.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No games added yet.\nClick '+ Add Game' to add your first game.",
                    AutoSize = true,
                    ForeColor = Color.FromArgb(150, 150, 150),
                    Font = new Font("Arial", 12, FontStyle.Italic),
                    Padding = new Padding(20)
                };
                gameLibraryPanel.Controls.Add(lblEmpty);
                return;
            }

            foreach (var game in games)
            {
                System.Diagnostics.Debug.WriteLine($"  Adding card for game: {game.Name} (ID: {game.Id})");
                var gameCard = CreateGameCard(game);
                gameLibraryPanel.Controls.Add(gameCard);
            }
        }

        private Panel CreateGameCard(Game game)
        {
            Panel card = new Panel
            {
                Size = new Size(180, 240),
                BackColor = Color.FromArgb(45, 45, 45),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5),
                Tag = game
            };

            // Container for image with adaptive sizing
            Panel imageContainer = new Panel
            {
                Location = new Point(5, 5),
                Size = new Size(170, 170),
                BackColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            // Game icon/image - will be sized to fit container while maintaining aspect ratio
            PictureBox pbIcon = new PictureBox
            {
                BackColor = Color.FromArgb(60, 60, 60),
                SizeMode = PictureBoxSizeMode.Zoom,  // Zoom maintains aspect ratio
                Cursor = Cursors.Hand
            };

            // Try to load icon from IconPath or extract from exe
            Image iconImage = null;
            
            if (!string.IsNullOrEmpty(game.IconPath) && System.IO.File.Exists(game.IconPath))
            {
                System.Diagnostics.Debug.WriteLine($"Loading icon from IconPath: {game.IconPath}");
                try
                {
                    iconImage = LoadIconHighQuality(game.IconPath);
                    if (iconImage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from IconPath");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load icon from IconPath");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception loading icon from IconPath: {ex.Message}");
                }
            }

            if (iconImage == null && !string.IsNullOrEmpty(game.GamePath) && System.IO.File.Exists(game.GamePath))
            {
                // Try to extract icon from the executable at high quality
                System.Diagnostics.Debug.WriteLine($"Loading icon from GamePath: {game.GamePath}");
                try
                {
                    iconImage = LoadIconHighQuality(game.GamePath);
                    if (iconImage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from GamePath");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load icon from GamePath");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception loading icon from GamePath: {ex.Message}");
                }
            }

            if (iconImage != null)
            {
                pbIcon.Image = iconImage;
                System.Diagnostics.Debug.WriteLine($"Icon set on PictureBox - Size: {iconImage.Width}x{iconImage.Height}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No icon loaded, using default gray background");
            }

            // Fill the container and center the image
            pbIcon.Dock = DockStyle.Fill;
            pbIcon.SizeMode = PictureBoxSizeMode.Zoom;

            // Click to play
            pbIcon.Click += (s, e) => LaunchGame(game);
            imageContainer.Controls.Add(pbIcon);
            card.Controls.Add(imageContainer);

            // Game name - click to play too
            Label lblName = new Label
            {
                Text = game.Name,
                Location = new Point(5, 180),
                Size = new Size(170, 55),
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Arial", 10, FontStyle.Bold),
                AutoEllipsis = false,
                TextAlign = ContentAlignment.TopLeft,
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => LaunchGame(game);
            card.Controls.Add(lblName);

            // Right-click context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            
            ToolStripMenuItem editItem = new ToolStripMenuItem("Edit");
            editItem.Click += (s, e) => OpenEditGameDialog(game);
            contextMenu.Items.Add(editItem);

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += (s, e) => DeleteGame(game);
            contextMenu.Items.Add(deleteItem);

            card.ContextMenuStrip = contextMenu;
            pbIcon.ContextMenuStrip = contextMenu;
            lblName.ContextMenuStrip = contextMenu;

            return card;
        }

        private Image LoadIconHighQuality(string path)
        {
            return IconExtractor.LoadIcon(path);
        }

        private void OpenAddGameDialog()
        {
            var form = new GameEditorForm(null, gameManager);
            if (form.ShowDialog() == DialogResult.OK)
            {
                System.Diagnostics.Debug.WriteLine("Dialog returned OK, refreshing library");
                RefreshGameLibrary();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Dialog was cancelled");
            }
        }

        private void OpenEditGameDialog(Game game)
        {
            var form = new GameEditorForm(game, gameManager);
            if (form.ShowDialog() == DialogResult.OK)
            {
                System.Diagnostics.Debug.WriteLine("Edit dialog returned OK, refreshing library");
                RefreshGameLibrary();
            }
        }

        private void LaunchGame(Game game)
        {
            try
            {
                if (!System.IO.File.Exists(game.GamePath))
                {
                    MessageBox.Show($"Executable not found at:\n{game.GamePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = game.GamePath,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(game.GamePath),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching game: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteGame(Game game)
        {
            var result = MessageBox.Show($"Delete '{game.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                gameManager.DeleteGame(game.Id);
                RefreshGameLibrary();
            }
        }

        private void ShowDataPath()
        {
            string dataPath = gameManager.GetGamesDataPath();
            MessageBox.Show($"Games data is saved at:\n\n{dataPath}\n\nYou can open this file with a text editor to view or edit game entries.", "Games Data Location", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void UpdateGlobalStatus(bool biosReady, bool windowsReady, bool defenderReady)
        {
            bool global = biosReady && windowsReady && defenderReady;
            lblGlobalStatus.Text = global ? "GLOBAL STATUS: READY" : "GLOBAL STATUS: NOT READY";
            lblGlobalStatus.ForeColor = global ? Color.Green : Color.Red;
        }
    }
}
