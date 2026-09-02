using System;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
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
}
