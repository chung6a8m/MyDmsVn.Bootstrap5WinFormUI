using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Draws BootstrapSelect result, group, selection, and chip presentation without owning control state.
/// </summary>
public interface IBootstrapSelectRenderer
{
    /// <summary>Draws one selectable result row.</summary>
    void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context);

    /// <summary>Draws one non-selectable group header.</summary>
    void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context);

    /// <summary>Draws the single-selection text or placeholder.</summary>
    void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context);

    /// <summary>Draws one multiple-selection chip.</summary>
    void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context);
}
