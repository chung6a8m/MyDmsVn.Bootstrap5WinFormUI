using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapTreeNodeVisualState
{
    public BootstrapTreeNodeVisualState(bool selected, bool hot, bool enabled)
    {
        Selected = selected;
        Hot = hot;
        Enabled = enabled;
    }

    public bool Selected { get; }

    public bool Hot { get; }

    public bool Enabled { get; }
}

internal readonly struct BootstrapTreeNodePalette
{
    public BootstrapTreeNodePalette(Color background, Color foreground, Color accentBorder)
    {
        Background = background;
        Foreground = foreground;
        AccentBorder = accentBorder;
    }

    public Color Background { get; }

    public Color Foreground { get; }

    public Color AccentBorder { get; }
}

internal static class BootstrapTreeViewRenderLogic
{
    public static BootstrapTreeNodePalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        BootstrapTreeNodeVisualState state)
    {
        var variantColor = BootstrapVariantColorResolver.Resolve(colors, variant);

        if (!state.Enabled)
        {
            return new BootstrapTreeNodePalette(
                colors.Surface,
                colors.MutedText,
                Color.Transparent);
        }

        if (state.Selected)
        {
            return new BootstrapTreeNodePalette(
                variantColor,
                ColorUtil.GetContrastingTextColor(variantColor, colors.Light, colors.Dark),
                variantColor);
        }

        if (state.Hot)
        {
            return new BootstrapTreeNodePalette(
                colors.Hover,
                colors.Text,
                Color.Transparent);
        }

        return new BootstrapTreeNodePalette(
            colors.Surface,
            colors.Text,
            Color.Transparent);
    }
}
