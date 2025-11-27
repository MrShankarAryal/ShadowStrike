using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;

namespace ShadowStrike.Core
{
    public class EmailSecurityAnalyzer
    {
        private readonly LookupClient _dnsClient;

        public EmailSecurityAnalyzer()
        {
            _dnsClient = new LookupClient();
        }

        public async Task<EmailSecurityIntelligence> AnalyzeDomain(string domain)
        {
            var intel = new EmailSecurityIntelligence { Domain = domain };

            try
            {
                // Clean domain
                domain = CleanDomain(domain);

                // Check SPF
                intel.SpfRecord = await CheckSpf(domain);
                intel.HasSpf = !string.IsNullOrEmpty(intel.SpfRecord) && intel.SpfRecord != "Not found";

                // Check DMARC
                intel.DmarcRecord = await CheckDmarc(domain);
                intel.HasDmarc = !string.IsNullOrEmpty(intel.DmarcRecord) && intel.DmarcRecord != "Not found";

                // Check DKIM (common selectors)
                intel.DkimSelectors = await CheckDkim(domain);
                intel.HasDkim = intel.DkimSelectors.Count > 0;

                // Calculate security score
                intel.SecurityScore = CalculateSecurityScore(intel);

                intel.Success = true;
            }
            catch (Exception ex)
            {
                intel.Error = ex.Message;
                intel.Success = false;
            }

            return intel;
        }

        private async Task<string> CheckSpf(string domain)
        {
            try
            {
                var txtRecords = await _dnsClient.QueryAsync(domain, QueryType.TXT);
                var spfRecord = txtRecords.Answers.TxtRecords()
                    .SelectMany(r => r.Text)
                    .FirstOrDefault(t => t.StartsWith("v=spf1"));

                return spfRecord ?? "Not found";
            }
            catch
            {
                return "Not found";
            }
        }

        private async Task<string> CheckDmarc(string domain)
        {
            try
            {
                var dmarcDomain = $"_dmarc.{domain}";
                var txtRecords = await _dnsClient.QueryAsync(dmarcDomain, QueryType.TXT);
                var dmarcRecord = txtRecords.Answers.TxtRecords()
                    .SelectMany(r => r.Text)
                    .FirstOrDefault(t => t.StartsWith("v=DMARC1"));

                return dmarcRecord ?? "Not found";
            }
            catch
            {
                return "Not found";
            }
        }

        private async Task<List<string>> CheckDkim(string domain)
        {
            var foundSelectors = new List<string>();
            
            // Common DKIM selectors
            var commonSelectors = new[]
            {
                "default", "google", "k1", "k2", "k3", "mail", "dkim",
                "selector1", "selector2", "s1", "s2", "mx", "email"
            };

            var tasks = commonSelectors.Select(async selector =>
            {
                try
                {
                    var dkimDomain = $"{selector}._domainkey.{domain}";
                    var txtRecords = await _dnsClient.QueryAsync(dkimDomain, QueryType.TXT);
                    var dkimRecord = txtRecords.Answers.TxtRecords()
                        .SelectMany(r => r.Text)
                        .FirstOrDefault(t => t.Contains("v=DKIM1") || t.Contains("k=rsa"));

                    if (!string.IsNullOrEmpty(dkimRecord))
                    {
                        lock (foundSelectors)
                        {
                            foundSelectors.Add($"{selector}: {dkimRecord.Substring(0, Math.Min(50, dkimRecord.Length))}...");
                        }
                    }
                }
                catch { }
            });

            await Task.WhenAll(tasks);
            return foundSelectors;
        }

        private int CalculateSecurityScore(EmailSecurityIntelligence intel)
        {
            int score = 0;

            if (intel.HasSpf) score += 33;
            if (intel.HasDmarc) score += 34;
            if (intel.HasDkim) score += 33;

            return score;
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

    public class EmailSecurityIntelligence
    {
        public string Domain { get; set; }
        public string SpfRecord { get; set; } = "Not found";
        public string DmarcRecord { get; set; } = "Not found";
        public List<string> DkimSelectors { get; set; } = new List<string>();
        public bool HasSpf { get; set; }
        public bool HasDmarc { get; set; }
        public bool HasDkim { get; set; }
        public int SecurityScore { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
