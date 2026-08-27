using System;
using System.Windows.Forms;

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
        Application.Run(new MainForm());
    }
}
