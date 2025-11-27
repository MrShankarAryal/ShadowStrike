using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class TargetAnalyzer
    {
        public class TargetInfo
        {
            public string Url { get; set; } = "";
            public string IPAddress { get; set; } = "";
            public List<string> AllIPs { get; set; } = new List<string>();
            public string[] OpenPorts { get; set; } = Array.Empty<string>();
            public string Hostname { get; set; } = "";
            
            // New Fields
            public string Server { get; set; } = "Unknown";
            public string CMS { get; set; } = "Unknown";
            public string Technologies { get; set; } = "Unknown";
            public string WAF { get; set; } = "None Detected";
            public string Hosting { get; set; } = "Unknown";
        }

        private static readonly HttpClient _client = new HttpClient();

        public async Task<TargetInfo> AnalyzeTarget(string url)
        {
            var info = new TargetInfo { Url = url };

            try
            {
                if (!url.StartsWith("http")) url = "http://" + url;
                var uri = new Uri(url);
                info.Hostname = uri.Host;

                // 1. DNS Resolution
                try
                {
                    var addresses = await Dns.GetHostAddressesAsync(uri.Host);
                    if (addresses.Length > 0)
                    {
                        info.IPAddress = addresses[0].ToString();
                        info.AllIPs = addresses.Select(a => a.ToString()).ToList();
                    }
                }
                catch { info.IPAddress = "Resolution Failed"; }

                // 2. Port Scanning (Fast)
                info.OpenPorts = await ScanCommonPorts(uri.Host);

                // 3. HTTP Fingerprinting (Server, CMS, Tech, WAF)
                await FingerprintWebStack(url, info);

                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Analysis failed: {ex.Message}");
                return info;
            }
        }

        private async Task<string[]> ScanCommonPorts(string host)
        {
            var commonPorts = new[] { 
                21, 22, 23, 25, 53, 80, 443, 3306, 3389, 5432, 8080, 8443 
            };
            
            var openPorts = new List<string>();
            var tasks = commonPorts.Select(async port =>
            {
                using var tcp = new TcpClient();
                try
                {
                    var connectTask = tcp.ConnectAsync(host, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(200)) == connectTask)
                    {
                        if (tcp.Connected)
                        {
                            string service = GetServiceName(port);
                            lock (openPorts) openPorts.Add($"{port} ({service})");
                        }
                    }
                }
                catch { }
            });

            await Task.WhenAll(tasks);
            return openPorts.OrderBy(p => int.Parse(p.Split(' ')[0])).ToArray();
        }

        private async Task FingerprintWebStack(string url, TargetInfo info)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                var response = await _client.SendAsync(request);
                var headers = response.Headers;
                var content = await response.Content.ReadAsStringAsync();
                var lowerContent = content.ToLower();

                // Detect Server
                if (headers.Server != null) info.Server = headers.Server.ToString();
                else if (headers.Contains("X-Powered-By")) info.Server = string.Join(", ", headers.GetValues("X-Powered-By"));

                // Detect CMS
                if (lowerContent.Contains("wp-content") || lowerContent.Contains("wp-includes")) info.CMS = "WordPress";
                else if (lowerContent.Contains("joomla")) info.CMS = "Joomla";
                else if (lowerContent.Contains("drupal")) info.CMS = "Drupal";
                else if (lowerContent.Contains("shopify")) info.CMS = "Shopify";
                else if (lowerContent.Contains("wix.com")) info.CMS = "Wix";

                // Detect WAF
                if (headers.Contains("CF-RAY") || (headers.Server != null && headers.Server.ToString().ToLower().Contains("cloudflare"))) info.WAF = "Cloudflare";
                else if (headers.Contains("X-Sucuri-ID")) info.WAF = "Sucuri";
                else if (headers.Contains("X-Amz-Cf-Id")) info.WAF = "AWS CloudFront";
                else if (lowerContent.Contains("captcha-delivery")) info.WAF = "Generic WAF/Captcha";

                // Detect Technologies
                var techs = new List<string>();
                if (lowerContent.Contains("react")) techs.Add("React");
                if (lowerContent.Contains("vue")) techs.Add("Vue.js");
                if (lowerContent.Contains("angular")) techs.Add("Angular");
                if (lowerContent.Contains("jquery")) techs.Add("jQuery");
                if (lowerContent.Contains("bootstrap")) techs.Add("Bootstrap");
                if (headers.Contains("X-AspNet-Version")) techs.Add("ASP.NET");
                if (headers.Contains("X-Powered-By") && headers.GetValues("X-Powered-By").Any(v => v.Contains("PHP"))) techs.Add("PHP");

                if (techs.Count > 0) info.Technologies = string.Join(", ", techs);
                else info.Technologies = "HTML/JS (Custom)";

                // Detect Hosting (Simple inference)
                if (info.WAF == "Cloudflare") info.Hosting = "Cloudflare CDN";
                else if (info.WAF == "AWS CloudFront") info.Hosting = "AWS";
                else info.Hosting = "Unknown / Dedicated";

            }
            catch 
            {
                info.Server = "Detection Failed (Firewall?)";
            }
        }

        private string GetServiceName(int port)
        {
            return port switch
            {
                21 => "FTP", 22 => "SSH", 23 => "Telnet", 25 => "SMTP",
                53 => "DNS", 80 => "HTTP", 443 => "HTTPS", 3306 => "MySQL",
                3389 => "RDP", 5432 => "PostgreSQL", 8080 => "HTTP-Alt",
                8443 => "HTTPS-Alt", _ => "Unknown"
            };
        }
    }
}
