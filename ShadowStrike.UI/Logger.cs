using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace ShadowStrike.UI
{
    public static class Logger
    {
        public static ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        private static object _lock = new object();

        public static void Log(string message)
        {
            lock (_lock)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                    if (Logs.Count > 1000)
                    {
                        Logs.RemoveAt(0);
                    }
                });
            }
        }
    }
}
