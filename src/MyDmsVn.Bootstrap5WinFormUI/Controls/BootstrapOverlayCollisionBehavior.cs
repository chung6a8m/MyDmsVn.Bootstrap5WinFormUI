namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Specifies how a floating Bootstrap overlay responds to boundary collisions.
/// </summary>
public enum BootstrapOverlayCollisionBehavior
{
    /// <summary>Returns the preferred geometry without collision correction.</summary>
    None,
    /// <summary>Flips to the exact opposite side when that improves main-axis fit.</summary>
    Flip,
    /// <summary>Shifts only along the cross axis to improve boundary fit.</summary>
    Shift,
    /// <summary>Applies exact-opposite flipping followed by cross-axis shifting.</summary>
    FlipAndShift
}
