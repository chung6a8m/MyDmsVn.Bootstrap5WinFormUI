using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
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

    private sealed class Item
    {
        internal Item(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
