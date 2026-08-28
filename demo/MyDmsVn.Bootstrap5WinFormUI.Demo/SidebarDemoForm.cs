using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class SidebarDemoForm : Form
{
    private readonly FlowLayoutPanel _toolbar = new FlowLayoutPanel();
    private readonly Panel _workspace = new Panel();
    private readonly Panel _content = new Panel();
    private readonly BootstrapSidebar _sidebar = new BootstrapSidebar();
    private readonly List<Button> _commandButtons = new List<Button>();
    private readonly Label _selection = new Label();
    private BootstrapSidebarItem? _salesItem;
    private BootstrapSidebarItem? _reportsItem;

    public SidebarDemoForm()
    {
        Text = "BootstrapSidebar Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 700);
        MinimumSize = new Size(760, 520);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureToolbar();
        ConfigureWorkspace();
        ConfigureSidebarItems();

        Controls.Add(_workspace);
        Controls.Add(_toolbar);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        UpdateSelectionCaption();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureToolbar()
    {
        _toolbar.Dock = DockStyle.Top;
        _toolbar.AutoSize = true;
        _toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _toolbar.FlowDirection = FlowDirection.LeftToRight;
        _toolbar.WrapContents = true;
        _toolbar.Padding = new Padding(12, 10, 12, 10);

        AddCommandButton("Toggle sidebar", () => _sidebar.Toggle());
        AddCommandButton("Select Sales", () =>
        {
            if (_salesItem is null || _reportsItem is null)
            {
                return;
            }

            _sidebar.Expand();
            _reportsItem.Expanded = true;
            _sidebar.SelectedItem = _salesItem;
        });

        var note = new Label
        {
            AutoSize = true,
            Margin = new Padding(16, 7, 0, 0),
            Text = "Use Tab / arrows / Enter / Space. Switch theme and Reduced motion from the main demo."
        };
        _toolbar.Controls.Add(note);
    }

    private void ConfigureWorkspace()
    {
        _workspace.Dock = DockStyle.Fill;

        _sidebar.Dock = DockStyle.Left;
        _sidebar.ExpandedWidth = 260;
        _sidebar.CollapsedWidth = 72;
        _sidebar.AnimationDuration = TimeSpan.FromMilliseconds(220);
        _sidebar.SelectedItemChanged += (_, _) => UpdateSelectionCaption();

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(28);

        var title = new Label
        {
            AutoSize = true,
            Font = new Font(_content.Font.FontFamily, 16f, FontStyle.Bold),
            Location = new Point(28, 28),
            Text = "Business workspace"
        };
        _selection.AutoSize = true;
        _selection.Location = new Point(30, 78);
        _selection.Text = "Selected: none";

        var instructions = new Label
        {
            AutoSize = false,
            Location = new Point(30, 118),
            Size = new Size(560, 150),
            Text = "The sidebar demonstrates source-neutral icons, badges, selected and disabled states, " +
                   "nested sections backed by BootstrapCollapse, keyboard navigation, width animation, " +
                   "runtime theme switching, and Reduced motion behavior."
        };

        _content.Controls.Add(title);
        _content.Controls.Add(_selection);
        _content.Controls.Add(instructions);

        _workspace.Controls.Add(_content);
        _workspace.Controls.Add(_sidebar);
    }

    private void ConfigureSidebarItems()
    {
        var home = new BootstrapSidebarItem
        {
            Text = "Home",
            Icon = IconDescriptor.SegoeMdl2('\uE80F')
        };
        var orders = new BootstrapSidebarItem
        {
            Text = "Orders",
            Icon = IconDescriptor.SegoeMdl2('\uE8A5'),
            BadgeText = "12"
        };
        var reports = new BootstrapSidebarItem
        {
            Text = "Reports",
            Icon = IconDescriptor.SegoeMdl2('\uE9D2'),
            Expanded = true
        };
        var sales = new BootstrapSidebarItem
        {
            Text = "Sales",
            Icon = IconDescriptor.SegoeMdl2('\uE9D5')
        };
        var inventory = new BootstrapSidebarItem
        {
            Text = "Inventory",
            Icon = IconDescriptor.SegoeMdl2('\uE7B8'),
            BadgeText = "4"
        };
        var admin = new BootstrapSidebarItem
        {
            Text = "Administration",
            Icon = IconDescriptor.SegoeMdl2('\uE713'),
            Enabled = false
        };

        reports.Items.Add(sales);
        reports.Items.Add(inventory);
        _reportsItem = reports;
        _salesItem = sales;

        _sidebar.Items.Add(home);
        _sidebar.Items.Add(orders);
        _sidebar.Items.Add(reports);
        _sidebar.Items.Add(admin);
        _sidebar.SelectedItem = home;
    }

    private void AddCommandButton(string text, Action action)
    {
        var button = new Button
        {
            AutoSize = true,
            Text = text,
            UseVisualStyleBackColor = false,
            Margin = new Padding(_commandButtons.Count == 0 ? 0 : 8, 0, 0, 0)
        };
        button.Click += (_, _) => action();
        _commandButtons.Add(button);
        _toolbar.Controls.Add(button);
    }

    private void UpdateSelectionCaption()
    {
        _selection.Text = _sidebar.SelectedItem is null
            ? "Selected: none"
            : "Selected: " + _sidebar.SelectedItem.Text;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _workspace.BackColor = theme.Colors.Body;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        _toolbar.BackColor = theme.Colors.SurfaceSecondary;
        _toolbar.ForeColor = theme.Colors.Text;

        foreach (var button in _commandButtons)
        {
            button.BackColor = theme.Colors.Surface;
            button.ForeColor = theme.Colors.Text;
        }

        foreach (Control control in _content.Controls)
        {
            control.ForeColor = theme.Colors.Text;
            control.BackColor = theme.Colors.Body;
        }
    }
}
