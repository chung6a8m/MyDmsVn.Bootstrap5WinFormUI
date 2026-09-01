using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
    private const int WmActivate = 0x0006;
    private const int WmActivateApp = 0x001C;

    private sealed class TestForm : Form
    {
        internal void RaiseDeactivate()
        {
            OnDeactivate(EventArgs.Empty);
        }
    }

    private sealed class InteractivePopoverFixture : IDisposable
    {
        public InteractivePopoverFixture()
        {
            Form = new TestForm
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(200, 200, 700, 500)
            };
            Target = new Button
            {
                Text = "Open",
                Location = new Point(30, 30),
                Size = new Size(120, 30),
                TabIndex = 1
            };
            Before = new Button
            {
                Text = "Before",
                Location = new Point(30, 80),
                Size = new Size(120, 30),
                TabIndex = 0
            };
            Outside = new Button
            {
                Text = "Outside",
                Location = new Point(180, 30),
                Size = new Size(120, 30),
                TabIndex = 2
            };
            Content = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                MinimumSize = new Size(280, 0)
            };
            Editor = new TextBox { Width = 220, TabIndex = 0 };
            Option = new CheckBox { AutoSize = true, Text = "Enable", TabIndex = 1 };
            Commands = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                TabIndex = 2,
                TabStop = false
            };
            Apply = new Button { Text = "Apply", TabIndex = 0 };
            Close = new Button { Text = "Close", TabIndex = 1 };
            Commands.Controls.Add(Apply);
            Commands.Controls.Add(Close);
            Content.Controls.Add(Editor);
            Content.Controls.Add(Option);
            Content.Controls.Add(Commands);
            Form.Controls.Add(Before);
            Form.Controls.Add(Target);
            Form.Controls.Add(Outside);
            Popover = new BootstrapPopover
            {
                Target = Target,
                Content = Content,
                CloseOnEscape = true,
                CloseOnClickOutside = true
            };
        }

        public TestForm Form { get; }

        public Button Target { get; }

        public Button Before { get; }

        public Button Outside { get; }

        public FlowLayoutPanel Content { get; }

        public TextBox Editor { get; }

        public CheckBox Option { get; }

        public FlowLayoutPanel Commands { get; }

        public Button Apply { get; }

        public Button Close { get; }

        public BootstrapPopover Popover { get; }

        public void Show()
        {
            Form.Show();
            Form.Activate();
            Target.Focus();
            Popover.Show();
            Application.DoEvents();
        }

        public void Dispose()
        {
            Popover.Dispose();
            Content.Dispose();
            Form.Dispose();
        }
    }

    private sealed class NestedContainerPopoverFixture : IDisposable
    {
        public NestedContainerPopoverFixture(bool containerTabStop)
        {
            Form = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(200, 200, 700, 500)
            };
            Target = new Button
            {
                Text = "Open",
                Location = new Point(30, 30),
                Size = new Size(120, 30)
            };
            Content = new Panel { Size = new Size(300, 150) };
            Before = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(220, 23),
                TabIndex = 0
            };
            var container = new UserControl
            {
                Location = new Point(10, 40),
                Size = new Size(260, 60),
                TabIndex = 1,
                TabStop = containerTabStop
            };
            NestedEditor = new TextBox
            {
                Location = new Point(0, 0),
                Size = new Size(150, 23),
                TabIndex = 0
            };
            NestedButton = new Button
            {
                Text = "Nested",
                Location = new Point(160, 0),
                Size = new Size(90, 23),
                TabIndex = 1
            };
            After = new TextBox
            {
                Location = new Point(10, 110),
                Size = new Size(220, 23),
                TabIndex = 2
            };
            container.Controls.Add(NestedEditor);
            container.Controls.Add(NestedButton);
            Content.Controls.Add(Before);
            Content.Controls.Add(container);
            Content.Controls.Add(After);
            Form.Controls.Add(Target);
            Popover = new BootstrapPopover
            {
                Target = Target,
                Content = Content
            };
        }

        public Form Form { get; }

        public Button Target { get; }

        public Panel Content { get; }

        public TextBox Before { get; }

        public TextBox NestedEditor { get; }

        public Button NestedButton { get; }

        public TextBox After { get; }

        public BootstrapPopover Popover { get; }

        public void Show()
        {
            Form.Show();
            Form.Activate();
            Target.Focus();
            Popover.Show();
            Application.DoEvents();
        }

        public void Dispose()
        {
            Popover.Dispose();
            Content.Dispose();
            Form.Dispose();
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
    public void FocusableRootContentReceivesFocusWhenPopoverOpens()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var target = new Button { Location = new Point(20, 20), Size = new Size(100, 30) };
        using var content = new TextBox { Size = new Size(160, 24) };
        using var popover = new BootstrapPopover { Target = target, Content = content };
        form.Controls.Add(target);

        try
        {
            form.Show();
            form.Activate();
            popover.Show();
            Application.DoEvents();

            Assert.That(content.Focused, Is.True);
        }
        finally
        {
            popover.Hide();
        }
    }

    [Test]
    public void NestedContentFocusUsesTabOrderAndSkipsIneligibleControls()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var target = new Button { Location = new Point(20, 20), Size = new Size(100, 30) };
        using var content = new Panel { Size = new Size(220, 120) };
        using var later = new TextBox { TabIndex = 5, Location = new Point(10, 80) };
        using var expected = new TextBox { TabIndex = 3, Location = new Point(10, 50) };
        using var hidden = new TextBox { TabIndex = 0, Visible = false };
        using var disabled = new TextBox { TabIndex = 1, Enabled = false };
        using var noTabStop = new TextBox { TabIndex = 2, TabStop = false };
        content.Controls.Add(later);
        content.Controls.Add(expected);
        content.Controls.Add(hidden);
        content.Controls.Add(disabled);
        content.Controls.Add(noTabStop);
        using var popover = new BootstrapPopover { Target = target, Content = content };
        form.Controls.Add(target);

        try
        {
            form.Show();
            form.Activate();
            popover.Show();
            Application.DoEvents();

            Assert.That(expected.Focused, Is.True);
        }
        finally
        {
            popover.Hide();
        }
    }

    [Test]
    public void AltDoesNotDismissOpenPopover()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();

        SendKeys.SendWait("%");
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.True);
            Assert.That(fixture.Editor.Focused, Is.True);
        }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void OwningFormDeactivateClosesPopover(bool closeOnClickOutside)
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Popover.CloseOnClickOutside = closeOnClickOutside;
        fixture.Show();
        Assert.That(fixture.Editor.Focused, Is.True);

        fixture.Form.RaiseDeactivate();
        Application.DoEvents();

        Assert.That(fixture.Popover.IsOpen, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ApplicationDeactivateMessageAfterContentFocusClosesPopover(bool closeOnClickOutside)
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Popover.CloseOnClickOutside = closeOnClickOutside;
        fixture.Show();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.True);
            Assert.That(fixture.Editor.Focused, Is.True);
            Assert.That(fixture.Popover.DropDownHandleForTest, Is.Not.EqualTo(IntPtr.Zero));
        }));

        SendMessage(fixture.Popover.DropDownHandleForTest, WmActivateApp, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();

        Assert.That(fixture.Popover.IsOpen, Is.False);
    }

    [Test]
    public void PopupDeactivateBackToOwnerKeepsPopoverWhenOutsideCloseIsDisabled()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Popover.CloseOnClickOutside = false;
        fixture.Show();

        SendMessage(
            fixture.Popover.DropDownHandleForTest,
            WmActivate,
            IntPtr.Zero,
            fixture.Form.Handle);
        Application.DoEvents();

        Assert.That(fixture.Popover.IsOpen, Is.True);
    }

    [Test]
    public void PopupDeactivateToSecondApplicationFormClosesPopoverWhenOutsideCloseIsDisabled()
    {
        using var fixture = new InteractivePopoverFixture();
        using var secondForm = new Form { ShowInTaskbar = false };
        fixture.Popover.CloseOnClickOutside = false;
        fixture.Show();
        var secondFormHandle = secondForm.Handle;

        SendMessage(
            fixture.Popover.DropDownHandleForTest,
            WmActivate,
            IntPtr.Zero,
            secondFormHandle);
        Application.DoEvents();

        Assert.That(fixture.Popover.IsOpen, Is.False);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void EscapeHonorsClosePolicyAndRestoresTargetFocus(bool closeOnEscape, bool remainsOpen)
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Popover.CloseOnEscape = closeOnEscape;
        fixture.Show();

        SendKeys.SendWait("{ESC}");
        Application.DoEvents();

        Assert.That(fixture.Popover.IsOpen, Is.EqualTo(remainsOpen));
        if (closeOnEscape)
        {
            Assert.That(fixture.Target.Focused, Is.True);
        }
        else
        {
            Assert.That(fixture.Editor.Focused, Is.True);
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void OutsideActivationHonorsClosePolicyAndPreservesOutsideFocus(bool closeOnClickOutside, bool remainsOpen)
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Popover.CloseOnClickOutside = closeOnClickOutside;
        fixture.Show();

        fixture.Outside.Select();
        fixture.Outside.Focus();
        fixture.Outside.PerformClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.EqualTo(remainsOpen));
            Assert.That(fixture.Outside.Focused, Is.True);
        }));
    }

    [Test]
    public void TabMovesForwardThroughNestedInteractiveContent()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();

        Assert.That(fixture.Editor.Focused, Is.True);
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.Option.Focused, Is.True);
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.Apply.Focused, Is.True);
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.Close.Focused, Is.True);
    }

    [Test]
    public void ShiftTabMovesBackwardThroughNestedInteractiveContent()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();
        fixture.Close.Focus();

        Assert.That(fixture.Close.Focused, Is.True);
        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.Apply.Focused, Is.True);
        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.Option.Focused, Is.True);
        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.Editor.Focused, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TabMovesForwardThroughNestedUserControl(bool containerTabStop)
    {
        using var fixture = new NestedContainerPopoverFixture(containerTabStop);
        fixture.Show();

        Assert.That(fixture.Before.Focused, Is.True);
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.NestedEditor.Focused, Is.True);
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.NestedButton.Focused, Is.True);
        SendTab(fixture.Content, forward: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.After.Focused, Is.True);
            Assert.That(fixture.Popover.IsOpen, Is.True);
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ShiftTabMovesBackwardThroughNestedUserControl(bool containerTabStop)
    {
        using var fixture = new NestedContainerPopoverFixture(containerTabStop);
        fixture.Show();
        fixture.After.Focus();

        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.NestedButton.Focused, Is.True);
        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.NestedEditor.Focused, Is.True);
        SendTab(fixture.Content, forward: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Before.Focused, Is.True);
            Assert.That(fixture.Popover.IsOpen, Is.True);
        }));
    }

    [Test]
    public void TabTraversalSkipsIneligibleControlsInBothDirections()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Commands.TabIndex = 6;
        using var hidden = new TextBox { TabIndex = 2, Visible = false };
        using var disabled = new TextBox { TabIndex = 3, Enabled = false };
        using var noTabStop = new TextBox { TabIndex = 4, TabStop = false };
        using var label = new Label { TabIndex = 5, Text = "Information" };
        fixture.Content.Controls.Add(hidden);
        fixture.Content.Controls.Add(disabled);
        fixture.Content.Controls.Add(noTabStop);
        fixture.Content.Controls.Add(label);
        fixture.Show();

        fixture.Option.Focus();
        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.Apply.Focused, Is.True);

        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.Option.Focused, Is.True);
    }

    [Test]
    public void ForwardTabFromLastContentControlClosesAndContinuesAfterTarget()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();
        fixture.Close.Focus();

        SendTab(fixture.Content, forward: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.False);
            Assert.That(fixture.Outside.Focused, Is.True);
            Assert.That(fixture.Target.Focused, Is.False);
        }));
    }

    [Test]
    public void BackwardTabFromFirstContentControlClosesAndContinuesBeforeTarget()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();

        SendTab(fixture.Content, forward: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.False);
            Assert.That(fixture.Before.Focused, Is.True);
            Assert.That(fixture.Target.Focused, Is.False);
        }));
    }

    [Test]
    public void RuntimeEligibilityChangesAreAppliedToTheNextTabRequest()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();
        fixture.Option.Enabled = false;

        SendTab(fixture.Content, forward: true);
        Assert.That(fixture.Apply.Focused, Is.True);

        fixture.Option.Enabled = true;
        fixture.Option.Visible = false;
        SendTab(fixture.Content, forward: false);
        Assert.That(fixture.Editor.Focused, Is.True);
    }

    [Test]
    public void TargetDisposalWhileKeyboardCallbackIsActiveClosesSafely()
    {
        using var fixture = new InteractivePopoverFixture();
        fixture.Show();

        fixture.Target.Dispose();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(fixture.Popover.IsOpen, Is.False);
            Assert.That(fixture.Popover.Target, Is.Null);
            Assert.DoesNotThrow((Action)(() => SendTab(fixture.Content, forward: true)));
        }));
    }

    [Test]
    public void RepeatedKeyboardCyclesDeliverOneOpenAndClosePerCycle()
    {
        using var fixture = new InteractivePopoverFixture();
        var opened = 0;
        var closed = 0;
        fixture.Popover.Opened += (_, _) => opened++;
        fixture.Popover.Closed += (_, _) => closed++;
        fixture.Form.Show();
        fixture.Form.Activate();

        for (var cycle = 0; cycle < 100; cycle++)
        {
            fixture.Target.Focus();
            fixture.Popover.Show();
            Application.DoEvents();
            if (cycle % 10 == 0)
            {
                SendKeys.SendWait("%");
                Application.DoEvents();
                Assert.That(fixture.Popover.IsOpen, Is.True, $"Alt cycle {cycle}");
                SendKeys.SendWait("{ESC}");
                Application.DoEvents();
                Assert.That(fixture.Target.Focused, Is.True, $"Escape cycle {cycle}");
            }
            else
            {
                SendTab(fixture.Content, forward: true);
                SendTab(fixture.Content, forward: true);
                SendTab(fixture.Content, forward: true);
                SendTab(fixture.Content, forward: true);
                Assert.That(fixture.Outside.Focused, Is.True, $"Boundary cycle {cycle}");
            }
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(100));
            Assert.That(closed, Is.EqualTo(100));
            Assert.That(fixture.Popover.IsOpen, Is.False);
        }));
    }

    [Test]
    public void TargetReplacementAssignedFromClosedSurvivesOriginalTargetDisposal()
    {
        using var form = new Form { ShowInTaskbar = false };
        var original = new Button { Location = new Point(20, 20), Size = new Size(100, 30) };
        using var replacement = new Button { Location = new Point(140, 20), Size = new Size(100, 30) };
        using var content = new Panel { Size = new Size(160, 60) };
        using var popover = new BootstrapPopover { Target = original, Content = content };
        form.Controls.Add(original);
        form.Controls.Add(replacement);
        popover.Closed += (_, _) => popover.Target = replacement;

        form.Show();
        popover.Show();
        Application.DoEvents();
        original.Dispose();
        Application.DoEvents();

        Assert.That(popover.Target, Is.SameAs(replacement));
    }

    [Test]
    public void ContentReplacementAssignedFromClosedSurvivesOriginalContentDisposal()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var target = new Button { Location = new Point(20, 20), Size = new Size(100, 30) };
        var original = new Panel { Size = new Size(160, 60) };
        using var replacement = new TextBox { Size = new Size(180, 24) };
        using var popover = new BootstrapPopover { Target = target, Content = original };
        form.Controls.Add(target);
        popover.Closed += (_, _) => popover.Content = replacement;

        form.Show();
        popover.Show();
        Application.DoEvents();
        original.Dispose();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(popover.Content, Is.SameAs(replacement));
            Assert.That(replacement.Parent, Is.Not.Null);
            Assert.That(replacement.IsDisposed, Is.False);
        }));
    }

    [Test]
    public void NoneAndFlipPreserveActualPopoverWindowGeometryAtWorkingAreaEdges()
    {
        var workingArea = Screen.PrimaryScreen!.WorkingArea;
        using var form = new Form
        {
            Bounds = new Rectangle(workingArea.Right - 100, workingArea.Top, 100, 100),
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual
        };
        using var target = new Button { Location = new Point(60, 0), Size = new Size(40, 30) };
        using var content = new Label
        {
            AutoSize = true,
            Text = "Popover content with measurable preferred size"
        };
        using var popover = new BootstrapPopover
        {
            Target = target,
            Content = content,
            Placement = BootstrapOverlayPlacement.Right,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.None,
            Offset = 0,
            BoundaryPadding = 0,
            ContentPadding = Padding.Empty
        };
        form.Controls.Add(target);

        try
        {
            form.Show();
            popover.Show();
            Application.DoEvents();
            var nativePopup = content.TopLevelControl;
            Assert.That(nativePopup, Is.TypeOf<BootstrapOverlayDropDown>());
            var size = nativePopup!.Size;
            var anchor = target.RectangleToScreen(target.ClientRectangle);
            Assert.That(GetActualBounds(nativePopup.Handle), Is.EqualTo(new Rectangle(
                anchor.Right,
                anchor.Top + ((anchor.Height - size.Height) / 2),
                size.Width,
                size.Height)));

            popover.Hide();
            popover.Placement = BootstrapOverlayPlacement.TopStart;
            popover.CollisionBehavior = BootstrapOverlayCollisionBehavior.Flip;
            popover.Show();
            Application.DoEvents();
            size = nativePopup.Size;
            anchor = target.RectangleToScreen(target.ClientRectangle);
            Assert.That(GetActualBounds(nativePopup.Handle), Is.EqualTo(new Rectangle(
                anchor.Left,
                anchor.Bottom,
                size.Width,
                size.Height)));
        }
        finally
        {
            popover.Hide();
        }
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
                SendTab(content, forward: true);
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

    private static Rectangle GetActualBounds(IntPtr handle)
    {
        Assert.That(GetWindowRect(handle, out var bounds), Is.True);
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    private static void SendTab(Control content, bool forward)
    {
        var dropDown = (BootstrapOverlayDropDown)content.TopLevelControl!;
        var processDialogKey = typeof(BootstrapOverlayDropDown).GetMethod(
            "ProcessDialogKey",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var handled = (bool)processDialogKey.Invoke(
            dropDown,
            new object[] { forward ? Keys.Tab : Keys.Shift | Keys.Tab })!;
        Application.DoEvents();
        Assert.That(handled, Is.True);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRectangle bounds);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
