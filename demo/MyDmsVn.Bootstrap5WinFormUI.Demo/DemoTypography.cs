using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal static class DemoTypography
{
    internal const string FontFamilyName = "Segoe UI";
    internal const float BodySizeInPoints = 12f;
    internal const float BodySmallSizeInPoints = 10.5f;
    internal const float LabelSizeInPoints = 12f;
    internal const float HeadingSmallSizeInPoints = 15f;
    internal const float HeadingMediumSizeInPoints = 18f;

    internal static BootstrapThemeTypography CreateThemeTypography()
    {
        return new BootstrapThemeTypography(
            new BootstrapFontToken(FontFamilyName, BodySizeInPoints),
            new BootstrapFontToken(FontFamilyName, BodySmallSizeInPoints),
            new BootstrapFontToken(FontFamilyName, LabelSizeInPoints, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, HeadingSmallSizeInPoints, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, HeadingMediumSizeInPoints, FontStyle.Bold));
    }

    internal static Font CreateBodyFont()
    {
        return new Font(
            FontFamilyName,
            BodySizeInPoints,
            FontStyle.Regular,
            GraphicsUnit.Point);
    }
}
