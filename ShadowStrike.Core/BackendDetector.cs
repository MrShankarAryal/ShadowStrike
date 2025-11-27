using System;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace ShadowStrike.Core
{
    public class BackendDetector
    {
        private IWebDriver _driver;

        public class BackendCapabilities
        {
            public bool HasBackend { get; set; }
            public bool HasDatabase { get; set; }
            public bool HasUserAccounts { get; set; }
            public bool HasUploadFeatures { get; set; }
            public bool HasDynamicContent { get; set; }
            public string Details { get; set; } = "";
        }

        public BackendDetector(IWebDriver driver)
        {
            _driver = driver;
        }

        public async Task<BackendCapabilities> DetectCapabilitiesAsync(string url)
        {
            var capabilities = new BackendCapabilities();
            var details = "";

            try
            {
                _driver.Navigate().GoToUrl(url);
                
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

                // Check for session cookies (indicates backend)
                var cookies = _driver.Manage().Cookies.AllCookies;
                var hasSessionCookie = cookies.Any(c => 
                    c.Name.ToLower().Contains("session") || 
                    c.Name.ToLower().Contains("phpsessid") ||
                    c.Name.ToLower().Contains("jsessionid") ||
                    c.Name.ToLower().Contains("asp.net"));

                if (hasSessionCookie)
                {
                    capabilities.HasBackend = true;
                    details += "✓ Session cookies detected\n";
                }

                // Check for login/account features
                var pageSource = _driver.PageSource.ToLower();
                var hasLoginFeatures = pageSource.Contains("login") || 
                                      pageSource.Contains("sign in") ||
                                      pageSource.Contains("register") ||
                                      pageSource.Contains("account") ||
                                      pageSource.Contains("profile");

                if (hasLoginFeatures)
                {
                    capabilities.HasUserAccounts = true;
                    details += "✓ User account features found\n";
                }

                // Check for upload features
                var hasFileInputs = _driver.FindElements(By.CssSelector("input[type='file']")).Count > 0;
                var hasUploadText = pageSource.Contains("upload") || 
                                   pageSource.Contains("avatar") ||
                                   pageSource.Contains("profile picture");

                if (hasFileInputs || hasUploadText)
                {
                    capabilities.HasUploadFeatures = true;
                    details += "✓ Upload features detected\n";
                }

                // Check for dynamic content indicators
                var hasDynamicIndicators = pageSource.Contains("<?php") ||
                                          pageSource.Contains("asp.net") ||
                                          pageSource.Contains("jsp") ||
                                          cookies.Count > 0;

                if (hasDynamicIndicators)
                {
                    capabilities.HasDynamicContent = true;
                    capabilities.HasDatabase = true;
                    details += "✓ Dynamic content detected\n";
                }

                // Check for common CMS/frameworks
                if (pageSource.Contains("wordpress") || pageSource.Contains("wp-content"))
                {
                    capabilities.HasBackend = true;
                    capabilities.HasDatabase = true;
                    details += "✓ WordPress detected\n";
                }

                if (pageSource.Contains("drupal"))
                {
                    capabilities.HasBackend = true;
                    capabilities.HasDatabase = true;
                    details += "✓ Drupal detected\n";
                }

                capabilities.Details = details;

                // Final determination
                if (!capabilities.HasBackend && !capabilities.HasDynamicContent)
                {
                    capabilities.Details = "❌ Static site detected - no backend/database\nPersistent exploitation not possible on static sites.";
                }

            }
            catch (Exception ex)
            {
                capabilities.Details = $"Error detecting capabilities: {ex.Message}";
            }

            return capabilities;
        }
    }
}
