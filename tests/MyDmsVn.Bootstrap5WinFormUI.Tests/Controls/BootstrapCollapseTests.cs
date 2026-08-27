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
public sealed class BootstrapCollapseTests
{
    [Test]
    public void DefaultsMatchPhase9Contract()
    {
        using var collapse = new BootstrapCollapse();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(collapse.Expanded, Is.True);
            Assert.That(collapse.ExpandedHeightMode, Is.EqualTo(BootstrapCollapseHeightMode.Auto));
            Assert.That(collapse.ExpandedHeight, Is.EqualTo(0));
            Assert.That(collapse.AnimationDuration, Is.EqualTo(TimeSpan.FromMilliseconds(200)));
            Assert.That(collapse.AnimationProgress, Is.EqualTo(1.0).Within(0.0001));
            Assert.That(collapse.IsAnimating, Is.False);
            Assert.That(collapse.TabStop, Is.False);
        }));
    }

    [Test]
    public void FixedHeightExpandCollapseAndToggleReachExactFinalHeightsWithReducedMotion()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                ExpandedHeight = 144,
                Expanded = false
            };

            Assert.That(collapse.Height, Is.EqualTo(0));

            collapse.Expand();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(collapse.Expanded, Is.True);
                Assert.That(collapse.Height, Is.EqualTo(144));
                Assert.That(collapse.AnimationProgress, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(collapse.IsAnimating, Is.False);
            }));

            collapse.Toggle();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(collapse.Expanded, Is.False);
                Assert.That(collapse.Height, Is.EqualTo(0));
                Assert.That(collapse.AnimationProgress, Is.EqualTo(0.0).Within(0.0001));
                Assert.That(collapse.IsAnimating, Is.False);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AutoHeightMeasuresVisibleContentAndTracksContentResizeWhenExpanded()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                Width = 260,
                Padding = new Padding(8),
                ExpandedHeightMode = BootstrapCollapseHeightMode.Auto
            };
            using var content = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                Margin = Padding.Empty
            };

            collapse.Controls.Add(content);
            collapse.PerformLayout();
            collapse.Collapse();
            collapse.Expand();
            var initialHeight = collapse.Height;

            content.Height = 92;
            collapse.PerformLayout();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(initialHeight, Is.GreaterThanOrEqualTo(64));
                Assert.That(collapse.Height, Is.GreaterThanOrEqualTo(108));
                Assert.That(collapse.AnimationProgress, Is.EqualTo(1.0).Within(0.0001));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void FixedExpandedHeightChangeUpdatesStableExpandedControl()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                ExpandedHeight = 100
            };

            collapse.ExpandedHeight = 180;

            Assert.That(collapse.Height, Is.EqualTo(180));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ExpandedChangedFiresOnlyWhenRequestedStateActuallyChanges()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                ExpandedHeight = 100
            };
            var changeCount = 0;
            collapse.ExpandedChanged += (_, _) => changeCount++;

            collapse.Expand();
            collapse.Collapse();
            collapse.Collapse();
            collapse.Expand();

            Assert.That(changeCount, Is.EqualTo(2));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void RapidToggleSequenceEndsInLastRequestedState()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                ExpandedHeight = 120,
                Expanded = false
            };

            collapse.Expand();
            collapse.Collapse();
            collapse.Expand();
            collapse.Collapse();
            collapse.Expand();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(collapse.Expanded, Is.True);
                Assert.That(collapse.Height, Is.EqualTo(120));
                Assert.That(collapse.AnimationProgress, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(collapse.IsAnimating, Is.False);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void InvalidDurationAndExpandedHeightAreRejected()
    {
        using var collapse = new BootstrapCollapse();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => collapse.AnimationDuration = TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => collapse.AnimationDuration = TimeSpan.FromMilliseconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => collapse.ExpandedHeight = -1));
    }

    [Test]
    public void SettingExpandedPropertyUsesSameStateTransitionContractAsMethods()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Dark,
                reducedMotion: true);

            using var collapse = new BootstrapCollapse
            {
                ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
                ExpandedHeight = 132
            };

            collapse.Expanded = false;
            Assert.That(collapse.Height, Is.EqualTo(0));

            collapse.Expanded = true;
            Assert.That(collapse.Height, Is.EqualTo(132));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }
}
