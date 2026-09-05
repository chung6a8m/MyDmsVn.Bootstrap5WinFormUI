using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
    private const int CddsPrePaint = 0x00000001;
    private const int CddsPostPaint = 0x00000002;
    private const int CddsItemPrePaint = 0x00010001;
    private const int CdrfSkipDefault = 0x00000004;
    private const int CdrfNotifyPostPaint = 0x00000010;
    private const int HdmGetItemRect = 0x1207;
    private const int LvcdItemGroup = 0x00000001;
    private const int LvmArrange = 0x1016;
    private const int LvmGetHeader = 0x101F;
    private const int LvmGetGroupInfo = 0x1095;
    private const int LvmGetItemW = 0x104B;
    private const int LvmSetTileViewInfo = 0x10A2;
    private const int NmCustomDraw = -12;
    private const int SourceCopy = 0x00CC0020;
    private const int WmNotify = 0x004E;
    private const int WmReflectNotify = 0x204E;
    private const uint LvifImage = 0x0002;
    private const uint LvgfHeader = 0x00000001;
    private const uint LvgfAlign = 0x00000008;
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
    private IntPtr _ownerDrawBufferHdc;
    private IntPtr _activeNativePaintHdc;
    private readonly BootstrapListViewNativeWindow _nativeWindow;

    /// <summary>Initializes a new instance of the <see cref="BootstrapListView"/> class.</summary>
    public BootstrapListView()
    {
        _nativeWindow = new BootstrapListViewNativeWindow(this);
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
        _nativeWindow.AssignHandle(Handle);
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
        if (_nativeWindow.Handle != IntPtr.Zero) _nativeWindow.ReleaseHandle();
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

    private void ProcessNativeMessage(ref Message m, NativeMessageDispatcher dispatch)
    {
        var headerDraw = default(NativeCustomDraw);
        var headerCustomDraw = m.Msg == WmNotify && IsHeaderCustomDraw(m.LParam, out headerDraw);
        var listDraw = default(NativeListViewCustomDraw);
        var listCustomDraw = m.Msg == WmReflectNotify && IsListCustomDraw(m.LParam, out listDraw);
        var groupCustomDraw = listCustomDraw && IsGroupCustomDraw(listDraw);
        var previousPaintHdc = _activeNativePaintHdc;
        if (headerCustomDraw) _activeNativePaintHdc = headerDraw.DeviceContext;
        else if (listCustomDraw) _activeNativePaintHdc = listDraw.CustomDraw.DeviceContext;

        try
        {
            dispatch(ref m);
        }
        finally
        {
            _activeNativePaintHdc = previousPaintHdc;
        }

        if (headerCustomDraw)
        {
            if (headerDraw.DrawStage == CddsPrePaint)
            {
                m.Result = OrCustomDrawResult(m.Result, CdrfNotifyPostPaint);
            }
            else if (headerDraw.DrawStage == CddsPostPaint)
            {
                PaintHeaderFiller(headerDraw.Header.WindowFrom, headerDraw.DeviceContext);
            }
        }

        if (groupCustomDraw)
        {
            ApplyGroupHeaderTheme(ref listDraw, ref m);
            Marshal.StructureToPtr(listDraw, m.LParam, false);
        }

        if (m.Msg == LvmSetTileViewInfo && View == View.Tile && IsHandleCreated)
        {
            SendMessage(Handle, LvmArrange, IntPtr.Zero, IntPtr.Zero);
            Invalidate();
        }
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

        CaptureOwnerDrawTarget(e.Graphics, paintBounds);
        try
        {
            PaintColumnHeader(e, paintBounds);
            base.OnDrawColumnHeader(e);
            if (e.DrawDefault) RestoreOwnerDrawTarget(e.Graphics, paintBounds);
        }
        finally
        {
            ReleaseOwnerDrawBackup();
        }
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

        CaptureOwnerDrawTarget(e.Graphics, e.Bounds);
        try
        {
            DrawNonDetailsItem(e);
            base.OnDrawItem(e);
            if (e.DrawDefault) RestoreOwnerDrawTarget(e.Graphics, e.Bounds);
        }
        finally
        {
            ReleaseOwnerDrawBackup();
        }
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

        CaptureOwnerDrawTarget(e.Graphics, paintBounds);
        try
        {
            if (View == View.Details) DrawDetailsSubItem(e);
            base.OnDrawSubItem(e);
            if (e.DrawDefault) RestoreOwnerDrawTarget(e.Graphics, paintBounds);
        }
        finally
        {
            ReleaseOwnerDrawBackup();
        }
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

        var selected = IsActuallySelected(item);
        var hotTracked = HotTracking && (e.ItemState & ListViewItemStates.Hot) != 0;
        var hovered = _hoverHighlight && e.ItemIndex == _hoveredItemIndex;
        var rowBounds = e.Bounds;
        if (e.ColumnIndex == 0)
        {
            rowBounds = GetNativeBounds(item, ItemBoundsPortion.Entire, e.Bounds);
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
        var selected = IsActuallySelected(item);
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
                item.UseItemStyleForSubItems
                    ? palette.ForeColor
                    : BootstrapThemeManager.CurrentTheme.Colors.MutedText,
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
        if (stateImage is null && (StateImageList is not null || !CheckBoxes || !allowCheckboxFallback)) return;
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

    private static bool IsActuallySelected(ListViewItem item) => item.Selected;

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

    private void CaptureOwnerDrawTarget(Graphics targetGraphics, Rectangle targetBounds)
    {
        if (targetBounds.Width <= 0 || targetBounds.Height <= 0) return;
        var requiredSize = new Size(
            Math.Max(1, targetBounds.Width),
            Math.Max(1, targetBounds.Height));
        if (_ownerDrawBuffer is null ||
            _ownerDrawBuffer.Width < requiredSize.Width ||
            _ownerDrawBuffer.Height < requiredSize.Height ||
            Math.Abs(_ownerDrawBuffer.HorizontalResolution - targetGraphics.DpiX) > 0.01f ||
            Math.Abs(_ownerDrawBuffer.VerticalResolution - targetGraphics.DpiY) > 0.01f)
        {
            var width = Math.Max(requiredSize.Width, _ownerDrawBuffer?.Width ?? 0);
            var height = Math.Max(requiredSize.Height, _ownerDrawBuffer?.Height ?? 0);
            DisposeOwnerDrawBuffer();
            _ownerDrawBuffer = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            _ownerDrawBuffer.SetResolution(targetGraphics.DpiX, targetGraphics.DpiY);
            _ownerDrawBufferGraphics = Graphics.FromImage(_ownerDrawBuffer);
        }

        var graphics = _ownerDrawBufferGraphics!;
        graphics.Flush(FlushIntention.Sync);
        _ownerDrawBufferHdc = graphics.GetHdc();
        if (_activeNativePaintHdc != IntPtr.Zero)
        {
            BitBlt(_ownerDrawBufferHdc, 0, 0, targetBounds.Width, targetBounds.Height,
                _activeNativePaintHdc, targetBounds.X, targetBounds.Y, SourceCopy);
        }
        else
        {
            targetGraphics.Flush(FlushIntention.Sync);
            var sourceHdc = targetGraphics.GetHdc();
            try
            {
                BitBlt(_ownerDrawBufferHdc, 0, 0, targetBounds.Width, targetBounds.Height,
                    sourceHdc, targetBounds.X, targetBounds.Y, SourceCopy);
            }
            finally
            {
                targetGraphics.ReleaseHdc(sourceHdc);
            }
        }
    }

    private void RestoreOwnerDrawTarget(Graphics targetGraphics, Rectangle targetBounds)
    {
        if (_ownerDrawBufferHdc == IntPtr.Zero) return;
        if (targetBounds.Width <= 0 || targetBounds.Height <= 0) return;
        if (_activeNativePaintHdc != IntPtr.Zero)
        {
            BitBlt(_activeNativePaintHdc, targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height,
                _ownerDrawBufferHdc, 0, 0, SourceCopy);
        }
        else
        {
            targetGraphics.Flush(FlushIntention.Sync);
            var destinationHdc = targetGraphics.GetHdc();
            try
            {
                BitBlt(destinationHdc, targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height,
                    _ownerDrawBufferHdc, 0, 0, SourceCopy);
            }
            finally
            {
                targetGraphics.ReleaseHdc(destinationHdc);
            }
        }
    }

    private void ReleaseOwnerDrawBackup()
    {
        if (_ownerDrawBufferHdc == IntPtr.Zero || _ownerDrawBufferGraphics is null) return;
        _ownerDrawBufferGraphics.ReleaseHdc(_ownerDrawBufferHdc);
        _ownerDrawBufferHdc = IntPtr.Zero;
    }

    private void DisposeOwnerDrawBuffer()
    {
        ReleaseOwnerDrawBackup();
        _ownerDrawBufferGraphics?.Dispose();
        _ownerDrawBufferGraphics = null;
        _ownerDrawBuffer?.Dispose();
        _ownerDrawBuffer = null;
    }

    private bool IsHeaderCustomDraw(IntPtr parameter, out NativeCustomDraw customDraw)
    {
        customDraw = default;
        if (parameter == IntPtr.Zero || !IsHandleCreated) return false;
        customDraw = Marshal.PtrToStructure<NativeCustomDraw>(parameter);
        return customDraw.Header.Code == NmCustomDraw && customDraw.Header.WindowFrom == GetHeaderHandle();
    }

    private static bool IsListCustomDraw(IntPtr parameter, out NativeListViewCustomDraw customDraw)
    {
        customDraw = default;
        if (parameter == IntPtr.Zero) return false;
        customDraw = Marshal.PtrToStructure<NativeListViewCustomDraw>(parameter);
        return customDraw.CustomDraw.Header.Code == NmCustomDraw;
    }


    private static bool IsGroupCustomDraw(NativeListViewCustomDraw customDraw) =>
        customDraw.CustomDraw.DrawStage == CddsPrePaint && customDraw.ItemType == LvcdItemGroup;

    private void ApplyGroupHeaderTheme(ref NativeListViewCustomDraw customDraw, ref Message message)
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        customDraw.TextColor = ColorToColorRef(colors.Text);
        customDraw.TextBackgroundColor = ColorToColorRef(colors.Surface);
        customDraw.FaceColor = ColorToColorRef(colors.Border);
        if (PaintGroupHeader(customDraw, colors)) message.Result = (IntPtr)CdrfSkipDefault;
    }

    private bool PaintGroupHeader(NativeListViewCustomDraw customDraw, BootstrapThemeColors colors)
    {
        var deviceContext = customDraw.CustomDraw.DeviceContext;
        var bounds = customDraw.CustomDraw.Rectangle.ToRectangle();
        if (deviceContext == IntPtr.Zero || bounds.Width <= 0 || bounds.Height <= 0 ||
            !TryGetGroupHeader(customDraw.CustomDraw.ItemSpec, out var header, out var alignment)) return false;

        var horizontalPadding = DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.SpacingSM, GetCurrentDpi());
        var textBounds = customDraw.TextRectangle.ToRectangle();
        if (textBounds.Width <= 0 || textBounds.Height <= 0)
        {
            textBounds = BootstrapListViewLayoutLogic.Deflate(bounds, horizontalPadding, 0);
        }

        var headerBounds = Rectangle.FromLTRB(bounds.Left, textBounds.Top, bounds.Right, textBounds.Bottom);

        using var graphics = Graphics.FromHdc(deviceContext);
        using var background = new SolidBrush(colors.Surface);
        using var separator = new Pen(colors.Border, Math.Max(1, DpiScaler.Scale(1, GetCurrentDpi())));
        using var groupFont = new Font(Font, Font.Style | FontStyle.Bold);
        graphics.FillRectangle(background, headerBounds);
        TextRenderer.DrawText(
            graphics,
            header,
            groupFont,
            textBounds,
            colors.Text,
            BootstrapListViewLayoutLogic.GetTextFlags(alignment, RightToLeft == RightToLeft.Yes, false) |
            TextFormatFlags.VerticalCenter);
        var textSize = TextRenderer.MeasureText(graphics, header, groupFont, Size.Empty, TextFormatFlags.NoPadding);
        var separatorY = headerBounds.Top + (headerBounds.Height / 2);
        if (alignment == HorizontalAlignment.Right)
        {
            graphics.DrawLine(separator, headerBounds.Left + horizontalPadding, separatorY,
                Math.Max(headerBounds.Left + horizontalPadding, textBounds.Right - textSize.Width - horizontalPadding), separatorY);
        }
        else if (alignment == HorizontalAlignment.Center)
        {
            var textLeft = textBounds.Left + ((textBounds.Width - textSize.Width) / 2);
            graphics.DrawLine(separator, headerBounds.Left + horizontalPadding, separatorY,
                Math.Max(headerBounds.Left + horizontalPadding, textLeft - horizontalPadding), separatorY);
            graphics.DrawLine(separator, Math.Min(headerBounds.Right - horizontalPadding, textLeft + textSize.Width + horizontalPadding), separatorY,
                headerBounds.Right - horizontalPadding, separatorY);
        }
        else
        {
            graphics.DrawLine(separator, Math.Min(headerBounds.Right - horizontalPadding, textBounds.Left + textSize.Width + horizontalPadding), separatorY,
                headerBounds.Right - horizontalPadding, separatorY);
        }

        return true;
    }

    private bool TryGetGroupHeader(UIntPtr itemSpec, out string header, out HorizontalAlignment alignment)
    {
        const int bufferCharacters = 512;
        header = string.Empty;
        alignment = HorizontalAlignment.Left;
        var buffer = Marshal.AllocHGlobal(bufferCharacters * sizeof(char));
        try
        {
            for (var offset = 0; offset < bufferCharacters * sizeof(char); offset += sizeof(int))
            {
                Marshal.WriteInt32(buffer, offset, 0);
            }

            var group = new NativeListViewGroup
            {
                Size = (uint)Marshal.SizeOf(typeof(NativeListViewGroup)),
                Mask = LvgfHeader | LvgfAlign,
                Header = buffer,
                HeaderLength = bufferCharacters
            };
            var groupId = unchecked((int)itemSpec.ToUInt64());
            if (SendMessage(Handle, LvmGetGroupInfo, (IntPtr)groupId, ref group).ToInt64() == -1) return false;
            header = Marshal.PtrToStringUni(buffer) ?? string.Empty;
            alignment = (group.Align & 0x00000004) != 0
                ? HorizontalAlignment.Right
                : (group.Align & 0x00000002) != 0
                    ? HorizontalAlignment.Center
                    : HorizontalAlignment.Left;
            return header.Length > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void PaintHeaderFiller(IntPtr headerHandle, IntPtr deviceContext)
    {
        if (headerHandle == IntPtr.Zero || deviceContext == IntPtr.Zero || Columns.Count == 0 ||
            !GetClientRect(headerHandle, out var clientRectangle)) return;

        var right = clientRectangle.Left;
        for (var index = 0; index < Columns.Count; index++)
        {
            var columnRectangle = new NativeRectangle();
            if (SendMessage(headerHandle, HdmGetItemRect, (IntPtr)index, ref columnRectangle) == IntPtr.Zero) continue;
            right = Math.Max(right, columnRectangle.Right);
        }

        // Header coordinates are logical when WS_EX_LAYOUTRTL is active; GDI maps this trailing
        // logical rectangle to the physical left side for a mirrored control.
        var filler = Rectangle.FromLTRB(
            Math.Min(clientRectangle.Right, right),
            clientRectangle.Top,
            clientRectangle.Right,
            clientRectangle.Bottom);
        if (filler.Width <= 0 || filler.Height <= 0) return;

        using var graphics = Graphics.FromHdc(deviceContext);
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        using var background = new SolidBrush(colors.SurfaceSecondary);
        using var separator = new Pen(colors.Border, Math.Max(1, DpiScaler.Scale(1, GetCurrentDpi())));
        graphics.FillRectangle(background, filler);
        graphics.DrawLine(separator, filler.Left, filler.Bottom - 1, filler.Right, filler.Bottom - 1);
    }

    private IntPtr GetHeaderHandle() => IsHandleCreated
        ? SendMessage(Handle, LvmGetHeader, IntPtr.Zero, IntPtr.Zero)
        : IntPtr.Zero;

    private static IntPtr OrCustomDrawResult(IntPtr current, int flags) =>
        new IntPtr(current.ToInt64() | (long)(uint)flags);

    private static uint ColorToColorRef(Color color) =>
        (uint)(color.R | (color.G << 8) | (color.B << 16));

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
        var header = GetHeaderHandle();
        if (header != IntPtr.Zero) InvalidateRect(header, IntPtr.Zero, true);
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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativeListViewGroup group);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativeRectangle rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out NativeRectangle rectangle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(IntPtr window, IntPtr rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(
        IntPtr destination,
        int destinationX,
        int destinationY,
        int width,
        int height,
        IntPtr source,
        int sourceX,
        int sourceY,
        int rasterOperation);

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeListViewGroup
    {
        internal uint Size;
        internal uint Mask;
        internal IntPtr Header;
        internal int HeaderLength;
        internal IntPtr Footer;
        internal int FooterLength;
        internal int GroupId;
        internal uint StateMask;
        internal uint State;
        internal uint Align;
        internal IntPtr Subtitle;
        internal uint SubtitleLength;
        internal IntPtr Task;
        internal uint TaskLength;
        internal IntPtr DescriptionTop;
        internal uint DescriptionTopLength;
        internal IntPtr DescriptionBottom;
        internal uint DescriptionBottomLength;
        internal int TitleImage;
        internal int ExtendedImage;
        internal int FirstItem;
        internal uint ItemCount;
        internal IntPtr SubsetTitle;
        internal uint SubsetTitleLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeNotifyHeader
    {
        internal IntPtr WindowFrom;
        internal UIntPtr IdFrom;
        internal int Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCustomDraw
    {
        internal NativeNotifyHeader Header;
        internal uint DrawStage;
        internal IntPtr DeviceContext;
        internal NativeRectangle Rectangle;
        internal UIntPtr ItemSpec;
        internal uint ItemState;
        internal IntPtr ItemParameter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeListViewCustomDraw
    {
        internal NativeCustomDraw CustomDraw;
        internal uint TextColor;
        internal uint TextBackgroundColor;
        internal int SubItem;
        internal uint ItemType;
        internal uint FaceColor;
        internal int IconEffect;
        internal int IconPhase;
        internal int PartId;
        internal int StateId;
        internal NativeRectangle TextRectangle;
        internal uint Align;
    }

    private delegate void NativeMessageDispatcher(ref Message message);

    private sealed class BootstrapListViewNativeWindow : NativeWindow
    {
        private readonly BootstrapListView _owner;

        internal BootstrapListViewNativeWindow(BootstrapListView owner) => _owner = owner;

        protected override void WndProc(ref Message m) => _owner.ProcessNativeMessage(ref m, Dispatch);

        private void Dispatch(ref Message message) => base.WndProc(ref message);
    }
}
