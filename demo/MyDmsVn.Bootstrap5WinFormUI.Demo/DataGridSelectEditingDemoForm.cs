using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class DataGridSelectEditingDemoForm : Form
{
    private const string ProductNameColumnName = "ProductNameColumn";
    private const string UnitColumnName = "UnitColumn";
    private const string QuantityColumnName = "QuantityColumn";
    private const string UnitPriceColumnName = "UnitPriceColumn";
    private const string LineTotalColumnName = "LineTotalColumn";

    private readonly BootstrapDataGridView _grid = new BootstrapDataGridView();
    private readonly BootstrapSelect _productEditor = new BootstrapSelect();
    private readonly Label _instructions = new Label();
    private readonly Label _status = new Label();
    private readonly DataTable _lines = new DataTable("OrderLines");
    private readonly ProductOption[] _products =
    {
        new ProductOption(1, "Cà phê rang xay Arabica", "Gói 500 g", 185000m),
        new ProductOption(2, "Trà ô long cao sơn", "Hộp 20 túi", 128000m),
        new ProductOption(3, "Mật ong hoa cà phê", "Chai 500 ml", 215000m),
        new ProductOption(4, "Hạt điều rang muối", "Hũ 350 g", 149000m),
        new ProductOption(5, "Bánh quy bơ thủ công", "Hộp 300 g", 96000m)
    };

    private DataGridViewTextBoxEditingControl? _nativeProductEditingControl;
    private int _editingProductRowIndex = -1;
    private bool _syncingProductEditor;
    private bool _recalculatingLineTotal;

    public DataGridSelectEditingDemoForm()
    {
        Text = "BootstrapDataGridView + BootstrapSelect Editing Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 620);
        MinimumSize = new Size(760, 460);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureInstructions();
        ConfigureGrid();
        ConfigureProductEditor();
        ConfigureStatus();
        CreateSampleRows();

        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(_instructions);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _grid.EditingControlShowing -= OnEditingControlShowing;
            _grid.CellEndEdit -= OnCellEndEdit;
            _grid.CellValueChanged -= OnCellValueChanged;
            _grid.DataError -= OnDataError;
            _productEditor.SelectionChanged -= OnProductSelectionChanged;
            _productEditor.Dispose();
            _lines.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ConfigureInstructions()
    {
        _instructions.Dock = DockStyle.Top;
        _instructions.Height = 58;
        _instructions.Padding = new Padding(12, 8, 12, 6);
        _instructions.TextAlign = ContentAlignment.MiddleLeft;
        _instructions.Text =
            "Edit Tên hàng to use BootstrapSelect. The column remains a normal DataGridViewTextBoxColumn; " +
            "the demo swaps the visible editor from EditingControlShowing. Quantity and unit price recalculate Thành tiền.";
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.MultiSelect = false;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 36;
        _grid.EmptyStateText = "No order lines.";

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = ProductNameColumnName,
            HeaderText = "Tên hàng",
            DataPropertyName = "ProductName",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 260
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = UnitColumnName,
            HeaderText = "Đơn vị tính",
            DataPropertyName = "Unit",
            Width = 130
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = QuantityColumnName,
            HeaderText = "Số lượng",
            DataPropertyName = "Quantity",
            Width = 105,
            DefaultCellStyle = CreateNumberStyle("N2")
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = UnitPriceColumnName,
            HeaderText = "Đơn giá",
            DataPropertyName = "UnitPrice",
            Width = 140,
            DefaultCellStyle = CreateNumberStyle("N0")
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = LineTotalColumnName,
            HeaderText = "Thành tiền",
            DataPropertyName = "LineTotal",
            Width = 150,
            ReadOnly = true,
            DefaultCellStyle = CreateNumberStyle("N0")
        });

        _grid.EditingControlShowing += OnEditingControlShowing;
        _grid.CellEndEdit += OnCellEndEdit;
        _grid.CellValueChanged += OnCellValueChanged;
        _grid.DataError += OnDataError;

        _lines.Columns.Add("ProductId", typeof(int));
        _lines.Columns.Add("ProductName", typeof(string));
        _lines.Columns.Add("Unit", typeof(string));
        _lines.Columns.Add("Quantity", typeof(decimal));
        _lines.Columns.Add("UnitPrice", typeof(decimal));
        _lines.Columns.Add("LineTotal", typeof(decimal));
        _grid.DataSource = _lines;
    }

    private void ConfigureProductEditor()
    {
        _productEditor.Visible = false;
        _productEditor.Margin = Padding.Empty;
        _productEditor.Placeholder = "Chọn hàng hóa...";
        _productEditor.SelectionMode = BootstrapSelectMode.Single;
        _productEditor.SearchEnabled = true;
        _productEditor.AllowClear = false;
        _productEditor.DropDownWidth = 360;
        _productEditor.AccessibleName = "Product editor for Tên hàng";

        for (var index = 0; index < _products.Length; index++)
        {
            var product = _products[index];
            _productEditor.Items.Add(new BootstrapSelectItem(product.Id, product.Name)
            {
                Tag = product
            });
        }

        _productEditor.SelectionChanged += OnProductSelectionChanged;
    }

    private void ConfigureStatus()
    {
        _status.Dock = DockStyle.Bottom;
        _status.Height = 38;
        _status.Padding = new Padding(12, 4, 12, 4);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = "Click a cell to edit. Tên hàng uses BootstrapSelect through EditingControlShowing.";
    }

    private void CreateSampleRows()
    {
        AddSampleRow(_products[0], 2m);
        AddSampleRow(_products[1], 5m);
        AddSampleRow(_products[2], 1m);
        AddSampleRow(_products[3], 3m);
    }

    private void AddSampleRow(ProductOption product, decimal quantity)
    {
        _lines.Rows.Add(
            product.Id,
            product.Name,
            product.Unit,
            quantity,
            product.UnitPrice,
            quantity * product.UnitPrice);
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        var currentCell = _grid.CurrentCell;
        if (currentCell is null || currentCell.OwningColumn.Name != ProductNameColumnName)
        {
            HideProductEditor();
            return;
        }

        if (e.Control is not DataGridViewTextBoxEditingControl textEditor || textEditor.Parent is null)
        {
            HideProductEditor();
            return;
        }

        _nativeProductEditingControl = textEditor;
        _editingProductRowIndex = currentCell.RowIndex;

        if (!ReferenceEquals(_productEditor.Parent, textEditor.Parent))
        {
            _productEditor.Parent?.Controls.Remove(_productEditor);
            textEditor.Parent.Controls.Add(_productEditor);
        }

        _syncingProductEditor = true;
        try
        {
            _productEditor.SelectedItem = null;
            var rowView = _grid.Rows[_editingProductRowIndex].DataBoundItem as DataRowView;
            if (rowView is not null && rowView["ProductId"] != DBNull.Value)
            {
                _productEditor.SelectedValue = Convert.ToInt32(rowView["ProductId"]);
            }
        }
        finally
        {
            _syncingProductEditor = false;
        }

        textEditor.Visible = false;
        _productEditor.Dock = DockStyle.Fill;
        _productEditor.Visible = true;
        _productEditor.BringToFront();
        _productEditor.Focus();
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        HideProductEditor();
    }

    private void HideProductEditor()
    {
        _productEditor.Visible = false;
        if (_nativeProductEditingControl is not null && !_nativeProductEditingControl.IsDisposed)
        {
            _nativeProductEditingControl.Visible = true;
        }

        _nativeProductEditingControl = null;
        _editingProductRowIndex = -1;
    }

    private void OnProductSelectionChanged(object? sender, EventArgs e)
    {
        if (_syncingProductEditor || _editingProductRowIndex < 0 || _productEditor.SelectedItem?.Tag is not ProductOption product)
        {
            return;
        }

        if (_editingProductRowIndex >= _grid.Rows.Count)
        {
            return;
        }

        var row = _grid.Rows[_editingProductRowIndex];
        if (row.DataBoundItem is not DataRowView rowView)
        {
            return;
        }

        var quantity = ReadDecimal(rowView["Quantity"]);
        if (quantity <= 0m)
        {
            quantity = 1m;
        }

        rowView["ProductId"] = product.Id;
        rowView["ProductName"] = product.Name;
        rowView["Unit"] = product.Unit;
        rowView["Quantity"] = quantity;
        rowView["UnitPrice"] = product.UnitPrice;
        rowView["LineTotal"] = quantity * product.UnitPrice;

        if (_nativeProductEditingControl is not null && !_nativeProductEditingControl.IsDisposed)
        {
            _nativeProductEditingControl.Text = product.Name;
        }

        _grid.InvalidateRow(row.Index);
        _status.Text = "Selected: " + product.Name + " — " + product.Unit + " — " + product.UnitPrice.ToString("N0");
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_recalculatingLineTotal || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName != QuantityColumnName && columnName != UnitPriceColumnName)
        {
            return;
        }

        RecalculateLineTotal(e.RowIndex);
    }

    private void RecalculateLineTotal(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
        {
            return;
        }

        var row = _grid.Rows[rowIndex];
        if (row.DataBoundItem is not DataRowView rowView)
        {
            return;
        }

        var quantity = ReadDecimal(row.Cells[QuantityColumnName].Value);
        var unitPrice = ReadDecimal(row.Cells[UnitPriceColumnName].Value);
        var total = quantity * unitPrice;

        _recalculatingLineTotal = true;
        try
        {
            rowView["LineTotal"] = total;
            row.Cells[LineTotalColumnName].Value = total;
        }
        finally
        {
            _recalculatingLineTotal = false;
        }
    }

    private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        _status.Text = "Invalid edit value: " + (e.Exception?.Message ?? "Unknown data error.");
    }

    private static decimal ReadDecimal(object? value)
    {
        return value is null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
    }

    private static DataGridViewCellStyle CreateNumberStyle(string format)
    {
        return new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleRight,
            Format = format,
            NullValue = "0"
        };
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _instructions.BackColor = theme.Colors.Body;
        _instructions.ForeColor = theme.Colors.MutedText;
        _status.BackColor = theme.Colors.SurfaceSecondary;
        _status.ForeColor = theme.Colors.Text;
    }

    private sealed class ProductOption
    {
        public ProductOption(int id, string name, string unit, decimal unitPrice)
        {
            Id = id;
            Name = name;
            Unit = unit;
            UnitPrice = unitPrice;
        }

        public int Id { get; }

        public string Name { get; }

        public string Unit { get; }

        public decimal UnitPrice { get; }
    }
}
