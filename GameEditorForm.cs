using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using K_Envoy;

namespace K_Envoy
{
    public class GameEditorForm : Form
    {
        private Game game;
        private GameManager gameManager;
        private TextBox txtName;
        private TextBox txtExePath;
        private TextBox txtIconPath;
        private RichTextBox txtNotes;
        private Button btnBrowseExe;
        private Button btnBrowseIcon;
        private Button btnSave;
        private Button btnCancel;
        private PictureBox pbPreview;

        public GameEditorForm(Game existingGame, GameManager manager)
        {
            game = existingGame ?? new Game();
            gameManager = manager;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = game.Id == null ? "Add New Game" : "Edit Game";
            this.Size = new Size(600, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Color backColor = Color.FromArgb(30, 30, 30);
            Color foreColor = Color.WhiteSmoke;
            this.BackColor = backColor;
            this.ForeColor = foreColor;

            int yPos = 10;

            Label lblTitle = new Label { Text = "Game Information", Location = new Point(10, yPos), AutoSize = true, Font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Bold) };
            this.Controls.Add(lblTitle);
            yPos += 30;

            Label lblName = new Label { Text = "Game Name:", Location = new Point(10, yPos), AutoSize = true };
            this.Controls.Add(lblName);
            yPos += 20;
            txtName = new TextBox { Location = new Point(10, yPos), Width = 570, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor, Text = game.Name ?? "" };
            this.Controls.Add(txtName);
            yPos += 30;

            Label lblExePath = new Label { Text = "Game Executable (.exe):", Location = new Point(10, yPos), AutoSize = true };
            this.Controls.Add(lblExePath);
            yPos += 20;
            Label lblExeHint = new Label { Text = "Select the executable to launch.", Location = new Point(10, yPos), Width = 570, AutoSize = false, ForeColor = Color.FromArgb(200, 200, 200), Font = new Font(this.Font.FontFamily, 8) };
            this.Controls.Add(lblExeHint);
            yPos += 30;
            txtExePath = new TextBox { Location = new Point(10, yPos), Width = 520, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor, ReadOnly = true, Text = game.GamePath ?? "" };
            this.Controls.Add(txtExePath);
            btnBrowseExe = new Button { Text = "Browse...", Location = new Point(535, yPos), Width = 45, Height = 20, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor };
            btnBrowseExe.Click += (s, e) => BrowseExeFile();
            this.Controls.Add(btnBrowseExe);
            yPos += 30;

            Label lblIconPath = new Label { Text = "Game Icon (Optional):", Location = new Point(10, yPos), AutoSize = true };
            this.Controls.Add(lblIconPath);
            yPos += 20;
            Label lblIconHint = new Label { Text = "Choose game .exe for its icon, or select a custom image. If none selected, game's default icon will be used.", Location = new Point(10, yPos), Width = 570, AutoSize = false, ForeColor = Color.FromArgb(200, 200, 200), Font = new Font(this.Font.FontFamily, 8) };
            this.Controls.Add(lblIconHint);
            yPos += 30;
            txtIconPath = new TextBox { Location = new Point(10, yPos), Width = 520, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor, ReadOnly = true, Text = game.IconPath ?? "" };
            this.Controls.Add(txtIconPath);
            btnBrowseIcon = new Button { Text = "Browse...", Location = new Point(535, yPos), Width = 45, Height = 20, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor };
            btnBrowseIcon.Click += (s, e) => BrowseIconFile();
            this.Controls.Add(btnBrowseIcon);
            yPos += 30;

            pbPreview = new PictureBox
            {
                Location = new Point(10, yPos),
                Size = new Size(100, 100),
                BackColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            if (!string.IsNullOrEmpty(game.IconPath) && System.IO.File.Exists(game.IconPath))
            {
                try { pbPreview.Image = LoadIconFromPath(game.IconPath); }
                catch { }
            }
            this.Controls.Add(pbPreview);
            yPos += 120;

            Label lblNotes = new Label { Text = "Notes:", Location = new Point(10, yPos), AutoSize = true };
            this.Controls.Add(lblNotes);
            yPos += 20;
            txtNotes = new RichTextBox { Location = new Point(10, yPos), Size = new Size(570, 100), BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor, Text = game.Notes ?? "" };
            this.Controls.Add(txtNotes);
            yPos += 110;

            Label lblWarning = new Label { Text = "? Only add games with folders in system exclusions. Add it in the [Configuration] page if needed.", Location = new Point(10, yPos), Width = 570, AutoSize = false, ForeColor = Color.FromArgb(255, 200, 0), Font = new Font(this.Font.FontFamily, 8, FontStyle.Italic), MaximumSize = new Size(570, 0) };
            this.Controls.Add(lblWarning);
            yPos += 35;

            btnSave = new Button { Text = "Save", Location = new Point(415, yPos), Width = 80, Height = 30, BackColor = Color.FromArgb(0, 150, 0), ForeColor = foreColor };
            btnSave.Click += (s, e) => SaveGame();
            this.Controls.Add(btnSave);

            btnCancel = new Button { Text = "Cancel", Location = new Point(505, yPos), Width = 75, Height = 30, BackColor = Color.FromArgb(100, 0, 0), ForeColor = foreColor };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            this.Size = new Size(600, yPos + 70);
        }

        private void BrowseExeFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executable Files|*.exe|All Files|*.*";
                ofd.Title = "Select Game Executable";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtExePath.Text = ofd.FileName;
                    
                    // Auto-populate game name from folder name if it's empty
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        string folderName = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(ofd.FileName)).Name;
                        txtName.Text = folderName;
                    }
                }
            }
        }

        private void BrowseIconFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|Executable Files|*.exe|All Files|*.*";
                ofd.Title = "Select Icon or Executable";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtIconPath.Text = ofd.FileName;
                    try
                    {
                        Image img = LoadIconFromPath(ofd.FileName);
                        if (img != null)
                        {
                            pbPreview.Image = img;
                        }
                        else
                        {
                            MessageBox.Show("Could not load image preview.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not load image preview: {ex.Message}");
                    }
                }
            }
        }

        private void SaveGame()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Game name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtExePath.Text))
            {
                MessageBox.Show("Game executable path is required.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"SaveGame: Creating game object");
            game.Name = txtName.Text;
            game.GamePath = txtExePath.Text;
            game.IconPath = txtIconPath.Text; // Can be empty
            game.Notes = txtNotes.Text;

            System.Diagnostics.Debug.WriteLine($"SaveGame: Game object - Name={game.Name}, Path={game.GamePath}, ID={game.Id}");

            if (string.IsNullOrEmpty(game.Id))
            {
                System.Diagnostics.Debug.WriteLine($"SaveGame: Adding new game (ID is null/empty)");
                gameManager.AddGame(game);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SaveGame: Updating existing game (ID={game.Id})");
                gameManager.UpdateGame(game);
            }

            System.Diagnostics.Debug.WriteLine($"SaveGame: Setting DialogResult to OK and closing");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private Image LoadIconFromPath(string path)
        {
            return IconExtractor.LoadIcon(path);
        }
    }
}
