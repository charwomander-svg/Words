using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace Words.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var message = new StringBuilder()
            .AppendLine("An unexpected error occurred.")
            .AppendLine()
            .AppendLine(e.Exception.GetType().Name)
            .AppendLine(e.Exception.Message)
            .ToString();

        MessageBox.Show(message, "Words.Windows Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
