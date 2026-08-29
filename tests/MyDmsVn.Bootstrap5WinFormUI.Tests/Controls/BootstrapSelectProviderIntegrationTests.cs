using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectProviderIntegrationTests
{
    [Test]
    public void AsyncProviderMergeAndSelectedSnapshotRefreshStayOnOwningUiThread()
    {
        var uiThreadId = Thread.CurrentThread.ManagedThreadId;
        using var synchronizationContext = new SynchronizationContextScope(new WindowsFormsSynchronizationContext());
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
        private readonly IDisposable? _owned;

        internal SynchronizationContextScope(SynchronizationContext current)
        {
            _previous = SynchronizationContext.Current;
            _owned = current as IDisposable;
            SynchronizationContext.SetSynchronizationContext(current);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
            _owned?.Dispose();
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
}
