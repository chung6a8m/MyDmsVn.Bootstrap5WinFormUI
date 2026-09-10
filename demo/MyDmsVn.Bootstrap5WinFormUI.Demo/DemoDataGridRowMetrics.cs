using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal static class DemoDataGridRowMetrics
{
    public static int Calculate(Font font, BootstrapThemeMetrics metrics, int dpi)
    {
        if (font is null) throw new ArgumentNullException(nameof(font));
        if (metrics is null) throw new ArgumentNullException(nameof(metrics));

        var bootstrapEditingHeight = DpiScaler.Scale(metrics.ControlHeight + metrics.SpacingXS, dpi);
        var textHeight = (int)Math.Ceiling(font.GetHeight(dpi));
        var textHeightWithPadding = textHeight + (DpiScaler.Scale(metrics.SpacingXS, dpi) * 2);
        return Math.Max(bootstrapEditingHeight, textHeightWithPadding);
    }

    public static void Apply(DataGridView grid, BootstrapTheme theme)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (theme is null) throw new ArgumentNullException(nameof(theme));

        var dpi = grid.DeviceDpi > 0 ? grid.DeviceDpi : DpiScaler.DefaultDpi;
        var height = Calculate(grid.Font, theme.Metrics, dpi);
        grid.RowTemplate.Height = height;
        foreach (DataGridViewRow row in grid.Rows)
        {
            row.Height = height;
        }
    }
}
