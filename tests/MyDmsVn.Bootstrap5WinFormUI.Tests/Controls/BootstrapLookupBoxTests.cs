using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapLookupBoxTests
{
    [Test]
    public void TypeDefaultsAndDesignerCollectionsMatchReviewedContract()
    {
        using var lookup = new BootstrapLookupBox();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup, Is.InstanceOf<BootstrapTextBox>());
            Assert.That(TypeDescriptor.GetDefaultProperty(lookup)?.Name, Is.EqualTo(nameof(BootstrapLookupBox.DisplayMember)));
            Assert.That(TypeDescriptor.GetDefaultEvent(lookup)?.Name, Is.EqualTo(nameof(BootstrapLookupBox.SelectionCommitted)));
            Assert.That(lookup.SearchDebounceMilliseconds, Is.EqualTo(150));
            Assert.That(lookup.MinimumSearchLength, Is.Zero);
            Assert.That(lookup.EmptyQueryBehavior, Is.EqualTo(BootstrapLookupEmptyQueryBehavior.ShowAll));
            Assert.That(lookup.TypingPopupBehavior, Is.EqualTo(BootstrapLookupTypingPopupBehavior.AutoOpen));
            Assert.That(lookup.UnmatchedTextBehavior, Is.EqualTo(BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection));
            Assert.That(lookup.EnterKeyBehavior, Is.EqualTo(BootstrapLookupEnterKeyBehavior.CommitSelection));
            Assert.That(lookup.ClosedEnterKeyBehavior, Is.EqualTo(BootstrapLookupClosedEnterKeyBehavior.ResolvePendingText));
            Assert.That(lookup.DropDownWidth, Is.Zero);
            Assert.That(lookup.MaxDropDownHeight, Is.EqualTo(320));
            Assert.That(lookup.ShowColumnHeaders, Is.True);
            Assert.That(lookup.ShowRefreshButton, Is.False);
            Assert.That(lookup.ShowAddNewButton, Is.False);
            Assert.That(lookup.InvalidTextMessage, Is.EqualTo("Please select a valid value."));
            Assert.That(TypeDescriptor.GetProperties(lookup)[nameof(BootstrapLookupBox.Columns)]!.SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Content));
            Assert.That(TypeDescriptor.GetProperties(lookup)[nameof(BootstrapLookupBox.SearchMembers)]!.SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Content));
        }));
    }

    [Test]
    public void TypingKeepsCommittedSelectionUntilExplicitCommit()
    {
        var products = new[] { new Product(15, "Coffee"), new Product(21, "Milk") };
        using var lookup = CreateLookup(products);
        Assert.That(lookup.SelectValue(15), Is.True);
        lookup.Text = "cof";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(15));
            Assert.That(lookup.SelectedItem, Is.SameAs(products[0]));
            Assert.That(lookup.CommittedDisplayText, Is.EqualTo("Coffee"));
            Assert.That(lookup.Text, Is.EqualTo("cof"));
            Assert.That(lookup.HasPendingText, Is.True);
        }));
    }

    [Test]
    public void ProgrammaticSelectionRaisesValueChangeOnlyForLogicalChanges()
    {
        var products = new[] { new Product(15, "Coffee"), new Product(21, "Milk") };
        using var lookup = CreateLookup(products);
        var changes = 0;
        lookup.SelectedValueChanged += (_, _) => changes++;

        Assert.That(lookup.SelectValue(15), Is.True);
        Assert.That(lookup.SelectItem(products[0]), Is.True);
        lookup.SelectedValue = 21;
        lookup.ClearSelection();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(changes, Is.EqualTo(3));
            Assert.That(lookup.SelectedValue, Is.Null);
            Assert.That(lookup.CommittedDisplayText, Is.Empty);
        }));
    }

    [Test]
    public void MissingRawValueIsPreservedAndCancelRestoresCommittedText()
    {
        using var lookup = CreateLookup(new[] { new Product(1, "One") });
        lookup.SelectedValue = 99;
        lookup.Text = "pending";
        lookup.CancelPendingEdit();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(99));
            Assert.That(lookup.SelectedItem, Is.Null);
            Assert.That(lookup.Text, Is.EqualTo(lookup.CommittedDisplayText));
            Assert.That(lookup.HasPendingText, Is.False);
            Assert.That(lookup.ValidationMessage, Is.Empty);
        }));
    }

    [Test]
    public void LookupOwnsOneAlwaysVisibleNonSelectableAffordance()
    {
        using var lookup = new BootstrapLookupBox();
        var affordance = lookup.Controls.Cast<Control>().Single(control => control.GetType().Name == "BootstrapLookupDropDownAffordance");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(affordance.Visible, Is.True);
            Assert.That(affordance.TabStop, Is.False);
            Assert.That(affordance.CanSelect, Is.False);
        }));
    }

    private static BootstrapLookupBox CreateLookup(object dataSource) => new BootstrapLookupBox
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = dataSource
    };

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
