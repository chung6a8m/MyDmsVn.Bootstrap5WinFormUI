using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class PaginationDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly BootstrapDataGridView _grid = new BootstrapDataGridView();
    private readonly BootstrapPagination _gridPagination = new BootstrapPagination();
    private readonly Label _gridStatus = new Label();
    private readonly DataTable _orders = CreateOrdersTable(53);

    public PaginationDemoForm()
    {
        Text = "BootstrapPagination Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1080, 760);
        MinimumSize = new Size(760, 520);

        ConfigureContent();
        Controls.Add(_content);

        AddWindowScenarios();
        AddSizeScenarios();
        AddVisibilityScenario();
        AddGridScenario();
    }

    private void ConfigureContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
    }

    private void AddWindowScenarios()
    {
        _content.Controls.Add(CreateScenario(
            "Small range — no ellipsis",
            "Five total pages, current page 3.",
            CreatePagination(totalItems: 50, pageSize: 10, currentPage: 3)));

        _content.Controls.Add(CreateScenario(
            "Large range — middle window",
            "Twenty total pages, current page 10.",
            CreatePagination(totalItems: 200, pageSize: 10, currentPage: 10)));

        _content.Controls.Add(CreateScenario(
            "Boundary state",
            "Last page selected; Next and Last are disabled.",
            CreatePagination(totalItems: 200, pageSize: 10, currentPage: 20)));

        _content.Controls.Add(CreateScenario(
            "Zero items",
            "Empty data still has a stable selected page 1.",
            new BootstrapPagination()));
    }

    private void AddSizeScenarios()
    {
        var panel = CreateSection("Button sizes", "Small, Default, and Large reuse BootstrapButton sizing.");
        panel.Controls.Add(CreatePagination(200, 10, 10, BootstrapButtonSize.Small), 0, 2);
        panel.Controls.Add(CreatePagination(200, 10, 10, BootstrapButtonSize.Default), 0, 3);
        panel.Controls.Add(CreatePagination(200, 10, 10, BootstrapButtonSize.Large), 0, 4);
        _content.Controls.Add(panel);
    }

    private void AddVisibilityScenario()
    {
        var pagination = CreatePagination(200, 10, 10);
        var firstLast = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Show First / Last",
            Margin = new Padding(0, 4, 16, 4)
        };
        var previousNext = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Show Previous / Next",
            Margin = new Padding(0, 4, 16, 4)
        };

        firstLast.CheckedChanged += (_, _) => pagination.ShowFirstLast = firstLast.Checked;
        previousNext.CheckedChanged += (_, _) => pagination.ShowPreviousNext = previousNext.Checked;

        var toggles = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        toggles.Controls.Add(firstLast);
        toggles.Controls.Add(previousNext);

        var panel = CreateSection("Navigation visibility", "Toggle First/Last and Previous/Next independently.");
        panel.Controls.Add(toggles, 0, 2);
        panel.Controls.Add(pagination, 0, 3);
        _content.Controls.Add(panel);
    }

    private void AddGridScenario()
    {
        var panel = CreateSection(
            "Application-owned DataGrid paging",
            "The demo owns the DataTable and slices ten rows after PageChanged; BootstrapPagination never owns the data source.");

        _grid.Width = 960;
        _grid.Height = 280;
        _grid.AutoGenerateColumns = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.EmptyStateText = "No rows for this page.";

        _gridPagination.TotalItems = _orders.Rows.Count;
        _gridPagination.PageSize = 10;
        _gridPagination.CurrentPage = 1;
        _gridPagination.PageChanged += (_, _) => ApplyGridPage();

        _gridStatus.AutoSize = true;
        _gridStatus.Margin = new Padding(0, 4, 0, 4);

        panel.Controls.Add(_grid, 0, 2);
        panel.Controls.Add(_gridPagination, 0, 3);
        panel.Controls.Add(_gridStatus, 0, 4);
        _content.Controls.Add(panel);

        ApplyGridPage();
    }

    private void ApplyGridPage()
    {
        var page = _orders.Clone();
        var start = (_gridPagination.CurrentPage - 1) * _gridPagination.PageSize;
        var endExclusive = Math.Min(start + _gridPagination.PageSize, _orders.Rows.Count);
        for (var index = start; index < endExclusive; index++)
        {
            page.ImportRow(_orders.Rows[index]);
        }

        _grid.DataSource = page;
        _gridStatus.Text = $"Page {_gridPagination.CurrentPage} of {_gridPagination.TotalPages} — rows {start + 1}–{endExclusive} of {_orders.Rows.Count}.";
    }

    private static BootstrapPagination CreatePagination(
        int totalItems,
        int pageSize,
        int currentPage,
        BootstrapButtonSize buttonSize = BootstrapButtonSize.Default)
    {
        return new BootstrapPagination
        {
            TotalItems = totalItems,
            PageSize = pageSize,
            CurrentPage = currentPage,
            ButtonSize = buttonSize,
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    private static TableLayoutPanel CreateScenario(string title, string description, BootstrapPagination pagination)
    {
        var panel = CreateSection(title, description);
        panel.Controls.Add(pagination, 0, 2);
        return panel;
    }

    private static TableLayoutPanel CreateSection(string title, string description)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(8),
            Width = 990
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Text = title,
            Margin = new Padding(0, 0, 0, 2)
        };
        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = description,
            Margin = new Padding(0, 0, 0, 6)
        };

        panel.Controls.Add(titleLabel, 0, 0);
        panel.Controls.Add(descriptionLabel, 0, 1);
        return panel;
    }

    private static DataTable CreateOrdersTable(int rowCount)
    {
        var table = new DataTable("Orders");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Customer", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("Total", typeof(decimal));

        var statuses = new[] { "Draft", "Open", "Packed", "Shipped" };
        for (var index = 1; index <= rowCount; index++)
        {
            table.Rows.Add(
                index,
                $"Customer {index:000}",
                statuses[(index - 1) % statuses.Length],
                125000m + (index * 13750m));
        }

        return table;
    }
}
