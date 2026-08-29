using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Describes semantic state supplied to a BootstrapSelect renderer.</summary>
[Flags]
public enum BootstrapSelectRenderState
{
    /// <summary>No special state.</summary>
    None = 0,
    /// <summary>The logical value is selected.</summary>
    Selected = 1,
    /// <summary>The row is the current keyboard highlight.</summary>
    Highlighted = 2,
    /// <summary>The pointer is over the row.</summary>
    Hot = 4,
    /// <summary>The row cannot be newly selected.</summary>
    Disabled = 8
}

/// <summary>Provides presentation data for a selectable result row.</summary>
public sealed class BootstrapSelectResultRenderContext
{
    internal BootstrapSelectResultRenderContext(BootstrapSelectItem item, Rectangle bounds, BootstrapSelectRenderState state, int dpi, BootstrapTheme theme, Font font)
    {
        Item = item;
        Bounds = bounds;
        State = state;
        Dpi = dpi;
        Theme = theme;
        Font = font;
    }

    /// <summary>Gets the logical item.</summary>
    public BootstrapSelectItem Item { get; }
    /// <summary>Gets the drawing bounds.</summary>
    public Rectangle Bounds { get; }
    /// <summary>Gets semantic row state.</summary>
    public BootstrapSelectRenderState State { get; }
    /// <summary>Gets the target DPI.</summary>
    public int Dpi { get; }
    /// <summary>Gets the active Bootstrap theme.</summary>
    public BootstrapTheme Theme { get; }
    /// <summary>Gets the caller-owned font used by the control.</summary>
    public Font Font { get; }
}

/// <summary>Provides presentation data for a non-selectable group header.</summary>
public sealed class BootstrapSelectGroupRenderContext
{
    internal BootstrapSelectGroupRenderContext(string group, Rectangle bounds, int dpi, BootstrapTheme theme, Font font)
    {
        Group = group;
        Bounds = bounds;
        Dpi = dpi;
        Theme = theme;
        Font = font;
    }

    /// <summary>Gets the group display text.</summary>
    public string Group { get; }
    /// <summary>Gets the drawing bounds.</summary>
    public Rectangle Bounds { get; }
    /// <summary>Gets the target DPI.</summary>
    public int Dpi { get; }
    /// <summary>Gets the active Bootstrap theme.</summary>
    public BootstrapTheme Theme { get; }
    /// <summary>Gets the caller-owned font used by the control.</summary>
    public Font Font { get; }
}

/// <summary>Provides presentation data for the single-selection surface.</summary>
public sealed class BootstrapSelectSelectionRenderContext
{
    internal BootstrapSelectSelectionRenderContext(BootstrapSelectItem? item, string text, bool isPlaceholder, Rectangle bounds, int dpi, BootstrapTheme theme, Font font)
    {
        Item = item;
        Text = text;
        IsPlaceholder = isPlaceholder;
        Bounds = bounds;
        Dpi = dpi;
        Theme = theme;
        Font = font;
    }

    /// <summary>Gets the selected item, or null when a placeholder is shown.</summary>
    public BootstrapSelectItem? Item { get; }
    /// <summary>Gets the text to draw.</summary>
    public string Text { get; }
    /// <summary>Gets whether the text represents the placeholder.</summary>
    public bool IsPlaceholder { get; }
    /// <summary>Gets the drawing bounds.</summary>
    public Rectangle Bounds { get; }
    /// <summary>Gets the target DPI.</summary>
    public int Dpi { get; }
    /// <summary>Gets the active Bootstrap theme.</summary>
    public BootstrapTheme Theme { get; }
    /// <summary>Gets the caller-owned font used by the control.</summary>
    public Font Font { get; }
}

/// <summary>Provides presentation data for one multiple-selection chip.</summary>
public sealed class BootstrapSelectChipRenderContext
{
    internal BootstrapSelectChipRenderContext(BootstrapSelectItem item, Rectangle bounds, Rectangle removeBounds, BootstrapSelectRenderState state, int dpi, BootstrapTheme theme, Font font)
    {
        Item = item;
        Bounds = bounds;
        RemoveBounds = removeBounds;
        State = state;
        Dpi = dpi;
        Theme = theme;
        Font = font;
    }

    /// <summary>Gets the selected item represented by the chip.</summary>
    public BootstrapSelectItem Item { get; }
    /// <summary>Gets the complete chip bounds.</summary>
    public Rectangle Bounds { get; }
    /// <summary>Gets the remove-glyph hit/drawing bounds.</summary>
    public Rectangle RemoveBounds { get; }
    /// <summary>Gets semantic chip state.</summary>
    public BootstrapSelectRenderState State { get; }
    /// <summary>Gets the target DPI.</summary>
    public int Dpi { get; }
    /// <summary>Gets the active Bootstrap theme.</summary>
    public BootstrapTheme Theme { get; }
    /// <summary>Gets the caller-owned font used by the control.</summary>
    public Font Font { get; }
}
