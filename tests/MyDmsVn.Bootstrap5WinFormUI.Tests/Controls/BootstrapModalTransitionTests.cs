using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalTransitionTests
{
    [Test]
    public void ReducedMotionOpeningImmediatelyUsesFinalVisualStateWithoutScheduling()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var host = new WinFormsMessageLoopTestHost();
            var state = host.Run(() =>
            {
                using var owner = new Form();
                using var modal = new BootstrapModal();
                owner.Show();
                var opacity = 0d;
                var running = true;
                ModalTestHost.ShowAndDrive(modal, owner, dialog =>
                {
                    opacity = modal.Opacity;
                    running = modal.TransitionController.IsRunning;
                    dialog.DialogResult = DialogResult.Cancel;
                });
                return (opacity, running);
            });

            Assert.That(state, Is.EqualTo((1d, false)));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void CallerCloseDuringOpeningUsesNativeCloseLifecycle()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var closed = 0;
        host.Run(() =>
        {
            using var owner = new Form();
            using var modal = new BootstrapModal();
            modal.FormClosed += (_, _) => closed++;
            owner.Show();
            ModalTestHost.ShowAndDrive(modal, owner, dialog => dialog.Close());
        });

        Assert.That(closed, Is.EqualTo(1));
    }

    [Test]
    public void OpeningTransitionRestoresCallerOpacity()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
            using var host = new WinFormsMessageLoopTestHost();
            var opacity = host.Run(() =>
            {
                using var owner = new Form();
                using var modal = new BootstrapModal { Opacity = 0.8d };
                owner.Show();
                var observed = 0d;
                ModalTestHost.ShowAndDrive(modal, owner, dialog =>
                {
                    observed = modal.Opacity;
                    dialog.DialogResult = DialogResult.Cancel;
                });
                return observed;
            });
            Assert.That(opacity, Is.EqualTo(0.8d).Within(0.001d));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }
}
