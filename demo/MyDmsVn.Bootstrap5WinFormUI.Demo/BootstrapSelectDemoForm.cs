using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            AccessibleName = "Local customer select"
        };
        single.Items.Add(new BootstrapSelectItem(1, "Contoso"));
        single.Items.Add(new BootstrapSelectItem(2, "Fabrikam"));
        single.Items.Add(new BootstrapSelectItem(3, "Northwind"));
        single.Items.Add(new BootstrapSelectItem(4, "Adventure Works") { Disabled = true });
        _singleStatus.AutoSize = true;
        _singleStatus.Text = "Selected: none";
        single.SelectionChanged += (_, _) =>
            _singleStatus.Text = "Selected: " + (single.SelectedItem?.Text ?? "none");
        AddScenario(grid, "Single / local search", single, _singleStatus,
            "Type to filter. Disabled results remain visible but cannot be newly selected.");

        var multiple = new BootstrapSelect
        {
            Width = 420,
            SelectionMode = BootstrapSelectMode.Multiple,
            Placeholder = "Choose products...",
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
            return normalized.Length == 0 ? null : new BootstrapSelectItem("custom:" + normalized.ToLowerInvariant(), normalized);
        };
        multiple.Select(multiple.Items[0]);
        multiple.Select(multiple.Items[2]);
        _multipleStatus.AutoSize = true;
        UpdateMultipleStatus(multiple, _multipleStatus);
        multiple.SelectionChanged += (_, _) => UpdateMultipleStatus(multiple, _multipleStatus);
        AddScenario(grid, "Multiple / groups / custom values", multiple, _multipleStatus,
            "Selected values render as chips. Type a new exact value to get a Create action; partial matches do not suppress it.");

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
            validated.ValidationState = validated.SelectedItem is null ? BootstrapValidationState.Invalid : BootstrapValidationState.Valid;
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
            DataProvider = new DemoSelectProvider(),
            SearchDebounce = TimeSpan.FromMilliseconds(180),
            MinimumSearchLength = 0,
            PageSize = 8,
            DropDownWidth = 460,
            AccessibleName = "Async paged customer select"
        };
        _asyncSingleStatus.AutoSize = true;
        _asyncSingleStatus.Text = "Open and type quickly to exercise debounce/cancellation/latest-query wins.";
        asyncSingle.SearchStarted += (_, e) => _asyncSingleStatus.Text = "Loading query page...";
        asyncSingle.SearchCompleted += (_, e) => _asyncSingleStatus.Text = "Loaded results. Scroll near the end to request the next page.";
        asyncSingle.SearchFailed += (_, e) => _asyncSingleStatus.Text = "Provider failure: " + e.Error.Message;
        asyncSingle.SelectionChanged += (_, _) =>
            _asyncSingleStatus.Text = "Selected: " + (asyncSingle.SelectedItem?.Text ?? "none");
        AddScenario(grid, "Async single / delayed provider / paging", asyncSingle, _asyncSingleStatus,
            "The provider is transport-agnostic. The control owns debounce, cancellation, stale-generation rejection, paging and selection snapshots.");

        var asyncMultiple = new BootstrapSelect
        {
            Width = 460,
            SelectionMode = BootstrapSelectMode.Multiple,
            Placeholder = "Search paged results...",
            DataProvider = new DemoSelectProvider(),
            SearchDebounce = TimeSpan.FromMilliseconds(120),
            PageSize = 6,
            MaximumSelectionRows = 3,
            AccessibleName = "Async multiple customer select"
        };
        _asyncMultipleStatus.AutoSize = true;
        _asyncMultipleStatus.Text = "Type 'retry', then scroll to page 2: the demo provider fails once and the retry row reloads the same page.";
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
                _asyncMultipleStatus.Text = asyncMultiple.SelectedItems.Count + " selected across current/previous result pages.";
            }
        };
        AddScenario(grid, "Async multiple / failure + retry", asyncMultiple, _asyncMultipleStatus,
            "Search 'retry' to exercise later-page failure/retry. Search 'race' and type rapidly to exercise stale-result protection.");

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            Text = "Manual matrix: switch Light/Dark in the integrated header; test keyboard-only open/search/navigation/select/deselect/clear/close; move the host near screen edges; and repeat at 100%, 125%, 150%, and 200% Windows scaling."
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

    private static void AddScenario(TableLayoutPanel grid, string title, Control control, Control? status, string note)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
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

    private sealed class DemoSelectProvider : IBootstrapSelectDataProvider
    {
        private readonly List<BootstrapSelectItem> _items;
        private int _retryFailureIssued;

        internal DemoSelectProvider()
        {
            _items = new List<BootstrapSelectItem>();
            for (var i = 1; i <= 48; i++)
            {
                _items.Add(new BootstrapSelectItem(i, "Customer " + i.ToString("00"))
                {
                    Group = i <= 24 ? "North region" : "South region"
                });
            }

            for (var i = 1; i <= 18; i++)
            {
                _items.Add(new BootstrapSelectItem(1000 + i, "Retry sample " + i.ToString("00"))
                {
                    Group = "Retry samples"
                });
            }

            for (var i = 1; i <= 12; i++)
            {
                _items.Add(new BootstrapSelectItem(2000 + i, "Race sample " + i.ToString("00"))
                {
                    Group = "Race samples"
                });
            }
        }

        public async Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken)
        {
            var searchText = query.SearchText.Trim();
            var delay = searchText.StartsWith("race", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(80, 520 - (searchText.Length * 70))
                : 260;

            if (searchText.StartsWith("race", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(searchText, "retry", StringComparison.OrdinalIgnoreCase)
                && query.Page == 2
                && Interlocked.Exchange(ref _retryFailureIssued, 1) == 0)
            {
                throw new InvalidOperationException("Demo page-2 failure. Activate the retry row to continue.");
            }

            var filtered = _items
                .Where(item => searchText.Length == 0 || item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            var start = (query.Page - 1) * query.PageSize;
            var pageItems = filtered.Skip(start).Take(query.PageSize).ToList();
            return new BootstrapSelectPage(pageItems, start + pageItems.Count < filtered.Count);
        }
    }
}
