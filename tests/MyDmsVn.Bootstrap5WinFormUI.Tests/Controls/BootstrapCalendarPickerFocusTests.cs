using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCalendarPickerFocusTests
{
    [Test]
    public void PickerLeftMouseClickClaimsFocusBeforeNativePopupOpens()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-10000, -10000),
            Size = new Size(480, 300)
        };
        using var previous = new TextBox
        {
            Location = new Point(24, 24),
            Size = new Size(240, 36)
        };

        Control? activeControlAtShow = null;
        using var picker = new BootstrapCalendarPicker(
            effectiveDpiProvider: () => 96,
            hostedCalendarSetupCompleted: null,
            showNativeDropDown: (_, _, _) => activeControlAtShow = form.ActiveControl)
        {
            Location = new Point(24, 80),
            Size = new Size(240, 36)
        };
        form.Controls.Add(previous);
        form.Controls.Add(picker);
        form.Show();
        form.Activate();
        Application.DoEvents();

        previous.Focus();
        Application.DoEvents();
        Assert.That(form.ActiveControl, Is.SameAs(previous),
            "The regression setup requires another control to own focus first.");

        RaiseMouseClick(picker, MouseButtons.Left);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(activeControlAtShow, Is.SameAs(picker),
                "The picker must own focus before ToolStripDropDown.Show so native dismissal restores focus to the picker.");
            Assert.That(form.ActiveControl, Is.SameAs(picker),
                "Mouse activation must not leave the previously active control as the form focus owner.");
        }));
    }

    [Test]
    public void FocusTransitionsInvalidatePickerShellForKeyboardFocusVisuals()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-10000, -10000),
            Size = new Size(480, 300)
        };
        using var picker = new BootstrapCalendarPicker
        {
            Location = new Point(24, 24),
            Size = new Size(240, 36)
        };
        form.Controls.Add(picker);
        form.Show();
        form.Activate();
        Application.DoEvents();

        var invalidationCount = 0;
        picker.Invalidated += (_, _) => invalidationCount++;

        RaiseFocusTransition(picker, "OnGotFocus");
        Assert.That(invalidationCount, Is.GreaterThan(0),
            "Receiving keyboard focus must repaint the owner-drawn shell so the active focus border becomes visible.");

        invalidationCount = 0;
        RaiseFocusTransition(picker, "OnLostFocus");
        Assert.That(invalidationCount, Is.GreaterThan(0),
            "Losing keyboard focus must repaint the owner-drawn shell so a stale active border is cleared.");
    }

    private static void RaiseMouseClick(BootstrapCalendarPicker picker, MouseButtons button)
    {
        var method = typeof(BootstrapCalendarPicker).GetMethod("OnMouseClick", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(picker, new object[]
        {
            new MouseEventArgs(button, 1, picker.Width / 2, picker.Height / 2, 0)
        });
    }

    private static void RaiseFocusTransition(BootstrapCalendarPicker picker, string methodName)
    {
        var method = typeof(BootstrapCalendarPicker).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(picker, new object[] { EventArgs.Empty });
        Application.DoEvents();
    }
}
