namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Classification of a pending lexical input before host acceptance.</summary>
public enum InputParseState
{
    /// <summary>A required input has no content.</summary>
    Empty,
    /// <summary>The text is a possible prefix but cannot yet supply a value.</summary>
    Incomplete,
    /// <summary>The text cannot be accepted under the captured profile.</summary>
    Invalid,
    /// <summary>The text supplies an exact value, or an allowed null.</summary>
    Valid
}
