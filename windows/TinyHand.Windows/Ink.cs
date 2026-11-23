using System;
using System.Collections.Generic;
using System.Windows;

namespace TinyHand.Windows;

// Points are physical desktop pixels, including negative coordinates on secondary displays.
public sealed class InkStroke
{
    public List<Point> Points { get; } = new();
    public bool Arrow { get; set; }

    public void Add(Point point)
    {
        if (Points.Count == 0 || (point - Points[^1]).Length >= 2)
            Points.Add(point);
    }

    public (Point Left, Point Right)? Arrowhead(double length)
    {
        if (!Arrow || Points.Count < 2) return null;
        var end = Points[^1];
        for (int i = Points.Count - 2; i >= 0; i--)
        {
            Vector direction = end - Points[i];
            if (direction.Length < 2) continue;
            direction.Normalize();
            var normal = new Vector(-direction.Y, direction.X);
            return (end - direction * length + normal * length * 0.5,
                    end - direction * length - normal * length * 0.5);
        }
        return null;
    }
}
