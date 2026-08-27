using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDataGridViewTests
{
    [Test]
    public void DefaultsMatchPhase13Contract()
    {
        using var grid = new BootstrapDataGridView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.EmptyStateText, Is.EqualTo("No data to display."));
            Assert.That(grid.Loading, Is.False);
            Assert.That(grid.LoadingText, Is.EqualTo("Loading..."));
            Assert.That(grid.EnableHeadersVisualStyles, Is.False);
        }));
    }

    [Test]
    public void CurrentThemeStylesHeaderRowsAlternateRowsSelectionAndGridLines()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = theme;
            using var grid = new BootstrapDataGridView();
            var selectionText = ColorUtil.GetContrastingTextColor(
                theme.Colors.Primary,
                theme.Colors.Light,
                theme.Colors.Dark);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.BackgroundColor.ToArgb(), Is.EqualTo(theme.Colors.Surface.ToArgb()));
                Assert.That(grid.GridColor.ToArgb(), Is.EqualTo(theme.Colors.Border.ToArgb()));
                Assert.That(grid.DefaultCellStyle.BackColor.ToArgb(), Is.EqualTo(theme.Colors.Surface.ToArgb()));
                Assert.That(grid.DefaultCellStyle.ForeColor.ToArgb(), Is.EqualTo(theme.Colors.Text.ToArgb()));
                Assert.That(grid.AlternatingRowsDefaultCellStyle.BackColor.ToArgb(), Is.EqualTo(theme.Colors.SurfaceSecondary.ToArgb()));
                Assert.That(grid.DefaultCellStyle.SelectionBackColor.ToArgb(), Is.EqualTo(theme.Colors.Primary.ToArgb()));
                Assert.That(grid.DefaultCellStyle.SelectionForeColor.ToArgb(), Is.EqualTo(selectionText.ToArgb()));
                Assert.That(grid.ColumnHeadersDefaultCellStyle.BackColor.ToArgb(), Is.EqualTo(theme.Colors.SurfaceSecondary.ToArgb()));
                Assert.That(grid.ColumnHeadersDefaultCellStyle.ForeColor.ToArgb(), Is.EqualTo(theme.Colors.Text.ToArgb()));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void RuntimeThemeSwitchUpdatesGridPresentation()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var grid = new BootstrapDataGridView();
            var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            BootstrapThemeManager.CurrentTheme = dark;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(grid.BackgroundColor.ToArgb(), Is.EqualTo(dark.Colors.Surface.ToArgb()));
                Assert.That(grid.DefaultCellStyle.ForeColor.ToArgb(), Is.EqualTo(dark.Colors.Text.ToArgb()));
                Assert.That(grid.AlternatingRowsDefaultCellStyle.BackColor.ToArgb(), Is.EqualTo(dark.Colors.SurfaceSecondary.ToArgb()));
                Assert.That(grid.ColumnHeadersDefaultCellStyle.BackColor.ToArgb(), Is.EqualTo(dark.Colors.SurfaceSecondary.ToArgb()));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void StandardBindingAndColumnApisRemainCallerOwned()
    {
        using var grid = new BootstrapDataGridView
        {
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("Alpha");
        table.Rows.Add("Beta");
        var column = new DataGridViewTextBoxColumn
        {
            Name = "NameColumn",
            HeaderText = "Name",
            DataPropertyName = "Name"
        };
        grid.Columns.Add(column);

        grid.DataSource = table;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.DataSource, Is.SameAs(table));
            Assert.That(grid.AutoGenerateColumns, Is.False);
            Assert.That(grid.AllowUserToAddRows, Is.False);
            Assert.That(grid.SelectionMode, Is.EqualTo(DataGridViewSelectionMode.FullRowSelect));
            Assert.That(grid.Columns, Has.Count.EqualTo(1));
            Assert.That(grid.Columns[0], Is.SameAs(column));
            Assert.That(grid.Rows, Has.Count.EqualTo(2));
        }));
    }

    [Test]
    public void LoadingOverlayReusesBootstrapSpinnerWithoutMutatingGridState()
    {
        using var grid = new BootstrapDataGridView
        {
            Enabled = false
        };
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Rows.Add(1);
        grid.DataSource = table;

        var spinner = Descendants(grid).OfType<BootstrapSpinner>().SingleOrDefault();
        Assert.That(spinner, Is.Not.Null, "Loading overlay must compose BootstrapSpinner.");
        Assert.That(spinner!.Spinning, Is.False);

        grid.Loading = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.Enabled, Is.False, "Loading must not overwrite caller-owned Enabled state.");
            Assert.That(grid.DataSource, Is.SameAs(table), "Loading must not replace the caller's data source.");
            Assert.That(spinner.Spinning, Is.True);
            Assert.That(spinner.Visible, Is.True);
        }));

        grid.Loading = false;
        Assert.That(spinner.Spinning, Is.False);
    }

    [Test]
    public void NullOverlayTextAssignmentsNormalizeToEmptyStrings()
    {
        using var grid = new BootstrapDataGridView();

        grid.EmptyStateText = null!;
        grid.LoadingText = null!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.EmptyStateText, Is.Empty);
            Assert.That(grid.LoadingText, Is.Empty);
        }));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
