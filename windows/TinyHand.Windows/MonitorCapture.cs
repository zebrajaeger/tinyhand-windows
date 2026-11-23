using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace TinyHand.Windows;

internal sealed record CaptureTarget(string Id, Rect Bounds, double DpiX, double DpiY);
internal sealed record CaptureResult(bool Ok, string? Error = null)
{
    internal static CaptureResult Busy => new(false, "busy");
}

internal interface IMonitorCapturePlatform
{
    CaptureTarget? ResolveTarget();
    bool IsCurrent(CaptureTarget target);
    Task SynchronizeAsync(CancellationToken cancellation);
    Task<BitmapSource> AcquireAsync(CaptureTarget target, CancellationToken cancellation);
    void Publish(BitmapSource bitmap);
}

internal sealed class MonitorCapture
{
    private readonly IMonitorCapturePlatform platform;
    private readonly TimeSpan deadline;
    internal bool Pending { get; private set; }
    internal MonitorCapture(IMonitorCapturePlatform platform, TimeSpan? deadline = null)
    {
        this.platform = platform;
        this.deadline = deadline ?? TimeSpan.FromSeconds(5);
    }

    internal async Task<CaptureResult> CaptureAsync(Annotation[] snapshot, bool inputPending,
        Action<CaptureTarget, bool> suppress, CancellationToken cancellation)
    {
        if (Pending || inputPending) return CaptureResult.Busy;
        Pending = true;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(deadline);
        CaptureTarget? target = null;
        bool suppressed = false;
        try
        {
            target = platform.ResolveTarget();
            if (target == null) return new(false, "monitorUnavailable");
            BitmapSource background;
            try
            {
                suppressed = true;
                suppress(target, true);
                await platform.SynchronizeAsync(timeout.Token).WaitAsync(timeout.Token);
                if (!platform.IsCurrent(target)) return new(false, "monitorUnavailable");
                background = await platform.AcquireAsync(target, timeout.Token).WaitAsync(timeout.Token);
                if (!platform.IsCurrent(target)) return new(false, "monitorUnavailable");
            }
            finally
            {
                if (suppressed) { suppressed = false; suppress(target, false); }
            }
            timeout.Token.ThrowIfCancellationRequested();
            var bitmap = Compose(background, snapshot, target);
            timeout.Token.ThrowIfCancellationRequested();
            for (int attempt = 0; ; attempt++)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (!platform.IsCurrent(target)) return new(false, "monitorUnavailable");
                try { platform.Publish(bitmap); break; }
                catch (COMException) when (attempt < 2)
                {
                    await Task.Delay(50, timeout.Token);
                }
            }
            return new(true);
        }
        catch (OperationCanceledException) { return new(false, cancellation.IsCancellationRequested ? "cancelled" : "timeout"); }
        catch (Exception e) when (e is not OutOfMemoryException and not StackOverflowException)
        {
            return new(false, "operationFailed");
        }
        finally
        {
            try { if (suppressed && target != null) suppress(target, false); }
            finally { Pending = false; }
        }
    }

    internal static BitmapSource Compose(BitmapSource background, Annotation[] snapshot, CaptureTarget target)
    {
        int width = checked((int)target.Bounds.Width), height = checked((int)target.Bounds.Height);
        if (background.PixelWidth != width || background.PixelHeight != height)
            throw new InvalidOperationException("Capture dimensions changed.");
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(background, new Rect(0, 0, width / target.DpiX, height / target.DpiY));
            AnnotationRenderer.Draw(dc, snapshot, target.Bounds, target.DpiX, target.DpiY);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96 * target.DpiX, 96 * target.DpiY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}

internal sealed class WindowsMonitorCapturePlatform : IMonitorCapturePlatform
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);
    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    private readonly Func<Rect, (double X, double Y)> dpi;
    internal WindowsMonitorCapturePlatform(Func<Rect, (double X, double Y)> dpi) => this.dpi = dpi;

    public CaptureTarget? ResolveTarget()
    {
        if (!Native.GetCursorPos(out var cursor)) return null;
        var screen = Forms.Screen.AllScreens.FirstOrDefault(s => s.Bounds.Contains(cursor.X, cursor.Y));
        if (screen == null) return null;
        var bounds = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
        var scale = dpi(bounds);
        return new(screen.DeviceName, bounds, scale.X, scale.Y);
    }

    public bool IsCurrent(CaptureTarget target) => Forms.Screen.AllScreens.Any(s => s.DeviceName == target.Id
        && new Rect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height) == target.Bounds);

    public async Task SynchronizeAsync(CancellationToken cancellation)
    {
        // Let WPF submit the invalidated transparent frame before waiting for DWM.
        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle, cancellation);
        await Task.Run(() =>
        {
        cancellation.ThrowIfCancellationRequested();
        Marshal.ThrowExceptionForHR(DwmFlush());
        }, cancellation);
    }

    public Task<BitmapSource> AcquireAsync(CaptureTarget target, CancellationToken cancellation) => Task.Run<BitmapSource>(() =>
    {
        cancellation.ThrowIfCancellationRequested();
        int width = (int)target.Bounds.Width, height = (int)target.Bounds.Height;
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen((int)target.Bounds.X, (int)target.Bounds.Y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        cancellation.ThrowIfCancellationRequested();
        IntPtr handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally { DeleteObject(handle); }
    }, cancellation);

    public void Publish(BitmapSource bitmap) => System.Windows.Clipboard.SetImage(bitmap);
}
