using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

/// <summary>
/// Creates disposable rounded-rectangle paths with optional per-corner radii.
/// </summary>
public static class RoundedPath
{
    /// <summary>
    /// Creates a closed path for the supplied rectangle and corner radii.
    /// The caller owns and must dispose the returned path.
    /// </summary>
    public static GraphicsPath Create(RectangleF bounds, CornerRadius cornerRadius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return path;
        }

        var radius = cornerRadius.NormalizeTo(bounds);
        if (radius == CornerRadius.Empty)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;

        path.StartFigure();
        path.AddLine(left + radius.TopLeft, top, right - radius.TopRight, top);
        AddArc(path, right - (radius.TopRight * 2f), top, radius.TopRight, 270f);

        path.AddLine(right, top + radius.TopRight, right, bottom - radius.BottomRight);
        AddArc(path, right - (radius.BottomRight * 2f), bottom - (radius.BottomRight * 2f), radius.BottomRight, 0f);

        path.AddLine(right - radius.BottomRight, bottom, left + radius.BottomLeft, bottom);
        AddArc(path, left, bottom - (radius.BottomLeft * 2f), radius.BottomLeft, 90f);

        path.AddLine(left, bottom - radius.BottomLeft, left, top + radius.TopLeft);
        AddArc(path, left, top, radius.TopLeft, 180f);
        path.CloseFigure();

        return path;
    }

    private static void AddArc(GraphicsPath path, float x, float y, float radius, float startAngle)
    {
        if (radius <= 0f)
        {
            return;
        }

        var diameter = radius * 2f;
        path.AddArc(x, y, diameter, diameter, startAngle, 90f);
    }
}
