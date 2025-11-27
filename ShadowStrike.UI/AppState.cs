using System;
using System.IO;
using System.Text.Json;

namespace ShadowStrike.UI
{
    public class AppState
    {
        private static AppState _instance;
        
        public string TargetUrl { get; set; } = "";
        public string TargetIP { get; set; } = "";
        public System.Collections.Generic.List<string> TargetIPs { get; set; } = new System.Collections.Generic.List<string>();
        public string OpenPorts { get; set; } = "";
        public string LastScanResults { get; set; } = "";
        public DateTime LastScanTime { get; set; }
        public int TotalTestsRun { get; set; }
        public int TotalVulnerabilities { get; set; }
        public string CurrentStatus { get; set; } = "READY";
        public bool IsScanCompleted { get; set; } = false;
        public string WafDetected { get; set; } = "None Detected";
        public string Server { get; set; } = "";
        public string CMS { get; set; } = "";
        public string Technologies { get; set; } = "";
        public string Hosting { get; set; } = "";

        // Private constructor
        private AppState() { }

        public static AppState Load()
        {
            if (_instance == null)
            {
                _instance = new AppState();
            }
            return _instance;
        }

        public void Save()
        {
            // In-memory only, no action needed
        }

        public void Clear()
        {
            TargetUrl = "";
            TargetIP = "";
            TargetIPs.Clear();
            OpenPorts = "";
            LastScanResults = "";
            TotalTestsRun = 0;
            TotalVulnerabilities = 0;
            CurrentStatus = "READY";
            IsScanCompleted = false;
            WafDetected = "None Detected";
            Server = "";
            CMS = "";
            Technologies = "";
            Hosting = "";
        }
    }
}
