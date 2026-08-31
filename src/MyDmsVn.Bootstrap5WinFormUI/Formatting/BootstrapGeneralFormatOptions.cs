using System;
using System.ComponentModel;
using System.Linq;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Configures general block-based input formatting.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapGeneralFormatOptions
{
    private int[] _blocks = Array.Empty<int>();
    private string _delimiter = " ";
    private string[] _delimiters = Array.Empty<string>();
    private bool _delimiterLazyShow;
    private string _prefix = string.Empty;
    private bool _numericOnly;
    private bool _uppercase;
    private bool _lowercase;

    internal event EventHandler? Changed;

    /// <summary>Gets or sets the positive lengths of display blocks.</summary>
    public int[] Blocks
    {
        get => (int[])_blocks.Clone();
        set
        {
            var next = value is null ? Array.Empty<int>() : (int[])value.Clone();
            if (next.Any(length => length <= 0)) throw new ArgumentException("Block lengths must be greater than zero.", nameof(value));
            if (_blocks.SequenceEqual(next)) return;
            _blocks = next;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets or sets the fallback block delimiter.</summary>
    [DefaultValue(" ")]
    public string Delimiter
    {
        get => _delimiter;
        set => SetString(ref _delimiter, value);
    }

    /// <summary>Gets or sets per-boundary delimiters.</summary>
    public string[] Delimiters
    {
        get => (string[])_delimiters.Clone();
        set
        {
            var next = value is null ? Array.Empty<string>() : value.Select(InputFormatOptionValidation.Normalize).ToArray();
            if (_delimiters.SequenceEqual(next)) return;
            _delimiters = next;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets or sets whether delimiters appear only after the next raw character.</summary>
    [DefaultValue(false)]
    public bool DelimiterLazyShow
    {
        get => _delimiterLazyShow;
        set => SetValue(ref _delimiterLazyShow, value);
    }

    /// <summary>Gets or sets display decoration placed before non-empty raw text.</summary>
    [DefaultValue("")]
    public string Prefix
    {
        get => _prefix;
        set => SetString(ref _prefix, value);
    }

    /// <summary>Gets or sets whether non-digit characters are removed.</summary>
    [DefaultValue(false)]
    public bool NumericOnly
    {
        get => _numericOnly;
        set => SetValue(ref _numericOnly, value);
    }

    /// <summary>Gets or sets whether canonical text is converted to uppercase.</summary>
    [DefaultValue(false)]
    public bool Uppercase
    {
        get => _uppercase;
        set
        {
            if (_uppercase == value && (!value || !_lowercase)) return;
            _uppercase = value;
            if (value) _lowercase = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets or sets whether canonical text is converted to lowercase.</summary>
    [DefaultValue(false)]
    public bool Lowercase
    {
        get => _lowercase;
        set
        {
            if (_lowercase == value && (!value || !_uppercase)) return;
            _lowercase = value;
            if (value) _uppercase = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetString(ref string field, string? value)
    {
        var next = InputFormatOptionValidation.Normalize(value);
        if (field == next) return;
        field = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SetValue(ref bool field, bool value)
    {
        if (field == value) return;
        field = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
