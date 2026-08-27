using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Icons;

/// <summary>
/// Renders Windows Segoe MDL2 Assets font glyphs.
/// </summary>
public sealed class SegoeMdl2IconProvider : IIconProvider
{
    /// <summary>
    /// Gets the Windows font family used by this provider.
    /// </summary>
    public const string FontFamilyName = "Segoe MDL2 Assets";

    /// <inheritdoc />
    public bool CanRender(IconDescriptor descriptor)
    {
        return descriptor is not null && descriptor.SourceKind == IconSourceKind.SegoeMdl2;
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

        var pixelSize = Math.Max(1f, Math.Min(bounds.Width, bounds.Height) * 0.75f);

        try
        {
            using var font = new Font(FontFamilyName, pixelSize, FontStyle.Regular, GraphicsUnit.Pixel);
            if (!string.Equals(font.FontFamily.Name, FontFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            TextRenderer.DrawText(
                graphics,
                descriptor.Value,
                font,
                bounds,
                color,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
