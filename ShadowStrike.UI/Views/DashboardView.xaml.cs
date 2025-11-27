using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShadowStrike.Core;

namespace ShadowStrike.UI.Views
{
    public partial class DashboardView : UserControl
    {
        private AppState _appState;
        private static ComprehensiveOsintReport _lastReport;

        public static void LoadReport(ComprehensiveOsintReport report)
        {
            _lastReport = report;
            // Also update AppState to match
            var appState = AppState.Load();
            appState.TargetUrl = report.Target;
            if (report.DnsIntelligence != null && report.DnsIntelligence.ARecords.Count > 0)
            {
                appState.TargetIP = report.DnsIntelligence.ARecords.First();
                appState.TargetIPs = report.DnsIntelligence.ARecords;
            }
            appState.IsScanCompleted = true;
            appState.Save();
        }

        public DashboardView()
        {
            InitializeComponent();
            _appState = AppState.Load();
            
            if (!string.IsNullOrEmpty(_appState.TargetUrl))
            {
                TargetInput.Text = _appState.TargetUrl;
                
                if (_lastReport != null && _lastReport.Success)
                {
                    _ = RestoreOsintDisplay(_lastReport);
                }
                else
                {
                    LoadSavedResults();
                }
            }
        }

        private void LoadSavedResults()
        {
            if (!string.IsNullOrEmpty(_appState.TargetIP))
            {
                if (_appState.TargetIPs != null && _appState.TargetIPs.Count > 0)
                {
                    IpListText.Text = string.Join(", ", _appState.TargetIPs);
                }
                else
                {
                    IpListText.Text = _appState.TargetIP;
                }
                
                ResultsPanel.Visibility = Visibility.Visible;
            }
            
            if (!string.IsNullOrEmpty(_appState.OpenPorts))
            {
                PortsText.Text = _appState.OpenPorts;
            }

            if (_appState.TotalTestsRun > 0)
            {
                StatusText.Text = $"Last scan: {_appState.LastScanTime:g} - {_appState.TotalVulnerabilities} vulnerabilities found";
            }
        }

        private async void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            ResultsPanel.Visibility = Visibility.Collapsed;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            StatusText.Text = "Initializing comprehensive OSINT analysis...";
            
            var target = TargetInput.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                StatusText.Text = "Please enter a target URL";
                ProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            if (!target.StartsWith("http://") && !target.StartsWith("https://"))
            {
                target = "https://" + target;
                TargetInput.Text = target; // Update UI
            }

            try
            {
                _appState.TargetUrl = target;
                _appState.Save();

                var osintEngine = new OsintEngine();
                var report = await osintEngine.PerformFullAnalysis(target, (status) =>
                {
                    Dispatcher.Invoke(() => StatusText.Text = status);
                });

                if (!report.Success)
                {
                    StatusText.Text = $"Analysis failed: {report.Error}";
                    ProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                if (report.DnsIntelligence != null && report.DnsIntelligence.ARecords.Count > 0)
                {
                    _appState.TargetIP = report.DnsIntelligence.ARecords.First();
                    _appState.TargetIPs = report.DnsIntelligence.ARecords;

                    // Run Target Analyzer
                    Dispatcher.Invoke(() => StatusText.Text = "Analyzing technology stack & ports...");
                    var analyzer = new TargetAnalyzer();
                    var targetInfo = await analyzer.AnalyzeTarget(target);
                    
                    report.OpenPorts = targetInfo.OpenPorts;
                    report.Server = targetInfo.Server;
                    report.CMS = targetInfo.CMS;
                    report.Technologies = targetInfo.Technologies;
                    report.WAF = targetInfo.WAF;
                }

                // Run Vulnerability Hunter (if not zone transfer vulnerable)
                if (!report.ZoneTransferVulnerable)
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Hunting for web vulnerabilities...");
                    var browserTester = new BrowserInjectionTester();
                    await browserTester.InitializeBrowserAsync();
                    
                    var hunter = new VulnerabilityHunter(browserTester.GetDriver());
                    var weaknesses = await hunter.FindAllWeaknesses(target);
                    report.Vulnerabilities = weaknesses;
                }

                _appState.IsScanCompleted = true;
                _appState.Save();

                // Display all data
                await DisplayOsintResults(report, target);

                // Cache report
                _lastReport = report;

                ResultsPanel.Visibility = Visibility.Visible;
                StatusText.Text = $"✅ Comprehensive OSINT analysis complete! ({report.Subdomains.Count} subdomains discovered)";

                Logger.Log($"OSINT analysis completed: {target}");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                Logger.Log($"Analysis error: {ex.Message}");
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async Task DisplayOsintResults(ComprehensiveOsintReport report, string target)
        {
            // Domain Intelligence
            if (report.DnsIntelligence != null)
            {
                DomainText.Text = report.DnsIntelligence.Domain;
                IpListText.Text = report.DnsIntelligence.ARecords.Count > 0 
                    ? string.Join(", ", report.DnsIntelligence.ARecords) 
                    : "Not resolved";
            }

            // Hosting & CDN
            if (report.IpWhoisIntelligence != null && report.IpWhoisIntelligence.Success)
            {
                HostingText.Text = $"{report.IpWhoisIntelligence.ISP} ({report.IpWhoisIntelligence.Country})";
                CdnText.Text = report.IpWhoisIntelligence.Organization;
                _appState.Hosting = $"{report.IpWhoisIntelligence.ISP} ({report.IpWhoisIntelligence.Country})";
            }

            // Subdomains
            if (report.Subdomains != null && report.Subdomains.Count > 0)
            {
                SubdomainsText.Text = string.Join("\n", report.Subdomains.Take(10).Select(s => $"• {s}"));
                if (report.Subdomains.Count > 10)
                    SubdomainsText.Text += $"\n... and {report.Subdomains.Count - 10} more";
            }
            else
            {
                SubdomainsText.Text = "No subdomains discovered";
            }

            // Ports & Technology Stack
            if (report.OpenPorts.Length > 0)
            {
                PortsText.Text = string.Join("\n", report.OpenPorts.Select(p => $"• {p}"));
                PortsQuick.Text = report.OpenPorts.Length.ToString();
                _appState.OpenPorts = string.Join(", ", report.OpenPorts);
            }
            else
            {
                PortsText.Text = "No open ports detected (or firewall blocking)";
                PortsQuick.Text = "0";
            }

            ServerText.Text = report.Server;
            CmsText.Text = report.CMS;
            TechText.Text = report.Technologies;
            WafQuick.Text = report.WAF;
            
            _appState.Server = report.Server;
            _appState.CMS = report.CMS;
            _appState.Technologies = report.Technologies;
            _appState.WafDetected = report.WAF;

            // WHOIS
            if (report.WhoisIntelligence != null && report.WhoisIntelligence.Success)
            {
                RegistrantText.Text = report.WhoisIntelligence.Registrant;
                OrgText.Text = report.WhoisIntelligence.Registrar;
                
                if (report.WhoisIntelligence.Nameservers.Count > 0)
                {
                    NameserversText.Text = string.Join("\n", report.WhoisIntelligence.Nameservers.Select(ns => $"• {ns}"));
                }
            }

            // SSL SANs
            if (report.SslIntelligence != null && report.SslIntelligence.Success && report.SslIntelligence.SubjectAlternativeNames.Count > 0)
            {
                AssociatedDomainsText.Text = string.Join("\n", report.SslIntelligence.SubjectAlternativeNames.Take(5).Select(d => $"• {d}"));
            }

            // Email Security
            if (report.EmailSecurityIntelligence != null && report.EmailSecurityIntelligence.Success)
            {
                var securityIssues = new System.Collections.Generic.List<string>();
                
                if (!report.EmailSecurityIntelligence.HasSpf)
                    securityIssues.Add("❌ No SPF record");
                else
                    securityIssues.Add("✅ SPF configured");

                if (!report.EmailSecurityIntelligence.HasDmarc)
                    securityIssues.Add("❌ No DMARC policy");
                else
                    securityIssues.Add("✅ DMARC configured");

                if (!report.EmailSecurityIntelligence.HasDkim)
                    securityIssues.Add("⚠️ DKIM not detected");
                else
                    securityIssues.Add("✅ DKIM configured");

                SecurityHeadersText.Text = string.Join("\n", securityIssues);
            }

            // SSL Info
            if (report.SslIntelligence != null && report.SslIntelligence.Success)
            {
                var sslInfo = $"Issuer: {report.SslIntelligence.Issuer}\n";
                sslInfo += $"Valid: {report.SslIntelligence.ValidFrom} to {report.SslIntelligence.ValidTo}\n";
                
                if (report.SslIntelligence.Vulnerabilities.Count > 0)
                {
                    sslInfo += "\n⚠️ Issues:\n" + string.Join("\n", report.SslIntelligence.Vulnerabilities.Select(v => $"• {v}"));
                }
                
                WafText.Text = sslInfo;
            }

            // Zone Transfer Check
            if (report.ZoneTransferVulnerable)
            {
                SuggestionsText.Text = "🚨 CRITICAL: DNS Zone Transfer is ENABLED!\n\nThis is a severe misconfiguration.\n\nRecommendation: Disable AXFR immediately.";
            }
            else
            {
                if (report.Vulnerabilities.Count > 0)
                {
                    var weaknessDetails = string.Join("\n\n", report.Vulnerabilities.Select(w =>
                        $"[{w.Severity}] {w.Type}\n  {w.Description}\n  Location: {w.Location}\n  Exploitable: {(w.Exploitable ? "YES ⚠️" : "NO")}"
                    ));
                    
                    SuggestionsText.Text = weaknessDetails;
                    _appState.TotalVulnerabilities = report.Vulnerabilities.Count(w => w.Exploitable);
                }
                else
                {
                    SuggestionsText.Text = "✅ No obvious weaknesses found\n\nThe target appears well-secured.";
                }
            }

            // Quick Stats
            if (report.DnsIntelligence != null && report.DnsIntelligence.ARecords.Count > 0)
            {
                StatusQuick.Text = "ONLINE";
                StatusQuick.Foreground = Brushes.Green;
                _appState.CurrentStatus = "ONLINE";
                
                try
                {
                    var ping = new System.Net.NetworkInformation.Ping();
                    var reply = await ping.SendPingAsync(report.DnsIntelligence.ARecords.First(), 1000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        LatencyQuick.Text = $"{reply.RoundtripTime}ms";
                    }
                    else
                    {
                        LatencyQuick.Text = "N/A";
                    }
                }
                catch
                {
                    LatencyQuick.Text = "N/A";
                }
            }
            else
            {
                StatusQuick.Text = "OFFLINE";
                StatusQuick.Foreground = Brushes.Red;
                _appState.CurrentStatus = "OFFLINE";
                LatencyQuick.Text = "N/A";
            }

            _appState.TotalTestsRun++;
            _appState.LastScanTime = DateTime.Now;
            _appState.Save();

            // Save to history
            var historyManager = new HistoryManager();
            await historyManager.SaveReportAsync(report);
        }

        private async Task RestoreOsintDisplay(ComprehensiveOsintReport report)
        {
            try
            {
                ResultsPanel.Visibility = Visibility.Visible;

                // Use the same display logic
                await DisplayOsintResults(report, report.Target);

                StatusText.Text = $"Showing cached results for {report.Target}";
            }
            catch (Exception ex)
            {
                Logger.Log($"Error restoring OSINT display: {ex.Message}");
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            _appState.Clear();
            _lastReport = null;
            TargetInput.Text = "";
            DomainText.Text = "";
            IpListText.Text = "";
            PortsText.Text = "";
            SecurityHeadersText.Text = "";
            SuggestionsText.Text = "";
            StatusText.Text = "State cleared";
            StatusQuick.Text = "READY";
            PortsQuick.Text = "0";
            LatencyQuick.Text = "0ms";
            WafQuick.Text = "Unknown";
            ResultsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
