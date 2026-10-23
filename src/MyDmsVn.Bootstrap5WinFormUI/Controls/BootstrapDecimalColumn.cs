using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Edits decimal or nullable decimal values through a native decimal input.</summary>
public sealed class BootstrapDecimalColumn : DataGridViewColumn
{
    private DecimalInputProfile _profile = new DecimalInputProfile(CultureInfo.CurrentCulture, true, 4, decimal.MinValue, decimal.MaxValue);

    /// <summary>Creates a decimal column with a decimal editing cell.</summary>
    public BootstrapDecimalColumn() : base(new BootstrapDecimalCell()) { }

    /// <summary>Gets or sets the profile used when a cell starts editing.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DecimalInputProfile Profile
    {
        get => _profile;
        set => _profile = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public override object Clone()
    {
        var clone = (BootstrapDecimalColumn)base.Clone();
        clone._profile = _profile;
        return clone;
    }
}
