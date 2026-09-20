using System;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapBreadcrumbItemCollectionTests
{
    [Test]
    public void ItemDefaultsAndConstructorPreserveValidCallerText()
    {
        var defaultItem = new BootstrapBreadcrumbItem();
        var spacedItem = new BootstrapBreadcrumbItem("  Home  ");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(defaultItem.Text, Is.EqualTo("Item"));
            Assert.That(defaultItem.Tag, Is.Null);
            Assert.That(spacedItem.Text, Is.EqualTo("  Home  "));
            Assert.That(spacedItem.Tag, Is.Null);
        }));
    }

    [Test]
    public void TextRejectsValuesThatCannotFormANativeAncestorLink()
    {
        Assert.That((Action)(() => _ = new BootstrapBreadcrumbItem(null!)), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => _ = new BootstrapBreadcrumbItem(string.Empty)), Throws.TypeOf<ArgumentException>());
        Assert.That((Action)(() => _ = new BootstrapBreadcrumbItem("   ")), Throws.TypeOf<ArgumentException>());

        var item = new BootstrapBreadcrumbItem("Home");

        Assert.Multiple((Action)(() =>
        {
            Assert.That((Action)(() => item.Text = null!), Throws.TypeOf<ArgumentNullException>());
            Assert.That((Action)(() => item.Text = string.Empty), Throws.TypeOf<ArgumentException>());
            Assert.That((Action)(() => item.Text = "   "), Throws.TypeOf<ArgumentException>());
            Assert.That(item.Text, Is.EqualTo("Home"));
        }));
    }

    [Test]
    public void TextNotificationOccursOnlyForAnEffectiveTextChange()
    {
        var item = new BootstrapBreadcrumbItem("Home");
        var notifications = 0;
        item.TextChangedForOwner += (_, _) => notifications++;

        item.Text = "Home";
        item.Tag = new object();
        item.Text = "Library";

        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void InternalConstructorRequiresBothCallbacks()
    {
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbItemCollection(null!, _ => { })),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            (Action)(() => _ = new BootstrapBreadcrumbItemCollection(() => { }, null!)),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void StructuralMutationsNotifyExactlyOnceAndEmptyClearIsNotEffective()
    {
        var structuralChanges = 0;
        var textChanges = 0;
        var items = new BootstrapBreadcrumbItemCollection(
            () => structuralChanges++,
            _ => textChanges++);
        var home = new BootstrapBreadcrumbItem("Home");
        var library = new BootstrapBreadcrumbItem("Library");
        var replacement = new BootstrapBreadcrumbItem("Replacement");

        items.Add(home);
        items.Insert(0, library);
        items[1] = replacement;
        items.RemoveAt(0);
        items.Clear();
        items.Clear();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(structuralChanges, Is.EqualTo(5));
            Assert.That(textChanges, Is.Zero);
        }));
    }

    [Test]
    public void TextChangesForwardWithoutStructuralNotification()
    {
        var structuralChanges = 0;
        BootstrapBreadcrumbItem? changedItem = null;
        var items = new BootstrapBreadcrumbItemCollection(
            () => structuralChanges++,
            item => changedItem = item);
        var home = new BootstrapBreadcrumbItem("Home");
        items.Add(home);
        structuralChanges = 0;

        home.Text = "Start";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(structuralChanges, Is.Zero);
            Assert.That(changedItem, Is.SameAs(home));
        }));
    }

    [Test]
    public void RemovedReplacedAndClearedItemsNoLongerNotifyOwner()
    {
        var textChanges = 0;
        var items = new BootstrapBreadcrumbItemCollection(
            () => { },
            _ => textChanges++);
        var removed = new BootstrapBreadcrumbItem("Removed");
        var replaced = new BootstrapBreadcrumbItem("Replaced");
        var cleared = new BootstrapBreadcrumbItem("Cleared");

        items.Add(removed);
        items.Remove(removed);
        items.Add(replaced);
        items[0] = cleared;
        items.Clear();

        removed.Text = "Removed 2";
        replaced.Text = "Replaced 2";
        cleared.Text = "Cleared 2";

        Assert.That(textChanges, Is.Zero);
    }

    [Test]
    public void NullAndDuplicateReferenceMutationsRollBackWithoutNotification()
    {
        var structuralChanges = 0;
        var textChanges = 0;
        var items = new BootstrapBreadcrumbItemCollection(
            () => structuralChanges++,
            _ => textChanges++);
        var home = new BootstrapBreadcrumbItem("Home");
        var library = new BootstrapBreadcrumbItem("Library");
        items.Add(home);
        items.Add(library);
        structuralChanges = 0;

        Assert.That((Action)(() => items.Add(null!)), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => items[0] = null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => items.Add(home)), Throws.TypeOf<ArgumentException>());
        Assert.That((Action)(() => items[1] = home), Throws.TypeOf<ArgumentException>());

        home.Text = "Start";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(items, Is.EqualTo(new[] { home, library }));
            Assert.That(structuralChanges, Is.Zero);
            Assert.That(textChanges, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DistinctItemsMayHaveEqualValuesAndCallerObjectsAreNotDisposed()
    {
        var tag = new DisposableProbe();
        var first = new BootstrapBreadcrumbItem("Home") { Tag = tag };
        var second = new BootstrapBreadcrumbItem("Home") { Tag = tag };
        var items = new BootstrapBreadcrumbItemCollection(() => { }, _ => { });

        items.Add(first);
        items.Add(second);
        items.Clear();
        first.Text = "Still owned by caller";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Text, Is.EqualTo("Still owned by caller"));
            Assert.That(tag.DisposeCount, Is.Zero);
        }));
    }

    [Test]
    public void CollectionDeclaresOnlyThePlannedProtectedMutationSurface()
    {
        var declaredProtectedMethods = typeof(BootstrapBreadcrumbItemCollection)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.IsFamily)
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();
        var publicConstructors = typeof(BootstrapBreadcrumbItemCollection)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                declaredProtectedMethods,
                Is.EqualTo(new[] { "ClearItems", "InsertItem", "RemoveItem", "SetItem" }));
            Assert.That(publicConstructors, Is.Empty);
        }));
    }

    [Test]
    public void EventArgsPreserveItemAndIndexAndRejectInvalidInput()
    {
        var item = new BootstrapBreadcrumbItem("Home");
        var args = new BootstrapBreadcrumbItemClickedEventArgs(item, 2);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(args.Item, Is.SameAs(item));
            Assert.That(args.Index, Is.EqualTo(2));
            Assert.That(
                (Action)(() => _ = new BootstrapBreadcrumbItemClickedEventArgs(null!, 0)),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                (Action)(() => _ = new BootstrapBreadcrumbItemClickedEventArgs(item, -1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }));
    }

    private sealed class DisposableProbe : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
