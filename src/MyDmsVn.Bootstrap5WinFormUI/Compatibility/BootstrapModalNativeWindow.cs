using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Compatibility;

internal static class BootstrapModalNativeWindow
{
    private const uint GwOwner = 4;

    public static IntPtr GetOwner(IntPtr windowHandle)
    {
        return windowHandle == IntPtr.Zero || !IsWindow(windowHandle) ? IntPtr.Zero : GetWindow(windowHandle, GwOwner);
    }

    public static bool IsUsable(IntPtr windowHandle) => windowHandle != IntPtr.Zero && IsWindow(windowHandle);

    public static void ApplyResolvedBounds(Form window, Rectangle bounds)
    {
        if (window is null) throw new ArgumentNullException(nameof(window));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));

        var minimum = window.MinimumSize;
        var conflictsWithMinimum = minimum.Width > bounds.Width || minimum.Height > bounds.Height;
        if (conflictsWithMinimum && window.IsHandleCreated && BootstrapOverlayWindowBounds.TrySetBounds(window.Handle, bounds))
            return;

        window.Bounds = bounds;
        if (window.Bounds != bounds && window.IsHandleCreated)
            BootstrapOverlayWindowBounds.TrySetBounds(window.Handle, bounds);
    }

    public static bool TryGetBounds(IntPtr windowHandle, out Rectangle bounds)
    {
        bounds = Rectangle.Empty;
        if (!IsUsable(windowHandle) || !GetWindowRect(windowHandle, out var rectangle)) return false;
        bounds = Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRectangle rectangle);
}
