using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public static class DemoThemeFactory
{
    public static BootstrapTheme Create(
        BootstrapThemeMode mode,
        bool reducedMotion = false)
    {
        return new BootstrapTheme(
            mode,
            BootstrapThemeColors.CreateDefault(mode),
            BootstrapThemeMetrics.Default,
            DemoTypography.CreateThemeTypography(),
            reducedMotion);
    }
}
