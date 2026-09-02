using System;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupPopupTests
{
    [Test]
    public void PublicOpenCloseAndCancelHaveDistinctStateSemantics()
    {
        using var host = new Form();
        using var lookup = CreateLookup();
        host.Controls.Add(lookup);
        host.Show();
        lookup.SelectValue(1);
        lookup.Text = "pending";
        lookup.OpenDropDown();
        Assert.That(lookup.IsDropDownOpen, Is.True);

        lookup.CloseDropDown();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("pending"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));

        lookup.OpenDropDown();
        lookup.CancelPendingEdit();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("Coffee"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
        host.Close();
    }

    [Test]
    public void RefreshRaisesRequestWithoutCommittingOrChangingPendingText()
    {
        using var lookup = CreateLookup();
        lookup.SelectValue(1);
        lookup.Text = "cof";
        var refreshes = 0;
        lookup.RefreshRequested += (_, e) => { refreshes++; Assert.That(e.QueryText, Is.EqualTo("cof")); };
        lookup.RefreshResults();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(refreshes, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("cof"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
    }

    private static BootstrapLookupBox CreateLookup() => new BootstrapLookupBox
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = new BindingList<Product> { new(1, "Coffee"), new(2, "Tea") }
    };

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
