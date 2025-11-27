using System;
using System.IO;
using System.Windows;

namespace ShadowStrike.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                LogException((Exception)args.ExceptionObject, "AppDomain.UnhandledException");

            DispatcherUnhandledException += (s, args) =>
            {
                LogException(args.Exception, "DispatcherUnhandledException");
                args.Handled = true;
            };

            base.OnStartup(e);
        }

        private void LogException(Exception ex, string source)
        {
            string message = $"[{DateTime.Now}] {source}: {ex.Message}\n{ex.StackTrace}\n";
            if (ex.InnerException != null)
            {
                message += $"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}\n";
            }
            try
            {
                System.IO.File.AppendAllText("crash.log", message);
            }
            catch { }
        }
    }
}
