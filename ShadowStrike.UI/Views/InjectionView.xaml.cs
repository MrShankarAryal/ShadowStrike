using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShadowStrike.Core;
using ShadowStrike.UI;

namespace ShadowStrike.UI.Views
{
    public partial class InjectionView : UserControl
    {
        private InjectionTester _tester;
        public ObservableCollection<InjectionResultViewModel> Results { get; set; } = new ObservableCollection<InjectionResultViewModel>();

        public InjectionView()
        {
            InitializeComponent();
            _tester = new InjectionTester();
            ResultsGrid.ItemsSource = Results;

            // Auto-fill from AppState
            var appState = AppState.Load();
            if (!string.IsNullOrEmpty(appState.TargetUrl))
            {
                TargetUrlInput.Text = appState.TargetUrl;
            }

            // Check if scan is required
            // Check if scan is required
            this.Loaded += (s, e) =>
            {
                var state = AppState.Load();
                if (!string.IsNullOrEmpty(state.TargetUrl))
                {
                    TargetUrlInput.Text = state.TargetUrl;
                }
            };
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select File to Upload",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathInput.Text = openFileDialog.FileName;
            }
        }

        private async void TestSqlInjection_Click(object sender, RoutedEventArgs e)
        {
            var state = AppState.Load();
            if (!state.IsScanCompleted)
            {
                CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartTest("SQL Injection");
            Results.Clear();

            try
            {
                var url = TargetUrlInput.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                    TargetUrlInput.Text = url;
                }
                var parameter = ParameterInput.Text;

                var results = await _tester.TestSqlInjectionAsync(url, parameter);

                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "SQL Injection",
                        Status = result.Vulnerable ? "VULNERABLE" : "SECURE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Severity,
                        Details = $"Payload: {result.Payload}\n\nResponse: {result.Response}"
                    });
                }

                UpdateStats(results.Count, results.Count(r => r.Vulnerable));
                
                if (results.Any(r => r.Vulnerable))
                {
                    MitigationText.Text = _tester.GetMitigationGuidance("SQLi");
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async void TestFileUpload_Click(object sender, RoutedEventArgs e)
        {
            var state = AppState.Load();
            if (!state.IsScanCompleted)
            {
                CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartTest("File Upload");
            Results.Clear();

            try
            {
                var url = TargetUrlInput.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                    TargetUrlInput.Text = url;
                }
                var filePath = FilePathInput.Text;

                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                {
                    MessageBox.Show("Please select a valid file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _tester.UseBrowserMode = BrowserModeCheckbox.IsChecked == true;
                var results = await _tester.TestFileUploadAsync(url, filePath);

                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "File Upload",
                        Status = result.Vulnerable ? "VULNERABLE" : "SECURE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Vulnerable ? "High" : "None",
                        Details = $"{result.Description}\n\n{result.Details}"
                    });
                }

                UpdateStats(results.Count, results.Count(r => r.Vulnerable));

                if (results.Any(r => r.Vulnerable))
                {
                    MitigationText.Text = _tester.GetMitigationGuidance("File Upload");
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async void AutoExploit_Click(object sender, RoutedEventArgs e)
        {
            var state = AppState.Load();
            if (!state.IsScanCompleted)
            {
                CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartTest("Auto Exploit");
            Results.Clear();

            try
            {
                var url = TargetUrlInput.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                    TargetUrlInput.Text = url;
                }
                _tester.UseBrowserMode = true;
                
                var results = await _tester.AutoDiscoverAndExploitAsync(url);

                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "Auto Exploit",
                        Status = result.Vulnerable ? "EXPLOITED" : "SECURE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Vulnerable ? "Critical" : "Info",
                        Details = $"{result.Description}\n\n{result.Details}"
                    });
                }

                UpdateStats(results.Count, results.Count(r => r.Vulnerable));
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async void AdvancedSQLi_Click(object sender, RoutedEventArgs e)
        {
            var state = AppState.Load();
            if (!state.IsScanCompleted)
            {
                CustomMessageBox.Show("Please scan a target in the Dashboard first to identify vulnerabilities and open ports.", "Scan Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StartTest("Defacement");
            Results.Clear();

            try
            {
                var url = TargetUrlInput.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                    TargetUrlInput.Text = url;
                }
                var browserTester = new ShadowStrike.Core.BrowserInjectionTester();
                await browserTester.InitializeBrowserAsync();

                var exploiter = new ShadowStrike.Core.AdvancedSQLiExploiter(browserTester.GetDriver());
                var result = await exploiter.ExecuteDefacementAttack(url);

                Results.Add(new InjectionResultViewModel
                {
                    TestName = "Defacement Attack",
                    Type = "Advanced SQLi",
                    Status = result.Success ? "SUCCESS" : "FAILED",
                    StatusColor = result.Success ? "#DA3633" : "#238636",
                    Severity = result.Success ? "Critical" : "None",
                    Details = result.Details
                });

                UpdateStats(1, result.Success ? 1 : 0);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private void StartTest(string testName)
        {
            StatusText.Text = "TESTING...";
            StatusText.Foreground = Brushes.Orange;
            TestsRunText.Text = "-";
            VulnerabilitiesText.Text = "-";
        }

        private void UpdateStats(int tests, int vulns)
        {
            TestsRunText.Text = tests.ToString();
            VulnerabilitiesText.Text = vulns.ToString();

            if (vulns > 0)
            {
                StatusText.Text = "VULNERABLE";
                StatusText.Foreground = Brushes.Red;
            }
            else
            {
                StatusText.Text = "SECURE";
                StatusText.Foreground = Brushes.Green;
            }
        }

        private void HandleError(Exception ex)
        {
            StatusText.Text = "ERROR";
            StatusText.Foreground = Brushes.Red;
            Results.Add(new InjectionResultViewModel
            {
                TestName = "Error",
                Type = "Error",
                Status = "ERROR",
                StatusColor = "#DA3633",
                Severity = "Error",
                Details = ex.Message
            });
            Logger.Log($"Injection Error: {ex.Message}");
        }
    }

    public class InjectionResultViewModel
    {
        public string TestName { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string Severity { get; set; }
        public string Details { get; set; }
    }
}
