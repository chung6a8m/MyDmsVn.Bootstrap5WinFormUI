using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapNotificationCenterTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void WindowCompositionAndExplicitDpiPlacementMatchContract()
    {
        using var window = new BootstrapNotificationCenterWindow();
        var screen = new BootstrapToastScreenInfo("DISPLAY2", new Rectangle(-1600, 100, 1600, 900), 144);
        window.ApplySettings(
            screen,
            new BootstrapNotificationCenterSettings(BootstrapToastPlacement.BottomRight, new Padding(16), topMost: true));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(window.FormBorderStyle, Is.EqualTo(FormBorderStyle.None));
            Assert.That(window.ShowInTaskbar, Is.False);
            Assert.That(window.StartPosition, Is.EqualTo(FormStartPosition.Manual));
            Assert.That(window.KeyPreview, Is.True);
            Assert.That(window.TopMost, Is.True);
            Assert.That(window.Bounds, Is.EqualTo(new Rectangle(-654, 136, 630, 840)));
            Assert.That(window.HistoryList, Is.Not.Null);
            Assert.That(window.UnreadBadge, Is.TypeOf<BootstrapBadge>());
            Assert.That(window.MarkAllButton, Is.TypeOf<BootstrapButton>());
            Assert.That(window.ClearButton, Is.TypeOf<BootstrapButton>());
            Assert.That(window.CloseButtonControl, Is.TypeOf<BootstrapButton>());
        }));
    }

    [Test]
    public void RefreshUsesNewestFirstSnapshotAndUpdatesEmptyUnreadActions()
    {
        using var window = new BootstrapNotificationCenterWindow();
        var newest = Item("newest", isRead: false);
        var older = Item("older", isRead: true);
        window.ApplySettings(
            new BootstrapToastScreenInfo("DISPLAY1", new Rectangle(0, 0, 1000, 800), 96),
            new BootstrapNotificationCenterSettings(BootstrapToastPlacement.TopRight, new Padding(16), false));
        window.ShowCenter();

        window.RefreshHistory(new[] { newest, older }, unreadCount: 1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(window.HistoryList.Items.Cast<BootstrapToastHistoryItem>().Select(item => item.Text), Is.EqualTo(new[] { "newest", "older" }));
            Assert.That(window.UnreadBadge.Text, Is.EqualTo("1"));
            Assert.That(window.UnreadBadge.Visible, Is.True);
            Assert.That(window.MarkAllButton.Enabled, Is.True);
            Assert.That(window.ClearButton.Enabled, Is.True);
            Assert.That(window.EmptyLabel.Visible, Is.False);
        }));

        window.RefreshHistory(Array.Empty<BootstrapToastHistoryItem>(), unreadCount: 0);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(window.HistoryList.Items, Is.Empty);
            Assert.That(window.UnreadBadge.Visible, Is.False);
            Assert.That(window.MarkAllButton.Enabled, Is.False);
            Assert.That(window.ClearButton.Enabled, Is.False);
            Assert.That(window.EmptyLabel.Visible, Is.True);
        }));
    }

    [Test]
    public void UserCloseEscapeAndCloseButtonHideWithoutDisposalAndReopenSameWindow()
    {
        using var window = new BootstrapNotificationCenterWindow();
        window.ApplySettings(
            new BootstrapToastScreenInfo("DISPLAY1", new Rectangle(0, 0, 1000, 800), 96),
            new BootstrapNotificationCenterSettings(BootstrapToastPlacement.TopRight, new Padding(16), false));
        window.ShowCenter();
        window.Close();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(window.Visible, Is.False);
            Assert.That(window.IsDisposed, Is.False);
        }));

        window.ShowCenter();
        window.ProcessEscapeForTests();
        Assert.That(window.Visible, Is.False);

        window.ShowCenter();
        window.CloseButtonControl.PerformClick();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(window.Visible, Is.False);
            Assert.That(window.IsDisposed, Is.False);
        }));
    }

    [Test]
    public void ServiceWiresReadMarkAllClearAndRefreshBeforePublicEvent()
    {
        var resolver = new SingleScreenResolver();
        var hosts = new PassiveHostFactory();
        using var service = new BootstrapToastService(resolver, hosts, historyRefreshObserver: null, subscribeSystemEvents: false);
        var first = service.Show("first");
        service.Show("second");
        service.ShowNotificationCenter();
        var center = service.NotificationCenterForTests!;
        var observedUnread = -1;
        service.HistoryChanged += (_, _) => observedUnread = center.DisplayedUnreadCount;

        center.HistoryList.SelectedIndex = 1;
        center.HistoryList.ProcessActivationKeyForTests(Keys.Enter);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(service.GetHistory().Single(item => item.Id == first).IsRead, Is.True);
            Assert.That(observedUnread, Is.EqualTo(1));
        }));

        center.MarkAllButton.PerformClick();
        Assert.That(service.UnreadCount, Is.Zero);
        center.ClearButton.PerformClick();
        Assert.That(service.GetHistory(), Is.Empty);
    }

    [Test]
    public void ServiceDisposeActuallyDisposesCenterAndRepeatedShowReusesIt()
    {
        var service = new BootstrapToastService(new SingleScreenResolver(), new PassiveHostFactory(), null, false);
        service.ShowNotificationCenter();
        var center = service.NotificationCenterForTests!;
        service.HideNotificationCenter();
        service.ShowNotificationCenter();
        Assert.That(service.NotificationCenterForTests, Is.SameAs(center));

        service.Dispose();

        Assert.That(center.IsDisposed, Is.True);
    }

    private static BootstrapToastHistoryItem Item(string text, bool isRead)
    {
        return new BootstrapToastHistoryItem(Guid.NewGuid(), DateTimeOffset.UtcNow, text, text, BootstrapVariant.Primary, isRead);
    }

    private sealed class SingleScreenResolver : IBootstrapToastScreenResolver
    {
        private readonly BootstrapToastScreenInfo _screen =
            new BootstrapToastScreenInfo("DISPLAY1", new Rectangle(0, 0, 1200, 900), 96);

        public BootstrapToastScreenInfo Resolve(Control? relativeTo) => _screen;

        public System.Collections.Generic.IReadOnlyList<BootstrapToastScreenInfo> GetCurrentScreens() => new[] { _screen };
    }

    private sealed class PassiveHostFactory : IBootstrapToastHostWindowFactory
    {
        public IBootstrapToastHostWindow Create() => new PassiveHost();
    }

    private sealed class PassiveHost : IBootstrapToastHostWindow
    {
        private readonly System.Collections.Generic.List<BootstrapToast> _toasts = new System.Collections.Generic.List<BootstrapToast>();

        public string ScreenDeviceName { get; private set; } = string.Empty;

        public bool HasOwnedToasts => _toasts.Count > 0;

        public event EventHandler? BecameEmpty;

        public void ApplySettings(BootstrapToastScreenInfo screen, BootstrapToastHostSettings settings) => ScreenDeviceName = screen.DeviceName;

        public void ShowToast(BootstrapToast toast) => _toasts.Add(toast);

        public void DismissAll()
        {
            foreach (var toast in _toasts) toast.Dispose();
            _toasts.Clear();
            BecameEmpty?.Invoke(this, EventArgs.Empty);
        }

        public void RetireForScreenRemoval() => DismissAll();

        public void Dispose() => DismissAll();
    }
}
