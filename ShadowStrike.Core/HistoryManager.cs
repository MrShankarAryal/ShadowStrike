using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShadowStrike.Core
{
    public class HistoryManager
    {
        private readonly string _logsDirectory;

        public HistoryManager()
        {
            // Use AppData for persistent storage, or a local "Logs" folder
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logsDirectory = Path.Combine(appData, "ShadowStrike", "Logs");

            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }

        public async Task SaveReportAsync(ComprehensiveOsintReport report)
        {
            try
            {
                if (report == null) return;

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var safeTarget = string.Join("_", report.Target.Split(Path.GetInvalidFileNameChars()));
                var filename = $"scan_{timestamp}_{safeTarget}.json";
                var filePath = Path.Combine(_logsDirectory, filename);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(report, options);

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving report: {ex.Message}");
            }
        }

        public async Task<ComprehensiveOsintReport?> LoadReportAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<ComprehensiveOsintReport>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading report: {ex.Message}");
                return null;
            }
        }

        public List<LogEntry> GetHistory()
        {
            var history = new List<LogEntry>();

            try
            {
                var files = Directory.GetFiles(_logsDirectory, "scan_*.json")
                                   .OrderByDescending(f => File.GetCreationTime(f));

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    // Filename format: scan_yyyyMMdd_HHmmss_target.json
                    var parts = Path.GetFileNameWithoutExtension(file).Split('_', 3);
                    
                    if (parts.Length >= 3)
                    {
                        var dateStr = parts[1]; // yyyyMMdd
                        // We can just use file creation time for simplicity
                        
                        history.Add(new LogEntry
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file),
                            Date = info.CreationTime,
                            Target = parts[2].Replace("_", ".") // Simple restoration
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting history: {ex.Message}");
            }

            return history;
        }

        public void ClearHistory()
        {
            try
            {
                var files = Directory.GetFiles(_logsDirectory, "scan_*.json");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing history: {ex.Message}");
            }
        }
    }

    public class LogEntry
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime Date { get; set; }
        public string Target { get; set; } = "";
        
        public string DisplayDate => Date.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
