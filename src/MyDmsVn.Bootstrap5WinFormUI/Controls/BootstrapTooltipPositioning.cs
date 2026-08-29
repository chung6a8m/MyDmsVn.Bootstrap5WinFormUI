namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Specifies whether a <see cref="BootstrapTooltip"/> uses native or framework-managed popup placement.
/// </summary>
public enum BootstrapTooltipPositioning
{
    /// <summary>Uses the native WinForms tooltip placement behavior.</summary>
    Native,
    /// <summary>Uses the shared overlay placement engine while retaining native tooltip timing and drawing.</summary>
    Managed
}
