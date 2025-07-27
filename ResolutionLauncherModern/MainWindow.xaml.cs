using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls;

namespace ResolutionLauncherModern
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => EnableMicaEffect();
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
            string target = UwpMode.IsChecked == true ? UwpPath.Text : TargetPath.Text;
            if (ResolutionCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                string? resolution = selectedItem.Content.ToString();
                // Launch logic would go here
                MessageBox.Show($"Launching {target} at {resolution}");
            }
            else
            {
                MessageBox.Show("Please select a resolution.");
            }
        }
    }
}
