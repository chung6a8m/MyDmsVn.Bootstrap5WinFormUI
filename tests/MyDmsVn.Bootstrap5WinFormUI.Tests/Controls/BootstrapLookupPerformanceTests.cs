using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupPerformanceTests
{
    [TestCase(1000)]
    [TestCase(5000)]
    [TestCase(10000)]
    public void LargeLocalSourcesKeepDeterministicRankingAndSourceOrder(int count)
    {
        var source = new List<BootstrapLookupSourceItem>(count);
        for (var index = 0; index < count; index++)
        {
            var item = new Item(index, index % 100 == 0 ? "Cà phê đặc biệt" : "Sản phẩm " + index);
            source.Add(new BootstrapLookupSourceItem(item, item.Id, item.Name, index));
        }

        var result = BootstrapLookupSearchEngine.Search(source, "ca phe", 0,
            BootstrapLookupEmptyQueryBehavior.ShowAll, new[] { "Name" }, "Name", BootstrapLookupTextNormalization.NormalizeSearchText);

        Assert.That(result.Items, Has.Count.EqualTo((count - 1) / 100 + 1));
        Assert.That(result.Items.Select(item => item.SourceIndex), Is.Ordered);
    }

    [Test]
    public void ColumnFormattingBuildsValueDisplayIndexOnceAndRefreshesItOnSourceChange()
    {
        var products = new BindingList<CountingItem> { new CountingItem(1, "Alpha") };
        using var column = new BootstrapLookupColumn { DataSource = products, DisplayMember = "Name", ValueMember = "Id" };
        Assert.That(column.ResolveDisplayText(1), Is.EqualTo("Alpha"));
        CountingItem.PropertyReads = 0;

        for (var index = 0; index < 20; index++) Assert.That(column.ResolveDisplayText(1), Is.EqualTo("Alpha"));
        Assert.That(CountingItem.PropertyReads, Is.Zero, "Paint-time formatting must use the existing value-to-display index.");

        products.Add(new CountingItem(2, "Beta"));
        CountingItem.PropertyReads = 0;
        Assert.That(column.ResolveDisplayText(2), Is.EqualTo("Beta"));
        Assert.That(CountingItem.PropertyReads, Is.Zero, "The source-change callback should rebuild the index before formatting.");
    }

    private sealed class Item
    {
        internal Item(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }

    private sealed class CountingItem
    {
        private readonly int _id;
        private readonly string _name;
        internal CountingItem(int id, string name) { _id = id; _name = name; }
        internal static int PropertyReads { get; set; }
        public int Id { get { PropertyReads++; return _id; } }
        public string Name { get { PropertyReads++; return _name; } }
    }
}
