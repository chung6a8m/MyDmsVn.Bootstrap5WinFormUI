using System;
using System.Collections.Generic;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectSelectionStateTests
{
    [Test]
    public void MultipleSelectionUsesValueComparerNotReferenceIdentity()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);

        Assert.That(state.TrySelect(new BootstrapSelectItem(7, "A"), BootstrapSelectChangeReason.Programmatic).Changed, Is.True);
        Assert.That(state.TrySelect(new BootstrapSelectItem(7, "B"), BootstrapSelectChangeReason.Programmatic).Changed, Is.False);
        Assert.That(state.SelectedItems, Has.Count.EqualTo(1));
        Assert.That(state.SelectedItems[0].Text, Is.EqualTo("A"));
    }

    [Test]
    public void CustomComparerDefinesLogicalIdentity()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, new CaseInsensitiveObjectComparer());

        state.TrySelect(new BootstrapSelectItem("ABC", "First"), BootstrapSelectChangeReason.Programmatic);
        var duplicate = state.TrySelect(new BootstrapSelectItem("abc", "Second"), BootstrapSelectChangeReason.Programmatic);

        Assert.That(duplicate.Changed, Is.False);
        Assert.That(state.SelectedItems, Has.Count.EqualTo(1));
    }

    [Test]
    public void SingleSelectionReplacementReportsRemovedThenAddedAndPreservesNewItem()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Single, EqualityComparer<object>.Default);
        var first = new BootstrapSelectItem(1, "One");
        var second = new BootstrapSelectItem(2, "Two");
        state.TrySelect(first, BootstrapSelectChangeReason.Programmatic);

        var mutation = state.TrySelect(second, BootstrapSelectChangeReason.Keyboard);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(mutation.Changed, Is.True);
            Assert.That(mutation.RemovedItems, Is.EqualTo(new[] { first }));
            Assert.That(mutation.AddedItems, Is.EqualTo(new[] { second }));
            Assert.That(mutation.Reason, Is.EqualTo(BootstrapSelectChangeReason.Keyboard));
            Assert.That(state.SelectedItems, Is.EqualTo(new[] { second }));
        }));
    }

    [Test]
    public void MultipleSelectionPreservesInsertionOrder()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
        var first = new BootstrapSelectItem(1, "One");
        var second = new BootstrapSelectItem(2, "Two");
        var third = new BootstrapSelectItem(3, "Three");

        state.TrySelect(first, BootstrapSelectChangeReason.Mouse);
        state.TrySelect(second, BootstrapSelectChangeReason.Mouse);
        state.TrySelect(third, BootstrapSelectChangeReason.Mouse);

        Assert.That(state.SelectedItems, Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void DisabledCandidateCannotBeNewlySelectedButDisabledExistingItemCanBeRemoved()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
        var disabled = new BootstrapSelectItem(1, "Disabled") { Disabled = true };
        Assert.That(state.TrySelect(disabled, BootstrapSelectChangeReason.Mouse).Changed, Is.False);

        var enabled = new BootstrapSelectItem(2, "Enabled");
        state.TrySelect(enabled, BootstrapSelectChangeReason.Programmatic);
        enabled.Disabled = true;

        Assert.That(state.TryRemove(enabled.Value, BootstrapSelectChangeReason.Clear).Changed, Is.True);
        Assert.That(state.SelectedItems, Is.Empty);
    }

    [Test]
    public void PreviewDoesNotMutateUntilCommit()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
        var item = new BootstrapSelectItem(1, "One");

        var mutation = state.PreviewSelect(item, BootstrapSelectChangeReason.Programmatic);
        Assert.That(state.SelectedItems, Is.Empty);

        state.Apply(mutation);
        Assert.That(state.SelectedItems, Is.EqualTo(new[] { item }));
    }

    [Test]
    public void MultipleToSinglePreviewKeepsFirstAndRemovesRemainingAtomically()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
        var first = new BootstrapSelectItem(1, "One");
        var second = new BootstrapSelectItem(2, "Two");
        state.TrySelect(first, BootstrapSelectChangeReason.Programmatic);
        state.TrySelect(second, BootstrapSelectChangeReason.Programmatic);

        var mutation = state.PreviewModeChange(BootstrapSelectMode.Single);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(state.Mode, Is.EqualTo(BootstrapSelectMode.Multiple));
            Assert.That(state.SelectedItems, Is.EqualTo(new[] { first, second }));
            Assert.That(mutation.RemovedItems, Is.EqualTo(new[] { second }));
            Assert.That(mutation.Reason, Is.EqualTo(BootstrapSelectChangeReason.ModeChange));
        }));

        state.Apply(mutation);
        Assert.That(state.Mode, Is.EqualTo(BootstrapSelectMode.Single));
        Assert.That(state.SelectedItems, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void SingleToMultiplePreservesSelection()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Single, EqualityComparer<object>.Default);
        var first = new BootstrapSelectItem(1, "One");
        state.TrySelect(first, BootstrapSelectChangeReason.Programmatic);

        state.Apply(state.PreviewModeChange(BootstrapSelectMode.Multiple));

        Assert.That(state.Mode, Is.EqualTo(BootstrapSelectMode.Multiple));
        Assert.That(state.SelectedItems, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void ClearReturnsOneBatchMutationAndRemovesEverything()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
        var first = new BootstrapSelectItem(1, "One");
        var second = new BootstrapSelectItem(2, "Two");
        state.TrySelect(first, BootstrapSelectChangeReason.Programmatic);
        state.TrySelect(second, BootstrapSelectChangeReason.Programmatic);

        var mutation = state.TryClear(BootstrapSelectChangeReason.Clear);

        Assert.That(mutation.RemovedItems, Is.EqualTo(new[] { first, second }));
        Assert.That(mutation.AddedItems, Is.Empty);
        Assert.That(state.SelectedItems, Is.Empty);
    }

    [Test]
    public void MetadataRefreshReplacesSameValueSnapshotWithoutLogicalSelectionMutation()
    {
        var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Single, EqualityComparer<object>.Default);
        var original = new BootstrapSelectItem(7, "Old");
        state.TrySelect(original, BootstrapSelectChangeReason.Programmatic);
        var refreshed = new BootstrapSelectItem(7, "New") { Group = "Updated" };

        var replaced = state.RefreshSelectedItem(refreshed);

        Assert.That(replaced, Is.True);
        Assert.That(state.SelectedItems, Has.Count.EqualTo(1));
        Assert.That(state.SelectedItems[0], Is.SameAs(refreshed));
        Assert.That(state.SelectedItems[0].Text, Is.EqualTo("New"));
    }

    private sealed class CaseInsensitiveObjectComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            if (x is string sx && y is string sy)
            {
                return string.Equals(sx, sy, StringComparison.OrdinalIgnoreCase);
            }

            return EqualityComparer<object>.Default.Equals(x!, y!);
        }

        public int GetHashCode(object obj)
        {
            return obj is string text
                ? StringComparer.OrdinalIgnoreCase.GetHashCode(text)
                : obj.GetHashCode();
        }
    }
}
