using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

[TestFixture]
[NonParallelizable]
public sealed class WinFormsTestEnvironmentTests
{
    [Test]
    public void UnhandledUiExceptionOnAnotherStaThreadPropagatesOutOfMessageLoop()
    {
        Exception? propagatedException = null;
        Exception? threadException = null;
        using var completed = new ManualResetEventSlim();

        ThreadExceptionEventHandler threadExceptionHandler = (_, e) =>
        {
            threadException = e.Exception;
            Application.ExitThread();
        };

        var thread = new Thread(() =>
        {
            Application.ThreadException += threadExceptionHandler;

            try
            {
                using var form = new Form
                {
                    ShowInTaskbar = false,
                    WindowState = FormWindowState.Minimized
                };

                form.Shown += (_, _) =>
                    form.BeginInvoke((MethodInvoker)(() =>
                        throw new InvalidOperationException("WinForms fail-fast probe")));

                try
                {
                    Application.Run(form);
                }
                catch (Exception exception)
                {
                    propagatedException = exception;
                }
            }
            finally
            {
                Application.ThreadException -= threadExceptionHandler;
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(10)), Is.True, "The WinForms probe thread did not complete.");
        Assert.That(thread.Join(TimeSpan.FromSeconds(1)), Is.True, "The WinForms probe thread did not terminate.");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(threadException, Is.Null, "The exception was routed to Application.ThreadException instead of propagating.");
            Assert.That(propagatedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(propagatedException!.Message, Is.EqualTo("WinForms fail-fast probe"));
        }));
    }
}
