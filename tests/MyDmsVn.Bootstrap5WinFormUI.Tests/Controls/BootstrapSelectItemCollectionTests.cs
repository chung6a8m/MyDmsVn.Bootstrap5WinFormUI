using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectItemCollectionTests
{
    [Test]
    public void PublicCollectionRejectsNullAndPreservesInsertionOrder()
    {
        var items = new BootstrapSelectItemCollection();
        var one = new BootstrapSelectItem(1, "One");
        var two = new BootstrapSelectItem(2, "Two");

        Assert.That(() => items.Add(null!), Throws.TypeOf<ArgumentNullException>());

        items.Add(one);
        items.Add(two);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0], Is.SameAs(one));
            Assert.That(items[1], Is.SameAs(two));
        }));

        Assert.That(() => items[0] = null!, Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void OwnerCallbackRunsExactlyOnceForEachSuccessfulMutation()
    {
        var changes = 0;
        var items = new BootstrapSelectItemCollection(() => changes++);
        var one = new BootstrapSelectItem(1, "One");
        var replacement = new BootstrapSelectItem(10, "Ten");
        var two = new BootstrapSelectItem(2, "Two");

        items.Add(one);
        items[0] = replacement;
        items.Add(two);
        items.RemoveAt(0);
        items.Clear();

        Assert.That(changes, Is.EqualTo(5));
    }

    [Test]
    public void FailedNullMutationDoesNotNotifyOwner()
    {
        var changes = 0;
        var items = new BootstrapSelectItemCollection(() => changes++);
        items.Add(new BootstrapSelectItem(1, "One"));
        changes = 0;

        Assert.That(() => items.Add(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => items[0] = null!, Throws.TypeOf<ArgumentNullException>());
        Assert.That(changes, Is.Zero);
    }
}
