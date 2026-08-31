using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Configures structural time formatting.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapTimeFormatOptions
{
    private string _pattern = "hm";
    private string _delimiter = ":";
    private bool _delimiterLazyShow;
    private BootstrapTimeFormat _timeFormat = BootstrapTimeFormat.TwentyFourHour;

    internal event EventHandler? Changed;

    /// <summary>Gets or sets the component pattern using h, m, or s.</summary>
    [DefaultValue("hm")]
    public string Pattern
    {
        get => _pattern;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            InputFormatOptionValidation.ValidatePattern(next, "hms", 3, rejectYearConflict: false);
            SetString(ref _pattern, next);
        }
    }

    /// <summary>Gets or sets the component delimiter.</summary>
    [DefaultValue(":")]
    public string Delimiter
    {
        get => _delimiter;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            InputFormatOptionValidation.ValidateContainsNoAsciiDigits(next, nameof(value));
            SetString(ref _delimiter, next);
        }
    }

    /// <summary>Gets or sets whether delimiters appear only after the next digit.</summary>
    [DefaultValue(false)]
    public bool DelimiterLazyShow
    {
        get => _delimiterLazyShow;
        set { if (_delimiterLazyShow == value) return; _delimiterLazyShow = value; RaiseChanged(); }
    }

    /// <summary>Gets or sets the structural hour range.</summary>
    [DefaultValue(BootstrapTimeFormat.TwentyFourHour)]
    public BootstrapTimeFormat TimeFormat
    {
        get => _timeFormat;
        set
        {
            InputFormatOptionValidation.ValidateEnum(value, nameof(value));
            if (_timeFormat == value) return;
            _timeFormat = value;
            RaiseChanged();
        }
    }

    private void SetString(ref string field, string value) { if (field == value) return; field = value; RaiseChanged(); }
    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
