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
        FlushPendingSearch();
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
        var definitions = Columns;
        if (definitions.Count == 0)
        {
            definitions = new BootstrapLookupColumnDefinitionCollection
            {
                new BootstrapLookupColumnDefinition { DataPropertyName = DisplayMember, HeaderText = DisplayMember }
            };
        }
        _dropDownContent.ApplyColumns(definitions, ShowColumnHeaders);
        IReadOnlyList<BootstrapLookupSourceItem> items = _currentSearchResult.Items;
        _dropDownContent.ApplyResults(items);
        var position = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i].Item, HighlightedItem) || Equals(items[i].Item, HighlightedItem)) { position = i + 1; break; }
        }
        _dropDownContent.ConfigureFooter(ShowRefreshButton, ShowAddNewButton);
        _dropDownContent.UpdateStatus(position, items.Count, _currentSearchResult.State == BootstrapLookupSearchState.WaitingForMinimumLength, MinimumSearchLength);
    }

    internal void RequestExplicitAddNew()
    {
        var args = new BootstrapLookupAddNewRequestedEventArgs(Text);
        CloseDropDown();
        RaiseAddNewRequested(args);
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
        ApplyCurrentResultsToContent();
    }

    internal void FocusLookupEditor()
    {
        if (!IsDisposed && Enabled) Editor.Focus();
    }
}
