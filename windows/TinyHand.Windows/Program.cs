using System;
using System.Threading;
using System.Windows;

namespace TinyHand.Windows;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (Array.IndexOf(args, "--check-audio-resources") >= 0)
        {
            try
            {
                TapSound.ValidateResources();
                AudioLog.Write("Published audio resource validation: PASS (all six WAV files decoded)");
                return 0;
            }
            catch (Exception e)
            {
                AudioLog.Write($"Published audio resource validation: FAIL {e}");
                return 1;
            }
        }
        using var instance = new Mutex(true, "Local\\TinyHand.Windows", out bool first);
        if (!first)
        {
            MessageBox.Show("TinyHand läuft bereits. Du findest es im Infobereich der Taskleiste.", "TinyHand");
            return 0;
        }
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using var controller = new PointerController();
        app.Run();
        return 0;
    }
}
