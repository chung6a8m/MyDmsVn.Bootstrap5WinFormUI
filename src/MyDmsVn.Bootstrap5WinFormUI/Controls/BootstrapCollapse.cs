using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a reusable animated vertical expand/collapse container.
/// </summary>
[DefaultProperty(nameof(Expanded))]
[DefaultEvent(nameof(ExpandedChanged))]
public class BootstrapCollapse : Panel
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(200);

    private readonly Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> _animationFactory;
    private BootstrapAnimation? _animation;
    private bool _expanded = true;
    private BootstrapCollapseHeightMode _expandedHeightMode = BootstrapCollapseHeightMode.Auto;
    private int _expandedHeight;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private double _animationProgress = 1.0;
    private bool _isAnimating;
    private bool _changingVisualHeight;
    private bool _refreshingAutoHeight;
    private bool _themeSubscribed;
    private int _lastAutoExpandedHeight = 120;
    private int _transitionStartHeight;
    private int _transitionTargetHeight;
    private int _transitionExpandedReferenceHeight = 120;

    /// <summary>
    /// Initializes a designer-safe collapse container that is expanded by default.
    /// </summary>
    public BootstrapCollapse()
        : this((duration, easing, owner) => new BootstrapAnimation(duration, easing, owner))
    {
    }

    internal BootstrapCollapse(Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> animationFactory)
    {
        _animationFactory = animationFactory ?? throw new ArgumentNullException(nameof(animationFactory));

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Expandable and collapsible content region.";
        Size = new Size(320, _lastAutoExpandedHeight);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>
    /// Gets or sets whether the content is logically expanded.
    /// </summary>
    [Category("Behavior")]
    [Description("Gets or sets whether the collapse content is expanded.")]
    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set => SetExpanded(value);
    }

    /// <summary>
    /// Gets or sets how the expanded height is determined.
    /// </summary>
    [Category("Layout")]
    [Description("Determines whether expanded height is measured from content or taken from ExpandedHeight.")]
    [DefaultValue(BootstrapCollapseHeightMode.Auto)]
    public BootstrapCollapseHeightMode ExpandedHeightMode
    {
        get => _expandedHeightMode;
        set
        {
            if (!Enum.IsDefined(typeof(BootstrapCollapseHeightMode), value))
            {
                throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BootstrapCollapseHeightMode));
            }

            if (_expandedHeightMode == value)
            {
                return;
            }

            _expandedHeightMode = value;
            RefreshExpandedTargetForConfigurationChange();
        }
    }

    /// <summary>
    /// Gets or sets the exact expanded height used when <see cref="ExpandedHeightMode"/> is <see cref="BootstrapCollapseHeightMode.Fixed"/>.
    /// </summary>
    [Category("Layout")]
    [Description("Sets the fixed expanded height. The value must be non-negative.")]
    [DefaultValue(0)]
    public int ExpandedHeight
    {
        get => _expandedHeight;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Expanded height must be non-negative.");
            }

            if (_expandedHeight == value)
            {
                return;
            }

            _expandedHeight = value;
            if (_expandedHeightMode == BootstrapCollapseHeightMode.Fixed)
            {
                RefreshExpandedTargetForConfigurationChange();
            }
        }
    }

    /// <summary>
    /// Gets or sets the full expand/collapse transition duration.
    /// Reversing a partial transition uses the proportional remaining duration.
    /// </summary>
    [Category("Behavior")]
    [Description("Sets the full expand/collapse animation duration. The value must be greater than zero.")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.2000000")]
    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            if (_animationDuration == value)
            {
                return;
            }

            _animationDuration = value;
            if (_isAnimating)
            {
                StartTransitionToRequestedState();
            }
        }
    }

    /// <summary>
    /// Gets the current visual expansion amount from 0 (collapsed) through 1 (fully expanded).
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double AnimationProgress => _animationProgress;

    /// <summary>
    /// Gets whether an expand/collapse transition is currently pending or running.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsAnimating => _isAnimating;

    /// <summary>
    /// Occurs when the requested expanded state changes.
    /// </summary>
    public event EventHandler? ExpandedChanged;

    /// <summary>
    /// Occurs when <see cref="AnimationProgress"/> changes.
    /// </summary>
    public event EventHandler? AnimationProgressChanged;

    /// <summary>
    /// Requests the expanded state.
    /// </summary>
    public void Expand()
    {
        SetExpanded(true);
    }

    /// <summary>
    /// Requests the collapsed state.
    /// </summary>
    public void Collapse()
    {
        SetExpanded(false);
    }

    /// <summary>
    /// Reverses the current requested state.
    /// </summary>
    public void Toggle()
    {
        SetExpanded(!_expanded);
    }

    /// <inheritdoc />
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        e.Control.SizeChanged += OnChildLayoutChanged;
        e.Control.VisibleChanged += OnChildLayoutChanged;
        RefreshAutoExpandedHeight();
    }

    /// <inheritdoc />
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        e.Control.SizeChanged -= OnChildLayoutChanged;
        e.Control.VisibleChanged -= OnChildLayoutChanged;
        base.OnControlRemoved(e);
        RefreshAutoExpandedHeight();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (!_changingVisualHeight)
        {
            RefreshAutoExpandedHeight();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeAnimation();

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            foreach (Control child in Controls)
            {
                child.SizeChanged -= OnChildLayoutChanged;
                child.VisibleChanged -= OnChildLayoutChanged;
            }
        }

        base.Dispose(disposing);
    }

    private void SetExpanded(bool value)
    {
        if (_expanded == value)
        {
            return;
        }

        _expanded = value;
        ExpandedChanged?.Invoke(this, EventArgs.Empty);
        StartTransitionToRequestedState();
    }

    private void StartTransitionToRequestedState()
    {
        if (IsDisposed)
        {
            return;
        }

        CaptureCurrentAnimationProgressAndDispose();

        var expandedTarget = ResolveExpandedHeight();
        var targetHeight = _expanded ? expandedTarget : 0;
        var startHeight = Math.Max(0, Height);
        var expandedReferenceHeight = Math.Max(1, expandedTarget > 0 ? expandedTarget : Math.Max(startHeight, _lastAutoExpandedHeight));

        if (startHeight == targetHeight)
        {
            SetVisualHeight(targetHeight);
            SetAnimationProgress(_expanded ? 1.0 : 0.0);
            _isAnimating = false;
            return;
        }

        _transitionStartHeight = startHeight;
        _transitionTargetHeight = targetHeight;
        _transitionExpandedReferenceHeight = expandedReferenceHeight;
        _isAnimating = true;

        var duration = CalculateRemainingDuration(startHeight, targetHeight, expandedReferenceHeight);
        var animation = _animationFactory(duration, BootstrapEasing.EaseInOut, this);
        _animation = animation;
        animation.ProgressChanged += OnAnimationProgressChanged;
        animation.Completed += OnAnimationCompleted;
        animation.Start();
    }

    private void CaptureCurrentAnimationProgressAndDispose()
    {
        var animation = _animation;
        if (animation is null)
        {
            return;
        }

        if (animation.IsRunning)
        {
            animation.Stop();
        }

        DisposeAnimation();
    }

    private void OnAnimationProgressChanged(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _animation))
        {
            return;
        }

        var progress = animation.Progress;
        var visualHeight = InterpolateHeight(_transitionStartHeight, _transitionTargetHeight, progress);
        SetVisualHeight(visualHeight);
        SetAnimationProgress(CalculateExpansionProgress(visualHeight, _transitionExpandedReferenceHeight));
    }

    private void OnAnimationCompleted(object? sender, EventArgs e)
    {
        if (sender is not BootstrapAnimation animation || !ReferenceEquals(animation, _animation))
        {
            return;
        }

        SetVisualHeight(_transitionTargetHeight);
        SetAnimationProgress(_expanded ? 1.0 : 0.0);
        _isAnimating = false;
        DisposeAnimation();
    }

    private void RefreshExpandedTargetForConfigurationChange()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!_expanded)
        {
            if (_isAnimating)
            {
                StartTransitionToRequestedState();
            }

            return;
        }

        if (_isAnimating)
        {
            StartTransitionToRequestedState();
            return;
        }

        var expandedTarget = ResolveExpandedHeight();
        SetVisualHeight(expandedTarget);
        SetAnimationProgress(1.0);
    }

    private void RefreshAutoExpandedHeight()
    {
        if (_expandedHeightMode != BootstrapCollapseHeightMode.Auto || _refreshingAutoHeight || IsDisposed)
        {
            return;
        }

        _refreshingAutoHeight = true;
        try
        {
            var measuredHeight = MeasureAutoExpandedHeight();
            if (measuredHeight == _lastAutoExpandedHeight)
            {
                return;
            }

            _lastAutoExpandedHeight = measuredHeight;
            if (!_expanded)
            {
                return;
            }

            if (_isAnimating)
            {
                StartTransitionToRequestedState();
            }
            else
            {
                SetVisualHeight(measuredHeight);
                SetAnimationProgress(1.0);
            }
        }
        finally
        {
            _refreshingAutoHeight = false;
        }
    }

    private int ResolveExpandedHeight()
    {
        if (_expandedHeightMode == BootstrapCollapseHeightMode.Fixed)
        {
            return _expandedHeight;
        }

        var measured = MeasureAutoExpandedHeight();
        _lastAutoExpandedHeight = measured;
        return measured;
    }

    private int MeasureAutoExpandedHeight()
    {
        var hasVisibleContent = false;
        var contentBottom = Padding.Top;
        var availableWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal);

        foreach (Control child in Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            hasVisibleContent = true;
            var childHeight = Math.Max(0, child.Height);
            if (child.AutoSize || child.Dock == DockStyle.Fill)
            {
                var preferredWidth = Math.Max(0, availableWidth - child.Margin.Horizontal);
                var preferred = child.GetPreferredSize(new Size(preferredWidth, 0));
                childHeight = Math.Max(childHeight, preferred.Height);
            }

            var childTop = Math.Max(Padding.Top, child.Top);
            contentBottom = Math.Max(contentBottom, childTop + childHeight + child.Margin.Bottom);
        }

        if (!hasVisibleContent)
        {
            return Math.Max(Padding.Vertical, _lastAutoExpandedHeight);
        }

        return Math.Max(Padding.Vertical, contentBottom + Padding.Bottom);
    }

    private TimeSpan CalculateRemainingDuration(int startHeight, int targetHeight, int expandedReferenceHeight)
    {
        var distance = Math.Abs(targetHeight - startHeight);
        var ratio = Math.Min(1.0, distance / (double)Math.Max(1, expandedReferenceHeight));
        var milliseconds = Math.Max(1.0, _animationDuration.TotalMilliseconds * ratio);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static int InterpolateHeight(int startHeight, int targetHeight, double progress)
    {
        var value = startHeight + ((targetHeight - startHeight) * progress);
        return Math.Max(0, (int)Math.Round(value, MidpointRounding.AwayFromZero));
    }

    private static double CalculateExpansionProgress(int visualHeight, int expandedReferenceHeight)
    {
        if (expandedReferenceHeight <= 0)
        {
            return visualHeight > 0 ? 1.0 : 0.0;
        }

        return Math.Max(0.0, Math.Min(1.0, visualHeight / (double)expandedReferenceHeight));
    }

    private void SetVisualHeight(int height)
    {
        height = Math.Max(0, height);
        if (Height == height)
        {
            return;
        }

        _changingVisualHeight = true;
        try
        {
            Height = height;
        }
        finally
        {
            _changingVisualHeight = false;
        }
    }

    private void SetAnimationProgress(double value)
    {
        value = Math.Max(0.0, Math.Min(1.0, value));
        if (Math.Abs(_animationProgress - value) < 0.000001)
        {
            return;
        }

        _animationProgress = value;
        AnimationProgressChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnChildLayoutChanged(object? sender, EventArgs e)
    {
        RefreshAutoExpandedHeight();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_isAnimating && e.OldTheme.ReducedMotion != e.NewTheme.ReducedMotion)
        {
            StartTransitionToRequestedState();
        }
    }

    private void DisposeAnimation()
    {
        var animation = _animation;
        if (animation is null)
        {
            return;
        }

        _animation = null;
        animation.ProgressChanged -= OnAnimationProgressChanged;
        animation.Completed -= OnAnimationCompleted;
        animation.Dispose();
    }
}
