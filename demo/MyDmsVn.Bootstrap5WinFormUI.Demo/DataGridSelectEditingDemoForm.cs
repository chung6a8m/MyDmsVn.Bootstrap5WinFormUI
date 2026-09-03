using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class DataGridSelectEditingDemoForm : Form
{
    private readonly BootstrapDataGridView _grid = new BootstrapDataGridView();
    private readonly Label _instructions = new Label();
    private readonly Label _status = new Label();
    private readonly BindingList<OrderLine> _lines = new BindingList<OrderLine>();
    private readonly BindingList<ProductOption> _products = new BindingList<ProductOption>();
    private readonly BindingList<string> _units = new BindingList<string> { "Gói 500 g", "Hộp 20 túi", "Chai 500 ml", "Hũ 350 g" };

    public DataGridSelectEditingDemoForm()
    {
        Text = "BootstrapDataGridView + BootstrapLookup Editing Demo";
        StartPosition = FormStartPosition.CenterParent; ClientSize = new Size(980, 620); MinimumSize = new Size(760, 460); AutoScaleMode = AutoScaleMode.Dpi;
        SeedProducts(); ConfigureText(); ConfigureGrid(); CreateSampleRows();
        Controls.Add(_grid); Controls.Add(_status); Controls.Add(_instructions);
        BootstrapThemeManager.ThemeChanged += OnThemeChanged; ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private void SeedProducts()
    {
        _products.Add(new ProductOption(1, "Cà phê rang xay Arabica", "CF-A", "Gói 500 g", 185000m));
        _products.Add(new ProductOption(2, "Trà ô long cao sơn", "TRA-O", "Hộp 20 túi", 128000m));
        _products.Add(new ProductOption(3, "Mật ong hoa cà phê", "MO-CF", "Chai 500 ml", 215000m));
        _products.Add(new ProductOption(4, "Hạt điều rang muối", "HD-RM", "Hũ 350 g", 149000m));
    }

    private void ConfigureText()
    {
        _instructions.Dock = DockStyle.Top; _instructions.Height = 58; _instructions.Padding = new Padding(12, 8, 12, 6); _instructions.TextAlign = ContentAlignment.MiddleLeft;
        _instructions.Text = "Edit Tên hàng with BootstrapLookupColumn. Search Vietnamese names/codes, use Refresh/Add New, then Tab through the typed order row.";
        _status.Dock = DockStyle.Bottom; _status.Height = 38; _status.Padding = new Padding(12, 4, 12, 4); _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = "Tên hàng displays Product.Name while storing raw ProductId.";
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill; _grid.AutoGenerateColumns = false; _grid.AllowUserToAddRows = true; _grid.AllowUserToDeleteRows = true;
        _grid.AllowUserToOrderColumns = false; _grid.SelectionMode = DataGridViewSelectionMode.CellSelect; _grid.MultiSelect = false;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter; _grid.RowHeadersVisible = false; _grid.RowTemplate.Height = 36; _grid.EmptyStateText = "No order lines.";

        var product = new BootstrapLookupColumn
        {
            Name = "ProductColumn", HeaderText = "Tên hàng", DataPropertyName = "ProductId", DataSource = _products,
            DisplayMember = "Name", ValueMember = "Id", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 260,
            DropDownWidth = 520, ShowRefreshButton = true, ShowAddNewButton = true
        };
        product.SearchMembers.Add("Name"); product.SearchMembers.Add("Code");
        product.LookupColumns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = "Code", HeaderText = "Mã", Width = 90 });
        product.LookupColumns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name", HeaderText = "Tên hàng",
            Width = 250, MinimumWidth = 220, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        product.LookupColumns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = "Unit", HeaderText = "Đơn vị", Width = 110 });
        product.SelectionCommitted += OnProductCommitted;
        product.RefreshRequested += (_, _) => _status.Text = "Product list refreshed in memory.";
        product.AddNewRequested += OnProductAddNewRequested;
        _grid.Columns.Add(product);

        var unit = new BootstrapLookupColumn
        {
            Name = "UnitColumn", HeaderText = "Đơn vị tính", DataPropertyName = "Unit", DataSource = _units,
            UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd, Width = 140
        };
        unit.LookupColumns.Add(new BootstrapLookupColumnDefinition { HeaderText = "Đơn vị", Width = 160 });
        _grid.Columns.Add(unit);
        _grid.Columns.Add(TextColumn("QuantityColumn", "Số lượng", "Quantity", 105, "N2"));
        _grid.Columns.Add(TextColumn("UnitPriceColumn", "Đơn giá", "UnitPrice", 140, "N0"));
        var total = TextColumn("LineTotalColumn", "Thành tiền", "LineTotal", 150, "N0"); total.ReadOnly = true; _grid.Columns.Add(total);
        _grid.CellValueChanged += (_, e) => { if (e.RowIndex >= 0 && (e.ColumnIndex == 2 || e.ColumnIndex == 3)) Recalculate(e.RowIndex); };
        _grid.DataError += (_, e) => { e.ThrowException = false; _status.Text = "Invalid edit value: " + (e.Exception?.Message ?? "Unknown data error."); };
        _grid.DataSource = _lines;
    }

    private void OnProductCommitted(object? sender, BootstrapLookupCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count || e.Item is not ProductOption product || _grid.Rows[e.RowIndex].DataBoundItem is not OrderLine line) return;
        line.ProductId = product.Id; line.ProductName = product.Name; line.Unit = product.Unit; line.UnitPrice = product.UnitPrice;
        if (line.Quantity <= 0) line.Quantity = 1; line.Recalculate(); _grid.InvalidateRow(e.RowIndex);
        _status.Text = "Selected: " + product.Name + " — " + product.Unit + " — " + product.UnitPrice.ToString("N0");
    }

    private void OnProductAddNewRequested(object? sender, BootstrapLookupCellEventArgs e)
    {
        var item = new ProductOption(_products.Count + 1, string.IsNullOrWhiteSpace(e.QueryText) ? "Sản phẩm mới" : e.QueryText.Trim(), "NEW", "Cái", 100000m);
        _products.Add(item); e.NewItem = item; _status.Text = "Added: " + item.Name;
    }

    private void CreateSampleRows() { Add(_products[0], 2); Add(_products[1], 5); Add(_products[2], 1); Add(_products[3], 3); }
    private void Add(ProductOption p, decimal quantity) => _lines.Add(new OrderLine { ProductId = p.Id, ProductName = p.Name, Unit = p.Unit, Quantity = quantity, UnitPrice = p.UnitPrice });
    private void Recalculate(int row) { if (row < _grid.Rows.Count && _grid.Rows[row].DataBoundItem is OrderLine line) line.Recalculate(); }
    private static DataGridViewTextBoxColumn TextColumn(string name, string header, string member, int width, string format) => new DataGridViewTextBoxColumn
    { Name = name, HeaderText = header, DataPropertyName = member, Width = width, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = format, NullValue = "0" } };
    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e) => ApplyTheme(e.NewTheme);
    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body; ForeColor = theme.Colors.Text; _instructions.BackColor = theme.Colors.Body;
        _instructions.ForeColor = theme.Colors.MutedText; _status.BackColor = theme.Colors.SurfaceSecondary; _status.ForeColor = theme.Colors.Text;
    }

    private sealed class ProductOption
    {
        internal ProductOption(int id, string name, string code, string unit, decimal unitPrice) { Id = id; Name = name; Code = code; Unit = unit; UnitPrice = unitPrice; }
        public int Id { get; } public string Name { get; } public string Code { get; } public string Unit { get; } public decimal UnitPrice { get; }
    }

    private sealed class OrderLine : INotifyPropertyChanged
    {
        private int _productId; private string _productName = string.Empty; private string _unit = string.Empty; private decimal _quantity = 1; private decimal _unitPrice; private decimal _lineTotal;
        public int ProductId { get => _productId; set => Set(ref _productId, value, nameof(ProductId)); }
        public string ProductName { get => _productName; set => Set(ref _productName, value, nameof(ProductName)); }
        public string Unit { get => _unit; set => Set(ref _unit, value, nameof(Unit)); }
        public decimal Quantity { get => _quantity; set { if (Set(ref _quantity, value, nameof(Quantity))) Recalculate(); } }
        public decimal UnitPrice { get => _unitPrice; set { if (Set(ref _unitPrice, value, nameof(UnitPrice))) Recalculate(); } }
        public decimal LineTotal { get => _lineTotal; private set => Set(ref _lineTotal, value, nameof(LineTotal)); }
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Recalculate() => LineTotal = Quantity * UnitPrice;
        private bool Set<T>(ref T field, T value, string name) { if (Equals(field, value)) return false; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true; }
    }
}
