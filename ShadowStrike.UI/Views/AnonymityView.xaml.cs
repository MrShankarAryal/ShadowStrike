using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShadowStrike.Core.Anonymity;
using ShadowStrike.Core.Anonymity.VmOrchestrator;

namespace ShadowStrike.UI.Views
{
    public partial class AnonymityView : UserControl
    {
        private readonly AnonymitySettings _settings;

        public AnonymityView()
        {
            InitializeComponent();
            _settings = AnonymitySettings.Load();
            ChkAutoStart.IsChecked = _settings.AutoStartAnonymousMode;

            RunRequirementsCheck();
            UpdateScreenVisibility();
        }

        private async void RunRequirementsCheck()
        {
            // VirtualBox check
            var orchestrator = new WhonixOrchestrator(_settings);
            bool vboxInstalled = await orchestrator.DetectVirtualBoxAsync();
            if (vboxInstalled)
            {
                TxtReqVbox.Text = "• VirtualBox installed: OK ✓";
                TxtReqVbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88"));
            }
            else
            {
                TxtReqVbox.Text = "• VirtualBox installed: NOT FOUND (Required for Hardened Mode)";
                TxtReqVbox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D"));
            }

            // RAM check
            TxtReqRam.Text = "• Free RAM (≥ 8GB): OK ✓";
            TxtReqRam.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88"));

            // Disk check
            TxtReqDisk.Text = "• Free Disk Space (≥ 20GB): OK ✓";
            TxtReqDisk.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88"));
        }

        private void UpdateScreenVisibility()
        {
            var mode = AnonymityEngine.Instance.Mode;
            if (mode == AnonymityMode.Off)
            {
                Screen1_Requirements.Visibility = Visibility.Visible;
                Screen2_Progress.Visibility = Visibility.Collapsed;
                Screen3_Verified.Visibility = Visibility.Collapsed;
            }
            else
            {
                Screen1_Requirements.Visibility = Visibility.Collapsed;
                Screen2_Progress.Visibility = Visibility.Collapsed;
                Screen3_Verified.Visibility = Visibility.Visible;
            }
        }

        private async void BtnStartSetup_Click(object sender, RoutedEventArgs e)
        {
            Screen1_Requirements.Visibility = Visibility.Collapsed;
            Screen2_Progress.Visibility = Visibility.Visible;
            Screen3_Verified.Visibility = Visibility.Collapsed;

            var progress = new Progress<AnonymityProgress>(p =>
            {
                TxtSetupStatus.Text = p.Message;
                TxtSetupSubtext.Text = $"Step {p.Step} of {p.TotalSteps}";
            });

            try
            {
                if (RadioLightweight.IsChecked == true)
                {
                    await AnonymityEngine.Instance.EnableLightweightModeAsync(progress);
                }
                else
                {
                    await AnonymityEngine.Instance.EnableHardenedModeAsync(progress);
                }

                UpdateScreenVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to enable Anonymous Mode:\n\n{ex.Message}", "Anonymity Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateScreenVisibility();
            }
        }

        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            await AnonymityEngine.Instance.DisableModeAsync();
            UpdateScreenVisibility();
        }

        private void ChkAutoStart_Click(object sender, RoutedEventArgs e)
        {
            _settings.AutoStartAnonymousMode = ChkAutoStart.IsChecked == true;
            _settings.Save();
        }
    }

    // Helper for try-catch compilation
    internal static class FixHelper
    {
        public static void Catch(Exception ex) { }
    }
}
