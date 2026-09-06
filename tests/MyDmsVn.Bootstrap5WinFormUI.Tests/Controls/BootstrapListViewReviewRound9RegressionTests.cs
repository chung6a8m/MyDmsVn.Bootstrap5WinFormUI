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
public sealed class BootstrapListViewReviewRound9RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void RaiseMouseMove(Point location) =>
            OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));
    }

    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null) BootstrapThemeManager.CurrentTheme = _originalTheme;
    }

    [TestCase(RightToLeft.No)]
    [TestCase(RightToLeft.Yes)]
    public void SmallIconLabelKeepsNativeLeadingEdgeWithoutStructuralMirroring(RightToLeft rightToLeft)
    {
        using var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        images.Images.Add(CreateSolidBitmap(Color.Black));
        using var form = new Form { ClientSize = new Size(760, 150), ShowInTaskbar = false };
        using var native = CreateSmallIconList<ListView>(new Rectangle(0, 0, 360, 120), images, rightToLeft);
        using var bootstrap = CreateSmallIconList<BootstrapListView>(new Rectangle(380, 0, 360, 120), images, rightToLeft);
        form.Controls.Add(native);
        form.Controls.Add(bootstrap);
        form.Show();
        Application.DoEvents();
        native.Update();
        bootstrap.Update();

        using var nativeBitmap = CaptureWindowClient(native.Handle);
        using var bootstrapBitmap = CaptureWindowClient(bootstrap.Handle);
        var nativeInk = FindRedPixelBounds(nativeBitmap);
        var bootstrapInk = FindRedPixelBounds(bootstrapBitmap);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeInk, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(bootstrapInk, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(Math.Abs(bootstrapInk.Left - nativeInk.Left), Is.LessThanOrEqualTo(2),
                "SmallIcon text must start at the native label leading edge.");
        }));
    }

    [Test]
    public void SmallIconFallbackLabelStartsAtLeadingEdge()
    {
        using var list = new TestBootstrapListView { ClientSize = new Size(280, 80), View = View.SmallIcon };
        using var bitmap = new Bitmap(280, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        var item = new ListViewItem("Fallback") { ForeColor = Color.Red };

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            new Rectangle(0, 0, 240, 30),
            0,
            ListViewItemStates.Default));

        var ink = FindRedPixelBounds(bitmap);
        Assert.That(ink.Left, Is.LessThan(20), "SmallIcon fallback text must use native-style left alignment.");
    }

    [Test]
    public void ScrollingUnderStationaryPointerMovesHoverToCurrentHitRow()
    {
        using var form = new Form { ClientSize = new Size(420, 240), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 380, 200),
            FullRowSelect = true,
            HoverHighlight = true,
            View = View.Details
        };
        list.Columns.Add("Name", 330);
        for (var index = 0; index < 40; index++) list.Items.Add($"Item {index}");
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();

        var hoveredBeforeScroll = list.Items[2];
        var initialBounds = hoveredBeforeScroll.Bounds;
        var pointer = new Point(initialBounds.Left + 250, initialBounds.Top + (initialBounds.Height / 2));
        var previousCursor = Cursor.Position;
        try
        {
            Cursor.Position = list.PointToScreen(pointer);
            Application.DoEvents();
            list.RaiseMouseMove(pointer);
            list.Update();

            SendMessage(list.Handle, 0x0115, (IntPtr)1, IntPtr.Zero);
            Application.DoEvents();
            var currentHit = list.HitTest(pointer).Item;
            Assert.That(currentHit, Is.Not.Null);
            Assert.That(currentHit!.Index, Is.Not.EqualTo(hoveredBeforeScroll.Index),
                "The native viewport must move while the pointer remains stationary.");
            list.Invalidate();
            list.Update();
            Application.DoEvents();

            using var bitmap = CaptureWindowClient(list.Handle);
            var currentBounds = currentHit.Bounds;
            var oldBounds = hoveredBeforeScroll.Bounds;
            var hoverColor = BootstrapThemeManager.CurrentTheme.Colors.Hover;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(bitmap.GetPixel(currentBounds.Left + 250, currentBounds.Top + (currentBounds.Height / 2)).ToArgb(),
                    Is.EqualTo(hoverColor.ToArgb()), "The row now under the pointer must receive hover presentation.");
                Assert.That(bitmap.GetPixel(oldBounds.Left + 250, oldBounds.Top + (oldBounds.Height / 2)).ToArgb(),
                    Is.Not.EqualTo(hoverColor.ToArgb()), "The previously hovered data row must lose hover presentation.");
            }));
        }
        finally
        {
            Cursor.Position = previousCursor;
        }
    }

    private static T CreateSmallIconList<T>(Rectangle bounds, ImageList images, RightToLeft rightToLeft)
        where T : ListView, new()
    {
        var list = new T
        {
            Bounds = bounds,
            ForeColor = Color.Red,
            RightToLeft = rightToLeft,
            RightToLeftLayout = false,
            SmallImageList = images,
            View = View.SmallIcon
        };
        list.Items.Add(new ListViewItem("i", 0) { ForeColor = Color.Red });
        return list;
    }

    private static Bitmap CreateSolidBitmap(Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static Rectangle FindRedPixelBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (pixel.R < 128 || pixel.R <= pixel.G + 64 || pixel.R <= pixel.B + 64) continue;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

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
