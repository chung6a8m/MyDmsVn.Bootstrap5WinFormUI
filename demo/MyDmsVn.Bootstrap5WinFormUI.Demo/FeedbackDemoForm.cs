using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
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
    private readonly List<BootstrapAlert> _dismissibleAlerts = new List<BootstrapAlert>();
    private readonly Label _dismissStatus = new Label();
    private readonly Button _restoreAlertsButton = new Button();

    public FeedbackDemoForm()
    {
        Text = "Feedback Components Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 720);
        MinimumSize = new Size(640, 420);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        Controls.Add(_content);

        AddSemanticVariantsSection();
        AddShapeAndStateSection();
        AddAlertsSection();
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
        var group = CreateGroup("Badge semantic variants");
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
        var group = CreateGroup("Badge shape, custom color, disabled, and content length");
        var stack = CreateVerticalStack();

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

    private void AddAlertsSection()
    {
        var group = CreateGroup("Alerts — semantic, icon, dismissal, multiline, and disabled states");
        var stack = CreateVerticalStack();

        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Primary,
            "Primary — inline feedback with the default themed radius."));
        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Secondary,
            "Secondary — lower-emphasis application feedback."));
        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Success,
            "Success — changes saved successfully.",
            IconDescriptor.Framework(FrameworkIconGlyph.Check)));

        var danger = CreateAlert(
            BootstrapVariant.Danger,
            "Danger — an operation failed. This example is dismissible.",
            dismissible: true);
        WireDismissStatus(danger, "Danger");
        stack.Controls.Add(danger);

        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Warning,
            "Warning — the upload has not completed.\r\nCheck the connection and try again."));

        var info = CreateAlert(
            BootstrapVariant.Info,
            "Info — keyboard users can Tab to the close affordance and activate it with Enter or Space.",
            IconDescriptor.Framework(FrameworkIconGlyph.Check),
            dismissible: true);
        WireDismissStatus(info, "Info");
        stack.Controls.Add(info);

        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Light,
            "Light — contrast fallback regression example."));
        stack.Controls.Add(CreateAlert(
            BootstrapVariant.Dark,
            "Dark — contrast fallback regression example."));

        var disabled = CreateAlert(
            BootstrapVariant.Success,
            "Disabled — neutral disabled palette and no user dismissal.");
        disabled.Enabled = false;
        disabled.AccessibleName = "Disabled alert";
        stack.Controls.Add(disabled);

        var square = CreateAlert(
            BootstrapVariant.Info,
            "Custom radius — BorderRadius = 0 keeps the surface square.");
        square.BorderRadius = 0;
        square.AccessibleName = "Square radius alert";
        stack.Controls.Add(square);

        ConfigureDismissControls();
        var commandRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 4, 0, 0)
        };
        commandRow.Controls.Add(_restoreAlertsButton);
        commandRow.Controls.Add(_dismissStatus);
        stack.Controls.Add(commandRow);

        group.Controls.Add(stack);
        _content.Controls.Add(group);
    }

    private void ConfigureDismissControls()
    {
        _restoreAlertsButton.AutoSize = true;
        _restoreAlertsButton.Text = "Restore dismissed alerts";
        _restoreAlertsButton.UseVisualStyleBackColor = false;
        _restoreAlertsButton.AccessibleName = "Restore dismissed alerts";
        _restoreAlertsButton.Click += (_, _) =>
        {
            foreach (var alert in _dismissibleAlerts)
            {
                alert.Visible = true;
            }

            _dismissStatus.Text = "Dismissed alerts restored.";
        };

        _dismissStatus.AutoSize = true;
        _dismissStatus.Text = "No alert dismissed yet.";
        _dismissStatus.Margin = new Padding(12, 8, 0, 0);
        _dismissStatus.AccessibleName = "Alert dismissal status";
    }

    private BootstrapAlert CreateAlert(
        BootstrapVariant variant,
        string text,
        IconDescriptor? icon = null,
        bool dismissible = false)
    {
        var multiline = text.IndexOf('\n') >= 0;
        var alert = new BootstrapAlert
        {
            Size = new Size(780, multiline ? 72 : 52),
            Margin = new Padding(0, 3, 0, 3),
            Text = text,
            Variant = variant,
            Icon = icon,
            Dismissible = dismissible,
            AccessibleName = $"{variant} alert"
        };

        if (dismissible)
        {
            _dismissibleAlerts.Add(alert);
        }

        return alert;
    }

    private void WireDismissStatus(BootstrapAlert alert, string label)
    {
        alert.Dismissed += (_, _) => _dismissStatus.Text = $"Last dismissed: {label}";
    }

    private void AddDpiGuidanceSection()
    {
        var group = CreateGroup("Theme and DPI verification");
        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(800, 0),
            Text = "Use the integrated demo's Light/Dark switch while this page is open. Repeat this page at Windows display scaling 100%, 125%, 150%, 175%, and 200% to verify Badge padding plus Alert borders, multiline text, icons, close focus, and rounded geometry.",
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

    private static FlowLayoutPanel CreateVerticalStack()
    {
        return new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Top
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
            if (child is BootstrapBadge || child is BootstrapAlert)
            {
                continue;
            }

            child.ForeColor = theme.Colors.Text;
            if (child is GroupBox || child is FlowLayoutPanel || child is Label)
            {
                child.BackColor = theme.Colors.Body;
            }

            if (child.HasChildren)
            {
                ApplyThemeToChildren(child, theme);
            }
        }
    }
}
