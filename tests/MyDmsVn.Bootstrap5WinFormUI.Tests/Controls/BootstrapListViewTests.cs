using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
public sealed class BootstrapListViewTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        private const int LvmSubItemHitTest = 0x1039;

        public bool DoubleBufferedForTest => DoubleBuffered;

        public int NativeHitTestMessageCount { get; private set; }

        public void RecreateHandleForTest() => RecreateHandle();

        public void RaiseDpiChangedAfterParent() => OnDpiChangedAfterParent(EventArgs.Empty);

        public void RaiseMouseMove(Point location) => OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));

        public void RaiseMouseLeave() => OnMouseLeave(EventArgs.Empty);

        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void DrawSubItemForTest(DrawListViewSubItemEventArgs e) => OnDrawSubItem(e);

        public void DrawColumnHeaderForTest(DrawListViewColumnHeaderEventArgs e) => OnDrawColumnHeader(e);

        public void ResetNativeHitTestMessageCount() => NativeHitTestMessageCount = 0;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == LvmSubItemHitTest) NativeHitTestMessageCount++;
            base.WndProc(ref m);
        }
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
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void DefaultsMatchNativeBackedContract()
    {
        using var list = new TestBootstrapListView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list, Is.InstanceOf<ListView>());
            Assert.That(list.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(list.Striped, Is.False);
            Assert.That(list.HoverHighlight, Is.True);
            Assert.That(list.OwnerDraw, Is.True);
            Assert.That(list.DoubleBufferedForTest, Is.True);
        }));
    }

    [Test]
    public void V1DeclaresOnlyBootstrapAppearanceProperties()
    {
        var declared = typeof(BootstrapListView)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

        Assert.That(declared, Is.EquivalentTo(new[]
        {
            nameof(BootstrapListView.Variant),
            nameof(BootstrapListView.Striped),
            nameof(BootstrapListView.HoverHighlight)
        }));
    }

    [Test]
    public void AppearanceChangesPreserveCallerOwnedNativeState()
    {
        using var images = new ImageList();
        using var image = new Bitmap(16, 16);
        images.Images.Add("item", image);

        using var list = new BootstrapListView
        {
            CheckBoxes = true,
            SmallImageList = images,
            View = View.Details
        };
        var column = new ColumnHeader { Text = "Name", Width = 160 };
        var group = new ListViewGroup("Group");
        var item = new ListViewItem("Item", group)
        {
            Checked = true,
            ImageKey = "item",
            Selected = true
        };
        list.Columns.Add(column);
        list.Groups.Add(group);
        list.Items.Add(item);

        list.Variant = BootstrapVariant.Success;
        list.Striped = true;
        list.HoverHighlight = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(list.Columns[0], Is.SameAs(column));
            Assert.That(list.Groups[0], Is.SameAs(group));
            Assert.That(list.SmallImageList, Is.SameAs(images));
            Assert.That(item.Group, Is.SameAs(group));
            Assert.That(item.Checked, Is.True);
            Assert.That(item.Selected, Is.True);
            Assert.That(item.ImageKey, Is.EqualTo("item"));
        }));
    }

    [Test]
    public void RuntimeThemeAndCallerFontLifecyclePreserveNativeData()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var list = new TestBootstrapListView();
        var item = list.Items.Add("Theme item");
        using var callerFont = new Font("Segoe UI", 13f, FontStyle.Italic);
        list.Font = callerFont;

        var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        BootstrapThemeManager.CurrentTheme = dark;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));
            Assert.That(list.BackColor, Is.EqualTo(dark.Colors.Surface));
            Assert.That(list.ForeColor, Is.EqualTo(dark.Colors.Text));
            Assert.That(list.Font, Is.SameAs(callerFont));
            Assert.That(list.Items[0], Is.SameAs(item));
        }));

        list.Dispose();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
            Assert.That(IsFontUsable(callerFont), Is.True);
        }));
    }

    [Test]
    public void DetailsDrawItemDoesNotPaintButColumnZeroSubItemOwnsBackground()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(240, 80),
            View = View.Details
        };
        list.Columns.Add("Name", 220);
        var item = list.Items.Add("Alpha");
        using var bitmap = new Bitmap(240, 40);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        var bounds = new Rectangle(0, 0, 220, 24);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            bounds,
            item.Index,
            ListViewItemStates.Default));
        Assert.That(bitmap.GetPixel(5, 5).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));

        list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
            graphics,
            bounds,
            item,
            item.SubItems[0],
            item.Index,
            0,
            list.Columns[0],
            ListViewItemStates.Default));
        Assert.That(
            bitmap.GetPixel(5, 5).ToArgb(),
            Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Surface.ToArgb()));
    }

    [TestCase(View.List)]
    [TestCase(View.SmallIcon)]
    [TestCase(View.LargeIcon)]
    [TestCase(View.Tile)]
    public void NonDetailsRenderingPreservesNativeState(View view)
    {
        using var images = new ImageList { ImageSize = new Size(16, 16) };
        images.Images.Add(new Bitmap(16, 16));
        using var list = new TestBootstrapListView
        {
            Size = new Size(320, 140),
            View = view,
            SmallImageList = images,
            LargeImageList = images
        };
        var item = new ListViewItem(new[] { "Primary", "Secondary" }, 0) { Selected = true };
        list.Items.Add(item);
        using var bitmap = new Bitmap(320, 140);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            new Rectangle(0, 0, 240, 72),
            item.Index,
            ListViewItemStates.Selected));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.View, Is.EqualTo(view));
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(item.Selected, Is.True);
            Assert.That(list.SmallImageList, Is.SameAs(images));
            Assert.That(list.LargeImageList, Is.SameAs(images));
        }));
    }

    [Test]
    public void ListViewDrawsCallerImageOnceAtTheNativeIconSlot()
    {
        using var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        using var source = new Bitmap(16, 16);
        using (var sourceGraphics = Graphics.FromImage(source))
        {
            sourceGraphics.Clear(Color.Red);
            images.Images.Add(source);
        }

        using var list = new TestBootstrapListView
        {
            Size = new Size(320, 100),
            View = View.List,
            SmallImageList = images
        };
        var item = list.Items.Add("One image", 0);
        _ = list.Handle;
        var bounds = item.GetBounds(ItemBoundsPortion.Entire);
        using var bitmap = new Bitmap(320, 100);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            bounds,
            item.Index,
            ListViewItemStates.Default));

        var nativeIconBounds = item.GetBounds(ItemBoundsPortion.Icon);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountPixels(bitmap, Color.Red), Is.LessThanOrEqualTo(16 * 16));
            Assert.That(AllPixelsOfColorAreInside(bitmap, Color.Red, nativeIconBounds), Is.True);
        }));
    }

    [TestCase(View.Details)]
    [TestCase(View.List)]
    [TestCase(View.SmallIcon)]
    [TestCase(View.LargeIcon)]
    public void StateImageRenderingStaysInsideNativeHitRegion(View view)
    {
        using var stateImages = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        using var stateImage = new Bitmap(16, 16);
        using (var stateGraphics = Graphics.FromImage(stateImage))
        {
            stateGraphics.Clear(Color.Red);
            stateImages.Images.Add(stateImage);
        }

        using var itemImages = new ImageList { ImageSize = new Size(24, 24) };
        itemImages.Images.Add(new Bitmap(24, 24));
        using var list = new TestBootstrapListView
        {
            Size = new Size(360, 180),
            View = view,
            StateImageList = stateImages,
            SmallImageList = itemImages,
            LargeImageList = itemImages
        };
        if (view == View.Details) list.Columns.Add("Name", 280);
        var item = list.Items.Add("State image", 0);
        item.StateImageIndex = 0;
        _ = list.Handle;
        var nativeStateBounds = FindHitBounds(list, item, ListViewHitTestLocations.StateImage);
        Assert.That(nativeStateBounds, Is.Not.EqualTo(Rectangle.Empty));

        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        if (view == View.Details)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(0, item.Bounds.Top, list.Columns[0].Width, item.Bounds.Height),
                item,
                item.SubItems[0],
                item.Index,
                0,
                list.Columns[0],
                ListViewItemStates.Default));
        }
        else
        {
            list.DrawItemForTest(new DrawListViewItemEventArgs(
                graphics,
                item,
                item.GetBounds(ItemBoundsPortion.Entire),
                item.Index,
                ListViewItemStates.Default));
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountPixels(bitmap, Color.Red), Is.GreaterThan(0));
            Assert.That(AllPixelsOfColorAreInside(bitmap, Color.Red, nativeStateBounds), Is.True);
        }));
    }

    [TestCase(View.Details)]
    [TestCase(View.List)]
    [TestCase(View.SmallIcon)]
    [TestCase(View.LargeIcon)]
    public void CheckboxRenderingStaysInsideNativeHitRegion(View view)
    {
        using var itemImages = new ImageList { ImageSize = new Size(24, 24) };
        itemImages.Images.Add(new Bitmap(24, 24));
        using var list = new TestBootstrapListView
        {
            CheckBoxes = true,
            Size = new Size(360, 180),
            SmallImageList = itemImages,
            LargeImageList = itemImages,
            View = view
        };
        if (view == View.Details) list.Columns.Add("Name", 280);
        var item = list.Items.Add(string.Empty, 0);
        item.Checked = true;
        item.ForeColor = Color.Red;
        _ = list.Handle;
        var nativeStateBounds = FindHitBounds(list, item, ListViewHitTestLocations.StateImage);
        Assert.That(nativeStateBounds, Is.Not.EqualTo(Rectangle.Empty));
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        if (view == View.Details)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(0, item.Bounds.Top, list.Columns[0].Width, item.Bounds.Height),
                item,
                item.SubItems[0],
                item.Index,
                0,
                list.Columns[0],
                ListViewItemStates.Default));
        }
        else
        {
            list.DrawItemForTest(new DrawListViewItemEventArgs(
                graphics,
                item,
                item.GetBounds(ItemBoundsPortion.Entire),
                item.Index,
                ListViewItemStates.Default));
        }

        var checkboxPixels = FindDominantPixelBounds(bitmap, Color.Red);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountDominantPixels(bitmap, Color.Red), Is.GreaterThan(0));
            Assert.That(AllDominantPixelsAreInside(bitmap, Color.Red, nativeStateBounds), Is.True);
            Assert.That(Math.Abs(checkboxPixels.Width - checkboxPixels.Height), Is.LessThanOrEqualTo(2));
        }));
    }

    [Test]
    public void StateImageBoundsDiscoveryDoesNotScaleWithRowWidth()
    {
        var narrowCount = MeasureStateImageHitTests(320);
        var wideCount = MeasureStateImageHitTests(1600);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(narrowCount, Is.GreaterThan(0));
            Assert.That(wideCount, Is.LessThanOrEqualTo(narrowCount + 8));
            Assert.That(wideCount, Is.LessThanOrEqualTo(64));
        }));
    }

    [Test]
    public void TileWithoutColumnsDoesNotRenderAdditionalSubItems()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(360, 160),
            TileSize = new Size(280, 96),
            View = View.Tile
        };
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems.Add("Hidden", Color.Red, Color.Empty, list.Font);
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            item.GetBounds(ItemBoundsPortion.Entire),
            item.Index,
            ListViewItemStates.Default));

        Assert.That(CountDominantPixels(bitmap, Color.Red), Is.Zero);
    }

    [Test]
    public void TileUsesVisibleColumnsInDisplayOrder()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(400, 180),
            TileSize = new Size(320, 120),
            View = View.Tile
        };
        list.Columns.Add("Primary", 100);
        var hidden = list.Columns.Add("Hidden", 0);
        var later = list.Columns.Add("Later", 100);
        var earlier = list.Columns.Add("Earlier", 100);
        earlier.DisplayIndex = 1;
        later.DisplayIndex = 2;
        hidden.DisplayIndex = 3;
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems.Add("Hidden", Color.Red, Color.Empty, list.Font);
        item.SubItems.Add("Later", Color.Blue, Color.Empty, list.Font);
        item.SubItems.Add("Earlier", Color.Lime, Color.Empty, list.Font);
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            item.GetBounds(ItemBoundsPortion.Entire),
            item.Index,
            ListViewItemStates.Default));

        var earlierY = AverageDominantPixelY(bitmap, Color.Lime);
        var laterY = AverageDominantPixelY(bitmap, Color.Blue);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountDominantPixels(bitmap, Color.Red), Is.Zero);
            Assert.That(earlierY, Is.LessThan(laterY));
        }));
    }

    [Test]
    public void TileMatchesNamedColumnsToNamedSubItemsBeforeApplyingDisplayOrder()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(400, 180),
            TileSize = new Size(320, 120),
            View = View.Tile
        };
        list.Columns.Add(new ColumnHeader { Name = "primary", Text = "Primary", Width = 100 });
        list.Columns.Add(new ColumnHeader { Name = "city", Text = "City", Width = 100 });
        list.Columns.Add(new ColumnHeader { Name = "code", Text = "Code", Width = 100 });
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems[0].Name = "primary";
        item.SubItems.Add(new ListViewItem.ListViewSubItem(item, "Code", Color.Blue, Color.Empty, list.Font) { Name = "code" });
        item.SubItems.Add(new ListViewItem.ListViewSubItem(item, "City", Color.Lime, Color.Empty, list.Font) { Name = "city" });
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            new Rectangle(0, 0, list.TileSize.Width, list.TileSize.Height),
            item.Index,
            ListViewItemStates.Default));

        Assert.That(
            AverageDominantPixelY(bitmap, Color.Lime),
            Is.LessThan(AverageDominantPixelY(bitmap, Color.Blue)));
    }

    [Test]
    public void TileNamedAdditionalColumnsDoNotDependOnRawPrimaryColumnIndex()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(400, 180),
            TileSize = new Size(320, 120),
            View = View.Tile
        };
        list.Columns.Add(new ColumnHeader { Name = "city", Text = "City", Width = 100 });
        list.Columns.Add(new ColumnHeader { Name = "code", Text = "Code", Width = 100 });
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems.Add(new ListViewItem.ListViewSubItem(item, "Code", Color.Blue, Color.Empty, list.Font) { Name = "code" });
        item.SubItems.Add(new ListViewItem.ListViewSubItem(item, "City", Color.Lime, Color.Empty, list.Font) { Name = "city" });
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            new Rectangle(0, 0, list.TileSize.Width, list.TileSize.Height),
            item.Index,
            ListViewItemStates.Default));

        Assert.That(
            AverageDominantPixelY(bitmap, Color.Lime),
            Is.LessThan(AverageDominantPixelY(bitmap, Color.Blue)));
    }

    [TestCase(0)]
    [TestCase(2)]
    public void DetailsIndentCountUsesNativeIconAndLabelGeometry(int indentCount)
    {
        using var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        using var image = new Bitmap(16, 16);
        using (var imageGraphics = Graphics.FromImage(image))
        {
            imageGraphics.Clear(Color.Red);
            images.Images.Add(image);
        }

        using var list = new TestBootstrapListView
        {
            Size = new Size(360, 120),
            SmallImageList = images,
            View = View.Details
        };
        list.Columns.Add("Name", 300);
        var item = list.Items.Add("Indented", 0);
        item.ForeColor = Color.Lime;
        item.IndentCount = indentCount;
        _ = list.Handle;
        var iconBounds = item.GetBounds(ItemBoundsPortion.Icon);
        var labelBounds = item.GetBounds(ItemBoundsPortion.Label);
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
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

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountPixels(bitmap, Color.Red), Is.GreaterThan(0));
            Assert.That(AllPixelsOfColorAreInside(bitmap, Color.Red, iconBounds), Is.True);
            Assert.That(AllDominantPixelsAreInside(bitmap, Color.Lime, labelBounds), Is.True);
        }));
    }

    [Test]
    public void DetailsSecondaryColumnsKeepTheirOwnCellTextBounds()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(360, 120),
            View = View.Details
        };
        list.Columns.Add("Primary", 100);
        list.Columns.Add("Second", 100);
        list.Columns.Add("Third", 100);
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems.Add("Second", Color.Red, Color.Empty, list.Font);
        item.SubItems.Add("Third", Color.Lime, Color.Empty, list.Font);
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);

        for (var columnIndex = 1; columnIndex <= 2; columnIndex++)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(columnIndex * 100, item.Bounds.Top, 100, item.Bounds.Height),
                item,
                item.SubItems[columnIndex],
                item.Index,
                columnIndex,
                list.Columns[columnIndex],
                ListViewItemStates.Default));
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountDominantPixels(bitmap, Color.Red), Is.GreaterThan(0));
            Assert.That(CountDominantPixels(bitmap, Color.Lime), Is.GreaterThan(0));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ColumnHeaderPreservesIndexAndKeyBackedImages(bool useKey)
    {
        using var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        using var first = new Bitmap(16, 16);
        using var expected = new Bitmap(16, 16);
        using (var firstGraphics = Graphics.FromImage(first)) firstGraphics.Clear(Color.Red);
        using (var expectedGraphics = Graphics.FromImage(expected)) expectedGraphics.Clear(Color.Lime);
        images.Images.Add("first", first);
        images.Images.Add("expected", expected);
        using var list = new TestBootstrapListView
        {
            Size = new Size(320, 100),
            SmallImageList = images,
            View = View.Details
        };
        var header = list.Columns.Add("Header text", 240);
        if (useKey) header.ImageKey = "expected"; else header.ImageIndex = 1;
        _ = list.Handle;
        using var bitmap = new Bitmap(260, 40);
        using var graphics = Graphics.FromImage(bitmap);
        var bounds = new Rectangle(0, 0, 240, 28);

        list.DrawColumnHeaderForTest(new DrawListViewColumnHeaderEventArgs(
            graphics,
            bounds,
            0,
            header,
            ListViewItemStates.Default,
            list.ForeColor,
            list.BackColor,
            list.Font));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountDominantPixels(bitmap, Color.Lime), Is.GreaterThan(0));
            Assert.That(CountDominantPixels(bitmap, Color.Red), Is.Zero);
        }));
    }

    [Test]
    public void RtlTextWithoutLayoutMirroringKeepsTileTextAfterNativeIcon()
    {
        using var images = new ImageList { ImageSize = new Size(32, 32) };
        images.Images.Add(new Bitmap(32, 32));
        using var list = new TestBootstrapListView
        {
            LargeImageList = images,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = false,
            Size = new Size(360, 160),
            TileSize = new Size(280, 96),
            View = View.Tile
        };
        var item = list.Items.Add("RTL tile", 0);
        item.ForeColor = Color.Red;
        _ = list.Handle;
        var iconBounds = item.GetBounds(ItemBoundsPortion.Icon);
        using var bitmap = new Bitmap(list.ClientSize.Width, list.ClientSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        var drawBounds = new Rectangle(0, 0, list.TileSize.Width, list.TileSize.Height);

        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics,
            item,
            drawBounds,
            item.Index,
            ListViewItemStates.Default));

        var textPixels = FindDominantPixelBounds(bitmap, Color.Red);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(textPixels, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(textPixels.Left, Is.GreaterThanOrEqualTo(iconBounds.Right));
        }));
    }

    [Test]
    public void RtlTextWithoutLayoutMirroringKeepsHeaderImageOnNativeLeadingEdge()
    {
        using var images = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        using var image = new Bitmap(16, 16);
        using (var imageGraphics = Graphics.FromImage(image)) imageGraphics.Clear(Color.Lime);
        images.Images.Add(image);
        using var list = new TestBootstrapListView
        {
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = false,
            Size = new Size(320, 100),
            SmallImageList = images,
            View = View.Details
        };
        var header = list.Columns.Add("RTL header", 240);
        header.ImageIndex = 0;
        _ = list.Handle;
        using var bitmap = new Bitmap(260, 40);
        using var graphics = Graphics.FromImage(bitmap);
        var bounds = new Rectangle(0, 0, 240, 28);

        list.DrawColumnHeaderForTest(new DrawListViewColumnHeaderEventArgs(
            graphics,
            bounds,
            0,
            header,
            ListViewItemStates.Default,
            list.ForeColor,
            list.BackColor,
            list.Font));

        var imagePixels = FindDominantPixelBounds(bitmap, Color.Lime);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(imagePixels, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(imagePixels.Right, Is.LessThan(bounds.Left + (bounds.Width / 2)));
        }));
    }

    [TestCase(View.Details)]
    [TestCase(View.List)]
    [TestCase(View.SmallIcon)]
    [TestCase(View.LargeIcon)]
    [TestCase(View.Tile)]
    public void HoverBookkeepingDoesNotMutateNativeInteractionState(View view)
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(320, 140),
            View = view,
            CheckBoxes = view != View.Tile,
            HotTracking = false,
            HoverSelection = false,
            Activation = ItemActivation.Standard
        };
        if (view == View.Details)
        {
            list.Columns.Add("Name", 220);
        }

        var item = list.Items.Add("Alpha");
        item.Checked = view != View.Tile;
        _ = list.Handle;
        var selectedBefore = item.Selected;
        var checkedBefore = item.Checked;
        var focusedBefore = list.FocusedItem;

        list.RaiseMouseMove(new Point(8, 8));
        list.RaiseMouseLeave();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Selected, Is.EqualTo(selectedBefore));
            Assert.That(item.Checked, Is.EqualTo(checkedBefore));
            Assert.That(list.FocusedItem, Is.SameAs(focusedBefore));
            Assert.That(list.HotTracking, Is.False);
            Assert.That(list.HoverSelection, Is.False);
            Assert.That(list.Activation, Is.EqualTo(ItemActivation.Standard));
        }));
    }

    [Test]
    public void VirtualModeUsesNativeSetupOrderAndPreservesTileRestriction()
    {
        using var virtualList = new BootstrapListView
        {
            VirtualMode = true,
            View = View.Details
        };
        virtualList.Columns.Add("Name", 180);
        virtualList.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem($"Item {e.ItemIndex}");
        virtualList.VirtualListSize = 1000;

        using var nativeTile = new ListView { View = View.Tile };
        using var bootstrapTile = new BootstrapListView { View = View.Tile };
        var nativeException = Assert.Catch((Action)(() => nativeTile.VirtualMode = true));
        var bootstrapException = Assert.Catch((Action)(() => bootstrapTile.VirtualMode = true));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(virtualList.VirtualListSize, Is.EqualTo(1000));
            Assert.That(bootstrapException?.GetType(), Is.EqualTo(nativeException?.GetType()));
            Assert.That(bootstrapTile.View, Is.EqualTo(nativeTile.View));
        }));
    }

    [Test]
    public void HandleRecreationRetainsPresentationAndCallerState()
    {
        using var list = new TestBootstrapListView { CheckBoxes = true, View = View.Details };
        var column = list.Columns.Add("Name", 180);
        var item = list.Items.Add("Alpha");
        item.Checked = true;
        item.Selected = true;
        _ = list.Handle;

        list.RecreateHandleForTest();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.OwnerDraw, Is.True);
            Assert.That(list.DoubleBufferedForTest, Is.True);
            Assert.That(list.Columns[0], Is.SameAs(column));
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(item.Checked, Is.True);
            Assert.That(item.Selected, Is.True);
        }));
    }

    [Test]
    public void GroupsRemainCallerOwnedAcrossThemeAndUnsupportedListViewMode()
    {
        using var list = new BootstrapListView { ShowGroups = true, View = View.Details };
        list.Columns.Add("Name", 180);
        var group = new ListViewGroup("Native group");
        var item = new ListViewItem("Grouped item", group);
        list.Groups.Add(group);
        list.Items.Add(item);

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        list.Variant = BootstrapVariant.Warning;
        list.View = View.List;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.ShowGroups, Is.True);
            Assert.That(list.Groups[0], Is.SameAs(group));
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(item.Group, Is.SameAs(group));
            Assert.That(list.View, Is.EqualTo(View.List));
        }));
    }

    [Test]
    public void LabelEditAndNativeEventsRemainInheritedAcrossThemeAndHandleChanges()
    {
        using var list = new TestBootstrapListView { LabelEdit = true, View = View.Details };
        list.Columns.Add("Name", 180);
        var item = list.Items.Add("Editable");
        var beforeLabelEdit = 0;
        var afterLabelEdit = 0;
        list.BeforeLabelEdit += (_, _) => beforeLabelEdit++;
        list.AfterLabelEdit += (_, _) => afterLabelEdit++;
        _ = list.Handle;

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        list.RecreateHandleForTest();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.LabelEdit, Is.True);
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(beforeLabelEdit, Is.Zero);
            Assert.That(afterLabelEdit, Is.Zero);
            Assert.That(typeof(BootstrapListView).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(typeof(BootstrapListView).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly), Is.Null);
        }));
    }

    [Test]
    public void VirtualSizeShrinkDoesNotLeaveHoverDependentOnNormalItemsCollection()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(280, 120),
            VirtualMode = true,
            View = View.Details
        };
        list.Columns.Add("Name", 240);
        list.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem($"Virtual {e.ItemIndex}");
        list.VirtualListSize = 100;
        _ = list.Handle;

        list.RaiseMouseMove(new Point(8, 24));
        list.VirtualListSize = 1;
        list.RaiseMouseMove(new Point(8, 80));
        list.RaiseMouseLeave();

        Assert.That(list.VirtualListSize, Is.EqualTo(1));
    }

    [Test]
    public void DpiLifecycleDoesNotRescaleCallerOwnedNativeGeometry()
    {
        using var images = new ImageList { ImageSize = new Size(24, 20) };
        images.Images.Add(new Bitmap(24, 20));
        using var list = new TestBootstrapListView
        {
            View = View.Tile,
            TileSize = new Size(260, 72),
            LargeImageList = images
        };
        var column = list.Columns.Add("Name", 173);
        list.Items.Add("Alpha", 0);

        list.RaiseDpiChangedAfterParent();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.TileSize, Is.EqualTo(new Size(260, 72)));
            Assert.That(column.Width, Is.EqualTo(173));
            Assert.That(images.ImageSize, Is.EqualTo(new Size(24, 20)));
            Assert.That(list.LargeImageList, Is.SameAs(images));
        }));
    }

    [TestCase(View.Details)]
    [TestCase(View.List)]
    [TestCase(View.Tile)]
    public void RtlOwnerDrawingUsesNativeViewWithoutMutatingLayoutProperties(View view)
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(320, 120),
            View = view,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true
        };
        if (view == View.Details) list.Columns.Add("Name", 220, HorizontalAlignment.Right);
        var item = new ListViewItem(new[] { "RTL item", "Secondary" });
        list.Items.Add(item);
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 120);
        using var graphics = Graphics.FromImage(bitmap);

        if (view == View.Details)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(0, 0, 220, 24),
                item,
                item.SubItems[0],
                item.Index,
                0,
                list.Columns[0],
                ListViewItemStates.Default));
        }
        else
        {
            list.DrawItemForTest(new DrawListViewItemEventArgs(
                graphics,
                item,
                new Rectangle(0, 0, 240, 72),
                item.Index,
                ListViewItemStates.Default));
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.View, Is.EqualTo(view));
            Assert.That(list.RightToLeft, Is.EqualTo(RightToLeft.Yes));
            Assert.That(list.RightToLeftLayout, Is.True);
            Assert.That(list.Items[0], Is.SameAs(item));
        }));
    }

    [Test]
    public void LargeNormalAndVirtualListsSupportConstantScopePaintSmoke()
    {
        using var normal = new TestBootstrapListView { Size = new Size(300, 120), View = View.Details };
        normal.Columns.Add("Name", 240);
        for (var index = 0; index < 5000; index++) normal.Items.Add($"Item {index}");
        using var bitmap = new Bitmap(300, 40);
        using var graphics = Graphics.FromImage(bitmap);
        var item = normal.Items[2500];
        normal.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
            graphics,
            new Rectangle(0, 0, 240, 24),
            item,
            item.SubItems[0],
            item.Index,
            0,
            normal.Columns[0],
            ListViewItemStates.Default));

        using var virtualList = new BootstrapListView { VirtualMode = true, View = View.Details };
        virtualList.Columns.Add("Name", 240);
        virtualList.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem($"Virtual {e.ItemIndex}");
        virtualList.VirtualListSize = 100000;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normal.Items.Count, Is.EqualTo(5000));
            Assert.That(virtualList.VirtualListSize, Is.EqualTo(100000));
        }));
    }

    [Test]
    public void InvalidVariantIsRejectedBeforeStateMutation()
    {
        using var list = new BootstrapListView();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => list.Variant = (BootstrapVariant)int.MaxValue));
        Assert.That(list.Variant, Is.EqualTo(BootstrapVariant.Primary));
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static int CountPixels(Bitmap bitmap, Color expected)
    {
        var count = 0;
        var argb = expected.ToArgb();
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == argb)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountDominantPixels(Bitmap bitmap, Color expected)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (IsDominant(bitmap.GetPixel(x, y), expected)) count++;
            }
        }

        return count;
    }

    private static double AverageDominantPixelY(Bitmap bitmap, Color expected)
    {
        var count = 0;
        var total = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (!IsDominant(bitmap.GetPixel(x, y), expected)) continue;
                count++;
                total += y;
            }
        }

        Assert.That(count, Is.GreaterThan(0));
        return (double)total / count;
    }

    private static bool IsDominant(Color actual, Color expected)
    {
        if (expected.R > expected.G && expected.R > expected.B) return actual.R > 128 && actual.R > actual.G + 64 && actual.R > actual.B + 64;
        if (expected.G > expected.R && expected.G > expected.B) return actual.G > 128 && actual.G > actual.R + 64 && actual.G > actual.B + 64;
        return actual.B > 128 && actual.B > actual.R + 64 && actual.B > actual.G + 64;
    }

    private static bool AllDominantPixelsAreInside(Bitmap bitmap, Color expected, Rectangle bounds)
    {
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (IsDominant(bitmap.GetPixel(x, y), expected) && !bounds.Contains(x, y)) return false;
            }
        }

        return true;
    }

    private static Rectangle FindDominantPixelBounds(Bitmap bitmap, Color expected)
    {
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (!IsDominant(bitmap.GetPixel(x, y), expected)) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return left == int.MaxValue
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static bool AllPixelsOfColorAreInside(Bitmap bitmap, Color expected, Rectangle bounds)
    {
        var argb = expected.ToArgb();
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == argb && !bounds.Contains(x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static Rectangle FindHitBounds(ListView list, ListViewItem item, ListViewHitTestLocations location)
    {
        var searchBounds = Rectangle.Intersect(list.ClientRectangle, item.GetBounds(ItemBoundsPortion.Entire));
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        for (var y = searchBounds.Top; y < searchBounds.Bottom; y++)
        {
            for (var x = searchBounds.Left; x < searchBounds.Right; x++)
            {
                var hit = list.HitTest(x, y);
                if (!ReferenceEquals(hit.Item, item) || (hit.Location & location) != location) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return left == int.MaxValue
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static int MeasureStateImageHitTests(int width)
    {
        using var images = new ImageList { ImageSize = new Size(16, 16) };
        images.Images.Add(new Bitmap(16, 16));
        using var list = new TestBootstrapListView
        {
            CheckBoxes = true,
            Size = new Size(width, 100),
            SmallImageList = images,
            View = View.Details
        };
        list.Columns.Add("Name", width - 20);
        var item = list.Items.Add("Measured", 0);
        _ = list.Handle;
        using var bitmap = new Bitmap(width, 50);
        using var graphics = Graphics.FromImage(bitmap);
        list.ResetNativeHitTestMessageCount();

        list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
            graphics,
            new Rectangle(0, item.Bounds.Top, list.Columns[0].Width, item.Bounds.Height),
            item,
            item.SubItems[0],
            item.Index,
            0,
            list.Columns[0],
            ListViewItemStates.Default));

        return list.NativeHitTestMessageCount;
    }

    private static bool IsFontUsable(Font font)
    {
        IntPtr handle = IntPtr.Zero;
        try
        {
            handle = font.ToHfont();
            return handle != IntPtr.Zero;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is ExternalException ||
            exception is ObjectDisposedException)
        {
            return false;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(handle);
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);
    }
}
