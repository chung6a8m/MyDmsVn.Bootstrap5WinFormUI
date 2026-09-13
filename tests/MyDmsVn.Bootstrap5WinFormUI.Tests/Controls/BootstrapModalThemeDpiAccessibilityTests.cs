using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapModalThemeDpiAccessibilityTests
{
    [Test]
    public void RuntimeThemeChangeUpdatesOpenShellAndBackdropWithoutLosingFocus()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var host = new WinFormsMessageLoopTestHost();
            var verified = host.Run(() =>
            {
                using var owner = new Form();
                using var modal = new BootstrapModal();
                using var editor = new TextBox();
                modal.BodyPanel.Controls.Add(editor);
                modal.InitialFocusControl = editor;
                owner.Show();
                var result = false;
                ModalTestHost.ShowAndDrive(modal, owner, dialog =>
                {
                    var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                    BootstrapThemeManager.CurrentTheme = dark;
                    result = modal.BackColor == dark.Colors.Surface &&
                             modal.BackdropWindow!.BackColor == dark.Colors.Dark &&
                             editor.Focused;
                    dialog.DialogResult = DialogResult.Cancel;
                });
                return result;
            });

            Assert.That(verified, Is.True);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AccessibleNameFallsBackToInheritedText()
    {
        using var modal = new BootstrapModal { Text = "Customer editor" };
        Assert.That(modal.AccessibleName, Is.EqualTo("Customer editor"));
    }

    [Test]
    public void AccessibleNameFallbackTracksTextUntilCallerOverridesIt()
    {
        using var modal = new BootstrapModal { Text = "Create customer" };

        modal.Text = "Edit customer";
        Assert.That(modal.AccessibleName, Is.EqualTo("Edit customer"));

        modal.AccessibleName = "Customer details dialog";
        modal.Text = "View customer";
        Assert.That(modal.AccessibleName, Is.EqualTo("Customer details dialog"));
    }

    [Test]
    public void RuntimeThemeChangeUpdatesHeaderTypography()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var modal = new BootstrapModal { Text = "Customer editor" };
            var title = (Label)modal.Controls.Find("BootstrapModalTitle", true)[0];
            var originalFont = title.Font;
            var typography = new BootstrapThemeTypography(
                original.Typography.Body,
                original.Typography.BodySmall,
                original.Typography.Label,
                new BootstrapFontToken("Segoe UI", 16f, FontStyle.Bold),
                original.Typography.HeadingMedium);

            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
                original.Mode,
                original.Colors,
                original.Metrics,
                typography,
                original.ReducedMotion);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(title.Font, Is.Not.SameAs(originalFont));
                Assert.That(title.Font.SizeInPoints, Is.EqualTo(16f).Within(0.1f));
                Assert.That(title.Font.Style, Is.EqualTo(FontStyle.Bold));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void RtlMovesCloseCommandAndFooterFlowToVisualTrailingSides()
    {
        using var modal = new BootstrapModal { RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        modal.PerformLayout();
        var close = (Button)modal.Controls.Find("BootstrapModalClose", true)[0];

        Assert.Multiple((Action)(() =>
        {
            Assert.That(close.Dock, Is.EqualTo(DockStyle.Left));
            Assert.That(modal.FooterPanel.FlowDirection, Is.EqualTo(FlowDirection.LeftToRight));
        }));
    }
}
