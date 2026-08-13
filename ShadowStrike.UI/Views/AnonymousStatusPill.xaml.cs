using System.Windows.Controls;
using System.Windows.Media;

namespace ShadowStrike.UI.Views
{
    public partial class AnonymousStatusPill : UserControl
    {
        public AnonymousStatusPill()
        {
            InitializeComponent();
            SetOff();
        }

        public void SetOff()
        {
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8899A6"));
            StatusText.Text = "ANONYMOUS: OFF";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8899A6"));
            PillBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363D"));
        }

        public void SetVerifying()
        {
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3B341")); // Yellow
            StatusText.Text = "ANONYMOUS: VERIFYING ◌";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3B341"));
            PillBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3B341"));
        }

        public void SetVerified(string modeName = "Lightweight")
        {
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88")); // Green
            StatusText.Text = $"ANONYMOUS ● ({modeName})";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88"));
            PillBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF88"));
        }

        public void SetCompromised()
        {
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D")); // Red
            StatusText.Text = "ANONYMOUS: COMPROMISED ✕";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D"));
            PillBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D"));
        }
    }
}
