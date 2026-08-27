using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ButtonDemoForm : Form
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

    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _status = new Label();

    public ButtonDemoForm()
    {
        Text = "BootstrapButton Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 720);
        MinimumSize = new Size(720, 540);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        ConfigureStatus();
        Controls.Add(_content);
        Controls.Add(_status);

        AddVariantSection(outline: false);
        AddVariantSection(outline: true);
        AddSizeAndIconSection();
        AddStateSection();
        AddLoadingSection();

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
        _content.Padding = new Padding(12);
    }

    private void ConfigureStatus()
    {
        _status.Dock = DockStyle.Bottom;
        _status.Height = 46;
        _status.Padding = new Padding(12, 6, 12, 6);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = "Hover, press, Tab-focus, and activate buttons with Enter/Space.";
    }

    private void AddVariantSection(bool outline)
    {
        var group = CreateGroup(outline ? "Outline variants" : "Filled variants");
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top
        };

        foreach (var variant in Variants)
        {
            var button = CreateButton(variant.ToString(), variant);
            button.Outline = outline;
            row.Controls.Add(button);
        }

        group.Controls.Add(row);
        _content.Controls.Add(group);
    }

    private void AddSizeAndIconSection()
    {
        var group = CreateGroup("Sizes and icons");
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top
        };

        var small = CreateButton("Small", BootstrapVariant.Primary);
        small.ButtonSize = BootstrapButtonSize.Small;

        var normal = CreateButton("Default", BootstrapVariant.Success);
        normal.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check);

        var large = CreateButton("Add item", BootstrapVariant.Info);
        large.ButtonSize = BootstrapButtonSize.Large;
        large.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus);

        var rightIcon = CreateButton("Continue", BootstrapVariant.Secondary);
        rightIcon.Icon = IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown);
        rightIcon.IconPosition = BootstrapIconPosition.Right;

        row.Controls.Add(small);
        row.Controls.Add(normal);
        row.Controls.Add(large);
        row.Controls.Add(rightIcon);
        group.Controls.Add(row);
        _content.Controls.Add(group);
    }

    private void AddStateSection()
    {
        var group = CreateGroup("Selected, disabled, and radius states");
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top
        };

        var selected = CreateButton("Selected", BootstrapVariant.Primary);
        selected.Outline = true;
        selected.Selected = true;

        var disabled = CreateButton("Disabled", BootstrapVariant.Secondary);
        disabled.Enabled = false;

        var square = CreateButton("Square", BootstrapVariant.Dark);
        square.BorderRadius = 0;

        var pillLike = CreateButton("Radius 16", BootstrapVariant.Success);
        pillLike.BorderRadius = 16;

        row.Controls.Add(selected);
        row.Controls.Add(disabled);
        row.Controls.Add(square);
        row.Controls.Add(pillLike);
        group.Controls.Add(row);
        _content.Controls.Add(group);
    }

    private void AddLoadingSection()
    {
        var group = CreateGroup("Loading — spinner infrastructure reuse");
        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
        };

        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "The async demo keeps the same preferred size while Loading=true and suppresses repeat clicks.",
            Margin = new Padding(3, 0, 3, 6)
        });

        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        var loading = CreateButton("Save", BootstrapVariant.Primary);
        loading.LoadingText = "Saving...";
        loading.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        loading.Click += async (_, _) => await SimulateLoadingAsync(loading);

        var staticLoading = CreateButton("Refresh", BootstrapVariant.Info);
        staticLoading.LoadingText = "Refreshing...";
        staticLoading.Loading = true;

        row.Controls.Add(loading);
        row.Controls.Add(staticLoading);
        stack.Controls.Add(row);
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private async Task SimulateLoadingAsync(BootstrapButton button)
    {
        var before = button.Size;
        button.Loading = true;
        _status.Text = $"Loading started · size {before.Width}×{before.Height}px · repeat activation suppressed";
        try
        {
            await Task.Delay(1200);
        }
        finally
        {
            button.Loading = false;
        }

        var after = button.Size;
        _status.Text = $"Loading completed · size preserved: {before == after} ({after.Width}×{after.Height}px)";
    }

    private BootstrapButton CreateButton(string text, BootstrapVariant variant)
    {
        var button = new BootstrapButton
        {
            AutoSize = true,
            Text = text,
            Variant = variant,
            Margin = new Padding(4),
            AccessibleName = $"{text} Bootstrap button"
        };

        button.Click += (_, _) =>
        {
            if (!button.Loading)
            {
                _status.Text = $"Activated: {button.Text} · {button.Variant} · Outline={button.Outline}";
            }
        };
        return button;
    }

    private static GroupBox CreateGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 850,
            MinimumSize = new Size(850, 0),
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(12)
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
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        _status.BackColor = theme.Colors.SurfaceSecondary;
        _status.ForeColor = theme.Colors.Text;
        ApplyThemeToChildren(_content, theme);
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is not BootstrapButton && child is not BootstrapSpinner)
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
}
