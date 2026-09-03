using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
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

    [TestCase(false)]
    [TestCase(true)]
    public void CommitAndAddInfersEmptyStringItemSource(bool useBindingSource)
    {
        var strings = useBindingSource ? null : new List<string>();
        var bindingList = useBindingSource ? new BindingList<string>() : null;
        using var bindingSource = useBindingSource ? new BindingSource { DataSource = bindingList } : null;
        using var lookup = new BootstrapLookupBox
        {
            DataSource = (object?)bindingSource ?? strings!,
            UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd,
            Text = "Tea"
        };

        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
        IEnumerable<string> actualSource = useBindingSource ? bindingList! : strings!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.NavigationAllowed, Is.True);
            Assert.That(lookup.SelectedValue, Is.EqualTo("Tea"));
            Assert.That(actualSource, Does.Contain("Tea"));
        }));
    }

    [Test]
    public void NestedSelectionChangeSuppressesStaleOuterCommitNotification()
    {
        using var lookup = Create(new BindingList<Product>
        {
            new(1, "Coffee"), new(2, "Tea")
        });
        var redirected = false;
        var observedValues = new List<object?>();
        lookup.SelectedValueChanged += (_, _) =>
        {
            if (redirected) return;
            redirected = true;
            lookup.SelectedValue = 2;
        };
        lookup.SelectionCommitted += (_, e) =>
        {
            Assert.That(e.Value, Is.EqualTo(lookup.SelectedValue));
            Assert.That(e.Item, Is.SameAs(lookup.SelectedItem));
            Assert.That(e.DisplayText, Is.EqualTo(lookup.CommittedDisplayText));
            observedValues.Add(e.Value);
        };

        lookup.SelectedValue = 1;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(observedValues, Is.EqualTo(new object?[] { 2 }));
        }));
    }

    [Test]
    public void HighlightNotificationDuringCommitObservesConsistentCommittedState()
    {
        using var lookup = Create(new BindingList<Product>
        {
            new(1, "Coffee"), new(2, "Tea")
        });
        lookup.SelectValue(1);
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
        lookup.Text = "invalid";
        lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);
        var observations = 0;
        lookup.HighlightedItemChanged += (_, e) =>
        {
            observations++;
            Assert.That(e.NewItem, Is.SameAs(lookup.SelectedItem));
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(lookup.CommittedDisplayText, Is.EqualTo("Tea"));
            Assert.That(lookup.Text, Is.EqualTo("Tea"));
            Assert.That(lookup.HasPendingText, Is.False);
            Assert.That(lookup.ValidationMessage, Is.Empty);
        };

        lookup.SelectValue(2);

        Assert.That(observations, Is.EqualTo(1));
    }

    [Test]
    public void CommitAndAddRefreshesPlainListProjectionForReopen()
    {
        var source = new List<string>();
        using var lookup = new BootstrapLookupBox
        {
            DataSource = source,
            UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd,
            SearchDebounceMilliseconds = 0,
            Text = "Gamma"
        };
        lookup.ExecuteSearchNow();

        Assert.That(lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard).NavigationAllowed, Is.True);
        lookup.OpenDropDown();

        Assert.That(lookup.ResultsGrid.Rows, Has.Count.EqualTo(1));
        Assert.That(lookup.ResultsGrid.Rows[0].Cells[0].Value, Is.EqualTo("Gamma"));
    }

    [Test]
    public void CommitAndAddUsesMetadataFromReconfiguredBindingSource()
    {
        using var source = new BindingSource();
        using var lookup = new BootstrapLookupBox
        {
            DataSource = source,
            UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd
        };
        var strings = new BindingList<string>();
        source.DataSource = strings;
        lookup.Text = "Tea";

        var result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard);

        Assert.That(result.NavigationAllowed, Is.True);
        Assert.That(strings, Is.EqualTo(new[] { "Tea" }));
        Assert.That(lookup.SelectedValue, Is.EqualTo("Tea"));
    }

    [Test]
    public void SelectionCommittedStopsStaleOuterDeliveryBetweenSubscribers()
    {
        using var lookup = Create(new BindingList<Product> { new(1, "Alpha"), new(2, "Beta") });
        var secondSubscriberValues = new List<object?>();
        var redirected = false;
        lookup.SelectionCommitted += (_, e) =>
        {
            if (!redirected && Equals(e.Value, 1))
            {
                redirected = true;
                lookup.SelectedValue = 2;
            }
        };
        lookup.SelectionCommitted += (_, e) =>
        {
            Assert.That(e.Value, Is.EqualTo(lookup.SelectedValue));
            secondSubscriberValues.Add(e.Value);
        };

        lookup.SelectedValue = 1;

        Assert.That(secondSubscriberValues, Is.EqualTo(new object?[] { 2 }));
    }

    [Test]
    public void TextChangedDuringCommitObservesConsistentCommittedState()
    {
        using var lookup = Create(new BindingList<Product> { new(1, "Alpha"), new(2, "Beta") });
        lookup.SelectValue(1);
        lookup.Text = "be";
        lookup.SetLookupValidation("old error");
        lookup.TextChanged += (_, _) =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(lookup.CommittedDisplayText, Is.EqualTo("Beta"));
            Assert.That(lookup.HasPendingText, Is.False);
            Assert.That(lookup.ValidationMessage, Is.Empty);
        };

        lookup.SelectValue(2);
    }

    [Test]
    public void TextChangedReentrantSelectionLeavesNestedHighlightAndCommitStateIntact()
    {
        var first = new Product(1, "Alpha");
        var second = new Product(2, "Beta");
        using var lookup = Create(new BindingList<Product> { first, second });
        var redirected = false;
        lookup.TextChanged += (_, _) =>
        {
            if (redirected || lookup.Text != "Alpha") return;
            redirected = true;
            lookup.SelectedValue = 2;
        };

        lookup.SelectedValue = 1;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(lookup.SelectedItem, Is.SameAs(second));
            Assert.That(lookup.Text, Is.EqualTo("Beta"));
            Assert.That(lookup.CommittedDisplayText, Is.EqualTo("Beta"));
            Assert.That(lookup.HighlightedItem, Is.SameAs(second));
        }));
    }

    [Test]
    public void CommitAndAddRejectsIncompatibleItemBeforeMutatingHeterogeneousSource()
    {
        var source = new ArrayList { new SearchableProduct(1, "Alpha", "A") };
        using var lookup = new BootstrapLookupBox
        {
            DataSource = source,
            DisplayMember = "Name",
            ValueMember = "Id",
            UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.CommitAndAdd,
            Text = "Gamma"
        };
        lookup.SearchMembers.Add("Code");
        lookup.CreateItemFromText += (_, e) => e.Item = new ValueOnlyProduct(3);

        BootstrapLookupCommitResult? result = null;
        Assert.DoesNotThrow((Action)(() => result = lookup.ResolvePendingText(BootstrapLookupCommitReason.Keyboard)));

        Assert.That(result!.NavigationAllowed, Is.False);
        Assert.That(source, Has.Count.EqualTo(1));
        Assert.That(lookup.SelectedValue, Is.Null);
    }

    private static BootstrapLookupBox Create(object source) => new BootstrapLookupBox { DisplayMember = "Name", ValueMember = "Id", DataSource = source };

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }

    private sealed class SearchableProduct
    {
        internal SearchableProduct(int id, string name, string code) { Id = id; Name = name; Code = code; }
        public int Id { get; }
        public string Name { get; }
        public string Code { get; }
    }

    private sealed class ValueOnlyProduct
    {
        internal ValueOnlyProduct(int id) { Id = id; }
        public int Id { get; }
    }
}
