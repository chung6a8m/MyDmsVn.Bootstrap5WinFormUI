namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Defines how a <see cref="BootstrapButtonGroup"/> manages the selected state of its buttons.
/// </summary>
public enum BootstrapButtonSelectionMode
{
    /// <summary>
    /// The group does not change button selection state.
    /// </summary>
    None = 0,

    /// <summary>
    /// Activating a button selects it and clears selection from the other buttons in the group.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Activating a button toggles only that button's selected state.
    /// </summary>
    Multiple = 2
}
