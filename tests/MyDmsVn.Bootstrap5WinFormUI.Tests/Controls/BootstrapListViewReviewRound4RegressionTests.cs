using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewReviewRound4RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void DrawSubItemForTest(DrawListViewSubItemEventArgs e) => OnDrawSubItem(e);

        public void DrawColumnHeaderForTest(DrawListViewColumnHeaderEventArgs e) => OnDrawColumnHeader(e);

        public void RaiseMouseMove(Point location) => OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));

    }

    [Test]
    public void DrawItemSubscriberPaintingRemainsFinalWhenDrawDefaultIsFalse()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 100), View = View.List };
        var item = list.Items.Add("Custom item");
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var bounds = new Rectangle(0, 0, 240, 30);
        list.DrawItem += (_, e) => e.Graphics.Clear(Color.Magenta);

        list.DrawItemForTest(new DrawListViewItemEventArgs(graphics, item, bounds, 0, ListViewItemStates.Default));

        Assert.That(IsSolid(bitmap, bounds, Color.Magenta), Is.True);
    }

    [Test]
    public void DrawSubItemSubscriberPaintingRemainsFinalWhenDrawDefaultIsFalse()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 100), View = View.Details };
        var header = list.Columns.Add("Name", 240);
        var item = list.Items.Add("Custom cell");
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var bounds = new Rectangle(0, 0, 240, 30);
        list.DrawSubItem += (_, e) => e.Graphics.Clear(Color.Magenta);

        list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
            graphics, bounds, item, item.SubItems[0], 0, 0, header, ListViewItemStates.Default));

        Assert.That(IsSolid(bitmap, bounds, Color.Magenta), Is.True);
    }

    [Test]
    public void DrawColumnHeaderSubscriberPaintingRemainsFinalWhenDrawDefaultIsFalse()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 100), View = View.Details };
        var header = list.Columns.Add("Custom header", 240);
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var bounds = new Rectangle(0, 0, 240, 30);
        list.DrawColumnHeader += (_, e) => e.Graphics.Clear(Color.Magenta);

        list.DrawColumnHeaderForTest(new DrawListViewColumnHeaderEventArgs(
            graphics, bounds, 0, header, ListViewItemStates.Default, list.ForeColor, list.BackColor, list.Font));

        Assert.That(IsSolid(bitmap, bounds, Color.Magenta), Is.True);
    }

    [Test]
    public void LargeIconLabelWrapControlsWhetherLongTextUsesMultipleLines()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapListViewLayoutLogic.ShouldWrapItemText(View.LargeIcon, true), Is.True);
            Assert.That(BootstrapListViewLayoutLogic.ShouldWrapItemText(View.LargeIcon, false), Is.False);
            Assert.That(BootstrapListViewLayoutLogic.ShouldWrapItemText(View.SmallIcon, true), Is.False);
        }));
    }

    [Test]
    public void TileProjectionStopsAfterTwentyAdditionalColumns()
    {
        using var list = new TestBootstrapListView
        {
            Size = new Size(360, 700),
            TileSize = new Size(320, 660),
            View = View.Tile
        };
        list.Columns.Add(new ColumnHeader { Name = "primary", Text = "Primary", Width = 100, DisplayIndex = 0 });
        var item = new ListViewItem("Primary") { UseItemStyleForSubItems = false };
        item.SubItems[0].Name = "primary";
        list.Items.Add(item);
        for (var index = 1; index <= 21; index++)
        {
            list.Columns.Add(new ColumnHeader
            {
                Name = $"field{index}",
                Text = $"Field {index}",
                Width = 100,
                DisplayIndex = index
            });
            var subItem = item.SubItems.Add(index == 20 ? "TWENTIETH" : index == 21 ? "TWENTY-FIRST" : $"Value {index}");
            subItem.Name = $"field{index}";
            subItem.ForeColor = index == 20 ? Color.Blue : index == 21 ? Color.Lime : Color.Black;
        }

        _ = list.Handle;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.ForeColor, Is.Not.EqualTo(Color.Lime));
            Assert.That(item.SubItems[1].ForeColor, Is.EqualTo(Color.Black));
            Assert.That(item.SubItems[20].ForeColor, Is.EqualTo(Color.Blue));
            Assert.That(item.SubItems[21].ForeColor, Is.EqualTo(Color.Lime));
        }));
        using var bitmap = RenderItem(list, item, new Rectangle(0, 0, 320, 660));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(CountDominantPixels(bitmap, Color.Blue), Is.GreaterThan(0), "The twentieth additional tile column must remain visible.");
            Assert.That(CountDominantPixels(bitmap, Color.Lime), Is.Zero, "The twenty-first additional tile column exceeds the native limit.");
        }));
    }

    [Test]
    public void VirtualHoverInvalidatesOnlyPreviousAndCurrentItemBounds()
    {
        using var form = new Form { ClientSize = new Size(400, 220), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            HoverHighlight = true,
            Bounds = new Rectangle(0, 0, 360, 180),
            View = View.Details,
            VirtualMode = true
        };
        list.Columns.Add("Name", 300);
        list.RetrieveVirtualItem += (_, e) => e.Item = new ListViewItem($"Virtual {e.ItemIndex}");
        list.VirtualListSize = 100000;
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        var first = list.GetItemRect(0);
        var second = list.GetItemRect(1);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.HitTest(ItemPoint(first)).Item?.Index, Is.EqualTo(0));
            Assert.That(list.HitTest(ItemPoint(second)).Item?.Index, Is.EqualTo(1));
        }));
        list.Update();
        list.RaiseMouseMove(ItemPoint(first));
        var firstUpdate = GetUpdateBounds(list);
        list.Update();
        list.RaiseMouseMove(ItemPoint(second));
        var secondUpdate = GetUpdateBounds(list);
        var transitionBounds = Rectangle.Union(first, second);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(firstUpdate, Is.EqualTo(first));
            Assert.That(secondUpdate, Is.EqualTo(transitionBounds));
            Assert.That(firstUpdate, Is.Not.EqualTo(list.ClientRectangle));
            Assert.That(secondUpdate, Is.Not.EqualTo(list.ClientRectangle));
        }));
    }

    private static Bitmap RenderItem(TestBootstrapListView list, ListViewItem item, Rectangle bounds)
    {
        var bitmap = new Bitmap(Math.Max(1, bounds.Right), Math.Max(1, bounds.Bottom));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        list.DrawItemForTest(new DrawListViewItemEventArgs(
            graphics, item, bounds, item.Index, ListViewItemStates.Default));
        return bitmap;
    }

    private static Point ItemPoint(Rectangle bounds) => new Point(bounds.Left + 4, bounds.Top + (bounds.Height / 2));

    private static bool IsSolid(Bitmap bitmap, Rectangle bounds, Color color)
    {
        var expected = color.ToArgb();
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (var x = bounds.Left; x < bounds.Right; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != expected) return false;
            }
        }

        return true;
    }

    private static Rectangle GetUpdateBounds(Control control)
    {
        Assert.That(NativeMethods.GetUpdateRect(control.Handle, out var update, false), Is.True);
        return Rectangle.FromLTRB(update.Left, update.Top, update.Right, update.Bottom);
    }

    private static int CountDominantPixels(Bitmap bitmap, Color expected)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var actual = bitmap.GetPixel(x, y);
                if (expected.R > expected.G && expected.R > expected.B && actual.R > 128 && actual.R > actual.G + 64 && actual.R > actual.B + 64) count++;
                if (expected.G > expected.R && expected.G > expected.B && actual.G > 128 && actual.G > actual.R + 64 && actual.G > actual.B + 64) count++;
                if (expected.B > expected.R && expected.B > expected.G && actual.B > 128 && actual.B > actual.R + 64 && actual.B > actual.G + 64) count++;
            }
        }

        return count;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool GetUpdateRect(IntPtr handle, out NativeRectangle rectangle, bool erase);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }
}
