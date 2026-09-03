using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!Enabled || ReadOnly) return base.ProcessCmdKey(ref msg, keyData);
        var key = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (key == Keys.Tab && (modifiers & (Keys.Alt | Keys.Control)) == Keys.None)
            return ProcessDialogKey(keyData);
        if (key == Keys.Escape && modifiers == Keys.None)
        {
            CancelPendingEdit();
            return true;
        }
        if ((key == Keys.F4 && modifiers == Keys.None) || (modifiers == Keys.Alt && key == Keys.Down) ||
            (!IsDropDownOpen && key == Keys.Down && modifiers == Keys.None))
        {
            OpenDropDown();
            return true;
        }
        if (IsDropDownOpen && modifiers == Keys.None && IsNavigationKey(key))
        {
            FlushPendingSearch();
            NavigateResults(key);
            return true;
        }
        if (key == Keys.Enter && modifiers == Keys.None && HandleEnterKey()) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc />
    protected override void OnEditorKeyDown(KeyEventArgs e)
    {
        if (e.Handled || !Enabled || ReadOnly)
        {
            base.OnEditorKeyDown(e);
            return;
        }

        var key = e.KeyCode;
        if (key == Keys.Escape && e.Modifiers == Keys.None)
        {
            CancelPendingEdit();
            Consume(e);
            return;
        }

        if ((key == Keys.F4 && e.Modifiers == Keys.None) || (e.Modifiers == Keys.Alt && key == Keys.Down) ||
            (!IsDropDownOpen && key == Keys.Down && e.Modifiers == Keys.None))
        {
            OpenDropDown();
            Consume(e);
            return;
        }

        if (IsDropDownOpen && e.Modifiers == Keys.None && IsNavigationKey(key))
        {
            FlushPendingSearch();
            NavigateResults(key);
            Consume(e);
            return;
        }

        if (key == Keys.Enter && e.Modifiers == Keys.None && HandleEnterKey())
        {
            Consume(e);
            return;
        }

        base.OnEditorKeyDown(e);
    }

    /// <inheritdoc />
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (!Enabled || ReadOnly) return base.ProcessDialogKey(keyData);

        var key = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (key == Keys.Tab && (modifiers & (Keys.Alt | Keys.Control)) == Keys.None)
        {
            FlushPendingSearch();
            var resolution = ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
            if (!resolution.NavigationAllowed)
            {
                OpenDropDown();
                return true;
            }
            CloseDropDown();
        }
        return base.ProcessDialogKey(keyData);
    }

    internal void NavigateResults(Keys key)
    {
        var grid = ResultsGrid;
        var count = grid.Rows.Count;
        if (count == 0)
        {
            SetHighlightedSourceItem(null);
            return;
        }

        var current = FindHighlightedRowIndex();
        var page = Math.Max(1, grid.DisplayedRowCount(false));
        switch (key & Keys.KeyCode)
        {
            case Keys.Home: current = 0; break;
            case Keys.End: current = count - 1; break;
            case Keys.Up: current = Math.Max(0, current <= 0 ? 0 : current - 1); break;
            case Keys.Down: current = Math.Min(count - 1, current < 0 ? 0 : current + 1); break;
            case Keys.PageUp: current = Math.Max(0, current < 0 ? 0 : current - page); break;
            case Keys.PageDown: current = Math.Min(count - 1, current < 0 ? 0 : current + page); break;
            default: return;
        }

        grid.ClearSelection();
        grid.Rows[current].Selected = true;
        grid.CurrentCell = FindFirstVisibleCell(grid.Rows[current]);
        var sourceItem = _dropDownContent.GetSourceItem(current);
        SetHighlightedSourceItem(sourceItem);
        _dropDownContent.UpdateStatus(current + 1, count, false, MinimumSearchLength);
    }

    private int FindHighlightedRowIndex()
    {
        var currentRow = ResultsGrid.CurrentCell?.RowIndex ?? -1;
        if (currentRow >= 0 && currentRow < ResultsGrid.Rows.Count)
        {
            var currentItem = _dropDownContent.GetSourceItem(currentRow);
            if (currentItem is not null && ReferenceEquals(currentItem.Item, _highlightedItem)) return currentRow;
        }
        for (var index = 0; index < ResultsGrid.Rows.Count; index++)
        {
            var sourceItem = _dropDownContent.GetSourceItem(index);
            if (sourceItem is not null && ReferenceEquals(sourceItem.Item, _highlightedItem)) return index;
        }
        for (var index = 0; index < ResultsGrid.Rows.Count; index++)
        {
            var sourceItem = _dropDownContent.GetSourceItem(index);
            if (sourceItem is not null && IsHighlightedSourceItem(sourceItem))
                return index;
        }
        return -1;
    }

    private bool HandleEnterKey()
    {
        FlushPendingSearch();
        if (!IsDropDownOpen && ClosedEnterKeyBehavior == BootstrapLookupClosedEnterKeyBehavior.DataGridViewDefault)
            return false;

        BootstrapLookupCommitResult resolution;
        if (IsDropDownOpen && HighlightedItem is not null && _dataAdapter is not null && _dataAdapter.TryFindByItem(HighlightedItem, out var highlighted))
        {
            resolution = TryCommitResult(highlighted!, BootstrapLookupCommitReason.Keyboard);
        }
        else
        {
            resolution = ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
        }

        if (!resolution.NavigationAllowed)
        {
            OpenDropDown();
            return true;
        }

        CloseDropDown();
        if (EnterKeyBehavior == BootstrapLookupEnterKeyBehavior.CommitSelectionAndMoveNext)
            ContinueOwnerNavigation(false);
        return true;
    }

    private protected virtual bool ContinueOwnerNavigation(bool reverse) => ContinueDialogNavigation(reverse);

    private bool ContinueDialogNavigation(bool reverse)
    {
        Control current = this;
        var container = Parent;
        while (container is not null)
        {
            if (container.SelectNextControl(current, !reverse, true, true, false)) return true;
            current = container;
            container = container.Parent;
        }
        return false;
    }

    private static bool IsNavigationKey(Keys key) => key == Keys.Up || key == Keys.Down || key == Keys.Home ||
        key == Keys.End || key == Keys.PageUp || key == Keys.PageDown;

    private static void Consume(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}
