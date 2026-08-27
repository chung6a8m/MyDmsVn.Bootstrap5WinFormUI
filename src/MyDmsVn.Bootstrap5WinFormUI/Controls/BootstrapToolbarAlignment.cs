namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Defines how button groups are positioned along a <see cref="BootstrapButtonToolbar"/> main axis.
/// </summary>
public enum BootstrapToolbarAlignment
{
    /// <summary>
    /// Places groups at the leading edge. In horizontal orientation this is the left edge.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Centers the combined groups on the toolbar main axis.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Places groups at the trailing edge. In horizontal orientation this is the right edge.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Places the first and last groups at opposite edges and distributes remaining space between groups.
    /// </summary>
    SpaceBetween = 3
}
