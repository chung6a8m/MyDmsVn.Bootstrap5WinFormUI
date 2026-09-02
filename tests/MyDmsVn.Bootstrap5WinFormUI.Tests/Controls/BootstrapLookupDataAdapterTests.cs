using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapLookupDataAdapterTests
{
    [Test]
    public void SnapshotSupportsLocalSourceShapesAndPreservesBindingSourceCurrency()
    {
        var list = new BindingList<Product> { new(2, "Tea"), new(1, "Coffee") };
        using var source = new BindingSource { DataSource = list, Position = 1 };
        using var adapter = new BootstrapLookupDataAdapter(source, "Name", "Id");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(adapter.Snapshot.Select(item => item.Value), Is.EqualTo(new object[] { 2, 1 }));
            Assert.That(adapter.Snapshot.Select(item => item.DisplayText), Is.EqualTo(new[] { "Tea", "Coffee" }));
            Assert.That(source.Position, Is.EqualTo(1));
        }));

        using var arrayAdapter = new BootstrapLookupDataAdapter(new[] { "A", "B" }, "", "");
        Assert.That(arrayAdapter.Snapshot.Select(item => item.DisplayText), Is.EqualTo(new[] { "A", "B" }));

        using var listAdapter = new BootstrapLookupDataAdapter(new List<Product>(list), "Name", "Id");
        Assert.That(listAdapter.Snapshot.Count, Is.EqualTo(2));
    }

    [Test]
    public void InvalidMemberFailsWhenMetadataIsAvailableAndNullDisplayBecomesEmpty()
    {
        var items = new[] { new Product(1, null) };
        using var adapter = new BootstrapLookupDataAdapter(items, "Name", "Id");
        Assert.That(adapter.Snapshot[0].DisplayText, Is.Empty);
        Assert.That((Action)(() => new BootstrapLookupDataAdapter(items, "Missing", "Id")), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void FindByValueDistinguishesAFoundNullFromMissing()
    {
        var first = new Product(null, "No id");
        using var adapter = new BootstrapLookupDataAdapter(new[] { first }, "Name", "Id");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(adapter.TryFindByValue(null, out var found), Is.True);
            Assert.That(found!.Item, Is.SameAs(first));
            Assert.That(adapter.TryFindByValue(99, out _), Is.False);
        }));
    }

    [Test]
    public void AddUsesThePublicBindingSourceBoundaryAndRefreshesSnapshot()
    {
        var list = new BindingList<Product>();
        using var source = new BindingSource { DataSource = list };
        using var adapter = new BootstrapLookupDataAdapter(source, "Name", "Id");
        var product = new Product(3, "Milk");

        Assert.That(adapter.CanAdd, Is.True);
        adapter.Add(product);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0], Is.SameAs(product));
            Assert.That(adapter.TryFindByValue(3, out var found), Is.True);
            Assert.That(found!.Item, Is.SameAs(product));
        }));

        using var arrayAdapter = new BootstrapLookupDataAdapter(Array.Empty<Product>(), "Name", "Id");
        Assert.That(arrayAdapter.CanAdd, Is.False);
    }

    [Test]
    public void SourceChangesNotifyUntilAdapterIsDisposed()
    {
        var list = new BindingList<Product>();
        var adapter = new BootstrapLookupDataAdapter(list, "Name", "Id");
        var changes = 0;
        adapter.SourceChanged += (_, _) => changes++;

        list.Add(new Product(1, "Coffee"));
        Assert.That(changes, Is.EqualTo(1));
        adapter.Dispose();
        list.Add(new Product(2, "Tea"));
        Assert.That(changes, Is.EqualTo(1));
    }

    private sealed class Product
    {
        internal Product(object? id, string? name) { Id = id; Name = name; }
        public object? Id { get; }
        public string? Name { get; }
    }
}
