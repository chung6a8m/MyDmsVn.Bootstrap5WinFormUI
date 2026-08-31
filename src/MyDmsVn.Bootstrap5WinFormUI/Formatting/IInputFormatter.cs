namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Defines deterministic formatting and canonical unformatting for text input.</summary>
public interface IInputFormatter
{
    /// <summary>Formats a canonical raw value for display.</summary>
    string Format(string rawValue);

    /// <summary>Converts a candidate display value to canonical raw text.</summary>
    string Unformat(string formattedValue);
}
