using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class DataGridSelectEditingDemoFormTests
{
    [Test]
    public void DemoUsesFiveNativeTextColumnsAndSampleRows()
    {
        using var form = CreateAndShowDemoForm();

        var grid = FindControls<BootstrapDataGridView>(form).Single();
        var headers = grid.Columns.Cast<DataGridViewColumn>().Select(column => column.HeaderText).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.AutoGenerateColumns, Is.False);
            Assert.That(grid.Columns.Cast<DataGridViewColumn>().All(column => column is DataGridViewTextBoxColumn), Is.True,
                "The demo must not introduce a custom DataGridView column/cell type.");
            Assert.That(headers, Is.EqualTo(new[] { "Tên hàng", "Đơn vị tính", "Số lượng", "Đơn giá", "Thành tiền" }));
            Assert.That(grid.Rows.Count, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void ProductCellEditingUsesBootstrapSelectPopupAndCommitsProductMetadata()
    {
        using var form = CreateAndShowDemoForm();

        var grid = FindControls<BootstrapDataGridView>(form).Single();
        grid.CurrentCell = grid.Rows[0].Cells["ProductNameColumn"];

        Assert.That(grid.BeginEdit(true), Is.True);
        Application.DoEvents();

        var select = FindControls<BootstrapSelect>(grid).SingleOrDefault(control => control.Visible);
        Assert.That(select, Is.Not.Null, "EditingControlShowing should replace the visible product editor with BootstrapSelect.");

        select!.OpenDropDownInternal();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True, "The Select popup must stay open while the grid cell is being edited.");
            Assert.That(grid.IsCurrentCellInEditMode, Is.True);
        }));

        select.SetSearchTextForTest("Trà ô long");
        Application.DoEvents();
        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Application.DoEvents();

        var row = grid.Rows[0];
        Assert.Multiple((Action)(() =>
        {
            Assert.That(row.Cells["ProductNameColumn"].Value, Is.EqualTo("Trà ô long cao sơn"));
            Assert.That(row.Cells["UnitColumn"].Value, Is.EqualTo("Hộp 20 túi"));
            Assert.That(Convert.ToDecimal(row.Cells["UnitPriceColumn"].Value), Is.EqualTo(128000m));
            Assert.That(Convert.ToDecimal(row.Cells["QuantityColumn"].Value), Is.EqualTo(2m));
            Assert.That(Convert.ToDecimal(row.Cells["LineTotalColumn"].Value), Is.EqualTo(256000m));
        }));
    }

    [Test]
    public void EditingQuantityRecalculatesLineTotal()
    {
        using var form = CreateAndShowDemoForm();

        var grid = FindControls<BootstrapDataGridView>(form).Single();
        var row = grid.Rows[0];

        row.Cells["QuantityColumn"].Value = 3m;
        grid.EndEdit();
        Application.DoEvents();

        Assert.That(Convert.ToDecimal(row.Cells["LineTotalColumn"].Value), Is.EqualTo(555000m));
    }

    private static Form CreateAndShowDemoForm()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridSelectEditingDemoForm");
        Assert.That(demoType, Is.Not.Null, "The integrated demo should include DataGridSelectEditingDemoForm.");

        var form = (Form)Activator.CreateInstance(demoType!)!;
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Application.DoEvents();
        return form;
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
