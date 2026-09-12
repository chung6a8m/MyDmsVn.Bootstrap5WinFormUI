using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapDropdownRenderer : BootstrapToolStripRendererBase
{
    internal static BootstrapDropdownPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled,
        bool selected)
    {
        return BootstrapToolStripRenderLogic.ResolvePalette(
            colors,
            variant,
            BootstrapToolStripSurfaceKind.DropDown,
            enabled,
            selected,
            pressed: false,
            @checked: false);
    }

    internal static BootstrapDropdownMetrics ResolveMetrics(BootstrapThemeMetrics metrics, int dpi)
    {
        return BootstrapToolStripRenderLogic.ResolveMetrics(metrics, dpi);
    }
}
