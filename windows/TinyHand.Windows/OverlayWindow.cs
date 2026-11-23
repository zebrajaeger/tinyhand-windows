using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyHand.Windows;

internal sealed class OverlayWindow : Window
{
    private readonly PointerController controller;
    private readonly System.Drawing.Rectangle bounds;
    private BitmapSource? hand;
    private Point fingertip;
    private readonly Pen ink = new(new SolidColorBrush(Color.FromRgb(225, 65, 100)), 4)
    {
        StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round
    };

    internal OverlayWindow(PointerController controller, System.Drawing.Rectangle bounds)
    {
        this.controller = controller;
        this.bounds = bounds;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Cursor = Cursors.None;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            Native.SetWindowLong(handle, -20, Native.GetWindowLong(handle, -20) | 0x08000000 | 0x00000080);
            Native.SetWindowPos(handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0010);
        };
        LoadStyle();
    }

    internal void LoadStyle()
    {
        hand = null;
        if (controller.Style == "laser") return;
        var image = new BitmapImage(new Uri($"pack://application:,,,/TinyHand.Windows;component/Hands/hand_{controller.Style}.png"));
        var pixels = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int stride = pixels.PixelWidth * 4;
        var bytes = new byte[stride * pixels.PixelHeight];
        pixels.CopyPixels(bytes, stride, 0);
        // Match the real visible fingertip rather than the transparent image margin.
        for (int y = 0; y < pixels.PixelHeight; y++)
        {
            int first = -1, last = -1;
            for (int x = 0; x < pixels.PixelWidth; x++)
                if (bytes[y * stride + x * 4 + 3] > 128) { if (first < 0) first = x; last = x; }
            if (first < 0) continue;
            fingertip = new Point((first + last) / 2.0, y);
            break;
        }
        image.Freeze();
        hand = image;
        InvalidateVisual();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ChangedButton != MouseButton.Left) return;
        CaptureMouse();
        controller.Begin(PointToScreen(e.GetPosition(this)));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (IsMouseCaptured) controller.Move(PointToScreen(e.GetPosition(this)));
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ChangedButton != MouseButton.Left || !IsMouseCaptured) return;
        controller.End(PointToScreen(e.GetPosition(this)), Native.GetAsyncKeyState(0x10) < 0);
        ReleaseMouseCapture();
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        if (controller.Current != null) controller.End(controller.Cursor, false);
        base.OnLostMouseCapture(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e) => e.Handled = true;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        double dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        foreach (var stroke in controller.Strokes)
        {
            if (stroke.Points.Count < 2) continue;
            var path = new StreamGeometry();
            using (var context = path.Open())
            {
                context.BeginFigure(PointFromScreen(stroke.Points[0]), false, false);
                for (int i = 1; i < stroke.Points.Count; i++)
                    context.LineTo(PointFromScreen(stroke.Points[i]), true, false);
            }
            dc.DrawGeometry(null, ink, path);
            var arrow = stroke.Arrowhead(20 * dpi);
            if (arrow is { } a)
            {
                var end = PointFromScreen(stroke.Points[^1]);
                dc.DrawLine(ink, end, PointFromScreen(a.Left));
                dc.DrawLine(ink, end, PointFromScreen(a.Right));
            }
        }
        double age = (DateTime.UtcNow - controller.TapTime).TotalSeconds;
        if (age >= 0 && age < 0.4)
        {
            var ring = new SolidColorBrush(Color.FromArgb((byte)(220 * (1 - age / 0.4)), 255, 100, 140));
            dc.DrawEllipse(null, new Pen(ring, 3), PointFromScreen(controller.TapPoint), 8 + age * 60, 8 + age * 60);
        }
        var cursor = PointFromScreen(controller.Cursor);
        if (hand != null)
        {
            double scale = (double)controller.PointerHeight / hand.PixelHeight;
            dc.DrawImage(hand, new Rect(cursor.X - fingertip.X * scale, cursor.Y - fingertip.Y * scale,
                hand.PixelWidth * scale, hand.PixelHeight * scale));
        }
        else
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 255, 30, 60)), null, cursor, 16, 16);
            dc.DrawEllipse(Brushes.Crimson, null, cursor, 7, 7);
            dc.DrawEllipse(Brushes.MistyRose, null, cursor, 2.5, 2.5);
        }
        dc.Pop();
    }
}
