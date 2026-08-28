using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class AdvancedInputsDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly GroupBox _numericSection = new GroupBox();
    private readonly GroupBox _comboSection = new GroupBox();
    private readonly Label _integerStatus = new Label();
    private readonly Label _comboStatus = new Label();
    private readonly Label _comboNote = new Label();

    public AdvancedInputsDemoForm()
    {
        Text = "Advanced Inputs Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 760);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        BuildNumericSection();
        BuildComboSection();
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

        _comboSection.Text = "ComboBox scenarios";
        _comboSection.AutoSize = true;
        _comboSection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _comboSection.MinimumSize = new Size(920, 0);
        _comboSection.Margin = new Padding(0, 0, 0, 12);
        _comboSection.Padding = new Padding(12);

        _content.Controls.Add(_numericSection);
        _content.Controls.Add(_comboSection);
    }

    private void BuildNumericSection()
    {
        var grid = CreateScenarioGrid();

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

    private void BuildComboSection()
    {
        var grid = CreateScenarioGrid();

        var dropDownList = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        dropDownList.Items.AddRange(new object[]
        {
            "Alpha",
            "Beta",
            "Gamma",
            "A deliberately long option used to verify end ellipsis without changing native selection"
        });
        dropDownList.SelectedIndex = 1;
        _comboStatus.AutoSize = true;
        _comboStatus.Text = "SelectedIndexChanged: 1 / Beta";
        _comboStatus.Margin = new Padding(0, 5, 0, 0);
        dropDownList.SelectedIndexChanged += (_, _) =>
            _comboStatus.Text = $"SelectedIndexChanged: {dropDownList.SelectedIndex} / {dropDownList.Text}";
        AddComboCell(grid, "DropDownList / native selection", dropDownList, _comboStatus);

        var editable = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Text = "Al"
        };
        editable.Items.AddRange(new object[] { "Alpha", "Alpine", "Beta", "Gamma" });
        AddComboCell(grid, "Editable / SuggestAppend", editable);

        var bound = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(ComboOption.Name),
            ValueMember = nameof(ComboOption.Id),
            DataSource = new[]
            {
                new ComboOption(10, "Warehouse 10"),
                new ComboOption(20, "Warehouse 20"),
                new ComboOption(30, "Warehouse 30")
            }
        };
        AddComboCell(grid, "DataSource / DisplayMember / ValueMember", bound);

        var leadingIcon = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            LeadingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Check)
        };
        leadingIcon.Items.AddRange(new object[] { "With leading icon", "Second option", "Third option" });
        leadingIcon.SelectedIndex = 0;
        AddComboCell(grid, "Leading icon", leadingIcon);

        var valid = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            ValidationState = BootstrapValidationState.Valid
        };
        valid.Items.AddRange(new object[] { "Valid value", "Alternative" });
        valid.SelectedIndex = 0;
        AddComboCell(grid, "Valid", valid);

        var invalid = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            ValidationState = BootstrapValidationState.Invalid
        };
        invalid.Items.AddRange(new object[] { "Invalid value", "Alternative" });
        invalid.SelectedIndex = 0;
        AddComboCell(grid, "Invalid", invalid);

        var disabled = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };
        disabled.Items.AddRange(new object[] { "Disabled", "Alternative" });
        disabled.SelectedIndex = 0;
        AddComboCell(grid, "Disabled", disabled);

        var explicitRadius = new BootstrapComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BorderRadius = 8
        };
        explicitRadius.Items.AddRange(new object[] { "8px logical radius", "Alternative" });
        explicitRadius.SelectedIndex = 0;
        AddComboCell(grid, "Explicit radius / no icon", explicitRadius);

        _comboNote.AutoSize = true;
        _comboNote.MaximumSize = new Size(860, 0);
        _comboNote.Margin = new Padding(6, 0, 6, 8);
        _comboNote.Text = "Native ownership note: WinForms/OS still owns the editable child, arrow button, hit-testing, and popup chrome. The popup may remain square or OS-themed.";

        _comboSection.Controls.Add(grid);
        _comboSection.Controls.Add(_comboNote);
    }

    private static TableLayoutPanel CreateScenarioGrid()
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
        return grid;
    }

    private static void AddNumericCell(
        TableLayoutPanel grid,
        string caption,
        BootstrapNumericBox input,
        Control? status = null)
    {
        AddScenarioCell(grid, caption, input, "numeric input", status);
    }

    private static void AddComboCell(
        TableLayoutPanel grid,
        string caption,
        BootstrapComboBox input,
        Control? status = null)
    {
        AddScenarioCell(grid, caption, input, "combo box", status);
    }

    private static void AddScenarioCell(
        TableLayoutPanel grid,
        string caption,
        Control input,
        string accessibleKind,
        Control? status)
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
        input.AccessibleName = $"{caption} {accessibleKind}";

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
        _comboSection.BackColor = theme.Colors.Body;
        _comboSection.ForeColor = theme.Colors.Text;
        ApplyStandardTextColor(_numericSection, theme.Colors.Text);
        ApplyStandardTextColor(_comboSection, theme.Colors.Text);
        _integerStatus.ForeColor = theme.Colors.MutedText;
        _comboStatus.ForeColor = theme.Colors.MutedText;
        _comboNote.ForeColor = theme.Colors.MutedText;
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

    private sealed class ComboOption
    {
        public ComboOption(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
