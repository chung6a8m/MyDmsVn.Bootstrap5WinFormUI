using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

internal static class ModalTestHost
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static DialogResult ShowAndDrive(
        Form modal,
        Form owner,
        Action<Form> afterShown,
        TimeSpan? timeout = null)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        return ShowAndDriveCore(modal, owner, afterShown, timeout ?? DefaultTimeout);
    }

    public static DialogResult ShowAndDrive(
        Form modal,
        IWin32Window owner,
        Action<Form> afterShown,
        TimeSpan? timeout = null)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        return ShowAndDriveCore(modal, owner, afterShown, timeout ?? DefaultTimeout);
    }

    public static DialogResult ShowAndDrive(
        Form modal,
        Action<Form> afterShown,
        TimeSpan? timeout = null)
    {
        return ShowAndDriveCore(modal, null, afterShown, timeout ?? DefaultTimeout);
    }

    private static DialogResult ShowAndDriveCore(
        Form modal,
        IWin32Window? owner,
        Action<Form> afterShown,
        TimeSpan timeout)
    {
        if (modal is null) throw new ArgumentNullException(nameof(modal));
        if (afterShown is null) throw new ArgumentNullException(nameof(afterShown));
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        ExceptionDispatchInfo? failure = null;
        var timedOut = false;
        EventHandler? shown = null;
        shown = (_, _) => modal.BeginInvoke((MethodInvoker)(() =>
        {
            try
            {
                afterShown(modal);
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
                modal.DialogResult = DialogResult.Abort;
            }
        }));

        using var watchdog = new System.Threading.Timer(_ =>
        {
            try
            {
                if (modal.IsHandleCreated && !modal.IsDisposed)
                {
                    modal.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (!modal.Visible) return;
                        timedOut = true;
                        modal.DialogResult = DialogResult.Abort;
                    }));
                }
            }
            catch (InvalidOperationException)
            {
            }
        }, null, timeout, Timeout.InfiniteTimeSpan);

        modal.Shown += shown;
        try
        {
            var result = owner is null ? modal.ShowDialog() : modal.ShowDialog(owner);
            failure?.Throw();
            if (timedOut)
            {
                throw new TimeoutException("The modal dialog did not exit within the configured timeout.");
            }

            return result;
        }
        finally
        {
            modal.Shown -= shown;
        }
    }
}
