using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

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
            Dock = DockStyle.None,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            TabStop = false,
            AutoGenerateColumns = false
        };
        _footer.Dock = DockStyle.None;
        Controls.Add(_footer);
        Controls.Add(ResultsGrid);
        _footer.BringToFront();
    }

    internal BootstrapDataGridView ResultsGrid { get; }
    internal event EventHandler? RefreshRequested { add => _footer.RefreshRequested += value; remove => _footer.RefreshRequested -= value; }
    internal event EventHandler? AddNewRequested { add => _footer.AddNewRequested += value; remove => _footer.AddNewRequested -= value; }

    internal bool ApplyColumns(BootstrapLookupColumnDefinitionCollection definitions, bool showHeaders, int dpi = DpiScaler.DefaultDpi)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        ReassertInvariants();
        ResultsGrid.ColumnHeadersVisible = showHeaders;
        var signature = BuildSignature(definitions, dpi);
        if (string.Equals(signature, _columnSignature, StringComparison.Ordinal)) return false;
        _columnSignature = signature;
        ResultsGrid.Columns.Clear();
        foreach (var definition in definitions)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = definition.DataPropertyName,
                HeaderText = definition.HeaderText,
                MinimumWidth = DpiScaler.Scale(definition.MinimumWidth, dpi),
                Visible = definition.Visible,
                AutoSizeMode = definition.AutoSizeMode,
                ValueType = definition.ValueType,
                DefaultCellStyle = { Alignment = definition.Alignment, Format = definition.Format },
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
            if (definition.AutoSizeMode == DataGridViewAutoSizeColumnMode.None)
                column.Width = DpiScaler.Scale(definition.Width, dpi);
            ResultsGrid.Columns.Add(column);
        }
        return true;
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

    public override System.Drawing.Size GetPreferredSize(System.Drawing.Size proposedSize)
    {
        var width = proposedSize.Width > 0 ? proposedSize.Width : Width;
        var borderHeight = ResultsGrid.BorderStyle == BorderStyle.None
            ? 0
            : ResultsGrid.BorderStyle == BorderStyle.FixedSingle
                ? SystemInformation.BorderSize.Height * 2
                : SystemInformation.Border3DSize.Height * 2;
        var borderWidth = ResultsGrid.BorderStyle == BorderStyle.None
            ? 0
            : ResultsGrid.BorderStyle == BorderStyle.FixedSingle
                ? SystemInformation.BorderSize.Width * 2
                : SystemInformation.Border3DSize.Width * 2;
        var headerHeight = ResultsGrid.ColumnHeadersVisible ? ResultsGrid.ColumnHeadersHeight : 0;
        var fixedHeight = _footer.Height + borderHeight + headerHeight;
        var maximumRowsHeight = proposedSize.Height > 0
            ? Math.Max(0, proposedSize.Height - fixedHeight)
            : int.MaxValue;
        var rowsHeight = AccumulateCappedHeight(
            ResultsGrid.Rows.Cast<DataGridViewRow>().Select(row => row.Height),
            maximumRowsHeight);
        var contentWidth = proposedSize.Width > 0 ? proposedSize.Width : Width;
        var columnsWidth = 0;
        foreach (DataGridViewColumn column in ResultsGrid.Columns)
            if (column.Visible) columnsWidth += column.Width;
        var baseHeight = fixedHeight + rowsHeight;
        var verticalScrollAllowed = ResultsGrid.ScrollBars == ScrollBars.Both || ResultsGrid.ScrollBars == ScrollBars.Vertical;
        var verticalScrollNeeded = verticalScrollAllowed && proposedSize.Height > 0 && baseHeight > proposedSize.Height;
        var horizontalScrollAllowed = ResultsGrid.ScrollBars == ScrollBars.Both || ResultsGrid.ScrollBars == ScrollBars.Horizontal;
        var availableColumnsWidth = contentWidth - borderWidth - (verticalScrollNeeded ? SystemInformation.VerticalScrollBarWidth : 0);
        var horizontalScrollHeight = horizontalScrollAllowed && columnsWidth > Math.Max(0, availableColumnsWidth)
            ? SystemInformation.HorizontalScrollBarHeight
            : 0;
        var desiredHeight = baseHeight + horizontalScrollHeight;
        var height = proposedSize.Height > 0 ? Math.Min(proposedSize.Height, desiredHeight) : desiredHeight;
        return new System.Drawing.Size(width, height);
    }

    internal static int AccumulateCappedHeight(IEnumerable<int> heights, int maximumHeight)
    {
        var total = 0;
        foreach (var height in heights)
        {
            total += height;
            if (total > maximumHeight) break;
        }
        return total;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        var footerHeight = Math.Min(ClientSize.Height, _footer.Height);
        _footer.Bounds = new System.Drawing.Rectangle(0, ClientSize.Height - footerHeight, ClientSize.Width, footerHeight);
        ResultsGrid.Bounds = new System.Drawing.Rectangle(0, 0, ClientSize.Width, Math.Max(0, _footer.Top));
    }

    private static string BuildSignature(IEnumerable<BootstrapLookupColumnDefinition> definitions, int dpi)
    {
        var builder = new StringBuilder();
        builder.Append(dpi).Append('\u0002');
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
