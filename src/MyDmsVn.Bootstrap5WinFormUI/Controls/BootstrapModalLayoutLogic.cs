using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapModalMetrics
{
    public BootstrapModalMetrics(int borderWidth, int radius, int headerHeight, int footerHeight, int padding, int gap, int closeTargetSize)
    {
        BorderWidth = borderWidth;
        Radius = radius;
        HeaderHeight = headerHeight;
        FooterHeight = footerHeight;
        Padding = padding;
        Gap = gap;
        CloseTargetSize = closeTargetSize;
    }

    public int BorderWidth { get; }
    public int Radius { get; }
    public int HeaderHeight { get; }
    public int FooterHeight { get; }
    public int Padding { get; }
    public int Gap { get; }
    public int CloseTargetSize { get; }
}

internal static class BootstrapModalLayoutLogic
{
    public static int ResolvePreferredWidth(BootstrapModalSize mode, int customWidth, int dpi)
    {
        var logicalWidth = mode switch
        {
            BootstrapModalSize.Small => 300,
            BootstrapModalSize.Default => 500,
            BootstrapModalSize.Large => 800,
            BootstrapModalSize.ExtraLarge => 1140,
            BootstrapModalSize.Custom => customWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported modal size.")
        };

        return mode == BootstrapModalSize.Custom ? Math.Max(1, customWidth) : DpiScaler.Scale(logicalWidth, dpi);
    }

    public static Size ResolveDialogSize(
        BootstrapModalSize mode,
        Size requested,
        int contentHeight,
        Size minimum,
        Size maximum,
        Size workingArea,
        int dpi)
    {
        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workingArea), workingArea, "Working area must be positive.");
        }

        var width = ResolvePreferredWidth(mode, requested.Width, dpi);
        var height = mode == BootstrapModalSize.Custom ? Math.Max(1, requested.Height) : Math.Max(1, contentHeight);
        width = ApplyCallerConstraints(width, minimum.Width, maximum.Width);
        height = ApplyCallerConstraints(height, minimum.Height, maximum.Height);
        return new Size(Math.Min(width, workingArea.Width), Math.Min(height, workingArea.Height));
    }

    public static Rectangle CenterAndClamp(Size desired, Rectangle ownerBounds, Rectangle workingArea)
    {
        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workingArea), workingArea, "Working area must be positive.");
        }

        var width = Math.Min(Math.Max(1, desired.Width), workingArea.Width);
        var height = Math.Min(Math.Max(1, desired.Height), workingArea.Height);
        var center = ownerBounds.Width > 0 && ownerBounds.Height > 0
            ? new Point(ownerBounds.Left + ownerBounds.Width / 2, ownerBounds.Top + ownerBounds.Height / 2)
            : new Point(workingArea.Left + workingArea.Width / 2, workingArea.Top + workingArea.Height / 2);
        var x = Clamp(center.X - width / 2, workingArea.Left, workingArea.Right - width);
        var y = Clamp(center.Y - height / 2, workingArea.Top, workingArea.Bottom - height);
        return new Rectangle(x, y, width, height);
    }

    public static BootstrapModalMetrics ResolveMetrics(BootstrapThemeMetrics theme, int borderRadius, int dpi)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        if (borderRadius < -1) throw new ArgumentOutOfRangeException(nameof(borderRadius));
        var radius = borderRadius >= 0 ? borderRadius : theme.Radius;
        return new BootstrapModalMetrics(
            Math.Max(1, DpiScaler.Scale(theme.BorderWidth, dpi)),
            DpiScaler.Scale(radius, dpi),
            DpiScaler.Scale(48, dpi),
            DpiScaler.Scale(56, dpi),
            DpiScaler.Scale(theme.SpacingLG, dpi),
            DpiScaler.Scale(theme.SpacingSM, dpi),
            DpiScaler.Scale(theme.ControlHeight, dpi));
    }

    private static int ApplyCallerConstraints(int value, int minimum, int maximum)
    {
        if (maximum > 0) value = Math.Min(value, maximum);
        if (minimum > 0) value = Math.Max(value, minimum);
        return Math.Max(1, value);
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum) return minimum;
        return value > maximum ? maximum : value;
    }
}
