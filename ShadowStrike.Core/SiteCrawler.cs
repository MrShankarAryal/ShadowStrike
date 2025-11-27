using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ShadowStrike.Core
{
    public class SiteCrawler
    {
        private IWebDriver _driver;
        private HashSet<string> _visitedUrls;
        private HashSet<string> _discoveredUrls;
        private List<FormInfo> _discoveredForms;
        private string _baseDomain;
        private int _maxDepth;

        public class FormInfo
        {
            public string Url { get; set; } = "";
            public string Action { get; set; } = "";
            public string Method { get; set; } = "GET";
            public List<InputInfo> Inputs { get; set; } = new List<InputInfo>();
            public bool HasFileUpload { get; set; }
        }

        public class InputInfo
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string Id { get; set; } = "";
        }

        public class CrawlResult
        {
            public List<string> DiscoveredPages { get; set; } = new List<string>();
            public List<FormInfo> DiscoveredForms { get; set; } = new List<FormInfo>();
            public List<string> DiscoveredParameters { get; set; } = new List<string>();
            public int TotalPages { get; set; }
            public int TotalForms { get; set; }
            public int UploadForms { get; set; }
        }

        public SiteCrawler(IWebDriver driver, int maxDepth = 3)
        {
            _driver = driver;
            _visitedUrls = new HashSet<string>();
            _discoveredUrls = new HashSet<string>();
            _discoveredForms = new List<FormInfo>();
            _maxDepth = maxDepth;
        }

        public async Task<CrawlResult> CrawlSiteAsync(string startUrl)
        {
            try
            {
                var uri = new Uri(startUrl);
                _baseDomain = uri.Host;

                // Start crawling from the homepage
                await CrawlPageAsync(startUrl, 0);

                // Compile results
                var result = new CrawlResult
                {
                    DiscoveredPages = _visitedUrls.ToList(),
                    DiscoveredForms = _discoveredForms,
                    TotalPages = _visitedUrls.Count,
                    TotalForms = _discoveredForms.Count,
                    UploadForms = _discoveredForms.Count(f => f.HasFileUpload)
                };

                // Extract parameters from URLs
                result.DiscoveredParameters = ExtractParameters(_visitedUrls.ToList());

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crawl error: {ex.Message}");
                return new CrawlResult();
            }
        }

        private async Task CrawlPageAsync(string url, int depth)
        {
            // Stop if max depth reached
            if (depth > _maxDepth)
                return;

            // Skip if already visited
            if (_visitedUrls.Contains(url))
                return;

            // Skip if different domain
            try
            {
                var uri = new Uri(url);
                if (uri.Host != _baseDomain)
                    return;
            }
            catch
            {
                return;
            }

            try
            {
                // Visit the page
                _driver.Navigate().GoToUrl(url);
                await Task.Delay(2000); // Wait for page load

                _visitedUrls.Add(url);

                // Discover forms on this page
                DiscoverFormsOnPage(url);

                // Discover links on this page
                var links = DiscoverLinksOnPage();

                // Crawl discovered links
                foreach (var link in links)
                {
                    if (!_visitedUrls.Contains(link) && !_discoveredUrls.Contains(link))
                    {
                        _discoveredUrls.Add(link);
                        await CrawlPageAsync(link, depth + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error crawling {url}: {ex.Message}");
            }
        }

        private void DiscoverFormsOnPage(string pageUrl)
        {
            try
            {
                var forms = _driver.FindElements(By.TagName("form"));

                foreach (var form in forms)
                {
                    var formInfo = new FormInfo
                    {
                        Url = pageUrl,
                        Action = form.GetAttribute("action") ?? "",
                        Method = form.GetAttribute("method") ?? "GET"
                    };

                    // Get all inputs in this form
                    var inputs = form.FindElements(By.TagName("input"));
                    var textareas = form.FindElements(By.TagName("textarea"));

                    foreach (var input in inputs)
                    {
                        var inputInfo = new InputInfo
                        {
                            Name = input.GetAttribute("name") ?? "",
                            Type = input.GetAttribute("type") ?? "text",
                            Id = input.GetAttribute("id") ?? ""
                        };

                        formInfo.Inputs.Add(inputInfo);

                        // Check if this is a file upload
                        if (inputInfo.Type == "file")
                        {
                            formInfo.HasFileUpload = true;
                        }
                    }

                    foreach (var textarea in textareas)
                    {
                        formInfo.Inputs.Add(new InputInfo
                        {
                            Name = textarea.GetAttribute("name") ?? "",
                            Type = "textarea",
                            Id = textarea.GetAttribute("id") ?? ""
                        });
                    }

                    _discoveredForms.Add(formInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering forms: {ex.Message}");
            }
        }

        private List<string> DiscoverLinksOnPage()
        {
            var links = new List<string>();

            try
            {
                var anchorElements = _driver.FindElements(By.TagName("a"));

                foreach (var anchor in anchorElements)
                {
                    var href = anchor.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        // Normalize URL
                        try
                        {
                            var uri = new Uri(href);
                            if (uri.Host == _baseDomain)
                            {
                                links.Add(uri.ToString());
                            }
                        }
                        catch
                        {
                            // Relative URL - construct full URL
                            try
                            {
                                var baseUri = new Uri(_driver.Url);
                                var fullUri = new Uri(baseUri, href);
                                if (fullUri.Host == _baseDomain)
                                {
                                    links.Add(fullUri.ToString());
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering links: {ex.Message}");
            }

            return links.Distinct().ToList();
        }

        private List<string> ExtractParameters(List<string> urls)
        {
            var parameters = new HashSet<string>();

            foreach (var url in urls)
            {
                try
                {
                    var uri = new Uri(url);
                    var query = uri.Query;

                    if (!string.IsNullOrEmpty(query))
                    {
                        var pairs = query.TrimStart('?').Split('&');
                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split('=');
                            if (parts.Length > 0)
                            {
                                parameters.Add(parts[0]);
                            }
                        }
                    }
                }
                catch { }
            }

            return parameters.ToList();
        }
    }
}
