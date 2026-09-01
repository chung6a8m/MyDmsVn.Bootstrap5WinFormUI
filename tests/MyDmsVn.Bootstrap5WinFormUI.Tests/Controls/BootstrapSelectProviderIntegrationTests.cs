using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectProviderIntegrationTests
{
    [Test]
    public void FirstPageCompletionReflowsOpenPopupFromLoadingHeight()
    {
        RunOnIsolatedWinFormsThread(FirstPageCompletionReflowsOpenPopupFromLoadingHeightCore);
    }

    private static void FirstPageCompletionReflowsOpenPopupFromLoadingHeightCore()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var form = CreatePopupSizingForm();
        using var select = CreatePopupSizingSelect(provider);
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        using var winFormsContext = new WindowsFormsSynchronizationContext();
        using var synchronizationContext = new SynchronizationContextScope(winFormsContext);

        select.OpenDropDownInternal();
        PumpUntil(() => provider.Queries.Count == 1, TimeSpan.FromSeconds(5));
        var loadingHeight = select.DropDownBoundsForTest.Height;

        provider.Complete(string.Empty, 1, CreateItems("First page", 20));
        PumpUntil(() => select.VisibleResultItemTextsForTest.Count == 20, TimeSpan.FromSeconds(5));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(20));
            Assert.That(select.DropDownBoundsForTest.Height, Is.GreaterThan(loadingHeight));
        }));
    }

    [Test]
    public void SearchCompletionReflowsOpenPopupForTwentyRaceMatches()
    {
        RunOnIsolatedWinFormsThread(SearchCompletionReflowsOpenPopupForTwentyRaceMatchesCore);
    }

    private static void SearchCompletionReflowsOpenPopupForTwentyRaceMatchesCore()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var form = CreatePopupSizingForm();
        using var select = CreatePopupSizingSelect(provider);
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        using var winFormsContext = new WindowsFormsSynchronizationContext();
        using var synchronizationContext = new SynchronizationContextScope(winFormsContext);

        select.OpenDropDownInternal();
        PumpUntil(() => provider.Queries.Count == 1, TimeSpan.FromSeconds(5));
        provider.Complete(string.Empty, 1, CreateItems("Initial", 5));
        PumpUntil(() => select.VisibleResultItemTextsForTest.Count == 5, TimeSpan.FromSeconds(5));

        select.SetSearchTextForTest("race");
        PumpUntil(
            () => provider.Queries.Any(query => query.SearchText == "race" && query.Page == 1),
            TimeSpan.FromSeconds(5));
        var loadingHeight = select.DropDownBoundsForTest.Height;

        provider.Complete("race", 1, CreateItems("Race sample", 20));
        PumpUntil(() => select.VisibleResultItemTextsForTest.Count == 20, TimeSpan.FromSeconds(5));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(20));
            Assert.That(select.DropDownBoundsForTest.Height, Is.GreaterThan(loadingHeight));
        }));
    }

    [Test]
    public void AsyncProviderMergeAndSelectedSnapshotRefreshStayOnOwningUiThread()
    {
        RunOnIsolatedWinFormsThread(AsyncProviderMergeAndSelectedSnapshotRefreshStayOnOwningUiThreadCore);
    }

    private static void AsyncProviderMergeAndSelectedSnapshotRefreshStayOnOwningUiThreadCore()
    {
        var uiThreadId = Thread.CurrentThread.ManagedThreadId;
        var comparer = new RecordingComparer();
        var provider = new ThreadPoolProvider(new BootstrapSelectItem("id", "Updated"));
        using var form = new Form();
        using var select = new BootstrapSelect
        {
            SearchDebounce = TimeSpan.Zero,
            ValueComparer = comparer
        };
        select.SelectedItem = new BootstrapSelectItem("id", "Old");
        comparer.Clear();
        select.DataProvider = provider;
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        using var winFormsContext = new WindowsFormsSynchronizationContext();
        using var synchronizationContext = new SynchronizationContextScope(winFormsContext);

        var completed = 0;
        var failed = 0;
        select.SearchCompleted += (_, _) => completed++;
        select.SearchFailed += (_, _) => failed++;

        select.OpenDropDownInternal();
        PumpUntil(() => completed + failed > 0, TimeSpan.FromSeconds(5));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(provider.WorkerThreadId, Is.Not.EqualTo(uiThreadId));
            Assert.That(comparer.ThreadIds, Is.Not.Empty);
            Assert.That(comparer.ThreadIds, Has.All.EqualTo(uiThreadId));
            Assert.That(select.SelectedItem!.Text, Is.EqualTo("Updated"));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(failed, Is.Zero);
        }));
    }

    [Test]
    public void ReplacingProviderClearsOldRowsAndIgnoresLateCompletion()
    {
        RunOnIsolatedWinFormsThread(ReplacingProviderClearsOldRowsAndIgnoresLateCompletionCore);
    }

    private static void ReplacingProviderClearsOldRowsAndIgnoresLateCompletionCore()
    {
        var providerA = new BootstrapSelectControlledProvider(honorCancellation: false);
        var providerB = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var form = new Form();
        using var select = new BootstrapSelect { SearchDebounce = TimeSpan.FromMilliseconds(100), DataProvider = providerA };
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        using var winFormsContext = new WindowsFormsSynchronizationContext();
        using var synchronizationContext = new SynchronizationContextScope(winFormsContext);

        select.OpenDropDownInternal();
        PumpUntil(() => providerA.Queries.Count == 1, TimeSpan.FromSeconds(5));
        providerA.Complete(string.Empty, 1, new[] { new BootstrapSelectItem("old", "Provider A") }, hasMore: true);
        PumpUntil(() => select.VisibleResultItemTextsForTest.SequenceEqual(new[] { "Provider A" }), TimeSpan.FromSeconds(5));

        select.RequestRemoteNextPage();
        PumpUntil(() => providerA.Queries.Count == 2, TimeSpan.FromSeconds(5));
        select.DataProvider = providerB;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.VisibleResultItemTextsForTest, Is.Empty);
            Assert.That(select.ActivateHighlightedResultForTest(), Is.False);
        }));

        providerA.Complete(string.Empty, 2, new[] { new BootstrapSelectItem("late", "Late Provider A") });
        PumpUntil(() => providerB.Queries.Count == 1, TimeSpan.FromSeconds(5));
        Assert.That(select.VisibleResultItemTextsForTest, Is.Empty);

        providerB.Complete(string.Empty, 1, new[] { new BootstrapSelectItem("new", "Provider B") });
        PumpUntil(() => select.VisibleResultItemTextsForTest.SequenceEqual(new[] { "Provider B" }), TimeSpan.FromSeconds(5));

        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Assert.That(select.SelectedValue, Is.EqualTo("new"));
    }

    [Test]
    public void ReplacingValueComparerRestartsPagingAndUsesNewIdentityRules()
    {
        RunOnIsolatedWinFormsThread(ReplacingValueComparerRestartsPagingAndUsesNewIdentityRulesCore);
    }

    private static void ReplacingValueComparerRestartsPagingAndUsesNewIdentityRulesCore()
    {
        var provider = new BootstrapSelectControlledProvider(honorCancellation: false);
        using var form = new Form();
        using var select = new BootstrapSelect
        {
            SelectionMode = BootstrapSelectMode.Multiple,
            SearchDebounce = TimeSpan.Zero,
            DataProvider = provider
        };
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        using var winFormsContext = new WindowsFormsSynchronizationContext();
        using var synchronizationContext = new SynchronizationContextScope(winFormsContext);

        select.OpenDropDownInternal();
        PumpUntil(() => provider.Queries.Count == 1, TimeSpan.FromSeconds(5));
        provider.Complete(string.Empty, 1, new[] { new BootstrapSelectItem("ABC", "Upper") }, hasMore: true);
        PumpUntil(() => select.VisibleResultItemTextsForTest.SequenceEqual(new[] { "Upper" }), TimeSpan.FromSeconds(5));
        select.SelectedItem = new BootstrapSelectItem("ABC", "Selected snapshot");
        Assert.That(select.IsDropDownOpenForTest, Is.True);

        var queryCountBeforeComparerChange = provider.Queries.Count;
        select.ValueComparer = new OrdinalIgnoreCaseObjectComparer();
        PumpUntil(
            () => provider.Queries.Count > queryCountBeforeComparerChange && provider.Queries.Last().Page == 1,
            TimeSpan.FromSeconds(5));
        var restartedPageOneIndex = provider.Queries.Count - 1;
        provider.Complete(string.Empty, 1, new[] { new BootstrapSelectItem("ABC", "Upper refreshed") }, hasMore: true);
        PumpUntil(() => select.VisibleResultItemTextsForTest.SequenceEqual(new[] { "Upper refreshed" }), TimeSpan.FromSeconds(5));

        select.RequestRemoteNextPage();
        PumpUntil(
            () => provider.Queries.Skip(restartedPageOneIndex + 1).Any(query => query.Page == 2),
            TimeSpan.FromSeconds(5));
        provider.Complete(string.Empty, 2, new[] { new BootstrapSelectItem("abc", "Lower refreshed") });
        PumpUntil(() => select.VisibleResultItemTextsForTest.SequenceEqual(new[] { "Lower refreshed" }), TimeSpan.FromSeconds(5));

        Assert.That(select.SelectedItems, Has.Count.EqualTo(1));
        Assert.That(select.SelectedItem!.Text, Is.EqualTo("Lower refreshed"));
        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Assert.That(select.SelectedItems, Is.Empty);
    }

    private static void RunOnIsolatedWinFormsThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                failure = error;
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.That(thread.Join(TimeSpan.FromSeconds(30)), Is.True, "Isolated WinForms integration thread timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void PumpUntil(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < timeout)
        {
            Application.DoEvents();
            Thread.Yield();
        }

        Assert.That(condition(), Is.True, "Timed out while pumping the WinForms message loop.");
    }

    private static Form CreatePopupSizingForm()
    {
        return new Form
        {
            Size = new Size(800, 700),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100)
        };
    }

    private static BootstrapSelect CreatePopupSizingSelect(IBootstrapSelectDataProvider provider)
    {
        return new BootstrapSelect
        {
            Location = new Point(40, 40),
            Width = 340,
            SearchDebounce = TimeSpan.Zero,
            PageSize = 20,
            DataProvider = provider
        };
    }

    private static IEnumerable<BootstrapSelectItem> CreateItems(string prefix, int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new BootstrapSelectItem(prefix + "-" + index, prefix + " " + index.ToString("00")));
    }

    private sealed class ThreadPoolProvider : IBootstrapSelectDataProvider
    {
        private readonly BootstrapSelectItem _item;

        internal ThreadPoolProvider(BootstrapSelectItem item)
        {
            _item = item;
        }

        internal int WorkerThreadId { get; private set; }

        public Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WorkerThreadId = Thread.CurrentThread.ManagedThreadId;
                return new BootstrapSelectPage(new[] { _item }, hasMore: false);
            }, cancellationToken);
        }
    }

    private sealed class SynchronizationContextScope : IDisposable
    {
        private readonly SynchronizationContext? _previous;

        internal SynchronizationContextScope(SynchronizationContext current)
        {
            _previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(current);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
        }
    }

    private sealed class RecordingComparer : IEqualityComparer<object>
    {
        private readonly object _gate = new object();
        private readonly List<int> _threadIds = new List<int>();

        internal IReadOnlyList<int> ThreadIds
        {
            get
            {
                lock (_gate) return _threadIds.ToArray();
            }
        }

        public new bool Equals(object? x, object? y)
        {
            lock (_gate) _threadIds.Add(Thread.CurrentThread.ManagedThreadId);
            return EqualityComparer<object>.Default.Equals(x!, y!);
        }

        public int GetHashCode(object value)
        {
            return EqualityComparer<object>.Default.GetHashCode(value);
        }

        internal void Clear()
        {
            lock (_gate) _threadIds.Clear();
        }
    }

    private sealed class OrdinalIgnoreCaseObjectComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            return string.Equals(x as string, y as string, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(object value)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode((string)value);
        }
    }
}
