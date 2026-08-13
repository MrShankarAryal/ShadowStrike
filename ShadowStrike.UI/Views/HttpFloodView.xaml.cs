using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ShadowStrike.Core;
using ShadowStrike.UI;

namespace ShadowStrike.UI.Views
{
    public partial class HttpFloodView : UserControl
    {
        private HttpFlooder _flooder = new HttpFlooder();
        private BrowserFlooder _browserFlooder = new BrowserFlooder();
        private CancellationTokenSource? _cts;
        private DispatcherTimer _timer;
        private bool _isAttacking = false;

        public HttpFloodView()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;

            // Auto-fill from AppState
            var appState = AppState.Load();
            if (!string.IsNullOrEmpty(appState.TargetUrl))
            {
                TargetInput.Text = appState.TargetUrl;
            }

            this.Loaded += (s, e) =>
            {
                // Reload target URL
                var state = AppState.Load();
                if (!string.IsNullOrEmpty(state.TargetUrl))
                {
                    TargetInput.Text = state.TargetUrl;
                }

                // Reflect actual Tor state (global protection was initialized in MainWindow)
                if (TorManager.IsRunning)
                {
                    TorStatusText.Text = $"Tor: Active ✓  (Port {TorManager.TorPort} · Auto-rotation 7s)";
                    TorStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));
                }
                else
                {
                    TorStatusText.Text = "Tor: Not Available — requests use direct IP";
                    TorStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                }
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Requests sent — from BrowserFlooder JS counter
            RequestsText.Text = _browserFlooder.RequestCount.ToString("N0");

            // Failed browsers — real count, no longer hardcoded to 0
            FailedText.Text = _browserFlooder.FailedCount.ToString("N0");

            // IP Rotations — how many Tor NEWNYM signals succeeded this session
            RotationsText.Text = TorManager.RotationCount.ToString("N0");
        }

        private async void AttackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isAttacking)
            {
                // Stop Attack
                _cts?.Cancel();
                _flooder.Stop();
                _browserFlooder.Stop();
                _timer.Stop();

                // Freeze final counts
                Timer_Tick(null, EventArgs.Empty);

                AttackBtn.Content = "LAUNCH ATTACK";
                AttackBtn.Background = (Brush)FindResource("PrimaryHueMidBrush");
                StatusText.Text = "STOPPED";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0));
                _isAttacking = false;
            }
            else
            {
                var state = AppState.Load();
                if (!state.IsScanCompleted)
                {
                    CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var target = TargetInput.Text;
                if (string.IsNullOrWhiteSpace(target))
                {
                    CustomMessageBox.Show("Please enter a target URL.", "No Target", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!target.StartsWith("http://") && !target.StartsWith("https://"))
                {
                    target = "https://" + target;
                    TargetInput.Text = target;
                }

                int threads = (int)ThreadSlider.Value;
                int duration = (int)DurationSlider.Value;

                _cts = new CancellationTokenSource();
                if (duration > 0)
                {
                    _cts.CancelAfter(TimeSpan.FromSeconds(duration));
                }

                _timer.Start();

                AttackBtn.Content = "STOP ATTACK";
                AttackBtn.Background = new SolidColorBrush(Color.FromRgb(200, 30, 30));
                StatusText.Text = "ATTACKING";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 60, 60));
                _isAttacking = true;

                try
                {
                    // Always use Browser Flood — Tor routing handled globally by TorManager
                    await _browserFlooder.StartAttackAsync(target, threads, _cts.Token, useExternalTor: false);
                }
                catch (OperationCanceledException)
                {
                    // Normal duration-based stop — not an error
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Attack Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isAttacking = false;
                    _timer.Stop();
                    Timer_Tick(null, EventArgs.Empty); // Freeze final counts
                    AttackBtn.Content = "LAUNCH ATTACK";
                    AttackBtn.Background = (Brush)FindResource("PrimaryHueMidBrush");
                    StatusText.Text = "STOPPED";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0));
                }
            }
        }
    }
}
