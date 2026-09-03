using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal enum BootstrapCheckableKind
{
    CheckBox,
    RadioButton,
    Switch
}

internal enum BootstrapCheckableHorizontalSlot
{
    Left,
    Center,
    Right
}

internal readonly struct BootstrapCheckableMetrics
{
    public BootstrapCheckableMetrics(int indicatorSize, Size indicatorBoundsSize, int textGap, int borderWidth, int focusWidth, float radius)
    {
        IndicatorSize = indicatorSize;
        IndicatorBoundsSize = indicatorBoundsSize;
        TextGap = textGap;
        BorderWidth = borderWidth;
        FocusWidth = focusWidth;
        Radius = radius;
    }

    public int IndicatorSize { get; }
    public Size IndicatorBoundsSize { get; }
    public int TextGap { get; }
    public int BorderWidth { get; }
    public int FocusWidth { get; }
    public float Radius { get; }
}

internal readonly struct BootstrapCheckablePalette
{
    public BootstrapCheckablePalette(Color surface, Color border, Color fill, Color glyph, Color text, Color focus)
    {
        Surface = surface;
        Border = border;
        Fill = fill;
        Glyph = glyph;
        Text = text;
        Focus = focus;
    }

    public Color Surface { get; }
    public Color Border { get; }
    public Color Fill { get; }
    public Color Glyph { get; }
    public Color Text { get; }
    public Color Focus { get; }
}

internal readonly struct BootstrapCheckableLayout
{
    public BootstrapCheckableLayout(Rectangle indicatorBounds, Rectangle textBounds)
    {
        IndicatorBounds = indicatorBounds;
        TextBounds = textBounds;
    }

    public Rectangle IndicatorBounds { get; }
    public Rectangle TextBounds { get; }
}

internal static class BootstrapCheckableRenderLogic
{
    public static BootstrapCheckableMetrics GetMetrics(BootstrapCheckableKind kind, BootstrapThemeMetrics metrics, int dpi)
    {
        if (metrics is null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (kind < BootstrapCheckableKind.CheckBox || kind > BootstrapCheckableKind.Switch)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported checkable kind.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        var indicator = DpiScaler.Scale(metrics.SpacingLG, dpi);
        var size = kind == BootstrapCheckableKind.Switch
            ? new Size(indicator * 2, indicator)
            : new Size(indicator, indicator);
        var radius = kind == BootstrapCheckableKind.RadioButton || kind == BootstrapCheckableKind.Switch
            ? indicator / 2f
            : Math.Min(DpiScaler.Scale((float)metrics.RadiusSmall, dpi), indicator / 2f);

        return new BootstrapCheckableMetrics(
            indicator,
            size,
            DpiScaler.Scale(metrics.SpacingSM, dpi),
            DpiScaler.Scale(metrics.BorderWidth, dpi),
            DpiScaler.Scale(metrics.FocusBorderWidth, dpi),
            radius);
    }

    public static BootstrapCheckablePalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        BootstrapValidationState validationState,
        CheckState checkState,
        bool enabled)
    {
        if (colors is null)
        {
            throw new ArgumentNullException(nameof(colors));
        }

        var variantColor = BootstrapVariantColorResolver.Resolve(colors, variant);
        BootstrapTextBoxRenderLogic.ValidateState(validationState);
        ValidateCheckState(checkState);

        if (!enabled)
        {
            return new BootstrapCheckablePalette(colors.SurfaceSecondary, colors.Disabled, colors.Disabled, colors.Surface, colors.MutedText, colors.Focus);
        }

        var validationColor = validationState == BootstrapValidationState.Valid
            ? colors.Success
            : validationState == BootstrapValidationState.Invalid
                ? colors.Danger
                : Color.Empty;
        var activeColor = validationColor.IsEmpty ? variantColor : validationColor;
        var isActive = checkState != CheckState.Unchecked;
        var border = !validationColor.IsEmpty ? validationColor : isActive ? activeColor : colors.Border;
        var fill = isActive ? activeColor : colors.Surface;
        var text = validationColor.IsEmpty ? colors.Text : validationColor;
        var glyph = ColorUtil.GetContrastingTextColor(activeColor, colors.Light, colors.Dark);
        return new BootstrapCheckablePalette(colors.Surface, border, fill, glyph, text, colors.Focus);
    }

    public static BootstrapCheckableHorizontalSlot ResolveIndicatorSlot(ContentAlignment checkAlign, bool rightToLeft)
    {
        ValidateAlignment(checkAlign);
        var alignedLeft = checkAlign == ContentAlignment.TopLeft || checkAlign == ContentAlignment.MiddleLeft || checkAlign == ContentAlignment.BottomLeft;
        var alignedRight = checkAlign == ContentAlignment.TopRight || checkAlign == ContentAlignment.MiddleRight || checkAlign == ContentAlignment.BottomRight;
        if (!alignedLeft && !alignedRight)
        {
            return BootstrapCheckableHorizontalSlot.Center;
        }

        var slot = alignedLeft ? BootstrapCheckableHorizontalSlot.Left : BootstrapCheckableHorizontalSlot.Right;
        if (!rightToLeft)
        {
            return slot;
        }

        return slot == BootstrapCheckableHorizontalSlot.Left
            ? BootstrapCheckableHorizontalSlot.Right
            : BootstrapCheckableHorizontalSlot.Left;
    }

    public static BootstrapCheckableLayout GetLayout(Rectangle clientBounds, Padding padding, BootstrapCheckableMetrics metrics, ContentAlignment checkAlign, bool rightToLeft)
    {
        var content = Rectangle.FromLTRB(
            Math.Min(clientBounds.Right, clientBounds.Left + Math.Max(0, padding.Left)),
            Math.Min(clientBounds.Bottom, clientBounds.Top + Math.Max(0, padding.Top)),
            Math.Max(clientBounds.Left, clientBounds.Right - Math.Max(0, padding.Right)),
            Math.Max(clientBounds.Top, clientBounds.Bottom - Math.Max(0, padding.Bottom)));
        if (content.Width < 0 || content.Height < 0)
        {
            content = Rectangle.Empty;
        }

        var indicatorWidth = Math.Min(Math.Max(0, metrics.IndicatorBoundsSize.Width), Math.Max(0, content.Width));
        var indicatorHeight = Math.Min(Math.Max(0, metrics.IndicatorBoundsSize.Height), Math.Max(0, content.Height));
        var y = AlignVertically(content, indicatorHeight, checkAlign);
        var slot = ResolveIndicatorSlot(checkAlign, rightToLeft);
        var x = slot == BootstrapCheckableHorizontalSlot.Left
            ? content.Left
            : slot == BootstrapCheckableHorizontalSlot.Right
                ? content.Right - indicatorWidth
                : content.Left + (content.Width - indicatorWidth) / 2;
        var indicator = new Rectangle(x, y, indicatorWidth, indicatorHeight);

        Rectangle text;
        if (slot == BootstrapCheckableHorizontalSlot.Center)
        {
            var verticalGap = Math.Min(metrics.TextGap, Math.Max(0, content.Height - indicatorHeight));
            if (checkAlign == ContentAlignment.TopCenter)
            {
                var textTop = Math.Min(content.Bottom, indicator.Bottom + verticalGap);
                text = Rectangle.FromLTRB(content.Left, textTop, content.Right, content.Bottom);
            }
            else if (checkAlign == ContentAlignment.BottomCenter)
            {
                var textBottom = Math.Max(content.Top, indicator.Top - verticalGap);
                text = Rectangle.FromLTRB(content.Left, content.Top, content.Right, textBottom);
            }
            else
            {
                text = content;
            }
        }
        else
        {
            var horizontalGap = Math.Min(metrics.TextGap, Math.Max(0, content.Width - indicatorWidth));
            text = slot == BootstrapCheckableHorizontalSlot.Left
                ? Rectangle.FromLTRB(Math.Min(content.Right, indicator.Right + horizontalGap), content.Top, content.Right, content.Bottom)
                : Rectangle.FromLTRB(content.Left, content.Top, Math.Max(content.Left, indicator.Left - horizontalGap), content.Bottom);
        }

        return new BootstrapCheckableLayout(indicator, text);
    }

    public static Size GetPreferredSize(Size textSize, Padding padding, BootstrapCheckableMetrics metrics)
    {
        return GetPreferredSize(textSize, padding, metrics, ContentAlignment.MiddleLeft);
    }

    public static Size GetPreferredSize(Size textSize, Padding padding, BootstrapCheckableMetrics metrics, ContentAlignment checkAlign)
    {
        if (textSize.Width < 0 || textSize.Height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textSize), textSize, "Text size cannot contain negative dimensions.");
        }

        ValidateAlignment(checkAlign);
        var hasText = textSize.Width > 0 || textSize.Height > 0;
        var indicatorWidth = Math.Max(0, metrics.IndicatorBoundsSize.Width);
        var indicatorHeight = Math.Max(0, metrics.IndicatorBoundsSize.Height);
        int width;
        int height;

        if (checkAlign == ContentAlignment.TopCenter || checkAlign == ContentAlignment.BottomCenter)
        {
            width = padding.Horizontal + Math.Max(indicatorWidth, textSize.Width);
            height = padding.Vertical + indicatorHeight + (hasText ? metrics.TextGap + textSize.Height : 0) + metrics.FocusWidth;
        }
        else if (checkAlign == ContentAlignment.MiddleCenter)
        {
            width = padding.Horizontal + Math.Max(indicatorWidth, textSize.Width);
            height = padding.Vertical + Math.Max(indicatorHeight, textSize.Height) + metrics.FocusWidth;
        }
        else
        {
            width = padding.Horizontal + indicatorWidth + (hasText ? metrics.TextGap + textSize.Width : 0);
            height = padding.Vertical + Math.Max(indicatorHeight, textSize.Height) + metrics.FocusWidth;
        }

        return new Size(Math.Max(0, width), Math.Max(0, height));
    }

    public static Rectangle GetRadioDotBounds(Rectangle indicatorBounds)
    {
        if (indicatorBounds.Width <= 0 || indicatorBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        var diameter = Math.Min(indicatorBounds.Width, indicatorBounds.Height);
        var inset = Math.Max(3, diameter / 4);
        var width = indicatorBounds.Width - inset * 2;
        var height = indicatorBounds.Height - inset * 2;
        if (width <= 0 || height <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(indicatorBounds.X + inset, indicatorBounds.Y + inset, width, height);
    }

    public static Rectangle GetSwitchThumbBounds(Rectangle track, int inset, CheckState checkState, bool rightToLeft)
    {
        ValidateCheckState(checkState);
        inset = Math.Max(0, inset);
        var diameter = Math.Max(0, Math.Min(track.Height - inset * 2, track.Width - inset * 2));
        var start = track.Left + inset;
        var end = track.Right - inset - diameter;
        int x;
        if (checkState == CheckState.Indeterminate)
        {
            x = track.Left + (track.Width - diameter) / 2;
        }
        else
        {
            var logicalEnd = checkState == CheckState.Checked;
            x = logicalEnd ^ rightToLeft ? end : start;
        }

        return new Rectangle(x, track.Top + inset, diameter, diameter);
    }

    public static bool ShouldUseNativeFallback(Appearance appearance, bool hasImage, bool hasImageList, int imageIndex, string? imageKey)
    {
        return ShouldUseNativeFallback(appearance, FlatStyle.Standard, hasImage, hasImageList, imageIndex, imageKey);
    }

    public static bool ShouldUseNativeFallback(Appearance appearance, FlatStyle flatStyle, bool hasImage, bool hasImageList, int imageIndex, string? imageKey)
    {
        return flatStyle == FlatStyle.System
            || appearance != Appearance.Normal
            || hasImage
            || (hasImageList && (imageIndex >= 0 || !string.IsNullOrEmpty(imageKey)));
    }

    public static TextFormatFlags GetTextFormatFlags(ContentAlignment textAlign, bool useMnemonic, bool showKeyboardCues, bool autoEllipsis, bool rightToLeft)
    {
        ValidateAlignment(textAlign);
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.PreserveGraphicsClipping;
        if (!useMnemonic)
        {
            flags |= TextFormatFlags.NoPrefix;
        }
        else if (!showKeyboardCues)
        {
            flags |= TextFormatFlags.HidePrefix;
        }

        if (autoEllipsis) flags |= TextFormatFlags.EndEllipsis;
        if (rightToLeft) flags |= TextFormatFlags.RightToLeft;

        var effectiveTextAlign = rightToLeft ? MirrorHorizontalAlignment(textAlign) : textAlign;
        switch (effectiveTextAlign)
        {
            case ContentAlignment.TopCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.BottomCenter:
                flags |= TextFormatFlags.HorizontalCenter;
                break;
            case ContentAlignment.TopRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.BottomRight:
                flags |= TextFormatFlags.Right;
                break;
        }

        switch (textAlign)
        {
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleRight:
                flags |= TextFormatFlags.VerticalCenter;
                break;
            case ContentAlignment.BottomLeft:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomRight:
                flags |= TextFormatFlags.Bottom;
                break;
        }

        return flags;
    }

    private static ContentAlignment MirrorHorizontalAlignment(ContentAlignment alignment)
    {
        switch (alignment)
        {
            case ContentAlignment.TopLeft: return ContentAlignment.TopRight;
            case ContentAlignment.MiddleLeft: return ContentAlignment.MiddleRight;
            case ContentAlignment.BottomLeft: return ContentAlignment.BottomRight;
            case ContentAlignment.TopRight: return ContentAlignment.TopLeft;
            case ContentAlignment.MiddleRight: return ContentAlignment.MiddleLeft;
            case ContentAlignment.BottomRight: return ContentAlignment.BottomLeft;
            default: return alignment;
        }
    }

    private static int AlignVertically(Rectangle bounds, int height, ContentAlignment alignment)
    {
        if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
        {
            return bounds.Top;
        }

        if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight)
        {
            return bounds.Bottom - height;
        }

        return bounds.Top + (bounds.Height - height) / 2;
    }

    private static void ValidateAlignment(ContentAlignment alignment)
    {
        if (!Enum.IsDefined(typeof(ContentAlignment), alignment))
        {
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unsupported content alignment.");
        }
    }

    private static void ValidateCheckState(CheckState checkState)
    {
        if (checkState < CheckState.Unchecked || checkState > CheckState.Indeterminate)
        {
            throw new ArgumentOutOfRangeException(nameof(checkState), checkState, "Unsupported check state.");
        }
    }
}
