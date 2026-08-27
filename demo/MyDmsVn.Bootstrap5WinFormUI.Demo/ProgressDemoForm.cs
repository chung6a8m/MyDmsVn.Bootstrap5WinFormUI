using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ProgressDemoForm : Form
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
    private readonly List<Button> _commandButtons = new List<Button>();
    private readonly BootstrapProgressBar _interactiveProgress = new BootstrapProgressBar();

    public ProgressDemoForm()
    {
        Text = "BootstrapProgressBar Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 760);
        MinimumSize = new Size(700, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureToolbar();
        ConfigureContent();

        Controls.Add(_content);
        Controls.Add(_toolbar);

        AddVariantSection();
        AddTextAndCustomColorSection();
        AddStripeSection();
        AddIndeterminateSection();
        AddInteractiveSection();

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

    private void ConfigureToolbar()
    {
        _toolbar.Dock = DockStyle.Top;
        _toolbar.AutoSize = true;
        _toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _toolbar.FlowDirection = FlowDirection.LeftToRight;
        _toolbar.WrapContents = true;
        _toolbar.Padding = new Padding(12, 10, 12, 10);

        AddCommandButton("25%", () => _interactiveProgress.AnimateTo(25));
        AddCommandButton("75%", () => _interactiveProgress.AnimateTo(75));
        AddCommandButton("Complete", () => _interactiveProgress.AnimateTo(100));
        AddCommandButton("Reset", () => _interactiveProgress.AnimateTo(0));

        var note = new Label
        {
            AutoSize = true,
            Margin = new Padding(16, 7, 0, 0),
            Text = "Switch theme / Reduced motion from the main demo while this window is open."
        };
        _toolbar.Controls.Add(note);
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(12);
    }

    private void AddVariantSection()
    {
        var group = CreateGroup("Semantic variants");
        var stack = (FlowLayoutPanel)group.Controls[0];
        var value = 20;

        foreach (var variant in Variants)
        {
            stack.Controls.Add(CreateProgressRow(
                variant.ToString(),
                new BootstrapProgressBar
                {
                    Width = 760,
                    Height = 20,
                    Value = value,
                    Variant = variant,
                    AccessibleName = $"{variant} progress"
                }));
            value = Math.Min(95, value + 10);
        }

        _content.Controls.Add(group);
    }

    private void AddTextAndCustomColorSection()
    {
        var group = CreateGroup("Text format, custom color, and radius");
        var stack = (FlowLayoutPanel)group.Controls[0];

        stack.Controls.Add(CreateProgressRow(
            "Formatted text",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 26,
                Value = 64,
                Variant = BootstrapVariant.Success,
                ShowText = true,
                TextFormat = "{1} / {3} ({0}%)",
                AccessibleName = "Formatted progress"
            }));

        stack.Controls.Add(CreateProgressRow(
            "Custom color",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 22,
                Value = 72,
                Variant = BootstrapVariant.Primary,
                CustomColor = Color.FromArgb(111, 66, 193),
                AccessibleName = "Custom color progress"
            }));

        stack.Controls.Add(CreateProgressRow(
            "Square radius",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 20,
                Value = 48,
                Variant = BootstrapVariant.Info,
                BorderRadius = 0,
                AccessibleName = "Square radius progress"
            }));

        _content.Controls.Add(group);
    }

    private void AddStripeSection()
    {
        var group = CreateGroup("Striped progress");
        var stack = (FlowLayoutPanel)group.Controls[0];

        stack.Controls.Add(CreateProgressRow(
            "Static stripes",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 22,
                Value = 58,
                Variant = BootstrapVariant.Warning,
                Striped = true,
                AccessibleName = "Static striped progress"
            }));

        stack.Controls.Add(CreateProgressRow(
            "Animated stripes",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 22,
                Value = 68,
                Variant = BootstrapVariant.Primary,
                Striped = true,
                Animated = true,
                AccessibleName = "Animated striped progress"
            }));

        _content.Controls.Add(group);
    }

    private void AddIndeterminateSection()
    {
        var group = CreateGroup("Indeterminate");
        var stack = (FlowLayoutPanel)group.Controls[0];

        stack.Controls.Add(CreateProgressRow(
            "Moving segment",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 22,
                Variant = BootstrapVariant.Info,
                Indeterminate = true,
                AccessibleName = "Indeterminate progress"
            }));

        stack.Controls.Add(CreateProgressRow(
            "Striped segment",
            new BootstrapProgressBar
            {
                Width = 760,
                Height = 22,
                Variant = BootstrapVariant.Danger,
                Indeterminate = true,
                Striped = true,
                Animated = true,
                AccessibleName = "Striped indeterminate progress"
            }));

        _content.Controls.Add(group);
    }

    private void AddInteractiveSection()
    {
        var group = CreateGroup("AnimateTo interaction");
        var stack = (FlowLayoutPanel)group.Controls[0];

        _interactiveProgress.Width = 760;
        _interactiveProgress.Height = 26;
        _interactiveProgress.Value = 35;
        _interactiveProgress.Variant = BootstrapVariant.Primary;
        _interactiveProgress.Striped = true;
        _interactiveProgress.Animated = true;
        _interactiveProgress.ShowText = true;
        _interactiveProgress.TextFormat = "{0}%";
        _interactiveProgress.AccessibleName = "Interactive progress";

        stack.Controls.Add(CreateCaption("Use the command bar above to exercise smooth finite transitions."));
        stack.Controls.Add(_interactiveProgress);
        _content.Controls.Add(group);
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

    private static GroupBox CreateGroup(string text)
    {
        var group = new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 850,
            MinimumSize = new Size(850, 0),
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

    private static FlowLayoutPanel CreateProgressRow(string labelText, BootstrapProgressBar progress)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 8)
        };

        row.Controls.Add(CreateCaption(labelText));
        progress.Margin = new Padding(3, 0, 3, 3);
        row.Controls.Add(progress);
        return row;
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(3, 2, 3, 3)
        };
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _toolbar.BackColor = theme.Colors.SurfaceSecondary;
        _toolbar.ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;

        foreach (var button in _commandButtons)
        {
            button.BackColor = theme.Colors.Surface;
            button.ForeColor = theme.Colors.Text;
        }

        ApplyThemeToChildren(_toolbar, theme, theme.Colors.SurfaceSecondary);
        ApplyThemeToChildren(_content, theme, theme.Colors.Body);
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme, Color background)
    {
        foreach (Control child in root.Controls)
        {
            if (child is not BootstrapProgressBar && child is not Button)
            {
                child.ForeColor = theme.Colors.Text;
                if (child is GroupBox || child is FlowLayoutPanel || child is Label)
                {
                    child.BackColor = background;
                }
            }

            if (child.HasChildren)
            {
                ApplyThemeToChildren(child, theme, background);
            }
        }
    }
}
