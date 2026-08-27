using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Theme;

/// <summary>
/// Groups the typography roles used by the framework.
/// </summary>
public sealed class BootstrapThemeTypography
{
    private static readonly BootstrapThemeTypography DefaultTypography = new BootstrapThemeTypography(
        new BootstrapFontToken("Segoe UI", 9f),
        new BootstrapFontToken("Segoe UI", 8.25f),
        new BootstrapFontToken("Segoe UI", 9f, FontStyle.Bold),
        new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
        new BootstrapFontToken("Segoe UI", 14f, FontStyle.Bold));

    /// <summary>
    /// Initializes a new typography token set.
    /// </summary>
    public BootstrapThemeTypography(
        BootstrapFontToken body,
        BootstrapFontToken bodySmall,
        BootstrapFontToken label,
        BootstrapFontToken headingSmall,
        BootstrapFontToken headingMedium)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        BodySmall = bodySmall ?? throw new ArgumentNullException(nameof(bodySmall));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        HeadingSmall = headingSmall ?? throw new ArgumentNullException(nameof(headingSmall));
        HeadingMedium = headingMedium ?? throw new ArgumentNullException(nameof(headingMedium));
    }

    /// <summary>
    /// Gets the default desktop typography tokens.
    /// </summary>
    public static BootstrapThemeTypography Default => DefaultTypography;

    /// <summary>
    /// Gets the normal body-text token.
    /// </summary>
    public BootstrapFontToken Body { get; }

    /// <summary>
    /// Gets the compact body-text token.
    /// </summary>
    public BootstrapFontToken BodySmall { get; }

    /// <summary>
    /// Gets the label token.
    /// </summary>
    public BootstrapFontToken Label { get; }

    /// <summary>
    /// Gets the small heading token.
    /// </summary>
    public BootstrapFontToken HeadingSmall { get; }

    /// <summary>
    /// Gets the medium heading token.
    /// </summary>
    public BootstrapFontToken HeadingMedium { get; }
}
