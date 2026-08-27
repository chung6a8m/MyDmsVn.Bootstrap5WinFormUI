using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Represents an immutable collection of theme tokens used by the framework.
/// </summary>
public sealed class BootstrapTheme
{
    /// <summary>
    /// Initializes a complete theme.
    /// </summary>
    public BootstrapTheme(
        BootstrapThemeMode mode,
        BootstrapThemeColors colors,
        BootstrapThemeMetrics metrics,
        BootstrapThemeTypography typography,
        bool reducedMotion = false)
    {
        if (mode != BootstrapThemeMode.Light && mode != BootstrapThemeMode.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported theme mode.");
        }

        Mode = mode;
        Colors = colors ?? throw new ArgumentNullException(nameof(colors));
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        Typography = typography ?? throw new ArgumentNullException(nameof(typography));
        ReducedMotion = reducedMotion;
    }

    /// <summary>
    /// Creates a framework default theme for the requested mode.
    /// </summary>
    public static BootstrapTheme CreateDefault(BootstrapThemeMode mode, bool reducedMotion = false)
    {
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            BootstrapThemeTypography.Default,
            reducedMotion);
    }

    /// <summary>Gets the theme mode.</summary>
    public BootstrapThemeMode Mode { get; }

    /// <summary>Gets the color tokens.</summary>
    public BootstrapThemeColors Colors { get; }

    /// <summary>Gets the 100%-DPI metric tokens.</summary>
    public BootstrapThemeMetrics Metrics { get; }

    /// <summary>Gets the typography tokens.</summary>
    public BootstrapThemeTypography Typography { get; }

    /// <summary>
    /// Gets whether nonessential motion should be shortened or skipped.
    /// </summary>
    public bool ReducedMotion { get; }
}
