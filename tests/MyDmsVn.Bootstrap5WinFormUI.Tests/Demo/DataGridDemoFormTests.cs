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
public sealed class DataGridDemoFormTests
{
    [Test]
    public void Phase13DemoExposesRealColumnsAndBoundSampleData()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridDemoForm");

        Assert.That(demoType, Is.Not.Null, "Phase 13 requires a DataGridDemoForm.");
        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var grid = FindControls<BootstrapDataGridView>(form).Single();
        var table = grid.DataSource as DataTable;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.AutoGenerateColumns, Is.False);
            Assert.That(grid.Columns.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(table, Is.Not.Null, "Demo should use a real tabular binding source.");
            Assert.That(table!.Rows.Count, Is.GreaterThan(0), "Demo should start with sample rows.");
        }));
    }

    [Test]
    public void DemoExposesEmptyLargeAndLoadingScenarios()
    {
        var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridDemoForm");
        Assert.That(demoType, Is.Not.Null);

        using var form = (Form)Activator.CreateInstance(demoType!)!;
        form.CreateControl();
        form.PerformLayout();

        var buttonTexts = FindControls<Button>(form).Select(button => button.Text).ToArray();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(buttonTexts, Does.Contain("Load sample"));
            Assert.That(buttonTexts, Does.Contain("Show empty"));
            Assert.That(buttonTexts, Does.Contain("Load 10,000 rows"));
            Assert.That(buttonTexts, Does.Contain("Toggle loading"));
        }));
    }

    [Test]
    public void MainDemoExposesDataGridNavigationPage()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        Assert.That(
            sidebar.Items.Any(item => item.Text == "DataGrid"),
            Is.True,
            "Phase 13 needs to remain reachable from the integrated demo navigation.");
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
