using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    /// <inheritdoc />
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!Enabled) return base.ProcessCmdKey(ref msg, keyData);
        var key = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (key == Keys.Tab && (modifiers & (Keys.Alt | Keys.Control)) == Keys.None)
            return ProcessDialogKey(keyData);
        if (key == Keys.Escape)
        {
            CancelPendingEdit();
            return true;
        }
        if (key == Keys.F4 || ((modifiers & Keys.Alt) == Keys.Alt && key == Keys.Down) || (!IsDropDownOpen && key == Keys.Down))
        {
            FlushPendingSearch();
            OpenDropDown();
            return true;
        }
        if (IsDropDownOpen && IsNavigationKey(key))
        {
            FlushPendingSearch();
            NavigateResults(key);
            return true;
        }
        if (key == Keys.Enter && HandleEnterKey()) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc />
    protected override void OnEditorKeyDown(KeyEventArgs e)
    {
        if (e.Handled || !Enabled)
        {
            base.OnEditorKeyDown(e);
            return;
        }

        var key = e.KeyCode;
        if (key == Keys.Escape)
        {
            CancelPendingEdit();
            Consume(e);
            return;
        }

        if (key == Keys.F4 || (e.Alt && key == Keys.Down) || (!IsDropDownOpen && key == Keys.Down))
        {
            FlushPendingSearch();
            OpenDropDown();
            Consume(e);
            return;
        }

        if (IsDropDownOpen && IsNavigationKey(key))
        {
            FlushPendingSearch();
            NavigateResults(key);
            Consume(e);
            return;
        }

        if (key == Keys.Enter && HandleEnterKey())
        {
            Consume(e);
            return;
        }

        base.OnEditorKeyDown(e);
    }

    /// <inheritdoc />
    protected override bool ProcessDialogKey(Keys keyData)
    {
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
            SetHighlightedItem(null);
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
        if (grid.Columns.Count > 0) grid.CurrentCell = grid.Rows[current].Cells[0];
        var sourceItem = _dropDownContent.GetSourceItem(current);
        SetHighlightedItem(sourceItem?.Item);
        _dropDownContent.UpdateStatus(current + 1, count, false, MinimumSearchLength);
    }

    private int FindHighlightedRowIndex()
    {
        for (var index = 0; index < ResultsGrid.Rows.Count; index++)
        {
            var sourceItem = _dropDownContent.GetSourceItem(index);
            if (sourceItem is not null && (ReferenceEquals(sourceItem.Item, HighlightedItem) || Equals(sourceItem.Item, HighlightedItem)))
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
            CommitSelection(highlighted!.Item, highlighted.Value, highlighted.DisplayText, BootstrapLookupCommitReason.Keyboard);
            resolution = BootstrapLookupCommitResult.Success();
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
            ContinueDialogNavigation(false);
        return true;
    }

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
