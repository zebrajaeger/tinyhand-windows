using System;
using System.Windows;
using System.Windows.Media;

namespace TinyHand.Windows;

// Screen-local WPF coordinates (Y points down, unlike AppKit).
internal sealed class HandPose
{
    private double? gripX;
    internal double Angle { get; private set; }

    internal void Update(Point cursor, double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        double target = Math.Clamp(cursor.X + 0.30 * width, 0.15 * width, 1.05 * width);
        double grip = gripX is { } previous ? previous + (target - previous) * 0.08 : target;
        gripX = grip;
        double depth = 1.12 * height;
        Angle = -Math.Atan2(grip - cursor.X, depth - cursor.Y) * 180 / Math.PI;
    }

    internal Matrix Transform(Point cursor, Point fingertip, double scale)
    {
        var matrix = Matrix.Identity;
        matrix.Translate(-fingertip.X, -fingertip.Y);
        matrix.Scale(scale, scale);
        matrix.Rotate(Angle);
        matrix.Translate(cursor.X, cursor.Y);
        return matrix;
    }
}
