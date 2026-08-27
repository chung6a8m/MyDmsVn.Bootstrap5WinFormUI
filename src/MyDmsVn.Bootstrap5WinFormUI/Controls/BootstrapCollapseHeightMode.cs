namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Specifies how <see cref="BootstrapCollapse"/> determines its expanded height.
/// </summary>
public enum BootstrapCollapseHeightMode
{
    /// <summary>
    /// Measures the visible child content and padding to determine the expanded height.
    /// </summary>
    Auto,

    /// <summary>
    /// Uses <see cref="BootstrapCollapse.ExpandedHeight"/> as the expanded height.
    /// </summary>
    Fixed
}
