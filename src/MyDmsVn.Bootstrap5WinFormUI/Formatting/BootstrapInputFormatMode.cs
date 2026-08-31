namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Specifies the formatter selected by a formatted text box.</summary>
public enum BootstrapInputFormatMode
{
    /// <summary>Preserves text unchanged.</summary>
    None,
    /// <summary>Uses configurable block formatting.</summary>
    General,
    /// <summary>Uses string-based numeral formatting.</summary>
    Numeral,
    /// <summary>Uses structural date formatting.</summary>
    Date,
    /// <summary>Uses structural time formatting.</summary>
    Time,
    /// <summary>Uses credit-card formatting and type detection.</summary>
    CreditCard,
    /// <summary>Uses the caller-supplied formatter.</summary>
    Custom
}
