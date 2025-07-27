using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ResolutionLauncherModern
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if launched with command line arguments (from shortcuts)
            if (e.Args.Length >= 2)
            {
                var targetPath = e.Args[0];
                var resolutionText = e.Args[1];
                
                if (TryParseResolution(resolutionText, out Resolution resolution))
                {
                    if (targetPath.StartsWith("uwp:"))
                    {
                        // UWP app launch
                        var appId = targetPath.Substring(4);
                        LaunchUwpWithResolution(appId, resolution);
                        Shutdown();
                        return;
                    }
                    else if (File.Exists(targetPath))
                    {
                        // Desktop app launch
                        LaunchWithResolution(targetPath, resolution);
                        Shutdown();
                        return;
                    }
                }
            }

            // Normal startup
            base.OnStartup(e);
        }

        private bool TryParseResolution(string resolutionText, out Resolution resolution)
        {
            resolution = new Resolution(0, 0);
            var parts = resolutionText.Split('x');
            
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int width) && 
                int.TryParse(parts[1], out int height))
            {
                resolution = new Resolution(width, height);
                return true;
            }
            
            return false;
        }

        private void LaunchWithResolution(string filePath, Resolution targetResolution)
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
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                resolutionManager.ChangeResolution(originalResolution);
            }
        }

        private void LaunchUwpWithResolution(string appId, Resolution targetResolution)
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
                        "UWP Launch", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("Failed to launch UWP application");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching UWP application: " + ex.Message, "Resolution Launcher", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                resolutionManager.ChangeResolution(originalResolution);
            }
        }
    }

    public class BrushLightenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color color = brush.Color;
                return new SolidColorBrush(Color.FromArgb(color.A, (byte)(color.R * 1.2), (byte)(color.G * 1.2), (byte)(color.B * 1.2)));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BrushDarkenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color color = brush.Color;
                return new SolidColorBrush(Color.FromArgb(color.A, (byte)(color.R * 0.8), (byte)(color.G * 0.8), (byte)(color.B * 0.8)));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
