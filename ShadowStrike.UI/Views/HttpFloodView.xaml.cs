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
        // private int _torPort = 9050; // REMOVED - Using Global TorManager
        private System.Net.CookieContainer _bypassedCookies;
        private string _bypassedUserAgent;

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

            // Check Tor connectivity - REMOVED (Handled Globally by MainWindow)
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
            RequestsText.Text = _browserFlooder.RequestCount.ToString("N0");
            FailedText.Text = "0"; 
        }

        // AttackModeCombo_SelectionChanged Removed
        
        // BypassBtn_Click Removed

        private async void AttackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isAttacking)
            {
                // Stop Attack
                _cts?.Cancel();
                _flooder.Stop();
                _browserFlooder.Stop();
                _timer.Stop();
                
                AttackBtn.Content = "LAUNCH ATTACK";
                AttackBtn.Background = (Brush)FindResource("PrimaryHueMidBrush"); // Restore original color
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
                int duration = (int)DurationSlider.Value;
                
                _cts = new CancellationTokenSource();
                
                if (duration > 0)
                {
                    _cts.CancelAfter(TimeSpan.FromSeconds(duration));
                }

                _timer.Start();
                
                AttackBtn.Content = "STOP ATTACK";
                AttackBtn.Background = Brushes.Red;
                StatusText.Text = "ATTACKING";
                StatusText.Foreground = Brushes.Red;
                _isAttacking = true;


                try
                {
                    // ALWAYS use Browser Flood (Bypass Mode)
                    // ALWAYS use Integrated Tor (User Request)
                    bool useExternalTor = false; 
                    
                    await _browserFlooder.StartAttackAsync(target, threads, _cts.Token, useExternalTor);
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
