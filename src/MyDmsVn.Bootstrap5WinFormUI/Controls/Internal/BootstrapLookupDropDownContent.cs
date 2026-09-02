using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupDropDownContent : UserControl
{
    private readonly BootstrapLookupFooter _footer = new BootstrapLookupFooter();
    private string _columnSignature = string.Empty;

    internal BootstrapLookupDropDownContent()
    {
        TabStop = false;
        ResultsGrid = new BootstrapDataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            TabStop = false,
            AutoGenerateColumns = false
        };
        Controls.Add(ResultsGrid);
        Controls.Add(_footer);
        _footer.BringToFront();
    }

    internal BootstrapDataGridView ResultsGrid { get; }
    internal event EventHandler? RefreshRequested { add => _footer.RefreshRequested += value; remove => _footer.RefreshRequested -= value; }
    internal event EventHandler? AddNewRequested { add => _footer.AddNewRequested += value; remove => _footer.AddNewRequested -= value; }

    internal void ApplyColumns(BootstrapLookupColumnDefinitionCollection definitions, bool showHeaders)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        ReassertInvariants();
        ResultsGrid.ColumnHeadersVisible = showHeaders;
        var signature = BuildSignature(definitions);
        if (string.Equals(signature, _columnSignature, StringComparison.Ordinal)) return;
        _columnSignature = signature;
        ResultsGrid.Columns.Clear();
        foreach (var definition in definitions)
        {
            ResultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = definition.DataPropertyName,
                HeaderText = definition.HeaderText,
                Width = definition.Width,
                MinimumWidth = definition.MinimumWidth,
                Visible = definition.Visible,
                AutoSizeMode = definition.AutoSizeMode,
                ValueType = definition.ValueType,
                DefaultCellStyle = { Alignment = definition.Alignment, Format = definition.Format },
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            });
        }
    }

    internal void ApplyResults(IReadOnlyList<BootstrapLookupSourceItem> items)
    {
        ReassertInvariants();
        ResultsGrid.Rows.Clear();
        foreach (var item in items)
        {
            var rowIndex = ResultsGrid.Rows.Add();
            var row = ResultsGrid.Rows[rowIndex];
            row.Tag = new BootstrapLookupResultBindingItem(item);
            for (var columnIndex = 0; columnIndex < ResultsGrid.Columns.Count; columnIndex++)
            {
                var member = ResultsGrid.Columns[columnIndex].Name;
                row.Cells[columnIndex].Value = string.IsNullOrEmpty(member)
                    ? item.Item.ToString() ?? string.Empty
                    : BootstrapLookupMemberAccessor.GetValue(item.Item, member);
            }
        }
    }

    internal void ConfigureFooter(bool showRefresh, bool showAddNew) => _footer.Configure(showRefresh, showAddNew);
    internal void UpdateStatus(int position, int total, bool waiting, int minimumLength) => _footer.UpdateStatus(position, total, waiting, minimumLength);

    internal BootstrapLookupSourceItem? GetSourceItem(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= ResultsGrid.Rows.Count) return null;
        return (ResultsGrid.Rows[rowIndex].Tag as BootstrapLookupResultBindingItem)?.SourceItem;
    }

    internal void ReassertInvariants()
    {
        ResultsGrid.ReadOnly = true;
        ResultsGrid.MultiSelect = false;
        ResultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ResultsGrid.AllowUserToAddRows = false;
        ResultsGrid.AllowUserToDeleteRows = false;
        ResultsGrid.RowHeadersVisible = false;
        ResultsGrid.TabStop = false;
        ResultsGrid.DataSource = null;
    }

    private static string BuildSignature(IEnumerable<BootstrapLookupColumnDefinition> definitions)
    {
        var builder = new StringBuilder();
        foreach (var definition in definitions)
        {
            builder.Append(definition.DataPropertyName).Append('\0').Append(definition.HeaderText).Append('\0')
                .Append(definition.Width).Append('\0').Append(definition.MinimumWidth).Append('\0')
                .Append(definition.Visible).Append('\0').Append((int)definition.AutoSizeMode).Append('\0')
                .Append((int)definition.Alignment).Append('\0').Append(definition.Format).Append('\0')
                .Append(definition.ValueType?.AssemblyQualifiedName).Append('\u0001');
        }
        return builder.ToString();
    }
}
