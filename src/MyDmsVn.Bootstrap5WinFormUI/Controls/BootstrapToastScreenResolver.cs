using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapToastScreenInfo
{
    public BootstrapToastScreenInfo(string deviceName, Rectangle workingArea, int dpi)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("A screen device name is required.", nameof(deviceName));
        }

        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workingArea), workingArea, "The screen working area must have positive dimensions.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "Screen DPI must be greater than zero.");
        }

        DeviceName = deviceName;
        WorkingArea = workingArea;
        Dpi = dpi;
    }

    public string DeviceName { get; }

    public Rectangle WorkingArea { get; }

    public int Dpi { get; }
}

internal interface IBootstrapToastScreenResolver
{
    BootstrapToastScreenInfo Resolve(Control? relativeTo);

    IReadOnlyList<BootstrapToastScreenInfo> GetCurrentScreens();
}

internal interface IBootstrapToastMonitorDpiProvider
{
    int GetDpi(Screen screen);
}

internal sealed class BootstrapToastScreenResolver : IBootstrapToastScreenResolver
{
    private readonly IBootstrapToastMonitorDpiProvider _monitorDpiProvider;

    public BootstrapToastScreenResolver()
        : this(new BootstrapToastMonitorDpiProvider())
    {
    }

    internal BootstrapToastScreenResolver(IBootstrapToastMonitorDpiProvider monitorDpiProvider)
    {
        _monitorDpiProvider = monitorDpiProvider ?? throw new ArgumentNullException(nameof(monitorDpiProvider));
    }

    public BootstrapToastScreenInfo Resolve(Control? relativeTo)
    {
        if (IsLive(relativeTo))
        {
            return CreateInfo(Screen.FromControl(relativeTo!), ResolveControlDpi(relativeTo!));
        }

        var activeForm = Form.ActiveForm;
        if (IsLive(activeForm))
        {
            return CreateInfo(Screen.FromControl(activeForm!), ResolveControlDpi(activeForm!));
        }

        var primary = Screen.PrimaryScreen;
        if (primary is null)
        {
            throw new InvalidOperationException("No primary screen is available for Toast placement.");
        }

        return CreateInfo(primary, _monitorDpiProvider.GetDpi(primary));
    }

    public IReadOnlyList<BootstrapToastScreenInfo> GetCurrentScreens()
    {
        var screens = Screen.AllScreens;
        var result = new BootstrapToastScreenInfo[screens.Length];
        for (var index = 0; index < screens.Length; index++)
        {
            result[index] = CreateInfo(screens[index], _monitorDpiProvider.GetDpi(screens[index]));
        }

        return result;
    }

    private BootstrapToastScreenInfo CreateInfo(Screen screen, int dpi)
    {
        return new BootstrapToastScreenInfo(
            screen.DeviceName,
            screen.WorkingArea,
            dpi > 0 ? dpi : DpiScaler.DefaultDpi);
    }

    private static bool IsLive(Control? control)
    {
        return control is not null && !control.IsDisposed && control.IsHandleCreated;
    }

    private static int ResolveControlDpi(Control control)
    {
        return control.DeviceDpi > 0 ? control.DeviceDpi : DpiScaler.DefaultDpi;
    }
}

internal sealed class BootstrapToastMonitorDpiProvider : IBootstrapToastMonitorDpiProvider
{
    private const int MonitorDefaultToNearest = 2;

    public int GetDpi(Screen screen)
    {
        if (screen is null)
        {
            throw new ArgumentNullException(nameof(screen));
        }

        try
        {
            var bounds = screen.Bounds;
            var point = new NativePoint(
                bounds.Left + (bounds.Width / 2),
                bounds.Top + (bounds.Height / 2));
            var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0)
            {
                return (int)dpiX;
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (BadImageFormatException)
        {
        }

        return DpiScaler.DefaultDpi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
}
