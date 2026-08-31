using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Configures string-based numeral formatting.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapNumeralFormatOptions
{
    private string _delimiter = ",";
    private BootstrapNumeralGroupStyle _thousandsGroupStyle = BootstrapNumeralGroupStyle.Thousand;
    private int _integerScale;
    private string _decimalMark = ".";
    private int _decimalScale = 2;
    private bool _positiveOnly;
    private bool _tailPrefix;
    private bool _signBeforePrefix;
    private bool _stripLeadingZeroes = true;
    private string _prefix = string.Empty;

    internal event EventHandler? Changed;

    /// <summary>Gets or sets the optional single-character group delimiter.</summary>
    [DefaultValue(",")]
    public string Delimiter
    {
        get => _delimiter;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            InputFormatOptionValidation.ValidateSingleCharacter(next, nameof(value));
            if (next.Length > 0 && next == _decimalMark) throw new ArgumentException("Delimiter and decimal mark must differ.", nameof(value));
            SetString(ref _delimiter, next);
        }
    }

    /// <summary>Gets or sets the integer grouping style.</summary>
    [DefaultValue(BootstrapNumeralGroupStyle.Thousand)]
    public BootstrapNumeralGroupStyle ThousandsGroupStyle
    {
        get => _thousandsGroupStyle;
        set
        {
            InputFormatOptionValidation.ValidateEnum(value, nameof(value));
            if (_thousandsGroupStyle == value) return;
            _thousandsGroupStyle = value;
            RaiseChanged();
        }
    }

    /// <summary>Gets or sets the maximum integer digits, or zero for unlimited.</summary>
    [DefaultValue(0)]
    public int IntegerScale
    {
        get => _integerScale;
        set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ref _integerScale, value); }
    }

    /// <summary>Gets or sets the single display decimal mark.</summary>
    [DefaultValue(".")]
    public string DecimalMark
    {
        get => _decimalMark;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            InputFormatOptionValidation.ValidateSingleCharacter(next, nameof(value), allowEmpty: _decimalScale == 0);
            if (next.Length > 0 && next == _delimiter) throw new ArgumentException("Decimal mark and delimiter must differ.", nameof(value));
            SetString(ref _decimalMark, next);
        }
    }

    /// <summary>Gets or sets the maximum decimal digits.</summary>
    [DefaultValue(2)]
    public int DecimalScale
    {
        get => _decimalScale;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value > 0 && _decimalMark.Length == 0) throw new ArgumentException("A decimal mark is required when decimal digits are enabled.", nameof(value));
            SetValue(ref _decimalScale, value);
        }
    }

    /// <summary>Gets or sets whether negative signs are removed.</summary>
    [DefaultValue(false)]
    public bool PositiveOnly { get => _positiveOnly; set => SetValue(ref _positiveOnly, value); }

    /// <summary>Gets or sets whether the prefix is displayed after the number.</summary>
    [DefaultValue(false)]
    public bool TailPrefix { get => _tailPrefix; set => SetValue(ref _tailPrefix, value); }

    /// <summary>Gets or sets whether a negative sign precedes a leading prefix.</summary>
    [DefaultValue(false)]
    public bool SignBeforePrefix { get => _signBeforePrefix; set => SetValue(ref _signBeforePrefix, value); }

    /// <summary>Gets or sets whether redundant leading zeroes are removed.</summary>
    [DefaultValue(true)]
    public bool StripLeadingZeroes { get => _stripLeadingZeroes; set => SetValue(ref _stripLeadingZeroes, value); }

    /// <summary>Gets or sets display prefix or suffix decoration.</summary>
    [DefaultValue("")]
    public string Prefix { get => _prefix; set => SetString(ref _prefix, InputFormatOptionValidation.Normalize(value)); }

    private void SetString(ref string field, string value) { if (field == value) return; field = value; RaiseChanged(); }
    private void SetValue(ref int field, int value) { if (field == value) return; field = value; RaiseChanged(); }
    private void SetValue(ref bool field, bool value) { if (field == value) return; field = value; RaiseChanged(); }
    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
