using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectFirstPageRetryTests
{
    [Test]
    public async Task FirstPageFailureRetriesTheSameLogicalPage()
    {
        var provider = new FailFirstProvider();
        using var controller = new BootstrapSelectSearchController(
            EqualityComparer<object>.Default,
            _ => false,
            _ => { });
        var generation = controller.BeginQuery("alpha", 10);

        await controller.LoadFirstPageAsync(provider, generation);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(controller.FailedPage, Is.EqualTo(1));
            Assert.That(controller.CurrentPage, Is.EqualTo(0));
            Assert.That(controller.LastError, Is.Not.Null);
        }));

        var retried = await controller.RetryLastFailureAsync(provider, generation);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(retried, Is.True);
            Assert.That(provider.CallCount, Is.EqualTo(2));
            Assert.That(provider.Pages, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(controller.CurrentPage, Is.EqualTo(1));
            Assert.That(controller.FailedPage, Is.EqualTo(0));
            Assert.That(controller.LastError, Is.Null);
            Assert.That(controller.LoadedItems, Has.Count.EqualTo(1));
            Assert.That(controller.LoadedItems[0].Text, Is.EqualTo("Recovered"));
        }));
    }

    [Test]
    public void FirstPageErrorRowIsKeyboardActionable()
    {
        using var view = new BootstrapSelectResultsView();
        BootstrapSelectResultRow? activated = null;
        view.RowActivated += (row, _) => activated = row;
        view.SetResults(BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Error, "Retry search"));

        var highlighted = view.MoveToFirst();
        var activationAccepted = view.ActivateHighlighted(BootstrapSelectChangeReason.Keyboard);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(highlighted || view.HighlightedIndex == 0, Is.True);
            Assert.That(view.HighlightedIndex, Is.EqualTo(0));
            Assert.That(activationAccepted, Is.True);
            Assert.That(activated?.Kind, Is.EqualTo(BootstrapSelectResultRowKind.Error));
        }));
    }

    private sealed class FailFirstProvider : IBootstrapSelectDataProvider
    {
        internal int CallCount { get; private set; }
        internal List<int> Pages { get; } = new List<int>();

        public Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            Pages.Add(query.Page);
            if (CallCount == 1)
            {
                throw new InvalidOperationException("First page failed.");
            }

            return Task.FromResult(new BootstrapSelectPage(
                new[] { new BootstrapSelectItem(1, "Recovered") },
                hasMore: false));
        }
    }
}
