using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Specialized;

namespace ShadowStrike.UI.Views
{
    public partial class TerminalView : UserControl
    {
        public TerminalView()
        {
            InitializeComponent();
            LogItemsControl.ItemsSource = Logger.Logs;
            
            // Auto-scroll to bottom
            ((INotifyCollectionChanged)LogItemsControl.Items).CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    LogScrollViewer.ScrollToEnd();
                }
            };
        }

        private void CmdInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = CmdInput.Text;
                CmdInput.Clear();
                Logger.Log($"> {command}");
                ExecuteCommand(command);
            }
        }

        private void ExecuteCommand(string command)
        {
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = $"/c {command}";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                
                // Async read to avoid blocking UI
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) Logger.Log(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) Logger.Log(e.Data); };
                
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error: {ex.Message}");
            }
        }
    }
}
