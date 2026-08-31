using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Configures structural date formatting.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapDateFormatOptions
{
    private string _pattern = "dmY";
    private string _delimiter = "/";
    private bool _delimiterLazyShow;

    internal event EventHandler? Changed;

    /// <summary>Gets or sets the date component pattern using d, m, y, or Y.</summary>
    [DefaultValue("dmY")]
    public string Pattern
    {
        get => _pattern;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            InputFormatOptionValidation.ValidatePattern(next, "dmyY", 3, rejectYearConflict: true);
            SetString(ref _pattern, next);
        }
    }

    /// <summary>Gets or sets the component delimiter.</summary>
    [DefaultValue("/")]
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
        set { if (_delimiterLazyShow == value) return; _delimiterLazyShow = value; Changed?.Invoke(this, EventArgs.Empty); }
    }

    private void SetString(ref string field, string value)
    {
        if (field == value) return;
        field = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
