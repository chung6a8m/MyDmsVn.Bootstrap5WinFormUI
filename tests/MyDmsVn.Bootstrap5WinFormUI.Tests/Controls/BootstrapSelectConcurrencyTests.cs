using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectConcurrencyTests
{
    [Test]
    public async Task StaleCancellationIgnoringGenerationCannotReplaceNewerResults()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var controller = new BootstrapSelectSearchController(EqualityComparer<object>.Default, _ => false, _ => { });

        var generationA = controller.BeginQuery("a", 20);
        var taskA = controller.LoadFirstPageAsync(provider, generationA);
        var generationAb = controller.BeginQuery("ab", 20);
        var taskAb = controller.LoadFirstPageAsync(provider, generationAb);

        provider.Complete("ab", 1, new[] { new BootstrapSelectItem(2, "AB result") });
        await taskAb;
        provider.Complete("a", 1, new[] { new BootstrapSelectItem(1, "stale A") });
        await taskA;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.SearchText, Is.EqualTo("ab"));
            Assert.That(controller.LoadedItems.Select(x => x.Text), Is.EqualTo(new[] { "AB result" }));
        }));
    }

    [Test]
    public async Task NonCancellationFailureIsPublishedOnlyForCurrentGeneration()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var controller = new BootstrapSelectSearchController(EqualityComparer<object>.Default, _ => false, _ => { });
        var generation = controller.BeginQuery("boom", 20);
        var task = controller.LoadFirstPageAsync(provider, generation);
        var error = new InvalidOperationException("provider failed");

        provider.Fail("boom", 1, error);
        await task;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.LastError, Is.SameAs(error));
            Assert.That(controller.Results.Rows.Single().Kind, Is.EqualTo(BootstrapSelectResultRowKind.Error));
        }));
    }
}
