using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>
/// Creates demo-scoped themes with the Integrated Demo typography hierarchy.
/// </summary>
public static class DemoThemeFactory
{
    /// <summary>
    /// Creates a new demo theme without publishing it as the application theme.
    /// </summary>
    /// <param name="mode">The requested Light or Dark theme mode.</param>
    /// <param name="reducedMotion">Whether the returned theme enables reduced motion.</param>
    /// <returns>A new theme using the Integrated Demo typography tokens.</returns>
    public static BootstrapTheme Create(
        BootstrapThemeMode mode,
        bool reducedMotion = false)
    {
        return Create(mode, DemoTypographyPreset.Base16Px, reducedMotion);
    }

    internal static BootstrapTheme Create(
        BootstrapThemeMode mode,
        DemoTypographyPreset typographyPreset,
        bool reducedMotion = false)
    {
        return Create(
            mode,
            DemoTypography.CreateThemeTypography(typographyPreset),
            reducedMotion);
    }

    internal static BootstrapTheme Create(
        BootstrapThemeMode mode,
        BootstrapThemeTypography typography,
        bool reducedMotion = false)
    {
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            typography,
            reducedMotion);
    }
}
