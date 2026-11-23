using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TinyHand.Windows;

internal abstract record Annotation(string Color);
internal sealed record StrokeAnnotation(ReadOnlyCollection<Point> Points, bool Arrow, string InkColor) : Annotation(InkColor);
internal sealed record TextAnnotation(Point Anchor, string Text, string InkColor) : Annotation(InkColor);

internal sealed class AnnotationDocument
{
    private readonly List<Annotation> items = new();
    internal IReadOnlyList<Annotation> Items => items.AsReadOnly();
    internal void Commit(Annotation item)
    {
        items.Add(item);
        if (items.Count > 500) items.RemoveAt(0);
    }
    internal void Clear() => items.Clear();
    internal Annotation[] Snapshot() => items.ToArray();
}

internal static class AnnotationColor
{
    internal const string Default = "#E14164";
    internal static bool IsValid(string? value) => value is { Length: 7 } && value[0] == '#'
        && uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    internal static string Normalize(string? value) => IsValid(value) ? value!.ToUpperInvariant() : Default;
    internal static Color Parse(string value) => (Color)ColorConverter.ConvertFromString(Normalize(value));
}

internal static class AnnotationText
{
    internal static string Normalize(string text)
    {
        var result = new StringBuilder();
        int count = 0;
        foreach (var rune in text.Replace("\r\n", " ").EnumerateRunes())
        {
            if (count++ == 500) break;
            result.Append(rune.Value is '\r' or '\n' or 0x2028 or 0x2029 ? " " : rune.ToString());
        }
        return result.ToString();
    }
}

internal static class AnnotationRenderer
{
    // The target DrawingContext uses DIPs. Desktop coordinates and clip bounds use physical pixels.
    internal static void Draw(DrawingContext dc, IEnumerable<Annotation> items, Rect bounds, double dpiX, double dpiY)
    {
        Point Local(Point p) => new((p.X - bounds.X) / dpiX, (p.Y - bounds.Y) / dpiY);
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, bounds.Width / dpiX, bounds.Height / dpiY)));
        foreach (var item in items)
        {
            var brush = new SolidColorBrush(AnnotationColor.Parse(item.Color));
            if (item is TextAnnotation text)
            {
                var formatted = new FormattedText(text.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 28, brush, dpiX);
                dc.DrawText(formatted, Local(text.Anchor));
            }
            else if (item is StrokeAnnotation stroke && stroke.Points.Count >= 2)
            {
                var pen = new Pen(brush, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
                var path = new StreamGeometry();
                using (var context = path.Open())
                {
                    context.BeginFigure(Local(stroke.Points[0]), false, false);
                    foreach (var point in stroke.Points.Skip(1)) context.LineTo(Local(point), true, false);
                }
                dc.DrawGeometry(null, pen, path);
                // Arrow geometry is computed in local DIPs, so both axes use the target monitor DPI.
                if (stroke.Arrow)
                {
                    var end = Local(stroke.Points[^1]);
                    for (int i = stroke.Points.Count - 2; i >= 0; i--)
                    {
                        Vector direction = end - Local(stroke.Points[i]);
                        if (direction.Length < 0.001) continue;
                        direction.Normalize();
                        var normal = new Vector(-direction.Y, direction.X);
                        dc.DrawLine(pen, end, end - direction * 20 + normal * 10);
                        dc.DrawLine(pen, end, end - direction * 20 - normal * 10);
                        break;
                    }
                }
            }
        }
        dc.Pop();
    }
}
