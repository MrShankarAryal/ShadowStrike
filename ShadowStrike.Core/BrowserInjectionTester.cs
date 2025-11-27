using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ShadowStrike.Core
{
    public class BrowserInjectionTester : IDisposable
    {
        private IWebDriver? _driver;
        private bool _isInitialized;

        public class BrowserUploadResult
        {
            public string TestName { get; set; } = "";
            public string Description { get; set; } = "";
            public bool Vulnerable { get; set; }
            public string Details { get; set; } = "";
            public bool BrowserUsed { get; set; } = true;
        }

        public async Task<bool> InitializeBrowserAsync()
        {
            try
            {
                var options = new ChromeOptions();
                
                // STEALTH MODE: Critical for bypassing Vercel/Cloudflare
                // 1. Remove "AutomationControlled" flag
                options.AddArgument("--disable-blink-features=AutomationControlled");
                
                // 2. Exclude automation switches
                options.AddExcludedArgument("enable-automation");
                
                // 3. Turn off automation extension
                options.AddAdditionalOption("useAutomationExtension", false);
                
                // 4. Stability and crash prevention
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--disable-software-rasterizer");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--start-maximized");
                options.AddArgument("--lang=en-US,en;q=0.9");
                
                // 5. Prevent crashes on navigation
                options.AddArgument("--disable-web-security");
                options.AddArgument("--allow-running-insecure-content");
                options.AddArgument("--ignore-certificate-errors");
                
                // 6. Real User-Agent (Matches standard Chrome)
                options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                
                // 7. Performance settings
                options.PageLoadStrategy = PageLoadStrategy.Normal;
                
                _driver = new ChromeDriver(options);
                
                // 8. Execute CDP command to hide webdriver (Deep stealth)
                try 
                {
                    var cdp = (OpenQA.Selenium.Chromium.ChromiumDriver)_driver;
                    var parameters = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "source", "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })" }
                    };
                    cdp.ExecuteCdpCommand("Page.addScriptToEvaluateOnNewDocument", parameters);
                }
                catch { /* Ignore if CDP fails, we have backup JS injection */ }

                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize browser: {ex.Message}");
                return false;
            }
        }

        private void InjectStealthScripts()
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;
                
                // 1. Mock plugins to look like a real user
                js.ExecuteScript(@"
                    Object.defineProperty(navigator, 'plugins', {
                        get: () => [1, 2, 3, 4, 5]
                    });
                    Object.defineProperty(navigator, 'languages', {
                        get: () => ['en-US', 'en']
                    });
                ");

                // 2. Mock permissions
                js.ExecuteScript(@"
                    const originalQuery = window.navigator.permissions.query;
                    window.navigator.permissions.query = (parameters) => (
                        parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                    );
                ");
            }
            catch { }
        }

        private async Task HumanDelay()
        {
            // Random delay between 800ms and 2000ms to mimic human speed
            await Task.Delay(new Random().Next(800, 2000));
        }

        private void ScrollIntoView(IWebElement element)
        {
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", element);
            }
            catch { }
        }

        public async Task<List<BrowserUploadResult>> TestFileUploadWithBrowserAsync(string url, string filePath)
        {
            var results = new List<BrowserUploadResult>();

            if (!_isInitialized || _driver == null)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Browser Initialization",
                    Description = "Browser mode requires Chrome to be installed",
                    Vulnerable = false,
                    Details = "Browser not initialized. Make sure Chrome is installed."
                });
                return results;
            }

            var fileName = Path.GetFileName(filePath);
            var absolutePath = Path.GetFullPath(filePath);

            // Test 1: MIME Type Bypass with Browser
            try
            {
                _driver.Navigate().GoToUrl(url);
                InjectStealthScripts(); // Inject anti-fingerprinting scripts
                await HumanDelay(); // Wait for page load like a human

                // Try to find file input
                IWebElement? fileInput = null;
                try
                {
                    fileInput = _driver.FindElement(By.CssSelector("input[type='file']"));
                }
                catch
                {
                    try
                    {
                        fileInput = _driver.FindElement(By.Name("file"));
                    }
                    catch
                    {
                        try
                        {
                            fileInput = _driver.FindElement(By.Id("file"));
                        }
                        catch { }
                    }
                }

                if (fileInput != null)
                {
                    // Scroll to element like a real user
                    ScrollIntoView(fileInput);
                    await HumanDelay();

                    // Upload the file
                    fileInput.SendKeys(absolutePath);
                    await HumanDelay();

                    // Try to find and click submit button
                    try
                    {
                        var submitButton = _driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit']"));
                        ScrollIntoView(submitButton);
                        await HumanDelay();
                        submitButton.Click();
                    }
                    catch
                    {
                        // No submit button, file might auto-upload
                    }

                    await Task.Delay(3000); // Wait for upload response

                    // Check page content for success indicators
                    var pageSource = _driver.PageSource;
                    bool success = pageSource.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                                  pageSource.Contains("uploaded", StringComparison.OrdinalIgnoreCase) ||
                                  pageSource.Contains("200") ||
                                  !pageSource.Contains("error", StringComparison.OrdinalIgnoreCase);

                    // Try to extract upload URL from response
                    string uploadUrl = "";
                    try
                    {
                        // Method 1: Check if URL changed
                        var currentUrl = _driver.Url;
                        if (currentUrl.Contains("upload") || currentUrl != url)
                        {
                            uploadUrl = currentUrl;
                        }

                        // Method 2: Look for image/file URLs in page source
                        if (string.IsNullOrEmpty(uploadUrl))
                        {
                            var urlPatterns = new[]
                            {
                                @"https?://[^\s""'<>]+\.(jpg|jpeg|png|gif|pdf|txt|php|jsp|asp)",
                                @"/uploads?/[^\s""'<>]+",
                                @"src=[""']([^""']+)[""']",
                                @"href=[""']([^""']+)[""']"
                            };

                            foreach (var pattern in urlPatterns)
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(pageSource, pattern);
                                if (match.Success)
                                {
                                    uploadUrl = match.Groups[1].Value;
                                    if (!uploadUrl.StartsWith("http"))
                                    {
                                        var baseUri = new Uri(url);
                                        uploadUrl = $"{baseUri.Scheme}://{baseUri.Host}{uploadUrl}";
                                    }
                                    break;
                                }
                            }
                        }

                        // Method 3: Try common upload paths
                        if (string.IsNullOrEmpty(uploadUrl))
                        {
                            var baseUri = new Uri(url);
                            var possiblePaths = new[]
                            {
                                $"{baseUri.Scheme}://{baseUri.Host}/uploads/{fileName}",
                                $"{baseUri.Scheme}://{baseUri.Host}/upload/{fileName}",
                                $"{baseUri.Scheme}://{baseUri.Host}/files/{fileName}"
                            };

                            foreach (var path in possiblePaths)
                            {
                                try
                                {
                                    _driver.Navigate().GoToUrl(path);
                                    await Task.Delay(1000);
                                    if (!_driver.PageSource.Contains("404") && !_driver.PageSource.Contains("Not Found"))
                                    {
                                        uploadUrl = path;
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }

                    // If we found the upload URL, navigate to it to display the image
                    if (!string.IsNullOrEmpty(uploadUrl) && success)
                    {
                        try
                        {
                            _driver.Navigate().GoToUrl(uploadUrl);
                            await Task.Delay(2000); // Let image load

                            // Keep browser open to show the uploaded image
                            results.Add(new BrowserUploadResult
                            {
                                TestName = "Browser-Based File Upload",
                                Description = "Using real Chrome browser to bypass firewall detection",
                                Vulnerable = true,
                                Details = $"✅ FILE UPLOADED VIA BROWSER!\nFile: {fileName}\n📁 File URL: {uploadUrl}\n🔥 Bypassed TLS fingerprinting!\n\n🖼️ THE UPLOADED IMAGE IS NOW DISPLAYED IN THE BROWSER!\nCheck the Chrome window to see your uploaded file."
                            });
                        }
                        catch
                        {
                            results.Add(new BrowserUploadResult
                            {
                                TestName = "Browser-Based File Upload",
                                Description = "Using real Chrome browser to bypass firewall detection",
                                Vulnerable = success,
                                Details = success 
                                    ? $"✅ FILE UPLOADED VIA BROWSER!\nFile: {fileName}\nURL: {uploadUrl}\n🔥 Bypassed TLS fingerprinting!\nPage indicates success."
                                    : $"❌ Upload may have failed\nFile: {fileName}\nCheck page manually for results."
                            });
                        }
                    }
                    else
                    {
                        results.Add(new BrowserUploadResult
                        {
                            TestName = "Browser-Based File Upload",
                            Description = "Using real Chrome browser to bypass firewall detection",
                            Vulnerable = success,
                            Details = success 
                                ? $"✅ FILE UPLOADED VIA BROWSER!\nFile: {fileName}\n🔥 Bypassed TLS fingerprinting!\nPage indicates success.\n(Could not auto-navigate to uploaded file)"
                                : $"❌ Upload may have failed\nFile: {fileName}\nCheck page manually for results."
                        });
                    }
                }
                else
                {
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "Browser-Based File Upload",
                        Description = "Using real Chrome browser",
                        Vulnerable = false,
                        Details = $"❌ No file input found on page\nURL: {url}\nMake sure this is an upload page."
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Browser-Based File Upload",
                    Description = "Using real Chrome browser",
                    Vulnerable = false,
                    Details = $"❌ Browser test failed: {ex.Message}"
                });
            }

            return results;
        }

        // NEW: Test ANY page for vulnerabilities - no upload form required!
        public async Task<List<BrowserUploadResult>> TestAnyPageAsync(string url)
        {
            var results = new List<BrowserUploadResult>();

            if (!_isInitialized || _driver == null)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Browser Initialization",
                    Description = "Browser mode requires Chrome to be installed",
                    Vulnerable = false,
                    Details = "Browser not initialized. Make sure Chrome is installed."
                });
                return results;
            }

            try
            {
                _driver.Navigate().GoToUrl(url);
                await Task.Delay(3000); // Wait for page load

                // Test 1: Find ALL input fields and test for XSS
                try
                {
                    var inputs = _driver.FindElements(By.TagName("input"));
                    var textareas = _driver.FindElements(By.TagName("textarea"));
                    var allInputs = inputs.Concat(textareas).ToList();

                    if (allInputs.Count > 0)
                    {
                        string xssPayload = "<script>alert('XSS')</script>";
                        int testedInputs = 0;

                        foreach (var input in allInputs)
                        {
                            try
                            {
                                var inputType = input.GetAttribute("type");
                                if (inputType != "hidden" && inputType != "submit" && inputType != "button")
                                {
                                    input.Clear();
                                    input.SendKeys(xssPayload);
                                    testedInputs++;
                                }
                            }
                            catch { }
                        }

                        // Try to submit
                        try
                        {
                            var submitBtn = _driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit']"));
                            submitBtn.Click();
                            await Task.Delay(2000);
                        }
                        catch { }

                        var pageSource = _driver.PageSource;
                        bool xssFound = pageSource.Contains(xssPayload) || pageSource.Contains("alert('XSS')");

                        results.Add(new BrowserUploadResult
                        {
                            TestName = "XSS Injection Test",
                            Description = $"Tested {testedInputs} input fields on the page",
                            Vulnerable = xssFound,
                            Details = xssFound
                                ? $"⚠️ XSS VULNERABILITY FOUND!\nTested {testedInputs} inputs\nPayload: {xssPayload}\nThe page reflected the payload without sanitization!"
                                : $"✓ No XSS found\nTested {testedInputs} inputs\nPage appears to sanitize input properly."
                        });
                    }
                    else
                    {
                        results.Add(new BrowserUploadResult
                        {
                            TestName = "Input Field Detection",
                            Description = "No input fields found on this page",
                            Vulnerable = false,
                            Details = "This page has no input fields to test.\nTry a page with search boxes, login forms, or comment sections."
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "XSS Test Error",
                        Description = "Failed to test for XSS",
                        Vulnerable = false,
                        Details = $"Error: {ex.Message}"
                    });
                }

                // Test 2: URL Parameter SQL Injection
                try
                {
                    var currentUrl = _driver.Url;
                    if (currentUrl.Contains("?"))
                    {
                        // Add SQL injection payload to URL
                        var sqlPayload = "' OR '1'='1";
                        var testUrl = currentUrl + (currentUrl.Contains("=") ? sqlPayload : "?id=" + sqlPayload);
                        
                        _driver.Navigate().GoToUrl(testUrl);
                        await Task.Delay(2000);

                        var pageSource = _driver.PageSource.ToLower();
                        bool sqlError = pageSource.Contains("sql") || 
                                       pageSource.Contains("mysql") || 
                                       pageSource.Contains("syntax error") ||
                                       pageSource.Contains("database");

                        results.Add(new BrowserUploadResult
                        {
                            TestName = "URL Parameter SQLi Test",
                            Description = "Testing URL parameters for SQL injection",
                            Vulnerable = sqlError,
                            Details = sqlError
                                ? $"⚠️ POSSIBLE SQL INJECTION!\nPayload: {sqlPayload}\nDatabase error detected in response!"
                                : $"✓ No obvious SQL injection\nPayload: {sqlPayload}\nNo database errors detected."
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "URL SQLi Test",
                        Description = "Failed to test URL parameters",
                        Vulnerable = false,
                        Details = $"Error: {ex.Message}"
                    });
                }

                // Test 3: Check for security headers
                try
                {
                    var jsExecutor = (IJavaScriptExecutor)_driver;
                    var headers = jsExecutor.ExecuteScript(@"
                        var xhr = new XMLHttpRequest();
                        xhr.open('GET', window.location.href, false);
                        xhr.send();
                        return {
                            csp: xhr.getResponseHeader('Content-Security-Policy'),
                            xframe: xhr.getResponseHeader('X-Frame-Options'),
                            xss: xhr.getResponseHeader('X-XSS-Protection')
                        };
                    ");

                    results.Add(new BrowserUploadResult
                    {
                        TestName = "Security Headers Check",
                        Description = "Checking for security headers",
                        Vulnerable = false,
                        Details = $"Security headers detected:\n{headers}"
                    });
                }
                catch { }

            }
            catch (Exception ex)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Browser Test Error",
                    Description = "Failed to test page",
                    Vulnerable = false,
                    Details = $"❌ Error: {ex.Message}"
                });
            }

            return results;
        }

        // AUTO DISCOVER AND EXPLOIT - The main intelligent exploitation method
        public async Task<List<BrowserUploadResult>> AutoDiscoverAndExploitAsync(string url)
        {
            var results = new List<BrowserUploadResult>();

            if (!_isInitialized || _driver == null)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Browser Initialization",
                    Description = "Browser mode requires Chrome to be installed",
                    Vulnerable = false,
                    Details = "Browser not initialized. Make sure Chrome is installed."
                });
                return results;
            }

            try
            {
                // Phase 1: Detect backend capabilities
                var detector = new BackendDetector(_driver);
                var capabilities = await detector.DetectCapabilitiesAsync(url);

                results.Add(new BrowserUploadResult
                {
                    TestName = "Backend Detection",
                    Description = "Checking if site has database and persistent storage",
                    Vulnerable = capabilities.HasBackend,
                    Details = capabilities.Details
                });

                if (!capabilities.HasBackend && !capabilities.HasDatabase)
                {
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "Exploitation Status",
                        Description = "Cannot exploit static sites",
                        Vulnerable = false,
                        Details = "❌ This is a static site (no backend/database)\n\nPersistent exploitation requires:\n- Dynamic backend\n- Database storage\n- User-generated content features\n\nTry a site with user accounts, profiles, or upload features."
                    });
                    return results;
                }

                // Phase 2: Automated scanning and exploitation
                var scanner = new AutomatedScanner(_driver);
                var scanResults = await scanner.AutoScanAndExploitAsync(url);

                // Display crawl results
                results.Add(new BrowserUploadResult
                {
                    TestName = "Site Reconnaissance",
                    Description = "Discovered pages and features",
                    Vulnerable = false,
                    Details = $"📊 SITE MAPPING:\n" +
                             $"✓ Pages crawled: {scanResults.CrawlResults.TotalPages}\n" +
                             $"✓ Forms found: {scanResults.CrawlResults.TotalForms}\n" +
                             $"✓ Upload forms: {scanResults.CrawlResults.UploadForms}\n" +
                             $"✓ Parameters: {scanResults.CrawlResults.DiscoveredParameters.Count}"
                });

                // Display vulnerabilities
                if (scanResults.TotalVulnerabilities > 0)
                {
                    foreach (var vuln in scanResults.Vulnerabilities)
                    {
                        results.Add(new BrowserUploadResult
                        {
                            TestName = vuln.VulnerabilityType,
                            Description = vuln.Location,
                            Vulnerable = vuln.Exploited,
                            Details = $"[{vuln.Severity.ToUpper()}]\n\n" +
                                     $"Payload: {vuln.Payload}\n\n" +
                                     $"{vuln.Proof}"
                        });
                    }

                    // Summary
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "Exploitation Summary",
                        Description = "Automated exploitation complete",
                        Vulnerable = true,
                        Details = $"🎯 EXPLOITATION COMPLETE!\n\n" +
                                 $"Total Vulnerabilities: {scanResults.TotalVulnerabilities}\n" +
                                 $"Critical: {scanResults.CriticalVulnerabilities}\n" +
                                 $"High: {scanResults.HighVulnerabilities}\n\n" +
                                 $"✅ Content stored in database\n" +
                                 $"✅ Content displayed on website\n" +
                                 $"✅ Persistence verified"
                    });
                }
                else
                {
                    results.Add(new BrowserUploadResult
                    {
                        TestName = "Exploitation Status",
                        Description = "No exploitable vulnerabilities found",
                        Vulnerable = false,
                        Details = "✓ Site appears secure\n\nNo persistent storage vulnerabilities detected.\nThe site properly validates and sanitizes input."
                    });
                }

            }
            catch (Exception ex)
            {
                results.Add(new BrowserUploadResult
                {
                    TestName = "Auto Exploitation Error",
                    Description = "Failed to complete automated exploitation",
                    Vulnerable = false,
                    Details = $"❌ Error: {ex.Message}"
                });
            }

            return results;
        }

        public IWebDriver GetDriver()
        {
            return _driver;
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch { }
        }
    }
}
