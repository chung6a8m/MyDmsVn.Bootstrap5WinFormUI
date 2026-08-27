namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes the validation presentation of a <see cref="BootstrapTextBox"/>.
/// </summary>
public enum BootstrapValidationState
{
    /// <summary>No validation result is being presented.</summary>
    None = 0,

    /// <summary>The current value is valid.</summary>
    Valid = 1,

    /// <summary>The current value is invalid.</summary>
    Invalid = 2
}
