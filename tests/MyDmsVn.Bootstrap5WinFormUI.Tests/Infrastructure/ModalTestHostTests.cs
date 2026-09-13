using System;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

[TestFixture]
[NonParallelizable]
public sealed class ModalTestHostTests
{
    [Test]
    public void ShowAndDriveCompletesScheduledModalExit()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new Form();
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.DialogResult = DialogResult.OK);
        });

        Assert.That(result, Is.EqualTo(DialogResult.OK));
    }

    [Test]
    public void ShowAndDriveRethrowsScheduledException()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var expected = new InvalidOperationException("scheduled failure");

        var actual = Assert.Throws<InvalidOperationException>((Action)(() => host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new Form();
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, _ => throw expected);
        })));

        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void ShowAndDrivePropagatesCallerShownException()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var expected = new InvalidOperationException("caller Shown failure");

        var actual = Assert.Throws<InvalidOperationException>((Action)(() => host.Run(() =>
        {
            using var modal = new Form();
            modal.Shown += (_, _) => throw expected;
            return ModalTestHost.ShowAndDrive(modal, _ => modal.DialogResult = DialogResult.OK);
        })));

        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void ShowAndDriveSupportsCanceledFirstExitAndSecondExit()
    {
        using var host = new WinFormsMessageLoopTestHost();

        var result = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new Form();
            var attempts = 0;
            modal.FormClosing += (_, e) =>
            {
                e.Cancel = ++attempts == 1;
                if (e.Cancel)
                {
                    modal.BeginInvoke((MethodInvoker)(() => modal.DialogResult = DialogResult.OK));
                }
            };
            owner.Show();
            return ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.DialogResult = DialogResult.Cancel);
        });

        Assert.That(result, Is.EqualTo(DialogResult.OK));
    }

    [Test]
    public void ShowAndDriveFailsBoundedlyInsteadOfHanging()
    {
        using var host = new WinFormsMessageLoopTestHost(TimeSpan.FromSeconds(2));

        Assert.Throws<TimeoutException>((Action)(() => host.Run(() =>
        {
            using var modal = new Form();
            return ModalTestHost.ShowAndDrive(modal, _ => { }, TimeSpan.FromMilliseconds(100));
        })));
    }
}
