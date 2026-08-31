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
    public void PickerLeftMouseDownClaimsFocusBeforeNativePopupOpens()
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
        using var picker = new BootstrapCalendarPicker
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
        Assert.That(previous.Focused, Is.True, "The regression setup requires another control to own focus first.");

        RaiseMouseDown(picker, MouseButtons.Left);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.Focused, Is.True,
                "Mouse interaction must make the picker the pre-popup focus owner so native ToolStrip dismissal restores focus here.");
            Assert.That(previous.Focused, Is.False,
                "The previously active control must release focus before the native calendar popup opens.");
        }));
    }

    private static void RaiseMouseDown(BootstrapCalendarPicker picker, MouseButtons button)
    {
        var method = typeof(BootstrapCalendarPicker).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(picker, new object[]
        {
            new MouseEventArgs(button, 1, picker.Width / 2, picker.Height / 2, 0)
        });
    }
}
