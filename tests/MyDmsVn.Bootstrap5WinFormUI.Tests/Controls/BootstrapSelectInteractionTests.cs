using System;
using System.Collections.Generic;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectInteractionTests
{
    [Test]
    public void ProgrammaticSelectionKeepsAllSelectionViewsCoherent()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Items.Add(alpha);
        select.Items.Add(beta);

        Assert.That(select.Select(alpha), Is.True);
        Assert.That(select.Select(new BootstrapSelectItem(1, "Duplicate instance")), Is.False);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedItem, Is.SameAs(alpha));
            Assert.That(select.SelectedValue, Is.EqualTo(1));
            Assert.That(select.SelectedItems, Has.Count.EqualTo(1));
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 1 }));
        }));

        Assert.That(select.SelectValue(2), Is.True);
        Assert.That(select.SelectedItem, Is.SameAs(beta));
        Assert.That(select.DeselectValue(2), Is.True);
        Assert.That(select.SelectedItems, Is.Empty);
    }

    [Test]
    public void SuccessfulSelectUsesDeterministicEventOrderAndCancellationPreventsCommit()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var events = new List<string>();
        select.Selecting += (_, e) => events.Add("Selecting:" + e.Reason);
        select.Selected += (_, e) => events.Add("Selected:" + e.Reason);
        select.SelectionChanged += (_, _) => events.Add("SelectionChanged");

        Assert.That(select.Select(alpha), Is.True);
        Assert.That(events, Is.EqualTo(new[] { "Selecting:Programmatic", "Selected:Programmatic", "SelectionChanged" }));

        select.ClearSelection();
        events.Clear();
        select.Selecting += (_, e) => { if (e.Item.Value.Equals(2)) e.Cancel = true; };
        Assert.That(select.Select(new BootstrapSelectItem(2, "Beta")), Is.False);
        Assert.That(select.SelectedItems, Is.Empty);
        Assert.That(events, Is.Empty);
    }

    [Test]
    public void ClearInMultipleModeHonorsPerItemCancellationAndRaisesOneBatchChangedEvent()
    {
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Select(alpha);
        select.Select(beta);

        var deselected = new List<object>();
        var changed = 0;
        select.Deselecting += (_, e) => { if (e.Item.Value.Equals(2)) e.Cancel = true; };
        select.Deselected += (_, e) => deselected.Add(e.Item.Value);
        select.SelectionChanged += (_, _) => changed++;

        select.ClearSelection();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 2 }));
            Assert.That(deselected, Is.EqualTo(new object[] { 1 }));
            Assert.That(changed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void MultipleToSingleTransitionIsAtomicWhenModeChangeDeselectIsCancelled()
    {
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Select(alpha);
        select.Select(beta);
        select.Deselecting += (_, e) =>
        {
            if (e.Reason == BootstrapSelectChangeReason.ModeChange && e.Item.Value.Equals(2)) e.Cancel = true;
        };

        select.SelectionMode = BootstrapSelectMode.Single;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectionMode, Is.EqualTo(BootstrapSelectMode.Multiple));
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 1, 2 }));
        }));
    }

    [Test]
    public void SelectedItemAndSelectedValueSettersUseNormalSelectionPipeline()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem("a", "Alpha");
        select.Items.Add(alpha);
        var selecting = 0;
        select.Selecting += (_, _) => selecting++;

        select.SelectedItem = alpha;
        Assert.That(select.SelectedValue, Is.EqualTo("a"));
        select.SelectedValue = null;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedItems, Is.Empty);
            Assert.That(selecting, Is.EqualTo(1));
        }));
    }
}
