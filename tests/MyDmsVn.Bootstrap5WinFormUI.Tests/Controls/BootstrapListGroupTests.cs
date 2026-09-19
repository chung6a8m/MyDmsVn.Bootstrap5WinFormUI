using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListGroupTests
{
    [Test]
    public void DefaultsDefineACompositionOnlyListContract()
    {
        using var group = new BootstrapListGroup();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group, Is.InstanceOf<Panel>());
            Assert.That(group.Orientation, Is.EqualTo(Orientation.Vertical));
            Assert.That(group.Flush, Is.False);
            Assert.That(group.BorderRadius, Is.EqualTo(-1));
            Assert.That(group.AutoSize, Is.True);
            Assert.That(group.AutoSizeMode, Is.EqualTo(AutoSizeMode.GrowAndShrink));
            Assert.That(group.TabStop, Is.False);
            Assert.That(group.AccessibleRole, Is.EqualTo(AccessibleRole.List));
        }));
    }

    [Test]
    public void InvalidOrientationAndRadiusAreRejected()
    {
        using var group = new BootstrapListGroup();

        Assert.Throws<InvalidEnumArgumentException>((Action)(() => group.Orientation = (Orientation)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => group.BorderRadius = -2));
    }

    [Test]
    public void ItemsIsAFreshSnapshotOfDirectItemChildrenInControlIndexOrder()
    {
        using var group = new BootstrapListGroup();
        using var first = new BootstrapListGroupItem { Text = "First" };
        using var second = new BootstrapListGroupItem { Text = "Second" };
        using var ignored = new Label();
        group.Controls.Add(first);
        group.Controls.Add(ignored);
        group.Controls.Add(second);

        AssertItemsMatchControls(group);
        var beforeReorder = group.Items;

        group.Controls.SetChildIndex(second, 0);

        AssertItemsMatchControls(group);
        Assert.That(beforeReorder, Is.Not.SameAs(group.Items));
        Assert.That(beforeReorder.ToArray(), Is.Not.EqualTo(group.Items.ToArray()));

        first.BringToFront();
        AssertItemsMatchControls(group);
        first.SendToBack();
        AssertItemsMatchControls(group);
    }

    [Test]
    public void HelpersUseNormalControlOwnershipAndNeverDisposeRemovedItems()
    {
        using var group = new BootstrapListGroup();
        using var otherParent = new Panel();
        var created = group.AddItem("Created");
        var supplied = new BootstrapListGroupItem { Text = "Supplied" };
        group.AddItem(supplied);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(created.Parent, Is.SameAs(group));
            Assert.That(supplied.Parent, Is.SameAs(group));
            Assert.That(group.Items, Does.Contain(created));
            Assert.That(group.Items, Does.Contain(supplied));
        }));

        otherParent.Controls.Add(supplied);
        Assert.That(group.Items, Does.Not.Contain(supplied));
        group.AddItem(supplied);
        Assert.That(supplied.Parent, Is.SameAs(group));

        Assert.That(group.RemoveItem(supplied), Is.True);
        Assert.That(supplied.IsDisposed, Is.False);
        Assert.That(group.RemoveItem(supplied), Is.False);

        group.ClearItems();
        Assert.That(created.IsDisposed, Is.False);
        Assert.That(group.Items, Is.Empty);

        supplied.Dispose();
        created.Dispose();
    }

    [Test]
    public void RemoveAndReaddDoesNotDuplicateItemClickSubscription()
    {
        using var group = new BootstrapListGroup();
        using var item = new ClickProbeItem { Actionable = true };
        var count = 0;
        group.ItemClick += (_, e) =>
        {
            Assert.That(e.Item, Is.SameAs(item));
            count++;
        };

        group.Controls.Add(item);
        group.Controls.Remove(item);
        group.Controls.Add(item);
        item.RaiseClick();

        Assert.That(count, Is.EqualTo(1));
    }

    private static void AssertItemsMatchControls(BootstrapListGroup group)
    {
        var expected = group.Controls.Cast<Control>().OfType<BootstrapListGroupItem>().ToArray();
        Assert.That(group.Items, Is.EqualTo(expected));
    }

    private sealed class ClickProbeItem : BootstrapListGroupItem
    {
        public void RaiseClick() => OnClick(EventArgs.Empty);
    }
}
