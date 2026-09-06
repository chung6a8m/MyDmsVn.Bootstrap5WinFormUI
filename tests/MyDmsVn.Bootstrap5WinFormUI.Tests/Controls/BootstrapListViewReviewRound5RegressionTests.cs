using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewReviewRound5RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void DrawSubItemForTest(DrawListViewSubItemEventArgs e) => OnDrawSubItem(e);

        public void DrawColumnHeaderForTest(DrawListViewColumnHeaderEventArgs e) => OnDrawColumnHeader(e);
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
    public void SubscriberCompositionBufferIsLocalToAbsolutePaintBounds()
    {
        using var list = new TestBootstrapListView { ClientSize = new Size(1200, 800), View = View.List };
        var item = list.Items.Add("Local buffer");
        _ = list.Handle;
        var bounds = new Rectangle(900, 600, 80, 24);
        using var bitmap = new Bitmap(1000, 650);
        using var graphics = Graphics.FromImage(bitmap);
        list.DrawItem += (_, e) => e.Graphics.FillRectangle(Brushes.Magenta, e.Bounds);

        list.DrawItemForTest(new DrawListViewItemEventArgs(graphics, item, bounds, 0, ListViewItemStates.Default));

        var buffer = GetCompositionBuffer(list);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(buffer, Is.Not.Null);
            Assert.That(buffer!.Size, Is.EqualTo(bounds.Size));
            Assert.That(bitmap.GetPixel(bounds.Left + 2, bounds.Top + 2).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
        }));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void NormalTextRenderingBypassesCompositionBitmap(BootstrapThemeMode mode)
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
        using var list = new TestBootstrapListView { ClientSize = new Size(320, 80), View = View.List };
        var item = list.Items.Add("Direct TextRenderer path");
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics, item, new Rectangle(0, 0, 260, 28), 0, ListViewItemStates.Default));

        Assert.That(GetCompositionBuffer(list), Is.Null,
            "Normal GDI text must be rendered directly to the final paint surface.");
    }

    [Test]
    public void ImageKeyKeepsNativeIndexWhenSwitchingBetweenSmallAndLargeLists()
    {
        using var small = CreateImageList(Color.Red, "small-zero", Color.Lime, "small-key");
        using var large = CreateImageList(Color.Blue, "large-zero", Color.Magenta, "large-corresponding");
        using var list = new TestBootstrapListView
        {
            ClientSize = new Size(360, 160),
            SmallImageList = small,
            LargeImageList = large,
            View = View.List
        };
        var item = list.Items.Add("Switch image");
        item.ImageKey = "small-key";
        _ = list.Handle;

        using var smallBitmap = RenderItem(list, item);
        list.View = View.LargeIcon;
        Application.DoEvents();
        using var largeBitmap = RenderItem(list, item);
        list.View = View.Tile;
        Application.DoEvents();
        using var tileBitmap = RenderItem(list, item);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountPixels(smallBitmap, Color.Lime), Is.GreaterThan(0));
            Assert.That(CountPixels(largeBitmap, Color.Magenta), Is.GreaterThan(0));
            Assert.That(CountPixels(tileBitmap, Color.Magenta), Is.GreaterThan(0));
        }));
    }

    [Test]
    public void LastDetailsHeaderThemesTrailingClientArea()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        using var list = new TestBootstrapListView { ClientSize = new Size(320, 100), View = View.Details };
        var header = list.Columns.Add("Only column", 120);
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);

        list.DrawColumnHeaderForTest(new DrawListViewColumnHeaderEventArgs(
            graphics, new Rectangle(0, 0, 120, 28), 0, header, ListViewItemStates.Default,
            list.ForeColor, list.BackColor, list.Font));

        Assert.That(bitmap.GetPixel(280, 10), Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary));
    }

    [Test]
    public void GroupFocusDoesNotPromoteTransientDrawStateToSelection()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var form = new Form { ClientSize = new Size(420, 240), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 380, 200),
            FullRowSelect = true,
            ShowGroups = true,
            View = View.Details
        };
        list.Columns.Add("Name", 300);
        var first = list.Groups.Add("first", "First");
        var second = list.Groups.Add("second", "Second");
        for (var index = 0; index < 6; index++) list.Items.Add(new ListViewItem($"First {index}", first));
        for (var index = 0; index < 6; index++) list.Items.Add(new ListViewItem($"Second {index}", second));
        var observedStates = new List<ListViewItemStates>();
        DrawListViewSubItemEventHandler observer = (_, e) => observedStates.Add(e.ItemState);
        list.DrawSubItem += observer;
        form.Controls.Add(list);
        form.Show();
        list.Focus();
        list.Invalidate();
        list.Update();
        Application.DoEvents();
        list.DrawSubItem -= observer;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(observedStates, Is.Not.Empty);
            Assert.That(observedStates.Exists(state => (state & ListViewItemStates.Selected) != 0), Is.True,
                "The handle-backed fixture must retain the grouped custom-draw state that caused the regression.");
            Assert.That(list.SelectedIndices.Count, Is.Zero);
            Assert.That(list.Items.Count, Is.EqualTo(12));
            foreach (ListViewItem item in list.Items) Assert.That(item.Selected, Is.False);
        }));

        var accent = BootstrapThemeManager.CurrentTheme.Colors.Primary;
        foreach (ListViewItem item in list.Items)
        {
            using var bitmap = new Bitmap(320, Math.Max(1, item.Bounds.Bottom + 2));
            using var graphics = Graphics.FromImage(bitmap);
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics, new Rectangle(0, item.Bounds.Top, 300, item.Bounds.Height), item, item.SubItems[0],
                item.Index, 0, list.Columns[0], ListViewItemStates.Selected));
            Assert.That(bitmap.GetPixel(2, item.Bounds.Top + 2), Is.Not.EqualTo(accent));
        }
    }

    private static Bitmap RenderItem(TestBootstrapListView list, ListViewItem item)
    {
        var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics, item, item.GetBounds(ItemBoundsPortion.Entire), item.Index, ListViewItemStates.Default));
        return bitmap;
    }

    private static ImageList CreateImageList(Color first, string firstKey, Color second, string secondKey)
    {
        var images = new ImageList { ImageSize = new Size(24, 24), ColorDepth = ColorDepth.Depth32Bit };
        images.Images.Add(firstKey, CreateBitmap(first));
        images.Images.Add(secondKey, CreateBitmap(second));
        return images;
    }

    private static Bitmap CreateBitmap(Color color)
    {
        var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static Bitmap? GetCompositionBuffer(BootstrapListView list) =>
        (Bitmap?)typeof(BootstrapListView)
            .GetField("_ownerDrawBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(list);

    private static int CountPixels(Bitmap bitmap, Color color)
    {
        var count = 0;
        var expected = color.ToArgb();
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
            if (bitmap.GetPixel(x, y).ToArgb() == expected) count++;
        return count;
    }

}
