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
public sealed class BootstrapCardTests
{
    [Test]
    public void DefaultsMatchPhase8Contract()
    {
        using var card = new BootstrapCard();
        var metrics = BootstrapThemeManager.CurrentTheme.Metrics;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(card.ShowBorder, Is.True);
            Assert.That(card.ShowShadow, Is.False);
            Assert.That(card.BorderRadius, Is.EqualTo(-1));
            Assert.That(card.Padding, Is.EqualTo(new Padding(metrics.SpacingMD)));
            Assert.That(card.Header.Visible, Is.False);
            Assert.That(card.Body.Visible, Is.True);
            Assert.That(card.Footer.Visible, Is.False);
            Assert.That(card.Header.Dock, Is.EqualTo(DockStyle.Top));
            Assert.That(card.Body.Dock, Is.EqualTo(DockStyle.Fill));
            Assert.That(card.Footer.Dock, Is.EqualTo(DockStyle.Bottom));
        }));
    }

    [Test]
    public void HeaderBodyAndFooterAreStableChildContainers()
    {
        using var card = new BootstrapCard();
        var header = card.Header;
        var body = card.Body;
        var footer = card.Footer;

        header.Visible = true;
        footer.Visible = true;
        header.Controls.Add(new Label { Text = "Header" });
        body.Controls.Add(new Label { Text = "Body" });
        footer.Controls.Add(new Label { Text = "Footer" });
        card.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(card.Controls.Contains(header), Is.True);
            Assert.That(card.Controls.Contains(body), Is.True);
            Assert.That(card.Controls.Contains(footer), Is.True);
            Assert.That(header.Controls.Count, Is.EqualTo(1));
            Assert.That(body.Controls.Count, Is.EqualTo(1));
            Assert.That(footer.Controls.Count, Is.EqualTo(1));
            Assert.That(card.Header, Is.SameAs(header));
            Assert.That(card.Body, Is.SameAs(body));
            Assert.That(card.Footer, Is.SameAs(footer));
        }));
    }

    [Test]
    public void RuntimeThemeChangeUpdatesSectionSurfaceColors()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var card = new BootstrapCard();
            var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            BootstrapThemeManager.CurrentTheme = dark;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(card.Header.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(card.Body.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(card.Footer.BackColor, Is.EqualTo(dark.Colors.Surface));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void CustomPaddingIsNotOverwrittenByThemeChanges()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var card = new BootstrapCard
            {
                Padding = new Padding(20, 18, 16, 14)
            };

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.That(card.Padding, Is.EqualTo(new Padding(20, 18, 16, 14)));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var card = new BootstrapCard();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => card.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => card.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => card.BorderRadius = 0));
    }

    [Test]
    public void CardUsesDoubleBufferedCustomPainting()
    {
        using var card = new StyleProbeBootstrapCard();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(card.HasStyle(ControlStyles.UserPaint), Is.True);
            Assert.That(card.HasStyle(ControlStyles.OptimizedDoubleBuffer), Is.True);
            Assert.That(card.HasStyle(ControlStyles.ResizeRedraw), Is.True);
        }));
    }

    private sealed class StyleProbeBootstrapCard : BootstrapCard
    {
        public bool HasStyle(ControlStyles style)
        {
            return GetStyle(style);
        }
    }
}
