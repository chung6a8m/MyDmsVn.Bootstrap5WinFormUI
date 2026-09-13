using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class RangeDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _diagnostics = new Label();
    private int _scrollCount;
    private int _valueChangedCount;

    public RangeDemoForm()
    {
        Text = "BootstrapRange Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(940, 760);
        MinimumSize = new Size(720, 520);

        ConfigureContent();
        BuildThemeAndGuidanceSection();
        BuildNativeModesSection();
        BuildVariantsSection();
        BuildInteractionSection();
        Controls.Add(_content);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
    }

    private void BuildThemeAndGuidanceSection()
    {
        var group = CreateGroup("Theme and manual interaction checklist");
        var stack = CreateVerticalStack();
        var themeButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var light = new Button { AutoSize = true, Text = "Use Light theme" };
        var dark = new Button { AutoSize = true, Text = "Use Dark theme" };
        light.Click += (_, _) => BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        dark.Click += (_, _) => BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        themeButtons.Controls.Add(light);
        themeButtons.Controls.Add(dark);

        stack.Controls.Add(themeButtons);
        stack.Controls.Add(CreateNote("Keyboard: Tab/Shift+Tab, arrows, Home/End, and PageUp/PageDown. Mouse: channel clicks and thumb drag."));
        stack.Controls.Add(CreateNote("DPI check: resize at real Windows 100%, 150%, and 200% scaling; rail, thumb, focus halo, and ticks must not clip."));
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void BuildNativeModesSection()
    {
        var group = CreateGroup("Native TrackBar modes");
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 3,
            Dock = DockStyle.Top,
            Margin = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        AddScenario(grid, 0, 0, "Bootstrap-like / no ticks", new BootstrapRange
        {
            AccessibleName = "Horizontal range without ticks",
            TickStyle = TickStyle.None,
            Maximum = 100,
            Value = 35
        });
        AddScenario(grid, 1, 0, "Horizontal / native ticks", new BootstrapRange
        {
            AccessibleName = "Horizontal ticked range",
            TickStyle = TickStyle.BottomRight,
            TickFrequency = 10,
            Maximum = 100,
            Value = 55
        });
        AddScenario(grid, 0, 1, "Disabled", new BootstrapRange
        {
            AccessibleName = "Disabled range",
            TickStyle = TickStyle.None,
            Maximum = 100,
            Value = 65,
            Enabled = false
        });
        AddScenario(grid, 1, 1, "RTL + RightToLeftLayout", new BootstrapRange
        {
            AccessibleName = "Right-to-left range",
            TickStyle = TickStyle.Both,
            TickFrequency = 20,
            Maximum = 100,
            Value = 25,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true
        });
        AddScenario(grid, 0, 2, "Vertical", new BootstrapRange
        {
            AccessibleName = "Vertical range",
            Orientation = Orientation.Vertical,
            TickStyle = TickStyle.Both,
            TickFrequency = 10,
            Maximum = 100,
            Value = 45,
            AutoSize = false,
            Size = new Size(80, 190)
        });

        group.Controls.Add(grid);
        _content.Controls.Add(group);
    }

    private void BuildVariantsSection()
    {
        var group = CreateGroup("Semantic variants");
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 4,
            Dock = DockStyle.Top,
            Margin = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        var variants = (BootstrapVariant[])Enum.GetValues(typeof(BootstrapVariant));
        for (var index = 0; index < variants.Length; index++)
        {
            var variant = variants[index];
            AddScenario(grid, index % 2, index / 2, variant.ToString(), new BootstrapRange
            {
                AccessibleName = $"{variant} range",
                TickStyle = TickStyle.None,
                Variant = variant,
                Maximum = 100,
                Value = 20 + (index * 8)
            });
        }

        group.Controls.Add(grid);
        _content.Controls.Add(group);
    }

    private void BuildInteractionSection()
    {
        var group = CreateGroup("Live native event diagnostics");
        var stack = CreateVerticalStack();
        var range = new BootstrapRange
        {
            AccessibleName = "Interactive range diagnostics",
            Width = 560,
            TickStyle = TickStyle.BottomRight,
            TickFrequency = 10,
            SmallChange = 1,
            LargeChange = 10,
            Maximum = 100,
            Value = 40
        };
        _diagnostics.AutoSize = true;
        _diagnostics.AccessibleName = "Range event diagnostics";
        range.Scroll += (_, _) =>
        {
            _scrollCount++;
            UpdateDiagnostics(range);
        };
        range.ValueChanged += (_, _) =>
        {
            _valueChangedCount++;
            UpdateDiagnostics(range);
        };
        UpdateDiagnostics(range);
        stack.Controls.Add(range);
        stack.Controls.Add(_diagnostics);
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private static GroupBox CreateGroup(string text) => new GroupBox
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(860, 0),
        Margin = new Padding(0, 0, 0, 14),
        Padding = new Padding(14)
    };

    private static FlowLayoutPanel CreateVerticalStack() => new FlowLayoutPanel
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Dock = DockStyle.Top,
        Margin = Padding.Empty
    };

    private static Label CreateNote(string text) => new Label
    {
        AutoSize = true,
        MaximumSize = new Size(800, 0),
        Text = text,
        Margin = new Padding(0, 6, 0, 0)
    };

    private static void AddScenario(TableLayoutPanel grid, int column, int row, string caption, BootstrapRange range)
    {
        var panel = CreateVerticalStack();
        panel.Margin = new Padding(0, 0, 18, 10);
        panel.MinimumSize = new Size(390, range.Orientation == Orientation.Vertical ? 230 : 0);
        panel.Controls.Add(new Label { AutoSize = true, Text = caption });
        if (range.Orientation == Orientation.Horizontal)
        {
            range.Width = 360;
        }

        range.Margin = new Padding(0, 6, 0, 0);
        panel.Controls.Add(range);
        grid.Controls.Add(panel, column, row);
    }

    private void UpdateDiagnostics(BootstrapRange range)
    {
        _diagnostics.Text = $"Value: {range.Value} | Scroll: {_scrollCount} | ValueChanged: {_valueChangedCount}";
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        ApplyThemeRecursive(_content, theme);
    }

    private static void ApplyThemeRecursive(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is GroupBox || child is Label || child is FlowLayoutPanel || child is TableLayoutPanel)
            {
                child.BackColor = theme.Colors.Surface;
                child.ForeColor = theme.Colors.Text;
            }

            ApplyThemeRecursive(child, theme);
        }
    }
}
