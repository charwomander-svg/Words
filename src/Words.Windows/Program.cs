using Words.Windows;
using Words.Core.Services;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new WordleForm(new WordleService(WordService.FromEmbeddedResource())));
    }
}
