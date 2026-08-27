using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Contains semantic and application-surface colors for a theme.
/// </summary>
public sealed class BootstrapThemeColors
{
    /// <summary>
    /// Initializes a complete color token set.
    /// </summary>
    public BootstrapThemeColors(
        Color primary,
        Color secondary,
        Color success,
        Color danger,
        Color warning,
        Color info,
        Color light,
        Color dark,
        Color body,
        Color surface,
        Color surfaceSecondary,
        Color border,
        Color text,
        Color mutedText,
        Color disabled,
        Color focus,
        Color hover,
        Color active)
    {
        Primary = primary;
        Secondary = secondary;
        Success = success;
        Danger = danger;
        Warning = warning;
        Info = info;
        Light = light;
        Dark = dark;
        Body = body;
        Surface = surface;
        SurfaceSecondary = surfaceSecondary;
        Border = border;
        Text = text;
        MutedText = mutedText;
        Disabled = disabled;
        Focus = focus;
        Hover = hover;
        Active = active;
    }

    /// <summary>
    /// Creates the framework's default palette for the requested mode.
    /// </summary>
    public static BootstrapThemeColors CreateDefault(BootstrapThemeMode mode)
    {
        switch (mode)
        {
            case BootstrapThemeMode.Light:
                return CreateLight();
            case BootstrapThemeMode.Dark:
                return CreateDark();
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported theme mode.");
        }
    }

    /// <summary>Gets the primary semantic color.</summary>
    public Color Primary { get; }
    /// <summary>Gets the secondary semantic color.</summary>
    public Color Secondary { get; }
    /// <summary>Gets the success semantic color.</summary>
    public Color Success { get; }
    /// <summary>Gets the danger semantic color.</summary>
    public Color Danger { get; }
    /// <summary>Gets the warning semantic color.</summary>
    public Color Warning { get; }
    /// <summary>Gets the informational semantic color.</summary>
    public Color Info { get; }
    /// <summary>Gets the light semantic color.</summary>
    public Color Light { get; }
    /// <summary>Gets the dark semantic color.</summary>
    public Color Dark { get; }
    /// <summary>Gets the application body background.</summary>
    public Color Body { get; }
    /// <summary>Gets the primary surface background.</summary>
    public Color Surface { get; }
    /// <summary>Gets the secondary surface background.</summary>
    public Color SurfaceSecondary { get; }
    /// <summary>Gets the neutral border color.</summary>
    public Color Border { get; }
    /// <summary>Gets the primary text color.</summary>
    public Color Text { get; }
    /// <summary>Gets the secondary text color.</summary>
    public Color MutedText { get; }
    /// <summary>Gets the disabled-state basis color.</summary>
    public Color Disabled { get; }
    /// <summary>Gets the focus indicator color.</summary>
    public Color Focus { get; }
    /// <summary>Gets the hover-state surface color.</summary>
    public Color Hover { get; }
    /// <summary>Gets the active/pressed surface color.</summary>
    public Color Active { get; }

    private static BootstrapThemeColors CreateLight()
    {
        return new BootstrapThemeColors(
            Color.FromArgb(0x0D, 0x6E, 0xFD),
            Color.FromArgb(0x6C, 0x75, 0x7D),
            Color.FromArgb(0x19, 0x87, 0x54),
            Color.FromArgb(0xDC, 0x35, 0x45),
            Color.FromArgb(0xFF, 0xC1, 0x07),
            Color.FromArgb(0x0D, 0xCA, 0xF0),
            Color.FromArgb(0xF8, 0xF9, 0xFA),
            Color.FromArgb(0x21, 0x25, 0x29),
            Color.White,
            Color.White,
            Color.FromArgb(0xF8, 0xF9, 0xFA),
            Color.FromArgb(0xDE, 0xE2, 0xE6),
            Color.FromArgb(0x21, 0x25, 0x29),
            Color.FromArgb(0x6C, 0x75, 0x7D),
            Color.FromArgb(0xAD, 0xB5, 0xBD),
            Color.FromArgb(0x86, 0xB7, 0xFE),
            Color.FromArgb(0xE9, 0xEC, 0xEF),
            Color.FromArgb(0xDE, 0xE2, 0xE6));
    }

    private static BootstrapThemeColors CreateDark()
    {
        return new BootstrapThemeColors(
            Color.FromArgb(0x6E, 0xA8, 0xFE),
            Color.FromArgb(0xA7, 0xAC, 0xB1),
            Color.FromArgb(0x75, 0xB7, 0x98),
            Color.FromArgb(0xEA, 0x86, 0x8F),
            Color.FromArgb(0xFF, 0xDA, 0x6A),
            Color.FromArgb(0x6E, 0xDF, 0xF6),
            Color.FromArgb(0xF8, 0xF9, 0xFA),
            Color.FromArgb(0x21, 0x25, 0x29),
            Color.FromArgb(0x21, 0x25, 0x29),
            Color.FromArgb(0x2B, 0x30, 0x35),
            Color.FromArgb(0x34, 0x3A, 0x40),
            Color.FromArgb(0x49, 0x50, 0x57),
            Color.FromArgb(0xF8, 0xF9, 0xFA),
            Color.FromArgb(0xAD, 0xB5, 0xBD),
            Color.FromArgb(0x6C, 0x75, 0x7D),
            Color.FromArgb(0x6E, 0xA8, 0xFE),
            Color.FromArgb(0x34, 0x3A, 0x40),
            Color.FromArgb(0x49, 0x50, 0x57));
    }
}
