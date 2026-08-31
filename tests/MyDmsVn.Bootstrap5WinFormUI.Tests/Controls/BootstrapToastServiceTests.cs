using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastServiceTests
{
    [Test]
    public void DefaultsValidationAndDisposeContractAreDeterministic()
    {
        using var service = CreateService(out _, out _);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(service.Placement, Is.EqualTo(BootstrapToastPlacement.TopRight));
            Assert.That(service.ToastSpacing, Is.EqualTo(8));
            Assert.That(service.MaximumVisibleToasts, Is.EqualTo(5));
            Assert.That(service.ToastWidth, Is.EqualTo(320));
            Assert.That(service.ScreenMargin, Is.EqualTo(new Padding(16)));
            Assert.That(service.TopMost, Is.False);
            Assert.That(service.HistoryCapacity, Is.EqualTo(100));
            Assert.That(service.IconRenderer, Is.Not.Null);
            Assert.That(service.UnreadCount, Is.Zero);
            Assert.That(service.IsNotificationCenterVisible, Is.False);
        }));

        service.ToastWidth = 400;
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.Placement = (BootstrapToastPlacement)999));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.ToastSpacing = -1));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.MaximumVisibleToasts = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.ToastWidth = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.ScreenMargin = new Padding(-1)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => service.HistoryCapacity = 0));
            Assert.Throws<ArgumentNullException>((Action)(() => service.IconRenderer = null!));
            Assert.That(service.ToastWidth, Is.EqualTo(400));
        }));
    }

    [Test]
    public void ConstructorAndPublicOperationsAreBoundToCreatingStaThread()
    {
        using var service = CreateService(out _, out _);
        Exception? wrongThread = null;
        var thread = new Thread(() =>
        {
            try
            {
                service.GetHistory();
            }
            catch (Exception ex)
            {
                wrongThread = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        Exception? constructorFailure = null;
        var constructorThread = new Thread(() =>
        {
            try
            {
                using var ignored = new BootstrapToastService();
            }
            catch (Exception ex)
            {
                constructorFailure = ex;
            }
        });
        constructorThread.SetApartmentState(ApartmentState.MTA);
        constructorThread.Start();
        constructorThread.Join();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(wrongThread, Is.TypeOf<InvalidOperationException>());
            Assert.That(constructorFailure, Is.TypeOf<InvalidOperationException>());
        }));
    }

    [Test]
    public void FrameworkCallbackPostsToCreatingUiThreadAndDisposedGuardDropsLateWork()
    {
        var service = CreateService(out _, out _);
        var uiThread = Thread.CurrentThread.ManagedThreadId;
        var callbackThread = 0;
        var callbackCount = 0;
        var worker = new Thread(() => service.PostFrameworkCallbackToUiForTests(() =>
        {
            callbackThread = Thread.CurrentThread.ManagedThreadId;
            callbackCount++;
        }));
        worker.Start();
        worker.Join();
        PumpUntil(() => callbackCount == 1);

        var lateWorker = new Thread(() => service.PostFrameworkCallbackToUiForTests(() => callbackCount++));
        lateWorker.Start();
        lateWorker.Join();
        service.Dispose();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(callbackThread, Is.EqualTo(uiThread));
            Assert.That(callbackCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DefaultIsLazyRecreatableAndIndependentFromManualService()
    {
        var first = BootstrapToastService.Default;
        using var manual = new BootstrapToastService();
        Assert.That(manual, Is.Not.SameAs(first));

        first.Dispose();
        var second = BootstrapToastService.Default;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.GetHistory(), Is.Empty);
        }));
        second.Dispose();
    }

    [Test]
    public void ShowRoutesByDeviceAndUsesExplicitScreenDpiForWidth()
    {
        using var service = CreateService(out var resolver, out var factory);
        resolver.Resolved = new BootstrapToastScreenInfo("A", new Rectangle(0, 0, 1000, 800), 96);
        var firstId = service.Show(new BootstrapToastOptions { Text = "first", IncludeInHistory = false });
        resolver.Resolved = new BootstrapToastScreenInfo("A", new Rectangle(0, 0, 1000, 800), 144);
        var secondId = service.Show(new BootstrapToastOptions { Text = "second", IncludeInHistory = false });
        resolver.Resolved = new BootstrapToastScreenInfo("B", new Rectangle(1000, 0, 500, 800), 144);
        service.Show(new BootstrapToastOptions { Text = "third", IncludeInHistory = false });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(firstId, Is.Not.EqualTo(secondId));
            Assert.That(factory.Hosts.Count, Is.EqualTo(2));
            Assert.That(factory.Hosts[0].Toasts.Select(toast => toast.Width), Is.EqualTo(new[] { 320, 480 }));
            Assert.That(factory.Hosts[1].Toasts.Single().Width, Is.EqualTo(452));
        }));
    }

    [Test]
    public void ShowCommitsTransferThenRefreshesCenterBeforePublicEvent()
    {
        var order = new List<string>();
        using var service = CreateService(out _, out var factory, () => order.Add("refresh"));
        factory.OnShow = _ => order.Add("transfer");
        service.HistoryChanged += (_, _) => order.Add("event");

        var id = service.Show(new BootstrapToastOptions
        {
            Title = "Saved",
            Text = "Order saved",
            Variant = BootstrapVariant.Success
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(order, Is.EqualTo(new[] { "transfer", "refresh", "event" }));
            Assert.That(service.GetHistory().Single().Id, Is.EqualTo(id));
            Assert.That(service.UnreadCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void TransferFailureRollsBackHistoryAndThrowingSubscriberDoesNotRollBackCommit()
    {
        using var failedService = CreateService(out _, out var failedFactory);
        failedFactory.ThrowOnShow = true;
        var events = 0;
        failedService.HistoryChanged += (_, _) => events++;

        Assert.Throws<InvalidOperationException>((Action)(() => failedService.Show("failure")));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(failedService.GetHistory(), Is.Empty);
            Assert.That(events, Is.Zero);
        }));

        using var committedService = CreateService(out _, out var committedFactory);
        committedService.HistoryChanged += (_, _) => throw new ApplicationException("subscriber");
        Assert.Throws<ApplicationException>((Action)(() => committedService.Show("committed")));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(committedFactory.Hosts.Single().Toasts.Count, Is.EqualTo(1));
            Assert.That(committedService.GetHistory().Single().Text, Is.EqualTo("committed"));
        }));
    }

    [Test]
    public void TransferFailureAtCapacityPreservesExistingHistoryTransaction()
    {
        using var service = CreateService(out _, out var factory);
        service.HistoryCapacity = 2;
        var firstId = service.Show("first");
        var secondId = service.Show("second");
        var beforeFailure = service.GetHistory();
        var historyChangedCount = 0;
        service.HistoryChanged += (_, _) => historyChangedCount++;
        factory.ThrowOnShow = true;

        Assert.Throws<InvalidOperationException>((Action)(() => service.Show("third")));

        var afterFailure = service.GetHistory();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(afterFailure.Select(item => item.Id), Is.EqualTo(new[] { secondId, firstId }));
            Assert.That(afterFailure.Select(item => item.Text), Is.EqualTo(beforeFailure.Select(item => item.Text)));
            Assert.That(afterFailure.Select(item => item.IsRead), Is.EqualTo(beforeFailure.Select(item => item.IsRead)));
            Assert.That(service.UnreadCount, Is.EqualTo(2));
            Assert.That(historyChangedCount, Is.Zero);
        }));
    }

    [Test]
    public void HistoryMutationsPublishOnlyEffectiveChangesAfterRefresh()
    {
        var order = new List<string>();
        using var service = CreateService(out _, out _, () => order.Add("refresh"));
        service.HistoryChanged += (_, _) => order.Add("event");
        var first = service.Show("first");
        service.Show("second");
        order.Clear();

        Assert.That(service.MarkAsRead(first), Is.True);
        Assert.That(service.MarkAsRead(first), Is.False);
        service.MarkAllAsRead();
        service.MarkAllAsRead();
        service.HistoryCapacity = 1;
        service.ClearHistory();
        service.ClearHistory();

        Assert.That(order, Is.EqualTo(new[]
        {
            "refresh", "event",
            "refresh", "event",
            "refresh", "event",
            "refresh", "event"
        }));
    }

    [Test]
    public void TopologyRemovalRetiresMissingHostWithoutRebindingOrChangingHistory()
    {
        using var service = CreateService(out var resolver, out var factory);
        resolver.Resolved = new BootstrapToastScreenInfo("A", new Rectangle(0, 0, 800, 600), 96);
        service.Show("A");
        resolver.Resolved = new BootstrapToastScreenInfo("B", new Rectangle(800, 0, 800, 600), 96);
        service.Show("B");
        var hostA = factory.Hosts[0];
        var hostB = factory.Hosts[1];
        resolver.CurrentScreens = new[] { new BootstrapToastScreenInfo("A", new Rectangle(0, 0, 900, 700), 120) };

        service.RefreshDisplayTopologyForTests();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(hostA.ApplyCount, Is.EqualTo(2));
            Assert.That(hostA.LastScreen.Dpi, Is.EqualTo(120));
            Assert.That(hostB.RetireCount, Is.EqualTo(1));
            Assert.That(hostB.ApplyCount, Is.EqualTo(1));
            Assert.That(service.GetHistory().Count, Is.EqualTo(2));
            Assert.That(service.UnreadCount, Is.EqualTo(2));
        }));
    }

    private static BootstrapToastService CreateService(
        out FakeScreenResolver resolver,
        out FakeHostFactory factory,
        Action? refresh = null)
    {
        resolver = new FakeScreenResolver
        {
            Resolved = new BootstrapToastScreenInfo("A", new Rectangle(0, 0, 1000, 800), 96)
        };
        resolver.CurrentScreens = new[] { resolver.Resolved };
        factory = new FakeHostFactory();
        return new BootstrapToastService(resolver, factory, refresh, subscribeSystemEvents: false);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(2))
        {
            Application.DoEvents();
        }

        Assert.That(condition(), Is.True, "The UI callback did not run before the test timeout.");
    }

    private sealed class FakeScreenResolver : IBootstrapToastScreenResolver
    {
        public BootstrapToastScreenInfo Resolved { get; set; }

        public IReadOnlyList<BootstrapToastScreenInfo> CurrentScreens { get; set; } = Array.Empty<BootstrapToastScreenInfo>();

        public BootstrapToastScreenInfo Resolve(Control? relativeTo) => Resolved;

        public IReadOnlyList<BootstrapToastScreenInfo> GetCurrentScreens() => CurrentScreens;
    }

    private sealed class FakeHostFactory : IBootstrapToastHostWindowFactory
    {
        public List<FakeHost> Hosts { get; } = new List<FakeHost>();

        public bool ThrowOnShow { get; set; }

        public Action<BootstrapToast>? OnShow { get; set; }

        public IBootstrapToastHostWindow Create()
        {
            var host = new FakeHost(this);
            Hosts.Add(host);
            return host;
        }
    }

    private sealed class FakeHost : IBootstrapToastHostWindow
    {
        private readonly FakeHostFactory _owner;

        public FakeHost(FakeHostFactory owner)
        {
            _owner = owner;
        }

        public string ScreenDeviceName { get; private set; } = string.Empty;

        public bool HasOwnedToasts => Toasts.Count > 0;

        public List<BootstrapToast> Toasts { get; } = new List<BootstrapToast>();

        public int ApplyCount { get; private set; }

        public int RetireCount { get; private set; }

        public BootstrapToastScreenInfo LastScreen { get; private set; }

        public event EventHandler? BecameEmpty;

        public void ApplySettings(BootstrapToastScreenInfo screen, BootstrapToastHostSettings settings)
        {
            ScreenDeviceName = screen.DeviceName;
            LastScreen = screen;
            ApplyCount++;
        }

        public void ShowToast(BootstrapToast toast)
        {
            if (_owner.ThrowOnShow)
            {
                throw new InvalidOperationException("transfer failed");
            }

            Toasts.Add(toast);
            _owner.OnShow?.Invoke(toast);
        }

        public void DismissAll()
        {
            foreach (var toast in Toasts)
            {
                toast.Dispose();
            }

            Toasts.Clear();
            BecameEmpty?.Invoke(this, EventArgs.Empty);
        }

        public void RetireForScreenRemoval()
        {
            RetireCount++;
        }

        public void Dispose()
        {
            DismissAll();
        }
    }
}
