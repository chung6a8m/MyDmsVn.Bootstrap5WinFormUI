using System;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Describes an icon independently from the provider that renders it.
/// </summary>
public sealed class IconDescriptor
{
    private IconDescriptor(IconSourceKind sourceKind, string value, string? sourceId)
    {
        SourceKind = sourceKind;
        Value = value;
        SourceId = sourceId;
    }

    /// <summary>
    /// Gets the icon source category.
    /// </summary>
    public IconSourceKind SourceKind { get; }

    /// <summary>
    /// Gets the source-specific value, such as a glyph, SVG markup, framework glyph name, or external icon key.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the external provider identifier when <see cref="SourceKind"/> is <see cref="IconSourceKind.External"/>.
    /// </summary>
    public string? SourceId { get; }

    /// <summary>
    /// Creates a descriptor for a Segoe MDL2 Assets glyph.
    /// </summary>
    public static IconDescriptor SegoeMdl2(char glyph)
    {
        if (glyph == '\0')
        {
            throw new ArgumentOutOfRangeException(nameof(glyph));
        }

        return new IconDescriptor(IconSourceKind.SegoeMdl2, glyph.ToString(), null);
    }

    /// <summary>
    /// Creates a descriptor containing SVG markup.
    /// </summary>
    public static IconDescriptor Svg(string svgMarkup)
    {
        if (string.IsNullOrWhiteSpace(svgMarkup))
        {
            throw new ArgumentException("SVG markup must not be empty.", nameof(svgMarkup));
        }

        return new IconDescriptor(IconSourceKind.Svg, svgMarkup, null);
    }

    /// <summary>
    /// Creates a descriptor for a framework-owned vector glyph.
    /// </summary>
    public static IconDescriptor Framework(FrameworkIconGlyph glyph)
    {
        if (!Enum.IsDefined(typeof(FrameworkIconGlyph), glyph))
        {
            throw new ArgumentOutOfRangeException(nameof(glyph));
        }

        return new IconDescriptor(IconSourceKind.FrameworkVector, glyph.ToString(), null);
    }

    /// <summary>
    /// Creates a descriptor owned by an optional or application-defined provider.
    /// </summary>
    public static IconDescriptor External(string sourceId, string value)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("External icon source id must not be empty.", nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("External icon value must not be empty.", nameof(value));
        }

        return new IconDescriptor(IconSourceKind.External, value, sourceId);
    }
}
