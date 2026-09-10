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
    public void BoundRowsFitTheDemoBodyFontAndBootstrapControlMetrics()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
            var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridDemoForm");
            using var form = (Form)Activator.CreateInstance(demoType!)!;
            form.CreateControl();
            form.PerformLayout();

            var grid = FindControls<BootstrapDataGridView>(form).Single();
            var dpi = grid.DeviceDpi > 0 ? grid.DeviceDpi : 96;
            var expectedMinimum = (int)Math.Round(36d * dpi / 96d, MidpointRounding.AwayFromZero);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
                Assert.That(grid.RowTemplate.Height, Is.GreaterThanOrEqualTo(expectedMinimum));
                Assert.That(grid.Rows.Cast<DataGridViewRow>().All(row => row.Height >= expectedMinimum), Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
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
    public void ThemeChangePreservesRowSharingForTheLargeScenario()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
            var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridDemoForm");
            using var form = (Form)Activator.CreateInstance(demoType!)!;
            form.Show();
            form.PerformLayout();

            var grid = FindControls<BootstrapDataGridView>(form).Single();
            DataGridViewTestGuard.FailOnDataError(grid);
            FindControls<Button>(form).Single(button => button.Text == "Load 10,000 rows").PerformClick();
            Application.DoEvents();
            var unsharedCount = 0;
            grid.RowUnshared += (_, _) => unsharedCount++;

            BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Dark);
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.Rows.Count, Is.EqualTo(10000));
                Assert.That(unsharedCount, Is.Zero);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void PostBindDpiTransitionRecreatesLargeRowsAtTheScaledHeightWithoutUnsharing()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = theme;
            var demoType = typeof(MainForm).Assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Demo.DataGridDemoForm");
            using var form = (Form)Activator.CreateInstance(demoType!)!;
            form.CreateControl();
            form.PerformLayout();

            var grid = FindControls<BootstrapDataGridView>(form).Single();
            DataGridViewTestGuard.FailOnDataError(grid);
            var loadScenario = demoType!.GetMethod("LoadScenario", BindingFlags.Instance | BindingFlags.NonPublic)!;
            loadScenario.Invoke(form, new object[] { 10000, "Large binding: 10,000 rows" });
            var unsharedCount = 0;
            grid.RowUnshared += (_, _) => unsharedCount++;

            InvokeDpiRebind(form, 144);
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.Rows.Count, Is.EqualTo(10000));
                Assert.That(grid.RowTemplate.Height, Is.EqualTo(54));
                Assert.That(grid.Rows.SharedRow(0).Height, Is.EqualTo(54));
                Assert.That(grid.Rows.SharedRow(9999).Height, Is.EqualTo(54));
                Assert.That(unsharedCount, Is.Zero);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
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

    private static void InvokeDpiRebind(Form form, int dpi)
    {
        var rebind = form.GetType().GetMethod("RebindGridForDpi", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(rebind, Is.Not.Null, "The demo must provide a post-bind DPI rebind path.");
        rebind!.Invoke(form, new object[] { dpi });
    }
}
