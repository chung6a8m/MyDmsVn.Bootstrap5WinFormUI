using System;
using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal static class DemoTypography
{
    private const string FontFamilyName = "Segoe UI";

    private static readonly BootstrapThemeTypography Base14Typography =
        new BootstrapThemeTypography(
            new BootstrapFontToken(FontFamilyName, 10.5f),
            new BootstrapFontToken(FontFamilyName, 9.1875f),
            new BootstrapFontToken(FontFamilyName, 10.5f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 13.125f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 15.75f, FontStyle.Bold));

    private static readonly BootstrapThemeTypography Base16Typography =
        new BootstrapThemeTypography(
            new BootstrapFontToken(FontFamilyName, 12f),
            new BootstrapFontToken(FontFamilyName, 10.5f),
            new BootstrapFontToken(FontFamilyName, 12f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 15f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 18f, FontStyle.Bold));

    internal static BootstrapThemeTypography CreateThemeTypography(DemoTypographyPreset preset)
    {
        switch (preset)
        {
            case DemoTypographyPreset.Default:
                return BootstrapThemeTypography.Default;
            case DemoTypographyPreset.Base14Px:
                return Base14Typography;
            case DemoTypographyPreset.Base16Px:
                return Base16Typography;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(preset),
                    preset,
                    "Unsupported demo typography preset.");
        }
    }

    internal static bool TryGetPreset(
        BootstrapThemeTypography typography,
        out DemoTypographyPreset preset)
    {
        if (ReferenceEquals(typography, BootstrapThemeTypography.Default))
        {
            preset = DemoTypographyPreset.Default;
            return true;
        }

        if (ReferenceEquals(typography, Base14Typography))
        {
            preset = DemoTypographyPreset.Base14Px;
            return true;
        }

        if (ReferenceEquals(typography, Base16Typography))
        {
            preset = DemoTypographyPreset.Base16Px;
            return true;
        }

        preset = default;
        return false;
    }

    internal static Font CreateFont(BootstrapFontToken token)
    {
        return new Font(
            token.FontFamilyName,
            token.SizeInPoints,
            token.Style,
            GraphicsUnit.Point);
    }

    internal static bool FontMatchesToken(Font font, BootstrapFontToken token)
    {
        return string.Equals(font.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(font.SizeInPoints - token.SizeInPoints) <= 0.001f &&
            font.Style == token.Style;
    }
}
