using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Renders one supported icon source into a target rectangle.
/// </summary>
public interface IIconProvider
{
    /// <summary>
    /// Returns whether this provider can handle the descriptor.
    /// </summary>
    bool CanRender(IconDescriptor descriptor);

    /// <summary>
    /// Attempts to render the descriptor using the requested foreground color.
    /// </summary>
    bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color);
}
