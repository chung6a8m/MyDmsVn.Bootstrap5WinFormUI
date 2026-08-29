using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapConnectedButtonLayoutLogic
{
    internal static int ResolveSeamOverlap(BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        return Math.Max(1, DpiScaler.Scale(metrics.BorderWidth, dpi));
    }

    internal static CornerRadius ResolveCornerRadius(
        Orientation orientation,
        int index,
        int count,
        float radius)
    {
        if (orientation != Orientation.Horizontal && orientation != Orientation.Vertical)
        {
            throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Unsupported orientation.");
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Connected button count must be positive.");
        }

        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Connected button index is outside the group.");
        }

        if (radius < 0f || float.IsNaN(radius) || float.IsInfinity(radius))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Corner radius must be finite and non-negative.");
        }

        if (count == 1)
        {
            return new CornerRadius(radius);
        }

        if (orientation == Orientation.Horizontal)
        {
            if (index == 0)
            {
                return new CornerRadius(radius, 0f, 0f, radius);
            }

            return index == count - 1
                ? new CornerRadius(0f, radius, radius, 0f)
                : CornerRadius.Empty;
        }

        if (index == 0)
        {
            return new CornerRadius(radius, radius, 0f, 0f);
        }

        return index == count - 1
            ? new CornerRadius(0f, 0f, radius, radius)
            : CornerRadius.Empty;
    }
}
