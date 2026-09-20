using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>Demonstrates short composed List Group content and interaction.</summary>
public sealed class ListGroupDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();

    /// <summary>Initializes all List Group scenarios on one scrollable page.</summary>
    public ListGroupDemoForm()
    {
        Text = "BootstrapListGroup Demo";
        ClientSize = new Size(1000, 760);
        MinimumSize = new Size(760, 520);
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
        Controls.Add(_content);

        AddScenario("Basic", CreateBasicGroup(), "Short static composition; use ListView/DataGridView for large data.");
        AddScenario("Active + disabled", CreateStateGroup(), "Active is explicit application state and clicks do not select automatically.");
        AddScenario("Actionable settings navigation", CreateSettingsGroup(), "Tab, Up/Down, Enter and Space use the item surface.");
        AddScenario("Reorder", CreateReorderScenario(), "Controls.SetChildIndex immediately changes layout and keyboard order.");
        AddScenario("Contextual variants", CreateVariantsGroup(), "All shared semantic variants remain theme-aware.");
        AddScenario("Rich content", CreateRichGroup(), "Labels and badges forward activation; the embedded button keeps its own click.");
        AddScenario("Flush", CreateFlushGroup(), "Vertical flush removes outer rounding while keeping separators.");
        AddScenario("Horizontal", CreateHorizontalGroup(), "Left/Right navigation; Flush remains stored but is a visual no-op in V1.");
    }

    private static BootstrapListGroup CreateBasicGroup()
    {
        var group = CreateGroup();
        group.AddItem("First static item");
        group.AddItem("Second static item");
        group.AddItem("Third static item");
        return group;
    }

    private static BootstrapListGroup CreateStateGroup()
    {
        var group = CreateGroup();
        group.AddItem("Explicitly active").Active = true;
        var actionable = group.AddItem("Actionable, but clicking does not change Active");
        actionable.Actionable = true;
        var disabled = group.AddItem("Disabled item");
        disabled.Actionable = true;
        disabled.Enabled = false;
        return group;
    }

    private static BootstrapListGroup CreateSettingsGroup()
    {
        var group = CreateGroup();
        var status = new Label { AutoSize = true, Text = "No setting activated", AccessibleName = "List group activation status" };
        foreach (var text in new[] { "Profile", "Security", "Notifications" })
        {
            group.AddItem(text).Actionable = true;
        }
        group.ItemClick += (_, e) => status.Text = "Activated: " + e.Item.Text;
        group.Tag = status;
        return group;
    }

    private static Control CreateReorderScenario()
    {
        var panel = new Panel { Width = 760, Height = 180 };
        var group = CreateGroup();
        group.AccessibleName = "Reorder list group";
        group.AddItem("Alpha").Actionable = true;
        group.AddItem("Bravo").Actionable = true;
        group.AddItem("Charlie").Actionable = true;
        var button = new Button { AutoSize = true, Text = "Move last item first", Dock = DockStyle.Bottom };
        button.Click += (_, _) =>
        {
            if (group.Items.Count > 1) group.Controls.SetChildIndex(group.Items[group.Items.Count - 1], 0);
            group.PerformLayout();
        };
        group.Dock = DockStyle.Top;
        panel.Controls.Add(button);
        panel.Controls.Add(group);
        return panel;
    }

    private static BootstrapListGroup CreateVariantsGroup()
    {
        var group = CreateGroup();
        foreach (BootstrapVariant variant in Enum.GetValues(typeof(BootstrapVariant)))
        {
            group.AddItem(variant.ToString()).Variant = variant;
        }
        return group;
    }

    private static BootstrapListGroup CreateRichGroup()
    {
        var group = CreateGroup();
        var item = new BootstrapListGroupItem { Actionable = true, Height = 66 };
        var heading = new Label { AutoSize = true, Text = "Build notifications", Location = new Point(12, 8) };
        var description = new Label { AutoSize = true, Text = "Decorative text activates the row.", Location = new Point(12, 30) };
        var badge = new BootstrapBadge { Text = "3", Variant = BootstrapVariant.Info, Location = new Point(320, 15) };
        var button = new Button { AutoSize = true, Text = "Details", Location = new Point(400, 12) };
        item.Controls.Add(heading);
        item.Controls.Add(description);
        item.Controls.Add(badge);
        item.Controls.Add(button);
        group.AddItem(item);
        return group;
    }

    private static BootstrapListGroup CreateFlushGroup()
    {
        var group = CreateGroup();
        group.Flush = true;
        group.AddItem("Flush first");
        group.AddItem("Flush middle");
        group.AddItem("Flush last");
        return group;
    }

    private static BootstrapListGroup CreateHorizontalGroup()
    {
        var group = CreateGroup();
        group.Orientation = Orientation.Horizontal;
        group.Flush = true;
        foreach (var text in new[] { "Left", "Center", "Right" }) group.AddItem(text).Actionable = true;
        return group;
    }

    private static BootstrapListGroup CreateGroup() => new BootstrapListGroup { Width = 720 };

    private void AddScenario(string title, Control content, string note)
    {
        var panel = new Panel { Width = 900, Height = Math.Max(150, content.PreferredSize.Height + 72), Margin = new Padding(0, 0, 0, 14) };
        var heading = new Label { AutoSize = true, Text = title, Location = new Point(0, 0) };
        var description = new Label { AutoSize = true, Text = note, Location = new Point(0, 24) };
        content.Location = new Point(0, 50);
        content.Width = Math.Min(820, panel.Width);
        panel.Controls.Add(content);
        panel.Controls.Add(description);
        panel.Controls.Add(heading);
        if (content is BootstrapListGroup group && group.Tag is Label status)
        {
            status.Location = new Point(0, content.Bottom + 6);
            panel.Controls.Add(status);
            panel.Height = Math.Max(panel.Height, status.Bottom + 8);
        }
        _content.Controls.Add(panel);
    }
}
