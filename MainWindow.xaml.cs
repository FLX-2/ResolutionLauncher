using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls;

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
            return Width + "×" + Height; 
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
            // Get current resolution to include it
            var currentRes = GetCurrentResolution();
            
            // Curated list of practical gaming resolutions
            var practicalResolutions = new[]
            {
                new Resolution(3840, 2160),  // 4K
                new Resolution(2560, 1440),  // 1440p
                new Resolution(1920, 1080),  // 1080p
                new Resolution(1600, 900),   // 900p
                new Resolution(1366, 768),   // Common laptop
                new Resolution(1280, 720),   // 720p
                currentRes                   // Always include current resolution
            };

            // Use HashSet to avoid duplicates, then convert to list
            var resolutions = new HashSet<Resolution>(practicalResolutions);

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

    public partial class MainWindow : Window
    {
        private readonly ResolutionManager _resolutionManager;

        public MainWindow()
        {
            InitializeComponent();
            _resolutionManager = new ResolutionManager();
            this.Loaded += (s, e) => {
                EnableMicaEffect();
                LoadAvailableResolutions();
            };
        }

        // For Mica effect
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private void EnableMicaEffect()
        {
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                try
                {
                    IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                    int trueValue = 1;
                    DwmSetWindowAttribute(windowHandle, 1029, ref trueValue, sizeof(int)); // DWMWA_MICA_EFFECT
                    
                    // Set background to a semi-transparent color to see the effect
                    this.Background = new SolidColorBrush(Color.FromArgb(217, 243, 243, 243));
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(128, 32, 32, 32));
                }
                catch (Exception)
                {
                    // Fallback for older systems
                    this.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) {} // Not implemented
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void LoadAvailableResolutions()
        {
            var resolutions = _resolutionManager.GetAvailableResolutions();
            ResolutionCombo.Items.Clear();
            
            foreach (var resolution in resolutions)
            {
                var item = new ComboBoxItem();
                item.Content = resolution.ToString();
                item.Tag = resolution;
                ResolutionCombo.Items.Add(item);
            }

            if (ResolutionCombo.Items.Count > 0)
            {
                ResolutionCombo.SelectedIndex = 0;
            }
        }

        private void ModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (ExeTarget == null || UwpTarget == null) return;
            bool isUwp = UwpMode.IsChecked == true;
            ExeTarget.Visibility = isUwp ? Visibility.Collapsed : Visibility.Visible;
            UwpTarget.Visibility = isUwp ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk|All files (*.*)|*.*",
                Title = "Select Application"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TargetPath.Text = openFileDialog.FileName;
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResolutionCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is Resolution resolution)
            {
                try
                {
                    if (UwpMode.IsChecked == true)
                    {
                        string appId = UwpPath.Text.Trim();
                        if (string.IsNullOrWhiteSpace(appId))
                        {
                            MessageBox.Show("Please enter a UWP app ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        LaunchUwpWithResolution(appId, resolution);
                    }
                    else
                    {
                        string filePath = TargetPath.Text;
                        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                        {
                            MessageBox.Show("Please select a valid application.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        LaunchWithResolution(filePath, resolution);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to launch application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LaunchWithResolution(string filePath, Resolution targetResolution)
        {
            var originalResolution = _resolutionManager.GetCurrentResolution();
            
            // Change resolution
            if (!_resolutionManager.ChangeResolution(targetResolution))
            {
                MessageBox.Show("Failed to change resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
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
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _resolutionManager.ChangeResolution(originalResolution);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error monitoring process: " + ex.Message);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                
                // Minimize the launcher
                this.WindowState = WindowState.Minimized;
            }
        }

        private void LaunchUwpWithResolution(string appId, Resolution targetResolution)
        {
            var originalResolution = _resolutionManager.GetCurrentResolution();
            
            // Change resolution
            if (!_resolutionManager.ChangeResolution(targetResolution))
            {
                MessageBox.Show("Failed to change resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Launch UWP app
            if (UwpManager.LaunchUwpApp(appId))
            {
                // Minimize the launcher
                this.WindowState = WindowState.Minimized;
                
                // Show a message to the user about manual resolution restore
                var result = MessageBox.Show(
                    $"UWP application launched with resolution {targetResolution}.\n\n" +
                    "Resolution will be restored when you click OK.\n" +
                    "Launch the app and then click OK when you're done playing.",
                    "UWP Launch", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Restore resolution when user clicks OK
                _resolutionManager.ChangeResolution(originalResolution);
            }
            else
            {
                // Restore resolution if launch failed
                _resolutionManager.ChangeResolution(originalResolution);
                MessageBox.Show("Failed to launch UWP application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResolutionCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is Resolution resolution)
            {
                try
                {
                    string appName;
                    string shortcutName;
                    
                    if (UwpMode.IsChecked == true)
                    {
                        string appId = UwpPath.Text.Trim();
                        if (string.IsNullOrWhiteSpace(appId))
                        {
                            MessageBox.Show("Please enter a UWP app ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        appName = ExtractAppNameFromId(appId);
                        shortcutName = appName + " (" + resolution.ToString() + ")";
                        CreateUwpDesktopShortcut(shortcutName, appId, resolution);
                    }
                    else
                    {
                        string filePath = TargetPath.Text;
                        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                        {
                            MessageBox.Show("Please select a valid application.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        appName = Path.GetFileNameWithoutExtension(filePath);
                        shortcutName = appName + " (" + resolution.ToString() + ")";
                        CreateDesktopShortcut(shortcutName, filePath, resolution);
                    }
                    
                    MessageBox.Show($"Shortcut '{shortcutName}' created on desktop!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create shortcut: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var launcherPath = GetExecutablePath();

            // Create a proper .lnk shortcut
            CreateWindowsShortcut(shortcutPath, launcherPath, $"\"{targetPath}\" {resolution.Width}x{resolution.Height}");
        }

        private void CreateUwpDesktopShortcut(string shortcutName, string appId, Resolution resolution)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");
            var launcherPath = GetExecutablePath();

            // Create shortcut for UWP apps
            CreateWindowsShortcut(shortcutPath, launcherPath, $"\"uwp:{appId}\" {resolution.Width}x{resolution.Height}");
        }

        private string GetExecutablePath()
        {
            // Get the actual executable path, not the DLL
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
            {
                return processPath;
            }

            // Fallback: try to find the .exe in the same directory as the assembly
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                // For single-file apps, use base directory
                assemblyLocation = System.AppContext.BaseDirectory;
            }
            var directory = Path.GetDirectoryName(assemblyLocation);
            var exeName = Path.GetFileNameWithoutExtension(assemblyLocation) + ".exe";
            var exePath = Path.Combine(directory!, exeName);
            
            if (File.Exists(exePath))
            {
                return exePath;
            }

            // Last resort: return the assembly location (might be DLL but better than nothing)
            return assemblyLocation;
        }

        private void CreateWindowsShortcut(string shortcutPath, string targetPath, string arguments)
        {
            // Create shortcut using WScript.Shell COM object
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
    }
}
