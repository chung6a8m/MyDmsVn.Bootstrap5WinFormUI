using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ButtonGroupToolbarDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _status = new Label();

    public ButtonGroupToolbarDemoForm()
    {
        Text = "BootstrapButtonGroup / BootstrapButtonToolbar Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 720);
        MinimumSize = new Size(720, 540);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        ConfigureStatus();
        Controls.Add(_content);
        Controls.Add(_status);

        AddHorizontalSingleSelectionSection();
        AddVerticalMultipleSelectionSection();
        AddEqualWidthSection();
        AddSpaceBetweenToolbarSection();
        AddVerticalToolbarSection();

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
        _status.Text = "ButtonGroup owns selection; ButtonToolbar owns layout only.";
    }

    private void AddHorizontalSingleSelectionSection()
    {
        var section = CreateSection("Horizontal group — Single selection");
        var group = new BootstrapButtonGroup
        {
            SelectionMode = BootstrapButtonSelectionMode.Single,
            Location = new Point(12, 24),
            AccessibleName = "Horizontal single-selection button group"
        };

        group.Controls.Add(CreateButton("Day", BootstrapVariant.Primary, outline: true));
        group.Controls.Add(CreateButton("Week", BootstrapVariant.Primary, outline: true));
        group.Controls.Add(CreateButton("Month", BootstrapVariant.Primary, outline: true));
        group.SelectionChanged += (_, _) =>
            _status.Text = $"Single selection: {GetSelectedText(group)}";

        section.Controls.Add(group);
        _content.Controls.Add(section);
    }

    private void AddVerticalMultipleSelectionSection()
    {
        var section = CreateSection("Vertical group — Multiple selection");
        var group = new BootstrapButtonGroup
        {
            Orientation = Orientation.Vertical,
            SelectionMode = BootstrapButtonSelectionMode.Multiple,
            Location = new Point(12, 24),
            AccessibleName = "Vertical multiple-selection button group"
        };

        group.Controls.Add(CreateButton("Bold", BootstrapVariant.Secondary, outline: true));
        group.Controls.Add(CreateButton("Italic", BootstrapVariant.Secondary, outline: true));
        group.Controls.Add(CreateButton("Underline", BootstrapVariant.Secondary, outline: true));
        group.SelectionChanged += (_, _) =>
            _status.Text = $"Multiple selection: {GetSelectedText(group)}";

        section.Controls.Add(group);
        _content.Controls.Add(section);
    }

    private void AddEqualWidthSection()
    {
        var section = CreateSection("EqualWidth connected group");
        var group = new BootstrapButtonGroup
        {
            EqualWidth = true,
            BorderRadius = 10,
            Location = new Point(12, 24),
            AccessibleName = "Equal-width connected button group"
        };

        group.Controls.Add(CreateButton("Short", BootstrapVariant.Success));
        group.Controls.Add(CreateButton("Longer action", BootstrapVariant.Success));
        group.Controls.Add(CreateButton("Longest connected action", BootstrapVariant.Success));

        section.Controls.Add(group);
        _content.Controls.Add(section);
    }

    private void AddSpaceBetweenToolbarSection()
    {
        var section = CreateSection("Horizontal toolbar — SpaceBetween desktop layout");
        var toolbar = new BootstrapButtonToolbar
        {
            AutoSize = false,
            Size = new Size(800, 42),
            Location = new Point(12, 24),
            GroupSpacing = 12,
            Alignment = BootstrapToolbarAlignment.SpaceBetween,
            AccessibleName = "Space-between button toolbar"
        };

        var primaryActions = new BootstrapButtonGroup();
        primaryActions.Controls.Add(CreateButton("New", BootstrapVariant.Primary));
        primaryActions.Controls.Add(CreateButton("Edit", BootstrapVariant.Primary, outline: true));

        var secondaryActions = new BootstrapButtonGroup();
        secondaryActions.Controls.Add(CreateButton("Refresh", BootstrapVariant.Secondary, outline: true));
        secondaryActions.Controls.Add(CreateButton("Export", BootstrapVariant.Secondary, outline: true));

        toolbar.Controls.Add(primaryActions);
        toolbar.Controls.Add(secondaryActions);
        section.Controls.Add(toolbar);
        _content.Controls.Add(section);
    }

    private void AddVerticalToolbarSection()
    {
        var section = CreateSection("Vertical toolbar — orientation and group spacing");
        var toolbar = new BootstrapButtonToolbar
        {
            Orientation = Orientation.Vertical,
            GroupSpacing = 12,
            Alignment = BootstrapToolbarAlignment.Left,
            Location = new Point(12, 24),
            AccessibleName = "Vertical button toolbar"
        };

        var navigation = new BootstrapButtonGroup
        {
            Orientation = Orientation.Vertical,
            EqualWidth = true
        };
        navigation.Controls.Add(CreateButton("Overview", BootstrapVariant.Info, outline: true));
        navigation.Controls.Add(CreateButton("Details", BootstrapVariant.Info, outline: true));

        var actions = new BootstrapButtonGroup
        {
            Orientation = Orientation.Vertical,
            EqualWidth = true
        };
        actions.Controls.Add(CreateButton("Apply", BootstrapVariant.Success));
        actions.Controls.Add(CreateButton("Reset", BootstrapVariant.Secondary, outline: true));

        toolbar.Controls.Add(navigation);
        toolbar.Controls.Add(actions);
        section.Controls.Add(toolbar);
        _content.Controls.Add(section);
    }

    private BootstrapButton CreateButton(string text, BootstrapVariant variant, bool outline = false)
    {
        var button = new BootstrapButton
        {
            AutoSize = true,
            Text = text,
            Variant = variant,
            Outline = outline,
            AccessibleName = $"{text} grouped Bootstrap button"
        };

        button.Click += (_, _) =>
        {
            if (button.Parent is BootstrapButtonGroup group &&
                group.SelectionMode == BootstrapButtonSelectionMode.None)
            {
                _status.Text = $"Toolbar action: {button.Text} — selection state unchanged by toolbar.";
            }
        };
        return button;
    }

    private static GroupBox CreateSection(string text)
    {
        return new GroupBox
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Width = 850,
            MinimumSize = new Size(850, 0),
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(12, 22, 12, 12)
        };
    }

    private static string GetSelectedText(BootstrapButtonGroup group)
    {
        var selected = group.SelectedButtons;
        if (selected.Count == 0)
        {
            return "none";
        }

        var text = selected[0].Text;
        for (var i = 1; i < selected.Count; i++)
        {
            text += ", " + selected[i].Text;
        }

        return text;
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
            if (child is not BootstrapButton &&
                child is not BootstrapButtonGroup &&
                child is not BootstrapButtonToolbar)
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
