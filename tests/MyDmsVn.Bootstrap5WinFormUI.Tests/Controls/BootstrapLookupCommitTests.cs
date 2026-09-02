using System;
using System.Collections.Generic;
using System.ComponentModel;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapLookupCommitTests
{
    [Test]
    public void EmptyTextClearsSelectionWithoutUsingUnmatchedPolicy()
    {
        using var lookup = Create(new BindingList<Product> { new(1, "Coffee") });
        lookup.SelectValue(1);
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
        lookup.Text = "  ";

        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.NavigationAllowed, Is.True);
            Assert.That(lookup.SelectedValue, Is.Null);
            Assert.That(lookup.ValidationMessage, Is.Empty);
        }));
    }

    [Test]
    public void UniqueExactMatchUsesTrimAndCurrentCultureIgnoreCase()
    {
        using var lookup = Create(new BindingList<Product> { new(1, "Coffee") });
        lookup.Text = " coffee ";
        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.NavigationAllowed, Is.True);
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("Coffee"));
        }));
    }

    [Test]
    public void AmbiguousExactDisplayBlocksWithoutCommitOrCreate()
    {
        var source = new BindingList<Product> { new(1, "Same"), new(2, "Same") };
        using var lookup = Create(source);
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd;
        var creates = 0;
        lookup.CreateItemFromText += (_, _) => creates++;
        lookup.Text = "same";

        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.NavigationAllowed, Is.False);
            Assert.That(lookup.SelectedValue, Is.Null);
            Assert.That(lookup.Text, Is.EqualTo("same"));
            Assert.That(lookup.ValidationMessage, Is.Not.Empty);
            Assert.That(creates, Is.Zero);
            Assert.That(source, Has.Count.EqualTo(2));
        }));
    }

    [Test]
    public void DuplicateRowsWithSameLogicalValueCommitFirstSourceItem()
    {
        var first = new Product(1, "Same");
        var second = new Product(1, "Same");
        using var lookup = Create(new BindingList<Product> { first, second });
        lookup.Text = "Same";
        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
        Assert.That(result.NavigationAllowed, Is.True);
        Assert.That(lookup.SelectedItem, Is.SameAs(first));
    }

    [Test]
    public void UnmatchedPoliciesRestoreOrLayerTransientValidation()
    {
        using var lookup = Create(new BindingList<Product> { new(1, "Coffee") });
        lookup.SelectValue(1);
        lookup.Text = "missing";
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection;
        Assert.That(lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard).NavigationAllowed, Is.True);
        Assert.That(lookup.Text, Is.EqualTo("Coffee"));

        lookup.ValidationState = BootstrapValidationState.Valid;
        lookup.Text = "missing";
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
        Assert.That(lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard).NavigationAllowed, Is.False);
        lookup.ValidationState = BootstrapValidationState.None;
        Assert.That(lookup.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
        lookup.Text = "new typing";
        Assert.That(lookup.ValidationState, Is.EqualTo(BootstrapValidationState.None));
    }

    [Test]
    public void CommitAndAddIsAtomicForWritableAndReadOnlySources()
    {
        var strings = new BindingList<string> { "Coffee" };
        using var stringLookup = new BootstrapLookupBox { DataSource = strings, UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd, Text = " Tea " };
        Assert.That(stringLookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard).NavigationAllowed, Is.True);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(strings, Does.Contain("Tea"));
            Assert.That(stringLookup.SelectedValue, Is.EqualTo("Tea"));
        }));

        var readOnly = Array.AsReadOnly(new[] { new Product(1, "Coffee") });
        using var objectLookup = Create(readOnly);
        objectLookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd;
        objectLookup.CreateItemFromText += (_, e) => e.Item = new Product(2, "Tea");
        objectLookup.Text = "Tea";
        Assert.That(objectLookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard).NavigationAllowed, Is.False);
        Assert.That(objectLookup.SelectedValue, Is.Null);
    }

    private static BootstrapLookupBox Create(object source) => new BootstrapLookupBox { DisplayMember = "Name", ValueMember = "Id", DataSource = source };

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
