using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.VisualStyleHost;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var form = new Form { ClientSize = new Size(440, 280), ShowInTaskbar = false };
            using var list = new BootstrapListView
            {
                Bounds = new Rectangle(0, 0, 400, 240),
                FullRowSelect = true,
                ShowGroups = true,
                View = View.Details
            };
            list.Columns.Add("Name", 300);
            var active = list.Groups.Add("active", "Active");
            var archived = list.Groups.Add("archived", "Archived");
            list.Items.Add(new ListViewItem("Active item", active) { ForeColor = Color.Magenta });
            list.Items.Add(new ListViewItem("Archived item", archived) { ForeColor = Color.Magenta });
            form.Controls.Add(list);
            form.Show();
            Application.DoEvents();
            Verify(list, BootstrapThemeMode.Light);
            Verify(list, BootstrapThemeMode.Dark);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Verify(BootstrapListView list, BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        list.Invalidate();
        list.Update();
        Application.DoEvents();

        using var bitmap = CaptureWindowClient(list.Handle);
        var header = SendMessage(list.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
        if (!GetClientRect(header, out var headerBounds)) throw new InvalidOperationException("Cannot read the native header bounds.");
        var first = list.Items[0].Bounds;
        var second = list.Items[1].Bounds;
        var firstHeader = Rectangle.FromLTRB(0, headerBounds.Bottom, bitmap.Width, first.Top + 1);
        var secondHeader = Rectangle.FromLTRB(0, first.Bottom, bitmap.Width, second.Top + 1);
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        Require(firstHeader.Height > 0 && secondHeader.Height > 0, $"{mode}: native group bands were not rendered.");
        Require(CountPixelsNear(bitmap, colors.Text, 32, firstHeader) > 4, $"{mode}: Active header is not using theme text.");
        Require(CountPixelsNear(bitmap, colors.Text, 32, secondHeader) > 4, $"{mode}: Archived header is not using theme text.");
        Require(CountPixelsNear(bitmap, colors.Border, 16, firstHeader) > 20, $"{mode}: group separator is not using the theme border.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static Bitmap CaptureWindowClient(IntPtr window)
    {
        if (!GetClientRect(window, out var bounds)) throw new InvalidOperationException("Cannot read the list client bounds.");
        var bitmap = new Bitmap(Math.Max(1, bounds.Right), Math.Max(1, bounds.Bottom));
        var source = GetDC(window);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            var destination = graphics.GetHdc();
            try
            {
                if (!BitBlt(destination, 0, 0, bitmap.Width, bitmap.Height, source, 0, 0, 0x00CC0020))
                    throw new InvalidOperationException("Cannot capture the rendered list pixels.");
            }
            finally
            {
                graphics.ReleaseHdc(destination);
            }
        }
        finally
        {
            ReleaseDC(window, source);
        }

        return bitmap;
    }

    private static int CountPixelsNear(Bitmap bitmap, Color expected, int tolerance, Rectangle region)
    {
        var bounds = Rectangle.Intersect(region, new Rectangle(Point.Empty, bitmap.Size));
        var count = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            var actual = bitmap.GetPixel(x, y);
            if (Math.Abs(actual.R - expected.R) <= tolerance &&
                Math.Abs(actual.G - expected.G) <= tolerance &&
                Math.Abs(actual.B - expected.B) <= tolerance) count++;
        }

        return count;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(
        IntPtr destination, int destinationX, int destinationY, int width, int height,
        IntPtr source, int sourceX, int sourceY, int rasterOperation);
}
