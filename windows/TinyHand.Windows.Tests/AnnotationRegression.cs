using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyHand.Windows;

internal static class AnnotationRegression
{
    internal static void Run(Action<bool, string> check)
    {
        var document = new AnnotationDocument();
        var stroke = new InkStroke("#ff0000");
        stroke.Add(new Point(-180, 30));
        stroke.Add(new Point(-120, 30));
        document.Commit(stroke.Snapshot());
        stroke.Points.Clear();
        check(((StrokeAnnotation)document.Items[0]).Points.Count == 2, "Committed geometry is an immutable snapshot");
        check(document.Items[0].Color == "#FF0000", "Stroke captures normalized color");
        for (int i = 0; i < 500; i++) document.Commit(new TextAnnotation(new Point(i, 0), i.ToString(), "#00FF00"));
        check(document.Items.Count == 500 && document.Items[0] is TextAnnotation { Text: "0" }, "Mixed history evicts oldest only at commit");
        var snapshot = document.Snapshot();
        document.Clear();
        check(snapshot.Length == 500 && document.Items.Count == 0, "Document snapshots survive subsequent clear");
        check(AnnotationText.Normalize("a\r\nb\nc\rd") == "a b c d", "Multiline paste becomes single line");
        string unicode = AnnotationText.Normalize(string.Concat(Enumerable.Repeat("😀", 501)));
        check(unicode.EnumerateRunes().Count() == 500 && !char.IsHighSurrogate(unicode[^1]), "Text limit preserves Unicode scalar boundaries");
        check(AnnotationColor.Normalize("invalid") == "#E14164" && !AnnotationColor.IsValid("#80FF0000"), "Invalid or translucent colors are rejected");
        var settings = new Settings { AnnotationColor = "invalid" };
        settings.Normalize();
        check(settings.AnnotationColor == AnnotationColor.Default, "Invalid stored color falls back safely");
        check(new Settings().AnnotationColor == "#E14164", "Legacy settings without color retain the original ink default");

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            AnnotationRenderer.Draw(dc, new Annotation[] {
                new StrokeAnnotation(Array.AsReadOnly(new[] { new Point(-180, 30), new Point(-120, 30) }), true, "#FF0000"),
                new TextAnnotation(new Point(-170, 45), "Ä😀", "#00FF00")
            }, new Rect(-200, 0, 100, 100), 1.25, 1.5);
        var bitmap = new RenderTargetBitmap(100, 100, 120, 144, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[100 * 100 * 4];
        bitmap.CopyPixels(pixels, 400, 0);
        check(pixels[(30 * 100 + 50) * 4 + 2] > 200, "Shared renderer converts negative origin and per-axis DPI");
        check(Enumerable.Range(0, 10000).Any(i => pixels[i * 4 + 1] > 0), "Shared renderer draws Unicode text in captured color");
        check(pixels[(30 * 100 + 10) * 4 + 3] == 0, "Negative-origin line does not drift into unrelated pixels");
        var clipped = new DrawingVisual();
        using (var dc = clipped.RenderOpen())
            AnnotationRenderer.Draw(dc, new[] {
                new StrokeAnnotation(Array.AsReadOnly(new[] { new Point(-250, 50), new Point(-50, 50) }), true, "#0000FF")
            }, new Rect(-200, 0, 100, 100), 1, 1);
        var clippedBitmap = new RenderTargetBitmap(140, 100, 96, 96, PixelFormats.Pbgra32);
        clippedBitmap.Render(clipped);
        var clippedPixels = new byte[140 * 100 * 4];
        clippedBitmap.CopyPixels(clippedPixels, 560, 0);
        check(clippedPixels[(50 * 140 + 50) * 4] > 200 && clippedPixels[(50 * 140 + 110) * 4 + 3] == 0,
            "Cross-monitor strokes clip at target monitor rather than bitmap extent");
    }
}
