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
                // Try to fill all target inputs
                SqliTargetUrlInput.Text = appState.TargetUrl;
                XssTargetUrlInput.Text = appState.TargetUrl;
                UploadTargetUrlInput.Text = appState.TargetUrl;
            }
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
            var url = SqliTargetUrlInput.Text;
            var parameter = SqliParameterInput.Text;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(parameter))
            {
                CustomMessageBox.Show("Please enter a Target URL and Parameter for SQL Injection.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!url.StartsWith("http")) url = "https://" + url;
            SqliTargetUrlInput.Text = url;

            StatusText.Text = "SCANNING SQLi...";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D29922"));
            
            try
            {
                var results = await _tester.TestSqlInjectionAsync(url, parameter);
                
                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "SQL Injection",
                        Status = result.Vulnerable ? "VULNERABLE" : "SAFE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Severity,
                        Details = $"Payload: {result.Payload}\nResponse: {result.Response}"
                    });
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            StatusText.Text = "READY";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3FB950"));
        }

        private async void SqliDumpData_Click(object sender, RoutedEventArgs e)
        {
            var url = SqliTargetUrlInput.Text;
            var parameter = SqliParameterInput.Text;
            
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(parameter)) return;

            StatusText.Text = "DUMPING DATA...";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DA3633"));

            try 
            {
                var results = await _tester.ExploitSqlInjectionAsync(url, parameter, "DUMP");
                
                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = "Data Dump Attempt",
                        Type = "SQLi Exploit",
                        Status = result.Success ? "SUCCESS" : "FAILED",
                        StatusColor = result.Success ? "#DA3633" : "#8B949E",
                        Severity = "CRITICAL",
                        Details = result.Data
                    });
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Exploit Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            StatusText.Text = "READY";
        }

        private async void SqliAuthBypass_Click(object sender, RoutedEventArgs e)
        {
            var url = SqliTargetUrlInput.Text;
            if (string.IsNullOrWhiteSpace(url)) return;

            StatusText.Text = "BYPASSING AUTH...";
            var results = await _tester.ExploitSqlInjectionAsync(url, "username", "AUTH_BYPASS");
             
             foreach (var result in results)
            {
                Results.Add(new InjectionResultViewModel
                {
                    TestName = "Auth Bypass Attempt",
                    Type = "SQLi Exploit",
                    Status = result.Success ? "SUCCESS" : "FAILED",
                    StatusColor = result.Success ? "#DA3633" : "#8B949E",
                    Severity = "CRITICAL",
                    Details = result.Data
                });
            }
            UpdateStats();
            StatusText.Text = "READY";
        }

        private async void TestXss_Click(object sender, RoutedEventArgs e)
        {
            var url = XssTargetUrlInput.Text;
            var parameter = XssParameterInput.Text;
            var beefHook = BeefHookInput.Text;
            var attackerServer = AttackerServerInput.Text;

            if (string.IsNullOrWhiteSpace(url))
            {
                 CustomMessageBox.Show("Please enter a Target URL for XSS.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return;
            }
            if (!url.StartsWith("http")) url = "https://" + url;

            StatusText.Text = "SCANNING XSS...";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8957E5"));

            try
            {
                var exploiter = new XssExploiter();
                var reconResults = await exploiter.ScanForXssAsync(url, parameter);
                
                foreach (var result in reconResults)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "XSS Recon",
                        Status = result.Vulnerable ? "REFLECTED" : "SAFE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Severity,
                        Details = result.Details
                    });
                }

                if (reconResults.Any(r => r.Vulnerable))
                {
                    var payloads = await exploiter.DeployPayloadsAsync(url, parameter, beefHook, attackerServer);
                    foreach (var payload in payloads)
                    {
                         Results.Add(new InjectionResultViewModel
                        {
                            TestName = payload.TestName,
                            Type = "XSS Payload",
                            Status = "GENERATED",
                            StatusColor = "#D29922",
                            Severity = payload.Severity,
                            Details = $"Payload: {payload.Payload}\n\nDetails: {payload.Details}"
                        });
                    }
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            StatusText.Text = "READY";
        }

        private void XssBrowserTest_Click(object sender, RoutedEventArgs e)
        {
            var url = XssTargetUrlInput.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                try 
                { 
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private async void TestFileUpload_Click(object sender, RoutedEventArgs e)
        {
            var url = UploadTargetUrlInput.Text;
            var filePath = FilePathInput.Text;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filePath))
            {
                CustomMessageBox.Show("Please enter a Target URL and select a File.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusText.Text = "TESTING UPLOAD...";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#238636"));

            try
            {
                _tester.UseBrowserMode = BrowserModeCheckbox.IsChecked ?? false;
                
                var results = await _tester.TestFileUploadAsync(url, filePath);
                
                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = result.TestName,
                        Type = "File Upload",
                        Status = result.Vulnerable ? "VULNERABLE" : "SECURE",
                        StatusColor = result.Vulnerable ? "#DA3633" : "#238636",
                        Severity = result.Vulnerable ? "HIGH" : "NONE",
                        Details = result.Details
                    });
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            StatusText.Text = "READY";
        }

        private async void UploadShell_Click(object sender, RoutedEventArgs e)
        {
            var url = UploadTargetUrlInput.Text;
            if (string.IsNullOrWhiteSpace(url)) return;

            StatusText.Text = "UPLOADING SHELL...";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DA3633"));

            try
            {
                var results = await _tester.UploadWebShellAsync(url);
                
                foreach (var result in results)
                {
                    Results.Add(new InjectionResultViewModel
                    {
                        TestName = "Web Shell Upload",
                        Type = "Exploit",
                        Status = result.Success ? "SHELL ACTIVE" : "FAILED",
                        StatusColor = result.Success ? "#DA3633" : "#8B949E",
                        Severity = "CRITICAL",
                        Details = result.Data
                    });
                }
                UpdateStats();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            StatusText.Text = "READY";
        }

        private void UpdateStats()
        {
            TestsRunText.Text = Results.Count.ToString();
            VulnerabilitiesText.Text = Results.Count(r => r.Status == "VULNERABLE" || r.Status == "SUCCESS" || r.Status == "SHELL ACTIVE").ToString();
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
