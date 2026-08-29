using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapPopoverTests
{
    [Test]
    public void DefaultsMatchInteractivePopoverContract()
    {
        using var popover = new BootstrapPopover();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(popover.Target, Is.Null);
            Assert.That(popover.Content, Is.Null);
            Assert.That(popover.Trigger, Is.EqualTo(BootstrapPopoverTrigger.Click));
            Assert.That(popover.Placement, Is.EqualTo(BootstrapOverlayPlacement.Auto));
            Assert.That(popover.CollisionBehavior, Is.EqualTo(BootstrapOverlayCollisionBehavior.FlipAndShift));
            Assert.That(popover.Offset, Is.EqualTo(8));
            Assert.That(popover.BoundaryPadding, Is.EqualTo(8));
            Assert.That(popover.ContentPadding, Is.EqualTo(new Padding(12, 8, 12, 8)));
            Assert.That(popover.BorderRadius, Is.EqualTo(-1));
            Assert.That(popover.CloseOnEscape, Is.True);
            Assert.That(popover.CloseOnClickOutside, Is.True);
            Assert.That(popover.IsOpen, Is.False);
        }));
    }

    [Test]
    public void ContainerConstructorAddsOnlyPopoverWrapperAndRejectsNull()
    {
        using var container = new Container();
        using var popover = new BootstrapPopover(container);

        Assert.That(container.Components.Cast<IComponent>(), Does.Contain(popover));
        Assert.Throws<ArgumentNullException>((Action)(() => new BootstrapPopover(null!)));
    }

    [Test]
    public void ShowRequiresConfiguredLiveTargetAndContent()
    {
        using var popover = new BootstrapPopover();
        using var target = new Button();
        using var content = new Panel();

        Assert.Throws<InvalidOperationException>((Action)popover.Show);
        popover.Target = target;
        Assert.Throws<InvalidOperationException>((Action)popover.Show);
        popover.Target = null;
        popover.Content = content;
        Assert.Throws<InvalidOperationException>((Action)popover.Show);
    }

    [Test]
    public void ContentAssignmentRejectsDisposedOrParentedControls()
    {
        using var popover = new BootstrapPopover();
        var disposed = new Panel();
        disposed.Dispose();
        using var parent = new Panel();
        using var parented = new Panel();
        parent.Controls.Add(parented);

        Assert.Throws<ArgumentException>((Action)(() => popover.Content = disposed));
        Assert.Throws<InvalidOperationException>((Action)(() => popover.Content = parented));
        Assert.That(popover.Content, Is.Null);
    }

    [Test]
    public void ReplacingContentDetachesOldContentWithoutDisposingEitherControl()
    {
        using var popover = new BootstrapPopover();
        using var first = new Panel();
        using var second = new Panel();
        var firstDisposed = 0;
        var secondDisposed = 0;
        first.Disposed += (_, _) => firstDisposed++;
        second.Disposed += (_, _) => secondDisposed++;

        popover.Content = first;
        Assert.That(first.Parent, Is.Not.Null);
        popover.Content = second;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Parent, Is.Null);
            Assert.That(second.Parent, Is.Not.Null);
            Assert.That(firstDisposed, Is.Zero);
            Assert.That(secondDisposed, Is.Zero);
        }));
    }

    [Test]
    public void DisposeDoesNotDisposeCallerOwnedTargetOrContent()
    {
        using var target = new Button();
        using var content = new Panel();
        var targetDisposed = 0;
        var contentDisposed = 0;
        target.Disposed += (_, _) => targetDisposed++;
        content.Disposed += (_, _) => contentDisposed++;
        var popover = new BootstrapPopover { Target = target, Content = content };

        popover.Dispose();
        popover.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(targetDisposed, Is.Zero);
            Assert.That(contentDisposed, Is.Zero);
            Assert.That(content.Parent, Is.Null);
        }));
    }

    [Test]
    public void ExternalTargetOrContentDisposalClearsReferenceAndHideIsIdempotent()
    {
        using var popover = new BootstrapPopover();
        var target = new Button();
        var content = new Panel();
        popover.Target = target;
        popover.Content = content;

        Assert.DoesNotThrow((Action)(() =>
        {
            popover.Hide();
            popover.Hide();
        }));

        target.Dispose();
        Assert.That(popover.Target, Is.Null);
        content.Dispose();
        Assert.That(popover.Content, Is.Null);
    }

    [Test]
    public void FiveHundredOpenCloseCyclesReuseContentAndBalanceNativeEvents()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        using var form = new Form { Size = new Size(500, 400) };
        using var target = new Button { Location = new Point(100, 100), Size = new Size(100, 32), Text = "Open" };
        using var content = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        content.Controls.Add(new TextBox { Width = 180, Text = "Persistent" });
        form.Controls.Add(target);
        using var popover = new BootstrapPopover { Target = target, Content = content };
        var opened = 0;
        var closed = 0;
        popover.Opened += (_, _) => opened++;
        popover.Closed += (_, _) => closed++;

        try
        {
            form.Show();
            Application.DoEvents();
            for (var cycle = 0; cycle < 500; cycle++)
            {
                popover.Show();
                Application.DoEvents();
                popover.Hide();
                Application.DoEvents();
                if (cycle % 50 == 0)
                {
                    var mode = BootstrapThemeManager.CurrentTheme.Mode == BootstrapThemeMode.Light
                        ? BootstrapThemeMode.Dark
                        : BootstrapThemeMode.Light;
                    BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
                }
            }

            Assert.Multiple((Action)(() =>
            {
                Assert.That(opened, Is.EqualTo(500));
                Assert.That(closed, Is.EqualTo(500));
                Assert.That(popover.IsOpen, Is.False);
                Assert.That(popover.Content, Is.SameAs(content));
                Assert.That(content.IsDisposed, Is.False);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }
}
