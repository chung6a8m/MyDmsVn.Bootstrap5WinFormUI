using System;
using System.Collections.Generic;
using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapLookupBox
{
    /// <summary>Gets whether the lookup popup is currently open.</summary>
    [Browsable(false)]
    public bool IsDropDownOpen => _dropDownController.IsOpen;

    /// <summary>Opens the lookup result popup without committing pending text.</summary>
    public void OpenDropDown()
    {
        if (!Enabled || ReadOnly) return;
        FlushPendingSearch();
        ApplyCurrentPresentationToContent(_dropDownController.EffectiveDpi);
        _dropDownController.Open();
    }

    /// <summary>Closes only popup presentation without resolving pending text.</summary>
    public void CloseDropDown() => _dropDownController.Close(false);

    /// <summary>Raises RefreshRequested, reconciles the source, and reapplies the active query.</summary>
    public void RefreshResults()
    {
        RaiseRefreshRequested(new BootstrapLookupRefreshRequestedEventArgs(Text));
        _dataAdapter?.Refresh();
        ReconcileCommittedSelection();
        ExecuteSearchNow();
        if (IsDropDownOpen) _dropDownController.Reposition();
    }

    internal void ApplyCurrentResultsToContent()
    {
        ApplyCurrentContent(includeResults: true, _dropDownController.EffectiveDpi);
    }

    internal void ApplyCurrentPresentationToContent(int dpi)
    {
        ApplyCurrentContent(includeResults: false, dpi);
    }

    private void ApplyCurrentContent(bool includeResults, int dpi)
    {
        var definitions = Columns;
        if (definitions.Count == 0)
        {
            definitions = new BootstrapLookupColumnDefinitionCollection
            {
                new BootstrapLookupColumnDefinition { DataPropertyName = DisplayMember, HeaderText = DisplayMember }
            };
        }
        var columnsChanged = _dropDownContent.ApplyColumns(definitions, ShowColumnHeaders, dpi);
        IReadOnlyList<BootstrapLookupSourceItem> items = _currentSearchResult.Items;
        if (includeResults || columnsChanged) _dropDownContent.ApplyResults(items);
        var position = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (IsHighlightedSourceItem(items[i])) { position = i + 1; break; }
        }
        SynchronizeHighlightedResult();
        _dropDownContent.ConfigureFooter(ShowRefreshButton, ShowAddNewButton);
        _dropDownContent.UpdateStatus(position, items.Count, _currentSearchResult.State == BootstrapLookupSearchState.WaitingForMinimumLength, MinimumSearchLength);
    }

    internal void SynchronizeHighlightedResult()
    {
        var rowIndex = FindPhysicalHighlightedRowIndex();
        if (rowIndex < 0) rowIndex = FindLogicalHighlightedRowIndex();
        ResultsGrid.ClearSelection();
        if (rowIndex < 0)
        {
            ResultsGrid.CurrentCell = null;
            return;
        }
        var row = ResultsGrid.Rows[rowIndex];
        row.Selected = true;
        ResultsGrid.CurrentCell = FindFirstVisibleCell(row);
        if (ResultsGrid.DisplayedRowCount(false) > 0)
            ResultsGrid.FirstDisplayedScrollingRowIndex = rowIndex;
    }

    private int FindPhysicalHighlightedRowIndex()
    {
        for (var index = 0; index < ResultsGrid.Rows.Count; index++)
        {
            var sourceItem = _dropDownContent.GetSourceItem(index);
            if (sourceItem is not null && ReferenceEquals(sourceItem.Item, _highlightedItem)) return index;
        }
        return -1;
    }

    private int FindLogicalHighlightedRowIndex()
    {
        for (var index = 0; index < ResultsGrid.Rows.Count; index++)
        {
            var sourceItem = _dropDownContent.GetSourceItem(index);
            if (sourceItem is not null && IsHighlightedSourceItem(sourceItem)) return index;
        }
        return -1;
    }

    private static System.Windows.Forms.DataGridViewCell? FindFirstVisibleCell(System.Windows.Forms.DataGridViewRow row)
    {
        foreach (System.Windows.Forms.DataGridViewCell cell in row.Cells)
        {
            if (cell.Visible) return cell;
        }
        return null;
    }

    internal void RequestExplicitAddNew()
    {
        if (!Enabled || ReadOnly) return;
        var args = new BootstrapLookupAddNewRequestedEventArgs(Text);
        _suspendedLeaveResolutionCount++;
        _leaveResolutionGeneration++;
        try
        {
            CloseDropDown();
            RaiseAddNewRequested(args);
        }
        finally
        {
            _suspendedLeaveResolutionCount--;
            _leaveResolutionGeneration++;
        }
        if (args.Cancel || args.NewItem is null) return;
        object? value;
        string display;
        try
        {
            value = BootstrapLookupMemberAccessor.GetValue(args.NewItem, ValueMember);
            display = BootstrapLookupMemberAccessor.GetValue(args.NewItem, DisplayMember)?.ToString() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return;
        }
        if (value is null && ValueMember.Length > 0) return;
        CommitSelection(args.NewItem, value, display, BootstrapLookupCommitReason.Programmatic);
        _dataAdapter?.Refresh();
        ExecuteSearchNow();
    }

    internal void FocusLookupEditor()
    {
        if (!IsDisposed && Enabled) Editor.Focus();
    }
}
