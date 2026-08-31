using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapToastHostWindowTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void NativeContractIsBorderlessTaskbarHiddenAndNonActivatingWithoutTransparencyKey()
    {
        using var host = new BootstrapToastHostWindow();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.FormBorderStyle, Is.EqualTo(FormBorderStyle.None));
            Assert.That(host.ShowInTaskbar, Is.False);
            Assert.That(host.ControlBox, Is.False);
            Assert.That(host.StartPosition, Is.EqualTo(FormStartPosition.Manual));
            Assert.That(host.ShowWithoutActivationForTests, Is.True);
            Assert.That(host.CreateParamsExStyleForTests & 0x00000080, Is.Not.Zero);
            Assert.That(host.CreateParamsExStyleForTests & 0x08000000, Is.Not.Zero);
            Assert.That(host.TransparencyKey, Is.EqualTo(Color.Empty));
            Assert.That(host.ToastContainer.Dock, Is.EqualTo(DockStyle.Fill));
            Assert.That(host.Controls.Count, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ApplySettingsUsesWorkingAreaMarginAndTracksHeightLimit()
    {
        using var host = new BootstrapToastHostWindow();
        var screen = new BootstrapToastScreenInfo("DISPLAY2", new Rectangle(-1920, 0, 1920, 1040), 144);
        var settings = new BootstrapToastHostSettings(
            BootstrapToastPlacement.BottomLeft,
            12,
            3,
            new Padding(16),
            topMost: true);

        host.ApplySettings(screen, settings);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.ScreenDeviceName, Is.EqualTo("DISPLAY2"));
            Assert.That(host.Bounds, Is.EqualTo(new Rectangle(-1896, 24, 1872, 992)));
            Assert.That(host.TopMost, Is.True);
            Assert.That(host.ToastContainer.Placement, Is.EqualTo(BootstrapToastPlacement.BottomLeft));
            Assert.That(host.ToastContainer.ToastSpacing, Is.EqualTo(12));
            Assert.That(host.ToastContainer.MaximumVisibleToasts, Is.EqualTo(3));
            Assert.That(host.ToastContainer.MaximumStackHeightPixels, Is.EqualTo(host.ClientSize.Height));
        }));
    }

    [Test]
    public void RegionContainsToastEnvelopeAndExcludesFarBlankSpaceThenClearsWhenEmpty()
    {
        using var host = CreateConfiguredHost();
        var toast = new BootstrapToast { Text = "Visible", AutoHide = false, Width = 240 };
        host.ShowToast(toast);
        host.RefreshRegionNowForTests();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.HasOwnedToasts, Is.True);
            Assert.That(host.Region, Is.Not.Null);
            Assert.That(host.Region!.IsVisible(toast.Left, toast.Top), Is.True);
            Assert.That(host.Region.IsVisible(host.ClientSize.Width / 2, host.ClientSize.Height / 2), Is.False);
        }));

        toast.Dismiss();
        host.RefreshRegionNowForTests();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.HasOwnedToasts, Is.False);
            Assert.That(host.Visible, Is.False);
            Assert.That(host.Region, Is.Null);
        }));
    }

    [Test]
    public void RetirementHidesDismissesAndRejectsNewToasts()
    {
        using var host = CreateConfiguredHost();
        var first = new BootstrapToast { Text = "first", AutoHide = false };
        var second = new BootstrapToast { Text = "second", AutoHide = false };
        var becameEmpty = 0;
        host.BecameEmpty += (_, _) => becameEmpty++;
        host.ShowToast(first);
        host.ShowToast(second);

        host.RetireForScreenRemoval();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Visible, Is.False);
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(host.HasOwnedToasts, Is.False);
            Assert.That(becameEmpty, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>((Action)(() => host.ShowToast(new BootstrapToast())));
        }));
    }

    private static BootstrapToastHostWindow CreateConfiguredHost()
    {
        var host = new BootstrapToastHostWindow();
        host.ApplySettings(
            new BootstrapToastScreenInfo("DISPLAY1", new Rectangle(0, 0, 800, 600), 96),
            new BootstrapToastHostSettings(BootstrapToastPlacement.TopRight, 8, 5, new Padding(16), false));
        return host;
    }
}
