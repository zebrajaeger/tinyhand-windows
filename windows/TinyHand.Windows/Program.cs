using System;
using System.Threading;
using System.Windows;

namespace TinyHand.Windows;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var instance = new Mutex(true, "Local\\TinyHand.Windows", out bool first);
        if (!first)
        {
            MessageBox.Show("TinyHand läuft bereits. Du findest es im Infobereich der Taskleiste.", "TinyHand");
            return;
        }
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var controller = new PointerController();
        app.Run();
    }
}
