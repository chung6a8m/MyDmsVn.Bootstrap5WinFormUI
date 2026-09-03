using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class ChecksDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _eventStatus = new Label();
    private readonly Bitmap _fallbackImage = new Bitmap(12, 12);
    private int _checkedEvents;
    private int _stateEvents;

    public ChecksDemoForm()
    {
        Text = "Checks / Radios / Switches";
        ClientSize = new Size(1040, 760);
        ConfigureFallbackImage();
        ConfigureContent();
        Controls.Add(_content);
        AddCheckBoxes();
        AddRadios();
        AddSwitches();
        AddVariantsAndFallback();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _fallbackImage.Dispose();
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
        _eventStatus.AutoSize = true;
        _eventStatus.AccessibleName = "Checkable event counters";
        _eventStatus.Margin = new Padding(8);
        UpdateEventStatus();
        _content.Controls.Add(_eventStatus);
    }

    private void AddCheckBoxes()
    {
        var row = CreateRow("CheckBox states");
        AddTracked(row, new BootstrapCheckBox { Text = "Unchecked" });
        AddTracked(row, new BootstrapCheckBox { Text = "Checked", Checked = true });
        AddTracked(row, new BootstrapCheckBox { Text = "Indeterminate (3-state)", ThreeState = true, CheckState = CheckState.Indeterminate });
        AddTracked(row, new BootstrapCheckBox { Text = "Programmatic mixed (2-state)", ThreeState = false, CheckState = CheckState.Indeterminate });
        AddTracked(row, new BootstrapCheckBox { Text = "Valid unchecked", ValidationState = BootstrapValidationState.Valid });
        AddTracked(row, new BootstrapCheckBox { Text = "Invalid unchecked", ValidationState = BootstrapValidationState.Invalid });
        AddTracked(row, new BootstrapCheckBox { Text = "Disabled", Enabled = false, Checked = true });
        AddTracked(row, new BootstrapCheckBox { Text = "AutoCheck=false", AutoCheck = false });
        AddTracked(row, new BootstrapCheckBox { Text = "RTL / right slot", RightToLeft = RightToLeft.Yes, CheckAlign = ContentAlignment.MiddleLeft, Checked = true });
        _content.Controls.Add(row);
    }

    private void AddRadios()
    {
        var section = CreateRow("RadioButton native groups");
        var nativeGroup = CreateInlinePanel();
        nativeGroup.Controls.Add(new BootstrapRadioButton { Text = "Standard A", Checked = true });
        nativeGroup.Controls.Add(new BootstrapRadioButton { Text = "Standard B" });
        nativeGroup.Controls.Add(new BootstrapRadioButton { Text = "Standard C" });
        var separateGroup = CreateInlinePanel();
        separateGroup.Controls.Add(new BootstrapRadioButton { Text = "Separate parent A", Checked = true });
        separateGroup.Controls.Add(new BootstrapRadioButton { Text = "Separate parent B" });
        var manualGroup = CreateInlinePanel();
        manualGroup.Controls.Add(new BootstrapRadioButton { Text = "Caller-managed A", AutoCheck = false, Checked = true });
        manualGroup.Controls.Add(new BootstrapRadioButton { Text = "Caller-managed B", AutoCheck = false, Checked = true });
        section.Controls.Add(nativeGroup);
        section.Controls.Add(separateGroup);
        section.Controls.Add(manualGroup);
        _content.Controls.Add(section);
    }

    private void AddSwitches()
    {
        var row = CreateRow("Switch states");
        AddTracked(row, new BootstrapSwitch { Text = "Off" });
        AddTracked(row, new BootstrapSwitch { Text = "On", Checked = true });
        AddTracked(row, new BootstrapSwitch { Text = "Indeterminate", ThreeState = true, CheckState = CheckState.Indeterminate });
        AddTracked(row, new BootstrapSwitch { Text = "Programmatic mixed", ThreeState = false, CheckState = CheckState.Indeterminate });
        AddTracked(row, new BootstrapSwitch { Text = "Valid off", ValidationState = BootstrapValidationState.Valid });
        AddTracked(row, new BootstrapSwitch { Text = "Invalid off", ValidationState = BootstrapValidationState.Invalid });
        AddTracked(row, new BootstrapSwitch { Text = "Disabled", Enabled = false, Checked = true });
        AddTracked(row, new BootstrapSwitch { Text = "AutoCheck=false", AutoCheck = false });
        AddTracked(row, new BootstrapSwitch { Text = "RTL thumb", RightToLeft = RightToLeft.Yes, Checked = true });
        _content.Controls.Add(row);
    }

    private void AddVariantsAndFallback()
    {
        var variants = CreateRow("Checked semantic variants");
        foreach (BootstrapVariant variant in Enum.GetValues(typeof(BootstrapVariant)))
        {
            variants.Controls.Add(new BootstrapCheckBox { Text = variant.ToString(), Variant = variant, Checked = true });
        }
        _content.Controls.Add(variants);

        var fallback = CreateRow("Native visual fallback");
        fallback.Controls.Add(new BootstrapCheckBox { Text = "Appearance.Button", Appearance = Appearance.Button, Checked = true });
        fallback.Controls.Add(new BootstrapCheckBox { Text = "Effective image", Image = _fallbackImage, ImageAlign = ContentAlignment.MiddleLeft, Checked = true });
        _content.Controls.Add(fallback);
    }

    private void AddTracked(Control parent, CheckBox control)
    {
        control.CheckedChanged += (_, _) => { _checkedEvents++; UpdateEventStatus(); };
        control.CheckStateChanged += (_, _) => { _stateEvents++; UpdateEventStatus(); };
        parent.Controls.Add(control);
    }

    private static FlowLayoutPanel CreateRow(string title)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Width = 980,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold), Margin = new Padding(0, 4, 16, 6) });
        return panel;
    }

    private static FlowLayoutPanel CreateInlinePanel() => new FlowLayoutPanel
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        Margin = new Padding(4)
    };

    private void ConfigureFallbackImage()
    {
        using var graphics = Graphics.FromImage(_fallbackImage);
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.MediumPurple);
        graphics.FillEllipse(brush, 1, 1, 10, 10);
    }

    private void UpdateEventStatus() => _eventStatus.Text = $"CheckedChanged: {_checkedEvents} · CheckStateChanged: {_stateEvents}";

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        _eventStatus.ForeColor = theme.Colors.MutedText;
        foreach (Control child in _content.Controls)
        {
            child.BackColor = theme.Colors.Surface;
            child.ForeColor = theme.Colors.Text;
        }
    }
}
