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
    public partial class UdpFloodView : UserControl
    {
        private UdpFlooder _flooder = new UdpFlooder();
        private CancellationTokenSource? _cts;
        private DispatcherTimer _timer;
        private bool _isAttacking = false;

        public UdpFloodView()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;

            // Auto-fill from AppState
            var appState = AppState.Load();
            if (!string.IsNullOrEmpty(appState.TargetIP))
            {
                IpInput.Text = appState.TargetIP;
            }
            
            // Try to parse first port
            if (!string.IsNullOrEmpty(appState.OpenPorts))
            {
                var ports = appState.OpenPorts.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (ports.Length > 0 && int.TryParse(ports[0], out int port))
                {
                    PortInput.Text = port.ToString();
                }
            }

            // Check if scan is required
            // Check if scan is required
            this.Loaded += (s, e) =>
            {
                var state = AppState.Load();
                if (!string.IsNullOrEmpty(state.TargetIP))
                {
                    IpInput.Text = state.TargetIP;
                    
                    if (!string.IsNullOrEmpty(state.OpenPorts))
                    {
                        var ports = state.OpenPorts.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (ports.Length > 0 && int.TryParse(ports[0], out int port))
                        {
                            PortInput.Text = port.ToString();
                        }
                    }
                }
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isAttacking)
            {
                PacketsText.Text = _flooder.PacketsSent.ToString("N0");
            }
        }

        private async void AttackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAttacking)
            {
                var state = AppState.Load();
                if (!state.IsScanCompleted)
                {
                    CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!System.Net.IPAddress.TryParse(IpInput.Text, out var ip))
                {
                    MessageBox.Show("Invalid IP Address.");
                    return;
                }

                if (!int.TryParse(PortInput.Text, out int port))
                {
                    MessageBox.Show("Invalid Port.");
                    return;
                }

                int threads = (int)ThreadSlider.Value;
                
                _isAttacking = true;
                AttackBtn.Content = "STOP ATTACK";
                AttackBtn.Background = new SolidColorBrush(Color.FromRgb(255, 50, 50));
                StatusText.Text = "ATTACKING";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 50, 50));
                
                _cts = new CancellationTokenSource();
                _timer.Start();

                try 
                {
                    await _flooder.StartAttackAsync(ip.ToString(), port, threads, _cts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    MessageBox.Show($"Attack Error: {ex.Message}");
                    StopAttack();
                }
            }
            else
            {
                StopAttack();
            }
        }

        private void StopAttack()
        {
            _isAttacking = false;
            _cts?.Cancel();
            _timer.Stop();
            
            AttackBtn.Content = "LAUNCH ATTACK";
            AttackBtn.Background = new SolidColorBrush(Color.FromRgb(0, 170, 255));
            StatusText.Text = "IDLE";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 136));
        }
    }
}
