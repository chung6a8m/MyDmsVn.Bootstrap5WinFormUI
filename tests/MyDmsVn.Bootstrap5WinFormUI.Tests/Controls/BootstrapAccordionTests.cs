using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Animation;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapAccordionTests
{
    [Test]
    public void HeaderDefaultsAreFocusableAndCollapsed()
    {
        using var header = new BootstrapAccordionHeader();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(header.TabStop, Is.True);
            Assert.That(header.ShowChevron, Is.True);
            Assert.That(header.Icon, Is.Null);
            Assert.That(header.Expanded, Is.False);
            Assert.That(header.AnimationProgress, Is.EqualTo(0.0).Within(0.0001));
            Assert.That(header.AccessibleRole, Is.EqualTo(AccessibleRole.PushButton));
        }));
    }

    [Test]
    public void HeaderMouseEnterAndSpaceActivateThroughTheSameClickEvent()
    {
        using var header = new TestAccordionHeader
        {
            Size = new Size(280, 44)
        };
        var clickCount = 0;
        header.Click += (_, _) => clickCount++;

        header.SimulateMouseActivation(new Point(12, 12));
        header.SimulateKeyActivation(Keys.Enter);
        header.SimulateKeyActivation(Keys.Space);

        Assert.That(clickCount, Is.EqualTo(3));
    }

    [Test]
    public void ItemHeaderActivationTogglesCollapseAndSynchronizesHeaderState()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var item = new BootstrapAccordionItem();
            item.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            item.Collapse.ExpandedHeight = 96;

            item.Header.PerformClick();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(item.Expanded, Is.True);
                Assert.That(item.Collapse.Height, Is.EqualTo(96));
                Assert.That(item.Header.Expanded, Is.True);
                Assert.That(item.Header.AnimationProgress, Is.EqualTo(1.0).Within(0.0001));
            }));

            item.Header.PerformClick();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(item.Expanded, Is.False);
                Assert.That(item.Collapse.Height, Is.EqualTo(0));
                Assert.That(item.Header.Expanded, Is.False);
                Assert.That(item.Header.AnimationProgress, Is.EqualTo(0.0).Within(0.0001));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void HeaderChevronProgressTracksUnderlyingCollapseAnimationProgress()
    {
        var harness = new CollapseAnimationHarness();
        using var collapse = new BootstrapCollapse(harness.CreateAnimation)
        {
            ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
            ExpandedHeight = 120,
            Expanded = false
        };
        using var item = new BootstrapAccordionItem(collapse);

        item.Expanded = true;
        harness.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(collapse.AnimationProgress, Is.EqualTo(0.5).Within(0.02));
            Assert.That(item.Header.AnimationProgress, Is.EqualTo(collapse.AnimationProgress).Within(0.0001));
            Assert.That(item.Header.Expanded, Is.True);
        }));
    }

    [Test]
    public void SingleOpenModeCollapsesPreviouslyExpandedSibling()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var accordion = new BootstrapAccordion();
            var first = accordion.AddItem("First");
            var second = accordion.AddItem("Second");
            first.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            second.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            first.Collapse.ExpandedHeight = 80;
            second.Collapse.ExpandedHeight = 80;

            first.Expanded = true;
            second.Expanded = true;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(accordion.AllowMultipleOpen, Is.False);
                Assert.That(first.Expanded, Is.False);
                Assert.That(second.Expanded, Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void MultipleOpenModeAllowsMoreThanOneExpandedItem()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Dark,
                reducedMotion: true);

            using var accordion = new BootstrapAccordion
            {
                AllowMultipleOpen = true
            };
            var first = accordion.AddItem("First");
            var second = accordion.AddItem("Second");
            first.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            second.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            first.Collapse.ExpandedHeight = 80;
            second.Collapse.ExpandedHeight = 90;

            first.Expanded = true;
            second.Expanded = true;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(first.Expanded, Is.True);
                Assert.That(second.Expanded, Is.True);
                Assert.That(accordion.Items, Has.Count.EqualTo(2));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AccordionAppearanceAndDurationPropagateToExistingAndFutureItems()
    {
        using var accordion = new BootstrapAccordion();
        var first = accordion.AddItem("First");
        var duration = TimeSpan.FromMilliseconds(360);

        accordion.Flush = true;
        accordion.AnimationDuration = duration;
        var second = accordion.AddItem("Second");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Flush, Is.True);
            Assert.That(second.Flush, Is.True);
            Assert.That(first.AnimationDuration, Is.EqualTo(duration));
            Assert.That(second.AnimationDuration, Is.EqualTo(duration));
        }));
    }

    [Test]
    public void AddRemoveClearAndCollapseAllKeepTypedItemsInSync()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                BootstrapThemeMode.Light,
                reducedMotion: true);

            using var accordion = new BootstrapAccordion
            {
                AllowMultipleOpen = true
            };
            var first = accordion.AddItem("First");
            var second = accordion.AddItem("Second");
            first.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            second.Collapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
            first.Collapse.ExpandedHeight = 60;
            second.Collapse.ExpandedHeight = 60;
            first.Expanded = true;
            second.Expanded = true;

            accordion.CollapseAll();
            Assert.That(accordion.Items, Has.All.Matches<BootstrapAccordionItem>(item => !item.Expanded));

            Assert.That(accordion.RemoveItem(first), Is.True);
            Assert.That(accordion.Items, Has.Count.EqualTo(1));

            accordion.ClearItems();
            Assert.That(accordion.Items, Is.Empty);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AnimationDurationRejectsNonPositiveValues()
    {
        using var accordion = new BootstrapAccordion();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => accordion.AnimationDuration = TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => accordion.AnimationDuration = TimeSpan.FromMilliseconds(-1)));
    }

    private sealed class TestAccordionHeader : BootstrapAccordionHeader
    {
        public void SimulateMouseActivation(Point point)
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
        }

        public void SimulateKeyActivation(Keys key)
        {
            OnKeyDown(new KeyEventArgs(key));
            OnKeyUp(new KeyEventArgs(key));
        }
    }

    private sealed class CollapseAnimationHarness
    {
        private ManualAnimationClock? _clock;
        private ManualAnimationFrameScheduler? _scheduler;

        public BootstrapAnimation CreateAnimation(TimeSpan duration, Func<double, double> easing, Control owner)
        {
            _clock = new ManualAnimationClock();
            _scheduler = new ManualAnimationFrameScheduler();
            return new BootstrapAnimation(duration, easing, owner, _clock, _scheduler, () => false);
        }

        public void Advance(TimeSpan elapsed)
        {
            if (_clock is null || _scheduler is null)
            {
                throw new InvalidOperationException("No active animation is available.");
            }

            _clock.Advance(elapsed);
            _scheduler.FireFrame();
        }
    }
}
