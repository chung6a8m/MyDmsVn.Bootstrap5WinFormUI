using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining the native <see cref="ListView"/> contract.
/// </summary>
public class BootstrapListView : ListView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _striped;
    private bool _hoverHighlight = true;
    private int _hoveredItemIndex = -1;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _initialized;
    private Font? _themeFont;

    /// <summary>Initializes a new instance of the <see cref="BootstrapListView"/> class.</summary>
    public BootstrapListView()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
        _initialized = true;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyThemePresentation();
    }

    /// <summary>Gets or sets the Bootstrap semantic variant used for selected-item presentation.</summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for selected-item presentation.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value) return;
            _variant = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether neutral rows alternate in Details and List views.</summary>
    [Category("Appearance")]
    [Description("Alternates neutral row backgrounds in Details and List views.")]
    [DefaultValue(false)]
    public bool Striped
    {
        get => _striped;
        set
        {
            if (_striped == value) return;
            _striped = value;
            Invalidate();
        }
    }

    /// <summary>Gets or sets whether the item under the pointer receives presentation-only highlighting.</summary>
    [Category("Appearance")]
    [Description("Highlights the item under the pointer without changing native selection behavior.")]
    [DefaultValue(true)]
    public bool HoverHighlight
    {
        get => _hoverHighlight;
        set
        {
            if (_hoverHighlight == value) return;
            _hoverHighlight = value;
            if (value) Invalidate(); else ClearHover();
        }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_initialized) return;
        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (_initialized && !IsDisposed && !Disposing) Invalidate();
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        OwnerDraw = true;
        base.OnHandleCreated(e);
        _hoveredItemIndex = -1;
        if (_initialized && !IsDisposed && !Disposing)
        {
            ApplyThemePresentation();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _hoveredItemIndex = -1;
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_hoverHighlight || IsDisposed || Disposing || !IsHandleCreated) return;
        var hit = HitTest(e.X, e.Y);
        UpdateHoveredIndex(hit.Item?.Index ?? -1);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        ClearHover();
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        if (HeaderStyle != ColumnHeaderStyle.None)
        {
            var theme = BootstrapThemeManager.CurrentTheme;
            using (var background = new SolidBrush(theme.Colors.SurfaceSecondary))
            using (var separator = new Pen(theme.Colors.Border, Math.Max(1, DpiScaler.Scale(1, GetCurrentDpi()))))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawLine(separator, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                e.Graphics.DrawLine(separator, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            var textBounds = BootstrapListViewLayoutLogic.Deflate(
                e.Bounds,
                DpiScaler.Scale(theme.Metrics.SpacingSM, GetCurrentDpi()),
                0);
            var headerImage = e.Header is null
                ? null
                : ResolveImage(SmallImageList, e.Header.ImageKey, e.Header.ImageIndex);
            if (headerImage is not null && !textBounds.IsEmpty)
            {
                var imageWidth = Math.Min(headerImage.Width, textBounds.Width);
                var imageBounds = RightToLeft == RightToLeft.Yes
                    ? new Rectangle(textBounds.Right - imageWidth, textBounds.Top, imageWidth, textBounds.Height)
                    : new Rectangle(textBounds.Left, textBounds.Top, imageWidth, textBounds.Height);
                DrawImage(e.Graphics, headerImage, imageBounds);
                var gap = DpiScaler.Scale(theme.Metrics.SpacingXS, GetCurrentDpi());
                textBounds = RightToLeft == RightToLeft.Yes
                    ? Rectangle.FromLTRB(textBounds.Left, textBounds.Top, Math.Max(textBounds.Left, imageBounds.Left - gap), textBounds.Bottom)
                    : Rectangle.FromLTRB(Math.Min(textBounds.Right, imageBounds.Right + gap), textBounds.Top, textBounds.Right, textBounds.Bottom);
            }

            if (!textBounds.IsEmpty)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header?.Text ?? string.Empty,
                    Font,
                    textBounds,
                    theme.Colors.Text,
                    BootstrapListViewLayoutLogic.GetTextFlags(
                        e.Header?.TextAlign ?? HorizontalAlignment.Left,
                        RightToLeft == RightToLeft.Yes,
                        false));
            }
        }

        base.OnDrawColumnHeader(e);
        e.DrawDefault = false;
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        if (View == View.Details)
        {
            base.OnDrawItem(e);
            e.DrawDefault = false;
            return;
        }

        DrawNonDetailsItem(e);
        base.OnDrawItem(e);
        e.DrawDefault = false;
    }

    /// <inheritdoc />
    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (View == View.Details) DrawDetailsSubItem(e);
        base.OnDrawSubItem(e);
        e.DrawDefault = false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _initialized = false;
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            _hoveredItemIndex = -1;
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void DrawDetailsSubItem(DrawListViewSubItemEventArgs e)
    {
        var item = e.Item;
        var subItem = e.SubItem;
        if (item is null || subItem is null)
        {
            return;
        }

        var selected = item.Selected || (e.ItemState & ListViewItemStates.Selected) != 0;
        var hovered = _hoverHighlight && e.ItemIndex == _hoveredItemIndex;
        var rowBounds = GetNativeBounds(item, ItemBoundsPortion.Entire, e.Bounds);
        if (e.ColumnIndex == 0)
        {
            var basePalette = ResolvePalette(
                item,
                item.SubItems[0],
                ResolveState(FullRowSelect && selected, hovered),
                e.ItemIndex);
            Fill(e.Graphics, rowBounds, basePalette.BackColor);
        }

        var cellSelected = selected && (FullRowSelect || e.ColumnIndex == 0);
        var palette = ResolvePalette(item, subItem, ResolveState(cellSelected, hovered), e.ItemIndex);
        Fill(e.Graphics, e.Bounds, palette.BackColor);
        var textBounds = Rectangle.Intersect(
            e.Bounds,
            GetNativeBounds(item, ItemBoundsPortion.Label, e.Bounds));
        if (e.ColumnIndex == 0)
        {
            DrawNativeStateImage(e.Graphics, item, palette.ForeColor);
            var iconBounds = Rectangle.Intersect(
                e.Bounds,
                GetNativeBounds(item, ItemBoundsPortion.Icon, Rectangle.Empty));
            var image = ResolveItemImage(item, View.Details);
            if (image is not null && !iconBounds.IsEmpty) DrawImage(e.Graphics, image, iconBounds);
        }

        DrawText(
            e.Graphics,
            subItem.Text,
            ResolveFont(item, subItem),
            textBounds,
            palette.ForeColor,
            e.Header?.TextAlign ?? HorizontalAlignment.Left,
            false);
        if (cellSelected && Focused && ShowFocusCues && item.Focused)
        {
            DrawFocus(e.Graphics, BootstrapListViewLayoutLogic.GetFocusBounds(View.Details, rowBounds, e.Bounds, FullRowSelect));
        }
    }

    private void DrawNonDetailsItem(DrawListViewItemEventArgs e)
    {
        var item = e.Item;
        var selected = item.Selected || (e.State & ListViewItemStates.Selected) != 0;
        var palette = ResolvePalette(
            item,
            item.SubItems[0],
            ResolveState(selected, _hoverHighlight && e.ItemIndex == _hoveredItemIndex),
            e.ItemIndex);
        var entireBounds = GetNativeBounds(item, ItemBoundsPortion.Entire, e.Bounds);
        Fill(e.Graphics, entireBounds, palette.BackColor);
        var iconBounds = GetNativeBounds(item, ItemBoundsPortion.Icon, Rectangle.Empty);
        var labelBounds = GetNativeBounds(item, ItemBoundsPortion.Label, e.Bounds);
        var image = ResolveItemImage(item, View);
        if (image is not null && !iconBounds.IsEmpty) DrawImage(e.Graphics, image, iconBounds);
        if (View != View.Tile) DrawNativeStateImage(e.Graphics, item, palette.ForeColor);

        if (View == View.List)
        {
            DrawText(e.Graphics, item.Text, ResolveFont(item, item.SubItems[0]), labelBounds, palette.ForeColor, HorizontalAlignment.Left, false);
        }
        else if (View == View.Tile)
        {
            DrawTileText(e.Graphics, item, entireBounds, iconBounds, palette);
        }
        else
        {
            DrawText(
                e.Graphics,
                item.Text,
                ResolveFont(item, item.SubItems[0]),
                labelBounds,
                palette.ForeColor,
                HorizontalAlignment.Center,
                View == View.LargeIcon);
        }

        if (selected && Focused && ShowFocusCues && item.Focused)
        {
            DrawFocus(e.Graphics, BootstrapListViewLayoutLogic.GetFocusBounds(View, entireBounds, labelBounds, FullRowSelect));
        }
    }

    private void DrawTileText(Graphics graphics, ListViewItem item, Rectangle itemBounds, Rectangle imageBounds, BootstrapListViewItemPalette palette)
    {
        var bounds = BootstrapListViewLayoutLogic.GetTileTextBounds(
            itemBounds,
            imageBounds,
            DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingSM, GetCurrentDpi()),
            RightToLeft == RightToLeft.Yes);
        if (bounds.IsEmpty) return;
        var lineCount = CountTileLines(item);
        var lineHeight = Math.Max(1, bounds.Height / lineCount);
        DrawTileLine(graphics, item, item.SubItems[0], bounds, lineHeight, 0, palette.ForeColor);
        var lineIndex = 1;
        for (var displayIndex = 0; displayIndex < Columns.Count; displayIndex++)
        {
            for (var columnIndex = 1; columnIndex < Columns.Count && columnIndex < item.SubItems.Count; columnIndex++)
            {
                var column = Columns[columnIndex];
                if (column.DisplayIndex != displayIndex || column.Width == 0) continue;
                DrawTileLine(
                    graphics,
                    item,
                    item.SubItems[columnIndex],
                    bounds,
                    lineHeight,
                    lineIndex,
                    BootstrapThemeManager.CurrentTheme.Colors.MutedText);
                lineIndex++;
            }
        }
    }

    private int CountTileLines(ListViewItem item)
    {
        var count = 1;
        for (var columnIndex = 1; columnIndex < Columns.Count && columnIndex < item.SubItems.Count; columnIndex++)
        {
            if (Columns[columnIndex].Width != 0) count++;
        }

        return count;
    }

    private void DrawTileLine(
        Graphics graphics,
        ListViewItem item,
        ListViewItem.ListViewSubItem subItem,
        Rectangle bounds,
        int lineHeight,
        int lineIndex,
        Color defaultForeground)
    {
        var foreground = defaultForeground;
        if (!item.UseItemStyleForSubItems && BootstrapListViewRenderLogic.HasEffectiveColorOverride(subItem.ForeColor, ForeColor))
        {
            foreground = subItem.ForeColor;
        }

        var line = new Rectangle(bounds.X, bounds.Y + (lineIndex * lineHeight), bounds.Width, lineHeight);
        DrawText(graphics, subItem.Text, ResolveFont(item, subItem), line, foreground, HorizontalAlignment.Left, false);
    }

    private void DrawNativeStateImage(Graphics graphics, ListViewItem item, Color foreground)
    {
        var stateImage = ResolveStateImage(item);
        if (stateImage is null && !CheckBoxes) return;
        var bounds = GetNativeStateImageBounds(item);
        if (bounds.IsEmpty) return;
        if (stateImage is not null) DrawImage(graphics, stateImage, bounds); else DrawCheckbox(graphics, bounds, item.Checked, foreground);
    }

    private Rectangle GetNativeStateImageBounds(ListViewItem item)
    {
        if (!IsHandleCreated || item.ListView != this) return Rectangle.Empty;
        var itemBounds = Rectangle.Intersect(ClientRectangle, GetNativeBounds(item, ItemBoundsPortion.Entire, Rectangle.Empty));
        var iconBounds = GetNativeBounds(item, ItemBoundsPortion.Icon, itemBounds);
        if (itemBounds.IsEmpty || iconBounds.IsEmpty) return Rectangle.Empty;

        var y = Math.Max(itemBounds.Top, Math.Min(itemBounds.Bottom - 1, iconBounds.Top + (iconBounds.Height / 2)));
        var firstX = -1;
        var lastX = -1;
        for (var x = itemBounds.Left; x < itemBounds.Right; x++)
        {
            var hit = HitTest(x, y);
            if (!ReferenceEquals(hit.Item, item) || (hit.Location & ListViewHitTestLocations.StateImage) == 0) continue;
            if (firstX < 0) firstX = x;
            lastX = x;
        }

        if (firstX < 0) return Rectangle.Empty;
        var probeX = firstX + ((lastX - firstX) / 2);
        var firstY = -1;
        var lastY = -1;
        for (var probeY = itemBounds.Top; probeY < itemBounds.Bottom; probeY++)
        {
            var hit = HitTest(probeX, probeY);
            if (!ReferenceEquals(hit.Item, item) || (hit.Location & ListViewHitTestLocations.StateImage) == 0) continue;
            if (firstY < 0) firstY = probeY;
            lastY = probeY;
        }

        return firstY < 0
            ? Rectangle.Empty
            : Rectangle.FromLTRB(firstX, firstY, lastX + 1, lastY + 1);
    }

    private BootstrapListViewItemVisualState ResolveState(bool selected, bool hovered)
    {
        return BootstrapListViewRenderLogic.ResolveState(Enabled, selected, Focused, HideSelection, hovered);
    }

    private BootstrapListViewItemPalette ResolvePalette(ListViewItem item, ListViewItem.ListViewSubItem subItem, BootstrapListViewItemVisualState state, int itemIndex)
    {
        var useItemStyle = item.UseItemStyleForSubItems || ReferenceEquals(subItem, item.SubItems[0]);
        var background = useItemStyle ? item.BackColor : subItem.BackColor;
        var foreground = useItemStyle ? item.ForeColor : subItem.ForeColor;
        return BootstrapListViewRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme,
            _variant,
            state,
            BootstrapListViewRenderLogic.ShouldUseStripe(View, _striped, itemIndex),
            BootstrapListViewRenderLogic.HasEffectiveColorOverride(background, BackColor),
            background,
            BootstrapListViewRenderLogic.HasEffectiveColorOverride(foreground, ForeColor),
            foreground);
    }

    private Font ResolveFont(ListViewItem item, ListViewItem.ListViewSubItem subItem)
    {
        return item.UseItemStyleForSubItems || ReferenceEquals(subItem, item.SubItems[0])
            ? item.Font ?? Font
            : subItem.Font ?? item.Font ?? Font;
    }

    private Image? ResolveItemImage(ListViewItem item, View view)
    {
        var imageList = view == View.LargeIcon || view == View.Tile ? LargeImageList : SmallImageList;
        return ResolveImage(imageList, item.ImageKey, item.ImageIndex);
    }

    private Image? ResolveStateImage(ListViewItem item) => ResolveImage(StateImageList, string.Empty, item.StateImageIndex);

    private static Image? ResolveImage(ImageList? imageList, string key, int index)
    {
        if (imageList is null || imageList.Images.Count == 0) return null;
        if (!string.IsNullOrEmpty(key))
        {
            var keyIndex = imageList.Images.IndexOfKey(key);
            return keyIndex >= 0 ? imageList.Images[keyIndex] : null;
        }

        return index >= 0 && index < imageList.Images.Count ? imageList.Images[index] : null;
    }

    private static Rectangle GetNativeBounds(ListViewItem item, ItemBoundsPortion portion, Rectangle fallback)
    {
        try
        {
            var bounds = item.GetBounds(portion);
            return bounds.Width > 0 && bounds.Height > 0 ? bounds : fallback;
        }
        catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
        {
            return fallback;
        }
    }

    private static void Fill(Graphics graphics, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bounds);
    }

    private void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color color, HorizontalAlignment alignment, bool wordWrap)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || string.IsNullOrEmpty(text)) return;
        var state = graphics.Save();
        try
        {
            graphics.SetClip(bounds, CombineMode.Intersect);
            TextRenderer.DrawText(graphics, text, font, bounds, color, BootstrapListViewLayoutLogic.GetTextFlags(alignment, RightToLeft == RightToLeft.Yes, wordWrap));
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static void DrawImage(Graphics graphics, Image image, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var width = Math.Min(bounds.Width, image.Width);
        var height = Math.Min(bounds.Height, image.Height);
        graphics.DrawImage(image, new Rectangle(bounds.X + ((bounds.Width - width) / 2), bounds.Y + ((bounds.Height - height) / 2), width, height));
    }

    private static void DrawCheckbox(Graphics graphics, Rectangle bounds, bool isChecked, Color color)
    {
        var size = Math.Min(bounds.Width, bounds.Height);
        bounds = new Rectangle(
            bounds.Left + ((bounds.Width - size) / 2),
            bounds.Top + ((bounds.Height - size) / 2),
            size,
            size);
        if (bounds.Width <= 2 || bounds.Height <= 2) return;
        using var pen = new Pen(color, 1f);
        graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        if (!isChecked) return;
        using var checkPen = new Pen(color, 2f);
        var start = new Point(bounds.Left + (bounds.Width / 5), bounds.Top + (bounds.Height / 2));
        var middle = new Point(bounds.Left + (bounds.Width * 2 / 5), bounds.Bottom - (bounds.Height / 5));
        var end = new Point(bounds.Right - (bounds.Width / 6), bounds.Top + (bounds.Height / 4));
        graphics.DrawLine(checkPen, start, middle);
        graphics.DrawLine(checkPen, middle, end);
    }

    private void DrawFocus(Graphics graphics, Rectangle bounds)
    {
        var focusBounds = BootstrapListViewLayoutLogic.Deflate(bounds, 1, 1);
        if (!focusBounds.IsEmpty) ControlPaint.DrawFocusRectangle(graphics, focusBounds, BootstrapThemeManager.CurrentTheme.Colors.Focus, Color.Transparent);
    }

    private void UpdateHoveredIndex(int index)
    {
        if (_hoveredItemIndex == index) return;
        var previous = _hoveredItemIndex;
        _hoveredItemIndex = index;
        if (!InvalidateItem(previous) || !InvalidateItem(index)) Invalidate();
    }

    private void ClearHover()
    {
        if (_hoveredItemIndex < 0 || IsDisposed || Disposing)
        {
            _hoveredItemIndex = -1;
            return;
        }

        var previous = _hoveredItemIndex;
        _hoveredItemIndex = -1;
        if (!InvalidateItem(previous)) Invalidate();
    }

    private bool InvalidateItem(int index)
    {
        if (index < 0 || !IsHandleCreated) return index < 0;
        try
        {
            ListViewItem? item = null;
            if (!VirtualMode && index < Items.Count) item = Items[index];
            var bounds = item?.GetBounds(ItemBoundsPortion.Entire) ?? Rectangle.Empty;
            if (bounds.IsEmpty) return false;
            Invalidate(bounds);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException || exception is InvalidOperationException)
        {
            return false;
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!_initialized || IsDisposed || Disposing) return;
        if (_useThemeFont) ApplyThemeFont();
        ApplyThemePresentation();
        Invalidate();
    }

    private void ApplyThemePresentation()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        if (_themeFont is not null && string.Equals(_themeFont.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) && Math.Abs(_themeFont.SizeInPoints - token.SizeInPoints) < 0.01f && _themeFont.Style == token.Style) return;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = next;
        _settingThemeFont = true;
        try
        {
            Font = next;
        }
        catch
        {
            _themeFont = previous;
            next.Dispose();
            throw;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private int GetCurrentDpi() => DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
}
