using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapNotificationCenterSettings
{
    public BootstrapNotificationCenterSettings(
        BootstrapToastPlacement placement,
        Padding screenMargin,
        bool topMost)
    {
        BootstrapToastLayoutLogic.ValidatePlacement(placement);
        if (screenMargin.Left < 0 || screenMargin.Top < 0 || screenMargin.Right < 0 || screenMargin.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(screenMargin), screenMargin, "Screen margin edges cannot be negative.");
        }

        Placement = placement;
        ScreenMargin = screenMargin;
        TopMost = topMost;
    }

    public BootstrapToastPlacement Placement { get; }
    public Padding ScreenMargin { get; }
    public bool TopMost { get; }
}

internal sealed class BootstrapNotificationCenterWindow : Form
{
    private static readonly Size LogicalPreferredSize = new Size(420, 560);

    private readonly Panel _headerPanel;
    private readonly Label _titleLabel;
    private readonly Panel _contentPanel;
    private readonly Panel _footerPanel;
    private bool _allowCloseForServiceDisposal;
    private bool _disposing;
    private bool _themeSubscribed;
    private Font? _headerFont;
    private int _currentDpi = DpiScaler.DefaultDpi;

    public BootstrapNotificationCenterWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        AccessibleRole = AccessibleRole.Window;
        AccessibleName = "Notifications";

        _headerPanel = new Panel { Dock = DockStyle.None, TabStop = false };
        _titleLabel = new Label
        {
            AutoSize = false,
            Text = "Notifications",
            TextAlign = ContentAlignment.MiddleLeft,
            TabStop = false
        };
        UnreadBadge = new BootstrapBadge { Text = "0", Pill = true, Visible = false };
        CloseButtonControl = new BootstrapButton
        {
            Text = "Close",
            AutoSize = false,
            Variant = BootstrapVariant.Secondary,
            AccessibleName = "Close notification center"
        };
        CloseButtonControl.Click += OnCloseRequested;
        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Controls.Add(UnreadBadge);
        _headerPanel.Controls.Add(CloseButtonControl);

        _contentPanel = new Panel { Dock = DockStyle.None, TabStop = false };
        HistoryList = new BootstrapNotificationHistoryListBox { Dock = DockStyle.Fill };
        HistoryList.ItemActivated += OnHistoryItemActivated;
        EmptyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "No notifications yet.",
            TextAlign = ContentAlignment.MiddleCenter,
            TabStop = false
        };
        _contentPanel.Controls.Add(HistoryList);
        _contentPanel.Controls.Add(EmptyLabel);
        EmptyLabel.BringToFront();

        _footerPanel = new Panel { Dock = DockStyle.None, TabStop = false };
        MarkAllButton = new BootstrapButton
        {
            Text = "Mark all read",
            AutoSize = false,
            Variant = BootstrapVariant.Secondary,
            Enabled = false
        };
        ClearButton = new BootstrapButton
        {
            Text = "Clear",
            AutoSize = false,
            Variant = BootstrapVariant.Danger,
            Enabled = false
        };
        MarkAllButton.Click += OnMarkAllRequested;
        ClearButton.Click += OnClearRequested;
        _footerPanel.Controls.Add(MarkAllButton);
        _footerPanel.Controls.Add(ClearButton);

        Controls.Add(_headerPanel);
        Controls.Add(_contentPanel);
        Controls.Add(_footerPanel);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        RebuildHeaderFont();
        ApplyTheme();
        PerformLayout();
    }

    public string ScreenDeviceName { get; private set; } = string.Empty;

    public event EventHandler<BootstrapNotificationHistoryItemActivatedEventArgs>? ItemActivated;
    public event EventHandler? MarkAllRequested;
    public event EventHandler? ClearRequested;

    internal BootstrapNotificationHistoryListBox HistoryList { get; }
    internal BootstrapBadge UnreadBadge { get; }
    internal BootstrapButton MarkAllButton { get; }
    internal BootstrapButton ClearButton { get; }
    internal BootstrapButton CloseButtonControl { get; }
    internal Label EmptyLabel { get; }
    internal int DisplayedUnreadCount { get; private set; }

    public void ApplySettings(BootstrapToastScreenInfo screen, BootstrapNotificationCenterSettings settings)
    {
        ThrowIfDisposed();
        ScreenDeviceName = screen.DeviceName;
        _currentDpi = screen.Dpi;
        TopMost = settings.TopMost;
        var available = BootstrapToastServiceLayoutLogic.InsetWorkingArea(screen.WorkingArea, settings.ScreenMargin, screen.Dpi);
        var desired = BootstrapToastServiceLayoutLogic.ResolveNotificationCenterSize(LogicalPreferredSize, available.Size, screen.Dpi);
        Bounds = BootstrapToastServiceLayoutLogic.CalculateNotificationCenterBounds(available, desired, settings.Placement);
        PerformLayout();
    }

    public void RefreshHistory(IReadOnlyList<BootstrapToastHistoryItem> items, int unreadCount)
    {
        ThrowIfDisposed();
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (unreadCount < 0 || unreadCount > items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(unreadCount), unreadCount, "Unread count must be within the history snapshot count.");
        }

        DisplayedUnreadCount = unreadCount;
        HistoryList.SetHistory(items);
        UnreadBadge.Text = unreadCount.ToString(CultureInfo.CurrentCulture);
        UnreadBadge.Visible = unreadCount > 0;
        MarkAllButton.Enabled = unreadCount > 0;
        ClearButton.Enabled = items.Count > 0;
        EmptyLabel.Visible = items.Count == 0;
        HistoryList.Visible = items.Count > 0;
        PerformLayout();
    }

    public void ShowCenter()
    {
        ThrowIfDisposed();
        if (!Visible)
        {
            Show();
        }

        Activate();
        HistoryList.Focus();
    }

    public void HideCenter()
    {
        if (!_disposing && !IsDisposed)
        {
            Hide();
        }
    }

    public void CloseForServiceDisposal()
    {
        if (_disposing || IsDisposed)
        {
            return;
        }

        _allowCloseForServiceDisposal = true;
        if (IsHandleCreated)
        {
            Close();
        }
        else
        {
            Dispose();
        }
    }

    internal void ProcessEscapeForTests()
    {
        OnKeyDown(new KeyEventArgs(Keys.Escape));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowCloseForServiceDisposal)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Hide();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_headerPanel is null || _contentPanel is null || _footerPanel is null)
        {
            return;
        }

        var padding = DpiScaler.Scale(12, _currentDpi);
        var gap = DpiScaler.Scale(8, _currentDpi);
        var headerHeight = DpiScaler.Scale(48, _currentDpi);
        var footerHeight = DpiScaler.Scale(56, _currentDpi);
        _headerPanel.Bounds = new Rectangle(0, 0, ClientSize.Width, Math.Min(headerHeight, ClientSize.Height));
        _footerPanel.Bounds = new Rectangle(0, Math.Max(_headerPanel.Bottom, ClientSize.Height - footerHeight), ClientSize.Width, Math.Min(footerHeight, ClientSize.Height));
        _contentPanel.Bounds = Rectangle.FromLTRB(0, _headerPanel.Bottom, ClientSize.Width, Math.Max(_headerPanel.Bottom, _footerPanel.Top));

        var closeWidth = DpiScaler.Scale(72, _currentDpi);
        CloseButtonControl.Bounds = new Rectangle(
            Math.Max(padding, _headerPanel.Width - padding - closeWidth),
            padding / 2,
            closeWidth,
            Math.Max(1, _headerPanel.Height - padding));
        var badgeSize = UnreadBadge.GetPreferredSize(Size.Empty);
        var badgeX = Math.Max(padding, CloseButtonControl.Left - gap - badgeSize.Width);
        UnreadBadge.Bounds = new Rectangle(badgeX, (_headerPanel.Height - badgeSize.Height) / 2, badgeSize.Width, badgeSize.Height);
        _titleLabel.Bounds = Rectangle.FromLTRB(padding, 0, Math.Max(padding, badgeX - gap), _headerPanel.Height);

        var clearWidth = DpiScaler.Scale(80, _currentDpi);
        var markWidth = DpiScaler.Scale(128, _currentDpi);
        var buttonHeight = Math.Max(1, _footerPanel.Height - padding);
        ClearButton.Bounds = new Rectangle(Math.Max(padding, _footerPanel.Width - padding - clearWidth), padding / 2, clearWidth, buttonHeight);
        MarkAllButton.Bounds = new Rectangle(Math.Max(padding, ClearButton.Left - gap - markWidth), padding / 2, markWidth, buttonHeight);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        RebuildHeaderFont();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposing)
        {
            _disposing = true;
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            HistoryList.ItemActivated -= OnHistoryItemActivated;
            CloseButtonControl.Click -= OnCloseRequested;
            MarkAllButton.Click -= OnMarkAllRequested;
            ClearButton.Click -= OnClearRequested;
            _headerFont?.Dispose();
            _headerFont = null;
        }

        base.Dispose(disposing);
    }

    private void OnHistoryItemActivated(object? sender, BootstrapNotificationHistoryItemActivatedEventArgs e)
    {
        ItemActivated?.Invoke(this, e);
    }

    private void OnMarkAllRequested(object? sender, EventArgs e)
    {
        MarkAllRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClearRequested(object? sender, EventArgs e)
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Hide();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed) return;
        ApplyTheme();
        Invalidate(true);
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
        _headerPanel.BackColor = colors.Surface;
        _contentPanel.BackColor = colors.Surface;
        _footerPanel.BackColor = colors.SurfaceSecondary;
        _titleLabel.BackColor = colors.Surface;
        _titleLabel.ForeColor = colors.Text;
        EmptyLabel.BackColor = colors.Surface;
        EmptyLabel.ForeColor = colors.MutedText;
    }

    private void RebuildHeaderFont()
    {
        _headerFont?.Dispose();
        _headerFont = new Font(Font, FontStyle.Bold);
        if (_titleLabel is not null)
        {
            _titleLabel.Font = _headerFont;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposing || IsDisposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapNotificationCenterWindow));
        }
    }
}
