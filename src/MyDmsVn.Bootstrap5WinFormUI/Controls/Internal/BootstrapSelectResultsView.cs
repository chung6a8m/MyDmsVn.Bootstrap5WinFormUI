using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectResultsView : Control
{
    private BootstrapSelectResultSet _results = new BootstrapSelectResultSet(Array.Empty<BootstrapSelectResultRow>());
    private IBootstrapSelectRenderer _renderer = new BootstrapSelectRenderer();
    private BootstrapTheme _theme = BootstrapThemeManager.CurrentTheme;
    private int _dpi = DpiScaler.DefaultDpi;
    private int _scrollOffset;
    private int _highlightedIndex = -1;
    private int _hotIndex = -1;

    internal BootstrapSelectResultsView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = false;
        BackColor = _theme.Colors.Surface;
    }

    internal event Action<BootstrapSelectResultRow, BootstrapSelectChangeReason>? RowActivated;
    internal event Action? NearEndReached;

    internal IReadOnlyList<BootstrapSelectResultRow> Rows => _results.Rows;
    internal int HighlightedIndex => _highlightedIndex;
    internal BootstrapSelectResultRow? HighlightedRow => _highlightedIndex >= 0 && _highlightedIndex < _results.Rows.Count ? _results.Rows[_highlightedIndex] : null;
    internal int ScrollOffset => _scrollOffset;
    internal int RowHeight => DpiScaler.Scale(32, _dpi);

    internal void ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        _dpi = dpi;
        BackColor = theme.Colors.Surface;
        ClampScroll();
        Invalidate();
    }

    internal void SetResults(BootstrapSelectResultSet results)
    {
        SetResults(
            results,
            BootstrapSelectResultsUpdateMode.ResetNavigation,
            EqualityComparer<object>.Default);
    }

    internal void SetResults(
        BootstrapSelectResultSet results,
        BootstrapSelectResultsUpdateMode updateMode,
        IEqualityComparer<object> valueComparer)
    {
        if (results is null) throw new ArgumentNullException(nameof(results));
        if (valueComparer is null) throw new ArgumentNullException(nameof(valueComparer));

        var previousRow = HighlightedRow;
        var previousIndex = _highlightedIndex;
        var previousScrollOffset = _scrollOffset;
        _results = results;
        _hotIndex = -1;

        switch (updateMode)
        {
            case BootstrapSelectResultsUpdateMode.ResetNavigation:
                _scrollOffset = 0;
                _highlightedIndex = FindFirstSelectable(preferSelected: true);
                break;

            case BootstrapSelectResultsUpdateMode.PreserveNavigation:
                _highlightedIndex = FindEquivalentItemIndex(previousRow, valueComparer);
                if (_highlightedIndex < 0)
                {
                    _highlightedIndex = FindNearestSelectable(previousIndex);
                }
                if (_highlightedIndex < 0)
                {
                    _highlightedIndex = FindFirstSelectable(preferSelected: true);
                }
                _scrollOffset = Math.Max(0, previousScrollOffset);
                ClampScroll();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(updateMode), updateMode, "Unsupported result update mode.");
        }

        EnsureHighlightedVisible();
        Invalidate();
        CheckNearEnd();
    }

    internal bool ActivateHighlighted(BootstrapSelectChangeReason reason)
    {
        var row = HighlightedRow;
        if (row is null || !IsSelectable(row)) return false;
        RowActivated?.Invoke(row, reason);
        return true;
    }

    internal bool MoveHighlight(int delta)
    {
        if (delta == 0 || _results.Rows.Count == 0) return false;
        var direction = delta > 0 ? 1 : -1;
        var remaining = Math.Abs(delta);
        var index = _highlightedIndex;
        if (index < 0) index = direction > 0 ? -1 : _results.Rows.Count;
        while (remaining > 0)
        {
            var next = FindSelectableFrom(index + direction, direction);
            if (next < 0) break;
            index = next;
            remaining--;
        }

        if (index < 0 || index == _highlightedIndex) return false;
        _highlightedIndex = index;
        EnsureHighlightedVisible();
        Invalidate();
        CheckNearEnd();
        return true;
    }

    internal bool MoveToFirst()
    {
        var index = FindSelectableFrom(0, 1);
        return SetHighlightedIndex(index);
    }

    internal bool MoveToLast()
    {
        var index = FindSelectableFrom(_results.Rows.Count - 1, -1);
        return SetHighlightedIndex(index);
    }

    internal bool Page(int direction)
    {
        var pageRows = Math.Max(1, ClientSize.Height / Math.Max(1, RowHeight));
        return MoveHighlight(direction >= 0 ? pageRows : -pageRows);
    }

    internal void SetScrollOffset(int offset)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        var layout = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, offset);
        if (_scrollOffset == layout.ScrollOffset) return;
        _scrollOffset = layout.ScrollOffset;
        Invalidate();
        CheckNearEnd();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var rows = Math.Max(1, Math.Min(8, _results.Rows.Count));
        return new Size(Math.Max(120, proposedSize.Width), rows * RowHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(_theme.Colors.Surface);
        var layout = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, _scrollOffset);
        if (layout.FirstVisibleIndex < 0) return;
        for (var index = layout.FirstVisibleIndex; index <= layout.LastVisibleIndex; index++)
        {
            DrawRow(e.Graphics, _results.Rows[index], layout.GetRowBounds(index, ClientSize.Width), index);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var layout = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, _scrollOffset);
        var next = layout.HitTestIndex(e.Y);
        if (next >= 0 && !IsSelectable(_results.Rows[next])) next = -1;
        if (_hotIndex != next)
        {
            _hotIndex = next;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hotIndex >= 0)
        {
            _hotIndex = -1;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        var layout = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, _scrollOffset);
        var index = layout.HitTestIndex(e.Y);
        if (index < 0 || !IsSelectable(_results.Rows[index])) return;
        _highlightedIndex = index;
        Invalidate();
        RowActivated?.Invoke(_results.Rows[index], BootstrapSelectChangeReason.Mouse);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        var rows = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        var deltaRows = e.Delta > 0 ? -rows : rows;
        var next = Math.Max(0, _scrollOffset + (deltaRows * RowHeight));
        SetScrollOffset(next);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ClampScroll();
    }

    private void DrawRow(Graphics graphics, BootstrapSelectResultRow row, Rectangle bounds, int index)
    {
        if (row.Kind == BootstrapSelectResultRowKind.GroupHeader)
        {
            _renderer.DrawGroupHeader(graphics, new BootstrapSelectGroupRenderContext(row.Text, bounds, _dpi, _theme, Font));
            return;
        }

        if (row.Kind == BootstrapSelectResultRowKind.Item && row.Item is not null)
        {
            var state = BootstrapSelectRenderState.None;
            if (row.IsSelected) state |= BootstrapSelectRenderState.Selected;
            if (index == _highlightedIndex) state |= BootstrapSelectRenderState.Highlighted;
            if (index == _hotIndex) state |= BootstrapSelectRenderState.Hot;
            if (row.Item.Disabled) state |= BootstrapSelectRenderState.Disabled;
            _renderer.DrawResult(graphics, new BootstrapSelectResultRenderContext(row.Item, bounds, state, _dpi, _theme, Font));
            return;
        }

        var background = index == _highlightedIndex ? _theme.Colors.Hover : _theme.Colors.Surface;
        using (var brush = new SolidBrush(background)) graphics.FillRectangle(brush, bounds);
        var textBounds = Rectangle.Inflate(bounds, -DpiScaler.Scale(8, _dpi), 0);
        TextRenderer.DrawText(graphics, row.Text, Font, textBounds, _theme.Colors.MutedText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    private int FindFirstSelectable(bool preferSelected)
    {
        if (preferSelected)
        {
            for (var i = 0; i < _results.Rows.Count; i++)
            {
                var row = _results.Rows[i];
                if (row.IsSelected && IsSelectable(row)) return i;
            }
        }

        return FindSelectableFrom(0, 1);
    }

    private int FindSelectableFrom(int start, int direction)
    {
        for (var i = start; i >= 0 && i < _results.Rows.Count; i += direction)
        {
            if (IsSelectable(_results.Rows[i])) return i;
        }
        return -1;
    }

    private int FindEquivalentItemIndex(
        BootstrapSelectResultRow? previousRow,
        IEqualityComparer<object> valueComparer)
    {
        if (previousRow?.Kind != BootstrapSelectResultRowKind.Item || previousRow.Item is null)
        {
            return -1;
        }

        for (var index = 0; index < _results.Rows.Count; index++)
        {
            var candidate = _results.Rows[index];
            if (candidate.Kind == BootstrapSelectResultRowKind.Item
                && candidate.Item is not null
                && IsSelectable(candidate)
                && valueComparer.Equals(previousRow.Item.Value, candidate.Item.Value))
            {
                return index;
            }
        }

        return -1;
    }

    private int FindNearestSelectable(int previousIndex)
    {
        if (_results.Rows.Count == 0 || previousIndex < 0)
        {
            return -1;
        }

        var origin = Math.Min(previousIndex, _results.Rows.Count - 1);
        for (var distance = 0; distance < _results.Rows.Count; distance++)
        {
            var after = origin + distance;
            if (after < _results.Rows.Count && IsSelectable(_results.Rows[after]))
            {
                return after;
            }

            var before = origin - distance;
            if (distance > 0 && before >= 0 && IsSelectable(_results.Rows[before]))
            {
                return before;
            }
        }

        return -1;
    }

    private static bool IsSelectable(BootstrapSelectResultRow row)
    {
        if (row.Kind == BootstrapSelectResultRowKind.Item) return row.Item is not null && !row.Item.Disabled;
        return row.Kind == BootstrapSelectResultRowKind.CreateValue
            || row.Kind == BootstrapSelectResultRowKind.Error
            || row.Kind == BootstrapSelectResultRowKind.LoadMoreError;
    }

    private bool SetHighlightedIndex(int index)
    {
        if (index < 0 || index == _highlightedIndex) return false;
        _highlightedIndex = index;
        EnsureHighlightedVisible();
        Invalidate();
        CheckNearEnd();
        return true;
    }

    private void EnsureHighlightedVisible()
    {
        if (_highlightedIndex < 0) return;
        var rowTop = _highlightedIndex * RowHeight;
        var rowBottom = rowTop + RowHeight;
        if (rowTop < _scrollOffset)
        {
            _scrollOffset = rowTop;
        }
        else if (rowBottom > _scrollOffset + ClientSize.Height)
        {
            _scrollOffset = Math.Max(0, rowBottom - ClientSize.Height);
        }
        ClampScroll();
    }

    private void ClampScroll()
    {
        _scrollOffset = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, Math.Max(0, _scrollOffset)).ScrollOffset;
    }

    private void CheckNearEnd()
    {
        if (_results.Rows.Count == 0) return;
        var layout = BootstrapSelectResultLayout.Create(_results.Rows.Count, RowHeight, ClientSize.Height, _scrollOffset);
        if (layout.LastVisibleIndex >= _results.Rows.Count - 3) NearEndReached?.Invoke();
    }
}
