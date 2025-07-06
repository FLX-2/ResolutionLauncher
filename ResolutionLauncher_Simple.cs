using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ResolutionLauncher
{
    public struct Resolution
    {
        private readonly int _width;
        private readonly int _height;
        
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }

        public Resolution(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public override string ToString() 
        { 
            return Width + "x" + Height; 
        }
    }

    public class ResolutionManager
    {
        [DllImport("user32.dll")]
        private static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int CDS_UPDATEREGISTRY = 0x01;
        private const int CDS_TEST = 0x02;
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        public Resolution GetCurrentResolution()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            return new Resolution(width, height);
        }

        public List<Resolution> GetAvailableResolutions()
        {
            var resolutions = new HashSet<Resolution>();
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            int modeIndex = 0;
            while (EnumDisplaySettings(null, modeIndex, ref devMode) != 0)
            {
                var resolution = new Resolution(devMode.dmPelsWidth, devMode.dmPelsHeight);
                resolutions.Add(resolution);
                modeIndex++;
            }

            // Add some common resolutions
            var commonResolutions = new[]
            {
                new Resolution(1920, 1080),
                new Resolution(1680, 1050),
                new Resolution(1600, 900),
                new Resolution(1440, 900),
                new Resolution(1366, 768),
                new Resolution(1280, 1024),
                new Resolution(1280, 800),
                new Resolution(1280, 720),
                new Resolution(1024, 768),
                new Resolution(800, 600)
            };

            foreach (var res in commonResolutions)
            {
                resolutions.Add(res);
            }

            return resolutions
                .OrderByDescending(r => r.Width)
                .ThenByDescending(r => r.Height)
                .ToList();
        }

        public bool ChangeResolution(Resolution resolution)
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode) == 0)
            {
                return false;
            }

            devMode.dmPelsWidth = resolution.Width;
            devMode.dmPelsHeight = resolution.Height;
            devMode.dmFields = 0x80000 | 0x100000;

            int result = ChangeDisplaySettings(ref devMode, CDS_TEST);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                return false;
            }

            result = ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY);
            return result == DISP_CHANGE_SUCCESSFUL;
        }
    }

    public class UwpManager
    {
        public static bool LaunchUwpApp(string appId)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:AppsFolder\\" + appId,
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public partial class MainForm : Form
    {
        private readonly ResolutionManager _resolutionManager;
        
        private RadioButton _desktopAppRadio;
        private RadioButton _uwpAppRadio;
        private TextBox _filePathTextBox;
        private Button _browseButton;
        private TextBox _uwpAppTextBox;
        private ComboBox _resolutionComboBox;
        private Button _launchButton;
        private Button _createShortcutButton;
        private Label _statusLabel;
        
        private bool _isUwpMode = false;
        private string _selectedUwpAppId = "";

        public MainForm()
        {
            _resolutionManager = new ResolutionManager();
            InitializeComponent();
            LoadAvailableResolutions();
            
            // Ensure desktop mode is properly initialized
            _isUwpMode = false;
            _desktopAppRadio.Checked = true;
            _uwpAppRadio.Checked = false;
            _filePathTextBox.Visible = true;
            _browseButton.Visible = true;
            _uwpAppTextBox.Visible = false;
        }

        private void InitializeComponent()
        {
            // Simple form setup
            Text = "Resolution Launcher";
            Size = new Size(480, 280);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // App type selection
            _desktopAppRadio = new RadioButton
            {
                Text = "Desktop App (.exe, .lnk)",
                Location = new Point(12, 15),
                Size = new Size(180, 20),
                Checked = true
            };
            _desktopAppRadio.CheckedChanged += AppTypeRadio_CheckedChanged;

            _uwpAppRadio = new RadioButton
            {
                Text = "UWP/Store App",
                Location = new Point(200, 15),
                Size = new Size(120, 20)
            };
            _uwpAppRadio.CheckedChanged += AppTypeRadio_CheckedChanged;

            // File selection
            _filePathTextBox = new TextBox
            {
                Location = new Point(12, 45),
                Size = new Size(350, 23),
                ReadOnly = true
            };

            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(370, 45),
                Size = new Size(80, 23)
            };
            _browseButton.Click += BrowseButton_Click;

            _uwpAppTextBox = new TextBox
            {
                Location = new Point(12, 45),
                Size = new Size(350, 23),
                Visible = false
            };
            _uwpAppTextBox.TextChanged += UwpAppTextBox_TextChanged;

            // Resolution selection
            var resolutionLabel = new Label
            {
                Text = "Target Resolution:",
                Location = new Point(12, 85),
                Size = new Size(120, 23)
            };

            _resolutionComboBox = new ComboBox
            {
                Location = new Point(12, 105),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _resolutionComboBox.SelectedIndexChanged += (s, e) => UpdateButtonStates();

            // Action buttons
            _launchButton = new Button
            {
                Text = "Launch Now",
                Location = new Point(12, 145),
                Size = new Size(100, 30),
                Enabled = false
            };
            _launchButton.Click += LaunchButton_Click;

            _createShortcutButton = new Button
            {
                Text = "Create Shortcut",
                Location = new Point(125, 145),
                Size = new Size(120, 30),
                Enabled = false
            };
            _createShortcutButton.Click += CreateShortcutButton_Click;

            // Status
            _statusLabel = new Label
            {
                Text = "Ready",
                Location = new Point(12, 190),
                Size = new Size(450, 40),
                ForeColor = Color.DarkGreen
            };

            // Add all controls
            Controls.AddRange(new Control[]
            {
                _desktopAppRadio, _uwpAppRadio,
                _filePathTextBox, _browseButton, _uwpAppTextBox,
                resolutionLabel, _resolutionComboBox,
                _launchButton, _createShortcutButton,
                _statusLabel
            });

            // Event handlers
            _filePathTextBox.TextChanged += (s, e) => UpdateButtonStates();
        }

        private void LoadAvailableResolutions()
        {
            var resolutions = _resolutionManager.GetAvailableResolutions();
            _resolutionComboBox.Items.Clear();
            
            foreach (var resolution in resolutions)
            {
                _resolutionComboBox.Items.Add(resolution.Width + "x" + resolution.Height);
            }

            if (_resolutionComboBox.Items.Count > 0)
            {
                _resolutionComboBox.SelectedIndex = 0;
            }
        }


        private void AppTypeRadio_CheckedChanged(object sender, EventArgs e)
        {
            // Update mode based on which radio button is currently checked
            _isUwpMode = _uwpAppRadio.Checked;
            
            // Toggle visibility based on mode
            _filePathTextBox.Visible = !_isUwpMode;
            _browseButton.Visible = !_isUwpMode;
            _uwpAppTextBox.Visible = _isUwpMode;
            
            // Clear selections when switching modes
            _selectedUwpAppId = "";
            if (_uwpAppTextBox != null)
                _uwpAppTextBox.Text = "";
            
            // Update status based on current mode
            if (_isUwpMode)
            {
                _statusLabel.Text = "Enter UWP App ID (e.g., Microsoft.MinecraftUWP_8wekyb3d8bbwe!App)";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            else
            {
                _statusLabel.Text = "Browse for a desktop application";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            
            UpdateButtonStates();
        }

        private void UwpAppTextBox_TextChanged(object sender, EventArgs e)
        {
            _selectedUwpAppId = _uwpAppTextBox.Text.Trim();
            UpdateButtonStates();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Desktop Application";
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|Shortcut Files (*.lnk)|*.lnk|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _filePathTextBox.Text = openFileDialog.FileName;
                }
            }
        }

        private void UpdateButtonStates()
        {
            bool hasApp = false;
            bool hasResolution = _resolutionComboBox.SelectedIndex >= 0;
            
            if (_isUwpMode)
            {
                hasApp = !string.IsNullOrWhiteSpace(_selectedUwpAppId);
            }
            else
            {
                hasApp = !string.IsNullOrWhiteSpace(_filePathTextBox.Text) && File.Exists(_filePathTextBox.Text);
            }
            
            _launchButton.Enabled = hasApp && hasResolution;
            _createShortcutButton.Enabled = hasApp && hasResolution;
            
            // Simple status updates
            if (hasApp && hasResolution)
            {
                _statusLabel.Text = "Ready to launch!";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            else if (!hasApp)
            {
                _statusLabel.Text = _isUwpMode ? "Select a UWP app" : "Browse for an app";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            else if (!hasResolution)
            {
                _statusLabel.Text = "Select a resolution";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
        }

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            if (_resolutionComboBox.SelectedIndex < 0)
                return;

            var resolutionText = _resolutionComboBox.SelectedItem.ToString();
            var resolution = ParseResolution(resolutionText);

            if (resolution.Width == 0 || resolution.Height == 0)
            {
                ShowError("Invalid resolution selected.");
                return;
            }

            try
            {
                _statusLabel.Text = "Launching application...";
                _statusLabel.ForeColor = Color.Blue;
                
                if (_isUwpMode)
                {
                    LaunchUwpWithResolution(_selectedUwpAppId, resolution);
                }
                else
                {
                    var filePath = _filePathTextBox.Text;
                    if (string.IsNullOrWhiteSpace(filePath))
                        return;
                    LaunchWithResolution(filePath, resolution);
                }
                
                _statusLabel.Text = "Application launched successfully";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                ShowError("Failed to launch application: " + ex.Message);
            }
        }

        private void CreateShortcutButton_Click(object sender, EventArgs e)
        {
            if (_resolutionComboBox.SelectedIndex < 0)
                return;

            var resolutionText = _resolutionComboBox.SelectedItem.ToString();
            var resolution = ParseResolution(resolutionText);

            if (resolution.Width == 0 || resolution.Height == 0)
            {
                ShowError("Invalid resolution selected.");
                return;
            }

            try
            {
                string appName;
                string shortcutName;
                
                if (_isUwpMode)
                {
                    // Extract app name from App ID (e.g., "Microsoft.MinecraftUWP_8wekyb3d8bbwe!App" -> "MinecraftUWP")
                    appName = ExtractAppNameFromId(_selectedUwpAppId);
                    shortcutName = appName + " (" + resolutionText + ")";
                    CreateUwpDesktopShortcut(shortcutName, _selectedUwpAppId, resolution);
                }
                else
                {
                    var filePath = _filePathTextBox.Text;
                    if (string.IsNullOrWhiteSpace(filePath))
                        return;
                    appName = Path.GetFileNameWithoutExtension(filePath);
                    shortcutName = appName + " (" + resolutionText + ")";
                    CreateDesktopShortcut(shortcutName, filePath, resolution);
                }
                
                _statusLabel.Text = "Shortcut '" + shortcutName + "' created on desktop";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                ShowError("Failed to create shortcut: " + ex.Message);
            }
        }

        private string ExtractAppNameFromId(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId))
                return "UWP App";
                
            try
            {
                // Extract from patterns like "Microsoft.MinecraftUWP_8wekyb3d8bbwe!App"
                var parts = appId.Split('!')[0]; // Remove "!App" part
                var namePart = parts.Split('.'); // Split by dots
                
                if (namePart.Length >= 2)
                {
                    // Take the second part (e.g., "MinecraftUWP" from "Microsoft.MinecraftUWP")
                    return namePart[1].Replace("_", " ");
                }
                
                return parts.Replace("Microsoft.", "").Replace("_", " ");
            }
            catch
            {
                return "UWP App";
            }
        }

        private void CreateDesktopShortcut(string shortcutName, string targetPath, Resolution resolution)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");
            var launcherPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Create a simple batch file instead of a complex COM shortcut for simplicity
            var batchPath = Path.Combine(desktopPath, shortcutName + ".bat");
            var batchContent = "@echo off\r\n\"" + launcherPath + "\" \"" + targetPath + "\" \"" + resolution.ToString() + "\"";
            
            File.WriteAllText(batchPath, batchContent);
            
            // Also create a proper .lnk shortcut if possible
            try
            {
                CreateWindowsShortcut(shortcutPath, launcherPath, "\"" + targetPath + "\" " + resolution.ToString());
            }
            catch
            {
                // If COM shortcut creation fails, the batch file will work as backup
            }
        }

        private void CreateWindowsShortcut(string shortcutPath, string targetPath, string arguments)
        {
            // Simple shortcut creation using WScript.Shell COM object
            var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            var shortcut = shell.GetType().InvokeMember("CreateShortcut", 
                System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            
            shortcut.GetType().InvokeMember("TargetPath", 
                System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcut.GetType().InvokeMember("Arguments", 
                System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
            shortcut.GetType().InvokeMember("Save", 
                System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private Resolution ParseResolution(string resolutionText)
        {
            var parts = resolutionText.Split('x');
            int width, height;
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out width) && 
                int.TryParse(parts[1], out height))
            {
                return new Resolution(width, height);
            }
            return new Resolution(0, 0);
        }

        private void LaunchWithResolution(string filePath, Resolution targetResolution)
        {
            var originalResolution = _resolutionManager.GetCurrentResolution();
            
            // Change resolution
            _resolutionManager.ChangeResolution(targetResolution);
            
            // Launch application
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            if (process != null)
            {
                // Monitor process in background thread
                var thread = new Thread(() =>
                {
                    try
                    {
                        process.WaitForExit();
                        // Restore original resolution
                        _resolutionManager.ChangeResolution(originalResolution);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error monitoring process: " + ex.Message);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void CreateUwpDesktopShortcut(string shortcutName, string appId, Resolution resolution)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var launcherPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Create a batch file for UWP apps
            var batchPath = Path.Combine(desktopPath, shortcutName + ".bat");
            var batchContent = "@echo off\r\n\"" + launcherPath + "\" \"uwp:" + appId + "\" \"" + resolution.ToString() + "\"";
            
            File.WriteAllText(batchPath, batchContent);
        }

        private void LaunchUwpWithResolution(string appId, Resolution targetResolution)
        {
            var originalResolution = _resolutionManager.GetCurrentResolution();
            
            // Change resolution
            _resolutionManager.ChangeResolution(targetResolution);
            
            // Launch UWP app
            if (UwpManager.LaunchUwpApp(appId))
            {
                // For UWP apps, we can't directly monitor the process, so we'll use a timer
                // to restore resolution after a delay or when the user manually closes
                var restoreTimer = new System.Windows.Forms.Timer();
                restoreTimer.Interval = 5000; // Check every 5 seconds
                restoreTimer.Tick += (s, e) =>
                {
                    // This is a simplified approach - in a real implementation,
                    // you might want to check if the UWP app is still running
                    // For now, we'll just show a message to the user
                };
                restoreTimer.Start();
                
                // Show a message to the user about manual resolution restore
                MessageBox.Show(
                    "UWP application launched with resolution " + targetResolution.ToString() + ".\n\n" +
                    "Note: Resolution will need to be manually restored when you exit the game.\n" +
                    "Click OK to restore resolution now, or close this dialog to keep the new resolution.",
                    "UWP Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Restore resolution when user clicks OK
                _resolutionManager.ChangeResolution(originalResolution);
                restoreTimer.Stop();
                restoreTimer.Dispose();
            }
            else
            {
                // Restore resolution if launch failed
                _resolutionManager.ChangeResolution(originalResolution);
                throw new Exception("Failed to launch UWP application");
            }
        }

        private void ShowError(string message)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = Color.Red;
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if launched with command line arguments
            if (args.Length >= 2)
            {
                var targetPath = args[0];
                var resolutionText = args[1];
                
                Resolution resolution;
                if (TryParseResolution(resolutionText, out resolution))
                {
                    if (targetPath.StartsWith("uwp:"))
                    {
                        // UWP app launch
                        var appId = targetPath.Substring(4);
                        LaunchUwpWithResolution(appId, resolution);
                        return;
                    }
                    else if (File.Exists(targetPath))
                    {
                        // Desktop app launch
                        LaunchWithResolution(targetPath, resolution);
                        return;
                    }
                }
            }

            Application.Run(new MainForm());
        }

        private static bool TryParseResolution(string resolutionText, out Resolution resolution)
        {
            resolution = new Resolution(0, 0);
            var parts = resolutionText.Split('x');
            
            int width, height;
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out width) && 
                int.TryParse(parts[1], out height))
            {
                resolution = new Resolution(width, height);
                return true;
            }
            
            return false;
        }

        private static void LaunchWithResolution(string filePath, Resolution targetResolution)
        {
            var resolutionManager = new ResolutionManager();
            var originalResolution = resolutionManager.GetCurrentResolution();
            
            try
            {
                resolutionManager.ChangeResolution(targetResolution);
                
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching application: " + ex.Message, "Resolution Launcher", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                resolutionManager.ChangeResolution(originalResolution);
            }
        }

        private static void LaunchUwpWithResolution(string appId, Resolution targetResolution)
        {
            var resolutionManager = new ResolutionManager();
            var originalResolution = resolutionManager.GetCurrentResolution();
            
            try
            {
                resolutionManager.ChangeResolution(targetResolution);
                
                if (UwpManager.LaunchUwpApp(appId))
                {
                    MessageBox.Show(
                        "UWP application launched with resolution " + targetResolution.ToString() + ".\n\n" +
                        "Click OK to restore your original resolution when you're done playing.",
                        "UWP Launch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    throw new Exception("Failed to launch UWP application");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching UWP application: " + ex.Message, "Resolution Launcher", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                resolutionManager.ChangeResolution(originalResolution);
            }
        }
    }
}