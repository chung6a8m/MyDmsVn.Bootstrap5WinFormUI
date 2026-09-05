using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
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
    private const int MaxTileColumns = 20;
    private const int LvmGetItemW = 0x104B;
    private const uint LvifImage = 0x0002;
    private static readonly object? DrawColumnHeaderEventKey = ResolveEventKey("s_drawColumnHeaderEvent", "EVENT_DRAWCOLUMNHEADER");
    private static readonly object? DrawItemEventKey = ResolveEventKey("s_drawItemEvent", "EVENT_DRAWITEM");
    private static readonly object? DrawSubItemEventKey = ResolveEventKey("s_drawSubItemEvent", "EVENT_DRAWSUBITEM");

    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _striped;
    private bool _hoverHighlight = true;
    private int _hoveredItemIndex = -1;
    private Rectangle _hoveredItemBounds = Rectangle.Empty;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _initialized;
    private Font? _themeFont;
    private Bitmap? _ownerDrawBuffer;
    private Graphics? _ownerDrawBufferGraphics;

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
        _hoveredItemBounds = Rectangle.Empty;
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
        _hoveredItemBounds = Rectangle.Empty;
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_hoverHighlight || IsDisposed || Disposing || !IsHandleCreated) return;
        var hit = HitTest(e.X, e.Y);
        var index = hit.Item?.Index ?? -1;
        UpdateHoveredIndex(index, GetItemBounds(index));
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
        var paintBounds = GetColumnHeaderPaintBounds(e);
        if (!HasOwnerDrawSubscribers(DrawColumnHeaderEventKey))
        {
            PaintColumnHeader(e, paintBounds);
            base.OnDrawColumnHeader(e);
            return;
        }

        var bufferGraphics = PrepareOwnerDrawBuffer(e.Graphics, paintBounds);
        var bufferedArgs = new DrawListViewColumnHeaderEventArgs(
            bufferGraphics,
            e.Bounds,
            e.ColumnIndex,
            e.Header,
            e.State,
            e.ForeColor,
            e.BackColor,
            e.Font);

        PaintColumnHeader(bufferedArgs, paintBounds);

        base.OnDrawColumnHeader(bufferedArgs);
        e.DrawDefault = bufferedArgs.DrawDefault;
        if (!e.DrawDefault) RenderOwnerDrawBuffer(e.Graphics, paintBounds);
    }

    /// <inheritdoc />
    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        if (View == View.Details)
        {
            base.OnDrawItem(e);
            return;
        }

        if (!HasOwnerDrawSubscribers(DrawItemEventKey))
        {
            DrawNonDetailsItem(e);
            base.OnDrawItem(e);
            return;
        }

        var bufferGraphics = PrepareOwnerDrawBuffer(e.Graphics, e.Bounds);
        var bufferedArgs = new DrawListViewItemEventArgs(
            bufferGraphics,
            e.Item,
            e.Bounds,
            e.ItemIndex,
            e.State);
        DrawNonDetailsItem(bufferedArgs);
        base.OnDrawItem(bufferedArgs);
        e.DrawDefault = bufferedArgs.DrawDefault;
        if (!e.DrawDefault) RenderOwnerDrawBuffer(e.Graphics, e.Bounds);
    }

    /// <inheritdoc />
    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        var paintBounds = e.ColumnIndex == 0 && e.Item is not null
            ? Rectangle.Union(e.Bounds, GetNativeBounds(e.Item, ItemBoundsPortion.Entire, e.Bounds))
            : e.Bounds;
        if (!HasOwnerDrawSubscribers(DrawSubItemEventKey))
        {
            if (View == View.Details) DrawDetailsSubItem(e);
            base.OnDrawSubItem(e);
            return;
        }

        var bufferGraphics = PrepareOwnerDrawBuffer(e.Graphics, paintBounds);
        var bufferedArgs = new DrawListViewSubItemEventArgs(
            bufferGraphics,
            e.Bounds,
            e.Item,
            e.SubItem,
            e.ItemIndex,
            e.ColumnIndex,
            e.Header,
            e.ItemState);
        if (View == View.Details) DrawDetailsSubItem(bufferedArgs);
        base.OnDrawSubItem(bufferedArgs);
        e.DrawDefault = bufferedArgs.DrawDefault;
        if (!e.DrawDefault) RenderOwnerDrawBuffer(e.Graphics, paintBounds);
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
            _hoveredItemBounds = Rectangle.Empty;
            DisposeOwnerDrawBuffer();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void PaintColumnHeader(DrawListViewColumnHeaderEventArgs e, Rectangle paintBounds)
    {
        if (HeaderStyle == ColumnHeaderStyle.None) return;
        var theme = BootstrapThemeManager.CurrentTheme;
        using (var background = new SolidBrush(theme.Colors.SurfaceSecondary))
        using (var separator = new Pen(theme.Colors.Border, Math.Max(1, DpiScaler.Scale(1, GetCurrentDpi()))))
        {
            e.Graphics.FillRectangle(background, paintBounds);
            e.Graphics.DrawLine(separator, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            e.Graphics.DrawLine(separator, paintBounds.Left, paintBounds.Bottom - 1, paintBounds.Right, paintBounds.Bottom - 1);
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
            var imageBounds = IsLayoutMirrored
                ? new Rectangle(textBounds.Right - imageWidth, textBounds.Top, imageWidth, textBounds.Height)
                : new Rectangle(textBounds.Left, textBounds.Top, imageWidth, textBounds.Height);
            DrawImage(e.Graphics, headerImage, imageBounds);
            var gap = DpiScaler.Scale(theme.Metrics.SpacingXS, GetCurrentDpi());
            textBounds = IsLayoutMirrored
                ? Rectangle.FromLTRB(textBounds.Left, textBounds.Top, Math.Max(textBounds.Left, imageBounds.Left - gap), textBounds.Bottom)
                : Rectangle.FromLTRB(Math.Min(textBounds.Right, imageBounds.Right + gap), textBounds.Top, textBounds.Right, textBounds.Bottom);
        }

        if (textBounds.IsEmpty) return;
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

    private Rectangle GetColumnHeaderPaintBounds(DrawListViewColumnHeaderEventArgs e)
    {
        if (HeaderStyle == ColumnHeaderStyle.None || e.Header is null || !IsLastVisibleColumn(e.ColumnIndex)) return e.Bounds;
        Rectangle filler;
        if (IsLayoutMirrored)
        {
            filler = Rectangle.FromLTRB(0, e.Bounds.Top, Math.Max(0, e.Bounds.Left), e.Bounds.Bottom);
        }
        else
        {
            filler = Rectangle.FromLTRB(
                Math.Min(ClientSize.Width, e.Bounds.Right),
                e.Bounds.Top,
                ClientSize.Width,
                e.Bounds.Bottom);
        }

        return filler.IsEmpty ? e.Bounds : Rectangle.Union(e.Bounds, filler);
    }

    private bool IsLastVisibleColumn(int columnIndex)
    {
        var lastColumnIndex = -1;
        var greatestDisplayIndex = -1;
        for (var index = 0; index < Columns.Count; index++)
        {
            var column = Columns[index];
            if (column.Width <= 0 || column.DisplayIndex <= greatestDisplayIndex) continue;
            greatestDisplayIndex = column.DisplayIndex;
            lastColumnIndex = index;
        }

        return columnIndex == lastColumnIndex;
    }

    private void DrawDetailsSubItem(DrawListViewSubItemEventArgs e)
    {
        var item = e.Item;
        var subItem = e.SubItem;
        if (item is null || subItem is null)
        {
            return;
        }

        var selected = IsActuallySelected(item, e.ItemIndex);
        var hotTracked = HotTracking && (e.ItemState & ListViewItemStates.Hot) != 0;
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
        var textBounds = e.ColumnIndex == 0
            ? Rectangle.Intersect(e.Bounds, GetNativeBounds(item, ItemBoundsPortion.Label, e.Bounds))
            : BootstrapListViewLayoutLogic.Deflate(
                e.Bounds,
                DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingXS, GetCurrentDpi()),
                0);
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
            false,
            hotTracked);
        if (e.ColumnIndex == 0 && Focused && ShowFocusCues && item.Focused)
        {
            DrawFocus(e.Graphics, BootstrapListViewLayoutLogic.GetFocusBounds(View.Details, rowBounds, textBounds, FullRowSelect));
        }
    }

    private void DrawNonDetailsItem(DrawListViewItemEventArgs e)
    {
        var item = e.Item;
        var selected = IsActuallySelected(item, e.ItemIndex);
        var hotTracked = HotTracking && (e.State & ListViewItemStates.Hot) != 0;
        var palette = ResolvePalette(
            item,
            item.SubItems[0],
            ResolveState(selected, _hoverHighlight && e.ItemIndex == _hoveredItemIndex),
            e.ItemIndex);
        var entireBounds = View == View.Tile
            ? e.Bounds
            : GetNativeBounds(item, ItemBoundsPortion.Entire, e.Bounds);
        Fill(e.Graphics, entireBounds, palette.BackColor);
        var iconBounds = GetNativeBounds(item, ItemBoundsPortion.Icon, Rectangle.Empty);
        var labelBounds = GetNativeBounds(item, ItemBoundsPortion.Label, e.Bounds);
        var image = ResolveItemImage(item, View);
        if (image is not null && !iconBounds.IsEmpty) DrawImage(e.Graphics, image, iconBounds);
        DrawNativeStateImage(e.Graphics, item, palette.ForeColor, View != View.Tile);

        if (View == View.List)
        {
            DrawText(e.Graphics, item.Text, ResolveFont(item, item.SubItems[0]), labelBounds, palette.ForeColor, HorizontalAlignment.Left, false, hotTracked);
        }
        else if (View == View.Tile)
        {
            DrawTileText(e.Graphics, item, entireBounds, iconBounds, palette, hotTracked);
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
                BootstrapListViewLayoutLogic.ShouldWrapItemText(View, LabelWrap),
                hotTracked);
        }

        if (Focused && ShowFocusCues && item.Focused)
        {
            DrawFocus(e.Graphics, BootstrapListViewLayoutLogic.GetFocusBounds(View, entireBounds, labelBounds, FullRowSelect));
        }
    }

    private void DrawTileText(
        Graphics graphics,
        ListViewItem item,
        Rectangle itemBounds,
        Rectangle imageBounds,
        BootstrapListViewItemPalette palette,
        bool hotTracked)
    {
        var bounds = BootstrapListViewLayoutLogic.GetTileTextBounds(
            itemBounds,
            imageBounds,
            DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingSM, GetCurrentDpi()),
            IsLayoutMirrored);
        if (bounds.IsEmpty) return;
        var lineCount = CountTileLines();
        var lineHeight = Math.Max(1, bounds.Height / lineCount);
        DrawTileLine(graphics, item, item.SubItems[0], bounds, lineHeight, 0, palette.ForeColor, hotTracked);
        var lineIndex = 1;
        var projectedColumnCount = Math.Min(MaxTileColumns, Columns.Count);
        for (var columnIndex = 0; columnIndex < projectedColumnCount; columnIndex++)
        {
            var subItemIndex = columnIndex + 1;
            if (subItemIndex >= item.SubItems.Count) continue;
            DrawTileLine(
                graphics,
                item,
                item.SubItems[subItemIndex],
                bounds,
                lineHeight,
                lineIndex,
                BootstrapThemeManager.CurrentTheme.Colors.MutedText,
                hotTracked);
            lineIndex++;
        }
    }

    private int CountTileLines() => Math.Max(1, Columns.Count + 1);

    private void DrawTileLine(
        Graphics graphics,
        ListViewItem item,
        ListViewItem.ListViewSubItem subItem,
        Rectangle bounds,
        int lineHeight,
        int lineIndex,
        Color defaultForeground,
        bool hotTracked)
    {
        var foreground = defaultForeground;
        if (!item.UseItemStyleForSubItems && BootstrapListViewRenderLogic.HasEffectiveColorOverride(subItem.ForeColor, ForeColor))
        {
            foreground = subItem.ForeColor;
        }

        var line = new Rectangle(bounds.X, bounds.Y + (lineIndex * lineHeight), bounds.Width, lineHeight);
        DrawText(graphics, subItem.Text, ResolveFont(item, subItem), line, foreground, HorizontalAlignment.Left, false, hotTracked);
    }

    private void DrawNativeStateImage(Graphics graphics, ListViewItem item, Color foreground, bool allowCheckboxFallback = true)
    {
        var stateImage = ResolveStateImage(item);
        if (stateImage is null && (!CheckBoxes || !allowCheckboxFallback)) return;
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
        var stateWidth = StateImageList?.ImageSize.Width ?? DpiScaler.Scale(16, GetCurrentDpi());
        if (!TryFindNativeStateHit(item, itemBounds, iconBounds, stateWidth, out var hitPoint)) return Rectangle.Empty;
        var left = FindStateHitStart(item, itemBounds.Left, hitPoint.X, hitPoint.Y, true);
        var right = FindStateHitEnd(item, hitPoint.X, itemBounds.Right, hitPoint.Y, true);
        var top = FindStateHitStart(item, itemBounds.Top, hitPoint.Y, hitPoint.X, false);
        var bottom = FindStateHitEnd(item, hitPoint.Y, itemBounds.Bottom, hitPoint.X, false);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private bool TryFindNativeStateHit(
        ListViewItem item,
        Rectangle itemBounds,
        Rectangle iconBounds,
        int stateWidth,
        out Point hitPoint)
    {
        var direction = IsLayoutMirrored ? 1 : -1;
        var iconLeadingEdge = IsLayoutMirrored ? iconBounds.Right - 1 : iconBounds.Left;
        var likelyX = iconLeadingEdge + (direction * Math.Max(1, stateWidth / 2));
        var alternateX = iconLeadingEdge;
        var centerY = iconBounds.Top + (iconBounds.Height / 2);
        var itemCenterY = itemBounds.Top + (itemBounds.Height / 2);
        var topY = iconBounds.Top;
        var bottomY = iconBounds.Bottom - 1;

        return TryNativeStateHit(item, itemBounds, likelyX, centerY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, likelyX, itemCenterY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, likelyX, topY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, likelyX, bottomY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, alternateX, centerY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, alternateX, itemCenterY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, alternateX, topY, out hitPoint) ||
               TryNativeStateHit(item, itemBounds, alternateX, bottomY, out hitPoint);
    }

    private bool TryNativeStateHit(ListViewItem item, Rectangle itemBounds, int x, int y, out Point hitPoint)
    {
        hitPoint = Point.Empty;
        if (!itemBounds.Contains(x, y) || !IsNativeStateHit(item, x, y)) return false;
        hitPoint = new Point(x, y);
        return true;
    }

    private int FindStateHitStart(ListViewItem item, int boundary, int inside, int fixedCoordinate, bool horizontal)
    {
        if (IsNativeStateHit(item, horizontal ? boundary : fixedCoordinate, horizontal ? fixedCoordinate : boundary)) return boundary;
        var outside = boundary;
        while (outside + 1 < inside)
        {
            var middle = outside + ((inside - outside) / 2);
            if (IsNativeStateHit(item, horizontal ? middle : fixedCoordinate, horizontal ? fixedCoordinate : middle)) inside = middle;
            else outside = middle;
        }

        return inside;
    }

    private int FindStateHitEnd(ListViewItem item, int inside, int boundary, int fixedCoordinate, bool horizontal)
    {
        var outside = boundary;
        while (inside + 1 < outside)
        {
            var middle = inside + ((outside - inside) / 2);
            if (IsNativeStateHit(item, horizontal ? middle : fixedCoordinate, horizontal ? fixedCoordinate : middle)) inside = middle;
            else outside = middle;
        }

        return outside;
    }

    private bool IsNativeStateHit(ListViewItem item, int x, int y)
    {
        var hit = HitTest(x, y);
        return ReferenceEquals(hit.Item, item) && (hit.Location & ListViewHitTestLocations.StateImage) != 0;
    }

    private BootstrapListViewItemVisualState ResolveState(bool selected, bool hovered)
    {
        return BootstrapListViewRenderLogic.ResolveState(Enabled, selected, Focused, HideSelection, hovered);
    }

    private bool IsActuallySelected(ListViewItem item, int itemIndex)
    {
        if (!VirtualMode) return item.Selected;
        return itemIndex >= 0 && SelectedIndices.Contains(itemIndex);
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
        if (imageList is null || imageList.Images.Count == 0) return null;
        var index = GetNativeItemImageIndex(item, imageList);
        return index >= 0 && index < imageList.Images.Count ? imageList.Images[index] : null;
    }

    private int GetNativeItemImageIndex(ListViewItem item, ImageList imageList)
    {
        if (IsHandleCreated && item.ListView == this && item.Index >= 0)
        {
            var nativeItem = new NativeListViewItem
            {
                Mask = LvifImage,
                ItemIndex = item.Index,
                ImageIndex = -1
            };
            if (SendMessage(Handle, LvmGetItemW, IntPtr.Zero, ref nativeItem) != IntPtr.Zero)
            {
                return nativeItem.ImageIndex;
            }
        }

        if (!string.IsNullOrEmpty(item.ImageKey)) return imageList.Images.IndexOfKey(item.ImageKey);
        return item.ImageIndex;
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

    private Graphics PrepareOwnerDrawBuffer(Graphics targetGraphics, Rectangle targetBounds)
    {
        var requiredSize = new Size(
            Math.Max(1, targetBounds.Width),
            Math.Max(1, targetBounds.Height));
        if (_ownerDrawBuffer is null ||
            _ownerDrawBuffer.Size != requiredSize ||
            Math.Abs(_ownerDrawBuffer.HorizontalResolution - targetGraphics.DpiX) > 0.01f ||
            Math.Abs(_ownerDrawBuffer.VerticalResolution - targetGraphics.DpiY) > 0.01f)
        {
            DisposeOwnerDrawBuffer();
            _ownerDrawBuffer = new Bitmap(requiredSize.Width, requiredSize.Height);
            _ownerDrawBuffer.SetResolution(targetGraphics.DpiX, targetGraphics.DpiY);
            _ownerDrawBufferGraphics = Graphics.FromImage(_ownerDrawBuffer);
        }

        var graphics = _ownerDrawBufferGraphics!;
        graphics.ResetTransform();
        graphics.ResetClip();
        graphics.Clear(BackColor);
        graphics.TranslateTransform(-targetBounds.X, -targetBounds.Y);
        graphics.SetClip(targetBounds);
        return graphics;
    }

    private void RenderOwnerDrawBuffer(Graphics graphics, Rectangle targetBounds)
    {
        if (_ownerDrawBuffer is null) return;
        if (targetBounds.Width <= 0 || targetBounds.Height <= 0) return;
        graphics.DrawImageUnscaled(_ownerDrawBuffer, targetBounds.Location);
    }

    private void DisposeOwnerDrawBuffer()
    {
        _ownerDrawBufferGraphics?.Dispose();
        _ownerDrawBufferGraphics = null;
        _ownerDrawBuffer?.Dispose();
        _ownerDrawBuffer = null;
    }

    // WinForms does not expose subscriber presence, but knowing it lets the normal TextRenderer path stay on the
    // final HDC while retaining DrawDefault=true rollback semantics for the inherited owner-draw events.
    private bool HasOwnerDrawSubscribers(object? eventKey) => eventKey is null || Events[eventKey] is not null;

    private static object? ResolveEventKey(string currentName, string frameworkName)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        return typeof(ListView).GetField(currentName, flags)?.GetValue(null) ??
               typeof(ListView).GetField(frameworkName, flags)?.GetValue(null);
    }

    private void DrawText(
        Graphics graphics,
        string text,
        Font font,
        Rectangle bounds,
        Color color,
        HorizontalAlignment alignment,
        bool wordWrap,
        bool hotTracked = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || string.IsNullOrEmpty(text)) return;
        Font? hotFont = null;
        var state = graphics.Save();
        try
        {
            if (hotTracked && (font.Style & FontStyle.Underline) == 0)
            {
                hotFont = new Font(font, font.Style | FontStyle.Underline);
            }

            graphics.SetClip(bounds, CombineMode.Intersect);
            TextRenderer.DrawText(graphics, text, hotFont ?? font, bounds, color, BootstrapListViewLayoutLogic.GetTextFlags(alignment, RightToLeft == RightToLeft.Yes, wordWrap));
        }
        finally
        {
            hotFont?.Dispose();
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

    private void UpdateHoveredIndex(int index, Rectangle bounds)
    {
        if (_hoveredItemIndex == index)
        {
            _hoveredItemBounds = bounds;
            return;
        }

        var previous = _hoveredItemIndex;
        var previousBounds = _hoveredItemBounds;
        _hoveredItemIndex = index;
        _hoveredItemBounds = bounds;
        if (!InvalidateItem(previous, previousBounds) || !InvalidateItem(index, bounds)) Invalidate();
    }

    private void ClearHover()
    {
        if (_hoveredItemIndex < 0 || IsDisposed || Disposing)
        {
            _hoveredItemIndex = -1;
            _hoveredItemBounds = Rectangle.Empty;
            return;
        }

        var previous = _hoveredItemIndex;
        var previousBounds = _hoveredItemBounds;
        _hoveredItemIndex = -1;
        _hoveredItemBounds = Rectangle.Empty;
        if (!InvalidateItem(previous, previousBounds)) Invalidate();
    }

    private bool InvalidateItem(int index, Rectangle knownBounds)
    {
        if (index < 0 || !IsHandleCreated) return index < 0;
        var bounds = knownBounds.IsEmpty ? GetItemBounds(index) : knownBounds;
        if (bounds.IsEmpty) return false;
        Invalidate(bounds);
        return true;
    }

    private Rectangle GetItemBounds(int index)
    {
        if (index < 0 || !IsHandleCreated) return Rectangle.Empty;
        try
        {
            return GetItemRect(index, ItemBoundsPortion.Entire);
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException || exception is InvalidOperationException)
        {
            return Rectangle.Empty;
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

    private bool IsLayoutMirrored => RightToLeft == RightToLeft.Yes && RightToLeftLayout;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativeListViewItem item);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeListViewItem
    {
        internal uint Mask;
        internal int ItemIndex;
        internal int SubItemIndex;
        internal uint State;
        internal uint StateMask;
        internal IntPtr Text;
        internal int TextLength;
        internal int ImageIndex;
        internal IntPtr Parameter;
        internal int Indent;
        internal int GroupId;
        internal uint ColumnCount;
        internal IntPtr Columns;
        internal IntPtr ColumnFormats;
        internal int Group;
    }
}
