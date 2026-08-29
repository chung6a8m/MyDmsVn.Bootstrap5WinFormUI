using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectPagingTests
{
    [Test]
    public async Task NextPageAdvancesOnlyAfterSuccessAndDeduplicatesByValue()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var controller = CreateController();
        var generation = controller.BeginQuery("a", 20);
        var first = controller.LoadFirstPageAsync(provider, generation);
        provider.Complete("a", 1, new[] { new BootstrapSelectItem(1, "Alpha") { Group = "G" } }, hasMore: true);
        await first;

        var next = controller.LoadNextPageAsync(provider, generation);
        Assert.That(controller.CurrentPage, Is.EqualTo(1));
        provider.Complete("a", 2, new[]
        {
            new BootstrapSelectItem(1, "Alpha refreshed") { Group = "G" },
            new BootstrapSelectItem(2, "Beta") { Group = "G" }
        }, hasMore: false);
        Assert.That(await next, Is.True);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.CurrentPage, Is.EqualTo(2));
            Assert.That(controller.HasMore, Is.False);
            Assert.That(controller.LoadedItems.Select(x => x.Text), Is.EqualTo(new[] { "Alpha refreshed", "Beta" }));
            Assert.That(controller.Results.Rows.Count(x => x.Kind == BootstrapSelectResultRowKind.GroupHeader), Is.EqualTo(1));
        }));
    }

    [Test]
    public async Task LaterPageFailurePreservesPriorItemsAndRetryRequestsSamePage()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var controller = CreateController();
        var generation = controller.BeginQuery("x", 20);
        var first = controller.LoadFirstPageAsync(provider, generation);
        provider.Complete("x", 1, new[] { new BootstrapSelectItem(1, "One") }, hasMore: true);
        await first;

        var next = controller.LoadNextPageAsync(provider, generation);
        var error = new InvalidOperationException("page 2 failed");
        provider.Fail("x", 2, error);
        Assert.That(await next, Is.False);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.CurrentPage, Is.EqualTo(1));
            Assert.That(controller.LoadedItems.Select(x => x.Text), Is.EqualTo(new[] { "One" }));
            Assert.That(controller.FailedPage, Is.EqualTo(2));
            Assert.That(controller.Results.Rows.Last().Kind, Is.EqualTo(BootstrapSelectResultRowKind.LoadMoreError));
        }));

        var retry = controller.RetryLastFailureAsync(provider, generation);
        provider.Complete("x", 2, new[] { new BootstrapSelectItem(2, "Two") }, hasMore: false);
        Assert.That(await retry, Is.True);
        Assert.That(provider.Queries.Last().Page, Is.EqualTo(2));
        Assert.That(controller.LoadedItems.Select(x => x.Text), Is.EqualTo(new[] { "One", "Two" }));
    }

    [Test]
    public async Task HasMoreFalseSuppressesAdditionalRequests()
    {
        var provider = new BootstrapSelectImmediateProvider(_ => new BootstrapSelectPage(
            new[] { new BootstrapSelectItem(1, "Only") }, hasMore: false));
        using var controller = CreateController();
        var generation = controller.BeginQuery(string.Empty, 20);
        await controller.LoadFirstPageAsync(provider, generation);

        Assert.That(await controller.LoadNextPageAsync(provider, generation), Is.False);
        Assert.That(provider.Queries, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DuplicateValueRefreshesSelectedSnapshotWithoutLogicalSelectionMutation()
    {
        var refreshed = new List<BootstrapSelectItem>();
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var controller = new BootstrapSelectSearchController(
            EqualityComparer<object>.Default,
            item => Equals(item.Value, 1),
            item => { if (Equals(item.Value, 1)) refreshed.Add(item); });
        var generation = controller.BeginQuery("a", 20);
        var first = controller.LoadFirstPageAsync(provider, generation);
        provider.Complete("a", 1, new[] { new BootstrapSelectItem(1, "Old") }, hasMore: true);
        await first;
        var next = controller.LoadNextPageAsync(provider, generation);
        var updated = new BootstrapSelectItem(1, "Updated");
        provider.Complete("a", 2, new[] { updated }, hasMore: false);
        await next;

        Assert.That(refreshed.Last(), Is.SameAs(updated));
        Assert.That(controller.LoadedItems.Single().Text, Is.EqualTo("Updated"));
    }

    private static BootstrapSelectSearchController CreateController()
    {
        return new BootstrapSelectSearchController(EqualityComparer<object>.Default, _ => false, _ => { });
    }
}
