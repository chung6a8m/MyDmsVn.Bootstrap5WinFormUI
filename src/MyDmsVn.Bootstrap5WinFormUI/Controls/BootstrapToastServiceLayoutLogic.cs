using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapToastServiceLayoutLogic
{
    public static Rectangle InsetWorkingArea(Rectangle workingArea, Padding logicalMargin, int dpi)
    {
        ValidatePositiveSize(workingArea.Size, nameof(workingArea));
        if (logicalMargin.Left < 0 || logicalMargin.Top < 0 || logicalMargin.Right < 0 || logicalMargin.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalMargin), logicalMargin, "Screen margin edges cannot be negative.");
        }

        var scaled = DpiScaler.Scale(logicalMargin, dpi);
        var left = Clamp((long)workingArea.Left + scaled.Left, workingArea.Left, (long)workingArea.Right - 1);
        var top = Clamp((long)workingArea.Top + scaled.Top, workingArea.Top, (long)workingArea.Bottom - 1);
        var right = Clamp((long)workingArea.Right - scaled.Right, left + 1, workingArea.Right);
        var bottom = Clamp((long)workingArea.Bottom - scaled.Bottom, top + 1, workingArea.Bottom);
        return Rectangle.FromLTRB((int)left, (int)top, (int)right, (int)bottom);
    }

    public static int ResolveToastWidth(int logicalToastWidth, int availablePixelWidth, int dpi)
    {
        if (logicalToastWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalToastWidth), logicalToastWidth, "Toast width must be greater than zero.");
        }

        if (availablePixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availablePixelWidth), availablePixelWidth, "Available width must be greater than zero.");
        }

        return Math.Min(Math.Max(1, DpiScaler.Scale(logicalToastWidth, dpi)), availablePixelWidth);
    }

    public static Size ResolveNotificationCenterSize(Size logicalPreferredSize, Size availablePixelSize, int dpi)
    {
        ValidatePositiveSize(logicalPreferredSize, nameof(logicalPreferredSize));
        ValidatePositiveSize(availablePixelSize, nameof(availablePixelSize));
        var scaled = DpiScaler.Scale(logicalPreferredSize, dpi);
        return new Size(
            Math.Min(Math.Max(1, scaled.Width), availablePixelSize.Width),
            Math.Min(Math.Max(1, scaled.Height), availablePixelSize.Height));
    }

    public static Rectangle CalculateNotificationCenterBounds(
        Rectangle availableWorkingArea,
        Size desiredPixelSize,
        BootstrapToastPlacement placement)
    {
        ValidatePositiveSize(availableWorkingArea.Size, nameof(availableWorkingArea));
        ValidatePositiveSize(desiredPixelSize, nameof(desiredPixelSize));
        BootstrapToastLayoutLogic.ValidatePlacement(placement);

        var width = Math.Min(desiredPixelSize.Width, availableWorkingArea.Width);
        var height = Math.Min(desiredPixelSize.Height, availableWorkingArea.Height);
        var right = placement == BootstrapToastPlacement.TopRight || placement == BootstrapToastPlacement.BottomRight;
        var bottom = placement == BootstrapToastPlacement.BottomLeft || placement == BootstrapToastPlacement.BottomRight;
        return new Rectangle(
            right ? availableWorkingArea.Right - width : availableWorkingArea.Left,
            bottom ? availableWorkingArea.Bottom - height : availableWorkingArea.Top,
            width,
            height);
    }

    private static long Clamp(long value, long minimum, long maximum)
    {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }

    private static void ValidatePositiveSize(Size size, string parameterName)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Dimensions must be greater than zero.");
        }
    }
}
