using System;
using System.Drawing;
using System.Linq;
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
    private readonly GroupBox _dateSection = new GroupBox();
    private readonly GroupBox _calendarSection = new GroupBox();
    private readonly Label _integerStatus = new Label();
    private readonly Label _comboStatus = new Label();
    private readonly Label _comboNote = new Label();
    private readonly Label _dateStatus = new Label();
    private readonly Label _dateNote = new Label();
    private readonly Label _calendarStatus = new Label();
    private readonly Label _calendarNote = new Label();

    public AdvancedInputsDemoForm()
    {
        Text = "Advanced Inputs Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 820);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        BuildNumericSection();
        BuildComboSection();
        BuildDateSection();
        BuildCalendarSection();
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

        _dateSection.Text = "DatePicker scenarios";
        _dateSection.AutoSize = true;
        _dateSection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _dateSection.MinimumSize = new Size(920, 0);
        _dateSection.Margin = new Padding(0, 0, 0, 12);
        _dateSection.Padding = new Padding(12);

        _calendarSection.Text = "Custom Calendar scenarios";
        _calendarSection.AutoSize = true;
        _calendarSection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _calendarSection.MinimumSize = new Size(920, 0);
        _calendarSection.Margin = new Padding(0, 0, 0, 12);
        _calendarSection.Padding = new Padding(12);

        _content.Controls.Add(_numericSection);
        _content.Controls.Add(_comboSection);
        _content.Controls.Add(_dateSection);
        _content.Controls.Add(_calendarSection);
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
        _comboNote.Dock = DockStyle.Top;
        _comboNote.Text = "Native ownership note: WinForms/OS still owns the editable child, arrow button, hit-testing, and popup chrome. The popup may remain square or OS-themed.";

        var stack = CreateSectionStack(grid, _comboNote);
        _comboSection.Controls.Add(stack);
    }

    private void BuildDateSection()
    {
        var grid = CreateScenarioGrid();
        var sample = new DateTime(2026, 8, 28, 10, 30, 0);

        var longDate = new BootstrapDatePicker
        {
            Value = sample,
            Format = DateTimePickerFormat.Long
        };
        _dateStatus.AutoSize = true;
        _dateStatus.Text = $"ValueChanged: {longDate.Value:yyyy-MM-dd HH:mm}";
        _dateStatus.Margin = new Padding(0, 5, 0, 0);
        longDate.ValueChanged += (_, _) => _dateStatus.Text = $"ValueChanged: {longDate.Value:yyyy-MM-dd HH:mm}";
        AddDateCell(grid, "Long / native locale", longDate, _dateStatus);

        AddDateCell(grid, "Short", new BootstrapDatePicker
        {
            Value = sample,
            Format = DateTimePickerFormat.Short
        });

        AddDateCell(grid, "Time", new BootstrapDatePicker
        {
            Value = sample,
            Format = DateTimePickerFormat.Time
        });

        AddDateCell(grid, "Custom date", new BootstrapDatePicker
        {
            Value = sample,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd"
        });

        AddDateCell(grid, "Custom date + time", new BootstrapDatePicker
        {
            Value = sample,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm"
        });

        AddDateCell(grid, "Optional / unchecked", new BootstrapDatePicker
        {
            Value = sample,
            ShowCheckBox = true,
            Checked = false
        });

        AddDateCell(grid, "Range constrained", new BootstrapDatePicker
        {
            MinDate = new DateTime(2026, 1, 1),
            MaxDate = new DateTime(2026, 12, 31),
            Value = sample
        });

        AddDateCell(grid, "Valid", new BootstrapDatePicker
        {
            Value = sample,
            ValidationState = BootstrapValidationState.Valid
        });

        AddDateCell(grid, "Invalid", new BootstrapDatePicker
        {
            Value = sample,
            ValidationState = BootstrapValidationState.Invalid
        });

        AddDateCell(grid, "Disabled", new BootstrapDatePicker
        {
            Value = sample,
            Enabled = false
        });

        AddDateCell(grid, "Explicit radius", new BootstrapDatePicker
        {
            Value = sample,
            BorderRadius = 8
        });

        _dateNote.AutoSize = true;
        _dateNote.MaximumSize = new Size(860, 0);
        _dateNote.Margin = new Padding(6, 0, 6, 8);
        _dateNote.Dock = DockStyle.Top;
        _dateNote.Text = "Native ownership note: WinForms/OS owns the DateTimePicker calendar popup, localized rendering, calendar navigation, and native keyboard behavior. Stage 9 intentionally does not expose ShowUpDown or replace the popup.";

        _dateSection.Controls.Add(CreateSectionStack(grid, _dateNote));
    }

    private void BuildCalendarSection()
    {
        var grid = CreateScenarioGrid();
        var minDate = new DateTime(2025, 1, 1);
        var maxDate = new DateTime(2030, 12, 31);
        var rangeStart = new DateTime(2026, 8, 10);
        var rangeEnd = new DateTime(2026, 8, 15);

        var rangeCalendar = new BootstrapCalendar
        {
            SelectionMode = BootstrapCalendarSelectionMode.Range,
            MinDate = minDate,
            MaxDate = maxDate,
            DisplayMonth = new DateTime(2026, 8, 1)
        };
        rangeCalendar.SetRange(rangeStart, rangeEnd);
        _calendarStatus.AutoSize = true;
        _calendarStatus.Margin = new Padding(0, 5, 0, 0);
        UpdateRangeStatus(rangeCalendar);
        rangeCalendar.SelectionChanged += (_, _) => UpdateRangeStatus(rangeCalendar);
        AddCalendarCell(grid, "Custom Calendar — Range", rangeCalendar, _calendarStatus);

        var singlePicker = CreateCalendarPicker(BootstrapCalendarSelectionMode.Single, minDate, maxDate);
        singlePicker.PlaceholderText = "Choose one date";
        singlePicker.SelectedDate = new DateTime(2026, 8, 12);
        var singleStatus = CreatePickerStatus(singlePicker);
        AddCalendarCell(grid, "Calendar Picker — Single", singlePicker, singleStatus);

        var rangePicker = CreateCalendarPicker(BootstrapCalendarSelectionMode.Range, minDate, maxDate);
        rangePicker.PlaceholderText = "Choose a date range";
        rangePicker.ValidationState = BootstrapValidationState.Invalid;
        rangePicker.SetRange(rangeStart, rangeEnd);
        var rangeStatus = CreatePickerStatus(rangePicker);
        AddCalendarCell(grid, "Calendar Picker — Range", rangePicker, rangeStatus);

        var multiplePicker = CreateCalendarPicker(BootstrapCalendarSelectionMode.Multiple, minDate, maxDate);
        multiplePicker.PlaceholderText = "Choose one or more dates";
        multiplePicker.SetSelectedDates(new[]
        {
            new DateTime(2026, 8, 8),
            new DateTime(2026, 8, 12),
            new DateTime(2026, 8, 18)
        });
        AddCalendarCell(grid, "Calendar Picker — Multiple", multiplePicker);

        var disabledPicker = CreateCalendarPicker(BootstrapCalendarSelectionMode.Single, minDate, maxDate);
        disabledPicker.SelectedDate = new DateTime(2026, 8, 20);
        disabledPicker.Enabled = false;
        AddCalendarCell(grid, "Calendar Picker — Disabled", disabledPicker);

        _calendarNote.AutoSize = true;
        _calendarNote.MaximumSize = new Size(860, 0);
        _calendarNote.Margin = new Padding(6, 0, 6, 8);
        _calendarNote.Dock = DockStyle.Top;
        _calendarNote.Text = "Calendar note: Range is shown directly; picker popups use the native dropdown host. The Range picker is invalid, Multiple remains enabled for stay-open toggling, and a separate Single picker shows the disabled state.";

        _calendarSection.Controls.Add(CreateSectionStack(grid, _calendarNote));
    }

    private static BootstrapCalendarPicker CreateCalendarPicker(
        BootstrapCalendarSelectionMode selectionMode,
        DateTime minDate,
        DateTime maxDate)
    {
        return new BootstrapCalendarPicker
        {
            SelectionMode = selectionMode,
            MinDate = minDate,
            MaxDate = maxDate,
            DateFormat = "yyyy-MM-dd"
        };
    }

    private static Label CreatePickerStatus(BootstrapCalendarPicker picker)
    {
        var status = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };
        UpdatePickerStatus(picker, status);
        picker.SelectionChanged += (_, _) => UpdatePickerStatus(picker, status);
        return status;
    }

    private void UpdateRangeStatus(BootstrapCalendar calendar)
    {
        _calendarStatus.Text = $"SelectionChanged: {FormatRange(calendar.RangeStart, calendar.RangeEnd)}";
    }

    private static void UpdatePickerStatus(BootstrapCalendarPicker picker, Label status)
    {
        status.Text = $"SelectionChanged: {FormatPickerSelection(picker)}";
    }

    private static string FormatPickerSelection(BootstrapCalendarPicker picker)
    {
        if (picker.SelectionMode == BootstrapCalendarSelectionMode.Range)
        {
            return FormatRange(picker.RangeStart, picker.RangeEnd);
        }

        if (picker.SelectionMode == BootstrapCalendarSelectionMode.Multiple)
        {
            return string.Join(", ", picker.SelectedDates.Select(date => date.ToString(picker.DateFormat)));
        }

        return picker.SelectedDate.HasValue ? picker.SelectedDate.Value.ToString(picker.DateFormat) : "No date selected";
    }

    private static string FormatRange(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
        {
            return "No date selected";
        }

        return end.HasValue
            ? $"{start.Value:yyyy-MM-dd} — {end.Value:yyyy-MM-dd}"
            : $"{start.Value:yyyy-MM-dd} — choose an end date";
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

    private static TableLayoutPanel CreateSectionStack(Control content, Control note)
    {
        var stack = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(content, 0, 0);
        stack.Controls.Add(note, 0, 1);
        return stack;
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

    private static void AddDateCell(
        TableLayoutPanel grid,
        string caption,
        BootstrapDatePicker input,
        Control? status = null)
    {
        AddScenarioCell(grid, caption, input, "date picker", status);
    }

    private static void AddCalendarCell(
        TableLayoutPanel grid,
        string caption,
        Control input,
        Control? status = null)
    {
        AddScenarioCell(grid, caption, input, "calendar", status);
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

        var cell = new ScenarioCellPanel
        {
            Margin = new Padding(6, 6, 18, 10),
            Size = new Size(410, status is null ? 68 : 88),
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

        cell.SetScenarioControls(label, input, status);
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
        _dateSection.BackColor = theme.Colors.Body;
        _dateSection.ForeColor = theme.Colors.Text;
        _calendarSection.BackColor = theme.Colors.Body;
        _calendarSection.ForeColor = theme.Colors.Text;
        ApplyStandardTextColor(_numericSection, theme.Colors.Text);
        ApplyStandardTextColor(_comboSection, theme.Colors.Text);
        ApplyStandardTextColor(_dateSection, theme.Colors.Text);
        ApplyStandardTextColor(_calendarSection, theme.Colors.Text);
        _integerStatus.ForeColor = theme.Colors.MutedText;
        _comboStatus.ForeColor = theme.Colors.MutedText;
        _comboNote.ForeColor = theme.Colors.MutedText;
        _dateStatus.ForeColor = theme.Colors.MutedText;
        _dateNote.ForeColor = theme.Colors.MutedText;
        _calendarStatus.ForeColor = theme.Colors.MutedText;
        _calendarNote.ForeColor = theme.Colors.MutedText;
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

    private sealed class ScenarioCellPanel : Panel
    {
        private Label? _caption;
        private Control? _input;
        private Control? _status;
        private bool _layingOut;

        public void SetScenarioControls(Label caption, Control input, Control? status)
        {
            _caption = caption;
            _input = input;
            _status = status;

            Controls.Add(caption);
            Controls.Add(input);
            if (status is not null)
            {
                Controls.Add(status);
            }

            caption.SizeChanged += OnChildSizeChanged;
            input.SizeChanged += OnChildSizeChanged;
            if (status is not null)
            {
                status.SizeChanged += OnChildSizeChanged;
            }

            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutScenario();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_caption is not null)
                {
                    _caption.SizeChanged -= OnChildSizeChanged;
                }

                if (_input is not null)
                {
                    _input.SizeChanged -= OnChildSizeChanged;
                }

                if (_status is not null)
                {
                    _status.SizeChanged -= OnChildSizeChanged;
                }
            }

            base.Dispose(disposing);
        }

        private void OnChildSizeChanged(object? sender, EventArgs e)
        {
            LayoutScenario();
        }

        private void LayoutScenario()
        {
            if (_layingOut || _caption is null || _input is null)
            {
                return;
            }

            _layingOut = true;
            try
            {
                var y = Padding.Top + _caption.Margin.Top;
                _caption.Location = new Point(Padding.Left + _caption.Margin.Left, y);
                y = _caption.Bottom + _caption.Margin.Bottom + _input.Margin.Top;

                _input.Location = new Point(Padding.Left + _input.Margin.Left, y);
                y = _input.Bottom + _input.Margin.Bottom;

                if (_status is not null)
                {
                    y += _status.Margin.Top;
                    _status.Location = new Point(Padding.Left + _status.Margin.Left, y);
                    y = _status.Bottom + _status.Margin.Bottom;
                }

                var requiredHeight = y + Padding.Bottom;
                if (Height < requiredHeight)
                {
                    Height = requiredHeight;
                }
            }
            finally
            {
                _layingOut = false;
            }
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
