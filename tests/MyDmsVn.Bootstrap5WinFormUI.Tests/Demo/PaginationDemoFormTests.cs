using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class PaginationDemoFormTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void PaginationDemoContainsPaginationAndDataGridScenarios()
    {
        using var form = new PaginationDemoForm();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(FindControls<BootstrapPagination>(form).Any(), Is.True);
            Assert.That(FindControls<BootstrapDataGridView>(form).Any(), Is.True);
        }));
    }

    [Test]
    public void IntegratedMainFormNavigationContainsPaginationPage()
    {
        using var form = new MainForm();
        var sidebar = FindControls<BootstrapSidebar>(form).Single();

        Assert.That(sidebar.Items.Any(item => string.Equals(item.Text, "Pagination", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void SectionHeadingsUseTheDemoHeadingTypography()
    {
        using var form = new PaginationDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var expectedTitles = new[]
        {
            "Small range — no ellipsis",
            "Large range — middle window",
            "Boundary state",
            "Zero items",
            "Button sizes",
            "Navigation visibility",
            "Application-owned DataGrid paging"
        };
        var headings = FindControls<Label>(form)
            .Where(label => expectedTitles.Contains(label.Text, StringComparer.Ordinal))
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(headings, Has.Length.EqualTo(expectedTitles.Length));
            Assert.That(headings.All(label => label.Font.FontFamily.Name == "Segoe UI"), Is.True);
            Assert.That(headings.All(label => Math.Abs(label.Font.SizeInPoints - 15f) < 0.01f), Is.True);
            Assert.That(headings.All(label => label.Font.Bold), Is.True);
        }));
    }

    [Test]
    public void BoundPaginationRowsFitTheDemoBodyTypography()
    {
        using var form = new PaginationDemoForm();
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

    private static System.Collections.Generic.IEnumerable<T> FindControls<T>(Control root)
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
