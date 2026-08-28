using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class FeedbackDemoForm : Form
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

    public FeedbackDemoForm()
    {
        Text = "Feedback Components Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 620);
        MinimumSize = new Size(640, 420);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        Controls.Add(_content);

        AddSemanticVariantsSection();
        AddShapeAndStateSection();
        AddDpiGuidanceSection();

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

    private void AddSemanticVariantsSection()
    {
        var group = CreateGroup("Semantic variants");
        var row = CreateBadgeRow();

        foreach (var variant in Variants)
        {
            row.Controls.Add(new BootstrapBadge
            {
                Text = variant.ToString(),
                Variant = variant,
                AccessibleName = $"{variant} badge"
            });
        }

        group.Controls.Add(row);
        _content.Controls.Add(group);
    }

    private void AddShapeAndStateSection()
    {
        var group = CreateGroup("Shape, custom color, disabled, and content length");
        var stack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
        };

        var shapeRow = CreateBadgeRow();
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Default",
            Variant = BootstrapVariant.Primary,
            AccessibleName = "Default badge"
        });
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Pill",
            Variant = BootstrapVariant.Success,
            Pill = true,
            AccessibleName = "Pill badge"
        });
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Custom color",
            Variant = BootstrapVariant.Danger,
            CustomColor = Color.FromArgb(111, 66, 193),
            AccessibleName = "Custom color badge"
        });
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Disabled",
            Variant = BootstrapVariant.Secondary,
            Enabled = false,
            AccessibleName = "Disabled badge"
        });
        shapeRow.Controls.Add(new BootstrapBadge
        {
            Text = "Square radius",
            Variant = BootstrapVariant.Info,
            BorderRadius = 0,
            AccessibleName = "Square radius badge"
        });

        var longRow = CreateBadgeRow();
        longRow.Controls.Add(new BootstrapBadge
        {
            Text = "A long badge label verifies auto-size content measurement",
            Variant = BootstrapVariant.Warning,
            AccessibleName = "Long text badge"
        });

        stack.Controls.Add(shapeRow);
        stack.Controls.Add(longRow);
        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void AddDpiGuidanceSection()
    {
        var group = CreateGroup("Theme and DPI verification");
        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(800, 0),
            Text = "Use the integrated demo's Light/Dark switch while this page is open. Repeat this page at Windows display scaling 100%, 125%, 150%, 175%, and 200% to verify padding, text clipping, and rounded geometry.",
            Margin = new Padding(3, 4, 3, 8)
        };
        group.Controls.Add(note);
        _content.Controls.Add(group);
    }

    private static GroupBox CreateGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 830,
            MinimumSize = new Size(830, 0),
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(12)
        };
    }

    private static FlowLayoutPanel CreateBadgeRow()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 2, 0, 8),
            Padding = new Padding(0, 4, 0, 4)
        };
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!IsDisposed)
        {
            ApplyTheme(e.NewTheme);
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        ApplyThemeToChildren(_content, theme);
    }

    private static void ApplyThemeToChildren(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is not BootstrapBadge)
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
