using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
#if NET8_0_OR_GREATER
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);
        Application.Run(new MainForm());
    }
}
