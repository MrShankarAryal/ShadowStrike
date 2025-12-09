using System;
using System.Diagnostics;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShadowStrike.UI.Views;

namespace ShadowStrike.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new DashboardView());
            LogMessage("ShadowStrike initialized successfully");
            
            // Global Tor Initialization
            InitializeGlobalProtection();
        }

        private async void InitializeGlobalProtection()
        {
            LogMessage("Initializing Global Security Shield...");
            GlobalTorStatus.Text = "Tor: Starting...";
            
            bool success = await ShadowStrike.Core.TorManager.StartTorAsync();
            if (success)
            {
                ShadowStrike.Core.TorManager.StartRotationService(7); // 7s Rotation
                
                // Update Status Bar
                GlobalTorStatus.Text = "Tor: Connected (Global)";
                GlobalTorStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136)); // Green
                
                GlobalTorPort.Text = $"Port: {ShadowStrike.Core.TorManager.TorPort}";
                
                GlobalRotationStatus.Text = "IP Rotation: Active (7s)";
                GlobalRotationStatus.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));

                LogMessage($"Global Protection Active (Tor Port {ShadowStrike.Core.TorManager.TorPort} | Auto-Rotation: 7s)");
            }
            else
            {
                GlobalTorStatus.Text = "Tor: Failed";
                GlobalTorStatus.Foreground = Brushes.Red;
                LogMessage("Warning: Failed to initialize Global Tor. Please ensure Tor is installed or run manually.");
            }
        }

        public void LogMessage(string message)
        {
            Logger.Log(message);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardView());
            LogMessage("Navigated to Dashboard");
        }

        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            var logsView = new LogsView();
            logsView.OnLoadReport += (s, report) =>
            {
                DashboardView.LoadReport(report);
                MainFrame.Navigate(new DashboardView());
                LogMessage($"Loaded historical report for {report.Target}");
            };
            MainFrame.Navigate(logsView);
            LogMessage("Navigated to Logs");
        }

        private void HttpFlood_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HttpFloodView());
            LogMessage("Navigated to HTTP Flood");
        }

        private void UdpFlood_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UdpFloodView());
            LogMessage("Navigated to UDP Flood");
        }

        private void SynFlood_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SynFloodView());
            LogMessage("Navigated to SYN Flood");
        }

        private void Terminal_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TerminalView());
            LogMessage("Navigated to Terminal");
        }

        private void Injection_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new InjectionView());
            LogMessage("Navigated to Injection Testing");
        }

        private void Ransomware_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RansomwareView());
            LogMessage("Navigated to Ransomware Module");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new Window
            {
                Title = "About ShadowStrike",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#161B22")),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ShadowStrike.UI;component/img/eagle.ico"))
            };

            var grid = new Grid { Margin = new Thickness(30) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            var titleText = new TextBlock
            {
                Text = "ShadowStrike",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var versionText = new TextBlock
            {
                Text = "Version 2.0",
                FontSize = 16,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#58A6FF")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };
            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(versionText);
            Grid.SetRow(headerPanel, 0);

            // Content
            var contentText = new TextBlock
            {
                Text = "Advanced Security Testing & Penetration Testing Tool\n\n" +
                       "Developed by: Shankar Aryal\n" +
                       "Email: ShadowStrikeContact@shankararyal404.com.np\n\n" +
                       "Source Code & Releases: https://github.com/MrShankarAryal/ShadowStrike\n\n" +
                       "Built with .NET 8 & Material Design\n\n" +
                       "© 2025 Shankar Aryal. All rights reserved.",
                FontSize = 13,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C9D1D9")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetRow(contentText, 1);

            // Close Button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 100,
                Height = 35,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#238636")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeButton.Click += (s, args) => aboutWindow.Close();
            Grid.SetRow(closeButton, 2);

            grid.Children.Add(headerPanel);
            grid.Children.Add(contentText);
            grid.Children.Add(closeButton);
            aboutWindow.Content = grid;
            aboutWindow.ShowDialog();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpWindow = new HelpWindow();
                helpWindow.Owner = this;
                helpWindow.ShowDialog();
                LogMessage("Opened Help window");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Help: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogMessage($"Error opening Help: {ex.Message}");
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaxRestoreIcon.Kind = PackIconKind.WindowMaximize;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaxRestoreIcon.Kind = PackIconKind.WindowRestore;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
