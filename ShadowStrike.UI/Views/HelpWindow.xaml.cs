using System.Windows;

namespace ShadowStrike.UI.Views
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GitHubIssues_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/MrShankarAryal/ShadowStrike/issues",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
