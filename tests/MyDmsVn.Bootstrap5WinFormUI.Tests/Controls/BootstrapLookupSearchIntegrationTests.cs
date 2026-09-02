using System;
using System.ComponentModel;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapLookupSearchIntegrationTests
{
    [Test]
    public void SearchAppliesVietnameseProjectionAndRaisesOnlyForLogicalChanges()
    {
        using var lookup = Create();
        var changes = 0;
        lookup.ResultsChanged += (_, _) => changes++;
        lookup.Text = "ca phe";
        lookup.ExecuteSearchNow();
        var firstCount = changes;
        lookup.ExecuteSearchNow();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.ResultsGrid.Rows.Cast<System.Windows.Forms.DataGridViewRow>().Select(row => row.Cells[0].Value), Is.EqualTo(new[] { "Cà phê rang", "Cà phê sữa" }));
            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
        }));
    }

    [Test]
    public void HighlightNavigationNeverCommitsPendingState()
    {
        using var lookup = Create();
        lookup.SelectValue(1);
        lookup.Text = "ca phe";
        lookup.ExecuteSearchNow();
        lookup.NavigateResults(System.Windows.Forms.Keys.End);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(((Product)lookup.HighlightedItem!).Id, Is.EqualTo(2));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("ca phe"));
            Assert.That(lookup.HasPendingText, Is.True);
        }));
    }

    [Test]
    public void WaitingMinimumLengthIsAResultStateTransitionAndCancelDiscardsDebounce()
    {
        using var lookup = Create();
        lookup.MinimumSearchLength = 3;
        var changes = 0;
        lookup.ResultsChanged += (_, _) => changes++;
        lookup.Text = "ca";
        lookup.ExecuteSearchNow();
        Assert.That(lookup.ResultsGrid.Rows, Is.Empty);
        Assert.That(changes, Is.EqualTo(1));

        lookup.SearchDebounceMilliseconds = 1000;
        lookup.Text = "ca phe";
        lookup.CancelPendingEdit();
        lookup.FlushPendingSearch();
        Assert.That(lookup.Text, Is.EqualTo(lookup.CommittedDisplayText));
    }

    private static BootstrapLookupBox Create() => new BootstrapLookupBox
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = new BindingList<Product>
        {
            new(1, "Cà phê rang"),
            new(2, "Cà phê sữa"),
            new(3, "Trà xanh")
        },
        SearchDebounceMilliseconds = 0
    };

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
