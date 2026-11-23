using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyHand.Windows;

internal static class TrayHandIcon
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    internal static string HandStyle(string style) => style == "laser" ? "lavender" : style;

    internal static Int32Rect HandBounds(BitmapSource image)
    {
        var pixels = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int width = pixels.PixelWidth, height = pixels.PixelHeight;
        int stride = width * 4;
        var bytes = new byte[stride * height];
        pixels.CopyPixels(bytes, stride, 0);
        var counts = new int[height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (bytes[y * stride + x * 4 + 3] > 128) counts[y]++;
        int top = Array.FindIndex(counts, count => count > 0);
        if (top < 0) throw new InvalidOperationException("Hand image is transparent.");
        int maximum = 0;
        for (int y = top; y < Math.Min(height, top + (int)(0.45 * height)); y++)
            maximum = Math.Max(maximum, counts[y]);
        // Like the macOS asset measurement: wide fist followed by narrow wrist.
        int fist = top;
        while (fist < height && counts[fist] <= 0.6 * maximum) fist++;
        int bottom = fist;
        while (bottom < height && counts[bottom] >= 0.35 * maximum) bottom++;
        if (bottom <= top || bottom >= height)
            bottom = Array.FindLastIndex(counts, count => count > 0) + 1;
        int left = width, right = -1;
        for (int y = top; y < bottom; y++)
            for (int x = 0; x < width; x++)
                if (bytes[y * stride + x * 4 + 3] > 128)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                }
        return new Int32Rect(left, top, right - left + 1, bottom - top);
    }

    internal static System.Drawing.Icon Create(string style)
    {
        string hand = HandStyle(style);
        var image = new BitmapImage(new Uri($"pack://application:,,,/TinyHand.Windows;component/Hands/hand_{hand}.png"));
        var cropped = new CroppedBitmap(image, HandBounds(image));
        const int size = 32;
        double scale = (size - 4.0) / Math.Max(cropped.PixelWidth, cropped.PixelHeight);
        double width = cropped.PixelWidth * scale, height = cropped.PixelHeight * scale;
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var dc = visual.RenderOpen())
            dc.DrawImage(cropped, new Rect((size - width) / 2, (size - height) / 2, width, height));
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;
        using var drawing = new System.Drawing.Bitmap(stream);
        IntPtr handle = drawing.GetHicon();
        try
        {
            using var borrowed = System.Drawing.Icon.FromHandle(handle);
            return (System.Drawing.Icon)borrowed.Clone();
        }
        finally { DestroyIcon(handle); }
    }
}
