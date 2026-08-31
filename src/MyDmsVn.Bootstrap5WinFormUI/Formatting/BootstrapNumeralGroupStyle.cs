namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Specifies how numeral integer digits are grouped.</summary>
public enum BootstrapNumeralGroupStyle
{
    /// <summary>Does not group digits.</summary>
    None,
    /// <summary>Groups digits in thousands.</summary>
    Thousand,
    /// <summary>Uses the Indian lakh grouping pattern.</summary>
    Lakh,
    /// <summary>Groups digits in sets of four.</summary>
    Wan
}
