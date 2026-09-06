using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewReviewRound10RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);
    }

    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null) BootstrapThemeManager.CurrentTheme = _originalTheme;
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void SelectedTileSecondaryLineUsesSelectedForegroundWithoutSubItemOverride(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var form = new Form { ClientSize = new Size(380, 110), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 360, 80),
            HideSelection = false,
            TileSize = new Size(340, 72),
            View = View.Tile
        };
        list.Columns.Add("Secondary");
        var item = list.Items.Add(new ListViewItem(new[] { string.Empty, "Selected secondary" })
        {
            UseItemStyleForSubItems = false
        });
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        list.Focus();
        item.Selected = true;
        Application.DoEvents();

        using var bitmap = new Bitmap(360, 80);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(list.BackColor);
            list.DrawItemForTest(new DrawListViewItemEventArgs(
                graphics, item, new Rectangle(0, 0, 340, 72), item.Index, ListViewItemStates.Selected));
        }

        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        var selectedForeground = GetContrastRatio(colors.Primary, colors.Light) >=
                                 GetContrastRatio(colors.Primary, colors.Dark)
            ? colors.Light
            : colors.Dark;
        var secondaryRegion = new Rectangle(0, 36, bitmap.Width, bitmap.Height - 36);
        Assert.That(CountPixelsNear(bitmap, selectedForeground, 24, secondaryRegion), Is.GreaterThan(4),
            $"{mode}: selected Tile secondary text must use the selected palette foreground.");
    }

    [Test]
    public void ViewportChangeAcquiresHoverWhenStationaryPointerStartedOverBlankSpace()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var form = new Form { ClientSize = new Size(420, 260), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 380, 213),
            FullRowSelect = true,
            HoverHighlight = true,
            View = View.Details
        };
        list.Columns.Add("Name", 330);
        for (var index = 0; index < 30; index++) list.Items.Add($"Item {index}");
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        list.EnsureVisible(list.Items.Count - 1);
        Application.DoEvents();

        var lastBounds = list.Items[list.Items.Count - 1].Bounds;
        var pointer = new Point(lastBounds.Left + 250, lastBounds.Bottom + 2);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.ClientRectangle.Contains(pointer), Is.True, "The initial pointer must remain inside the client.");
            Assert.That(list.HitTest(pointer).Item, Is.Null, "The initial pointer must be over a blank slot.");
        }));

        var previousCursor = Cursor.Position;
        try
        {
            Cursor.Position = list.PointToScreen(pointer);
            Application.DoEvents();
            SendMessage(list.Handle, 0x0115, IntPtr.Zero, IntPtr.Zero);
            Application.DoEvents();
            var currentHit = list.HitTest(pointer).Item;
            Assert.That(currentHit, Is.Not.Null, "Scrolling upward must move a row under the stationary pointer.");
            list.Update();

            using var bitmap = CaptureWindowClient(list.Handle);
            var currentBounds = currentHit!.Bounds;
            var hoverColor = BootstrapThemeManager.CurrentTheme.Colors.Hover;
            Assert.That(bitmap.GetPixel(currentBounds.Left + 250, currentBounds.Top + (currentBounds.Height / 2)).ToArgb(),
                Is.EqualTo(hoverColor.ToArgb()), "A viewport change must acquire hover without a preceding hovered item.");
        }
        finally
        {
            Cursor.Position = previousCursor;
        }
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

    private static double GetContrastRatio(Color first, Color second)
    {
        var firstLuminance = GetRelativeLuminance(first);
        var secondLuminance = GetRelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05d) /
               (Math.Min(firstLuminance, secondLuminance) + 0.05d);
    }

    private static double GetRelativeLuminance(Color color) =>
        (0.2126d * GetLinearComponent(color.R / 255d)) +
        (0.7152d * GetLinearComponent(color.G / 255d)) +
        (0.0722d * GetLinearComponent(color.B / 255d));

    private static double GetLinearComponent(double component) =>
        component <= 0.04045d ? component / 12.92d : Math.Pow((component + 0.055d) / 1.055d, 2.4d);

    private static Bitmap CaptureWindowClient(IntPtr window)
    {
        Assert.That(GetClientRect(window, out var bounds), Is.True);
        var bitmap = new Bitmap(Math.Max(1, bounds.Right), Math.Max(1, bounds.Bottom));
        var source = GetDC(window);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            var destination = graphics.GetHdc();
            try
            {
                Assert.That(BitBlt(destination, 0, 0, bitmap.Width, bitmap.Height, source, 0, 0, 0x00CC0020), Is.True);
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
