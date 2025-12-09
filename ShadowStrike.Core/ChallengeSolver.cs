using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ShadowStrike.Core
{
    public class ChallengeSolver
    {
        private const int TorPort = 9150; // Default to Tor Browser port

        public class BypassResult
        {
            public CookieContainer Cookies { get; set; }
            public string UserAgent { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        public async Task<BypassResult> SolveChallengeAsync(string url, bool useTor = true)
        {
            return await Task.Run(() =>
            {
                IWebDriver driver = null;
                try
                {
                    var options = new ChromeOptions();
                    
                    // Headless mode (hidden)
                    options.AddArgument("--headless"); 
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument("--disable-dev-shm-usage");
                    
                    // Randomize initial User-Agent
                    options.AddArgument($"--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                    // Tor Proxy Configuration
                    if (useTor)
                    {
                        options.AddArgument($"--proxy-server=socks5://127.0.0.1:{TorPort}");
                    }

                    // Hide automation flags (important for evasion)
                    options.AddExcludedArgument("enable-automation");
                    options.AddAdditionalOption("useAutomationExtension", false);

                    var service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    driver = new ChromeDriver(service, options);

                    // Navigate to target
                    driver.Navigate().GoToUrl(url);

                    // Wait for challenge to complete (wait for title to not contain "Just a moment" or similar)
                    // Or just wait a fixed time for the redirect
                    System.Threading.Thread.Sleep(10000); // 10 seconds wait for JS challenge

                    // Extract Cookies
                    var cookieContainer = new CookieContainer();
                    var seleniumCookies = driver.Manage().Cookies.AllCookies;
                    
                    foreach (var cookie in seleniumCookies)
                    {
                        try 
                        {
                            cookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                        }
                        catch { /* Ignore invalid cookies */ }
                    }

                    // Extract User-Agent (it might have changed or we want the one used)
                    var userAgent = (string)((IJavaScriptExecutor)driver).ExecuteScript("return navigator.userAgent;");

                    return new BypassResult
                    {
                        Success = true,
                        Cookies = cookieContainer,
                        UserAgent = userAgent
                    };
                }
                catch (Exception ex)
                {
                    return new BypassResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
                finally
                {
                    try { driver?.Quit(); } catch { }
                }
            });
        }
    }
}
