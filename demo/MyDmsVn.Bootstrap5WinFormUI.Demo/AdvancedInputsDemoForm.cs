using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class AdvancedInputsDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly GroupBox _numericSection = new GroupBox();
    private readonly Label _integerStatus = new Label();

    public AdvancedInputsDemoForm()
    {
        Text = "Advanced Inputs Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 760);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        BuildNumericSection();
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

        _numericSection.Text = "NumericBox scenarios";
        _numericSection.AutoSize = true;
        _numericSection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _numericSection.MinimumSize = new Size(920, 0);
        _numericSection.Margin = new Padding(0, 0, 0, 12);
        _numericSection.Padding = new Padding(12);

        _content.Controls.Add(_numericSection);
    }

    private void BuildNumericSection()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 0,
            Margin = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var integer = new BootstrapNumericBox
        {
            Minimum = 0m,
            Maximum = 100m,
            Increment = 1m,
            Value = 12m
        };
        _integerStatus.AutoSize = true;
        _integerStatus.Text = "ValueChanged: 12";
        _integerStatus.Margin = new Padding(0, 5, 0, 0);
        integer.ValueChanged += (_, _) => _integerStatus.Text = $"ValueChanged: {integer.Value}";
        AddNumericCell(grid, "Integer / default", integer, _integerStatus);

        AddNumericCell(grid, "Decimal", new BootstrapNumericBox
        {
            Minimum = 0m,
            Maximum = 100m,
            Increment = 0.25m,
            DecimalPlaces = 2,
            Value = 12.50m
        });

        AddNumericCell(grid, "Thousands", new BootstrapNumericBox
        {
            Minimum = 0m,
            Maximum = 1000000m,
            Increment = 1000m,
            ThousandsSeparator = true,
            Value = 123456m
        });

        AddNumericCell(grid, "Signed / large step", new BootstrapNumericBox
        {
            Minimum = -100m,
            Maximum = 100m,
            Increment = 10m,
            Value = 0m
        });

        AddNumericCell(grid, "Valid", new BootstrapNumericBox
        {
            Value = 50m,
            ValidationState = BootstrapValidationState.Valid
        });

        AddNumericCell(grid, "Invalid", new BootstrapNumericBox
        {
            Value = 50m,
            ValidationState = BootstrapValidationState.Invalid
        });

        AddNumericCell(grid, "Read-only", new BootstrapNumericBox
        {
            Value = 42m,
            ReadOnly = true,
            Enabled = true
        });

        AddNumericCell(grid, "Disabled", new BootstrapNumericBox
        {
            Value = 42m,
            Enabled = false
        });

        _numericSection.Controls.Add(grid);
    }

    private static void AddNumericCell(
        TableLayoutPanel grid,
        string caption,
        BootstrapNumericBox input,
        Control? status = null)
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
            MinimumSize = new Size(410, status is null ? 68 : 88)
        };
        var label = new Label
        {
            AutoSize = true,
            Text = caption,
            Margin = new Padding(0, 0, 0, 5)
        };

        input.Width = 360;
        input.Margin = Padding.Empty;
        input.AccessibleName = $"{caption} numeric input";

        cell.Controls.Add(label);
        cell.Controls.Add(input);
        if (status is not null)
        {
            cell.Controls.Add(status);
        }

        grid.Controls.Add(cell, column, row);
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
        _numericSection.BackColor = theme.Colors.Body;
        _numericSection.ForeColor = theme.Colors.Text;
        ApplyStandardTextColor(_numericSection, theme.Colors.Text);
        _integerStatus.ForeColor = theme.Colors.MutedText;
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
