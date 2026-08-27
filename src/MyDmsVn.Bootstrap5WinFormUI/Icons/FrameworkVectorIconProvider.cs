using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Renders small framework-owned structural glyphs as vector paths.
/// </summary>
public sealed class FrameworkVectorIconProvider : IIconProvider
{
    /// <inheritdoc />
    public bool CanRender(IconDescriptor descriptor)
    {
        return descriptor is not null && descriptor.SourceKind == IconSourceKind.FrameworkVector;
    }

    /// <inheritdoc />
    public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        if (!CanRender(descriptor) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        FrameworkIconGlyph glyph;
        if (!Enum.TryParse(descriptor.Value, out glyph)
            || !Enum.IsDefined(typeof(FrameworkIconGlyph), glyph))
        {
            return false;
        }

        using var path = CreatePath(glyph, bounds);
        if (path.PointCount == 0)
        {
            return false;
        }

        var strokeWidth = Math.Max(1f, Math.Min(bounds.Width, bounds.Height) / 12f);
        using var pen = new Pen(color, strokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var previousSmoothingMode = graphics.SmoothingMode;
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawPath(pen, path);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothingMode;
        }

        return true;
    }

    private static GraphicsPath CreatePath(FrameworkIconGlyph glyph, Rectangle bounds)
    {
        var path = new GraphicsPath();

        switch (glyph)
        {
            case FrameworkIconGlyph.ChevronDown:
                path.AddLines(new[]
                {
                    Point(bounds, 0.20f, 0.35f),
                    Point(bounds, 0.50f, 0.65f),
                    Point(bounds, 0.80f, 0.35f)
                });
                break;

            case FrameworkIconGlyph.ChevronUp:
                path.AddLines(new[]
                {
                    Point(bounds, 0.20f, 0.65f),
                    Point(bounds, 0.50f, 0.35f),
                    Point(bounds, 0.80f, 0.65f)
                });
                break;

            case FrameworkIconGlyph.Check:
                path.AddLines(new[]
                {
                    Point(bounds, 0.18f, 0.52f),
                    Point(bounds, 0.42f, 0.75f),
                    Point(bounds, 0.82f, 0.28f)
                });
                break;

            case FrameworkIconGlyph.Close:
                path.StartFigure();
                path.AddLine(Point(bounds, 0.25f, 0.25f), Point(bounds, 0.75f, 0.75f));
                path.StartFigure();
                path.AddLine(Point(bounds, 0.75f, 0.25f), Point(bounds, 0.25f, 0.75f));
                break;

            case FrameworkIconGlyph.Plus:
                path.StartFigure();
                path.AddLine(Point(bounds, 0.20f, 0.50f), Point(bounds, 0.80f, 0.50f));
                path.StartFigure();
                path.AddLine(Point(bounds, 0.50f, 0.20f), Point(bounds, 0.50f, 0.80f));
                break;

            case FrameworkIconGlyph.Minus:
                path.AddLine(Point(bounds, 0.20f, 0.50f), Point(bounds, 0.80f, 0.50f));
                break;
        }

        return path;
    }

    private static PointF Point(Rectangle bounds, float normalizedX, float normalizedY)
    {
        return new PointF(
            bounds.Left + (bounds.Width * normalizedX),
            bounds.Top + (bounds.Height * normalizedY));
    }
}
