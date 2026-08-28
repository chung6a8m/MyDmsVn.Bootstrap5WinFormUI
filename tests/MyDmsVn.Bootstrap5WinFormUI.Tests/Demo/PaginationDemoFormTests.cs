using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class PaginationDemoFormTests
{
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
