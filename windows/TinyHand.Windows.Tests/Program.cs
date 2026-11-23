using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyHand.Windows;

internal static class Program
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            var stroke = new InkStroke();
            stroke.Add(new Point(-200, 100));
            stroke.Add(new Point(-199, 100));
            Check(stroke.Points.Count == 1, "Near-duplicate samples are filtered");
            stroke.Add(new Point(-100, 100));
            Check(stroke.Arrowhead(20) == null, "Ordinary strokes have no arrow");
            stroke.Arrow = true;
            var head = stroke.Arrowhead(20)!.Value;
            Check(head.Left == new Point(-120, 110) && head.Right == new Point(-120, 90), "Arrow geometry supports negative coordinates");
            var dot = new InkStroke { Arrow = true };
            dot.Add(new Point());
            Check(dot.Arrowhead(20) == null, "Single-point taps have no arrow");
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            string[] styles = PointerController.AvailableStyles().ToArray();
            Check(styles.Contains("lavender") && styles.Contains("laser") && styles.Length >= 37, "Built-in hand assets and laser are available");
            foreach (string style in styles.Where(s => s != "laser"))
            {
                var bitmap = new BitmapImage(new Uri($"pack://application:,,,/TinyHand.Windows;component/Hands/hand_{style}.png"));
                Check(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, $"Asset decodes: {style}");
            }
            using (var controller = new PointerController())
            {
                Native.SendMessage(controller.MessageHandle, Native.WmHotkey, new IntPtr(1), IntPtr.Zero);
                Check(controller.Active && app.Windows.Count > 0, "Overlay windows open");
                foreach (Window window in app.Windows)
                {
                    window.UpdateLayout();
                    Check(window.ActualWidth > 0 && window.ActualHeight > 0, "Overlay has nonzero dimensions");
                    var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(window);
                    Check(bitmap.PixelWidth > 0, "Overlay renders");
                }
                controller.Begin(new Point(100, 100));
                controller.Move(new Point(200, 150));
                controller.End(new Point(250, 150), true);
                Check(controller.Strokes.Count == 1 && controller.Strokes[0].Arrow, "Drawing lifecycle commits an arrow");
                Native.SendMessage(controller.MessageHandle, Native.WmHotkey, new IntPtr(2), IntPtr.Zero);
                Check(controller.Strokes.Count == 0 && controller.Current == null, "Clear removes drawing state");
                Native.SendMessage(controller.MessageHandle, Native.WmHotkey, new IntPtr(3), IntPtr.Zero);
                Check(!controller.Active && app.Windows.Count == 0, "Deactivation closes overlays");
                controller.SetActive(true);
                controller.SetActive(false);
                Check(app.Windows.Count == 0, "Repeated toggle leaves no windows");
            }
            app.Shutdown();
            Console.WriteLine($"PASS: {assertions} assertions (geometry, assets, live overlays, rendering, drawing, cleanup).");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }
}
