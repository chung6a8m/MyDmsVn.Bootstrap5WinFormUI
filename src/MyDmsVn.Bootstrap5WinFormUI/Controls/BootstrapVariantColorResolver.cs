using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal static class BootstrapVariantColorResolver
{
    public static Color Resolve(BootstrapThemeColors colors, BootstrapVariant variant)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        switch (variant)
        {
            case BootstrapVariant.Primary:
                return colors.Primary;
            case BootstrapVariant.Secondary:
                return colors.Secondary;
            case BootstrapVariant.Success:
                return colors.Success;
            case BootstrapVariant.Danger:
                return colors.Danger;
            case BootstrapVariant.Warning:
                return colors.Warning;
            case BootstrapVariant.Info:
                return colors.Info;
            case BootstrapVariant.Light:
                return colors.Light;
            case BootstrapVariant.Dark:
                return colors.Dark;
            default:
                throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }
    }
}
