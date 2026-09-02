using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class InputGroupDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();

    public InputGroupDemoForm()
    {
        Text = "BootstrapInputGroup Demo";
        ClientSize = new Size(980, 820);
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
        Controls.Add(_content);

        Add("Prefix", CreateGroup(new BootstrapInputGroupText { Text = "@" }, new BootstrapTextBox { PlaceholderText = "Username" }));
        Add("Suffix", CreateGroup(new BootstrapTextBox { PlaceholderText = "Username" }, new BootstrapInputGroupText { Text = "@example.com" }));
        Add("Currency", CreateGroup(new BootstrapInputGroupText { Text = "$" }, new BootstrapNumericBox(), new BootstrapInputGroupText { Text = ".00" }));
        Add("Two inputs", CreateGroup(new BootstrapInputGroupText { Text = "Name" }, new BootstrapTextBox { PlaceholderText = "First" }, new BootstrapTextBox { PlaceholderText = "Last" }));
        Add("Search", CreateGroup(new BootstrapTextBox { PlaceholderText = "Search" }, new BootstrapButton { Text = "Search" }));
        Add("Multiple buttons", CreateGroup(new BootstrapTextBox(), new BootstrapButton { Text = "Save" }, new BootstrapButton { Text = "Clear", Outline = true }));

        var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Single };
        select.Items.Add(new BootstrapSelectItem(1, "Active"));
        select.Items.Add(new BootstrapSelectItem(2, "Archived"));
        Add("Single Select", CreateGroup(new BootstrapInputGroupText { Text = "Status" }, select));

        Add("Formatted", CreateGroup(new BootstrapInputGroupText { Text = "Card" }, new BootstrapFormattedTextBox()));
        var split = new BootstrapSplitButton { Text = "Save" };
        split.Items.Add(new BootstrapDropdownItem { Text = "Save as" });
        Add("Split button", CreateGroup(new BootstrapTextBox(), split));

        Add("Small", CreateGroup(BootstrapInputGroupSize.Small, new BootstrapInputGroupText { Text = "Small" }, new BootstrapTextBox()));
        Add("Default", CreateGroup(BootstrapInputGroupSize.Default, new BootstrapInputGroupText { Text = "Default" }, new BootstrapTextBox()));
        Add("Large", CreateGroup(BootstrapInputGroupSize.Large, new BootstrapInputGroupText { Text = "Large" }, new BootstrapTextBox()));

        Add("Validation / disabled", CreateGroup(
            new BootstrapTextBox { ValidationState = BootstrapValidationState.Valid, Text = "Valid" },
            new BootstrapTextBox { ValidationState = BootstrapValidationState.Invalid, Text = "Invalid" },
            new BootstrapButton { Text = "Disabled", Enabled = false }));

        var toggleAddon = new BootstrapInputGroupText { Text = "Middle" };
        var visibilityGroup = CreateGroup(new BootstrapInputGroupText { Text = "First" }, toggleAddon, new BootstrapTextBox());
        var toggle = new Button { Text = "Hide/show middle addon", AutoSize = true };
        toggle.Click += (_, _) => toggleAddon.Visible = !toggleAddon.Visible;
        Add("Visibility recomputation", visibilityGroup, toggle);

        var reorderGroup = CreateGroup(new BootstrapInputGroupText { Text = "A" }, new BootstrapInputGroupText { Text = "B" }, new BootstrapTextBox());
        var reorder = new Button { Text = "Move last to first", AutoSize = true };
        reorder.Click += (_, _) => reorderGroup.Controls.SetChildIndex(reorderGroup.Controls[reorderGroup.Controls.Count - 1], 0);
        Add("Runtime reorder", reorderGroup, reorder);

        var narrow = CreateGroup(new BootstrapInputGroupText { Text = "Narrow" }, new BootstrapTextBox(), new BootstrapButton { Text = "Go" });
        narrow.Width = 180;
        Add("Deterministic narrow compression", narrow);

        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(860, 0),
            Text = "Manual: Tab/Shift+Tab through children; use Enter/Space on buttons; inspect hover/pressed/focus seams, reorder and hide/show; switch Light/Dark and repeat at 100–200% DPI."
        };
        _content.Controls.Add(instructions);
    }

    private void Add(string title, BootstrapInputGroup group, Control? action = null)
    {
        var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        row.Controls.Add(new Label { Text = title, Width = 170, TextAlign = ContentAlignment.MiddleLeft, Height = 40 });
        row.Controls.Add(group);
        if (action is not null) row.Controls.Add(action);
        _content.Controls.Add(row);
    }

    private static BootstrapInputGroup CreateGroup(params Control[] children) => CreateGroup(BootstrapInputGroupSize.Default, children);

    private static BootstrapInputGroup CreateGroup(BootstrapInputGroupSize size, params Control[] children)
    {
        var group = new BootstrapInputGroup { Width = 650, Height = 40, InputGroupSize = size, Margin = Padding.Empty };
        group.Controls.AddRange(children);
        return group;
    }
}
