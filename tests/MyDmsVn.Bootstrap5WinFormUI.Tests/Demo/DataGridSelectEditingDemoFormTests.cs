using System;
using System.Collections.Generic;
using System.Data;
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
    public void DemoUsesNativeLookupColumnsAndTypedBinding()
    {
        using var form = CreateAndShow();
        var grid = Find<BootstrapDataGridView>(form).Single();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.Columns[0], Is.InstanceOf<BootstrapLookupColumn>());
            Assert.That(grid.Columns[1], Is.InstanceOf<BootstrapLookupColumn>());
            Assert.That(Find<BootstrapSelect>(form), Is.Empty);
            Assert.That(grid.DataSource, Is.Not.InstanceOf<DataTable>());
            Assert.That(grid.AllowUserToAddRows, Is.True);
        }));
    }

    [Test]
    public void ProductLookupCommitsRawIdAndDependentMetadata()
    {
        using var form = CreateAndShow();
        var grid = Find<BootstrapDataGridView>(form).Single();
        grid.CurrentCell = grid.Rows[0].Cells["ProductColumn"];
        Assert.That(grid.BeginEdit(true), Is.True); Application.DoEvents();
        ((BootstrapLookupBox)grid.EditingControl!).SelectValue(2);
        grid.EndEdit(); Application.DoEvents();
        var row = grid.Rows[0];
        Assert.Multiple((Action)(() =>
        {
            Assert.That(row.Cells["ProductColumn"].Value, Is.EqualTo(2));
            Assert.That(row.Cells["UnitColumn"].Value, Is.EqualTo("Hộp 20 túi"));
            Assert.That(Convert.ToDecimal(row.Cells["UnitPriceColumn"].Value), Is.EqualTo(128000m));
            Assert.That(Convert.ToDecimal(row.Cells["LineTotalColumn"].Value), Is.EqualTo(256000m));
        }));
    }

    [Test]
    public void EditingQuantityRecalculatesLineTotal()
    {
        using var form = CreateAndShow();
        var row = Find<BootstrapDataGridView>(form).Single().Rows[0];
        row.Cells["QuantityColumn"].Value = 3m; Application.DoEvents();
        Assert.That(Convert.ToDecimal(row.Cells["LineTotalColumn"].Value), Is.EqualTo(555000m));
    }

    private static Form CreateAndShow()
    {
        var type = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridSelectEditingDemoForm");
        var form = (Form)Activator.CreateInstance(type!)!; form.Show(); Application.DoEvents(); return form;
    }

    private static IEnumerable<T> Find<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in Find<T>(child)) yield return nested;
        }
    }
}
