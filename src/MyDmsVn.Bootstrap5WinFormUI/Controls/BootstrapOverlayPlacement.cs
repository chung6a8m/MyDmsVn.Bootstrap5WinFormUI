namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Specifies the preferred side and alignment of a floating Bootstrap overlay.
/// </summary>
public enum BootstrapOverlayPlacement
{
    /// <summary>Chooses the concrete side that minimizes overflow.</summary>
    Auto,
    /// <summary>Places the overlay centered above its anchor.</summary>
    Top,
    /// <summary>Places the overlay above and aligns logical starting edges.</summary>
    TopStart,
    /// <summary>Places the overlay above and aligns logical ending edges.</summary>
    TopEnd,
    /// <summary>Places the overlay centered below its anchor.</summary>
    Bottom,
    /// <summary>Places the overlay below and aligns logical starting edges.</summary>
    BottomStart,
    /// <summary>Places the overlay below and aligns logical ending edges.</summary>
    BottomEnd,
    /// <summary>Places the overlay centered to the left of its anchor.</summary>
    Left,
    /// <summary>Places the overlay left and aligns top edges.</summary>
    LeftStart,
    /// <summary>Places the overlay left and aligns bottom edges.</summary>
    LeftEnd,
    /// <summary>Places the overlay centered to the right of its anchor.</summary>
    Right,
    /// <summary>Places the overlay right and aligns top edges.</summary>
    RightStart,
    /// <summary>Places the overlay right and aligns bottom edges.</summary>
    RightEnd
}
