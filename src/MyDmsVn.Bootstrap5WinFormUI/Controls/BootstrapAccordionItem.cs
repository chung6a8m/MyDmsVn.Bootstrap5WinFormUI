using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Composes one focusable <see cref="BootstrapAccordionHeader"/> with one
/// <see cref="BootstrapCollapse"/> content region.
/// </summary>
[DefaultProperty(nameof(Expanded))]
[DefaultEvent(nameof(ExpandedChanged))]
public class BootstrapAccordionItem : Panel
{
    private readonly BootstrapAccordionHeader _header;
    private readonly BootstrapCollapse _collapse;
    private readonly Panel _body;
    private bool _flush;
    private bool _performingLayout;
    private bool _themeSubscribed;
    private bool _settingBodyPadding;
    private bool _useThemeBodyPadding = true;

    /// <summary>
    /// Initializes a collapsed designer-safe accordion item.
    /// </summary>
    public BootstrapAccordionItem()
        : this(new BootstrapCollapse())
    {
    }

    internal BootstrapAccordionItem(BootstrapCollapse collapse)
    {
        _collapse = collapse ?? throw new ArgumentNullException(nameof(collapse));
        _header = new BootstrapAccordionHeader
        {
            AutoSize = false,
            Dock = DockStyle.None
        };
        _body = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            TabStop = false
        };

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Accordion section.";
        Size = new Size(320, 48);

        _collapse.Dock = DockStyle.None;
        _collapse.BackColor = Color.Transparent;
        _collapse.TabStop = false;
        _collapse.Controls.Add(_body);

        Controls.Add(_collapse);
        Controls.Add(_header);

        _header.Click += OnHeaderClick;
        _header.SizeChanged += OnChildLayoutChanged;
        _collapse.ExpandedChanged += OnCollapseExpandedChanged;
        _collapse.AnimationProgressChanged += OnCollapseAnimationProgressChanged;
        _collapse.SizeChanged += OnChildLayoutChanged;
        _body.SizeChanged += OnChildLayoutChanged;
        _body.PaddingChanged += OnBodyPaddingChanged;

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeBodyPadding();

        // Items start collapsed without scheduling a construction-time transition.
        _collapse.Height = 0;
        _collapse.Expanded = false;
        SynchronizeHeaderState();
        PerformLayout();
    }

    /// <summary>
    /// Occurs when the requested expanded state changes.
    /// </summary>
    public event EventHandler? ExpandedChanged;

    /// <summary>
    /// Gets the stable focusable header control for this item.
    /// </summary>
    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapAccordionHeader Header => _header;

    /// <summary>
    /// Gets the stable panel that hosts application content inside the collapse region.
    /// </summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel Body => _body;

    /// <summary>
    /// Gets the underlying collapse primitive that owns expansion measurement and animation.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapCollapse Collapse => _collapse;

    /// <summary>
    /// Gets or sets whether this item is logically expanded.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets whether the accordion item is expanded.")]
    [DefaultValue(false)]
    public bool Expanded
    {
        get => _collapse.Expanded;
        set => _collapse.Expanded = value;
    }

    /// <summary>
    /// Gets or sets the full expand/collapse transition duration used by the composed collapse control.
    /// </summary>
    [Category("Behavior")]
    [Description("Sets the accordion item's full expand/collapse animation duration.")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.2000000")]
    public TimeSpan AnimationDuration
    {
        get => _collapse.AnimationDuration;
        set => _collapse.AnimationDuration = value;
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var headerPreferred = _header.GetPreferredSize(proposedSize);
        var width = proposedSize.Width > 0
            ? proposedSize.Width
            : Math.Max(320, headerPreferred.Width);
        var height = headerPreferred.Height + Math.Max(0, _collapse.Height);
        return new Size(Math.Max(1, width), Math.Max(1, height));
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (_performingLayout || IsDisposed)
        {
            return;
        }

        _performingLayout = true;
        try
        {
            var width = Math.Max(0, ClientSize.Width);
            var headerHeight = _header.GetPreferredSize(new Size(width, 0)).Height;
            var collapseHeight = Math.Max(0, _collapse.Height);

            _header.Bounds = new Rectangle(0, 0, width, headerHeight);
            _collapse.Bounds = new Rectangle(0, headerHeight, width, collapseHeight);
            if (_body.Width != _collapse.ClientSize.Width)
            {
                _body.Width = _collapse.ClientSize.Width;
            }

            var desiredHeight = headerHeight + collapseHeight;
            if (Height != desiredHeight)
            {
                Height = desiredHeight;
            }
        }
        finally
        {
            _performingLayout = false;
        }
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (_useThemeBodyPadding)
        {
            ApplyThemeBodyPadding();
        }

        PerformLayout();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var graphics = e.Graphics;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        try
        {
            PaintSurface(graphics, theme);
        }
        finally
        {
            graphics.SmoothingMode = previousSmoothing;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _header.Click -= OnHeaderClick;
            _header.SizeChanged -= OnChildLayoutChanged;
            _collapse.ExpandedChanged -= OnCollapseExpandedChanged;
            _collapse.AnimationProgressChanged -= OnCollapseAnimationProgressChanged;
            _collapse.SizeChanged -= OnChildLayoutChanged;
            _body.SizeChanged -= OnChildLayoutChanged;
            _body.PaddingChanged -= OnBodyPaddingChanged;

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }
        }

        base.Dispose(disposing);
    }

    internal bool Flush
    {
        get => _flush;
        set
        {
            if (_flush == value)
            {
                return;
            }

            _flush = value;
            _header.Flush = value;
            Invalidate();
        }
    }

    private void OnHeaderClick(object? sender, EventArgs e)
    {
        _collapse.Toggle();
    }

    private void OnCollapseExpandedChanged(object? sender, EventArgs e)
    {
        SynchronizeHeaderState();
        ExpandedChanged?.Invoke(this, EventArgs.Empty);
        PerformLayout();
    }

    private void OnCollapseAnimationProgressChanged(object? sender, EventArgs e)
    {
        SynchronizeHeaderState();
    }

    private void SynchronizeHeaderState()
    {
        _header.SetExpansionState(_collapse.Expanded, _collapse.AnimationProgress);
    }

    private void OnChildLayoutChanged(object? sender, EventArgs e)
    {
        if (!_performingLayout)
        {
            PerformLayout();
        }
    }

    private void OnBodyPaddingChanged(object? sender, EventArgs e)
    {
        if (!_settingBodyPadding)
        {
            _useThemeBodyPadding = false;
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_useThemeBodyPadding)
        {
            ApplyThemeBodyPadding();
        }

        PerformLayout();
        Invalidate();
    }

    private void ApplyThemeBodyPadding()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var padding = DpiScaler.Scale(new Padding(theme.Metrics.SpacingLG), dpi);
        _settingBodyPadding = true;
        try
        {
            _body.Padding = padding;
        }
        finally
        {
            _settingBodyPadding = false;
        }
    }

    private void PaintSurface(Graphics graphics, BootstrapTheme theme)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var borderWidth = Math.Max(1f, DpiScaler.Scale((float)theme.Metrics.BorderWidth, dpi));
        using var background = new SolidBrush(theme.Colors.Surface);

        if (_flush)
        {
            graphics.FillRectangle(background, ClientRectangle);
            var y = Math.Max(0f, ClientSize.Height - (borderWidth / 2f));
            using var separator = new Pen(theme.Colors.Border, borderWidth);
            graphics.DrawLine(separator, 0f, y, ClientSize.Width, y);
            return;
        }

        var inset = borderWidth / 2f;
        var bounds = new RectangleF(
            inset,
            inset,
            Math.Max(0f, ClientSize.Width - borderWidth),
            Math.Max(0f, ClientSize.Height - borderWidth));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var radius = DpiScaler.Scale(theme.Metrics.Radius, dpi);
        using var path = RoundedPath.Create(bounds, new CornerRadius(radius));
        using var border = new Pen(theme.Colors.Border, borderWidth);
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);
    }
}
