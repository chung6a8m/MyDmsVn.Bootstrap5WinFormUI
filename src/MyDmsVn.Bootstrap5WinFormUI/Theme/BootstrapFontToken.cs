using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Describes a typography role without owning a GDI <see cref="Font"/> instance.
/// </summary>
public sealed class BootstrapFontToken
{
    /// <summary>
    /// Initializes a new typography token.
    /// </summary>
    public BootstrapFontToken(string fontFamilyName, float sizeInPoints, FontStyle style = FontStyle.Regular)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyName))
        {
            throw new ArgumentException("A font family name is required.", nameof(fontFamilyName));
        }

        if (float.IsNaN(sizeInPoints) || float.IsInfinity(sizeInPoints) || sizeInPoints <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInPoints), sizeInPoints, "Font size must be a finite positive value.");
        }

        FontFamilyName = fontFamilyName;
        SizeInPoints = sizeInPoints;
        Style = style;
    }

    /// <summary>
    /// Gets the preferred font family name.
    /// </summary>
    public string FontFamilyName { get; }

    /// <summary>
    /// Gets the font size in points.
    /// </summary>
    public float SizeInPoints { get; }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    public FontStyle Style { get; }
}
