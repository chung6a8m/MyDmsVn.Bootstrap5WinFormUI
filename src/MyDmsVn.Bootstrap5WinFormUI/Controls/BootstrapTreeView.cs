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
/// Provides Bootstrap-themed presentation while retaining the native <see cref="TreeView"/> contract.
/// </summary>
public class BootstrapTreeView : TreeView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapTreeView"/> class.
    /// </summary>
    public BootstrapTreeView()
    {
        BorderStyle = BorderStyle.None;
        DrawMode = TreeViewDrawMode.OwnerDrawAll;
        HideSelection = false;

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        ItemHeight = CalculateDefaultItemHeight(theme, dpi);
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
        if (IsDisposed)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var selected = (state & TreeNodeStates.Selected) == TreeNodeStates.Selected;
        var visibleSelected = selected && (!HideSelection || Focused);
        var visualState = new BootstrapTreeNodeVisualState(
            selected: visibleSelected,
            hot: false,
            enabled: Enabled);
        var palette = BootstrapTreeViewRenderLogic.ResolvePalette(theme.Colors, _variant, visualState);
        var hasExpander = ShouldDrawExpander(
            node.Level,
            node.Nodes.Count,
            ShowPlusMinus,
            ShowRootLines);
        var layout = BootstrapTreeViewLayout.Calculate(new BootstrapTreeViewLayoutInput(
            ClientRectangle,
            rowBounds,
            nativeLabelBounds,
            node.Level,
            dpi,
            RightToLeft == RightToLeft.Yes,
            FullRowSelect && !ShowLines,
            hasExpander,
            hasStateImage: false,
            nativeStateImageSlotWidth: 0,
            hasNodeImage: false,
            nodeImageSize: Size.Empty));

        var backgroundBounds = visibleSelected ? layout.SelectionBounds : layout.TextBounds;
        var background = palette.Background;
        var foreground = palette.Foreground;

        if (Enabled && !visibleSelected)
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
                RightToLeft == RightToLeft.Yes,
                expanderColor,
                dpi);
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

            TextRenderer.DrawText(
                graphics,
                node.Text,
                font,
                layout.TextBounds,
                foreground,
                flags);
        }

        if (ShouldDrawFocusCueForTesting(visibleSelected, Focused, ShowFocusCues))
        {
            DrawFocusCue(graphics, layout.FocusBounds, theme.Colors.Focus, dpi);
        }
    }

    private void DrawConnectorLines(
        Graphics graphics,
        TreeNode node,
        BootstrapTreeViewNodeLayout layout,
        Color color,
        int dpi)
    {
        if (layout.RowBounds.IsEmpty || layout.ExpanderSlotBounds.IsEmpty)
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
                    RightToLeft == RightToLeft.Yes);
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
}
