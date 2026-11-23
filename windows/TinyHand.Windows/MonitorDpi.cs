using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace TinyHand.Windows;

internal static class MonitorDpi
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PhysicalPoint { internal int X; internal int Y; }
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(PhysicalPoint point, uint flags);
    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);

    internal static (double X, double Y) Get(Rect bounds)
    {
        var handle = MonitorFromPoint(new PhysicalPoint { X = (int)bounds.X, Y = (int)bounds.Y }, 0);
        if (handle == IntPtr.Zero || GetDpiForMonitor(handle, 0, out uint x, out uint y) != 0)
            throw new InvalidOperationException("Monitor DPI unavailable.");
        return (x / 96.0, y / 96.0);
    }
}
