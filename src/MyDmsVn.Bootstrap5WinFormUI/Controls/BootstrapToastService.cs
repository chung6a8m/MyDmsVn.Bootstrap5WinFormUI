using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Shows application-level transient Toasts and maintains bounded in-memory notification history.</summary>
public sealed class BootstrapToastService : IDisposable
{
    private sealed class HostRecord
    {
        public HostRecord(BootstrapToastScreenInfo screen, IBootstrapToastHostWindow host)
        {
            Screen = screen;
            Host = host;
        }

        public BootstrapToastScreenInfo Screen { get; set; }

        public IBootstrapToastHostWindow Host { get; }
    }

    private readonly struct OptionsSnapshot
    {
        public OptionsSnapshot(BootstrapToastOptions options)
        {
            Title = options.Title ?? string.Empty;
            Text = options.Text ?? string.Empty;
            Variant = options.Variant;
            Icon = options.Icon;
            Dismissible = options.Dismissible;
            AutoHide = options.AutoHide;
            AutoHideDelay = options.AutoHideDelay;
            AnimationDuration = options.AnimationDuration;
            IncludeInHistory = options.IncludeInHistory;
        }

        public string Title { get; }
        public string Text { get; }
        public BootstrapVariant Variant { get; }
        public IconDescriptor? Icon { get; }
        public bool Dismissible { get; }
        public bool AutoHide { get; }
        public int AutoHideDelay { get; }
        public int AnimationDuration { get; }
        public bool IncludeInHistory { get; }
    }

    private static readonly object DefaultSync = new object();
    private static BootstrapToastService? _default;

    private readonly int _uiThreadId;
    private readonly Control _uiDispatcher;
    private readonly IBootstrapToastScreenResolver _screenResolver;
    private readonly IBootstrapToastHostWindowFactory _hostFactory;
    private readonly BootstrapToastHistoryStore _historyStore;
    private readonly Dictionary<string, HostRecord> _canonicalHosts =
        new Dictionary<string, HostRecord>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<IBootstrapToastHostWindow> _retiringHosts =
        new HashSet<IBootstrapToastHostWindow>();
    private readonly Action? _historyRefreshObserver;
    private readonly bool _trackDisplayTopology;
    private readonly bool _isDefault;

    private BootstrapToastPlacement _placement = BootstrapToastPlacement.TopRight;
    private int _toastSpacing = 8;
    private int _maximumVisibleToasts = 5;
    private int _toastWidth = 320;
    private Padding _screenMargin = new Padding(16);
    private bool _topMost;
    private IIconRenderer _iconRenderer = BootstrapIconRenderer.CreateDefault();
    private EventHandler? _historyChanged;
    private BootstrapNotificationCenterWindow? _notificationCenter;
    private BootstrapToastScreenInfo? _notificationCenterScreen;
    private bool _displaySubscribed;
    private bool _applicationExitSubscribed;
    private bool _disposed;

    /// <summary>Initializes a service on the current STA Windows Forms UI thread.</summary>
    public BootstrapToastService()
        : this(
            new BootstrapToastScreenResolver(),
            new BootstrapToastHostWindowFactory(),
            historyRefreshObserver: null,
            subscribeSystemEvents: true,
            isDefault: false)
    {
    }

    internal BootstrapToastService(
        IBootstrapToastScreenResolver screenResolver,
        IBootstrapToastHostWindowFactory hostFactory,
        Action? historyRefreshObserver,
        bool subscribeSystemEvents)
        : this(screenResolver, hostFactory, historyRefreshObserver, subscribeSystemEvents, isDefault: false)
    {
    }

    private BootstrapToastService(
        IBootstrapToastScreenResolver screenResolver,
        IBootstrapToastHostWindowFactory hostFactory,
        Action? historyRefreshObserver,
        bool subscribeSystemEvents,
        bool isDefault)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new InvalidOperationException("BootstrapToastService must be created on an STA Windows Forms UI thread.");
        }

        _screenResolver = screenResolver ?? throw new ArgumentNullException(nameof(screenResolver));
        _hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
        _historyRefreshObserver = historyRefreshObserver;
        _trackDisplayTopology = subscribeSystemEvents;
        _isDefault = isDefault;
        _historyStore = new BootstrapToastHistoryStore(100);
        _uiThreadId = Thread.CurrentThread.ManagedThreadId;
        _uiDispatcher = new Control();
        _uiDispatcher.CreateControl();
        if (!_uiDispatcher.IsHandleCreated)
        {
            _ = _uiDispatcher.Handle;
        }

        if (_isDefault)
        {
            Application.ApplicationExit += OnApplicationExit;
            _applicationExitSubscribed = true;
        }
    }

    /// <summary>Gets the lazy application-wide service for the current UI thread.</summary>
    public static BootstrapToastService Default
    {
        get
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("BootstrapToastService.Default must be accessed from an STA Windows Forms UI thread.");
            }

            lock (DefaultSync)
            {
                if (_default is null || _default._disposed)
                {
                    _default = new BootstrapToastService(
                        new BootstrapToastScreenResolver(),
                        new BootstrapToastHostWindowFactory(),
                        historyRefreshObserver: null,
                        subscribeSystemEvents: true,
                        isDefault: true);
                }

                _default.VerifyAccess();
                return _default;
            }
        }
    }

    /// <summary>Gets or sets the screen corner used by transient hosts and the notification center.</summary>
    public BootstrapToastPlacement Placement
    {
        get { VerifyAccess(); return _placement; }
        set
        {
            VerifyAccess();
            BootstrapToastLayoutLogic.ValidatePlacement(value);
            if (_placement == value) return;
            _placement = value;
            ApplySettingsToCanonicalHosts();
            ApplySettingsToNotificationCenter();
        }
    }

    /// <summary>Gets or sets logical 96-DPI spacing between transient Toasts.</summary>
    public int ToastSpacing
    {
        get { VerifyAccess(); return _toastSpacing; }
        set
        {
            VerifyAccess();
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Toast spacing cannot be negative.");
            if (_toastSpacing == value) return;
            _toastSpacing = value;
            ApplySettingsToCanonicalHosts();
        }
    }

    /// <summary>Gets or sets the maximum number of visible Toasts per screen.</summary>
    public int MaximumVisibleToasts
    {
        get { VerifyAccess(); return _maximumVisibleToasts; }
        set
        {
            VerifyAccess();
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum visible Toasts must be greater than zero.");
            if (_maximumVisibleToasts == value) return;
            _maximumVisibleToasts = value;
            ApplySettingsToCanonicalHosts();
        }
    }

    /// <summary>Gets or sets the logical 96-DPI width assigned to newly created Toasts.</summary>
    public int ToastWidth
    {
        get { VerifyAccess(); return _toastWidth; }
        set
        {
            VerifyAccess();
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Toast width must be greater than zero.");
            _toastWidth = value;
        }
    }

    /// <summary>Gets or sets logical screen-edge margins used by service-owned windows.</summary>
    public Padding ScreenMargin
    {
        get { VerifyAccess(); return _screenMargin; }
        set
        {
            VerifyAccess();
            ValidateScreenMargin(value);
            if (_screenMargin == value) return;
            _screenMargin = value;
            ApplySettingsToCanonicalHosts();
            ApplySettingsToNotificationCenter();
        }
    }

    /// <summary>Gets or sets whether service-owned top-level windows are topmost.</summary>
    public bool TopMost
    {
        get { VerifyAccess(); return _topMost; }
        set
        {
            VerifyAccess();
            if (_topMost == value) return;
            _topMost = value;
            ApplySettingsToCanonicalHosts();
            ApplySettingsToNotificationCenter();
        }
    }

    /// <summary>Gets or sets the maximum number of retained in-memory history entries.</summary>
    public int HistoryCapacity
    {
        get { VerifyAccess(); return _historyStore.Capacity; }
        set
        {
            VerifyAccess();
            var previousCount = _historyStore.Count;
            _historyStore.Capacity = value;
            if (_historyStore.Count != previousCount)
            {
                PublishCommittedHistoryMutation();
            }
        }
    }

    /// <summary>Gets or sets the caller-owned renderer snapshotted onto newly created Toasts.</summary>
    public IIconRenderer IconRenderer
    {
        get { VerifyAccess(); return _iconRenderer; }
        set
        {
            VerifyAccess();
            _iconRenderer = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>Gets the number of unread entries currently retained in history.</summary>
    public int UnreadCount
    {
        get { VerifyAccess(); return _historyStore.UnreadCount; }
    }

    /// <summary>Gets whether the service-owned notification center is currently visible.</summary>
    public bool IsNotificationCenterVisible
    {
        get
        {
            VerifyAccess();
            return _notificationCenter is not null &&
                   !_notificationCenter.IsDisposed &&
                   _notificationCenter.Visible;
        }
    }

    /// <summary>Occurs after an effective history mutation and after framework-owned history UI has refreshed.</summary>
    public event EventHandler? HistoryChanged
    {
        add { VerifyAccess(); _historyChanged += value; }
        remove { VerifyAccess(); _historyChanged -= value; }
    }

    /// <summary>Shows a transient notification containing the supplied body text.</summary>
    public Guid Show(string text, Control? relativeTo = null)
    {
        return Show(new BootstrapToastOptions { Text = text ?? string.Empty }, relativeTo);
    }

    /// <summary>Shows a transient notification from a caller-owned options snapshot.</summary>
    public Guid Show(BootstrapToastOptions options, Control? relativeTo = null)
    {
        VerifyAccess();
        if (options is null) throw new ArgumentNullException(nameof(options));
        var snapshot = new OptionsSnapshot(options);
        BootstrapFeedbackRenderLogic.ValidateVariant(snapshot.Variant);
        if (snapshot.AutoHideDelay <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Auto-hide delay must be greater than zero.");
        if (snapshot.AnimationDuration <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Animation duration must be greater than zero.");

        var screen = _screenResolver.Resolve(relativeTo);
        var host = GetOrCreateCanonicalHost(screen);
        var available = BootstrapToastServiceLayoutLogic.InsetWorkingArea(screen.WorkingArea, _screenMargin, screen.Dpi);
        var width = BootstrapToastServiceLayoutLogic.ResolveToastWidth(_toastWidth, available.Width, screen.Dpi);
        var toast = CreateToast(snapshot, width);
        var id = Guid.NewGuid();
        var historyItem = snapshot.IncludeInHistory
            ? new BootstrapToastHistoryItem(
                id,
                DateTimeOffset.UtcNow,
                snapshot.Title,
                snapshot.Text,
                snapshot.Variant,
                isRead: false)
            : null;

        try
        {
            host.ShowToast(toast);
        }
        catch
        {
            if (!toast.IsDisposed && !toast.IsOwned)
            {
                toast.Dispose();
            }

            throw;
        }

        var historyAdded = historyItem is not null && _historyStore.Add(historyItem);
        if (historyAdded)
        {
            PublishCommittedHistoryMutation();
        }

        return id;
    }

    /// <summary>Requests dismissal of all transient Toasts without clearing history.</summary>
    public void DismissAll()
    {
        VerifyAccess();
        foreach (var host in _canonicalHosts.Values.Select(record => record.Host).Concat(_retiringHosts).Distinct().ToArray())
        {
            host.DismissAll();
        }
    }

    /// <summary>Returns a newest-first immutable snapshot of retained history.</summary>
    public IReadOnlyList<BootstrapToastHistoryItem> GetHistory()
    {
        VerifyAccess();
        return _historyStore.SnapshotNewestFirst();
    }

    /// <summary>Marks one retained notification as read.</summary>
    public bool MarkAsRead(Guid notificationId)
    {
        VerifyAccess();
        if (!_historyStore.MarkAsRead(notificationId)) return false;
        PublishCommittedHistoryMutation();
        return true;
    }

    /// <summary>Marks all unread retained notifications as read in one mutation batch.</summary>
    public void MarkAllAsRead()
    {
        VerifyAccess();
        if (_historyStore.MarkAllAsRead()) PublishCommittedHistoryMutation();
    }

    /// <summary>Clears retained history without dismissing live Toasts.</summary>
    public void ClearHistory()
    {
        VerifyAccess();
        if (_historyStore.Clear()) PublishCommittedHistoryMutation();
    }

    /// <summary>Shows and activates the reusable notification-center window.</summary>
    public void ShowNotificationCenter(Control? relativeTo = null)
    {
        VerifyAccess();
        EnsureDisplaySubscription();
        var screen = _screenResolver.Resolve(relativeTo);
        var center = EnsureNotificationCenter();
        _notificationCenterScreen = screen;
        center.ApplySettings(screen, CreateNotificationCenterSettings());
        RefreshNotificationCenterFromStore();
        center.ShowCenter();
    }

    /// <summary>Hides the notification-center window if it exists.</summary>
    public void HideNotificationCenter()
    {
        VerifyAccess();
        _notificationCenter?.HideCenter();
    }

    /// <summary>Toggles the reusable notification-center window.</summary>
    public void ToggleNotificationCenter(Control? relativeTo = null)
    {
        VerifyAccess();
        if (IsNotificationCenterVisible)
        {
            HideNotificationCenter();
        }
        else
        {
            ShowNotificationCenter(relativeTo);
        }
    }

    /// <summary>Releases hosts, callbacks, history UI, and static subscriptions owned by this service.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
        {
            throw new InvalidOperationException("BootstrapToastService can only be used from the UI thread that created it.");
        }

        _disposed = true;
        if (_displaySubscribed)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _displaySubscribed = false;
        }

        if (_applicationExitSubscribed)
        {
            Application.ApplicationExit -= OnApplicationExit;
            _applicationExitSubscribed = false;
        }

        foreach (var host in _canonicalHosts.Values.Select(record => record.Host).Concat(_retiringHosts).Distinct().ToArray())
        {
            host.BecameEmpty -= OnHostBecameEmpty;
            host.Dispose();
        }

        _canonicalHosts.Clear();
        _retiringHosts.Clear();

        if (_notificationCenter is not null)
        {
            _notificationCenter.ItemActivated -= OnNotificationCenterItemActivated;
            _notificationCenter.MarkAllRequested -= OnNotificationCenterMarkAllRequested;
            _notificationCenter.ClearRequested -= OnNotificationCenterClearRequested;
            _notificationCenter.CloseForServiceDisposal();
            _notificationCenter = null;
            _notificationCenterScreen = null;
        }

        _historyChanged = null;
        _uiDispatcher.Dispose();

        if (_isDefault)
        {
            lock (DefaultSync)
            {
                if (ReferenceEquals(_default, this)) _default = null;
            }
        }
    }

    internal void RefreshDisplayTopologyForTests()
    {
        VerifyAccess();
        RefreshDisplayTopology();
    }

    internal void PostFrameworkCallbackToUiForTests(Action callback)
    {
        PostFrameworkCallbackToUi(callback);
    }

    internal BootstrapNotificationCenterWindow? NotificationCenterForTests => _notificationCenter;

    private BootstrapToast CreateToast(OptionsSnapshot options, int widthPixels)
    {
        return new BootstrapToast
        {
            Width = widthPixels,
            Title = options.Title,
            Text = options.Text,
            Variant = options.Variant,
            Icon = options.Icon,
            IconRenderer = _iconRenderer,
            Dismissible = options.Dismissible,
            AutoHide = options.AutoHide,
            AutoHideDelay = options.AutoHideDelay,
            AnimationDuration = options.AnimationDuration
        };
    }

    private IBootstrapToastHostWindow GetOrCreateCanonicalHost(BootstrapToastScreenInfo screen)
    {
        EnsureDisplaySubscription();
        if (_canonicalHosts.TryGetValue(screen.DeviceName, out var existing))
        {
            existing.Screen = screen;
            existing.Host.ApplySettings(screen, CreateHostSettings());
            return existing.Host;
        }

        var host = _hostFactory.Create();
        try
        {
            host.ApplySettings(screen, CreateHostSettings());
            host.BecameEmpty += OnHostBecameEmpty;
            _canonicalHosts.Add(screen.DeviceName, new HostRecord(screen, host));
            return host;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    private BootstrapToastHostSettings CreateHostSettings()
    {
        return new BootstrapToastHostSettings(_placement, _toastSpacing, _maximumVisibleToasts, _screenMargin, _topMost);
    }

    private void ApplySettingsToCanonicalHosts()
    {
        var settings = CreateHostSettings();
        foreach (var record in _canonicalHosts.Values.ToArray())
        {
            record.Host.ApplySettings(record.Screen, settings);
        }
    }

    private BootstrapNotificationCenterSettings CreateNotificationCenterSettings()
    {
        return new BootstrapNotificationCenterSettings(_placement, _screenMargin, _topMost);
    }

    private void ApplySettingsToNotificationCenter()
    {
        if (_notificationCenter is null || _notificationCenter.IsDisposed || _notificationCenterScreen is null)
        {
            return;
        }

        _notificationCenter.ApplySettings(_notificationCenterScreen.Value, CreateNotificationCenterSettings());
    }

    private BootstrapNotificationCenterWindow EnsureNotificationCenter()
    {
        if (_notificationCenter is not null && !_notificationCenter.IsDisposed)
        {
            return _notificationCenter;
        }

        var center = new BootstrapNotificationCenterWindow();
        center.ItemActivated += OnNotificationCenterItemActivated;
        center.MarkAllRequested += OnNotificationCenterMarkAllRequested;
        center.ClearRequested += OnNotificationCenterClearRequested;
        _notificationCenter = center;
        return center;
    }

    private void RefreshNotificationCenterFromStore()
    {
        if (_notificationCenter is null || _notificationCenter.IsDisposed)
        {
            return;
        }

        _notificationCenter.RefreshHistory(_historyStore.SnapshotNewestFirst(), _historyStore.UnreadCount);
    }

    private void PublishCommittedHistoryMutation()
    {
        RefreshNotificationCenterFromStore();
        _historyRefreshObserver?.Invoke();
        _historyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureDisplaySubscription()
    {
        if (!_trackDisplayTopology || _displaySubscribed)
        {
            return;
        }

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _displaySubscribed = true;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        PostFrameworkCallbackToUi(RefreshDisplayTopology);
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        PostFrameworkCallbackToUi(Dispose);
    }

    private void PostFrameworkCallbackToUi(Action callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        if (_disposed) return;
        if (Thread.CurrentThread.ManagedThreadId == _uiThreadId)
        {
            callback();
            return;
        }

        if (_uiDispatcher.IsDisposed || !_uiDispatcher.IsHandleCreated) return;
        try
        {
            _uiDispatcher.BeginInvoke((MethodInvoker)(() =>
            {
                if (!_disposed) callback();
            }));
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void RefreshDisplayTopology()
    {
        if (_disposed) return;
        var screens = _screenResolver.GetCurrentScreens();
        var live = screens.ToDictionary(screen => screen.DeviceName, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _canonicalHosts.ToArray())
        {
            if (live.TryGetValue(pair.Key, out var screen))
            {
                pair.Value.Screen = screen;
                pair.Value.Host.ApplySettings(screen, CreateHostSettings());
                continue;
            }

            _canonicalHosts.Remove(pair.Key);
            _retiringHosts.Add(pair.Value.Host);
            pair.Value.Host.RetireForScreenRemoval();
        }

        RefreshNotificationCenterTopology(live);
    }

    private void RefreshNotificationCenterTopology(IReadOnlyDictionary<string, BootstrapToastScreenInfo> liveScreens)
    {
        if (_notificationCenter is null || _notificationCenter.IsDisposed || _notificationCenterScreen is null)
        {
            return;
        }

        BootstrapToastScreenInfo screen;
        if (!liveScreens.TryGetValue(_notificationCenterScreen.Value.DeviceName, out screen))
        {
            screen = _screenResolver.Resolve(null);
        }

        _notificationCenterScreen = screen;
        _notificationCenter.ApplySettings(screen, CreateNotificationCenterSettings());
    }

    private void OnNotificationCenterItemActivated(object? sender, BootstrapNotificationHistoryItemActivatedEventArgs e)
    {
        MarkAsRead(e.Item.Id);
    }

    private void OnNotificationCenterMarkAllRequested(object? sender, EventArgs e)
    {
        MarkAllAsRead();
    }

    private void OnNotificationCenterClearRequested(object? sender, EventArgs e)
    {
        ClearHistory();
    }

    private void OnHostBecameEmpty(object? sender, EventArgs e)
    {
        if (sender is not IBootstrapToastHostWindow host || !_retiringHosts.Remove(host))
        {
            return;
        }

        host.BecameEmpty -= OnHostBecameEmpty;
        host.Dispose();
    }

    private void VerifyAccess()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BootstrapToastService));
        if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
        {
            throw new InvalidOperationException("BootstrapToastService can only be used from the UI thread that created it.");
        }
    }

    private static void ValidateScreenMargin(Padding value)
    {
        if (value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Screen margin edges cannot be negative.");
        }
    }
}
