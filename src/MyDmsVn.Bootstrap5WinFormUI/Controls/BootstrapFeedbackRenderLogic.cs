using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapFeedbackPalette
{
    public BootstrapFeedbackPalette(Color surface, Color border, Color foreground, Color focus)
    {
        Surface = surface;
        Border = border;
        Foreground = foreground;
        Focus = focus;
    }

    public Color Surface { get; }
    public Color Border { get; }
    public Color Foreground { get; }
    public Color Focus { get; }
}

internal static class BootstrapFeedbackRenderLogic
{
    private const float SurfaceSemanticAmount = 0.12f;
    private const float BorderSemanticAmount = 0.45f;
    private const float ForegroundSemanticAmount = 0.72f;
    private const double MinimumTextContrast = 4.5d;

    public static void ValidateVariant(BootstrapVariant variant)
    {
        if (variant < BootstrapVariant.Primary || variant > BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported Bootstrap variant.");
        }
    }

    public static BootstrapFeedbackPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        ValidateVariant(variant);

        if (!enabled)
        {
            return new BootstrapFeedbackPalette(
                colors.SurfaceSecondary,
                colors.Border,
                colors.MutedText,
                colors.Disabled);
        }

        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        var surface = ColorUtil.Blend(semantic, colors.Surface, SurfaceSemanticAmount);
        var border = ColorUtil.Blend(semantic, colors.Border, BorderSemanticAmount);
        var foregroundCandidate = ColorUtil.Blend(semantic, colors.Text, ForegroundSemanticAmount);
        var foreground = ColorUtil.GetContrastRatio(foregroundCandidate, surface) >= MinimumTextContrast
            ? foregroundCandidate
            : colors.Text;

        return new BootstrapFeedbackPalette(surface, border, foreground, colors.Focus);
    }
}
