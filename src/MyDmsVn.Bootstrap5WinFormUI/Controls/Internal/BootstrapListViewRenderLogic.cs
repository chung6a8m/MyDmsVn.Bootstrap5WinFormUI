using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapListViewItemVisualState
{
    Neutral,
    Hovered,
    SelectedActive,
    SelectedInactive,
    Disabled
}

internal readonly struct BootstrapListViewItemPalette
{
    public BootstrapListViewItemPalette(Color backColor, Color foreColor, Color borderColor)
    {
        BackColor = backColor;
        ForeColor = foreColor;
        BorderColor = borderColor;
    }

    public Color BackColor { get; }

    public Color ForeColor { get; }

    public Color BorderColor { get; }
}

internal static class BootstrapListViewRenderLogic
{
    internal static BootstrapListViewItemVisualState ResolveState(
        bool enabled,
        bool selected,
        bool controlFocused,
        bool hideSelection,
        bool hovered)
    {
        if (!enabled)
        {
            return BootstrapListViewItemVisualState.Disabled;
        }

        if (selected && controlFocused)
        {
            return BootstrapListViewItemVisualState.SelectedActive;
        }

        if (selected && !hideSelection)
        {
            return BootstrapListViewItemVisualState.SelectedInactive;
        }

        return hovered
            ? BootstrapListViewItemVisualState.Hovered
            : BootstrapListViewItemVisualState.Neutral;
    }

    internal static bool ShouldUseStripe(View view, bool striped, int itemIndex)
    {
        return striped &&
               itemIndex >= 0 &&
               (itemIndex & 1) == 1 &&
               (view == View.Details || view == View.List);
    }

    internal static bool HasEffectiveColorOverride(Color candidate, Color inheritedColor)
    {
        return candidate.ToArgb() != inheritedColor.ToArgb();
    }

    internal static BootstrapListViewItemPalette ResolvePalette(
        BootstrapTheme theme,
        BootstrapVariant variant,
        BootstrapListViewItemVisualState state,
        bool striped,
        bool hasCallerBackColor,
        Color callerBackColor,
        bool hasCallerForeColor,
        Color callerForeColor)
    {
        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        var colors = theme.Colors;
        var accent = BootstrapVariantColorResolver.Resolve(colors, variant);

        switch (state)
        {
            case BootstrapListViewItemVisualState.Disabled:
                return new BootstrapListViewItemPalette(
                    ColorUtil.Blend(colors.Disabled, colors.Surface, 0.2f),
                    colors.MutedText,
                    colors.Border);
            case BootstrapListViewItemVisualState.SelectedActive:
                return new BootstrapListViewItemPalette(
                    accent,
                    ColorUtil.GetContrastingTextColor(accent, colors.Light, colors.Dark),
                    accent);
            case BootstrapListViewItemVisualState.SelectedInactive:
                return new BootstrapListViewItemPalette(
                    ColorUtil.Blend(accent, colors.Surface, 0.18f),
                    colors.Text,
                    colors.Border);
            case BootstrapListViewItemVisualState.Hovered:
                return new BootstrapListViewItemPalette(colors.Hover, colors.Text, colors.Border);
            case BootstrapListViewItemVisualState.Neutral:
                var background = hasCallerBackColor
                    ? callerBackColor
                    : striped ? colors.SurfaceSecondary : colors.Surface;
                var foreground = hasCallerForeColor ? callerForeColor : colors.Text;
                return new BootstrapListViewItemPalette(background, foreground, colors.Border);
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported ListView item visual state.");
        }
    }
}
