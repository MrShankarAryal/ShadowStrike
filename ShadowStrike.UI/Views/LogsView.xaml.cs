using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ShadowStrike.Core;

namespace ShadowStrike.UI.Views
{
    public partial class LogsView : UserControl
    {
        private readonly HistoryManager _historyManager;
        private ComprehensiveOsintReport _selectedReport;

        public event EventHandler<ComprehensiveOsintReport> OnLoadReport;

        public LogsView()
        {
            InitializeComponent();
            _historyManager = new HistoryManager();
            LoadLogs();
        }

        private void LoadLogs()
        {
            var history = _historyManager.GetHistory();
            LogsList.ItemsSource = history;
            
            if (history.Count == 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                SelectionInfoPanel.Visibility = Visibility.Collapsed;
                LogsList.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Visible; // Still visible until selection
                LogsList.Visibility = Visibility.Visible;
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadLogs();
        }

        private void ClearAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all scan history?", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _historyManager.ClearHistory();
                LoadLogs();
                SelectionInfoPanel.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        private async void LogsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogsList.SelectedItem is LogEntry entry)
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                SelectionInfoPanel.Visibility = Visibility.Visible;
                LoadBtn.Visibility = Visibility.Collapsed; // Hide until loaded
                SummaryText.Text = "Loading report details...";

                try
                {
                    _selectedReport = await _historyManager.LoadReportAsync(entry.FilePath);
                    
                    if (_selectedReport != null)
                    {
                        SelectedTargetText.Text = _selectedReport.Target;
                        SelectedDateText.Text = $"Scanned on: {entry.DisplayDate}";
                        
                        var summary = $"Target: {_selectedReport.Target}\n";
                        summary += $"Subdomains: {_selectedReport.Subdomains?.Count ?? 0}\n";
                        summary += $"Vulnerabilities: {_selectedReport.Vulnerabilities?.Count ?? 0}\n";
                        
                        if (_selectedReport.DnsIntelligence != null && _selectedReport.DnsIntelligence.ARecords.Count > 0)
                        {
                            summary += $"IP: {_selectedReport.DnsIntelligence.ARecords.First()}\n";
                        }
                        
                        SummaryText.Text = summary;
                        LoadBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SummaryText.Text = "Error loading report file.";
                    }
                }
                catch (Exception ex)
                {
                    SummaryText.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReport != null)
            {
                OnLoadReport?.Invoke(this, _selectedReport);
            }
        }
    }
}
