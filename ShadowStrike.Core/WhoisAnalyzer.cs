using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class WhoisAnalyzer
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<WhoisIntelligence> LookupDomain(string domain)
        {
            var intel = new WhoisIntelligence { Domain = domain };

            try
            {
                // Clean domain (remove http://, www., etc.)
                domain = CleanDomain(domain);

                // Perform WHOIS lookup using raw TCP
                var whoisServer = "whois.verisign-grs.com"; // Default WHOIS server
                var rawData = await QueryWhoisServer(whoisServer, domain);

                if (!string.IsNullOrEmpty(rawData))
                {
                    intel.RawData = rawData;
                    
                    // Parse registrar
                    var registrarMatch = Regex.Match(rawData, @"Registrar:\s*(.+)", RegexOptions.IgnoreCase);
                    if (registrarMatch.Success)
                        intel.Registrar = registrarMatch.Groups[1].Value.Trim();

                    // Parse registrant
                    var registrantMatch = Regex.Match(rawData, @"Registrant.*?Organization:\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (registrantMatch.Success)
                        intel.Registrant = registrantMatch.Groups[1].Value.Trim();
                    else
                    {
                        registrantMatch = Regex.Match(rawData, @"Registrant:\s*(.+)", RegexOptions.IgnoreCase);
                        if (registrantMatch.Success)
                            intel.Registrant = registrantMatch.Groups[1].Value.Trim();
                    }

                    // Parse creation date
                    var createdMatch = Regex.Match(rawData, @"Creation Date:\s*(.+)", RegexOptions.IgnoreCase);
                    if (createdMatch.Success)
                        intel.CreationDate = createdMatch.Groups[1].Value.Trim();

                    // Parse expiration date
                    var expiryMatch = Regex.Match(rawData, @"Registry Expiry Date:\s*(.+)", RegexOptions.IgnoreCase);
                    if (!expiryMatch.Success)
                        expiryMatch = Regex.Match(rawData, @"Expiration Date:\s*(.+)", RegexOptions.IgnoreCase);
                    if (expiryMatch.Success)
                        intel.ExpirationDate = expiryMatch.Groups[1].Value.Trim();

                    // Parse nameservers
                    var nsMatches = Regex.Matches(rawData, @"Name Server:\s*(.+)", RegexOptions.IgnoreCase);
                    intel.Nameservers = nsMatches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).ToList();

                    // Parse status
                    var statusMatches = Regex.Matches(rawData, @"Domain Status:\s*(.+)", RegexOptions.IgnoreCase);
                    intel.Status = statusMatches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).ToList();

                    intel.Success = true;
                }
                else
                {
                    intel.Error = "No WHOIS data available";
                }
            }
            catch (Exception ex)
            {
                intel.Error = ex.Message;
                intel.Success = false;
            }

            return intel;
        }

        public async Task<IpWhoisIntelligence> LookupIp(string ip)
        {
            var intel = new IpWhoisIntelligence { IpAddress = ip };

            try
            {
                // Use ip-api.com for IP geolocation and WHOIS data
                var response = await _httpClient.GetStringAsync($"http://ip-api.com/json/{ip}?fields=status,message,country,countryCode,region,regionName,city,zip,lat,lon,timezone,isp,org,as,asname");
                
                if (response.Contains("\"status\":\"success\""))
                {
                    // Parse JSON manually (simple parsing)
                    intel.Country = ExtractJsonValue(response, "country");
                    intel.City = ExtractJsonValue(response, "city");
                    intel.ISP = ExtractJsonValue(response, "isp");
                    intel.Organization = ExtractJsonValue(response, "org");
                    intel.ASN = ExtractJsonValue(response, "as");
                    intel.Latitude = ExtractJsonValue(response, "lat");
                    intel.Longitude = ExtractJsonValue(response, "lon");
                    intel.Timezone = ExtractJsonValue(response, "timezone");
                    
                    intel.Success = true;
                }
                else
                {
                    intel.Error = ExtractJsonValue(response, "message");
                }
            }
            catch (Exception ex)
            {
                intel.Error = ex.Message;
                intel.Success = false;
            }

            return intel;
        }

        private async Task<string> QueryWhoisServer(string server, string query)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(server, 43); // WHOIS port

                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                using var reader = new StreamReader(stream, Encoding.ASCII);

                await writer.WriteLineAsync(query);
                return await reader.ReadToEndAsync();
            }
            catch
            {
                return "";
            }
        }

        private string CleanDomain(string domain)
        {
            domain = domain.Replace("http://", "").Replace("https://", "").Replace("www.", "");
            var slashIndex = domain.IndexOf('/');
            if (slashIndex > 0)
                domain = domain.Substring(0, slashIndex);
            return domain;
        }

        private string ExtractJsonValue(string json, string key)
        {
            var pattern = $"\"{key}\":\"([^\"]+)\"";
            var match = Regex.Match(json, pattern);
            if (match.Success)
                return match.Groups[1].Value;
            
            // Try numeric value
            pattern = $"\"{key}\":([^,}}]+)";
            match = Regex.Match(json, pattern);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            
            return "N/A";
        }
    }

    public class WhoisIntelligence
    {
        public string Domain { get; set; }
        public string Registrar { get; set; } = "Unknown";
        public string Registrant { get; set; } = "Unknown";
        public string CreationDate { get; set; } = "Unknown";
        public string ExpirationDate { get; set; } = "Unknown";
        public System.Collections.Generic.List<string> Nameservers { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> Status { get; set; } = new System.Collections.Generic.List<string>();
        public string RawData { get; set; } = "";
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }

    public class IpWhoisIntelligence
    {
        public string IpAddress { get; set; }
        public string Country { get; set; } = "Unknown";
        public string City { get; set; } = "Unknown";
        public string ISP { get; set; } = "Unknown";
        public string Organization { get; set; } = "Unknown";
        public string ASN { get; set; } = "Unknown";
        public string Latitude { get; set; } = "Unknown";
        public string Longitude { get; set; } = "Unknown";
        public string Timezone { get; set; } = "Unknown";
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
