using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectPopupTests
{
    [Test]
    public void PopupIsLazyReusedAndRaisesLifecycleEvents()
    {
        using var form = new Form { Size = new Size(500, 400), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        using var select = new BootstrapSelect { Location = new Point(30, 30), Width = 240 };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        var opened = 0;
        var closed = 0;
        select.DropDownOpened += (_, _) => opened++;
        select.DropDownClosed += (_, _) => closed++;
        Assert.That(select.IsDropDownCreatedForTest, Is.False);

        select.OpenDropDownInternal();
        Application.DoEvents();
        var creationCount = select.DropDownCreationCountForTest;
        var firstBounds = select.DropDownBoundsForTest;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(creationCount, Is.EqualTo(1));
            Assert.That(firstBounds.Width, Is.GreaterThanOrEqualTo(select.Width));
            Assert.That(opened, Is.EqualTo(1));
        }));

        select.CloseDropDownInternal(false);
        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(creationCount));
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void OwningFormDeactivateClosesOpenPopup()
    {
        using var form = new TestForm();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        var closed = 0;
        select.DropDownClosed += (_, _) => closed++;

        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.That(select.IsDropDownOpenForTest, Is.True);

        form.RaiseDeactivate();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void LocalSinglePopupKeepsStableHeightAcrossRepeatedOpenCycles()
    {
        using var form = new Form
        {
            Size = new Size(600, 500),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100)
        };
        using var select = new BootstrapSelect { Location = new Point(30, 30), Width = 340 };
        select.Items.Add(new BootstrapSelectItem(1, "Contoso"));
        select.Items.Add(new BootstrapSelectItem(2, "Fabrikam"));
        select.Items.Add(new BootstrapSelectItem(3, "Northwind"));
        select.Items.Add(new BootstrapSelectItem(4, "Adventure Works") { Disabled = true });
        select.Items.Add(new BootstrapSelectItem(
            5,
            "Tailspin Toys — a deliberately long customer caption used to verify ellipsis and popup width behavior"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        var heights = new int[3];
        for (var cycle = 0; cycle < heights.Length; cycle++)
        {
            select.OpenDropDownInternal();
            Application.DoEvents();

            heights[cycle] = select.DropDownBoundsForTest.Height;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(select.IsDropDownOpenForTest, Is.True);
                Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(1));
                Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(5));
            }));

            select.CloseDropDownInternal(false);
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(heights[1], Is.EqualTo(heights[0]));
            Assert.That(heights[2], Is.EqualTo(heights[0]));
        }));
    }

    [Test]
    public void LocalSearchFiltersRowsAndActivatedRowUsesSelectionPipeline()
    {
        using var form = new Form();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        select.Items.Add(new BootstrapSelectItem(2, "Beta"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        select.SetSearchTextForTest("be");
        Application.DoEvents();

        Assert.That(select.VisibleResultItemTextsForTest, Is.EqualTo(new[] { "Beta" }));
        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Assert.That(select.SelectedValue, Is.EqualTo(2));
        Assert.That(select.IsDropDownOpenForTest, Is.False);
    }

    [Test]
    public void DisabledRowsAreSkippedByKeyboardNavigation()
    {
        using var form = new Form();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Disabled") { Disabled = true });
        select.Items.Add(new BootstrapSelectItem(2, "Enabled"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        Assert.That(select.HighlightedResultTextForTest, Is.EqualTo("Enabled"));
    }

    private sealed class TestForm : Form
    {
        internal void RaiseDeactivate()
        {
            OnDeactivate(EventArgs.Empty);
        }
    }
}
