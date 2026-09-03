using System;
using System.Collections.Generic;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapLookupSearchEngineTests
{
    [TestCase("Cà phê sữa", "ca phe sua")]
    [TestCase("Đường trắng", "duong trang")]
    [TestCase("  ÁO   ĐỎ  ", "ao   do")]
    public void DefaultSearchNormalizerIsVietnameseFriendly(string input, string expected)
    {
        Assert.That(BootstrapLookupTextNormalization.NormalizeSearchText(input), Is.EqualTo(expected));
    }

    [Test]
    public void EmptyAndMinimumLengthStatesAreDistinct()
    {
        var source = Source(new Product(1, "CF", "Coffee"), new Product(2, "TE", "Tea"));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Search(source, "", 0, BootstrapLookupEmptyQueryBehavior.ShowAll).Items.Select(x => x.Value), Is.EqualTo(new object[] { 1, 2 }));
            Assert.That(Search(source, "", 0, BootstrapLookupEmptyQueryBehavior.ShowNone).Items, Is.Empty);
            Assert.That(Search(source, "c", 2, BootstrapLookupEmptyQueryBehavior.ShowAll).State, Is.EqualTo(BootstrapLookupSearchState.WaitingForMinimumLength));
        }));
    }

    [Test]
    public void TokensCanMatchDifferentMembersButEveryTokenMustMatch()
    {
        var source = Source(new Product(1, "CF001", "Cà phê rang"), new Product(2, "CF002", "Trà xanh"));
        var result = Search(source, "cf rang", 0, BootstrapLookupEmptyQueryBehavior.ShowAll);
        Assert.That(result.Items.Select(x => x.Value), Is.EqualTo(new object[] { 1 }));
    }

    [Test]
    public void AggregateRankingPrefersStrongWorstTokenOverOneExactAndOneWeakToken()
    {
        var source = Source(
            new Product(1, "red", "contains blue somewhere"),
            new Product(2, "redwood", "blueberry"));

        var result = Search(source, "red blue", 0, BootstrapLookupEmptyQueryBehavior.ShowAll);

        Assert.That(result.Items.Select(x => x.Value), Is.EqualTo(new object[] { 2, 1 }));
    }

    [Test]
    public void RankingUsesDisplayThenMemberPriorityThenSourceOrder()
    {
        var source = Source(
            new Product(1, "match", "other"),
            new Product(2, "other", "match"),
            new Product(3, "match", "other"));

        var result = Search(source, "match", 0, BootstrapLookupEmptyQueryBehavior.ShowAll);
        Assert.That(result.Items.Select(x => x.Value), Is.EqualTo(new object[] { 2, 1, 3 }));
    }

    [Test]
    public void EmptySearchMembersFallBackToDisplayMember()
    {
        var source = Source(new Product(1, "hidden-hit", "Visible miss"));
        var result = BootstrapLookupSearchEngine.Search(source, "hidden", 0, BootstrapLookupEmptyQueryBehavior.ShowAll,
            Array.Empty<string>(), "Name", BootstrapLookupTextNormalization.NormalizeSearchText);
        Assert.That(result.Items, Is.Empty);
    }

    private static BootstrapLookupSearchResult Search(IReadOnlyList<BootstrapLookupSourceItem> source, string query, int minimum, BootstrapLookupEmptyQueryBehavior empty)
        => BootstrapLookupSearchEngine.Search(source, query, minimum, empty, new[] { "Code", "Name" }, "Name", BootstrapLookupTextNormalization.NormalizeSearchText);

    private static IReadOnlyList<BootstrapLookupSourceItem> Source(params Product[] products)
        => products.Select((item, index) => new BootstrapLookupSourceItem(item, item.Id, item.Name, index)).ToArray();

    private sealed class Product
    {
        internal Product(int id, string code, string name) { Id = id; Code = code; Name = name; }
        public int Id { get; }
        public string Code { get; }
        public string Name { get; }
    }
}
