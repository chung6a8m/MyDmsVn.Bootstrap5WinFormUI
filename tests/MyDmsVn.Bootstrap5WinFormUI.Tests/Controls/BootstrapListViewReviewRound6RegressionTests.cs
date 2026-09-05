using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewReviewRound6RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void DrawSubItemForTest(DrawListViewSubItemEventArgs e) => OnDrawSubItem(e);

        public void RecreateHandleForTest() => RecreateHandle();

    }

    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null) BootstrapThemeManager.CurrentTheme = _originalTheme;
    }

    [Test]
    public void VirtualCellPaintingDoesNotRetrieveTheItemAgainForSelection()
    {
        using var list = new TestBootstrapListView
        {
            ClientSize = new Size(500, 120),
            FullRowSelect = true,
            View = View.Details,
            VirtualMode = true
        };
        list.Columns.Add("First", 220);
        list.Columns.Add("Second", 130);
        list.Columns.Add("Third", 110);
        var retrievals = 0;
        list.RetrieveVirtualItem += (_, e) =>
        {
            retrievals++;
            e.Item = new ListViewItem(new[] { $"Row {e.ItemIndex}", "Two", "Three" });
        };
        list.VirtualListSize = 1;
        _ = list.Handle;
        var item = list.Items[0];
        retrievals = 0;
        using var bitmap = new Bitmap(500, 60);
        using var graphics = Graphics.FromImage(bitmap);

        for (var column = 0; column < 3; column++)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(column == 0 ? 0 : column == 1 ? 220 : 350, 0, list.Columns[column].Width, 24),
                item,
                item.SubItems[column],
                0,
                column,
                list.Columns[column],
                ListViewItemStates.Default));
        }

        Assert.That(retrievals, Is.EqualTo(3),
            "Each manual cell draw queries unavoidable native geometry; selection must not add another retrieval per cell.");
    }

    [Test]
    public void AlternatingSubscriberPaintSizesReuseOneGrowingBackupSurface()
    {
        using var list = new TestBootstrapListView { ClientSize = new Size(500, 100), View = View.Details };
        list.Columns.Add("Wide", 300);
        list.Columns.Add("Narrow", 80);
        var item = list.Items.Add(new ListViewItem(new[] { "Wide", "Narrow" }));
        _ = list.Handle;
        list.DrawSubItem += (_, _) => { };
        using var bitmap = new Bitmap(500, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var buffers = new HashSet<Bitmap>();

        for (var iteration = 0; iteration < 8; iteration++)
        {
            for (var column = 0; column < 2; column++)
            {
                var bounds = new Rectangle(column == 0 ? 0 : 300, 0, list.Columns[column].Width, 24);
                list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                    graphics, bounds, item, item.SubItems[column], 0, column, list.Columns[column], ListViewItemStates.Default));
                buffers.Add(GetBackupBuffer(list)!);
            }
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(buffers.Count, Is.EqualTo(1));
            Assert.That(buffers.Single().Width, Is.GreaterThanOrEqualTo(300));
            Assert.That(buffers.Single().Height, Is.GreaterThanOrEqualTo(24));
            Assert.That(buffers.Single().Width, Is.LessThan(list.ClientSize.Width));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void NoOpSubscriberDoesNotChangeRenderedTextPixels(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var direct = CreateTextList();
        using var observed = CreateTextList();
        observed.DrawItem += (_, _) => { };

        using var directBitmap = RenderListItem(direct);
        using var observedBitmap = RenderListItem(observed);

        Assert.That(BitmapsEqual(directBitmap, observedBitmap), Is.True);
    }

    [Test]
    public void DrawDefaultTrueRestoresTheNativeCustomDrawTarget()
    {
        using var list = new TestBootstrapListView { ClientSize = new Size(320, 80), View = View.List };
        list.Items.Add("Native rollback");
        _ = list.Handle;
        var invocations = 0;
        list.DrawItem += (_, e) =>
        {
            invocations++;
            e.DrawDefault = true;
        };
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();
        var brush = CreateSolidBrush(ToColorRef(Color.Magenta));
        var memory = IntPtr.Zero;
        try
        {
            var bounds = new NativeRectangle { Right = 240, Bottom = 30 };
            FillRect(hdc, ref bounds, brush);
            var native = new NativeListViewCustomDraw
            {
                CustomDraw = new NativeCustomDraw
                {
                    Header = new NativeNotifyHeader { WindowFrom = list.Handle, Code = -12 },
                    DrawStage = 0x00010001,
                    DeviceContext = hdc,
                    Rectangle = bounds
                }
            };
            memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeListViewCustomDraw)));
            Marshal.StructureToPtr(native, memory, false);

            SendMessage(list.Handle, 0x204E, IntPtr.Zero, memory);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(invocations, Is.EqualTo(1));
                Assert.That(GetPixel(hdc, 5, 5), Is.EqualTo(ToColorRef(Color.Magenta)));
            }));
        }
        finally
        {
            if (memory != IntPtr.Zero) Marshal.FreeHGlobal(memory);
            DeleteObject(brush);
            graphics.ReleaseHdc(hdc);
        }
    }

    [Test]
    public void CallerStateImagesRemainStableAcrossCheckboxVisibilityAndHandleTransitions()
    {
        using var stateImages = CreateStateImages();
        using var itemImages = new ImageList { ImageSize = new Size(20, 20), ColorDepth = ColorDepth.Depth32Bit };
        itemImages.Images.Add(CreateSolidBitmap(20, Color.Blue));
        using var form = new Form { ClientSize = new Size(400, 180), ShowInTaskbar = false };
        using var tabs = new TabControl { Dock = DockStyle.Fill };
        var firstPage = new TabPage("List");
        var secondPage = new TabPage("Other");
        using var list = new TestBootstrapListView
        {
            Dock = DockStyle.Fill,
            CheckBoxes = false,
            SmallImageList = itemImages,
            StateImageList = stateImages,
            View = View.Details
        };
        list.Columns.Add("Name", 300);
        var item = list.Items.Add("Custom state", 0);
        item.StateImageIndex = 1;
        firstPage.Controls.Add(list);
        tabs.TabPages.Add(firstPage);
        tabs.TabPages.Add(secondPage);
        form.Controls.Add(tabs);
        form.Show();
        Application.DoEvents();
        var initial = RenderStateSlot(list, item, Color.Lime);

        list.CheckBoxes = true;
        list.CheckBoxes = false;
        tabs.SelectedTab = secondPage;
        Application.DoEvents();
        tabs.SelectedTab = firstPage;
        list.Visible = false;
        list.Visible = true;
        list.RecreateHandleForTest();
        Application.DoEvents();
        var afterTransitions = RenderStateSlot(list, item, Color.Lime);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.StateImageList, Is.SameAs(stateImages));
            Assert.That(item.StateImageIndex, Is.EqualTo(1));
            Assert.That(item.Checked, Is.True);
            Assert.That(initial, Is.GreaterThan(0));
            Assert.That(afterTransitions, Is.EqualTo(initial));
        }));
    }

    [Test]
    public void CheckboxFallbackRemainsStableWithoutCallerStateImages()
    {
        using var itemImages = new ImageList { ImageSize = new Size(20, 20), ColorDepth = ColorDepth.Depth32Bit };
        itemImages.Images.Add(CreateSolidBitmap(20, Color.Blue));
        using var list = new TestBootstrapListView
        {
            ClientSize = new Size(340, 100),
            CheckBoxes = true,
            SmallImageList = itemImages,
            View = View.Details
        };
        list.Columns.Add("Name", 280);
        var item = list.Items.Add(string.Empty, 0);
        item.Checked = true;
        item.ForeColor = Color.Red;
        _ = list.Handle;
        var initial = RenderStateSlot(list, item, Color.Red);

        list.CheckBoxes = false;
        list.CheckBoxes = true;
        list.RecreateHandleForTest();
        var afterTransitions = RenderStateSlot(list, item, Color.Red);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.StateImageList, Is.Null);
            Assert.That(item.Checked, Is.True);
            Assert.That(initial, Is.GreaterThan(0));
            Assert.That(afterTransitions, Is.EqualTo(initial));
        }));
    }

    [Test]
    public void DemoSeparatesCheckboxAndCallerStateImageFixtures()
    {
        using var demo = new ListViewDemoForm();
        var tabs = demo.Controls.OfType<TabControl>().Single();
        var details = tabs.TabPages[0].Controls.OfType<BootstrapListView>().Single();
        var views = tabs.TabPages[1].Controls.OfType<BootstrapListView>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(details.CheckBoxes, Is.True);
            Assert.That(details.StateImageList, Is.Null);
            Assert.That(views.CheckBoxes, Is.False);
            Assert.That(views.StateImageList, Is.Not.Null);
            Assert.That(views.Items.Cast<ListViewItem>().All(item => item.StateImageIndex >= 0), Is.True);
        }));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void RealHeaderPostPaintThemesFillerAcrossModesAndThemeSwitch(bool virtualMode, bool mirrored)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        using var form = new Form { ClientSize = new Size(480, 180), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 440, 140),
            RightToLeft = mirrored ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = mirrored,
            ShowGroups = !virtualMode,
            View = View.Details,
            VirtualMode = virtualMode
        };
        list.Columns.Add("First", 120);
        if (virtualMode)
        {
            list.Columns.Add("Second", 100);
            list.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem(new[] { $"Virtual {e.ItemIndex}", "Ready" });
            list.VirtualListSize = 3;
        }
        else
        {
            var group = list.Groups.Add("group", "Group");
            list.Items.Add(new ListViewItem("Grouped", group));
        }
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();

        AssertHeaderFillerColor(list, BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary, mirrored);
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        list.Update();
        Application.DoEvents();
        AssertHeaderFillerColor(list, BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary, mirrored);
    }

    [Test]
    public void GroupHeaderCustomDrawUsesReadableThemeColorsAfterRuntimeSwitch()
    {
        using var form = new Form { ClientSize = new Size(440, 280), ShowInTaskbar = false };
        using var list = CreateGroupedList();
        form.Controls.Add(list);
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        form.Show();
        Application.DoEvents();
        AssertGroupHeaderCustomDrawColors(list);

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        list.Update();
        Application.DoEvents();
        AssertGroupHeaderCustomDrawColors(list);
    }

    [Test]
    public void FirstListToTileTransitionMatchesSubsequentNativeGeometry()
    {
        using var images = new ImageList { ImageSize = new Size(32, 32), ColorDepth = ColorDepth.Depth32Bit };
        images.Images.Add(CreateSolidBitmap(32, Color.Blue));
        using var form = new Form { ClientSize = new Size(800, 500), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 760, 440),
            LargeImageList = images,
            SmallImageList = images,
            TileSize = new Size(360, 72),
            View = View.List
        };
        for (var index = 0; index < 16; index++)
            list.Items.Add(new ListViewItem(new[] { $"Item {index}", $"Secondary {index}" }, 0));
        var observed = new Dictionary<int, Rectangle>();
        list.DrawItem += (_, e) =>
        {
            if (!observed.ContainsKey(e.ItemIndex)) observed.Add(e.ItemIndex, e.Bounds);
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();

        observed.Clear();
        list.View = View.Tile;
        list.Update();
        Application.DoEvents();
        var first = CaptureObservedBounds(list, observed);
        AssertNonOverlappingTileGeometry(first);

        list.View = View.List;
        Application.DoEvents();
        observed.Clear();
        list.View = View.Tile;
        list.Update();
        Application.DoEvents();
        var second = CaptureObservedBounds(list, observed);

        Assert.That(first, Is.EqualTo(second));
        AssertNonOverlappingTileGeometry(second);
    }

    private static TestBootstrapListView CreateTextList()
    {
        var list = new TestBootstrapListView { ClientSize = new Size(320, 80), View = View.List };
        list.Items.Add("Text fidelity observer path");
        _ = list.Handle;
        return list;
    }

    private static TestBootstrapListView CreateGroupedList()
    {
        var list = new TestBootstrapListView
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
        return list;
    }

    private static Bitmap RenderListItem(TestBootstrapListView list)
    {
        var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics, list.Items[0], new Rectangle(0, 0, 280, 28), 0, ListViewItemStates.Default));
        return bitmap;
    }

    private static int RenderStateSlot(TestBootstrapListView list, ListViewItem item, Color expected)
    {
        using var bitmap = new Bitmap(Math.Max(1, list.ClientSize.Width), Math.Max(1, list.ClientSize.Height));
        using var graphics = Graphics.FromImage(bitmap);
        list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
            graphics,
            new Rectangle(0, item.Bounds.Top, list.Columns[0].Width, item.Bounds.Height),
            item,
            item.SubItems[0],
            item.Index,
            0,
            list.Columns[0],
            ListViewItemStates.Default));
        return CountPixelsNear(bitmap, expected, 8);
    }

    private static ImageList CreateStateImages()
    {
        var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        list.Images.Add(CreateSolidBitmap(16, Color.Red));
        list.Images.Add(CreateSolidBitmap(16, Color.Lime));
        return list;
    }

    private static Bitmap CreateSolidBitmap(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static Bitmap? GetBackupBuffer(BootstrapListView list) =>
        (Bitmap?)typeof(BootstrapListView)
            .GetField("_ownerDrawBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(list);

    private static bool BitmapsEqual(Bitmap first, Bitmap second)
    {
        for (var y = 0; y < first.Height; y++)
        for (var x = 0; x < first.Width; x++)
            if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb()) return false;
        return true;
    }

    private static void AssertHeaderFillerColor(BootstrapListView list, Color expected, bool mirrored)
    {
        var header = SendMessage(list.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
        using var bitmap = CaptureWindowClient(header);
        var x = mirrored ? 8 : bitmap.Width - 8;
        Assert.That(bitmap.GetPixel(x, 8).ToArgb(), Is.EqualTo(expected.ToArgb()));
    }

    private static void AssertGroupHeaderCustomDrawColors(TestBootstrapListView list)
    {
        using var bitmap = new Bitmap(120, 24);
        using var graphics = Graphics.FromImage(bitmap);
        var hdc = graphics.GetHdc();
        var native = new NativeListViewCustomDraw
        {
            CustomDraw = new NativeCustomDraw
            {
                Header = new NativeNotifyHeader { WindowFrom = list.Handle, Code = -12 },
                DrawStage = 0x00010001,
                DeviceContext = hdc,
                Rectangle = new NativeRectangle { Right = 120, Bottom = 24 }
            },
            ItemType = 1
        };
        var memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeListViewCustomDraw)));
        try
        {
            Marshal.StructureToPtr(native, memory, false);
            SendMessage(list.Handle, 0x204E, IntPtr.Zero, memory);
            native = Marshal.PtrToStructure<NativeListViewCustomDraw>(memory);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
            graphics.ReleaseHdc(hdc);
        }

        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.TextColor, Is.EqualTo(ToColorRef(colors.Text)));
            Assert.That(native.FaceColor, Is.EqualTo(ToColorRef(colors.Border)));
            Assert.That(ContrastRatio(colors.Text, colors.Surface), Is.GreaterThanOrEqualTo(4.5));
        }));
    }

    private static uint ToColorRef(Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

    private static double ContrastRatio(Color first, Color second)
    {
        var lighter = Math.Max(RelativeLuminance(first), RelativeLuminance(second));
        var darker = Math.Min(RelativeLuminance(first), RelativeLuminance(second));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte component)
    {
        var value = component / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static Rectangle[] CaptureObservedBounds(ListView list, IReadOnlyDictionary<int, Rectangle> observed)
    {
        Assert.That(observed.Count, Is.EqualTo(list.Items.Count));
        return Enumerable.Range(0, list.Items.Count).Select(index => observed[index]).ToArray();
    }

    private static void AssertNonOverlappingTileGeometry(Rectangle[] bounds)
    {
        for (var index = 0; index < bounds.Length; index++)
        {
            for (var other = index + 1; other < bounds.Length; other++)
                Assert.That(Rectangle.Intersect(bounds[index], bounds[other]).IsEmpty, Is.True);
        }
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

    private static int CountPixelsNear(Bitmap bitmap, Color expected, int tolerance, Rectangle? region = null)
    {
        var bounds = Rectangle.Intersect(region ?? new Rectangle(Point.Empty, bitmap.Size), new Rectangle(Point.Empty, bitmap.Size));
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeNotifyHeader
    {
        internal IntPtr WindowFrom;
        internal UIntPtr IdFrom;
        internal int Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCustomDraw
    {
        internal NativeNotifyHeader Header;
        internal uint DrawStage;
        internal IntPtr DeviceContext;
        internal NativeRectangle Rectangle;
        internal UIntPtr ItemSpec;
        internal uint ItemState;
        internal IntPtr ItemParameter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeListViewCustomDraw
    {
        internal NativeCustomDraw CustomDraw;
        internal uint TextColor;
        internal uint TextBackgroundColor;
        internal int SubItem;
        internal uint ItemType;
        internal uint FaceColor;
        internal int IconEffect;
        internal int IconPhase;
        internal int PartId;
        internal int StateId;
        internal NativeRectangle TextRectangle;
        internal uint Align;
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

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr deviceContext, ref NativeRectangle rectangle, IntPtr brush);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint color);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr deviceContext, int x, int y);
}
