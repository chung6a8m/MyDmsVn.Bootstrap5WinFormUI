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
public sealed class BootstrapListViewReviewRound3RegressionTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public bool ShowFocusCuesForTest => ShowFocusCues;

        public void DrawItemForTest(DrawListViewItemEventArgs e) => OnDrawItem(e);

        public void DrawSubItemForTest(DrawListViewSubItemEventArgs e) => OnDrawSubItem(e);

        public void DrawColumnHeaderForTest(DrawListViewColumnHeaderEventArgs e) => OnDrawColumnHeader(e);

        public void ShowKeyboardFocusCuesForTest()
        {
            const int wmChangeUiState = 0x0127;
            const int uisClear = 2;
            const int uisfHideFocus = 1;
            NativeMethods.SendMessage(Handle, wmChangeUiState, new IntPtr(uisClear | (uisfHideFocus << 16)), IntPtr.Zero);
        }
    }

    [TestCase(View.Details)]
    [TestCase(View.List)]
    public void FocusedUnselectedItemStillRendersFocusCue(View view)
    {
        using var form = new Form { ClientSize = new Size(360, 180), ShowInTaskbar = false };
        using var list = new TestBootstrapListView
        {
            Bounds = new Rectangle(0, 0, 320, 140),
            FullRowSelect = false,
            View = view
        };
        if (view == View.Details) list.Columns.Add("Name", 240);
        var item = list.Items.Add("Focused but not selected");
        form.Controls.Add(list);
        form.Show();
        list.Focus();
        list.ShowKeyboardFocusCuesForTest();
        item.Focused = true;
        item.Selected = false;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.Focused, Is.True);
            Assert.That(list.ShowFocusCuesForTest, Is.True);
            Assert.That(item.Focused, Is.True);
            Assert.That(item.Selected, Is.False);
        }));

        using var focused = RenderItem(list, item, view);
        item.Focused = false;
        using var unfocused = RenderItem(list, item, view);

        Assert.That(BitmapsDiffer(focused, unfocused), Is.True,
            "The native focused item must retain a visible focus cue independently of selection.");
    }

    [Test]
    public void RtlReadingDirectionPreservesPhysicalRightAlignmentForSecondaryCellAndHeader()
    {
        using var list = new TestBootstrapListView
        {
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = false,
            Size = new Size(420, 140),
            View = View.Details
        };
        list.Columns.Add("Primary", 140);
        var header = list.Columns.Add("Right aligned header", 240, HorizontalAlignment.Right);
        var item = new ListViewItem(new[] { "Primary", "Right aligned cell" })
        {
            UseItemStyleForSubItems = false
        };
        item.SubItems[1].ForeColor = Color.Red;
        list.Items.Add(item);
        _ = list.Handle;

        using var cellBitmap = new Bitmap(260, 40);
        using (var graphics = Graphics.FromImage(cellBitmap))
        {
            graphics.Clear(Color.White);
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(0, 0, 240, 30),
                item,
                item.SubItems[1],
                item.Index,
                1,
                header,
                ListViewItemStates.Default));
        }

        using var headerBitmap = new Bitmap(260, 40);
        using (var graphics = Graphics.FromImage(headerBitmap))
        {
            graphics.Clear(Color.White);
            list.DrawColumnHeaderForTest(new DrawListViewColumnHeaderEventArgs(
                graphics,
                new Rectangle(0, 0, 240, 30),
                1,
                header,
                ListViewItemStates.Default,
                list.ForeColor,
                list.BackColor,
                list.Font));
        }

        var cellInk = FindDominantPixelBounds(cellBitmap, Color.Red);
        var headerInk = FindApproximatePixelBounds(
            headerBitmap,
            BootstrapThemeManager.CurrentTheme.Colors.Text,
            new Rectangle(4, 2, 232, 25));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(cellInk, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(cellInk.Left, Is.GreaterThan(120));
            Assert.That(headerInk, Is.Not.EqualTo(Rectangle.Empty));
            Assert.That(headerInk.Left, Is.GreaterThan(120));
        }));
    }

    [Test]
    public void HotTrackingControlsHotItemPresentation()
    {
        using var list = new TestBootstrapListView
        {
            HoverHighlight = false,
            HotTracking = false,
            Size = new Size(320, 120),
            View = View.List
        };
        var item = list.Items.Add("Hot tracked item");
        item.ForeColor = Color.Red;
        _ = list.Handle;

        using var disabled = RenderItem(list, item, View.List, ListViewItemStates.Hot);
        list.HotTracking = true;
        using var enabled = RenderItem(list, item, View.List, ListViewItemStates.Hot);

        Assert.That(BitmapsDiffer(disabled, enabled), Is.True,
            "HotTracking must retain a distinct presentation for native hot items under owner draw.");
    }

    [Test]
    public void DrawDefaultTrueIsPreservedForItemPainting()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 120), View = View.List };
        var item = list.Items.Add("Native item");
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var args = new DrawListViewItemEventArgs(graphics, item, new Rectangle(0, 0, 240, 30), 0, ListViewItemStates.Default);
        list.DrawItem += (_, e) => e.DrawDefault = true;

        list.DrawItemForTest(args);

        Assert.That(args.DrawDefault, Is.True);
    }

    [Test]
    public void DrawDefaultTrueIsPreservedForSubItemPainting()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 120), View = View.Details };
        var header = list.Columns.Add("Name", 240);
        var item = list.Items.Add("Native cell");
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var args = new DrawListViewSubItemEventArgs(
            graphics, new Rectangle(0, 0, 240, 30), item, item.SubItems[0], 0, 0, header, ListViewItemStates.Default);
        list.DrawSubItem += (_, e) => e.DrawDefault = true;

        list.DrawSubItemForTest(args);

        Assert.That(args.DrawDefault, Is.True);
    }

    [Test]
    public void DrawDefaultTrueIsPreservedForHeaderPainting()
    {
        using var list = new TestBootstrapListView { Size = new Size(320, 120), View = View.Details };
        var header = list.Columns.Add("Native header", 240);
        _ = list.Handle;
        using var bitmap = new Bitmap(320, 60);
        using var graphics = Graphics.FromImage(bitmap);
        var args = new DrawListViewColumnHeaderEventArgs(
            graphics, new Rectangle(0, 0, 240, 30), 0, header, ListViewItemStates.Default, list.ForeColor, list.BackColor, list.Font);
        list.DrawColumnHeader += (_, e) => e.DrawDefault = true;

        list.DrawColumnHeaderForTest(args);

        Assert.That(args.DrawDefault, Is.True);
    }

    private static Bitmap RenderItem(
        TestBootstrapListView list,
        ListViewItem item,
        View view,
        ListViewItemStates state = ListViewItemStates.Default)
    {
        var bitmap = new Bitmap(320, 80);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        if (view == View.Details)
        {
            list.DrawSubItemForTest(new DrawListViewSubItemEventArgs(
                graphics,
                new Rectangle(0, 0, 240, 30),
                item,
                item.SubItems[0],
                item.Index,
                0,
                list.Columns[0],
                state));
        }
        else
        {
            list.DrawItemForTest(new DrawListViewItemEventArgs(
                graphics,
                item,
                new Rectangle(0, 0, 240, 30),
                item.Index,
                state));
        }

        return bitmap;
    }

    private static bool BitmapsDiffer(Bitmap first, Bitmap second)
    {
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb()) return true;
            }
        }

        return false;
    }

    private static Rectangle FindDominantPixelBounds(Bitmap bitmap, Color expected)
    {
        return FindPixelBounds(bitmap, new Rectangle(Point.Empty, bitmap.Size), actual =>
            expected.R > expected.G && expected.R > expected.B &&
            actual.R > 128 && actual.R > actual.G + 64 && actual.R > actual.B + 64);
    }

    private static Rectangle FindApproximatePixelBounds(Bitmap bitmap, Color expected, Rectangle searchBounds)
    {
        return FindPixelBounds(bitmap, searchBounds, actual =>
            Math.Abs(actual.R - expected.R) <= 20 &&
            Math.Abs(actual.G - expected.G) <= 20 &&
            Math.Abs(actual.B - expected.B) <= 20);
    }

    private static Rectangle FindPixelBounds(Bitmap bitmap, Rectangle searchBounds, Func<Color, bool> predicate)
    {
        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        for (var y = searchBounds.Top; y < searchBounds.Bottom; y++)
        {
            for (var x = searchBounds.Left; x < searchBounds.Right; x++)
            {
                if (!predicate(bitmap.GetPixel(x, y))) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return left == int.MaxValue ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    }
}
