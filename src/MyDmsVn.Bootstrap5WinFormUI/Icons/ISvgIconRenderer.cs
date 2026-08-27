using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Adapter contract for an SVG-capable renderer supplied outside the core package.
/// </summary>
public interface ISvgIconRenderer
{
    /// <summary>
    /// Attempts to render SVG markup in the requested bounds and color.
    /// </summary>
    bool TryRender(Graphics graphics, string svgMarkup, Rectangle bounds, Color color);
}
