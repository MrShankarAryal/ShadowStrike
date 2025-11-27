using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class OsintEngine
    {
        private readonly DnsAnalyzer _dnsAnalyzer;
        private readonly WhoisAnalyzer _whoisAnalyzer;
        private readonly SslAnalyzer _sslAnalyzer;
        private readonly EmailSecurityAnalyzer _emailAnalyzer;

        public OsintEngine()
        {
            _dnsAnalyzer = new DnsAnalyzer();
            _whoisAnalyzer = new WhoisAnalyzer();
            _sslAnalyzer = new SslAnalyzer();
            _emailAnalyzer = new EmailSecurityAnalyzer();
        }

        public async Task<ComprehensiveOsintReport> PerformFullAnalysis(string target, Action<string> progressCallback = null)
        {
            var report = new ComprehensiveOsintReport { Target = target };

            try
            {
                // Clean target
                var cleanDomain = CleanDomain(target);
                var isHttps = target.StartsWith("https://");

                // Phase 1: DNS Intelligence
                progressCallback?.Invoke("Performing DNS enumeration...");
                report.DnsIntelligence = await _dnsAnalyzer.AnalyzeDomain(cleanDomain);

                // Phase 2: Subdomain Discovery
                progressCallback?.Invoke("Discovering subdomains...");
                report.Subdomains = await _dnsAnalyzer.DiscoverSubdomains(cleanDomain, 50);

                // Phase 3: Zone Transfer Check
                progressCallback?.Invoke("Checking for zone transfer vulnerability...");
                report.ZoneTransferVulnerable = await _dnsAnalyzer.AttemptZoneTransfer(cleanDomain);

                // Phase 4: WHOIS Lookup
                progressCallback?.Invoke("Performing WHOIS lookup...");
                report.WhoisIntelligence = await _whoisAnalyzer.LookupDomain(cleanDomain);

                // Phase 5: IP WHOIS & Geolocation
                if (report.DnsIntelligence.ARecords.Count > 0)
                {
                    progressCallback?.Invoke("Geolocating IP addresses...");
                    var firstIp = report.DnsIntelligence.ARecords.First();
                    report.IpWhoisIntelligence = await _whoisAnalyzer.LookupIp(firstIp);
                }

                // Phase 6: SSL/TLS Analysis (if HTTPS)
                if (isHttps || target.Contains("443"))
                {
                    progressCallback?.Invoke("Analyzing SSL/TLS certificate...");
                    report.SslIntelligence = await _sslAnalyzer.AnalyzeCertificate(target);
                }

                // Phase 7: Email Security
                progressCallback?.Invoke("Checking email security (SPF/DKIM/DMARC)...");
                report.EmailSecurityIntelligence = await _emailAnalyzer.AnalyzeDomain(cleanDomain);

                progressCallback?.Invoke("OSINT analysis complete!");
                report.Success = true;
            }
            catch (Exception ex)
            {
                report.Error = ex.Message;
                report.Success = false;
            }

            return report;
        }

        private string CleanDomain(string domain)
        {
            domain = domain.Replace("http://", "").Replace("https://", "").Replace("www.", "");
            var slashIndex = domain.IndexOf('/');
            if (slashIndex > 0)
                domain = domain.Substring(0, slashIndex);
            return domain;
        }
    }

    public class ComprehensiveOsintReport
    {
        public string Target { get; set; }
        public DnsIntelligence DnsIntelligence { get; set; }
        public List<string> Subdomains { get; set; } = new List<string>();
        public bool ZoneTransferVulnerable { get; set; }
        public WhoisIntelligence WhoisIntelligence { get; set; }
        public IpWhoisIntelligence IpWhoisIntelligence { get; set; }
        public SslIntelligence SslIntelligence { get; set; }
        public EmailSecurityIntelligence EmailSecurityIntelligence { get; set; }
        
        // Target Analyzer Data
        public string[] OpenPorts { get; set; } = Array.Empty<string>();
        public string Server { get; set; } = "";
        public string CMS { get; set; } = "";
        public string Technologies { get; set; } = "";
        public string WAF { get; set; } = "";

        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
