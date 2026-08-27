using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class TextBoxCardDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly GroupBox _inputSection = new GroupBox();
    private readonly GroupBox _cardSection = new GroupBox();

    public TextBoxCardDemoForm()
    {
        Text = "BootstrapTextBox / BootstrapCard Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 760);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        BuildInputSection();
        BuildCardSection();
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
        _content.Padding = new Padding(12);

        ConfigureSection(_inputSection, "TextBox states");
        ConfigureSection(_cardSection, "Card composition");
        _content.Controls.Add(_inputSection);
        _content.Controls.Add(_cardSection);
    }

    private static void ConfigureSection(GroupBox section, string text)
    {
        section.Text = text;
        section.AutoSize = true;
        section.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        section.MinimumSize = new Size(920, 0);
        section.Margin = new Padding(0, 0, 0, 12);
        section.Padding = new Padding(12);
    }

    private void BuildInputSection()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 0
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddInputCell(grid, "Placeholder", new BootstrapTextBox
        {
            PlaceholderText = "Email address"
        });
        AddInputCell(grid, "Leading icon", new BootstrapTextBox
        {
            PlaceholderText = "Verified value",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check)
        });
        AddInputCell(grid, "Trailing icon", new BootstrapTextBox
        {
            PlaceholderText = "Dismiss affordance",
            TrailingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close)
        });
        AddInputCell(grid, "Clear button", new BootstrapTextBox
        {
            Text = "Search term",
            ShowClearButton = true
        });
        AddInputCell(grid, "Valid", new BootstrapTextBox
        {
            Text = "valid@example.com",
            ValidationState = BootstrapValidationState.Valid
        });
        AddInputCell(grid, "Invalid", new BootstrapTextBox
        {
            Text = "not-an-email",
            ValidationState = BootstrapValidationState.Invalid
        });
        AddInputCell(grid, "Read only", new BootstrapTextBox
        {
            Text = "Read-only value",
            ReadOnly = true
        });
        AddInputCell(grid, "Password", new BootstrapTextBox
        {
            Text = "Bootstrap5",
            UseSystemPasswordChar = true
        });
        AddInputCell(grid, "Disabled", new BootstrapTextBox
        {
            Text = "Disabled value",
            Enabled = false
        });

        _inputSection.Controls.Add(grid);
    }

    private static void AddInputCell(TableLayoutPanel grid, string caption, BootstrapTextBox input)
    {
        var index = grid.Controls.Count;
        var column = index % 2;
        var row = index / 2;
        while (grid.RowCount <= row)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowCount++;
        }

        var cell = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(6, 6, 18, 10),
            MinimumSize = new Size(410, 68)
        };
        var label = new Label
        {
            AutoSize = true,
            Text = caption,
            Margin = new Padding(0, 0, 0, 5)
        };
        input.Width = 360;
        input.Margin = Padding.Empty;
        input.AccessibleName = $"{caption} input";

        cell.Controls.Add(label);
        cell.Controls.Add(input);
        grid.Controls.Add(cell, column, row);
    }

    private void BuildCardSection()
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        row.Controls.Add(CreateCard(
            "Default card",
            "Bordered surface with theme radius and the default body region.",
            showFooter: false,
            showShadow: false,
            showBorder: true,
            borderRadius: -1));
        row.Controls.Add(CreateCard(
            "Header + footer",
            "Header, body and footer are stable child containers that remain Designer-friendly.",
            showFooter: true,
            showShadow: false,
            showBorder: true,
            borderRadius: -1));
        row.Controls.Add(CreateCard(
            "Shadow",
            "The optional shadow is painted directly as lightweight rounded geometry.",
            showFooter: false,
            showShadow: true,
            showBorder: true,
            borderRadius: -1));
        row.Controls.Add(CreateCard(
            "Borderless / custom radius",
            "A custom radius does not mutate theme tokens and the border can be omitted.",
            showFooter: false,
            showShadow: false,
            showBorder: false,
            borderRadius: 14));

        _cardSection.Controls.Add(row);
    }

    private static BootstrapCard CreateCard(
        string title,
        string bodyText,
        bool showFooter,
        bool showShadow,
        bool showBorder,
        int borderRadius)
    {
        var card = new BootstrapCard
        {
            Size = new Size(420, 170),
            Margin = new Padding(6, 6, 18, 12),
            ShowShadow = showShadow,
            ShowBorder = showBorder,
            BorderRadius = borderRadius
        };

        var headerLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        card.Header.Height = 38;
        card.Header.Visible = true;
        card.Header.Controls.Add(headerLabel);

        var bodyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = bodyText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        card.Body.Controls.Add(bodyLabel);

        if (showFooter)
        {
            var footerLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Footer region",
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Footer.Height = 34;
            card.Footer.Visible = true;
            card.Footer.Controls.Add(footerLabel);
        }

        return card;
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
        _inputSection.BackColor = theme.Colors.Body;
        _inputSection.ForeColor = theme.Colors.Text;
        _cardSection.BackColor = theme.Colors.Body;
        _cardSection.ForeColor = theme.Colors.Text;
        ApplyStandardTextColor(_inputSection, theme.Colors.Text);
        ApplyStandardTextColor(_cardSection, theme.Colors.Text);
    }

    private static void ApplyStandardTextColor(Control root, Color color)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label || child is GroupBox)
            {
                child.ForeColor = color;
            }

            ApplyStandardTextColor(child, color);
        }
    }
}
