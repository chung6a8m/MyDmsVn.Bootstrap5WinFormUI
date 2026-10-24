using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Wraps one content region with a delayed, token-owned busy presentation.</summary>
public sealed class BootstrapBusyHost : Panel
{
    private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(150);
    private readonly Control _content;
    private readonly Panel _overlay;
    private readonly BootstrapSpinner _spinner;
    private readonly Label _message;
    private readonly Button _cancel;
    private readonly IBootstrapBusyDelay _delay;
    private readonly List<BusyToken> _tokens = new();
    private readonly int _uiThreadId;
    private bool _showScheduled;
    private bool _themeSubscribed;

    /// <summary>Creates a host that owns the supplied content control.</summary>
    public BootstrapBusyHost(Control content) : this(content, new BootstrapBusyDelay()) { }

    internal BootstrapBusyHost(Control content, IBootstrapBusyDelay delay)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        if (content.Parent is not null)
        {
            throw new ArgumentException("Content must be unparented.", nameof(content));
        }

        _uiThreadId = Thread.CurrentThread.ManagedThreadId;
        _content.Dock = DockStyle.Fill;
        _overlay = new Panel { Dock = DockStyle.Fill, Visible = false, TabStop = false };
        _spinner = new BootstrapSpinner { SpinnerSize = BootstrapSpinnerSize.Large, TabStop = false, AccessibleName = "Busy" };
        _message = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, TabStop = false, AccessibleRole = AccessibleRole.StaticText };
        _cancel = new Button { Text = "Cancel", AutoSize = false, Visible = false, AccessibleRole = AccessibleRole.PushButton };
        _cancel.Click += OnCancelClick;
        _overlay.Controls.Add(_spinner);
        _overlay.Controls.Add(_message);
        _overlay.Controls.Add(_cancel);
        _overlay.Layout += OnOverlayLayout;
        Controls.Add(_content);
        Controls.Add(_overlay);
        _overlay.BringToFront();
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyTheme();
    }

    /// <summary>Gets the wrapped content. The host owns its lifetime.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control Content => _content;

    /// <summary>Begins a busy presentation; dispose the returned token when the operation ends.</summary>
    public IDisposable Begin(BusyPresentation presentation)
    {
        AssertUiThread();
        if (IsDisposed) throw new ObjectDisposedException(nameof(BootstrapBusyHost));
        if (presentation is null) throw new ArgumentNullException(nameof(presentation));
        var token = new BusyToken(this, presentation);
        _tokens.Add(token);
        ApplyActivePresentation();
        return token;
    }

    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            HidePresentation();
        }
        else
        {
            ApplyActivePresentation();
        }
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyActivePresentation();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        HidePresentation();
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        _overlay?.PerformLayout();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            _delay.Cancel();
            _delay.Dispose();
            _tokens.Clear();
            _cancel.Click -= OnCancelClick;
            _overlay.Layout -= OnOverlayLayout;
        }

        base.Dispose(disposing);
    }

    private void Release(BusyToken token)
    {
        if (IsDisposed) return;
        AssertUiThread();
        if (!_tokens.Remove(token)) return;
        ApplyActivePresentation();
    }

    private void ApplyActivePresentation()
    {
        if (IsDisposed || _overlay is null || _tokens.Count == 0)
        {
            HidePresentation();
            return;
        }

        var active = _tokens[_tokens.Count - 1].Presentation;
        _message.Text = active.Message;
        _message.AccessibleName = active.Message;
        _cancel.Visible = active.CancelRequested is not null;
        _overlay.PerformLayout();
        if (!Visible || !IsHandleCreated || _overlay.Visible || _showScheduled) return;
        _showScheduled = true;
        _delay.Schedule(ShowDelay, ShowPresentation);
    }

    private void ShowPresentation()
    {
        _showScheduled = false;
        if (IsDisposed || !Visible || !IsHandleCreated || _tokens.Count == 0) return;
        _overlay.Visible = true;
        _overlay.BringToFront();
        _spinner.Start();
        _overlay.PerformLayout();
    }

    private void HidePresentation()
    {
        if (_showScheduled)
        {
            _delay.Cancel();
            _showScheduled = false;
        }

        if (_overlay is null) return;
        _overlay.Visible = false;
        _spinner?.Stop();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        if (_tokens.Count == 0 || !_overlay.Visible) return;
        _tokens[_tokens.Count - 1].Presentation.CancelRequested?.Invoke();
    }

    private void OnOverlayLayout(object? sender, LayoutEventArgs e)
    {
        var metrics = BootstrapThemeManager.CurrentTheme.Metrics;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var inset = DpiScaler.Scale(metrics.SpacingLG, dpi);
        var gap = DpiScaler.Scale(metrics.SpacingSM, dpi);
        var width = Math.Max(0, _overlay.ClientSize.Width - (2 * inset));
        var spinnerSize = _spinner.GetPreferredSize(Size.Empty);
        var messageHeight = DpiScaler.Scale(metrics.ControlHeight, dpi);
        var cancelHeight = _cancel.Visible ? DpiScaler.Scale(metrics.ControlHeightSmall, dpi) : 0;
        var total = spinnerSize.Height + gap + messageHeight + (_cancel.Visible ? gap + cancelHeight : 0);
        var top = Math.Max(inset, (_overlay.ClientSize.Height - total) / 2);
        _spinner.Bounds = new Rectangle(Math.Max(inset, (_overlay.ClientSize.Width - spinnerSize.Width) / 2), top, spinnerSize.Width, spinnerSize.Height);
        _message.Bounds = new Rectangle(inset, top + spinnerSize.Height + gap, width, messageHeight);
        if (_cancel.Visible)
        {
            var buttonWidth = Math.Min(width, DpiScaler.Scale(96, dpi));
            _cancel.Bounds = new Rectangle(Math.Max(inset, (_overlay.ClientSize.Width - buttonWidth) / 2), _message.Bottom + gap, buttonWidth, cancelHeight);
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed) return;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        _overlay.BackColor = colors.SurfaceSecondary;
        _message.BackColor = colors.SurfaceSecondary;
        _message.ForeColor = colors.Text;
        _cancel.BackColor = colors.Surface;
        _cancel.ForeColor = colors.Text;
        _overlay.PerformLayout();
        _overlay.Invalidate();
    }

    private void AssertUiThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
            throw new InvalidOperationException("Busy host tokens must be managed on the owning UI thread.");
    }

    private sealed class BusyToken : IDisposable
    {
        private BootstrapBusyHost? _owner;

        internal BusyToken(BootstrapBusyHost owner, BusyPresentation presentation)
        {
            _owner = owner;
            Presentation = presentation;
        }

        internal BusyPresentation Presentation { get; }

        public void Dispose()
        {
            var owner = _owner;
            if (owner is null) return;
            owner.Release(this);
            _owner = null;
        }
    }
}
