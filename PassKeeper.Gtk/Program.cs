using Gtk;
using PassKeeper.Gtk.Extensions;
using System.Reflection;

namespace PassKeeper.Gtk;

static class Program
{
    static void Main()
    {
        Application.Init();

        var version = GetAppVersion();

        var mainWindow = new MainWindow($"PassKeeper {version}");
        mainWindow.ShowAll();

        Application.Run(); 
    }

    private static string GetAppVersion()
    {
        var entry = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = entry.GetName().Version?.ToFormatedString();
        return version ?? string.Empty;
    }
}
