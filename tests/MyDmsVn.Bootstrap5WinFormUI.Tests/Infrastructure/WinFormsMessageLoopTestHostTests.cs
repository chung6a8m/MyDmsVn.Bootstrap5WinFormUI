using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

[TestFixture]
[NonParallelizable]
public sealed class WinFormsMessageLoopTestHostTests
{
    [Test]
    public void RunExecutesOnStaThreadWithWindowsFormsSynchronizationContext()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var result = host.Run(() =>
            (Thread.CurrentThread.GetApartmentState(), SynchronizationContext.Current));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(result.Item1, Is.EqualTo(ApartmentState.STA));
            Assert.That(result.Item2, Is.TypeOf<WindowsFormsSynchronizationContext>());
        }));
    }

    [Test]
    public void RunProvidesRealBeginInvokeDispatch()
    {
        using var host = new WinFormsMessageLoopTestHost();
        using var dispatched = new ManualResetEventSlim();
        Control? control = null;

        try
        {
            host.Run(() =>
            {
                control = new Control();
                _ = control.Handle;
                control.BeginInvoke((MethodInvoker)(() => dispatched.Set()));
            });

            Assert.That(dispatched.Wait(TimeSpan.FromSeconds(2)), Is.True);
        }
        finally
        {
            host.Run(() => control?.Dispose());
        }
    }

    [Test]
    public void RunRethrowsTheOriginalUiException()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var expected = new InvalidOperationException("ui failure");

        var actual = Assert.Throws<InvalidOperationException>((Action)(() =>
            host.Run(() => throw expected)));

        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void DisposeRethrowsAnAsynchronousMessageLoopException()
    {
        var host = new WinFormsMessageLoopTestHost();
        using var callbackStarted = new ManualResetEventSlim();
        var expected = new InvalidOperationException("asynchronous UI failure");

        host.Run(() => SynchronizationContext.Current!.Post(_ =>
        {
            callbackStarted.Set();
            throw expected;
        }, null));

        Assert.That(callbackStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(SpinWait.SpinUntil(() => !host.IsUiThreadAlive, TimeSpan.FromSeconds(2)), Is.True);
        var actual = Assert.Throws<InvalidOperationException>((Action)host.Dispose);

        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void RunTimesOutInsteadOfWaitingForever()
    {
        using var host = new WinFormsMessageLoopTestHost(TimeSpan.FromMilliseconds(100));

        Assert.Throws<TimeoutException>((Action)(() =>
            host.Run(() => Thread.Sleep(300))));
    }

    [Test]
    public void DisposeTerminatesTheUiThread()
    {
        var host = new WinFormsMessageLoopTestHost();
        Assert.That(host.IsUiThreadAlive, Is.True);

        host.Dispose();

        Assert.That(host.IsUiThreadAlive, Is.False);
    }
}
