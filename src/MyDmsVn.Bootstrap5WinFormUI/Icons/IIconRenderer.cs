using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Source-neutral icon renderer consumed by framework controls.
/// </summary>
public interface IIconRenderer
{
    /// <summary>
    /// Attempts to render an icon in the requested bounds and color.
    /// </summary>
    bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color);
}
