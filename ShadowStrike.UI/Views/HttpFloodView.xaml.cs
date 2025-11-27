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
        private CancellationTokenSource? _cts;
        private DispatcherTimer _timer;
        private bool _isAttacking = false;

        public HttpFloodView()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;

            // Auto-fill from AppState
            var appState = AppState.Load();
            if (!string.IsNullOrEmpty(appState.TargetUrl))
            {
                TargetInput.Text = appState.TargetUrl;
            }

            // Check if scan is required
            // Check if scan is required
            this.Loaded += (s, e) =>
            {
                var state = AppState.Load();
                if (!string.IsNullOrEmpty(state.TargetUrl))
                {
                    TargetInput.Text = state.TargetUrl;
                }
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            RequestsText.Text = _flooder.RequestCount.ToString("N0");
            FailedText.Text = _flooder.FailedCount.ToString("N0");
        }

        private async void AttackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isAttacking)
            {
                // Stop Attack
                _cts?.Cancel();
                _flooder.Stop();
                _timer.Stop();
                
                AttackBtn.Content = "LAUNCH ATTACK";
                AttackBtn.Background = Brushes.Purple;
                StatusText.Text = "STOPPED";
                StatusText.Foreground = Brushes.Orange;
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

                // Start Attack
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
                
                _cts = new CancellationTokenSource();
                _timer.Start();
                
                AttackBtn.Content = "STOP ATTACK";
                AttackBtn.Background = Brushes.Red;
                StatusText.Text = "ATTACKING";
                StatusText.Foreground = Brushes.Red;
                _isAttacking = true;

                try
                {
                    await _flooder.StartAttackAsync(target, threads, _cts.Token);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Attack Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _isAttacking = false;
                    AttackBtn.Content = "LAUNCH ATTACK";
                    _timer.Stop();
                }
            }
        }
    }
}
