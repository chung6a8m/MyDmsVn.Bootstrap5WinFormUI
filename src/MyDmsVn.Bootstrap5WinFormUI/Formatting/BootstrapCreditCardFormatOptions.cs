using System;
using System.ComponentModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Configures credit-card formatting.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class BootstrapCreditCardFormatOptions
{
    private string _delimiter = " ";
    private bool _delimiterLazyShow;
    private bool _strictMode;

    internal event EventHandler? Changed;

    /// <summary>Gets or sets the block delimiter.</summary>
    [DefaultValue(" ")]
    public string Delimiter
    {
        get => _delimiter;
        set
        {
            var next = InputFormatOptionValidation.Normalize(value);
            if (_delimiter == next) return;
            _delimiter = next;
            RaiseChanged();
        }
    }

    /// <summary>Gets or sets whether delimiters appear only after the next digit.</summary>
    [DefaultValue(false)]
    public bool DelimiterLazyShow { get => _delimiterLazyShow; set => SetValue(ref _delimiterLazyShow, value); }

    /// <summary>Gets or sets whether the maximum raw length is extended to nineteen digits.</summary>
    [DefaultValue(false)]
    public bool StrictMode { get => _strictMode; set => SetValue(ref _strictMode, value); }

    private void SetValue(ref bool field, bool value) { if (field == value) return; field = value; RaiseChanged(); }
    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
