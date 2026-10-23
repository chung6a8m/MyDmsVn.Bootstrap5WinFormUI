using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapDecimalEditingControl : BootstrapDecimalInput, IDataGridViewEditingControl
{
    private bool _configuring;
    private DataGridViewCell? _cell;
    private DataGridView? _host;
    private DecimalInputProfile? _sessionProfile;

    public BootstrapDecimalEditingControl() => PendingTextChanged += OnPendingTextChanged;

    public DataGridView? EditingControlDataGridView { get; set; }
    public object EditingControlFormattedValue { get => PendingText; set { if (!_configuring && _cell is not null) ProjectCurrentModel(); } }
    public int EditingControlRowIndex { get; set; }
    public bool EditingControlValueChanged { get; set; }
    public Cursor EditingPanelCursor => Cursors.IBeam;
    public bool RepositionEditingControlOnValueChange => false;
    internal DataGridViewCell? EditingCell => _cell;
    internal DecimalInputProfile SessionProfile => _sessionProfile ?? Profile;

    internal void Configure(BootstrapDecimalColumn column, DataGridViewCell cell, int rowIndex, object? value)
    {
        _configuring = true;
        try
        {
            _cell = cell;
            _sessionProfile = column.Profile;
            EditingControlRowIndex = rowIndex;
            BeginEditSession(value is DBNull ? null : (decimal?)value, _sessionProfile);
            ValidationState = BootstrapValidationState.None;
            EditingControlValueChanged = false;
            if (!ReferenceEquals(_host, EditingControlDataGridView))
            {
                if (_host is not null) _host.CellEndEdit -= OnCellEndEdit;
                _host = EditingControlDataGridView;
                if (_host is not null) _host.CellEndEdit += OnCellEndEdit;
            }
        }
        finally { _configuring = false; }
    }

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) => PendingText;

    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
    {
        Font = style.Font;
        ForeColor = style.ForeColor;
        BackColor = style.BackColor;
    }

    public void PrepareEditingControlForEdit(bool selectAll)
    {
        if (selectAll) Editor.SelectAll();
        else Editor.Select(Editor.TextLength, 0);
    }

    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
    {
        switch (keyData & Keys.KeyCode)
        {
            case Keys.Left: return Editor.SelectionLength > 0 || Editor.SelectionStart > 0;
            case Keys.Right: return Editor.SelectionLength > 0 || Editor.SelectionStart < Editor.TextLength;
            case Keys.Home:
            case Keys.End: return Editor.SelectionLength != Editor.TextLength;
            case Keys.Delete: return Editor.SelectionLength > 0 || Editor.SelectionStart < Editor.TextLength;
            default: return !dataGridViewWantsInputKey;
        }
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if ((keyData & Keys.KeyCode) == Keys.Tab && (keyData & (Keys.Alt | Keys.Control)) == Keys.None)
            return ProcessDialogKey(keyData);
        return base.ProcessCmdKey(ref message, keyData);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if ((keyData & Keys.KeyCode) != Keys.Tab || (keyData & (Keys.Alt | Keys.Control)) != Keys.None)
            return base.ProcessDialogKey(keyData);
        var grid = EditingControlDataGridView;
        var current = _cell;
        if (grid is null || current is null || !ReferenceEquals(grid.CurrentCell, current))
            return base.ProcessDialogKey(keyData);
        if (!grid.EndEdit()) return true;
        var next = FindNextEditableCell(grid, current, (keyData & Keys.Shift) == Keys.Shift);
        if (next is null) return base.ProcessDialogKey(keyData);
        grid.CurrentCell = next;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PendingTextChanged -= OnPendingTextChanged;
            if (_host is not null) _host.CellEndEdit -= OnCellEndEdit;
            _host = null;
            _cell = null;
            _sessionProfile = null;
        }
        base.Dispose(disposing);
    }

    private void OnPendingTextChanged(object? sender, EventArgs e)
    {
        if (_configuring || _cell is null || EditingControlDataGridView?.CurrentCell != _cell) return;
        EditingControlValueChanged = true;
        EditingControlDataGridView.NotifyCurrentCellDirty(true);
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_cell is null || _cell.RowIndex != e.RowIndex || _cell.ColumnIndex != e.ColumnIndex) return;
        ProjectCurrentModel();
    }

    internal void ProjectCurrentModel()
    {
        if (_cell is null) return;
        var current = CurrentModelValue(_cell);
        if (current is decimal value) ProjectAcceptedValue(value);
        else if (current is null || current is DBNull) ProjectAcceptedValue(null);
    }

    private static DataGridViewCell? FindNextEditableCell(DataGridView grid, DataGridViewCell current, bool reverse)
    {
        var row = current.RowIndex;
        var column = current.ColumnIndex;
        while (row >= 0 && row < grid.Rows.Count)
        {
            column += reverse ? -1 : 1;
            if (column < 0 || column >= grid.Columns.Count)
            {
                row += reverse ? -1 : 1;
                column = reverse ? grid.Columns.Count - 1 : 0;
                continue;
            }
            var candidate = grid.Rows[row].Cells[column];
            if (candidate.Visible && !candidate.ReadOnly) return candidate;
        }
        return null;
    }

    private static object? CurrentModelValue(DataGridViewCell cell)
    {
        var item = cell.OwningRow?.DataBoundItem;
        var name = cell.OwningColumn?.DataPropertyName;
        if (item is not null && !string.IsNullOrEmpty(name))
        {
            var property = TypeDescriptor.GetProperties(item)[name!];
            if (property is not null) return property.GetValue(item);
        }
        return cell.Value;
    }
}
