namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Describes the origin of an effective <see cref="BootstrapSelect"/> selection change.
/// </summary>
public enum BootstrapSelectChangeReason
{
    /// <summary>
    /// The selection was changed through the public API.
    /// </summary>
    Programmatic = 0,

    /// <summary>
    /// The selection was changed through pointer interaction.
    /// </summary>
    Mouse = 1,

    /// <summary>
    /// The selection was changed through keyboard interaction.
    /// </summary>
    Keyboard = 2,

    /// <summary>
    /// One or more selections were removed by a clear operation.
    /// </summary>
    Clear = 3,

    /// <summary>
    /// A selected item was removed through its chip affordance.
    /// </summary>
    ChipRemove = 4,

    /// <summary>
    /// A new custom value was created and selected.
    /// </summary>
    CustomValue = 5,

    /// <summary>
    /// Selection changed while normalizing a selection-mode transition.
    /// </summary>
    ModeChange = 6
}
