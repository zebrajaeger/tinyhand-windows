using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TinyHand.Windows;

internal static class CaptureRegression
{
    private sealed class Platform : IMonitorCapturePlatform
    {
        internal CaptureTarget? Target = new("negative-monitor", new Rect(-200, -100, 100, 80), 1.25, 1.5);
        internal BitmapSource? Published;
        internal int Publishes;
        internal bool Current = true;
        internal int ClipboardFailures;
        internal bool AcquireFailure;
        internal bool SyncFailure;
        internal TaskCompletionSource? Gate;
        internal Action? Acquired;
        public CaptureTarget? ResolveTarget() => Target;
        public bool IsCurrent(CaptureTarget target) => Current;
        public async Task SynchronizeAsync(CancellationToken token)
        {
            if (SyncFailure) throw new InvalidOperationException("synthetic compositor failure");
            if (Gate != null) await Gate.Task.WaitAsync(token);
        }
        public Task<BitmapSource> AcquireAsync(CaptureTarget target, CancellationToken token)
        {
            if (AcquireFailure) throw new InvalidOperationException("synthetic acquisition failure");
            Acquired?.Invoke();
            var bytes = Enumerable.Repeat((byte)255, (int)target.Bounds.Width * (int)target.Bounds.Height * 4).ToArray();
            var bitmap = BitmapSource.Create((int)target.Bounds.Width, (int)target.Bounds.Height, 96, 96, PixelFormats.Bgra32, null, bytes, (int)target.Bounds.Width * 4);
            bitmap.Freeze();
            return Task.FromResult<BitmapSource>(bitmap);
        }
        public void Publish(BitmapSource bitmap)
        {
            Publishes++;
            if (Publishes <= ClipboardFailures) throw new COMException("synthetic clipboard busy");
            Published = bitmap;
        }
    }

    private static T Await<T>(Task<T> task)
    {
        var frame = new DispatcherFrame();
        task.ContinueWith(_ => Application.Current.Dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        return task.GetAwaiter().GetResult();
    }

    internal static void Run(Action<bool, string> check)
    {
        var platform = new Platform();
        var capture = new MonitorCapture(platform);
        var transitions = new List<bool>();
        var annotations = new Annotation[] {
            new StrokeAnnotation(Array.AsReadOnly(new[] { new Point(-220, -60), new Point(-120, -60) }), true, "#FF0000"),
            new TextAnnotation(new Point(-190, -90), "Ä", "#00FF00")
        };
        var result = Await(capture.CaptureAsync(annotations, false, (_, value) => transitions.Add(value), CancellationToken.None));
        check(result.Ok && transitions.SequenceEqual(new[] { true, false }) && !capture.Pending, "Capture suppresses/restores exactly once and publishes successfully");
        check(platform.Published is { PixelWidth: 100, PixelHeight: 80, IsFrozen: true }, "Negative-origin mixed-DPI capture preserves physical dimensions and freezes bitmap");
        var bytes = new byte[100 * 80 * 4];
        platform.Published!.CopyPixels(bytes, 400, 0);
        check(bytes[(40 * 100 + 50) * 4 + 2] > 200 && bytes[(40 * 100 + 50) * 4] < 30, "Capture composes colored stroke at physical monitor-relative position");
        check(bytes[(70 * 100 + 50) * 4] == 255, "Capture preserves unrelated background pixels");
        check(Enumerable.Range(0, 100 * 30).Any(i => bytes[i * 4 + 1] > bytes[i * 4 + 2]), "Capture includes colored text");

        int published = platform.Publishes;
        check(Await(capture.CaptureAsync(annotations, true, (_, _) => throw new Exception(), CancellationToken.None)).Error == "busy"
            && platform.Publishes == published, "Pending input prevents capture without suppression or publication");
        platform.Gate = new TaskCompletionSource();
        var pending = capture.CaptureAsync(annotations, false, (_, _) => { }, CancellationToken.None);
        check(capture.Pending && Await(capture.CaptureAsync(annotations, false, (_, _) => { }, CancellationToken.None)).Error == "busy", "Concurrent capture is rejected");
        platform.Gate.SetResult();
        check(Await(pending).Ok, "Original single-flight capture completes");
        platform.Gate = null;

        foreach (string failure in new[] { "sync", "acquire", "removed", "clipboard", "timeout", "cancel" })
        {
            var fake = new Platform { SyncFailure = failure == "sync", AcquireFailure = failure == "acquire", ClipboardFailures = failure == "clipboard" ? 5 : 0 };
            if (failure == "removed") fake.Acquired = () => fake.Current = false;
            if (failure is "timeout" or "cancel") fake.Gate = new TaskCompletionSource();
            using var cancel = new CancellationTokenSource();
            var service = new MonitorCapture(fake, failure == "timeout" ? TimeSpan.FromMilliseconds(20) : null);
            var states = new List<bool>();
            var task = service.CaptureAsync(annotations, false, (_, value) => states.Add(value), cancel.Token);
            if (failure == "cancel") cancel.Cancel();
            var failed = Await(task);
            check(!failed.Ok && states.SequenceEqual(new[] { true, false }) && !service.Pending, "Failure restores suppression and pending state: " + failure);
            check(fake.Published == null && (failure != "clipboard" || fake.Publishes == 3), "Failure never publishes fake success and bounds retries: " + failure);
            fake.Current = true; fake.Acquired = null; fake.Gate = null; fake.SyncFailure = false; fake.AcquireFailure = false; fake.ClipboardFailures = 0;
            check(Await(service.CaptureAsync(annotations, false, (_, _) => { }, CancellationToken.None)).Ok, "Capture recovers after failure: " + failure);
        }
        platform.Target = null;
        check(Await(capture.CaptureAsync(annotations, false, (_, _) => { }, CancellationToken.None)).Error == "monitorUnavailable", "Unresolved cursor monitor does not substitute primary screen");

        string directory = Path.Combine(Path.GetTempPath(), "TinyHand-capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var fake = new Platform { Gate = new TaskCompletionSource() };
            using var controller = new PointerController(Path.Combine(directory, "settings.json"), fake);
            controller.SetActive(true);
            controller.Begin(new Point(-180, -60));
            controller.End(new Point(-120, -60), false);
            var state = controller.State;
            var operation = controller.CaptureMonitorAsync();
            check(controller.State.CapturePending, "Controller exposes capture pending state");
            controller.Clear();
            controller.SetActive(false);
            check(controller.Active && controller.Annotations.Count == 1, "Controller defers mutating clear/activation during background acquisition");
            fake.Gate.SetResult();
            check(Await(operation).Ok && !controller.Active && controller.Annotations.Count == 0, "Deferred commands apply in order after acquisition");
            check(controller.State.SoundEnabled == state.SoundEnabled && controller.State.AnnotationColor == state.AnnotationColor,
                "Capture preserves settings and audio enablement");
            var frozen = new byte[100 * 80 * 4];
            fake.Published!.CopyPixels(frozen, 400, 0);
            check(frozen[(40 * 100 + 50) * 4] < 200, "Captured annotation snapshot survives deferred document clear");
            fake.Gate = null;
            fake.AcquireFailure = true;
            var commandResult = Await(new LocalCommands(controller).ExecuteAsync("capture.monitor", JsonDocument.Parse("{}").RootElement));
            check(!commandResult.Ok && commandResult.Error == "operationFailed", "External screenshot result reports actual acquisition failure");
            fake.AcquireFailure = false;
            check(Await(new LocalCommands(controller).ExecuteAsync("capture.monitor", JsonDocument.Parse("{}").RootElement)).Ok,
                "External screenshot success waits for actual clipboard adapter publication");
            fake.Target = fake.Target! with { Id = "fresh-monitor", Bounds = new Rect(-400, 0, 100, 80) };
            check(Await(controller.CaptureMonitorAsync()).Ok, "Passive capture resolves fresh platform cursor monitor on every request");
            controller.BeginTextPlacement();
            check(Await(controller.CaptureMonitorAsync()).Error == "busy" && controller.TextPhase == "placing", "Controller capture leaves a pending text draft untouched");
            controller.FinishText(null);
            controller.SetActive(true);
            controller.Begin(new Point());
            check(Await(controller.CaptureMonitorAsync()).Error == "busy" && controller.Current != null, "Controller capture leaves pending stroke untouched");
            controller.Clear();
            fake.Gate = new TaskCompletionSource();
            var interrupted = controller.CaptureMonitorAsync();
            controller.Dispose();
            check(Await(interrupted).Error == "cancelled" && !Application.Current.Windows.OfType<OverlayWindow>().Any(), "Shutdown cancels capture and removes overlays without deferred reactivation");
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}
