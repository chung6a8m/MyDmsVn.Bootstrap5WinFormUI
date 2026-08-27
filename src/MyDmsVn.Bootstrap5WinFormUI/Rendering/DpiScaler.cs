using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

/// <summary>
/// Scales logical 96-DPI measurements for a target Windows DPI value.
/// </summary>
public static class DpiScaler
{
    /// <summary>
    /// The WinForms logical-DPI baseline used by framework design tokens.
    /// </summary>
    public const int DefaultDpi = 96;

    /// <summary>
    /// Scales an integer logical-pixel value for the specified DPI.
    /// </summary>
    public static int Scale(int logicalPixels, int dpi)
    {
        ValidateDpi(dpi);
        return (int)Math.Round(
            logicalPixels * (double)dpi / DefaultDpi,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Scales a floating-point logical-pixel value for the specified DPI.
    /// </summary>
    public static float Scale(float logicalPixels, int dpi)
    {
        ValidateDpi(dpi);
        return logicalPixels * dpi / DefaultDpi;
    }

    /// <summary>
    /// Scales both dimensions of a <see cref="Size"/>.
    /// </summary>
    public static Size Scale(Size logicalSize, int dpi)
    {
        return new Size(
            Scale(logicalSize.Width, dpi),
            Scale(logicalSize.Height, dpi));
    }

    /// <summary>
    /// Scales every edge of a WinForms <see cref="Padding"/> value.
    /// </summary>
    public static Padding Scale(Padding logicalPadding, int dpi)
    {
        return new Padding(
            Scale(logicalPadding.Left, dpi),
            Scale(logicalPadding.Top, dpi),
            Scale(logicalPadding.Right, dpi),
            Scale(logicalPadding.Bottom, dpi));
    }

    /// <summary>
    /// Scales the position and size of a <see cref="Rectangle"/>.
    /// </summary>
    public static Rectangle Scale(Rectangle logicalRectangle, int dpi)
    {
        return new Rectangle(
            Scale(logicalRectangle.X, dpi),
            Scale(logicalRectangle.Y, dpi),
            Scale(logicalRectangle.Width, dpi),
            Scale(logicalRectangle.Height, dpi));
    }

    private static void ValidateDpi(int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }
    }
}
