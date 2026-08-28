using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired numeric input while delegating numeric editing, formatting, range, and spin behavior to a native <see cref="NumericUpDown"/>.
/// </summary>
[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapNumericBox : UserControl
{
    private readonly NumericUpDown _editor = new NumericUpDown();
    private BootstrapValidationState _validationState = BootstrapValidationState.None;
    private int _borderRadius = -1;

    /// <summary>
    /// Initializes a designer-safe native-backed numeric input.
    /// </summary>
    public BootstrapNumericBox()
    {
        SetStyle(ControlStyles.Selectable, true);

        TabStop = true;
        AccessibleRole = AccessibleRole.SpinButton;
        AccessibleDescription = "Bootstrap-inspired numeric input.";

        _editor.BorderStyle = BorderStyle.None;
        _editor.TabStop = false;
        _editor.Margin = Padding.Empty;
        _editor.ValueChanged += OnEditorValueChanged;

        Controls.Add(_editor);
    }

    /// <summary>
    /// Occurs when the native numeric value changes.
    /// </summary>
    [Category("Action")]
    [Description("Occurs when the native numeric value changes.")]
    public event EventHandler? ValueChanged;

    /// <summary>
    /// Gets or sets the current native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the current native numeric value.")]
    [DefaultValue(typeof(decimal), "0")]
    public decimal Value
    {
        get => _editor.Value;
        set => _editor.Value = value;
    }

    /// <summary>
    /// Gets or sets the minimum native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the minimum native numeric value.")]
    [DefaultValue(typeof(decimal), "0")]
    public decimal Minimum
    {
        get => _editor.Minimum;
        set => _editor.Minimum = value;
    }

    /// <summary>
    /// Gets or sets the maximum native numeric value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the maximum native numeric value.")]
    [DefaultValue(typeof(decimal), "100")]
    public decimal Maximum
    {
        get => _editor.Maximum;
        set => _editor.Maximum = value;
    }

    /// <summary>
    /// Gets or sets the amount by which native spin operations change the value.
    /// </summary>
    [Category("Data")]
    [Description("Gets or sets the native numeric increment.")]
    [DefaultValue(typeof(decimal), "1")]
    public decimal Increment
    {
        get => _editor.Increment;
        set => _editor.Increment = value;
    }

    /// <summary>
    /// Gets or sets the number of decimal places displayed by the native editor.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets the number of decimal places displayed by the native numeric editor.")]
    [DefaultValue(0)]
    public int DecimalPlaces
    {
        get => _editor.DecimalPlaces;
        set => _editor.DecimalPlaces = value;
    }

    /// <summary>
    /// Gets or sets whether the native editor displays a thousands separator when appropriate.
    /// </summary>
    [Category("Appearance")]
    [Description("Gets or sets whether the native numeric editor uses a thousands separator.")]
    [DefaultValue(false)]
    public bool ThousandsSeparator
    {
        get => _editor.ThousandsSeparator;
        set => _editor.ThousandsSeparator = value;
    }

    /// <summary>
    /// Gets or sets whether typed editing is read-only while native spin behavior remains available.
    /// </summary>
    [Category("Behavior")]
    [Description("Prevents typed numeric editing while retaining native spin behavior.")]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => _editor.ReadOnly;
        set => _editor.ReadOnly = value;
    }

    /// <summary>
    /// Gets or sets the validation state used by the themed numeric shell.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects neutral, valid, or invalid numeric-input validation presentation.")]
    [DefaultValue(BootstrapValidationState.None)]
    public BootstrapValidationState ValidationState
    {
        get => _validationState;
        set
        {
            BootstrapTextBoxRenderLogic.ValidateState(value);
            _validationState = value;
        }
    }

    /// <summary>
    /// Gets or sets a uniform logical corner radius. Use -1 to select the current theme radius.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets a uniform logical corner radius, or -1 to use the current theme radius.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            }

            _borderRadius = value;
        }
    }

    private void OnEditorValueChanged(object? sender, EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
