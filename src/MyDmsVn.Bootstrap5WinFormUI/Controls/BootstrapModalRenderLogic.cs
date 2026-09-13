using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapModalVisualState
{
    public BootstrapModalVisualState(Color surface, Color text, Color mutedText, Color border, Color backdrop, double backdropOpacity)
    {
        Surface = surface;
        Text = text;
        MutedText = mutedText;
        Border = border;
        Backdrop = backdrop;
        BackdropOpacity = backdropOpacity;
    }

    public Color Surface { get; }
    public Color Text { get; }
    public Color MutedText { get; }
    public Color Border { get; }
    public Color Backdrop { get; }
    public double BackdropOpacity { get; }
}

internal static class BootstrapModalRenderLogic
{
    public static BootstrapModalVisualState Resolve(BootstrapTheme theme)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        return new BootstrapModalVisualState(
            theme.Colors.Surface,
            theme.Colors.Text,
            theme.Colors.MutedText,
            theme.Colors.Border,
            theme.Colors.Dark,
            theme.Mode == BootstrapThemeMode.Dark ? 0.62d : 0.5d);
    }
}
