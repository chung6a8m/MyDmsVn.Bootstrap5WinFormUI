using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Hosts simple text or arbitrary WinForms content in a list-group row.</summary>
[DefaultEvent(nameof(Click))]
[DefaultProperty(nameof(Text))]
public class BootstrapListGroupItem : Panel
{
    private bool _active;
    private bool _actionable;
    private BootstrapVariant? _variant;
    private bool _useThemeFont = true;

    /// <summary>Initializes a neutral, presentational list-group item.</summary>
    public BootstrapListGroupItem()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        AccessibleRole = AccessibleRole.ListItem;
        AccessibleDescription = "Bootstrap-inspired list-group item.";
        BackColor = Color.Transparent;
        Size = new Size(320, 44);
    }

    /// <summary>Gets or sets the application-owned active presentation state.</summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Active
    {
        get => _active;
        set => _active = value;
    }

    /// <summary>Gets or sets whether the item itself is a selectable activation target.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool Actionable
    {
        get => _actionable;
        set
        {
            if (_actionable == value)
            {
                return;
            }

            _actionable = value;
            SetStyle(ControlStyles.Selectable, value);
            TabStop = value;
            UpdateStyles();
        }
    }

    /// <summary>Gets or sets an optional semantic contextual variant; null uses neutral theme colors.</summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public BootstrapVariant? Variant
    {
        get => _variant;
        set
        {
            if (value.HasValue && (value.Value < BootstrapVariant.Primary || value.Value > BootstrapVariant.Dark))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported Bootstrap variant.");
            }

            _variant = value;
        }
    }

    /// <summary>Gets or sets whether theme typography owns the item font.</summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    public bool UseThemeFont
    {
        get => _useThemeFont;
        set => _useThemeFont = value;
    }
}
