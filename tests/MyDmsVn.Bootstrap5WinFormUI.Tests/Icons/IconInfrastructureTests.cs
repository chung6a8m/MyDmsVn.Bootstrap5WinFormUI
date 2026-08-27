using System;
using System.Collections.Generic;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Icons;

[TestFixture]
public sealed class IconInfrastructureTests
{
    [Test]
    public void DescriptorFactoriesPreserveSourceMetadata()
    {
        var mdl2 = IconDescriptor.SegoeMdl2('\uE72E');
        var svg = IconDescriptor.Svg("<svg viewBox=\"0 0 24 24\"></svg>");
        var vector = IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown);
        var external = IconDescriptor.External("FontAwesome.Sharp", "House");

        Assert.That(mdl2.SourceKind, Is.EqualTo(IconSourceKind.SegoeMdl2));
        Assert.That(mdl2.Value, Is.EqualTo("\uE72E"));
        Assert.That(svg.SourceKind, Is.EqualTo(IconSourceKind.Svg));
        Assert.That(vector.SourceKind, Is.EqualTo(IconSourceKind.FrameworkVector));
        Assert.That(vector.Value, Is.EqualTo(nameof(FrameworkIconGlyph.ChevronDown)));
        Assert.That(external.SourceKind, Is.EqualTo(IconSourceKind.External));
        Assert.That(external.SourceId, Is.EqualTo("FontAwesome.Sharp"));
        Assert.That(external.Value, Is.EqualTo("House"));
    }

    [Test]
    public void ExternalDescriptorRejectsMissingSourceId()
    {
        Action action = () => IconDescriptor.External(string.Empty, "House");

        Assert.That(action, Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void RendererUsesFirstProviderThatAcceptsDescriptor()
    {
        var descriptor = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        var first = new RecordingProvider(false);
        var second = new RecordingProvider(true);
        var third = new RecordingProvider(true);
        var renderer = new BootstrapIconRenderer(new IIconProvider[] { first, second, third });

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        var rendered = renderer.TryRender(graphics, descriptor, new Rectangle(0, 0, 24, 24), Color.Red);

        Assert.That(rendered, Is.True);
        Assert.That(first.RenderCalls, Is.EqualTo(0));
        Assert.That(second.RenderCalls, Is.EqualTo(1));
        Assert.That(third.RenderCalls, Is.EqualTo(0));
    }

    [Test]
    public void RendererReturnsFalseWhenNoProviderMatches()
    {
        var renderer = new BootstrapIconRenderer(new IIconProvider[] { new RecordingProvider(false) });

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        var rendered = renderer.TryRender(
            graphics,
            IconDescriptor.External("Unknown", "Anything"),
            new Rectangle(0, 0, 24, 24),
            Color.Black);

        Assert.That(rendered, Is.False);
    }

    [Test]
    public void SvgProviderDelegatesToConfiguredSvgRenderer()
    {
        const string markup = "<svg viewBox=\"0 0 24 24\"><path d=\"M2 2h20v20H2z\"/></svg>";
        var svgRenderer = new RecordingSvgRenderer();
        var provider = new SvgIconProvider(svgRenderer);
        var descriptor = IconDescriptor.Svg(markup);
        var bounds = new Rectangle(3, 4, 18, 16);
        var color = Color.RoyalBlue;

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        var rendered = provider.TryRender(graphics, descriptor, bounds, color);

        Assert.That(rendered, Is.True);
        Assert.That(svgRenderer.Markup, Is.EqualTo(markup));
        Assert.That(svgRenderer.Bounds, Is.EqualTo(bounds));
        Assert.That(svgRenderer.Color, Is.EqualTo(color));
    }

    [Test]
    public void FrameworkVectorProviderRendersBuiltInGlyph()
    {
        var provider = new FrameworkVectorIconProvider();

        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        var rendered = provider.TryRender(
            graphics,
            IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown),
            new Rectangle(0, 0, 32, 32),
            Color.Black);

        Assert.That(rendered, Is.True);
    }

    private sealed class RecordingProvider : IIconProvider
    {
        private readonly bool _accepts;

        public RecordingProvider(bool accepts)
        {
            _accepts = accepts;
        }

        public int RenderCalls { get; private set; }

        public bool CanRender(IconDescriptor descriptor)
        {
            return _accepts;
        }

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            RenderCalls++;
            return true;
        }
    }

    private sealed class RecordingSvgRenderer : ISvgIconRenderer
    {
        public string? Markup { get; private set; }

        public Rectangle Bounds { get; private set; }

        public Color Color { get; private set; }

        public bool TryRender(Graphics graphics, string svgMarkup, Rectangle bounds, Color color)
        {
            Markup = svgMarkup;
            Bounds = bounds;
            Color = color;
            return true;
        }
    }
}
