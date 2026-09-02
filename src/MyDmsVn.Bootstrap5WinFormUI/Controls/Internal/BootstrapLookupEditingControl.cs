using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl
{
    private BootstrapLookupColumn? _column;
    private bool _configuring;
    private object? _editingValue;

    public BootstrapLookupEditingControl()
    {
        SelectionCommitted += OnLookupSelectionCommitted;
        RefreshRequested += OnLookupRefreshRequested;
        AddNewRequested += OnLookupAddNewRequested;
        CreateItemFromText += OnLookupCreateItemFromText;
    }

    public DataGridView? EditingControlDataGridView { get; set; }
    public object EditingControlFormattedValue { get => SelectedValue!; set => SelectedValue = value; }
    public int EditingControlRowIndex { get; set; }
    public bool EditingControlValueChanged { get; set; }
    public Cursor EditingPanelCursor => Cursors.IBeam;
    public bool RepositionEditingControlOnValueChange => false;

    internal void Configure(BootstrapLookupColumn column, int rowIndex, int columnIndex, object? rawValue)
    {
        CancelPendingEdit();
        DataSource = null;
        _column = null;
        _configuring = true;
        try
        {
            Columns.Clear(); SearchMembers.Clear();
            DisplayMember = column.DisplayMember; ValueMember = column.ValueMember; DataSource = column.DataSource;
            foreach (var definition in column.LookupColumns) Columns.Add(BootstrapLookupColumn.CloneDefinition(definition));
            foreach (var member in column.SearchMembers) SearchMembers.Add(member);
            UnmatchedTextBehavior = column.UnmatchedTextBehavior; EmptyQueryBehavior = column.EmptyQueryBehavior;
            TypingPopupBehavior = column.TypingPopupBehavior; EnterKeyBehavior = column.EnterKeyBehavior;
            ClosedEnterKeyBehavior = column.ClosedEnterKeyBehavior; SearchDebounceMilliseconds = column.SearchDebounceMilliseconds;
            MinimumSearchLength = column.MinimumSearchLength; DropDownWidth = column.DropDownWidth; MaxDropDownHeight = column.MaxDropDownHeight;
            ShowColumnHeaders = column.ShowColumnHeaders; ShowRefreshButton = column.ShowRefreshButton; ShowAddNewButton = column.ShowAddNewButton;
            SearchTextNormalizer = column.SearchTextNormalizer; TextNormalizer = column.TextNormalizer; TextComparer = column.TextComparer;
            InvalidTextMessage = column.InvalidTextMessage;
            SelectedValue = rawValue;
            EditingControlRowIndex = rowIndex;
            EditingControlValueChanged = false;
            _editingValue = rawValue;
            _column = column;
        }
        finally { _configuring = false; }
    }

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) => SelectedValue!;
    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
    {
        Font = dataGridViewCellStyle.Font;
        ForeColor = dataGridViewCellStyle.ForeColor;
        BackColor = dataGridViewCellStyle.BackColor;
    }
    public void PrepareEditingControlForEdit(bool selectAll) { if (selectAll) SelectAll(); }
    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
    {
        var key = keyData & Keys.KeyCode;
        return key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End || key == Keys.PageUp ||
            key == Keys.PageDown || key == Keys.F4 || key == Keys.Escape ||
            (key == Keys.Enter && (IsDropDownOpen || ClosedEnterKeyBehavior != BootstrapLookupClosedEnterKeyBehavior.DataGridViewDefault));
    }

    private void OnLookupSelectionCommitted(object? sender, BootstrapLookupSelectionCommittedEventArgs e)
    {
        if (_configuring || _column is null) return;
        if (!EqualityComparer<object?>.Default.Equals(_editingValue, e.Value))
        {
            _editingValue = e.Value;
            EditingControlValueChanged = true;
            EditingControlDataGridView?.NotifyCurrentCellDirty(true);
        }
        var args = Context();
        args.Item = e.Item; args.Value = e.Value; args.DisplayText = e.DisplayText; args.Reason = e.Reason;
        _column.RaiseSelectionCommitted(args);
    }

    private void OnLookupRefreshRequested(object? sender, BootstrapLookupRefreshRequestedEventArgs e)
    {
        if (_configuring || _column is null) return;
        var args = Context(); args.QueryText = e.QueryText;
        _column.RaiseRefreshRequested(args);
    }

    private void OnLookupAddNewRequested(object? sender, BootstrapLookupAddNewRequestedEventArgs e)
    {
        if (_configuring || _column is null) return;
        var args = Context(); args.QueryText = e.QueryText;
        _column.RaiseAddNewRequested(args);
        e.NewItem = args.NewItem; e.Cancel = args.Cancel;
    }

    private void OnLookupCreateItemFromText(object? sender, BootstrapLookupCreateItemFromTextEventArgs e)
    {
        if (_configuring || _column is null) return;
        var args = Context(); args.OriginalText = e.OriginalText; args.NormalizedText = e.NormalizedText;
        _column.RaiseCreateItemFromText(args);
        e.Item = args.Item; e.Cancel = args.Cancel;
    }

    private BootstrapLookupCellEventArgs Context()
    {
        var grid = EditingControlDataGridView ?? throw new InvalidOperationException("The lookup editor is not attached to a DataGridView.");
        return new BootstrapLookupCellEventArgs(grid, EditingControlRowIndex, grid.CurrentCell?.ColumnIndex ?? -1);
    }
}
