using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Animation;

/// <summary>
/// Represents a repeating UI-thread animation with normalized eased cycle progress.
/// </summary>
public sealed class BootstrapLoopAnimation : IDisposable
{
    private readonly IAnimationClock _clock;
    private readonly IAnimationFrameScheduler _scheduler;
    private readonly Func<bool> _reducedMotionProvider;
    private readonly AnimationOwnerLifecycle _ownerLifecycle;
    private double _rawProgress;
    private double _baseRawProgress;
    private double _progress;
    private bool _isRunning;
    private bool _resumeWhenVisible;
    private bool _disposed;

    /// <summary>
    /// Initializes a repeating animation.
    /// </summary>
    /// <param name="duration">Duration of one cycle. Must be greater than zero.</param>
    /// <param name="easing">Optional easing function. Linear easing is used when omitted.</param>
    /// <param name="owner">Optional control whose visibility and disposal govern scheduling.</param>
    public BootstrapLoopAnimation(TimeSpan duration, Func<double, double>? easing = null, Control? owner = null)
        : this(
            duration,
            easing ?? BootstrapEasing.Linear,
            owner,
            new StopwatchAnimationClock(),
            new WinFormsAnimationFrameScheduler(),
            () => BootstrapThemeManager.CurrentTheme.ReducedMotion)
    {
    }

    internal BootstrapLoopAnimation(
        TimeSpan duration,
        Func<double, double> easing,
        Control? owner,
        IAnimationClock clock,
        IAnimationFrameScheduler scheduler,
        Func<bool> reducedMotionProvider)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Animation duration must be greater than zero.");
        }

        Duration = duration;
        Easing = easing ?? throw new ArgumentNullException(nameof(easing));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _reducedMotionProvider = reducedMotionProvider ?? throw new ArgumentNullException(nameof(reducedMotionProvider));
        _ownerLifecycle = new AnimationOwnerLifecycle(owner, PauseForOwner, ResumeForOwner, StopForDisposedOwner);
    }

    /// <summary>Gets the duration of one loop cycle.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the easing function applied to raw normalized cycle progress.</summary>
    public Func<double, double> Easing { get; }

    /// <summary>Gets the current eased cycle progress in the range 0 through 1.</summary>
    public double Progress => _progress;

    /// <summary>Gets whether frame scheduling is currently active.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>Occurs when the published eased cycle progress changes.</summary>
    public event EventHandler? ProgressChanged;

    /// <summary>Starts or resumes the repeating animation when the owner can render.</summary>
    public void Start()
    {
        ThrowIfDisposed();

        if (_ownerLifecycle.IsOwnerDisposed || _isRunning)
        {
            return;
        }

        if (_reducedMotionProvider())
        {
            ResetProgress();
            _resumeWhenVisible = false;
            return;
        }

        if (!_ownerLifecycle.IsOwnerVisible)
        {
            _resumeWhenVisible = true;
            return;
        }

        BeginScheduling();
    }

    /// <summary>Stops scheduling and freezes the current cycle position.</summary>
    public void Stop()
    {
        ThrowIfDisposed();
        _resumeWhenVisible = false;

        if (!_isRunning)
        {
            return;
        }

        CaptureAndPause(false);
    }

    /// <summary>Resets the cycle to zero and begins a fresh loop when the owner can render.</summary>
    public void Restart()
    {
        ThrowIfDisposed();

        if (_ownerLifecycle.IsOwnerDisposed)
        {
            return;
        }

        if (_isRunning)
        {
            _isRunning = false;
            _scheduler.Stop();
        }

        _resumeWhenVisible = false;
        ResetProgress();
        if (_disposed)
        {
            return;
        }

        if (_reducedMotionProvider())
        {
            return;
        }

        if (!_ownerLifecycle.IsOwnerVisible)
        {
            _resumeWhenVisible = true;
            return;
        }

        BeginScheduling();
    }

    /// <summary>Releases timer and owner-lifecycle resources.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isRunning = false;
        _resumeWhenVisible = false;
        _ownerLifecycle.Dispose();
        _scheduler.Dispose();
    }

    private void BeginScheduling()
    {
        _baseRawProgress = _rawProgress;
        _clock.Restart();
        _isRunning = true;
        _resumeWhenVisible = false;
        _scheduler.Start(OnFrame);
    }

    private void OnFrame()
    {
        if (_disposed || !_isRunning)
        {
            return;
        }

        var rawProgress = CalculateRawProgress();
        _rawProgress = rawProgress;
        PublishProgress(rawProgress);
    }

    private double CalculateRawProgress()
    {
        var totalProgress = _baseRawProgress + (_clock.Elapsed.TotalMilliseconds / Duration.TotalMilliseconds);
        return totalProgress - Math.Floor(totalProgress);
    }

    private void CaptureAndPause(bool resumeWhenVisible)
    {
        var rawProgress = CalculateRawProgress();
        _isRunning = false;
        _scheduler.Stop();
        _rawProgress = rawProgress;
        _baseRawProgress = rawProgress;
        _resumeWhenVisible = resumeWhenVisible;
        PublishProgress(rawProgress);
    }

    private void ResetProgress()
    {
        _rawProgress = 0.0;
        _baseRawProgress = 0.0;
        PublishProgress(0.0);
    }

    private void PublishProgress(double rawProgress)
    {
        var easedProgress = BootstrapEasing.Normalize(Easing(BootstrapEasing.Normalize(rawProgress)));
        if (_progress.Equals(easedProgress))
        {
            return;
        }

        _progress = easedProgress;
        ProgressChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PauseForOwner()
    {
        if (_disposed || !_isRunning)
        {
            return;
        }

        CaptureAndPause(true);
    }

    private void ResumeForOwner()
    {
        if (_disposed || !_resumeWhenVisible || _ownerLifecycle.IsOwnerDisposed)
        {
            return;
        }

        BeginScheduling();
    }

    private void StopForDisposedOwner()
    {
        if (_disposed)
        {
            return;
        }

        _resumeWhenVisible = false;
        if (!_isRunning)
        {
            return;
        }

        var rawProgress = CalculateRawProgress();
        _isRunning = false;
        _scheduler.Stop();
        _rawProgress = rawProgress;
        _baseRawProgress = rawProgress;
        PublishProgress(rawProgress);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapLoopAnimation));
        }
    }
}
