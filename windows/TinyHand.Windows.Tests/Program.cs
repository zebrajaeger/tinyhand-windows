using System;
using System.Linq;
using System.IO;
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
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--audio-regression") >= 0)
            return AudioRegression.Run();
        try
        {
            var pose = new HandPose();
            var tip = new Point(180, 12);
            var cursor = new Point(500, 400);
            pose.Update(cursor, 1920, 1080);
            Check(pose.Angle < 0 && pose.Angle > -90, "Hand leans toward a grip below and right of the cursor");
            foreach (double scale in new[] { 0.15, 0.5, 1.0 })
                Check((pose.Transform(cursor, tip, scale).Transform(tip) - cursor).Length < 0.000001,
                    "Rotated fingertip stays anchored at every pointer size");
            var moved = new Point(1800, 400);
            pose.Update(moved, 1920, 1080);
            var fresh = new HandPose();
            fresh.Update(moved, 1920, 1080);
            Check(pose.Angle > fresh.Angle, "Grip lags a fast rightward movement");
            for (int i = 0; i < 200; i++) pose.Update(moved, 1920, 1080);
            Check(Math.Abs(pose.Angle - fresh.Angle) < 0.001, "Grip settles toward the new cursor position");
            pose.Update(new Point(1900, 1079), 1920, 1080);
            Check(double.IsFinite(pose.Angle), "Hand angle remains finite at screen edges");
            pose.Update(new Point(-200, -100), 1920, 1080);
            Check((pose.Transform(new Point(-200, -100), tip, 0.5).Transform(tip) - new Point(-200, -100)).Length < 0.000001,
                "Fingertip anchoring supports off-screen coordinates");
            double previousAngle = pose.Angle;
            pose.Update(cursor, 0, 0);
            Check(pose.Angle == previousAngle, "Unlaid-out overlays do not corrupt the pose");
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
            string testDirectory = Path.Combine(Path.GetTempPath(), "TinyHand-tests-" + Guid.NewGuid().ToString("N"));
            string settingsPath = Path.Combine(testDirectory, "settings.json");
            var defaults = Settings.Load(settingsPath);
            Check(defaults.Style == "lavender" && defaults.PointerHeight == 340 && !defaults.SoundEnabled, "Missing settings use defaults");
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(settingsPath, "not json");
            Check(Settings.Load(settingsPath).PointerHeight == 340, "Corrupt settings recover safely");
            File.WriteAllText(settingsPath, "{\"Style\":null,\"PointerHeight\":9000,\"Sound\":\"missing\"}");
            var normalized = Settings.Load(settingsPath);
            Check(normalized.Style == "lavender" && normalized.PointerHeight == 640 && normalized.Sound == "thock", "Invalid settings are normalized");
            normalized.Style = "laser";
            normalized.SoundEnabled = true;
            normalized.Sound = "pop";
            Check(normalized.Save(settingsPath), "Settings save atomically");
            var restored = Settings.Load(settingsPath);
            Check(restored.Style == "laser" && restored.SoundEnabled && restored.Sound == "pop", "Settings round trip");
            Check(!File.Exists(settingsPath + ".tmp"), "Atomic save leaves no temporary file");
            Check(!restored.Save(settingsPath + "/invalid.json"), "Write failures are nonfatal");
            defaults.Save(settingsPath);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--audio-probe") >= 0)
            {
                using var audio = new TapSound();
                audio.Play("quack");
                System.Threading.Thread.Sleep(1000);
                audio.Play("quack");
                System.Threading.Thread.Sleep(1000);
            }
            foreach (string sound in TapSound.Names)
            {
                using var resource = TapSound.OpenSound(sound);
                var header = new byte[12];
                resource.ReadExactly(header);
                Check(System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WAVE", "Embedded WAV is valid: " + sound);
                using var data = new MemoryStream();
                resource.Position = 0;
                resource.CopyTo(data);
                data.Position = 0;
                using var player = new System.Media.SoundPlayer(data);
                player.Load();
                Check(player.IsLoadCompleted, "Windows audio decoder accepts: " + sound);
            }
            using (var controller = new PointerController(settingsPath))
            {
                Native.SendMessage(controller.MessageHandle, Native.WmHotkey, new IntPtr(1), IntPtr.Zero);
                Check(controller.Active && app.Windows.Count > 0, "Overlay windows open");
                foreach (Window window in app.Windows)
                {
                    window.UpdateLayout();
                    if (window is OverlayWindow overlay) overlay.UpdatePose();
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
                controller.SelectStyle("laser");
                controller.SetPointerHeight(480);
                Check(Settings.Load(settingsPath).Style == "laser" && Settings.Load(settingsPath).PointerHeight == 480, "Tray choices persist");
                controller.SelectStyle("invalid");
                Check(controller.Style == "laser", "Unknown style cannot corrupt selection");
            }
            using (var controller = new PointerController(settingsPath))
                Check(controller.Style == "laser" && controller.PointerHeight == 480 && !controller.Active, "Restart restores preferences, never activates the overlay");
            File.Delete(settingsPath);
            Directory.Delete(testDirectory);
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
