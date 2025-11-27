using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ShadowStrike.UI.Views
{
    public partial class CustomMessageBox : Window
    {
        public bool Result { get; private set; } = false;

        public CustomMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;

            // Set Icon
            switch (icon)
            {
                case MessageBoxImage.Error:
                    MessageIcon.Kind = PackIconKind.Error;
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DA3633"));
                    break;
                case MessageBoxImage.Warning:
                    MessageIcon.Kind = PackIconKind.Warning;
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D29922"));
                    break;
                case MessageBoxImage.Question:
                    MessageIcon.Kind = PackIconKind.QuestionMarkCircle;
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF"));
                    break;
                default:
                    MessageIcon.Kind = PackIconKind.Information;
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58A6FF"));
                    break;
            }

            // Set Buttons
            if (button == MessageBoxButton.YesNo || button == MessageBoxButton.OKCancel)
            {
                CancelButton.Visibility = Visibility.Visible;
                CancelButton.Content = button == MessageBoxButton.YesNo ? "No" : "Cancel";
                OkButton.Content = button == MessageBoxButton.YesNo ? "Yes" : "OK";
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static bool Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            var msgBox = new CustomMessageBox(message, title, button, icon);
            if (Application.Current.MainWindow != null)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }
            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}
