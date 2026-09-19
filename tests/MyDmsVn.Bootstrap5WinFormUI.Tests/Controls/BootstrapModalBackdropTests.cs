using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Compatibility;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalBackdropTests
{
    [Test]
    public void NoneCreatesNoBackdropWindow()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var count = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal { BackdropMode = BootstrapModalBackdropMode.None };
            owner.Show();
            var duringLoop = 0;
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                duringLoop = Application.OpenForms.Cast<Form>().Count(form => form.GetType().Name == "BootstrapModalBackdropWindow");
                dialog.DialogResult = DialogResult.Cancel;
            });
            return duringLoop;
        });

        Assert.That(count, Is.Zero);
    }

    [Test]
    public void DismissibleBackdropIsPostSnapshotEnabledAndRequestsCancel()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var states = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            owner.Show();
            var backdropEnabled = false;
            var ownerEnabled = true;
            var topMost = true;
            var showInTaskbar = true;
            var result = ModalTestHost.ShowAndDrive(modal, owner, _ =>
            {
                var backdrop = modal.BackdropWindow!;
                backdropEnabled = IsWindowEnabled(backdrop.Handle);
                ownerEnabled = IsWindowEnabled(owner.Handle);
                topMost = backdrop.TopMost;
                showInTaskbar = backdrop.ShowInTaskbar;
                backdrop.RequestClickForTests();
            });
            return (backdropEnabled, ownerEnabled, topMost, showInTaskbar, result);
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(states.backdropEnabled, Is.True);
            Assert.That(states.ownerEnabled, Is.False);
            Assert.That(states.topMost, Is.False);
            Assert.That(states.showInTaskbar, Is.False);
            Assert.That(states.result, Is.EqualTo(DialogResult.Cancel));
        }));
    }

    [Test]
    public void StaticBackdropDoesNotDismissAndTracksManagedOwnerBounds()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var closingCount = 0;
        var tracked = false;
        host.Run(() =>
        {
            using var owner = new Form { Bounds = new System.Drawing.Rectangle(120, 140, 500, 360) };
            using var modal = new BootstrapModal { BackdropMode = BootstrapModalBackdropMode.Static };
            modal.FormClosing += (_, _) => closingCount++;
            owner.Show();
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                var backdrop = modal.BackdropWindow!;
                backdrop.RequestClickForTests();
                owner.Bounds = new System.Drawing.Rectangle(180, 190, 420, 300);
                Application.DoEvents();
                tracked = backdrop.Bounds == owner.Bounds;
                dialog.DialogResult = DialogResult.OK;
            });
        });

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tracked, Is.True);
            Assert.That(closingCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeOwnerHandleIsResolvedAfterShowDialogOwnershipExists()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var matches = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal { BackdropMode = BootstrapModalBackdropMode.None };
            owner.Show();
            var result = false;
            ModalTestHost.ShowAndDrive(modal, owner, dialog =>
            {
                result = BootstrapModalNativeWindow.GetOwner(dialog.Handle) == owner.Handle;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return result;
        });

        Assert.That(matches, Is.True);
    }

    [Test]
    public void ArbitraryIWin32WindowOwnerUsesEstablishedNativeHandleWithoutManagedOwner()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var state = host.Run(() =>
        {
            using var owner = new Form { Bounds = new System.Drawing.Rectangle(90, 100, 460, 320) };
            using var modal = new BootstrapModal();
            owner.Show();
            var wrapper = new OwnerWindow(owner.Handle);
            var managedOwnerWasNull = false;
            var ownerMatched = false;
            var backdropTracked = false;
            ModalTestHost.ShowAndDrive(modal, wrapper, dialog =>
            {
                managedOwnerWasNull = modal.Owner is null;
                ownerMatched = BootstrapModalNativeWindow.GetOwner(dialog.Handle) == owner.Handle;
                backdropTracked = modal.BackdropWindow!.Bounds == owner.Bounds;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return (managedOwnerWasNull, ownerMatched, backdropTracked);
        });

        Assert.That(state, Is.EqualTo((true, true, true)));
    }

    [Test]
    public void ArbitraryIWin32WindowOwnerCentersModalAgainstNativeOwnerBounds()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var centered = host.Run(() =>
        {
            var working = Screen.PrimaryScreen!.WorkingArea;
            using var owner = new Form
            {
                Bounds = new System.Drawing.Rectangle(
                    working.Left + working.Width / 5,
                    working.Top + working.Height / 5,
                    360,
                    260)
            };
            using var modal = new BootstrapModal
            {
                ModalSize = BootstrapModalSize.Custom,
                Size = new System.Drawing.Size(240, 160),
                BackdropMode = BootstrapModalBackdropMode.None
            };
            owner.Show();
            var wrapper = new OwnerWindow(owner.Handle);
            var result = false;
            ModalTestHost.ShowAndDrive(modal, wrapper, dialog =>
            {
                result = dialog.Left == owner.Left + (owner.Width - dialog.Width) / 2 &&
                         dialog.Top == owner.Top + (owner.Height - dialog.Height) / 2;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return result;
        });

        Assert.That(centered, Is.True);
    }

    [Test]
    public void ParameterlessShowDialogResolvesTheNativeActiveOwnerWhenWindowsProvidesOne()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var resolved = host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            owner.Show();
            owner.Activate();
            var found = false;
            ModalTestHost.ShowAndDrive(modal, dialog =>
            {
                var nativeOwner = BootstrapModalNativeWindow.GetOwner(dialog.Handle);
                found = nativeOwner == IntPtr.Zero || nativeOwner == owner.Handle;
                dialog.DialogResult = DialogResult.Cancel;
            });
            return found;
        });

        Assert.That(resolved, Is.True);
    }

    private sealed class OwnerWindow : IWin32Window
    {
        public OwnerWindow(IntPtr handle) => Handle = handle;
        public IntPtr Handle { get; }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr windowHandle);
}
