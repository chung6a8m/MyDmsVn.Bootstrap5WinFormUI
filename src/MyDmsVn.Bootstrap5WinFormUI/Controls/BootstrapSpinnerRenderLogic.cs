using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapSpinnerRenderLogic
{
    public static Color ResolveColor(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        if (!customColor.IsEmpty)
        {
            return customColor;
        }

        return BootstrapVariantColorResolver.Resolve(colors, variant);
    }

    public static int GetLogicalDiameter(BootstrapThemeMetrics metrics, BootstrapSpinnerSize size)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        switch (size)
        {
            case BootstrapSpinnerSize.Small:
                return metrics.SpacingLG;
            case BootstrapSpinnerSize.Default:
                return metrics.SpacingXL;
            case BootstrapSpinnerSize.Large:
                return metrics.ControlHeight;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, "Unsupported spinner size.");
        }
    }

    public static double GetGrowScale(double progress)
    {
        if (double.IsNaN(progress) || double.IsInfinity(progress))
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a finite number.");
        }

        if (progress < 0.0)
        {
            progress = 0.0;
        }
        else if (progress > 1.0)
        {
            progress = 1.0;
        }

        var pulse = 0.5 - (0.5 * Math.Cos(2.0 * Math.PI * progress));
        return 0.65 + (0.35 * pulse);
    }
}
