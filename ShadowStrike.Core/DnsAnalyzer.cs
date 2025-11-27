using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;

namespace ShadowStrike.Core
{
    public class DnsAnalyzer
    {
        private readonly LookupClient _dnsClient;

        public DnsAnalyzer()
        {
            _dnsClient = new LookupClient();
        }

        public async Task<DnsIntelligence> AnalyzeDomain(string domain)
        {
            var intel = new DnsIntelligence { Domain = domain };

            try
            {
                // A Records (IPv4)
                var aRecords = await _dnsClient.QueryAsync(domain, QueryType.A);
                intel.ARecords = aRecords.Answers.ARecords().Select(r => r.Address.ToString()).ToList();

                // AAAA Records (IPv6)
                var aaaaRecords = await _dnsClient.QueryAsync(domain, QueryType.AAAA);
                intel.AAAARecords = aaaaRecords.Answers.AaaaRecords().Select(r => r.Address.ToString()).ToList();

                // MX Records (Mail)
                var mxRecords = await _dnsClient.QueryAsync(domain, QueryType.MX);
                intel.MXRecords = mxRecords.Answers.MxRecords()
                    .Select(r => $"{r.Exchange} (Priority: {r.Preference})")
                    .ToList();

                // TXT Records
                var txtRecords = await _dnsClient.QueryAsync(domain, QueryType.TXT);
                intel.TXTRecords = txtRecords.Answers.TxtRecords()
                    .SelectMany(r => r.Text)
                    .ToList();

                // NS Records (Nameservers)
                var nsRecords = await _dnsClient.QueryAsync(domain, QueryType.NS);
                intel.NSRecords = nsRecords.Answers.NsRecords()
                    .Select(r => r.NSDName.Value)
                    .ToList();

                // SOA Record
                var soaRecords = await _dnsClient.QueryAsync(domain, QueryType.SOA);
                var soa = soaRecords.Answers.SoaRecords().FirstOrDefault();
                if (soa != null)
                {
                    intel.SOARecord = $"{soa.MName} (Serial: {soa.Serial})";
                }

                // CNAME Records
                var cnameRecords = await _dnsClient.QueryAsync(domain, QueryType.CNAME);
                intel.CNAMERecords = cnameRecords.Answers.CnameRecords()
                    .Select(r => r.CanonicalName.Value)
                    .ToList();

                intel.Success = true;
            }
            catch (Exception ex)
            {
                intel.Error = ex.Message;
                intel.Success = false;
            }

            return intel;
        }

        public async Task<List<string>> DiscoverSubdomains(string domain, int maxSubdomains = 50)
        {
            var discovered = new List<string>();
            
            // Top subdomains to check
            var commonSubdomains = new[]
            {
                "www", "mail", "ftp", "localhost", "webmail", "smtp", "pop", "ns1", "webdisk",
                "ns2", "cpanel", "whm", "autodiscover", "autoconfig", "m", "imap", "test",
                "ns", "blog", "pop3", "dev", "www2", "admin", "forum", "news", "vpn",
                "ns3", "mail2", "new", "mysql", "old", "lists", "support", "mobile", "mx",
                "static", "docs", "beta", "shop", "sql", "secure", "demo", "cp", "calendar",
                "wiki", "web", "media", "email", "images", "img", "www1", "intranet", "portal",
                "video", "sip", "dns2", "api", "cdn", "stats", "dns1", "ns4", "www3", "dns",
                "search", "staging", "server", "mx1", "chat", "wap", "my", "svn", "mail1",
                "sites", "proxy", "ads", "host", "crm", "cms", "backup", "mx2", "lyncdiscover",
                "info", "apps", "download", "remote", "db", "forums", "store", "relay", "files",
                "newsletter", "app", "live", "owa", "en", "start", "sms", "office", "exchange"
            };

            var tasks = commonSubdomains.Take(maxSubdomains).Select(async sub =>
            {
                try
                {
                    var fullDomain = $"{sub}.{domain}";
                    var result = await _dnsClient.QueryAsync(fullDomain, QueryType.A);
                    
                    if (result.Answers.Count > 0)
                    {
                        var ips = result.Answers.ARecords().Select(r => r.Address.ToString()).ToList();
                        lock (discovered)
                        {
                            discovered.Add($"{fullDomain} → {string.Join(", ", ips)}");
                        }
                    }
                }
                catch { }
            });

            await Task.WhenAll(tasks);
            return discovered.OrderBy(s => s).ToList();
        }

        public async Task<bool> AttemptZoneTransfer(string domain)
        {
            try
            {
                // Get nameservers
                var nsRecords = await _dnsClient.QueryAsync(domain, QueryType.NS);
                var nameservers = nsRecords.Answers.NsRecords().Select(r => r.NSDName.Value).ToList();

                foreach (var ns in nameservers)
                {
                    try
                    {
                        // Attempt AXFR (zone transfer)
                        var nsIp = await Dns.GetHostAddressesAsync(ns);
                        if (nsIp.Length > 0)
                        {
                            var nsClient = new LookupClient(nsIp[0]);
                            var axfr = await nsClient.QueryAsync(domain, QueryType.AXFR);
                            
                            if (axfr.Answers.Count > 0)
                            {
                                return true; // Zone transfer successful (misconfiguration!)
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }
    }

    public class DnsIntelligence
    {
        public string Domain { get; set; }
        public List<string> ARecords { get; set; } = new List<string>();
        public List<string> AAAARecords { get; set; } = new List<string>();
        public List<string> MXRecords { get; set; } = new List<string>();
        public List<string> TXTRecords { get; set; } = new List<string>();
        public List<string> NSRecords { get; set; } = new List<string>();
        public string SOARecord { get; set; } = "";
        public List<string> CNAMERecords { get; set; } = new List<string>();
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
