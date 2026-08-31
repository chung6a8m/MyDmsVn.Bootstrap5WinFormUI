using System;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToastHistoryStoreTests
{
    [Test]
    public void Add_TrimsOldestAndSnapshotIsNewestFirst()
    {
        var store = new BootstrapToastHistoryStore(2);
        var first = HistoryItem("first", false);
        var second = HistoryItem("second", false);
        var third = HistoryItem("third", false);

        Assert.That(store.Add(first), Is.True);
        Assert.That(store.Add(second), Is.True);
        Assert.That(store.Add(third), Is.True);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(store.SnapshotNewestFirst().Select(x => x.Text), Is.EqualTo(new[] { "third", "second" }));
            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.UnreadCount, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DuplicateIdIsRejectedWithoutChangingStore()
    {
        var store = new BootstrapToastHistoryStore(3);
        var item = HistoryItem("one", false);

        Assert.That(store.Add(item), Is.True);
        Assert.That(store.Add(new BootstrapToastHistoryItem(item.Id, DateTimeOffset.UtcNow, "", "duplicate", BootstrapVariant.Danger, false)), Is.False);
        Assert.That(store.SnapshotNewestFirst().Select(x => x.Text), Is.EqualTo(new[] { "one" }));
    }

    [Test]
    public void MarkAsReadReplacesSnapshotAndNoOpReadsReturnFalse()
    {
        var store = new BootstrapToastHistoryStore(3);
        var item = HistoryItem("one", false);
        store.Add(item);
        var previous = store.SnapshotNewestFirst()[0];

        Assert.That(store.MarkAsRead(item.Id), Is.True);
        var current = store.SnapshotNewestFirst()[0];

        Assert.Multiple((Action)(() =>
        {
            Assert.That(previous.IsRead, Is.False);
            Assert.That(current, Is.Not.SameAs(previous));
            Assert.That(current.IsRead, Is.True);
            Assert.That(store.UnreadCount, Is.Zero);
            Assert.That(store.MarkAsRead(item.Id), Is.False);
            Assert.That(store.MarkAsRead(Guid.NewGuid()), Is.False);
        }));
    }

    [Test]
    public void MarkAllCapacityClearAndRollbackReportOnlyEffectiveChanges()
    {
        var store = new BootstrapToastHistoryStore(4);
        var first = HistoryItem("first", false);
        var second = HistoryItem("second", true);
        var third = HistoryItem("third", false);
        store.Add(first);
        store.Add(second);
        store.Add(third);

        Assert.That(store.MarkAllAsRead(), Is.True);
        Assert.That(store.MarkAllAsRead(), Is.False);
        store.Capacity = 2;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(store.Count, Is.EqualTo(2));
            Assert.That(store.SnapshotNewestFirst().Select(x => x.Text), Is.EqualTo(new[] { "third", "second" }));
            Assert.That(store.Remove(first.Id), Is.False);
            Assert.That(store.Remove(third.Id), Is.True);
            Assert.That(store.Clear(), Is.True);
            Assert.That(store.Clear(), Is.False);
        }));
    }

    [Test]
    public void CapacityRejectsNonPositiveValuesWithoutMutation()
    {
        var store = new BootstrapToastHistoryStore(3);

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => store.Capacity = 0));
            Assert.That(store.Capacity, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapToastHistoryStore(-1)));
            Assert.Throws<ArgumentNullException>((Action)(() => store.Add(null!)));
        }));
    }

    private static BootstrapToastHistoryItem HistoryItem(string text, bool isRead)
    {
        return new BootstrapToastHistoryItem(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            text,
            text,
            BootstrapVariant.Primary,
            isRead);
    }
}
