using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ResolutionLauncher
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

}
