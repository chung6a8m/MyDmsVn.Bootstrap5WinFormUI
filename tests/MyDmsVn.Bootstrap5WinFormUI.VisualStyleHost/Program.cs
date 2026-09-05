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
#if NET8_0_OR_GREATER
            VerifyNativeGroupAffordances();
#endif
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

#if NET8_0_OR_GREATER
    private static void VerifyNativeGroupAffordances()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        using var groupImages = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        groupImages.Images.Add(CreateSolidBitmap(Color.Lime));
        using var form = new Form { ClientSize = new Size(520, 360), ShowInTaskbar = false };
        using var list = new BootstrapListView
        {
            Bounds = new Rectangle(0, 0, 480, 320),
            GroupImageList = groupImages,
            ShowGroups = true,
            View = View.Details
        };
        list.Columns.Add("Name", 400);
        var collapsible = new ListViewGroup("collapsible", "Collapsible")
        {
            CollapsedState = ListViewGroupCollapsedState.Expanded
        };
        var rich = new ListViewGroup("rich", "Rich group")
        {
            Subtitle = "Native subtitle",
            Footer = "Native footer",
            TaskLink = "Run task",
            TitleImageIndex = 0
        };
        list.Groups.Add(collapsible);
        list.Groups.Add(rich);
        list.Items.Add(new ListViewItem("Collapsible item", collapsible));
        list.Items.Add(new ListViewItem("Rich item", rich));
        var collapsedEvents = 0;
        var taskEvents = 0;
        list.GroupCollapsedStateChanged += (_, _) => collapsedEvents++;
        list.GroupTaskLinkClick += (_, _) => taskEvents++;
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        list.Update();

        var expandedHeader = GetGroupHeaderBounds(list.Handle, 0);
        var richHeader = GetGroupHeaderBounds(list.Handle, 1);
        using var expanded = CaptureWindowClient(list.Handle);
        Require(CountPixelsNear(expanded, Color.Lime, 8, richHeader) > 20,
            "The native group title image was suppressed.");

        collapsible.CollapsedState = ListViewGroupCollapsedState.Collapsed;
        list.Update();
        Application.DoEvents();
        var collapsedHeader = GetGroupHeaderBounds(list.Handle, 0);
        using var collapsed = CaptureWindowClient(list.Handle);
        var affordanceRegion = Rectangle.Intersect(
            new Rectangle(expandedHeader.Right - 28, expandedHeader.Top, 28, expandedHeader.Height),
            new Rectangle(collapsedHeader.Right - 28, collapsedHeader.Top, 28, collapsedHeader.Height));
        Require(CountPixelDifferences(expanded, collapsed, affordanceRegion) > 2,
            "The native expand/collapse affordance was suppressed.");

        var collapseHeader = GetGroupHeaderBounds(list.Handle, 0);
        Click(list, collapseHeader.Right - 12, collapseHeader.Top + 10);
        Require(collapsible.CollapsedState == ListViewGroupCollapsedState.Expanded && collapsedEvents > 0,
            "The native collapse affordance is not clickable.");
        var taskHeader = GetGroupHeaderBounds(list.Handle, 1);
        var taskLinkRegion = Rectangle.FromLTRB(taskHeader.Right - 70, taskHeader.Top, taskHeader.Right, taskHeader.Top + 24);
        using var taskCapture = CaptureWindowClient(list.Handle);
        Require(CountPixelsNear(taskCapture, Color.FromArgb(0, 102, 204), 80, taskLinkRegion) > 0,
            "The native group task link was suppressed.");
        RaiseNativeTaskLinkNotification(list.Handle, GetNativeGroupId(list.Handle, 1));
        Require(taskEvents > 0, "The native group task link is not clickable.");
    }

    private static Bitmap CreateSolidBitmap(Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static Rectangle GetGroupHeaderBounds(IntPtr list, int groupIndex)
    {
        var groupId = GetNativeGroupId(list, groupIndex);
        var bounds = new NativeRectangle { Top = 1 };
        Require(SendMessage(list, 0x1062, (IntPtr)groupId, ref bounds) != IntPtr.Zero,
            $"Cannot read native header bounds for group {groupIndex}.");
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    private static int GetNativeGroupId(IntPtr list, int groupIndex)
    {
        var group = new NativeListViewGroup
        {
            Size = (uint)Marshal.SizeOf(typeof(NativeListViewGroup)),
            Mask = 0x00000010
        };
        Require(SendMessage(list, 0x1099, (IntPtr)groupIndex, ref group) != IntPtr.Zero,
            $"Cannot read native ID for group {groupIndex}.");
        return group.GroupId;
    }

    private static void RaiseNativeTaskLinkNotification(IntPtr list, int groupId)
    {
        var notification = new NativeListViewLink
        {
            Header = new NativeNotifyHeader { WindowFrom = list, Code = -184 },
            Link = new NativeListItemLink { Id = string.Empty, Url = string.Empty },
            SubItem = groupId
        };
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeListViewLink)));
        try
        {
            Marshal.StructureToPtr(notification, pointer, false);
            SendMessage(list, 0x204E, IntPtr.Zero, pointer);
        }
        finally
        {
            Marshal.DestroyStructure<NativeListViewLink>(pointer);
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static void Click(ListView list, int x, int y)
    {
        var previousPosition = Cursor.Position;
        try
        {
            list.Focus();
            Cursor.Position = list.PointToScreen(new Point(x, y));
            Application.DoEvents();
            MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
            MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
            for (var index = 0; index < 3; index++) Application.DoEvents();
        }
        finally
        {
            Cursor.Position = previousPosition;
        }
    }

    private static int CountPixelDifferences(Bitmap first, Bitmap second, Rectangle region)
    {
        var bounds = Rectangle.Intersect(region, new Rectangle(Point.Empty, first.Size));
        var count = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++)
            if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb()) count++;
        return count;
    }
#endif

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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeNotifyHeader
    {
        internal IntPtr WindowFrom;
        internal UIntPtr IdFrom;
        internal int Code;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeListItemLink
    {
        internal uint Mask;
        internal int LinkIndex;
        internal uint State;
        internal uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)] internal string Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2084)] internal string Url;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeListViewLink
    {
        internal NativeNotifyHeader Header;
        internal NativeListItemLink Link;
        internal int Item;
        internal int SubItem;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeListViewGroup
    {
        internal uint Size;
        internal uint Mask;
        internal IntPtr Header;
        internal int HeaderLength;
        internal IntPtr Footer;
        internal int FooterLength;
        internal int GroupId;
        internal uint StateMask;
        internal uint State;
        internal uint Align;
        internal IntPtr Subtitle;
        internal uint SubtitleLength;
        internal IntPtr Task;
        internal uint TaskLength;
        internal IntPtr DescriptionTop;
        internal uint DescriptionTopLength;
        internal IntPtr DescriptionBottom;
        internal uint DescriptionBottomLength;
        internal int TitleImage;
        internal int ExtendedImage;
        internal int FirstItem;
        internal uint ItemCount;
        internal IntPtr SubsetTitle;
        internal uint SubsetTitleLength;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void MouseEvent(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativeRectangle rectangle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativeListViewGroup group);

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
