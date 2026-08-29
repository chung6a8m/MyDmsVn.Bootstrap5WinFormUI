using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectSearchControllerTests
{
    [Test]
    public void QueryAndPageContractsValidateAndSnapshotInputs()
    {
        Assert.That((Action)(() => new BootstrapSelectQuery(null!, 1, 20)), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => new BootstrapSelectQuery("a", 0, 20)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => new BootstrapSelectQuery("a", 1, 0)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That((Action)(() => new BootstrapSelectPage(null!, false)), Throws.TypeOf<ArgumentNullException>());

        var source = new List<BootstrapSelectItem> { new BootstrapSelectItem(1, "Alpha") };
        var page = new BootstrapSelectPage(source, true);
        source.Clear();
        var query = new BootstrapSelectQuery("abc", 2, 30);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(query.SearchText, Is.EqualTo("abc"));
            Assert.That(query.Page, Is.EqualTo(2));
            Assert.That(query.PageSize, Is.EqualTo(30));
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.HasMore, Is.True);
        }));
    }

    [Test]
    public void NewLogicalQueryCancelsPreviousRequest()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: true);
        using var controller = CreateController();
        var generationA = controller.BeginQuery("a", 20);
        var taskA = controller.LoadFirstPageAsync(provider, generationA);
        Assert.That(provider.Tokens[0].IsCancellationRequested, Is.False);

        controller.BeginQuery("ab", 20);
        Assert.That(provider.Tokens[0].IsCancellationRequested, Is.True);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await taskA);
    }

    [Test]
    public async Task SuccessfulFirstPagePublishesNormalizedResults()
    {
        var provider = new BootstrapSelectImmediateProvider(query => new BootstrapSelectPage(
            new[] { new BootstrapSelectItem(1, "Alpha"), new BootstrapSelectItem(2, "Beta") },
            hasMore: true));
        using var controller = CreateController();
        var generation = controller.BeginQuery("a", 20);

        await controller.LoadFirstPageAsync(provider, generation);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.SearchText, Is.EqualTo("a"));
            Assert.That(controller.CurrentPage, Is.EqualTo(1));
            Assert.That(controller.HasMore, Is.True);
            Assert.That(controller.LoadedItems.Select(x => x.Text), Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(controller.Results.Rows.Count, Is.EqualTo(2));
            Assert.That(controller.LastError, Is.Null);
        }));
    }

    private static BootstrapSelectSearchController CreateController()
    {
        return new BootstrapSelectSearchController(EqualityComparer<object>.Default, _ => false, _ => { });
    }
}
