using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class DataGridSelectEditingDemoFormTests
{
    [TestCase(96, 36)]
    [TestCase(120, 45)]
    [TestCase(144, 54)]
    [TestCase(192, 72)]
    public void DemoGridRowMetricsScaleTheBootstrapEditingHeight(int dpi, int expectedHeight)
    {
        var helperType = typeof(MainForm).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Demo.DemoDataGridRowMetrics",
            throwOnError: true)!;
        var calculate = helperType.GetMethod("Calculate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        using var font = new System.Drawing.Font("Segoe UI", 12f);

        var height = (int)calculate.Invoke(
            null,
            new object[] { font, BootstrapThemeMetrics.Default, dpi })!;

        Assert.That(height, Is.EqualTo(expectedHeight));
    }

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
            var product = (BootstrapLookupColumn)grid.Columns["ProductColumn"];
            Assert.That(product.LookupColumns[1].AutoSizeMode, Is.EqualTo(DataGridViewAutoSizeColumnMode.Fill));
            Assert.That(product.LookupColumns[1].MinimumWidth, Is.GreaterThanOrEqualTo(200));
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
    public void ProductLookupEditorFitsInsideTheThemedDataRow()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
            using var form = CreateAndShow();
            var grid = Find<BootstrapDataGridView>(form).Single();
            grid.CurrentCell = grid.Rows[0].Cells["ProductColumn"];
            Assert.That(grid.BeginEdit(true), Is.True);
            Application.DoEvents();

            var editor = (BootstrapLookupBox)grid.EditingControl!;
            var measuredText = TextRenderer.MeasureText("Ag", grid.Font);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.Rows[0].Height, Is.GreaterThanOrEqualTo(measuredText.Height));
                Assert.That(editor.Height, Is.LessThanOrEqualTo(grid.Rows[0].Height));
                Assert.That(grid.RowTemplate.Height, Is.EqualTo(grid.Rows[0].Height));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [TestCase(144, 54)]
    [TestCase(192, 72)]
    public void PostBindDpiTransitionRecreatesEditableRowsBeforeOpeningTheLookupEditor(
        int dpi,
        int expectedHeight)
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = theme;
            using var form = CreateAndShow();
            var grid = Find<BootstrapDataGridView>(form).Single();

            InvokeDpiRebind(form, dpi);
            Application.DoEvents();
            grid.CurrentCell = grid.Rows[0].Cells["ProductColumn"];
            Assert.That(grid.BeginEdit(true), Is.True);
            Application.DoEvents();

            var editor = (BootstrapLookupBox)grid.EditingControl!;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.EditMode, Is.EqualTo(DataGridViewEditMode.EditOnEnter));
                Assert.That(grid.RowTemplate.Height, Is.EqualTo(expectedHeight));
                Assert.That(grid.Rows.Cast<DataGridViewRow>().All(row => row.Height == expectedHeight), Is.True);
                Assert.That(editor.Height, Is.LessThanOrEqualTo(grid.Rows[0].Height));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
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
        var form = (Form)Activator.CreateInstance(type!)!;
        DataGridViewTestGuard.FailOnDataError(Find<BootstrapDataGridView>(form).Single());
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static IEnumerable<T> Find<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in Find<T>(child)) yield return nested;
        }
    }

    private static void InvokeDpiRebind(Form form, int dpi)
    {
        var rebind = form.GetType().GetMethod("RebindGridForDpi", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(rebind, Is.Not.Null, "The demo must provide a post-bind DPI rebind path.");
        rebind!.Invoke(form, new object[] { dpi });
    }
}
