using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-themed presentation while retaining the native <see cref="TreeView"/> contract.
/// </summary>
public class BootstrapTreeView : TreeView
{
    private const int NativeNoImageIndex = -2;
    private const int NativeLogicalStateImageSlotSize = 16;
    private const int NativeMaximumStateImageIndex = 14;
    private const int TvFirst = 0x1100;
    private const int TvmGetImageList = TvFirst + 8;
    private const int TvsilState = 2;

    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private IntPtr _nativeStateImageSlotHandle;
    private int _nativeStateImageSlotWidth;
    private TreeNode? _hotNode;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _initialized;
    private Font? _themeFont;
    private bool _useFrameworkItemHeight = true;
    private int _frameworkItemHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapTreeView"/> class.
    /// </summary>
    public BootstrapTreeView()
    {
        DrawMode = TreeViewDrawMode.OwnerDrawAll;

        _initialized = true;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyFrameworkItemHeight(BootstrapThemeManager.CurrentTheme, GetCurrentDpi());
        ApplyTheme();
    }

    /// <summary>
    /// Gets or sets the Bootstrap semantic variant used for selected-node presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Bootstrap semantic variant used for selected-node presentation.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        if (_initialized)
        {
            UpdateItemHeightOwnership();
        }

        var itemHeightBeforeBase = ItemHeight;
        base.OnFontChanged(e);
        if (!_initialized)
        {
            return;
        }

        if (_useFrameworkItemHeight && _frameworkItemHeight > 0 && ItemHeight != _frameworkItemHeight)
        {
            ItemHeight = _frameworkItemHeight;
        }
        else if (_settingThemeFont && !_useFrameworkItemHeight && ItemHeight != itemHeightBeforeBase)
        {
            ItemHeight = itemHeightBeforeBase;
        }

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
        if (_initialized)
        {
            UpdateItemHeightOwnership();
        }

        var callerItemHeight = ItemHeight;
        base.OnDpiChangedAfterParent(e);
        if (!_initialized || IsDisposed || Disposing)
        {
            return;
        }

        if (_useFrameworkItemHeight)
        {
            ApplyFrameworkItemHeight(BootstrapThemeManager.CurrentTheme, GetCurrentDpi());
        }
        else if (ItemHeight != callerItemHeight)
        {
            ItemHeight = callerItemHeight;
        }

        ResetNativeStateImageSlotCache();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ResetNativeStateImageSlotCache();
        if (_initialized && !IsDisposed && !Disposing)
        {
            ApplyTheme();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        ResetNativeStateImageSlotCache();
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!IsDisposed && !Disposing)
        {
            var hit = HitTest(e.Location);
            var nativeOnItemLocations =
                TreeViewHitTestLocations.Image |
                TreeViewHitTestLocations.Label |
                TreeViewHitTestLocations.StateImage;
            UpdateHotNode((hit.Location & nativeOnItemLocations) != 0 ? hit.Node : null);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!IsDisposed && !Disposing)
        {
            UpdateHotNode(null);
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateNodeRow(SelectedNode);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateNodeRow(SelectedNode);
    }

    /// <inheritdoc />
    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        RaiseObservableDrawNodeEvent(e);
        var node = e.Node;
        if (node is null)
        {
            return;
        }

        RenderNodeCore(e.Graphics, node, e.Bounds, node.Bounds, e.State);
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

            _hotNode = null;
            ResetNativeStateImageSlotCache();
            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    internal void RenderNodeForTesting(
        Graphics graphics,
        TreeNode node,
        Rectangle rowBounds,
        Rectangle nativeLabelBounds,
        TreeNodeStates state)
    {
        if (graphics is null)
        {
            throw new ArgumentNullException(nameof(graphics));
        }

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var eventArgs = new DrawTreeNodeEventArgs(graphics, node, rowBounds, state);
        RaiseObservableDrawNodeEvent(eventArgs);
        RenderNodeCore(graphics, node, rowBounds, nativeLabelBounds, state);
    }

    internal static int CalculateDefaultItemHeight(BootstrapTheme theme, int dpi)
    {
        if (theme is null)
        {
            throw new ArgumentNullException(nameof(theme));
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi));
        }

        var token = theme.Typography.Body;
        using var font = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var textHeight = (int)Math.Ceiling(font.GetHeight(dpi));
        var verticalAllowance = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        return Math.Max(1, textHeight + verticalAllowance);
    }

    internal static bool ShouldDrawFocusCueForTesting(
        bool visibleSelected,
        bool focused,
        bool showFocusCues)
    {
        return visibleSelected && focused && showFocusCues;
    }

    internal static bool ShouldDrawExpander(
        int nodeLevel,
        int childCount,
        bool showPlusMinus,
        bool showRootLines)
    {
        return childCount > 0 &&
               showPlusMinus &&
               (nodeLevel > 0 || showRootLines);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (!_initialized || IsDisposed || Disposing)
        {
            return;
        }

        UpdateItemHeightOwnership();
        if (_useThemeFont)
        {
            ApplyThemeFont();
        }

        if (_useFrameworkItemHeight)
        {
            ApplyFrameworkItemHeight(BootstrapThemeManager.CurrentTheme, GetCurrentDpi());
        }

        ApplyTheme();
        ResetNativeStateImageSlotCache();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        if (ThemeFontMatches(token))
        {
            return;
        }

        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = nextFont;
        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        catch
        {
            _themeFont = previous;
            nextFont.Dispose();
            throw;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
    }

    private bool ThemeFontMatches(BootstrapFontToken token)
    {
        return _themeFont is not null &&
            string.Equals(_themeFont.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(_themeFont.SizeInPoints - token.SizeInPoints) < 0.01f &&
            _themeFont.Style == token.Style;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private int GetCurrentDpi()
    {
        return DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
    }

    private void UpdateItemHeightOwnership()
    {
        if (_useFrameworkItemHeight &&
            _frameworkItemHeight > 0 &&
            ItemHeight != _frameworkItemHeight)
        {
            _useFrameworkItemHeight = false;
        }
    }

    private void ApplyFrameworkItemHeight(BootstrapTheme theme, int dpi)
    {
        if (!_useFrameworkItemHeight)
        {
            return;
        }

        var nextItemHeight = CalculateDefaultItemHeight(theme, dpi);
        if (ItemHeight != nextItemHeight)
        {
            ItemHeight = nextItemHeight;
        }

        _frameworkItemHeight = ItemHeight;
    }

    private void ResetNativeStateImageSlotCache()
    {
        _nativeStateImageSlotHandle = IntPtr.Zero;
        _nativeStateImageSlotWidth = 0;
    }

    private void UpdateHotNode(TreeNode? node)
    {
        if (IsDisposed || Disposing || ReferenceEquals(_hotNode, node))
        {
            return;
        }

        var previous = _hotNode;
        _hotNode = node;
        InvalidateNodeRow(previous);
        InvalidateNodeRow(node);
    }

    private void InvalidateNodeRow(TreeNode? node)
    {
        if (node is null || IsDisposed || Disposing || node.TreeView != this)
        {
            return;
        }

        var bounds = node.Bounds;
        if (bounds.IsEmpty || ClientRectangle.IsEmpty)
        {
            return;
        }

        var rowBounds = Rectangle.Intersect(
            ClientRectangle,
            new Rectangle(ClientRectangle.Left, bounds.Top, ClientRectangle.Width, ItemHeight));
        if (!rowBounds.IsEmpty)
        {
            Invalidate(rowBounds);
        }
    }

    private void RaiseObservableDrawNodeEvent(DrawTreeNodeEventArgs e)
    {
        base.OnDrawNode(e);

        // DrawNode remains observable, but BootstrapTreeView owns the complete
        // OwnerDrawAll presentation. A subscriber cannot switch back to native
        // painting by setting DrawDefault.
        e.DrawDefault = false;
    }

    private void RenderNodeCore(
        Graphics graphics,
        TreeNode node,
        Rectangle rowBounds,
        Rectangle nativeLabelBounds,
        TreeNodeStates state)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = GetCurrentDpi();
        var mirrorTreeStructure = RightToLeft == RightToLeft.Yes && RightToLeftLayout;
        var selected = (state & TreeNodeStates.Selected) == TreeNodeStates.Selected;
        var visibleSelected = selected && (!HideSelection || Focused);
        var hot = HotTracking && ReferenceEquals(node, _hotNode);
        var visualState = new BootstrapTreeNodeVisualState(
            selected: visibleSelected,
            hot: hot,
            enabled: Enabled);
        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(theme.Colors, _variant, visualState);
        var hasExpander = ShouldDrawExpander(
            node.Level,
            node.Nodes.Count,
            ShowPlusMinus,
            ShowRootLines);

        var nodeImageIndex = ResolveNodeImageIndex(node, selected);
        var nodeImage = ResolveImage(ImageList, nodeImageIndex);
        var stateImageIndex = ResolveStateImageIndex(node);
        var stateImage = ResolveImage(StateImageList, stateImageIndex);
        var drawFrameworkCheckbox = CheckBoxes && stateImage is null;
        var hasStateImage = drawFrameworkCheckbox || stateImage is not null;
        var nativeStateImageSlotWidth = hasStateImage
            ? ResolveNativeStateImageSlotWidth(node)
            : 0;
        var hasNodeImage = nodeImage is not null && ImageList is not null;
        var nodeImageSize = hasNodeImage ? ImageList!.ImageSize : Size.Empty;

        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            ClientRectangle,
            rowBounds,
            nativeLabelBounds,
            node.Level,
            dpi,
            mirrorTreeStructure,
            FullRowSelect && !ShowLines,
            hasExpander,
            hasStateImage,
            nativeStateImageSlotWidth,
            hasNodeImage,
            nodeImageSize,
            useNativeStateImageSize: stateImage is not null));

        var backgroundBounds = visibleSelected ? layout.SelectionBounds : layout.TextBounds;
        var background = palette.Background;
        var foreground = palette.Foreground;

        if (Enabled && !visibleSelected && !hot)
        {
            if (!node.BackColor.IsEmpty)
            {
                background = node.BackColor;
            }

            if (!node.ForeColor.IsEmpty)
            {
                foreground = node.ForeColor;
            }
        }

        if (backgroundBounds.Width > 0 && backgroundBounds.Height > 0)
        {
            using var backgroundBrush = new SolidBrush(background);
            graphics.FillRectangle(backgroundBrush, backgroundBounds);
        }

        if (ShowLines)
        {
            DrawConnectorLines(graphics, node, layout, theme.Colors.Border, dpi);
        }

        if (hasExpander && !layout.ExpanderBounds.IsEmpty)
        {
            var expanderColor = theme.Colors.MutedText;
            if (visibleSelected && layout.SelectionBounds.Contains(layout.ExpanderBounds))
            {
                expanderColor = foreground;
            }

            DrawExpander(
                graphics,
                layout.ExpanderBounds,
                node.IsExpanded,
                mirrorTreeStructure,
                expanderColor,
                dpi);
        }

        if (stateImage is not null && !layout.StateImageBounds.IsEmpty)
        {
            DrawImageInSlot(graphics, stateImage, layout.StateImageBounds, stretchToSlot: true);
        }
        else if (drawFrameworkCheckbox && !layout.StateImageBounds.IsEmpty)
        {
            DrawFrameworkCheckbox(graphics, layout.StateImageBounds, node.Checked, theme, dpi);
        }

        if (nodeImage is not null && !layout.NodeImageBounds.IsEmpty)
        {
            var imageState = graphics.Save();
            try
            {
                graphics.SetClip(layout.RowBounds, CombineMode.Intersect);
                DrawImageInSlot(graphics, nodeImage, layout.NodeImageBounds);
            }
            finally
            {
                graphics.Restore(imageState);
            }
        }

        if (layout.TextBounds.Width > 0 && layout.TextBounds.Height > 0 && !string.IsNullOrEmpty(node.Text))
        {
            var font = node.NodeFont ?? Font;
            var flags = TextFormatFlags.NoPrefix |
                        TextFormatFlags.NoPadding |
                        TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.VerticalCenter;
            if (RightToLeft == RightToLeft.Yes)
            {
                flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
            }

            var textState = graphics.Save();
            try
            {
                graphics.SetClip(layout.TextBounds, CombineMode.Intersect);
                TextRenderer.DrawText(
                    graphics,
                    node.Text,
                    font,
                    nativeLabelBounds,
                    foreground,
                    flags);
            }
            finally
            {
                graphics.Restore(textState);
            }
        }

        if (ShouldDrawFocusCueForTesting(visibleSelected, Focused, ShowFocusCues))
        {
            DrawFocusCue(graphics, layout.FocusBounds, theme.Colors.Focus, dpi);
        }
    }

    private int ResolveNodeImageIndex(TreeNode node, bool selected)
    {
        var imageList = ImageList;
        if (imageList is null || imageList.Images.Count == 0)
        {
            return -1;
        }

        if (selected)
        {
            var index = ResolveConfiguredImageIndex(
                imageList,
                node.SelectedImageKey,
                node.SelectedImageIndex);
            if (index == NativeNoImageIndex || index >= 0)
            {
                return index;
            }

            index = ResolveConfiguredImageIndex(
                imageList,
                SelectedImageKey,
                SelectedImageIndex);
            if (index == NativeNoImageIndex || index >= 0)
            {
                return index;
            }
        }

        var normalIndex = ResolveConfiguredImageIndex(imageList, node.ImageKey, node.ImageIndex);
        if (normalIndex == NativeNoImageIndex || normalIndex >= 0)
        {
            return normalIndex;
        }

        return ResolveConfiguredImageIndex(imageList, ImageKey, ImageIndex);
    }

    private int ResolveStateImageIndex(TreeNode node)
    {
        var stateImageList = StateImageList;
        if (stateImageList is null || stateImageList.Images.Count == 0)
        {
            return -1;
        }

        if (CheckBoxes)
        {
            var nativeIndex = node.Checked ? 1 : 0;
            return nativeIndex < stateImageList.Images.Count ? nativeIndex : -1;
        }

        var resolvedIndex = ResolveConfiguredImageIndex(
            stateImageList,
            node.StateImageKey,
            node.StateImageIndex);
        return resolvedIndex >= 0 && resolvedIndex <= NativeMaximumStateImageIndex
            ? resolvedIndex
            : -1;
    }

    private static int ResolveConfiguredImageIndex(ImageList imageList, string key, int index)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var keyIndex = imageList.Images.IndexOfKey(key);
            if (keyIndex >= 0)
            {
                return keyIndex;
            }
        }

        if (index == NativeNoImageIndex)
        {
            return NativeNoImageIndex;
        }

        if (index < 0 || imageList.Images.Count == 0)
        {
            return -1;
        }

        return Math.Min(index, imageList.Images.Count - 1);
    }

    private static Image? ResolveImage(ImageList? imageList, int index)
    {
        if (imageList is null || index < 0 || index >= imageList.Images.Count)
        {
            return null;
        }

        return imageList.Images[index];
    }

    private int ResolveNativeStateImageSlotWidth(TreeNode node)
    {
        if (!IsHandleCreated || node.TreeView != this)
        {
            return 0;
        }

        var currentHandle = Handle;
        if (_nativeStateImageSlotHandle != currentHandle)
        {
            _nativeStateImageSlotHandle = currentHandle;
            _nativeStateImageSlotWidth = 0;
        }

        if (_nativeStateImageSlotWidth > 0)
        {
            return _nativeStateImageSlotWidth;
        }

        var fallbackWidth = ResolveNativeStateImageListWidth();
        if (fallbackWidth <= 0)
        {
            fallbackWidth = NativeLogicalStateImageSlotSize;
        }

        var bounds = node.Bounds;
        if (bounds.IsEmpty)
        {
            return fallbackWidth;
        }

        var y = bounds.Top + (bounds.Height / 2);
        var first = -1;
        var last = -1;
        for (var x = ClientRectangle.Left; x < ClientRectangle.Right; x++)
        {
            var hit = HitTest(x, y);
            if (hit.Node != node ||
                (hit.Location & TreeViewHitTestLocations.StateImage) != TreeViewHitTestLocations.StateImage)
            {
                continue;
            }

            if (first < 0)
            {
                first = x;
            }

            last = x;
        }

        if (first < 0)
        {
            return fallbackWidth;
        }

        var width = last - first + 1;
        if (first > ClientRectangle.Left && last < ClientRectangle.Right - 1)
        {
            _nativeStateImageSlotWidth = width;
            return width;
        }

        return Math.Max(width, fallbackWidth);
    }

    private int ResolveNativeStateImageListWidth()
    {
        var nativeImageList = SendMessage(
            Handle,
            TvmGetImageList,
            new IntPtr(TvsilState),
            IntPtr.Zero);
        if (nativeImageList == IntPtr.Zero)
        {
            return 0;
        }

        return ImageListGetIconSize(nativeImageList, out var width, out _) && width > 0
            ? width
            : 0;
    }

    private void DrawConnectorLines(
        Graphics graphics,
        TreeNode node,
        BootstrapTreeViewNodeLayout layout,
        Color color,
        int dpi)
    {
        if (layout.RowBounds.IsEmpty)
        {
            return;
        }

        using var pen = new Pen(color, Math.Max(1, DpiScaler.Scale(1, dpi)))
        {
            DashStyle = DashStyle.Dot,
        };

        var currentAnchorX = layout.ExpanderAnchorX;
        var drawCurrentBranch = node.Level > 0 || ShowRootLines;
        if (drawCurrentBranch)
        {
            var currentVertical = BootstrapTreeViewLayout.CalculateVerticalConnectorLine(
                layout.RowBounds,
                currentAnchorX,
                continueAbove: node.Parent is not null || node.PrevNode is not null,
                continueBelow: node.NextNode is not null);
            DrawConnectorSegment(graphics, pen, currentVertical);

            var firstContentX = GetFirstContentX(layout);
            var currentHorizontal = BootstrapTreeViewLayout.CalculateHorizontalConnectorLine(
                layout.RowBounds,
                currentAnchorX,
                firstContentX);
            DrawConnectorSegment(graphics, pen, currentHorizontal);
        }

        var ancestor = node.Parent;
        var ancestorLevel = node.Level - 1;
        while (ancestor is not null && ancestorLevel >= 0)
        {
            if (ancestor.NextNode is not null && (ancestorLevel > 0 || ShowRootLines))
            {
                var ancestorX = BootstrapTreeViewLayout.CalculateAncestorConnectorX(
                    currentAnchorX,
                    node.Level,
                    ancestorLevel,
                    Indent,
                    RightToLeft == RightToLeft.Yes && RightToLeftLayout);
                var continuation = BootstrapTreeViewLayout.CalculateVerticalConnectorLine(
                    layout.RowBounds,
                    ancestorX,
                    continueAbove: true,
                    continueBelow: true);
                DrawConnectorSegment(graphics, pen, continuation);
            }

            ancestor = ancestor.Parent;
            ancestorLevel--;
        }
    }

    private static int GetFirstContentX(BootstrapTreeViewNodeLayout layout)
    {
        if (!layout.StateImageBounds.IsEmpty)
        {
            return layout.StateImageBounds.Left;
        }

        if (!layout.NodeImageBounds.IsEmpty)
        {
            return layout.NodeImageBounds.Left;
        }

        return layout.TextBounds.IsEmpty ? layout.ExpanderAnchorX : layout.TextBounds.Left;
    }

    private static void DrawConnectorSegment(
        Graphics graphics,
        Pen pen,
        BootstrapTreeViewLineSegment segment)
    {
        if (!segment.IsEmpty)
        {
            graphics.DrawLine(pen, segment.Start, segment.End);
        }
    }

    private static void DrawExpander(
        Graphics graphics,
        Rectangle bounds,
        bool expanded,
        bool rightToLeft,
        Color color,
        int dpi)
    {
        var glyph = BootstrapTreeViewLayout.CalculateExpanderGlyph(bounds, expanded, rightToLeft);
        if (glyph.First == glyph.Tip || glyph.Tip == glyph.Second)
        {
            return;
        }

        using var pen = new Pen(color, Math.Max(1, DpiScaler.Scale(1, dpi)))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        var previousSmoothingMode = graphics.SmoothingMode;
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawLine(pen, glyph.First, glyph.Tip);
            graphics.DrawLine(pen, glyph.Tip, glyph.Second);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothingMode;
        }
    }

    private void DrawFrameworkCheckbox(
        Graphics graphics,
        Rectangle bounds,
        bool isChecked,
        BootstrapTheme theme,
        int dpi)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var borderColor = Enabled ? theme.Colors.Border : theme.Colors.Disabled;
        var fillColor = isChecked
            ? BootstrapVariantColorResolver.Resolve(theme.Colors, _variant)
            : theme.Colors.Surface;
        if (!Enabled)
        {
            fillColor = theme.Colors.SurfaceSecondary;
        }

        using (var fillBrush = new SolidBrush(fillColor))
        {
            graphics.FillRectangle(fillBrush, bounds);
        }

        using (var borderPen = new Pen(borderColor, Math.Max(1, DpiScaler.Scale(1, dpi))))
        {
            graphics.DrawRectangle(
                borderPen,
                bounds.Left,
                bounds.Top,
                Math.Max(0, bounds.Width - 1),
                Math.Max(0, bounds.Height - 1));
        }

        if (!isChecked || bounds.Width < 6 || bounds.Height < 6)
        {
            return;
        }

        var checkColor = Enabled
            ? ColorUtil.GetContrastingTextColor(
                fillColor,
                theme.Colors.Light,
                theme.Colors.Dark)
            : theme.Colors.MutedText;
        var strokeWidth = Math.Max(1, DpiScaler.Scale(2, dpi));
        var left = bounds.Left + Math.Max(2, bounds.Width / 5);
        var middleX = bounds.Left + Math.Max(3, (bounds.Width * 2) / 5);
        var right = bounds.Right - Math.Max(2, bounds.Width / 5) - 1;
        var middleY = bounds.Top + (bounds.Height / 2);
        var bottom = bounds.Bottom - Math.Max(2, bounds.Height / 4) - 1;
        var top = bounds.Top + Math.Max(2, bounds.Height / 4);

        using var checkPen = new Pen(checkColor, strokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };
        var previousSmoothingMode = graphics.SmoothingMode;
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawLine(checkPen, new Point(left, middleY), new Point(middleX, bottom));
            graphics.DrawLine(checkPen, new Point(middleX, bottom), new Point(right, top));
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothingMode;
        }
    }

    private static void DrawImageInSlot(
        Graphics graphics,
        Image image,
        Rectangle slotBounds,
        bool stretchToSlot = false)
    {
        var targetBounds = stretchToSlot
            ? slotBounds
            : BootstrapTreeViewLayout.CalculateContainedImageBounds(slotBounds, image.Size);
        if (targetBounds.IsEmpty)
        {
            return;
        }

        if (!stretchToSlot && targetBounds.Size == image.Size)
        {
            graphics.DrawImageUnscaled(image, targetBounds.Location);
            return;
        }

        var previousInterpolationMode = graphics.InterpolationMode;
        var previousPixelOffsetMode = graphics.PixelOffsetMode;
        try
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(
                image,
                targetBounds,
                0,
                0,
                image.Width,
                image.Height,
                GraphicsUnit.Pixel);
        }
        finally
        {
            graphics.InterpolationMode = previousInterpolationMode;
            graphics.PixelOffsetMode = previousPixelOffsetMode;
        }
    }

    private static void DrawFocusCue(Graphics graphics, Rectangle bounds, Color color, int dpi)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        var inset = Math.Max(1, DpiScaler.Scale(1, dpi));
        var focusBounds = Rectangle.Inflate(bounds, -inset, -inset);
        if (focusBounds.Width <= 0 || focusBounds.Height <= 0)
        {
            return;
        }

        using var pen = new Pen(color, Math.Max(1, DpiScaler.Scale(1, dpi)))
        {
            DashStyle = DashStyle.Dot,
        };
        graphics.DrawRectangle(
            pen,
            focusBounds.Left,
            focusBounds.Top,
            Math.Max(0, focusBounds.Width - 1),
            Math.Max(0, focusBounds.Height - 1));
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("comctl32.dll", EntryPoint = "ImageList_GetIconSize")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImageListGetIconSize(IntPtr imageList, out int width, out int height);
}
