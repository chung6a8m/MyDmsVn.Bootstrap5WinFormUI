using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MyDmsVn.Bootstrap5WinFormUI.Compatibility;

internal static class BootstrapOverlayWindowBounds
{
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpNoSendChanging = 0x0400;

    public static IntPtr GetWindowHandle(Graphics graphics)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        var hdc = graphics.GetHdc();
        try
        {
            return WindowFromDC(hdc);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }
    }

    public static bool TrySetBounds(IntPtr windowHandle, Rectangle bounds)
    {
        if (windowHandle == IntPtr.Zero || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        if (TryGetBounds(windowHandle, out var current) && current == bounds)
        {
            return true;
        }

        return SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SwpNoZOrder | SwpNoActivate | SwpNoOwnerZOrder | SwpNoSendChanging);
    }

    public static bool TryGetBounds(IntPtr windowHandle, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (windowHandle == IntPtr.Zero || !GetWindowRect(windowHandle, out var nativeBounds))
        {
            return false;
        }

        bounds = nativeBounds.ToRectangle();
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(Left, Top, Right, Bottom);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromDC(IntPtr hdc);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRectangle bounds);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
