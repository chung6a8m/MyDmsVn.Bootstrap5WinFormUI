using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class SpinnerDemoForm : Form
{
    private static readonly BootstrapVariant[] Variants =
    {
        BootstrapVariant.Primary,
        BootstrapVariant.Secondary,
        BootstrapVariant.Success,
        BootstrapVariant.Danger,
        BootstrapVariant.Warning,
        BootstrapVariant.Info,
        BootstrapVariant.Light,
        BootstrapVariant.Dark
    };

    private readonly FlowLayoutPanel _toolbar = new FlowLayoutPanel();
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Button _startAll = new Button();
    private readonly Button _stopAll = new Button();
    private readonly Label _status = new Label();
    private readonly List<BootstrapSpinner> _spinners = new List<BootstrapSpinner>();

    public SpinnerDemoForm()
    {
        Text = "BootstrapSpinner Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(880, 650);
        MinimumSize = new Size(680, 500);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureToolbar();
        ConfigureContent();

        Controls.Add(_content);
        Controls.Add(_toolbar);

        AddModeSection(BootstrapSpinnerType.Border);
        AddModeSection(BootstrapSpinnerType.Grow);
        AddCustomColorSection();

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        UpdateStatus();
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
        _toolbar.WrapContents = false;
        _toolbar.Padding = new Padding(12, 10, 12, 10);

        _startAll.AutoSize = true;
        _startAll.Text = "Start all";
        _startAll.UseVisualStyleBackColor = false;
        _startAll.Click += (_, _) =>
        {
            foreach (var spinner in _spinners)
            {
                spinner.Start();
            }

            UpdateStatus();
        };

        _stopAll.AutoSize = true;
        _stopAll.Margin = new Padding(8, 0, 0, 0);
        _stopAll.Text = "Stop all";
        _stopAll.UseVisualStyleBackColor = false;
        _stopAll.Click += (_, _) =>
        {
            foreach (var spinner in _spinners)
            {
                spinner.Stop();
            }

            UpdateStatus();
        };

        _status.AutoSize = true;
        _status.Margin = new Padding(18, 6, 0, 0);

        _toolbar.Controls.Add(_startAll);
        _toolbar.Controls.Add(_stopAll);
        _toolbar.Controls.Add(_status);
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(12);
    }

    private void AddModeSection(BootstrapSpinnerType type)
    {
        var group = CreateGroup(type == BootstrapSpinnerType.Border ? "Border mode" : "Grow mode");
        var stack = (FlowLayoutPanel)group.Controls[0];

        stack.Controls.Add(CreateCaption("Sizes — Primary"));
        stack.Controls.Add(CreateSpinnerRow(new[]
        {
            CreateSpinner(type, BootstrapSpinnerSize.Small, BootstrapVariant.Primary),
            CreateSpinner(type, BootstrapSpinnerSize.Default, BootstrapVariant.Primary),
            CreateSpinner(type, BootstrapSpinnerSize.Large, BootstrapVariant.Primary)
        }, new[] { "Small", "Default", "Large" }));

        stack.Controls.Add(CreateCaption("Semantic variants — Default size"));
        var variantSpinners = new List<BootstrapSpinner>();
        var names = new List<string>();
        foreach (var variant in Variants)
        {
            variantSpinners.Add(CreateSpinner(type, BootstrapSpinnerSize.Default, variant));
            names.Add(variant.ToString());
        }

        stack.Controls.Add(CreateSpinnerRow(variantSpinners, names));
        _content.Controls.Add(group);
    }

    private void AddCustomColorSection()
    {
        var group = CreateGroup("Custom color override");
        var stack = (FlowLayoutPanel)group.Controls[0];
        stack.Controls.Add(CreateCaption("CustomColor overrides the semantic Variant without changing animation infrastructure."));

        var border = CreateSpinner(BootstrapSpinnerType.Border, BootstrapSpinnerSize.Large, BootstrapVariant.Primary);
        var grow = CreateSpinner(BootstrapSpinnerType.Grow, BootstrapSpinnerSize.Large, BootstrapVariant.Primary);
        var custom = Color.FromArgb(111, 66, 193);
        border.CustomColor = custom;
        grow.CustomColor = custom;

        stack.Controls.Add(CreateSpinnerRow(
            new[] { border, grow },
            new[] { "Border custom", "Grow custom" }));
        _content.Controls.Add(group);
    }

    private GroupBox CreateGroup(string text)
    {
        var group = new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 820,
            MinimumSize = new Size(820, 0),
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(12)
        };

        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
        };

        group.Controls.Add(stack);
        return group;
    }

    private BootstrapSpinner CreateSpinner(
        BootstrapSpinnerType type,
        BootstrapSpinnerSize size,
        BootstrapVariant variant)
    {
        var spinner = new BootstrapSpinner
        {
            Type = type,
            SpinnerSize = size,
            Variant = variant,
            Margin = new Padding(10, 6, 10, 2),
            AccessibleName = $"{variant} {type} spinner"
        };

        _spinners.Add(spinner);
        return spinner;
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(3, 8, 3, 4)
        };
    }

    private static FlowLayoutPanel CreateSpinnerRow(
        IEnumerable<BootstrapSpinner> spinners,
        IEnumerable<string> names)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        using var spinnerEnumerator = spinners.GetEnumerator();
        using var nameEnumerator = names.GetEnumerator();
        while (spinnerEnumerator.MoveNext() && nameEnumerator.MoveNext())
        {
            var cell = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 10, 0),
                MinimumSize = new Size(86, 68)
            };

            var label = new Label
            {
                AutoSize = true,
                Text = nameEnumerator.Current,
                Margin = new Padding(4, 2, 4, 2)
            };

            cell.Controls.Add(spinnerEnumerator.Current);
            cell.Controls.Add(label);
            row.Controls.Add(cell);
        }

        return row;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
        UpdateStatus();
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _toolbar.BackColor = theme.Colors.SurfaceSecondary;
        _toolbar.ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        _startAll.BackColor = theme.Colors.Surface;
        _startAll.ForeColor = theme.Colors.Text;
        _stopAll.BackColor = theme.Colors.Surface;
        _stopAll.ForeColor = theme.Colors.Text;
        _status.BackColor = theme.Colors.SurfaceSecondary;
        _status.ForeColor = theme.Colors.Text;

        ApplyThemeToChildren(_content, theme);
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is not BootstrapSpinner)
            {
                child.ForeColor = theme.Colors.Text;
                if (child is GroupBox || child is FlowLayoutPanel || child is Label)
                {
                    child.BackColor = theme.Colors.Body;
                }
            }

            if (child.HasChildren)
            {
                ApplyThemeToChildren(child, theme);
            }
        }
    }

    private void UpdateStatus()
    {
        var active = 0;
        foreach (var spinner in _spinners)
        {
            if (spinner.Spinning)
            {
                active++;
            }
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        _status.Text = $"Active: {active}/{_spinners.Count} · {theme.Mode} · Reduced motion: {theme.ReducedMotion}";
    }
}
