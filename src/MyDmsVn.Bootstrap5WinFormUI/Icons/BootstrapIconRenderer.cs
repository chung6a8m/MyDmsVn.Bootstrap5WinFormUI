using System;
using System.Collections.Generic;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Dispatches source-neutral icon descriptors to registered providers in order.
/// </summary>
public sealed class BootstrapIconRenderer : IIconRenderer
{
    private readonly IIconProvider[] _providers;

    /// <summary>
    /// Initializes a renderer with an ordered set of icon providers.
    /// </summary>
    public BootstrapIconRenderer(IEnumerable<IIconProvider> providers)
    {
        if (providers is null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        var collected = new List<IIconProvider>();
        foreach (var provider in providers)
        {
            if (provider is null)
            {
                throw new ArgumentException("Icon provider collections must not contain null entries.", nameof(providers));
            }

            collected.Add(provider);
        }

        _providers = collected.ToArray();
    }

    /// <summary>
    /// Creates the built-in renderer with Segoe MDL2 and framework-vector providers.
    /// </summary>
    public static BootstrapIconRenderer CreateDefault()
    {
        return new BootstrapIconRenderer(new IIconProvider[]
        {
            new SegoeMdl2IconProvider(),
            new FrameworkVectorIconProvider()
        });
    }

    /// <inheritdoc />
    public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return false;
        }

        foreach (var provider in _providers)
        {
            if (provider.CanRender(descriptor)
                && provider.TryRender(graphics, descriptor, bounds, color))
            {
                return true;
            }
        }

        return false;
    }
}
