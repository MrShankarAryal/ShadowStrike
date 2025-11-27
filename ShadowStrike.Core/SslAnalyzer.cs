using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class SslAnalyzer
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                // Accept all certificates for analysis
                return true;
            }
        });

        public async Task<SslIntelligence> AnalyzeCertificate(string url)
        {
            var intel = new SslIntelligence { Url = url };

            try
            {
                if (!url.StartsWith("https://"))
                    url = "https://" + url.Replace("http://", "");

                X509Certificate2 certificate = null;

                // Custom callback to capture certificate
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
                    {
                        if (cert != null)
                        {
                            certificate = new X509Certificate2(cert);
                        }
                        return true;
                    }
                };

                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(10);
                
                try
                {
                    await client.GetAsync(url);
                }
                catch { }

                if (certificate != null)
                {
                    intel.Issuer = certificate.Issuer;
                    intel.Subject = certificate.Subject;
                    intel.ValidFrom = certificate.NotBefore.ToString("yyyy-MM-dd");
                    intel.ValidTo = certificate.NotAfter.ToString("yyyy-MM-dd");
                    intel.SerialNumber = certificate.SerialNumber;
                    intel.Thumbprint = certificate.Thumbprint;
                    intel.SignatureAlgorithm = certificate.SignatureAlgorithm.FriendlyName;

                    // Check if expired
                    if (certificate.NotAfter < DateTime.Now)
                    {
                        intel.Vulnerabilities.Add("Certificate is EXPIRED");
                        intel.IsExpired = true;
                    }

                    // Check if self-signed
                    if (certificate.Issuer == certificate.Subject)
                    {
                        intel.Vulnerabilities.Add("Self-signed certificate detected");
                        intel.IsSelfSigned = true;
                    }

                    // Extract Subject Alternative Names (SANs)
                    foreach (var extension in certificate.Extensions)
                    {
                        if (extension.Oid.Value == "2.5.29.17") // SAN OID
                        {
                            var sanExtension = extension as X509SubjectAlternativeNameExtension;
                            if (sanExtension != null)
                            {
                                var sanData = extension.Format(false);
                                var sans = sanData.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => s.StartsWith("DNS Name="))
                                    .Select(s => s.Replace("DNS Name=", ""))
                                    .ToList();
                                
                                intel.SubjectAlternativeNames.AddRange(sans);
                            }
                            else
                            {
                                // Fallback parsing
                                var sanData = extension.Format(false);
                                var lines = sanData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    if (line.Contains("DNS Name="))
                                    {
                                        intel.SubjectAlternativeNames.Add(line.Replace("DNS Name=", "").Trim());
                                    }
                                }
                            }
                        }
                    }

                    // Check certificate chain
                    using var chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    var chainBuilt = chain.Build(certificate);
                    
                    if (!chainBuilt)
                    {
                        intel.Vulnerabilities.Add("Certificate chain validation failed");
                    }

                    intel.Success = true;
                }
                else
                {
                    intel.Error = "Could not retrieve SSL certificate";
                }
            }
            catch (Exception ex)
            {
                intel.Error = ex.Message;
                intel.Success = false;
            }

            return intel;
        }

        public async Task<List<string>> ExtractSanDomains(string url)
        {
            var intel = await AnalyzeCertificate(url);
            return intel.SubjectAlternativeNames;
        }
    }

    public class SslIntelligence
    {
        public string Url { get; set; }
        public string Issuer { get; set; } = "Unknown";
        public string Subject { get; set; } = "Unknown";
        public string ValidFrom { get; set; } = "Unknown";
        public string ValidTo { get; set; } = "Unknown";
        public string SerialNumber { get; set; } = "Unknown";
        public string Thumbprint { get; set; } = "Unknown";
        public string SignatureAlgorithm { get; set; } = "Unknown";
        public bool IsExpired { get; set; }
        public bool IsSelfSigned { get; set; }
        public List<string> SubjectAlternativeNames { get; set; } = new List<string>();
        public List<string> Vulnerabilities { get; set; } = new List<string>();
        public bool Success { get; set; }
        public string Error { get; set; } = "";
    }
}
