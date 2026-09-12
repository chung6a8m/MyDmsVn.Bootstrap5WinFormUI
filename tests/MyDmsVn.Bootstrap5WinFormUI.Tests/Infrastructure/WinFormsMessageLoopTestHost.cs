using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

internal sealed class WinFormsMessageLoopTestHost : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _timeout;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new ManualResetEventSlim();
    private readonly ManualResetEventSlim _loopStopped = new ManualResetEventSlim();
    private Control? _dispatcher;
    private ApplicationContext? _applicationContext;
    private ExceptionDispatchInfo? _startupFailure;
    private ExceptionDispatchInfo? _loopFailure;
    private bool _loopFailureObserved;
    private bool _disposed;

    public WinFormsMessageLoopTestHost()
        : this(DefaultTimeout)
    {
    }

    public WinFormsMessageLoopTestHost(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        _timeout = timeout;
        _uiThread = new Thread(RunMessageLoop)
        {
            IsBackground = true,
            Name = nameof(WinFormsMessageLoopTestHost)
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        if (!_ready.Wait(_timeout))
        {
            throw new TimeoutException("The WinForms message loop did not start within the configured timeout.");
        }

        _startupFailure?.Throw();
    }

    internal bool IsUiThreadAlive => _uiThread.IsAlive;

    public void Run(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        Run<object?>(() =>
        {
            action();
            return null;
        });
    }

    public T Run<T>(Func<T> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        ThrowIfDisposed();
        ThrowLoopFailureIfStopped();
        var completion = new ManualResetEventSlim();
        var result = default(T);
        ExceptionDispatchInfo? failure = null;

        try
        {
            _dispatcher!.BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception exception)
                {
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
                finally
                {
                    completion.Set();
                }
            }));
        }
        catch (InvalidOperationException)
        {
            ThrowLoopFailureIfStopped();
            throw;
        }

        var completed = WaitHandle.WaitAny(new[] { completion.WaitHandle, _loopStopped.WaitHandle }, _timeout);
        if (completed == WaitHandle.WaitTimeout)
        {
            throw new TimeoutException("WinForms UI work did not complete within the configured timeout.");
        }

        if (completed == 1)
        {
            ThrowLoopFailureIfStopped();
            throw new InvalidOperationException("The WinForms message loop stopped before UI work completed.");
        }

        failure?.Throw();
        ThrowLoopFailureIfStopped();
        return result!;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_uiThread.IsAlive && _dispatcher is not null && !_dispatcher.IsDisposed)
        {
            try
            {
                _dispatcher.BeginInvoke((MethodInvoker)(() => _applicationContext?.ExitThread()));
            }
            catch (InvalidOperationException)
            {
                _applicationContext?.ExitThread();
            }
        }

        if (_uiThread.IsAlive && !_uiThread.Join(ShutdownTimeout))
        {
            throw new TimeoutException("The WinForms UI thread did not stop within the configured timeout.");
        }

        _ready.Dispose();
        _loopStopped.Dispose();
        ThrowLoopFailureIfUnobserved();
    }

    private void RunMessageLoop()
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            _dispatcher = new Control();
            _ = _dispatcher.Handle;
            _applicationContext = new ApplicationContext();
            _dispatcher.BeginInvoke((MethodInvoker)(() => _ready.Set()));
            Application.Run(_applicationContext);
        }
        catch (Exception exception)
        {
            var actualException = exception is System.Reflection.TargetInvocationException { InnerException: not null } targetInvocationException
                ? targetInvocationException.InnerException!
                : exception;
            var failure = ExceptionDispatchInfo.Capture(actualException);
            _loopFailure = failure;
            _startupFailure = failure;
            _ready.Set();
        }
        finally
        {
            _dispatcher?.Dispose();
            _dispatcher = null;
            _applicationContext?.Dispose();
            _applicationContext = null;
            _loopStopped.Set();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WinFormsMessageLoopTestHost));
        }
    }

    private void ThrowLoopFailureIfStopped()
    {
        if (_loopStopped.IsSet)
        {
            ThrowLoopFailureIfUnobserved();
        }
    }

    private void ThrowLoopFailureIfUnobserved()
    {
        if (_loopFailure is null || _loopFailureObserved)
        {
            return;
        }

        _loopFailureObserved = true;
        _loopFailure.Throw();
    }
}
