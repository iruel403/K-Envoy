using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace K_Envoy
{
    public partial class Form1 : Form
    {
        // Configuration
        private ConfigManager configManager;
        private GamesUI gamesUI;

        // State tracking
        private bool isLoading = true;
        private Dictionary<string, bool> featureAvailable = new Dictionary<string, bool>();
        private Dictionary<string, (bool isOn, Button on, Button off)> pendingStateChanges = new Dictionary<string, (bool, Button, Button)>();
        private StringBuilder pendingCommands = new StringBuilder();
        private bool pendingExclusionsSetting = false;
        private string pendingExclusionPath = null;

        private bool biosReady = false;
        private bool windowsReady = false;
        private bool defenderReady = false;
        private bool gameReady = false;

        // UI Components
        private TabControl tabControl;
        private TabPage tabConfiguration, tabGames;

        private Label lblGlobalStatus;
        private Label lblSecureBootStatus, lblVirtStatus;
        private Label lblWindowsStatus, lblDefenderStatus;

        private Button btnHypervisorOn, btnHypervisorOff;
        private Button btnTestSigningOn, btnTestSigningOff;
        private Button btnNoIntegrityOn, btnNoIntegrityOff;

        private Button btnMemIntegrityOn, btnMemIntegrityOff;
        private Label lblMemIntegrityStatus;
        private Button btnKernelStackOn, btnKernelStackOff;
        private Label lblKernelStackStatus;
        private Button btnLSAOn, btnLSAOff;
        private Label lblLSAStatus;
        private Button btnVulnDriverOn, btnVulnDriverOff;
        private Label lblVulnDriverStatus;

        private TextBox txtGameFolder;
        private Button btnBrowseGame;
        private CheckBox chkAddExclusions;

        private Button btnTurnAllOnWindows, btnTurnAllOffWindows;
        private Button btnTurnAllOnDefender, btnTurnAllOffDefender;
        private Button btnTurnAllOn, btnTurnAllOff;
        private Button btnOpenLog, btnRefreshStatus;
        private CheckBox chkStepMode;
        private RichTextBox txtLog;
        private Button btnSendCommand;

        private GroupBox gbBios, gbWindows, gbDefender, gbGame;

        public Form1()
        {
            configManager = new ConfigManager();
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void InitializeComponent()
        {
            this.Text = "K-Envoy";
            this.Size = new Size(1100, 1200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = true;

            Color backColor = Color.FromArgb(30, 30, 30);
            Color foreColor = Color.WhiteSmoke;
            this.BackColor = backColor;
            this.ForeColor = foreColor;

            // Create Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                BackColor = backColor,
                ForeColor = foreColor
            };

            // Games Tab (First)
            tabGames = new TabPage { Text = "Games", BackColor = backColor, ForeColor = foreColor, AutoScroll = true };
            CreateGamesTab(tabGames, backColor, foreColor);
            tabControl.TabPages.Add(tabGames);

            // Configuration Tab (Second)
            tabConfiguration = new TabPage { Text = "Configuration", BackColor = backColor, ForeColor = foreColor, AutoScroll = true };
            CreateConfigurationTab(tabConfiguration, backColor, foreColor);
            tabControl.TabPages.Add(tabConfiguration);

            // Troubleshoot Tab (Third)
            TabPage tabTroubleshoot = new TabPage { Text = "Troubleshoot", BackColor = backColor, ForeColor = foreColor, AutoScroll = true };
            CreateTroubleshootTab(tabTroubleshoot, backColor, foreColor);
            tabControl.TabPages.Add(tabTroubleshoot);

            // Set Games tab as default (selected)
            tabControl.SelectedIndex = 0;

            this.Controls.Add(tabControl);
            this.ClientSize = new Size(1100, 1200);
        }

        private void CreateConfigurationTab(TabPage tab, Color backColor, Color foreColor)
        {
            // Top status label
            lblGlobalStatus = new Label
            {
                Text = "GLOBAL STATUS: NOT READY",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = Color.Red
            };
            tab.Controls.Add(lblGlobalStatus);

            // BIOS section
            gbBios = new GroupBox { Text = "1) BIOS PREPARATION", Location = new Point(10, 40), Size = new Size(1060, 180), BackColor = backColor, ForeColor = foreColor };
            Label lblBiosInstructions = new Label
            {
                Text = "Restart and enter BIOS (Del/F2/F12/Esc). Enable Virtualization (SVM/VT-x). Disable Secure Boot.",
                Location = new Point(10, 20),
                AutoSize = true,
                ForeColor = foreColor
            };
            lblSecureBootStatus = new Label { Text = "Secure Boot - Checking...", Location = new Point(10, 100), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = foreColor };
            lblVirtStatus = new Label { Text = "Virtualization - Checking...", Location = new Point(300, 100), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = foreColor };
            gbBios.Controls.AddRange(new Control[] { lblBiosInstructions, lblSecureBootStatus, lblVirtStatus });
            tab.Controls.Add(gbBios);

            // Windows Configuration section
            gbWindows = new GroupBox { Text = "2) WINDOWS CONFIGURATION", Location = new Point(10, 230), Size = new Size(1060, 200), BackColor = backColor, ForeColor = foreColor };

            AddButtonPair(gbWindows, "Disable Hypervisor", 10, 25, out btnHypervisorOn, out btnHypervisorOff, "hypervisor");
            AddButtonPair(gbWindows, "Test Signing", 10, 50, out btnTestSigningOn, out btnTestSigningOff, "testsigning");
            AddButtonPair(gbWindows, "No Integrity Checks", 10, 75, out btnNoIntegrityOn, out btnNoIntegrityOff, "nointegrity");

            Label lblIntelInfo = new Label
            {
                Text = "Intel users: Disable Meltdown and Spectre using InSpectre software (Download from Gibson Research)",
                Location = new Point(10, 105),
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 200, 0),
                Font = new Font(this.Font, FontStyle.Italic)
            };
            gbWindows.Controls.Add(lblIntelInfo);

            btnTurnAllOnWindows = new Button { Text = "Turn All ON", Location = new Point(10, 130), Width = 150, Height = 25, BackColor = Color.FromArgb(0, 120, 0), ForeColor = foreColor };
            btnTurnAllOnWindows.Click += (s, e) => TurnAllWindowsOn();
            btnTurnAllOffWindows = new Button { Text = "Turn All OFF", Location = new Point(170, 130), Width = 150, Height = 25, BackColor = Color.FromArgb(120, 0, 0), ForeColor = foreColor };
            btnTurnAllOffWindows.Click += (s, e) => TurnAllWindowsOff();

            lblWindowsStatus = new Label { Text = "Checking...", Location = new Point(10, 160), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = foreColor };

            gbWindows.Controls.AddRange(new Control[] { btnTurnAllOnWindows, btnTurnAllOffWindows, lblWindowsStatus });
            tab.Controls.Add(gbWindows);

            // Defender section
            gbDefender = new GroupBox { Text = "3) WINDOWS DEFENDER", Location = new Point(10, 440), Size = new Size(1060, 220), BackColor = backColor, ForeColor = foreColor };

            AddDynamicButtonPair(gbDefender, "Disable Memory Integrity", 10, 25, out btnMemIntegrityOn, out btnMemIntegrityOff, out lblMemIntegrityStatus, "memintegrity");
            AddDynamicButtonPair(gbDefender, "Disable Kernel Stack Protection", 10, 55, out btnKernelStackOn, out btnKernelStackOff, out lblKernelStackStatus, "kernelstack");
            AddDynamicButtonPair(gbDefender, "Disable LSA Protection", 10, 85, out btnLSAOn, out btnLSAOff, out lblLSAStatus, "lsa");
            AddDynamicButtonPair(gbDefender, "Disable Vulnerable Driver Blocklist", 10, 115, out btnVulnDriverOn, out btnVulnDriverOff, out lblVulnDriverStatus, "vulndriver");

            btnTurnAllOnDefender = new Button { Text = "Turn All ON", Location = new Point(10, 150), Width = 150, Height = 25, BackColor = Color.FromArgb(0, 120, 0), ForeColor = foreColor };
            btnTurnAllOnDefender.Click += (s, e) => TurnAllDefenderOn();
            btnTurnAllOffDefender = new Button { Text = "Turn All OFF", Location = new Point(170, 150), Width = 150, Height = 25, BackColor = Color.FromArgb(120, 0, 0), ForeColor = foreColor };
            btnTurnAllOffDefender.Click += (s, e) => TurnAllDefenderOff();

            lblDefenderStatus = new Label { Text = "Checking...", Location = new Point(10, 180), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold), ForeColor = foreColor };

            Label lblManualSettings = new Label
            {
                Text = "Tip: You can manually adjust these settings by opening Device Security in Windows Settings.",
                Location = new Point(10, 205),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font(this.Font, FontStyle.Italic),
                MaximumSize = new Size(1040, 0)
            };

            gbDefender.Controls.AddRange(new Control[] { btnTurnAllOnDefender, btnTurnAllOffDefender, lblDefenderStatus, lblManualSettings });
            tab.Controls.Add(gbDefender);

            // Game Setup section
            gbGame = new GroupBox { Text = "4) GAME SETUP", Location = new Point(10, 670), Size = new Size(1060, 200), BackColor = backColor, ForeColor = foreColor };

            Label lblGameFolder = new Label { Text = "Game folder:", Location = new Point(10, 25), AutoSize = true, ForeColor = foreColor };
            txtGameFolder = new TextBox { Location = new Point(100, 22), Width = 300, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor };
            btnBrowseGame = new Button { Text = "Browse...", Location = new Point(410, 20), Width = 80, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor };
            btnBrowseGame.Click += BrowseGameFolder;

            chkAddExclusions = new CheckBox { Text = "Add game folder to Defender exclusions", Location = new Point(10, 60), AutoSize = true, ForeColor = foreColor };
            chkAddExclusions.CheckedChanged += AddExclusionsToggled;

            Label lblGameInstructions = new Label
            {
                Text = "First, add the game folder to Defender exclusions using the checkbox above. Then, copy all files and folders from your crack archive to the game folder. This ensures Windows Defender won't remove or quarantine the bypass files.",
                Location = new Point(10, 90),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font(this.Font, FontStyle.Italic),
                MaximumSize = new Size(1040, 0)
            };

            gbGame.Controls.AddRange(new Control[] { lblGameFolder, txtGameFolder, btnBrowseGame, chkAddExclusions, lblGameInstructions });
            tab.Controls.Add(gbGame);

            // Global controls
            btnTurnAllOn = new Button { Text = "Turn All: ON", Location = new Point(10, 880), Width = 150, Height = 40, BackColor = Color.FromArgb(0, 150, 0), ForeColor = foreColor, Font = new Font(this.Font, FontStyle.Bold) };
            btnTurnAllOn.Click += TurnAllOn;
            btnTurnAllOff = new Button { Text = "Turn All: OFF", Location = new Point(170, 880), Width = 150, Height = 40, BackColor = Color.FromArgb(150, 0, 0), ForeColor = foreColor, Font = new Font(this.Font, FontStyle.Bold) };
            btnTurnAllOff.Click += TurnAllOff;
            btnRefreshStatus = new Button { Text = "Refresh Status", Location = new Point(330, 880), Width = 150, Height = 40, BackColor = Color.FromArgb(64, 64, 64), ForeColor = foreColor };
            btnRefreshStatus.Click += (s, e) => { isLoading = true; RefreshAllStatus(); isLoading = false; };
            btnOpenLog = new Button { Text = "Open Config File", Location = new Point(490, 880), Width = 150, Height = 40, BackColor = Color.FromArgb(0, 100, 150), ForeColor = foreColor };
            btnOpenLog.Click += OpenConfigFile;

            chkStepMode = new CheckBox { Text = "Step Mode (Queue commands, then Send all at once)", Location = new Point(650, 890), AutoSize = true, ForeColor = foreColor, Checked = false };
            chkStepMode.CheckedChanged += (s, e) => UpdateSendButtonState();

            ToolTip stepModeTip = new ToolTip();
            stepModeTip.SetToolTip(chkStepMode, "Enable: Queue multiple commands, review them, then click Send\nDisable: Execute commands immediately when clicked");

            tab.Controls.Add(btnTurnAllOn);
            tab.Controls.Add(btnTurnAllOff);
            tab.Controls.Add(btnRefreshStatus);
            tab.Controls.Add(btnOpenLog);
            tab.Controls.Add(chkStepMode);

            // Log panel
            Label lblLog = new Label { Text = "Command Log:", Location = new Point(10, 930), AutoSize = true, ForeColor = foreColor };
            txtLog = new RichTextBox
            {
                Location = new Point(10, 950),
                Size = new Size(850, 120),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9)
            };
            btnSendCommand = new Button
            {
                Text = "Send",
                Location = new Point(870, 950),
                Size = new Size(100, 120),
                BackColor = Color.FromArgb(0, 100, 200),
                ForeColor = foreColor,
                Enabled = false
            };
            btnSendCommand.Click += SendPendingCommands;
            tab.Controls.Add(lblLog);
            tab.Controls.Add(txtLog);
            tab.Controls.Add(btnSendCommand);
        }

        private void CreateGamesTab(TabPage tab, Color backColor, Color foreColor)
        {
            gamesUI = new GamesUI(tab, backColor, foreColor, configManager);
        }

        private void CreateTroubleshootTab(TabPage tab, Color backColor, Color foreColor)
        {
            // Title
            Label lblTitle = new Label
            {
                Text = "Troubleshooting Guide",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White
            };
            tab.Controls.Add(lblTitle);

            // Troubleshooting content in RichTextBox for better formatting
            RichTextBox rtbTroubleshoot = new RichTextBox
            {
                Location = new Point(10, 50),
                Size = new Size(1070, 1100),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.WhiteSmoke,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Add troubleshooting content
            rtbTroubleshoot.Text = @"COMMON ISSUES & SOLUTIONS

1. To be sure that the application is working, make sure to:
   - Run the application as Administrator
   - Reboot your system after making changes in the application.
   - Ensure you have sufficient user privileges
   - Check if settings are protected by Group Policy
   - Restart the application and try again
   - Press Refresh Status to update the UI

2. Insufficient Resource Errors
   This error can occur when the system doesn't have the correct debug settings enabled.
   1) Open a Command Prompt or Terminal window as Administrator
   2) Type the following command and press Enter:
   
      bcdedit /debug on
   
   If you see a message about a debug transport, also run this command:
   
      bcdedit /dbgsettings local
   
   Reboot your computer for the changes to take effect.

3. How to Remove the Test Mode Watermark
   After enabling the bypass, you may see a ""Test Mode"" watermark in the bottom-right corner
   of your screen. You can remove it without disabling Test Mode itself.

   1) Download Universal Watermark Disabler.
   2) Run the application as administrator and click Install.
   3) Reboot your system.

";

            // Format the text
            rtbTroubleshoot.Font = new Font("Courier New", 10);
            tab.Controls.Add(rtbTroubleshoot);
        }

        private void AddButtonPair(Control parent, string label, int x, int y, out Button btnOn, out Button btnOff, string stateKey)
        {
            Label lbl = new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.WhiteSmoke,
                Width = 250
            };

            btnOn = new Button { Text = "ON", Location = new Point(x + 260, y), Width = 60, Height = 25 };
            btnOff = new Button { Text = "OFF", Location = new Point(x + 330, y), Width = 60, Height = 25 };

            UpdateButtonColors(btnOn, btnOff, stateKey);

            Button on = btnOn;
            Button off = btnOff;
            btnOn.Click += (s, e) => OnButtonClicked(on, off, stateKey, true);
            btnOff.Click += (s, e) => OnButtonClicked(on, off, stateKey, false);

            parent.Controls.Add(lbl);
            parent.Controls.Add(btnOn);
            parent.Controls.Add(btnOff);
        }

        private void AddDynamicButtonPair(Control parent, string label, int x, int y, out Button btnOn, out Button btnOff, out Label lblStatus, string stateKey)
        {
            Label lbl = new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.WhiteSmoke,
                Width = 250
            };

            btnOn = new Button { Text = "ON", Location = new Point(x + 260, y), Width = 60, Height = 25 };
            btnOff = new Button { Text = "OFF", Location = new Point(x + 330, y), Width = 60, Height = 25 };
            lblStatus = new Label
            {
                Text = "(checking...)",
                Location = new Point(x + 400, y),
                AutoSize = true,
                ForeColor = Color.Yellow,
                Font = new Font(parent.Font, FontStyle.Italic)
            };

            UpdateButtonColors(btnOn, btnOff, stateKey);

            Button on = btnOn;
            Button off = btnOff;
            btnOn.Click += (s, e) => OnButtonClicked(on, off, stateKey, true);
            btnOff.Click += (s, e) => OnButtonClicked(on, off, stateKey, false);

            parent.Controls.Add(lbl);
            parent.Controls.Add(btnOn);
            parent.Controls.Add(btnOff);
            parent.Controls.Add(lblStatus);
        }

        private void UpdateButtonColors(Button btnOn, Button btnOff, string stateKey)
        {
            string state = configManager.GetSetting(stateKey, "OFF");
            Color onColor = state == "ON" ? Color.Green : Color.FromArgb(64, 64, 64);
            Color offColor = state == "OFF" ? Color.Red : Color.FromArgb(64, 64, 64);

            btnOn.BackColor = onColor;
            btnOff.BackColor = offColor;
            btnOn.ForeColor = btnOff.ForeColor = Color.WhiteSmoke;
        }

        private void OnButtonClicked(Button btnOn, Button btnOff, string stateKey, bool isOn)
        {
            bool isManaged = IsSettingManagedByGroupPolicy(stateKey);

            if (isManaged)
            {
                DialogResult result = MessageBox.Show(
                    $"This setting is locked by Group Policy.\n\n" +
                    $"Would you like to attempt to force-disable it?\n\n" +
                    $"YES - Try to bypass Group Policy restriction\n" +
                    $"NO - Cancel",
                    "Setting Managed by Group Policy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;

                string forceCommand = GetForceCommandForState(stateKey, isOn);
                if (!string.IsNullOrEmpty(forceCommand))
                {
                    QueueCommand(forceCommand);
                    Log($"? Attempting to bypass Group Policy for {stateKey}...");
                }
            }
            else
            {
                string command = GetCommandForState(stateKey, isOn);
                if (!string.IsNullOrEmpty(command))
                    QueueCommand(command);
            }

            if (!chkStepMode.Checked)
            {
                configManager.SetSetting(stateKey, isOn ? "ON" : "OFF");
                UpdateButtonColors(btnOn, btnOff, stateKey);
            }
            else
            {
                pendingStateChanges[stateKey] = (isOn, btnOn, btnOff);
            }
        }

        private bool IsSettingManagedByGroupPolicy(string stateKey)
        {
            try
            {
                if (stateKey == "memintegrity")
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
                    {
                        if (key != null)
                        {
                            object managedObj = key.GetValue("Managed");
                            return managedObj != null && (int)managedObj == 1;
                        }
                    }
                }
                else if (stateKey == "vulndriver")
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Config"))
                    {
                        if (key != null)
                        {
                            object managedObj = key.GetValue("Managed");
                            return managedObj != null && (int)managedObj == 1;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private string GetCommandForState(string key, bool isOn)
        {
            return key switch
            {
                "hypervisor" => $"bcdedit /set hypervisorlaunchtype {(isOn ? "off" : "on")}",
                "testsigning" => $"bcdedit /set testsigning {(isOn ? "on" : "off")}",
                "nointegrity" => $"bcdedit /set nointegritychecks {(isOn ? "on" : "off")}",
                "memintegrity" => $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"Enabled\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f",
                "kernelstack" => $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"Hvacs\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f",
                "lsa" => $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa\" /v \"RunAsPPL\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f",
                "vulndriver" => $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config\" /v \"VulnerableDriverBlockListEnable\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f",
                _ => null
            };
        }

        private string GetForceCommandForState(string key, bool isOn)
        {
            return key switch
            {
                "memintegrity" => $"reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f && " +
                                  $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"Enabled\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f && " +
                                  $"reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"Managed\" /f",

                "vulndriver" => $"reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"VulnerableDriverBlocklistEnable\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f && " +
                                $"reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config\" /v \"VulnerableDriverBlockListEnable\" /t REG_DWORD /d {(isOn ? 0 : 1)} /f && " +
                                $"reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config\" /v \"Managed\" /f",

                _ => GetCommandForState(key, isOn)
            };
        }

        private void TurnAllWindowsOn() { OnButtonClicked(btnHypervisorOn, btnHypervisorOff, "hypervisor", true); OnButtonClicked(btnTestSigningOn, btnTestSigningOff, "testsigning", true); OnButtonClicked(btnNoIntegrityOn, btnNoIntegrityOff, "nointegrity", true); }
        private void TurnAllWindowsOff() { OnButtonClicked(btnHypervisorOn, btnHypervisorOff, "hypervisor", false); OnButtonClicked(btnTestSigningOn, btnTestSigningOff, "testsigning", false); OnButtonClicked(btnNoIntegrityOn, btnNoIntegrityOff, "nointegrity", false); }
        private void TurnAllDefenderOn() { if (featureAvailable.GetValueOrDefault("memintegrity", false)) OnButtonClicked(btnMemIntegrityOn, btnMemIntegrityOff, "memintegrity", true); if (featureAvailable.GetValueOrDefault("kernelstack", false)) OnButtonClicked(btnKernelStackOn, btnKernelStackOff, "kernelstack", true); if (featureAvailable.GetValueOrDefault("lsa", false)) OnButtonClicked(btnLSAOn, btnLSAOff, "lsa", true); if (featureAvailable.GetValueOrDefault("vulndriver", false)) OnButtonClicked(btnVulnDriverOn, btnVulnDriverOff, "vulndriver", true); }
        private void TurnAllDefenderOff() { if (featureAvailable.GetValueOrDefault("memintegrity", false)) OnButtonClicked(btnMemIntegrityOn, btnMemIntegrityOff, "memintegrity", false); if (featureAvailable.GetValueOrDefault("kernelstack", false)) OnButtonClicked(btnKernelStackOn, btnKernelStackOff, "kernelstack", false); if (featureAvailable.GetValueOrDefault("lsa", false)) OnButtonClicked(btnLSAOn, btnLSAOff, "lsa", false); if (featureAvailable.GetValueOrDefault("vulndriver", false)) OnButtonClicked(btnVulnDriverOn, btnVulnDriverOff, "vulndriver", false); }
        private void TurnAllOn(object sender, EventArgs e) { TurnAllWindowsOn(); TurnAllDefenderOn(); }
        private void TurnAllOff(object sender, EventArgs e) { TurnAllWindowsOff(); TurnAllDefenderOff(); }

        private void DetectAvailableFeatures() { featureAvailable["memintegrity"] = true; featureAvailable["kernelstack"] = true; featureAvailable["lsa"] = true; featureAvailable["vulndriver"] = true; UpdateFeatureAvailability(); }
        private void UpdateFeatureAvailability() { btnMemIntegrityOn.Enabled = btnMemIntegrityOff.Enabled = featureAvailable.GetValueOrDefault("memintegrity", false); lblMemIntegrityStatus.Text = featureAvailable.GetValueOrDefault("memintegrity", false) ? "" : "(Check if available)"; lblMemIntegrityStatus.ForeColor = featureAvailable.GetValueOrDefault("memintegrity", false) ? Color.Yellow : Color.Orange; btnKernelStackOn.Enabled = btnKernelStackOff.Enabled = featureAvailable.GetValueOrDefault("kernelstack", false); lblKernelStackStatus.Text = featureAvailable.GetValueOrDefault("kernelstack", false) ? "" : "(Requires newer CPU)"; lblKernelStackStatus.ForeColor = featureAvailable.GetValueOrDefault("kernelstack", false) ? Color.Yellow : Color.Orange; btnLSAOn.Enabled = btnLSAOff.Enabled = featureAvailable.GetValueOrDefault("lsa", false); lblLSAStatus.Text = featureAvailable.GetValueOrDefault("lsa", false) ? "" : "(Not available)"; lblLSAStatus.ForeColor = featureAvailable.GetValueOrDefault("lsa", false) ? Color.Yellow : Color.Orange; btnVulnDriverOn.Enabled = btnVulnDriverOff.Enabled = featureAvailable.GetValueOrDefault("vulndriver", false); lblVulnDriverStatus.Text = featureAvailable.GetValueOrDefault("vulndriver", false) ? "" : "(Check if available)"; lblVulnDriverStatus.ForeColor = featureAvailable.GetValueOrDefault("vulndriver", false) ? Color.Yellow : Color.Orange; }

        private void OpenConfigFile(object sender, EventArgs e) { try { string configPath = configManager.GetConfigPath(); if (File.Exists(configPath)) Process.Start(new ProcessStartInfo { FileName = configPath, UseShellExecute = true }); } catch (Exception ex) { Log($"Error: {ex.Message}"); } }
        private void Form1_Load(object sender, EventArgs e) { isLoading = true; DetectAvailableFeatures(); LoadButtonStates(); RefreshAllStatus(); isLoading = false; }
        private void LoadButtonStates() { UpdateButtonColors(btnHypervisorOn, btnHypervisorOff, "hypervisor"); UpdateButtonColors(btnTestSigningOn, btnTestSigningOff, "testsigning"); UpdateButtonColors(btnNoIntegrityOn, btnNoIntegrityOff, "nointegrity"); if (featureAvailable.GetValueOrDefault("memintegrity", false)) UpdateButtonColors(btnMemIntegrityOn, btnMemIntegrityOff, "memintegrity"); if (featureAvailable.GetValueOrDefault("kernelstack", false)) UpdateButtonColors(btnKernelStackOn, btnKernelStackOff, "kernelstack"); if (featureAvailable.GetValueOrDefault("lsa", false)) UpdateButtonColors(btnLSAOn, btnLSAOff, "lsa"); if (featureAvailable.GetValueOrDefault("vulndriver", false)) UpdateButtonColors(btnVulnDriverOn, btnVulnDriverOff, "vulndriver"); }

        private void RefreshAllStatus() { CheckBIOS(); CheckWindowsConfig(); CheckDefenderConfig(); CheckGameReady(); UpdateGlobalStatus(); gamesUI.UpdateGlobalStatus(biosReady, windowsReady, defenderReady); }

        private void CheckBIOS()
        {
            bool virtEnabled = false;
            try { using (var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_Processor")) { foreach (var obj in searcher.Get()) { virtEnabled = (bool)obj["VirtualizationFirmwareEnabled"]; break; } } }
            catch { try { using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\hv_host")) { virtEnabled = key != null; } } catch { virtEnabled = false; } }

            bool secureBootOff = false;
            try { var psi = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-Command \"Confirm-SecureBootUEFI\"", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true }; using (var p = Process.Start(psi)) { string output = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit(); secureBootOff = output.Equals("False", StringComparison.OrdinalIgnoreCase); } }
            catch { secureBootOff = false; }

            lblSecureBootStatus.Text = $"Secure Boot - {(secureBootOff ? "OFF" : "ON")}";
            lblSecureBootStatus.ForeColor = secureBootOff ? Color.Green : Color.Red;
            lblVirtStatus.Text = $"Virtualization - {(virtEnabled ? "ON" : "OFF")}";
            lblVirtStatus.ForeColor = virtEnabled ? Color.Green : Color.Red;
            biosReady = virtEnabled && secureBootOff;
        }

        private void CheckWindowsConfig()
        {
            string bcdOutput = RunCommandAndGetOutput("bcdedit /enum");
            bool hypervisorOff = bcdOutput.Contains("hypervisorlaunchtype") && bcdOutput.Contains("Off");
            bool testSigningOn = bcdOutput.Contains("testsigning") && bcdOutput.Contains("Yes");
            bool noIntegrityOn = bcdOutput.Contains("nointegritychecks") && bcdOutput.Contains("Yes");

            windowsReady = hypervisorOff && testSigningOn && noIntegrityOn;
            lblWindowsStatus.Text = windowsReady ? "READY" : "NOT READY";
            lblWindowsStatus.ForeColor = windowsReady ? Color.Green : Color.Red;
        }

        private void CheckDefenderConfig()
        {
            bool memIntegrityOff = false, kernelStackOff = false, lsaOff = false, vulnDriverOff = false;
            bool memIntegrityManaged = false, vulnDriverManaged = false;

            try
            {
                if (featureAvailable.GetValueOrDefault("memintegrity", false)) { using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity")) { if (key != null) { memIntegrityOff = (int)key.GetValue("Enabled", 1) == 0; object managedObj = key.GetValue("Managed"); memIntegrityManaged = managedObj != null && (int)managedObj == 1; } } }
                if (featureAvailable.GetValueOrDefault("kernelstack", false)) { using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management")) { if (key != null) kernelStackOff = (int)key.GetValue("Hvacs", 1) == 0; } }
                if (featureAvailable.GetValueOrDefault("lsa", false)) { using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa")) { if (key != null) lsaOff = (int)key.GetValue("RunAsPPL", 1) == 0; } }
                if (featureAvailable.GetValueOrDefault("vulndriver", false)) { using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Config")) { if (key != null) { vulnDriverOff = (int)key.GetValue("VulnerableDriverBlockListEnable", 1) == 0; object managedObj = key.GetValue("Managed"); vulnDriverManaged = managedObj != null && (int)managedObj == 1; } } }
            }
            catch { }

            bool requiredOff = true;
            if (featureAvailable.GetValueOrDefault("memintegrity", false)) requiredOff &= memIntegrityOff;
            if (featureAvailable.GetValueOrDefault("kernelstack", false)) requiredOff &= kernelStackOff;
            if (featureAvailable.GetValueOrDefault("lsa", false)) requiredOff &= lsaOff;
            if (featureAvailable.GetValueOrDefault("vulndriver", false)) requiredOff &= vulnDriverOff;

            defenderReady = requiredOff;

            if (memIntegrityManaged || vulnDriverManaged)
            {
                lblDefenderStatus.Text = "MANAGED BY GROUP POLICY (Cannot change)";
                lblDefenderStatus.ForeColor = Color.Orange;
            }
            else
            {
                lblDefenderStatus.Text = defenderReady ? "READY" : "NOT READY";
                lblDefenderStatus.ForeColor = defenderReady ? Color.Green : Color.Orange;
            }
        }

        private void CheckGameReady()
        {
            bool gameFolderExists = !string.IsNullOrEmpty(txtGameFolder.Text) && Directory.Exists(txtGameFolder.Text);
            bool exclusionsOk = configManager.GetSetting("defender_exclusions", "NOT_DONE") == "DONE";

            gameReady = gameFolderExists && exclusionsOk;
        }

        private void UpdateGlobalStatus() { bool global = biosReady && windowsReady && defenderReady; lblGlobalStatus.Text = global ? "GLOBAL STATUS: READY" : "GLOBAL STATUS: NOT READY"; lblGlobalStatus.ForeColor = global ? Color.Green : Color.Red; }

        private void QueueCommand(string command) 
        { 
            if (chkStepMode.Checked) 
            { 
                pendingCommands.AppendLine(command); 
                Log($"[Pending] {command}"); 
                UpdateSendButtonState(); 
            } 
            else 
            { 
                ExecuteCommand(command); 
            } 
        }

        private void UpdateSendButtonState() 
        { 
            btnSendCommand.Enabled = chkStepMode.Checked && pendingCommands.Length > 0; 
            btnSendCommand.Visible = chkStepMode.Checked; 
        }

        private void SendPendingCommands(object sender, EventArgs e)
        {
            if (pendingCommands.Length == 0) return;
            string allCommands = pendingCommands.ToString();
            Log("--- Executing pending commands ---");
            using (StringReader reader = new StringReader(allCommands))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        ExecuteCommand(line);
                }
            }

            pendingCommands.Clear();
            UpdateSendButtonState();
            Log("--- Commands executed ---");

            foreach (var change in pendingStateChanges)
            {
                configManager.SetSetting(change.Key, change.Value.isOn ? "ON" : "OFF");
                UpdateButtonColors(change.Value.on, change.Value.off, change.Key);
            }
            pendingStateChanges.Clear();

            if (pendingExclusionsSetting)
            {
                if (!string.IsNullOrEmpty(pendingExclusionPath))
                {
                    configManager.AddExclusion(pendingExclusionPath);
                    Log("✓ Exclusion saved to config");
                    pendingExclusionPath = null;
                }
                pendingExclusionsSetting = false;
            }

            isLoading = true;
            RefreshAllStatus();
            LoadButtonStates();
            isLoading = false;
        }

        private void ExecuteCommand(string command)
        {
            Log($"> {command}");
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = $"/c {command}";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (!string.IsNullOrEmpty(output))
                    Log(output);
                if (!string.IsNullOrEmpty(error))
                    Log("ERROR: " + error);
            }
            catch (Exception ex)
            {
                Log($"Exception: {ex.Message}");
            }
        }

        private string RunCommandAndGetOutput(string command)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = $"/c {command}";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output;
            }
            catch
            {
                return "";
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
                Invoke(new Action<string>(Log), message);
            else
                txtLog.AppendText(message + Environment.NewLine);
        }

        private void BrowseGameFolder(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtGameFolder.Text = fbd.SelectedPath;
                    RefreshAllStatus();
                }
            }
        }

        private void AddExclusionsToggled(object sender, EventArgs e)
        {
            if (isLoading) return;

            if (chkAddExclusions.Checked)
            {
                if (string.IsNullOrEmpty(txtGameFolder.Text) || !Directory.Exists(txtGameFolder.Text))
                {
                    MessageBox.Show("Please select a valid game folder first.");
                    chkAddExclusions.Checked = false;
                    return;
                }

                string gameFolderPath = txtGameFolder.Text;

                // Step 1: Check if already in config exclusions
                if (configManager.IsExcluded(gameFolderPath))
                {
                    MessageBox.Show($"Folder is already in exclusions:\n{gameFolderPath}", "Already Excluded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    chkAddExclusions.Checked = false;
                    return;
                }

                // Step 2: Check if folder is already in Windows Defender exclusions
                string checkCommand = "powershell -Command \"Get-MpPreference | Select-Object -ExpandProperty ExclusionPath\"";
                string exclusionsList = RunCommandAndGetOutput(checkCommand);

                if (exclusionsList.Contains(gameFolderPath))
                {
                    MessageBox.Show($"Folder is already in Windows Defender exclusions:\n{gameFolderPath}", "Already Excluded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Add to config even though it's already in Defender
                    if (!chkStepMode.Checked)
                    {
                        configManager.AddExclusion(gameFolderPath);
                        Log("✓ Exclusion added to config");
                    }
                    else
                    {
                        pendingExclusionsSetting = true;
                        Log("[Pending] Exclusion will be saved when commands execute");
                    }
                    
                    chkAddExclusions.Checked = false;
                    return;
                }

                // Step 3: Add to Windows Defender exclusions
                string addCommand = $"powershell -Command \"Add-MpPreference -ExclusionPath '{gameFolderPath}'\"";
                QueueCommand(addCommand);

                if (!chkStepMode.Checked)
                {
                    configManager.AddExclusion(gameFolderPath);
                    Log("✓ Folder added to exclusions and saved");
                }
                else
                {
                    pendingExclusionsSetting = true;
                    pendingExclusionPath = gameFolderPath;
                    Log("[Pending] Folder will be added to exclusions when commands execute");
                }
            }
            else
            {
                Log("Exclusions can be removed in Windows Security settings.");
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabConfiguration)
            {
                // Refresh status when switching back to configuration tab
                isLoading = true;
                RefreshAllStatus();
                LoadButtonStates();
                isLoading = false;
            }
        }
    }
}
