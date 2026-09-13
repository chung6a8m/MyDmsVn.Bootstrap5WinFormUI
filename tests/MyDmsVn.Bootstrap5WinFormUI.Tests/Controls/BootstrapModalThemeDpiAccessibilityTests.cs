using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

    [Test]
    public void TopLevelDpiChangeReappliesPresetWidthAndHeaderMetrics()
    {
        using var host = new WinFormsMessageLoopTestHost();
        var actual = host.Run(() =>
        {
            using var modal = new DpiTestModal
            {
                ModalSize = BootstrapModalSize.Default,
                BackdropMode = BootstrapModalBackdropMode.None
            };
            modal.Show();
            modal.SimulateTopLevelDpiChange(96, 144);
            var title = (Label)modal.Controls.Find("BootstrapModalTitle", true)[0];
            var result = (modal.Width, title.Parent!.Height);
            modal.Close();
            return result;
        });

        Assert.That(actual, Is.EqualTo((750, 72)));
    }

    private sealed class DpiTestModal : BootstrapModal
    {
        private const int WmDpiChanged = 0x02E0;

        public void SimulateTopLevelDpiChange(int oldDpi, int newDpi)
        {
            var suggested = new NativeRectangle
            {
                Left = Left,
                Top = Top,
                Right = Right,
                Bottom = Bottom
            };
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeRectangle)));
            try
            {
                Marshal.StructureToPtr(suggested, pointer, false);
                var packedDpi = (IntPtr)(newDpi | newDpi << 16);
                var message = Message.Create(Handle, WmDpiChanged, packedDpi, pointer);
                var constructor = typeof(DpiChangedEventArgs)
                    .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(candidate =>
                    {
                        var parameters = candidate.GetParameters();
                        return parameters.Length == 2 &&
                               parameters[0].ParameterType == typeof(int) &&
                               parameters[1].ParameterType == typeof(Message);
                    });
                var args = (DpiChangedEventArgs)constructor.Invoke(new object[] { oldDpi, message });
                OnDpiChanged(args);
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
