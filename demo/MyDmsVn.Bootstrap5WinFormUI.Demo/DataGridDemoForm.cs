using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class DataGridDemoForm : Form
{
    private const int LargeRowCount = 10000;

    private readonly BootstrapDataGridView _grid = new BootstrapDataGridView();
    private readonly FlowLayoutPanel _commandBar = new FlowLayoutPanel();
    private readonly Button _sampleButton = new Button();
    private readonly Button _emptyButton = new Button();
    private readonly Button _largeButton = new Button();
    private readonly Button _loadingButton = new Button();
    private readonly Label _status = new Label();
    private readonly Label _instructions = new Label();

    public DataGridDemoForm()
    {
        Text = "BootstrapDataGridView Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1080, 680);
        MinimumSize = new Size(760, 480);

        ConfigureCommandBar();
        ConfigureGrid();
        ConfigureStatusArea();

        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(_instructions);
        Controls.Add(_commandBar);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        LoadScenario(32, "Sample binding: 32 rows");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureCommandBar()
    {
        _commandBar.Dock = DockStyle.Top;
        _commandBar.AutoSize = true;
        _commandBar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _commandBar.WrapContents = true;
        _commandBar.Padding = new Padding(12, 10, 12, 8);

        ConfigureButton(_sampleButton, "Load sample", (_, _) => LoadScenario(32, "Sample binding: 32 rows"));
        ConfigureButton(_emptyButton, "Show empty", (_, _) => LoadScenario(0, "Empty-state scenario"));
        ConfigureButton(_largeButton, "Load 10,000 rows", (_, _) => LoadScenario(LargeRowCount, "Large binding: 10,000 rows"));
        ConfigureButton(_loadingButton, "Toggle loading", (_, _) =>
        {
            _grid.Loading = !_grid.Loading;
            UpdateStatus(_grid.Loading ? "Loading overlay visible" : GetBindingSummary());
        });

        _commandBar.Controls.Add(_sampleButton);
        _commandBar.Controls.Add(_emptyButton);
        _commandBar.Controls.Add(_largeButton);
        _commandBar.Controls.Add(_loadingButton);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.Margin = Padding.Empty;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.EmptyStateText = "No rows in this scenario.";
        _grid.LoadingText = "Loading records...";

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IdColumn",
            HeaderText = "ID",
            DataPropertyName = "Id",
            Width = 72
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CustomerColumn",
            HeaderText = "Customer",
            DataPropertyName = "Customer",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StatusColumn",
            HeaderText = "Status",
            DataPropertyName = "Status",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalColumn",
            HeaderText = "Total",
            DataPropertyName = "Total",
            Width = 130,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "N2"
            }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "UpdatedColumn",
            HeaderText = "Updated",
            DataPropertyName = "Updated",
            Width = 155,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "yyyy-MM-dd HH:mm"
            }
        });
    }

    private void ConfigureStatusArea()
    {
        _instructions.Dock = DockStyle.Bottom;
        _instructions.AutoSize = false;
        _instructions.Height = 42;
        _instructions.Padding = new Padding(12, 4, 12, 4);
        _instructions.TextAlign = ContentAlignment.MiddleLeft;
        _instructions.Text = "Manual performance check: load 10,000 rows, scroll, sort, resize/reorder columns, then switch Light/Dark from the main demo.";

        _status.Dock = DockStyle.Bottom;
        _status.AutoSize = false;
        _status.Height = 34;
        _status.Padding = new Padding(12, 2, 12, 2);
        _status.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void ConfigureButton(Button button, string text, EventHandler click)
    {
        button.AutoSize = true;
        button.Margin = new Padding(0, 0, 8, 0);
        button.Text = text;
        button.UseVisualStyleBackColor = false;
        button.Click += click;
    }

    private void LoadScenario(int rowCount, string status)
    {
        _grid.Loading = false;
        _grid.DataSource = CreateTable(rowCount);
        UpdateStatus(status);
    }

    private static DataTable CreateTable(int rowCount)
    {
        var table = new DataTable("Orders");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Customer", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("Total", typeof(decimal));
        table.Columns.Add("Updated", typeof(DateTime));

        var statuses = new[] { "Draft", "Open", "Packed", "Shipped" };
        var baseTime = new DateTime(2026, 8, 28, 8, 0, 0, DateTimeKind.Local);
        table.BeginLoadData();
        try
        {
            for (var index = 1; index <= rowCount; index++)
            {
                table.Rows.Add(
                    index,
                    $"Customer {index:00000}",
                    statuses[(index - 1) % statuses.Length],
                    125000m + ((index * 137m) % 3500000m),
                    baseTime.AddMinutes(-(index % 1440)));
            }
        }
        finally
        {
            table.EndLoadData();
        }

        return table;
    }

    private string GetBindingSummary()
    {
        if (_grid.DataSource is DataTable table)
        {
            return table.Rows.Count == 0
                ? "Empty-state scenario"
                : $"Bound DataTable: {table.Rows.Count:N0} rows";
        }

        return "No data source";
    }

    private void UpdateStatus(string text)
    {
        _status.Text = text;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _commandBar.BackColor = theme.Colors.SurfaceSecondary;
        _commandBar.ForeColor = theme.Colors.Text;
        _status.BackColor = theme.Colors.SurfaceSecondary;
        _status.ForeColor = theme.Colors.Text;
        _instructions.BackColor = theme.Colors.Body;
        _instructions.ForeColor = theme.Colors.MutedText;

        foreach (var button in new[] { _sampleButton, _emptyButton, _largeButton, _loadingButton })
        {
            button.BackColor = theme.Colors.Surface;
            button.ForeColor = theme.Colors.Text;
        }
    }
}
