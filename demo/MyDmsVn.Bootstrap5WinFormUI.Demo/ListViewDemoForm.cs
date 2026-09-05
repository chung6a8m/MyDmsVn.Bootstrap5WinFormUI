using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ListViewDemoForm : Form
{
    private readonly TabControl _tabs = new TabControl();
    private readonly ImageList _smallImages = new ImageList { ImageSize = new Size(16, 16) };
    private readonly ImageList _largeImages = new ImageList { ImageSize = new Size(32, 32) };
    private readonly ImageList _stateImages = new ImageList { ImageSize = new Size(16, 16) };
    private readonly List<Bitmap> _imageSources = new List<Bitmap>();

    public ListViewDemoForm()
    {
        Text = "BootstrapListView Demo";
        ClientSize = new Size(1100, 760);
        MinimumSize = new Size(780, 540);
        ConfigureImages();
        _tabs.Dock = DockStyle.Fill;
        _tabs.TabPages.Add(CreateDetailsPage());
        _tabs.TabPages.Add(CreateViewsPage());
        _tabs.TabPages.Add(CreateGroupsAndVirtualPage());
        Controls.Add(_tabs);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _smallImages.Dispose();
        _largeImages.Dispose();
        _stateImages.Dispose();
        foreach (var image in _imageSources) image.Dispose();
        _imageSources.Clear();
    }

    private TabPage CreateDetailsPage()
    {
        var page = new TabPage("Details / hover regression");
        var list = CreateDetailsList();
        list.Dock = DockStyle.Fill;
        var controls = CreateToolbar();
        AddToggle(controls, "Full row", list.FullRowSelect, value => list.FullRowSelect = value);
        AddToggle(controls, "Grid lines", list.GridLines, value => list.GridLines = value);
        AddToggle(controls, "Striped", list.Striped, value => list.Striped = value);
        AddToggle(controls, "Checks", list.CheckBoxes, value => list.CheckBoxes = value);
        AddToggle(controls, "Hide selection", list.HideSelection, value => list.HideSelection = value);
        AddVariantSelector(controls, list);
        var note = CreateNote("Win32 regression: repeatedly sweep the pointer across all three columns, selected and neutral rows, with Full row on/off. Subitem text and images must never disappear.");
        page.Controls.Add(list);
        page.Controls.Add(note);
        page.Controls.Add(controls);
        return page;
    }

    private TabPage CreateViewsPage()
    {
        var page = new TabPage("List / icons / tile");
        var list = CreateViewList();
        list.Dock = DockStyle.Fill;
        var controls = CreateToolbar();
        var viewSelector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        foreach (View view in Enum.GetValues(typeof(View))) viewSelector.Items.Add(view);
        viewSelector.SelectedItem = View.List;
        viewSelector.SelectedIndexChanged += (_, _) =>
        {
            if (viewSelector.SelectedItem is View view) list.View = view;
        };
        controls.Controls.Add(new Label { AutoSize = true, Text = "Native view:", Margin = new Padding(0, 8, 6, 0) });
        controls.Controls.Add(viewSelector);
        AddToggle(controls, "Striped (List only)", list.Striped, value => list.Striped = value);
        AddToggle(controls, "Hover", list.HoverHighlight, value => list.HoverHighlight = value);
        AddToggle(controls, "RTL", false, value =>
        {
            list.RightToLeft = value ? RightToLeft.Yes : RightToLeft.No;
            list.RightToLeftLayout = value;
        });
        var note = CreateNote("All item positioning, scrolling, selection, keyboard navigation, image hit regions, and view restrictions remain native. Tile does not receive a framework checkbox fallback.");
        page.Controls.Add(list);
        page.Controls.Add(note);
        page.Controls.Add(controls);
        return page;
    }

    private TabPage CreateGroupsAndVirtualPage()
    {
        var page = new TabPage("Groups / virtual mode");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
        var groups = CreateGroupedList();
        groups.Dock = DockStyle.Fill;
        var groupToolbar = CreateToolbar();
        var groupView = new Button { AutoSize = true, Text = "Toggle Details / List" };
        groupView.Click += (_, _) => groups.View = groups.View == View.Details ? View.List : View.Details;
        groupToolbar.Controls.Add(groupView);
        split.Panel1.Controls.Add(groups);
        split.Panel1.Controls.Add(CreateNote("Group layout and interaction stay native; Bootstrap custom-paints only the header presentation for readable Light/Dark contrast. List view does not display groups."));
        split.Panel1.Controls.Add(groupToolbar);

        var virtualList = new BootstrapListView { Dock = DockStyle.Fill, View = View.Details, VirtualMode = true, FullRowSelect = true };
        virtualList.Columns.Add("Virtual row", 360);
        virtualList.Columns.Add("Status", 160);
        virtualList.RetrieveVirtualItem += (_, e) =>
            e.Item = new ListViewItem(new[] { $"Virtual item {e.ItemIndex:000000}", e.ItemIndex % 2 == 0 ? "Even" : "Odd" });
        virtualList.VirtualListSize = 100000;
        split.Panel2.Controls.Add(virtualList);
        split.Panel2.Controls.Add(CreateNote("Virtual setup order: attach RetrieveVirtualItem before positive VirtualListSize. Normal Items/SelectedItems/CheckedItems are not the virtual access path. Current WinForms rejects VirtualMode while Tile is active."));
        page.Controls.Add(split);
        return page;
    }

    private BootstrapListView CreateDetailsList()
    {
        var list = new BootstrapListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Striped = true,
            CheckBoxes = true,
            SmallImageList = _smallImages,
            MultiSelect = true,
            LabelEdit = true
        };
        list.Columns.Add("Code", 130);
        list.Columns.Add("Name", 360);
        list.Columns.Add("Status", 180, HorizontalAlignment.Center);
        for (var index = 1; index <= 30; index++)
        {
            var item = new ListViewItem(new[] { $"P-{index:000}", $"Business item {index} with a deliberately long label", index % 3 == 0 ? "Review" : "Ready" }, index % 2)
            {
                Checked = index % 4 == 0
            };
            list.Items.Add(item);
        }
        if (list.Items.Count > 2) list.Items[2].Selected = true;
        return list;
    }

    private BootstrapListView CreateViewList()
    {
        var list = new BootstrapListView
        {
            View = View.List,
            Striped = true,
            SmallImageList = _smallImages,
            LargeImageList = _largeImages,
            StateImageList = _stateImages,
            TileSize = new Size(360, 72)
        };
        for (var index = 1; index <= 24; index++)
        {
            var item = new ListViewItem(new[] { $"Item {index}: long native label for ellipsis and wrapping", $"Secondary line {index}" }, index % 2)
            {
                StateImageIndex = index % 2
            };
            list.Items.Add(item);
        }
        return list;
    }

    private BootstrapListView CreateGroupedList()
    {
        var list = new BootstrapListView { View = View.Details, ShowGroups = true, FullRowSelect = true };
        list.Columns.Add("Grouped item", 360);
        var active = new ListViewGroup("Active");
        var archived = new ListViewGroup("Archived");
        list.Groups.AddRange(new[] { active, archived });
        for (var index = 1; index <= 12; index++) list.Items.Add(new ListViewItem($"Grouped item {index}", index <= 7 ? active : archived));
        return list;
    }

    private static FlowLayoutPanel CreateToolbar() => new FlowLayoutPanel
    {
        Dock = DockStyle.Top,
        Height = 42,
        Padding = new Padding(8, 6, 8, 4),
        WrapContents = false,
        AutoScroll = true
    };

    private static Label CreateNote(string text) => new Label
    {
        Dock = DockStyle.Bottom,
        Height = 44,
        Padding = new Padding(8, 5, 8, 5),
        Text = text,
        AutoEllipsis = true
    };

    private static void AddToggle(FlowLayoutPanel toolbar, string text, bool initial, Action<bool> changed)
    {
        var toggle = new CheckBox { AutoSize = true, Text = text, Checked = initial, Margin = new Padding(6, 5, 8, 0) };
        toggle.CheckedChanged += (_, _) => changed(toggle.Checked);
        toolbar.Controls.Add(toggle);
    }

    private static void AddVariantSelector(FlowLayoutPanel toolbar, BootstrapListView list)
    {
        var selector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        foreach (BootstrapVariant variant in Enum.GetValues(typeof(BootstrapVariant))) selector.Items.Add(variant);
        selector.SelectedItem = list.Variant;
        selector.SelectedIndexChanged += (_, _) =>
        {
            if (selector.SelectedItem is BootstrapVariant variant) list.Variant = variant;
        };
        toolbar.Controls.Add(selector);
    }

    private void ConfigureImages()
    {
        AddImage(_smallImages, "blue", 16, Color.SteelBlue);
        AddImage(_smallImages, "green", 16, Color.SeaGreen);
        AddImage(_largeImages, "blue", 32, Color.SteelBlue);
        AddImage(_largeImages, "green", 32, Color.SeaGreen);
        AddImage(_stateImages, "pending", 16, Color.Goldenrod);
        AddImage(_stateImages, "ready", 16, Color.MediumSeaGreen);
    }

    private void AddImage(ImageList list, string key, int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(color))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);
        }
        list.Images.Add(key, bitmap);
        _imageSources.Add(bitmap);
    }
}
