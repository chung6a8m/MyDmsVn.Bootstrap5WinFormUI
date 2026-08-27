using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Adapts SVG icon descriptors to an application-supplied SVG renderer.
/// </summary>
public sealed class SvgIconProvider : IIconProvider
{
    private readonly ISvgIconRenderer _renderer;

    /// <summary>
    /// Initializes the provider with an SVG renderer adapter.
    /// </summary>
    public SvgIconProvider(ISvgIconRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <inheritdoc />
    public bool CanRender(IconDescriptor descriptor)
    {
        return descriptor is not null && descriptor.SourceKind == IconSourceKind.Svg;
    }

    /// <inheritdoc />
    public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        if (!CanRender(descriptor) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        return _renderer.TryRender(graphics, descriptor.Value, bounds, color);
    }
}
