using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class BootstrapSelectDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly GroupBox _localSection = new GroupBox();
    private readonly GroupBox _asyncSection = new GroupBox();
    private readonly Label _singleStatus = new Label();
    private readonly Label _multipleStatus = new Label();
    private readonly Label _asyncSingleStatus = new Label();
    private readonly Label _asyncMultipleStatus = new Label();

    public BootstrapSelectDemoForm()
    {
        Text = "BootstrapSelect Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(980, 820);
        MinimumSize = new Size(760, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        ConfigureContent();
        BuildLocalSection();
        BuildAsyncSection();
        Controls.Add(_content);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(12);

        ConfigureSection(_localSection, "Local Select2-style scenarios");
        ConfigureSection(_asyncSection, "Async provider / paging / retry scenarios");

        _content.Controls.Add(_localSection);
        _content.Controls.Add(_asyncSection);
    }

    private static void ConfigureSection(GroupBox section, string text)
    {
        section.Text = text;
        section.AutoSize = true;
        section.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        section.MinimumSize = new Size(920, 0);
        section.Margin = new Padding(0, 0, 0, 12);
        section.Padding = new Padding(12);
    }

    private void BuildLocalSection()
    {
        var grid = CreateScenarioGrid();

        var single = new BootstrapSelect
        {
            Width = 340,
            Placeholder = "Choose a customer...",
            AllowClear = true,
            AccessibleName = "Local customer select"
        };
        single.Items.Add(new BootstrapSelectItem(1, "Contoso"));
        single.Items.Add(new BootstrapSelectItem(2, "Fabrikam"));
        single.Items.Add(new BootstrapSelectItem(3, "Northwind"));
        single.Items.Add(new BootstrapSelectItem(4, "Adventure Works") { Disabled = true });
        single.Items.Add(new BootstrapSelectItem(
            5,
            "Tailspin Toys — a deliberately long customer caption used to verify ellipsis and popup width behavior"));
        _singleStatus.AutoSize = true;
        _singleStatus.Text = "Selected: none";
        single.SelectionChanged += (_, _) =>
            _singleStatus.Text = "Selected: " + (single.SelectedItem?.Text ?? "none");
        AddScenario(grid, "Single / local search / clear", single, _singleStatus,
            "Type to filter, select the long row, then use the clear affordance. Disabled results remain visible but cannot be newly selected.");

        var productSearch = new BootstrapSelect
        {
            Name = "productSearchSelect",
            Width = 420,
            Placeholder = "Tìm sản phẩm...",
            SearchEnabled = true,
            SelectionMode = BootstrapSelectMode.Single,
            ResultRowHeight = 48,
            DropDownWidth = 420,
            Renderer = new BootstrapSelectProductRenderer(),
            AccessibleName = "Product search with custom results"
        };
        var products = new[]
        {
            new BootstrapSelectProduct(101, "Cà phê rang xay Arabica", "Gói 500 g", 185000m, 42),
            new BootstrapSelectProduct(102, "Trà ô long cao sơn", "Hộp 20 túi", 128000m, 18),
            new BootstrapSelectProduct(103, "Mật ong hoa cà phê", "Chai 500 ml", 215000m, 7),
            new BootstrapSelectProduct(104, "Hạt điều rang muối", "Hũ 350 g", 149000m, 0)
        };
        foreach (var product in products)
        {
            productSearch.Items.Add(new BootstrapSelectItem(product.Id, product.Name)
            {
                Tag = product,
                Disabled = product.StockQuantity == 0
            });
        }
        AddScenario(grid, "Custom product result template", productSearch, null,
            "Each 48px result composes product name, unit, price, and stock metadata while the closed selection keeps the normal Select renderer.");

        var multiple = new BootstrapSelect
        {
            Width = 420,
            SelectionMode = BootstrapSelectMode.Multiple,
            Placeholder = "Choose products...",
            AllowClear = true,
            AllowCustomValues = true,
            MaximumSelectionRows = 3,
            AccessibleName = "Grouped multi-select with custom values"
        };
        multiple.Items.Add(new BootstrapSelectItem("crm", "CRM Suite") { Group = "Business Apps" });
        multiple.Items.Add(new BootstrapSelectItem("erp", "ERP Core") { Group = "Business Apps" });
        multiple.Items.Add(new BootstrapSelectItem("mail", "Mail Gateway") { Group = "Infrastructure" });
        multiple.Items.Add(new BootstrapSelectItem("backup", "Backup Service") { Group = "Infrastructure" });
        multiple.CustomValueFactory = text =>
        {
            var normalized = text.Trim();
            return normalized.Length == 0
                ? null
                : new BootstrapSelectItem("custom:" + normalized.ToLowerInvariant(), normalized);
        };
        multiple.Select(multiple.Items[0]);
        multiple.Select(multiple.Items[2]);
        _multipleStatus.AutoSize = true;
        UpdateMultipleStatus(multiple, _multipleStatus);
        multiple.SelectionChanged += (_, _) => UpdateMultipleStatus(multiple, _multipleStatus);
        AddScenario(grid, "Multiple / groups / custom values", multiple, _multipleStatus,
            "Selected values render as chips. Remove/clear chips, search groups, and type a new exact value to get a Create action.");

        var validated = new BootstrapSelect
        {
            Width = 340,
            Placeholder = "Validation state",
            ValidationState = BootstrapValidationState.Invalid,
            BorderRadius = 8
        };
        validated.Items.Add(new BootstrapSelectItem(10, "Invalid until selected"));
        validated.Items.Add(new BootstrapSelectItem(20, "Another value"));
        validated.SelectionChanged += (_, _) =>
            validated.ValidationState = validated.SelectedItem is null
                ? BootstrapValidationState.Invalid
                : BootstrapValidationState.Valid;
        AddScenario(grid, "Validation / explicit radius", validated, null,
            "The outer shell follows the shared validation/focus priority while popup results keep normal theme colors.");

        _localSection.Controls.Add(grid);
    }

    private void BuildAsyncSection()
    {
        var grid = CreateScenarioGrid();

        var asyncSingle = new BootstrapSelect
        {
            Width = 420,
            Placeholder = "Search remote customers...",
            DataProvider = new BootstrapSelectDemoProvider(),
            SearchDebounce = TimeSpan.FromMilliseconds(180),
            MinimumSearchLength = 0,
            PageSize = 20,
            DropDownWidth = 460,
            AccessibleName = "Async paged customer select"
        };
        _asyncSingleStatus.AutoSize = true;
        _asyncSingleStatus.Text = "Open and type quickly to exercise debounce/cancellation/latest-query wins.";
        asyncSingle.SearchStarted += (_, _) => _asyncSingleStatus.Text = "Loading query page...";
        asyncSingle.SearchCompleted += (_, _) =>
            _asyncSingleStatus.Text = "Loaded results. Scroll near the end to request the next 20-row page.";
        asyncSingle.SearchFailed += (_, e) =>
            _asyncSingleStatus.Text = "Provider failure: " + e.Error.Message;
        asyncSingle.SelectionChanged += (_, _) =>
            _asyncSingleStatus.Text = "Selected: " + (asyncSingle.SelectedItem?.Text ?? "none");
        AddScenario(grid, "Async single / delayed provider / paging", asyncSingle, _asyncSingleStatus,
            "The provider has 300+ deterministic in-memory rows. The control owns debounce, cancellation, stale-generation rejection, paging and selection snapshots.");

        var asyncMultiple = new BootstrapSelect
        {
            Width = 460,
            SelectionMode = BootstrapSelectMode.Multiple,
            Placeholder = "Search paged results...",
            DataProvider = new BootstrapSelectDemoProvider(),
            SearchDebounce = TimeSpan.FromMilliseconds(120),
            PageSize = 20,
            MaximumSelectionRows = 3,
            AccessibleName = "Async multiple customer select"
        };
        asyncMultiple.Select(new BootstrapSelectItem(1, "Customer 001") { Group = "North region" });
        _asyncMultipleStatus.AutoSize = true;
        _asyncMultipleStatus.Text = "Customer 001 is preselected. Change queries to verify the selected snapshot survives; use 'fail-first', 'retry', and 'race' for error/race scenarios.";
        asyncMultiple.SearchFailed += (_, e) =>
            _asyncMultipleStatus.Text = "Expected demo failure on page " + e.Page + ". Activate the retry row.";
        asyncMultiple.SearchCompleted += (_, e) =>
        {
            if (e.Page > 1)
            {
                _asyncMultipleStatus.Text = "Page " + e.Page + " loaded; selections from earlier pages remain stable.";
            }
        };
        asyncMultiple.SelectionChanged += (_, _) =>
        {
            if (asyncMultiple.SelectedItems.Count > 0)
            {
                _asyncMultipleStatus.Text = asyncMultiple.SelectedItems.Count
                    + " selected across current/previous result pages.";
            }
        };
        AddScenario(grid, "Async multiple / retained selection / retry", asyncMultiple, _asyncMultipleStatus,
            "Search 'fail-first' to retry page 1, 'retry' to retry a later page, or 'race' and type rapidly to exercise stale-result protection.");

        var placementHost = new Panel
        {
            Size = new Size(600, 150),
            Margin = Padding.Empty,
            BorderStyle = BorderStyle.FixedSingle
        };
        var placementSelect = new BootstrapSelect
        {
            Width = 300,
            Location = new Point(285, 100),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Placeholder = "Open near lower/right edge...",
            AccessibleName = "Lower-right placement select"
        };
        placementSelect.Items.Add(new BootstrapSelectItem("bottom", "Bottom-start preferred"));
        placementSelect.Items.Add(new BootstrapSelectItem("flip", "Flip above when lower space is constrained"));
        placementSelect.Items.Add(new BootstrapSelectItem("shift", "Shift inside the monitor working area"));
        placementHost.Controls.Add(placementSelect);
        AddScenario(grid, "Placement / lower-right edge", placementHost, null,
            "Move the demo window toward the lower-right of a monitor, then open this anchored Select to observe shared overlay flip/shift behavior.");

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            Text = "Manual matrix: switch Light/Dark in the integrated header; test keyboard-only open/search/navigation/select/deselect/clear/close and Vietnamese IME input; move the host near screen edges and across monitors; repeat at 100%, 125%, 150%, and 200% Windows scaling."
        };
        grid.Controls.Add(note, 0, grid.RowCount);
        grid.SetColumnSpan(note, 2);
        grid.RowCount++;

        _asyncSection.Controls.Add(grid);
    }

    private static TableLayoutPanel CreateScenarioGrid()
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 640));
        return grid;
    }

    private static void AddScenario(
        TableLayoutPanel grid,
        string title,
        Control control,
        Control? status,
        string note)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Margin = new Padding(0, 8, 12, 18)
        };

        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 14),
            Padding = Padding.Empty
        };
        control.Margin = new Padding(0, 0, 0, 4);
        panel.Controls.Add(control);
        if (status is not null)
        {
            status.Margin = new Padding(0, 2, 0, 2);
            status.MaximumSize = new Size(600, 0);
            panel.Controls.Add(status);
        }
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(600, 0),
            Text = note,
            Margin = new Padding(0, 2, 0, 0)
        });

        grid.Controls.Add(titleLabel, 0, row);
        grid.Controls.Add(panel, 1, row);
    }

    private static void UpdateMultipleStatus(BootstrapSelect select, Label label)
    {
        label.Text = select.SelectedItems.Count == 0
            ? "Selected: none"
            : "Selected: " + string.Join(", ", select.SelectedItems.Select(item => item.Text));
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _content.BackColor = theme.Colors.Body;
        _content.ForeColor = theme.Colors.Text;
        _localSection.ForeColor = theme.Colors.Text;
        _asyncSection.ForeColor = theme.Colors.Text;
        Invalidate(true);
    }
}
