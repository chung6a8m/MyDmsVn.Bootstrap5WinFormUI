using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapNumericBoxInteractionTests
{
    [Test]
    public void EnteringWrapperRedirectsFocusToOwnedNativeEditor()
    {
        using var form = CreateHost(out var input, out var native);

        input.Focus();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.TabStop, Is.True);
            Assert.That(native.TabStop, Is.False);
            Assert.That(native.Focused, Is.True);
            Assert.That(input.ContainsFocus, Is.True);
        }));
    }

    [Test]
    public void ShellMouseDownRedirectsFocusToOwnedNativeEditor()
    {
        using var form = CreateHost(out var input, out var native);
        form.ActiveControl = null;

        RaiseDeclaredProtectedEvent(
            input,
            nameof(BootstrapNumericBox),
            "OnMouseDown",
            new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0));
        Application.DoEvents();

        Assert.That(native.Focused, Is.True);
    }

    [Test]
    public void NativeEditorKeyboardEventsAreForwardedThroughWrapperExactlyOnce()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();
        var keyDownCount = 0;
        var keyPressCount = 0;
        var keyUpCount = 0;
        var previewKeyDownCount = 0;

        input.KeyDown += (_, e) =>
        {
            keyDownCount++;
            e.Handled = true;
            e.SuppressKeyPress = true;
        };
        input.KeyPress += (_, e) =>
        {
            keyPressCount++;
            e.Handled = true;
        };
        input.KeyUp += (_, _) => keyUpCount++;
        input.PreviewKeyDown += (_, e) =>
        {
            previewKeyDownCount++;
            e.IsInputKey = true;
        };

        var keyDown = new KeyEventArgs(Keys.Enter);
        var keyPress = new KeyPressEventArgs('5');
        var keyUp = new KeyEventArgs(Keys.Enter);
        var previewKeyDown = new PreviewKeyDownEventArgs(Keys.Tab);

        RaiseProtectedControlEvent(native, "OnKeyDown", keyDown);
        RaiseProtectedControlEvent(native, "OnKeyPress", keyPress);
        RaiseProtectedControlEvent(native, "OnKeyUp", keyUp);
        RaiseProtectedControlEvent(native, "OnPreviewKeyDown", previewKeyDown);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(keyDownCount, Is.EqualTo(1));
            Assert.That(keyPressCount, Is.EqualTo(1));
            Assert.That(keyUpCount, Is.EqualTo(1));
            Assert.That(previewKeyDownCount, Is.EqualTo(1));
            Assert.That(keyDown.Handled, Is.True);
            Assert.That(keyDown.SuppressKeyPress, Is.True);
            Assert.That(keyPress.Handled, Is.True);
            Assert.That(previewKeyDown.IsInputKey, Is.True);
        }));
    }

    [Test]
    public void ReadOnlyPreservesNativeSpinSemantics()
    {
        using var input = new BootstrapNumericBox
        {
            Minimum = 0m,
            Maximum = 10m,
            Value = 4m,
            Increment = 2m,
            ReadOnly = true
        };
        var native = input.Controls.OfType<NumericUpDown>().Single();
        var changed = 0;
        input.ValueChanged += (_, _) => changed++;

        native.UpButton();
        native.DownButton();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.ReadOnly, Is.True);
            Assert.That(input.Value, Is.EqualTo(4m));
            Assert.That(changed, Is.EqualTo(2));
        }));
    }

    [Test]
    public void NativeSpinStopsAtBoundsWithoutDuplicateValueChanged()
    {
        using var input = new BootstrapNumericBox
        {
            Minimum = 0m,
            Maximum = 2m,
            Increment = 1m,
            Value = 2m
        };
        var native = input.Controls.OfType<NumericUpDown>().Single();
        var changed = 0;
        input.ValueChanged += (_, _) => changed++;

        native.UpButton();
        Assert.That(input.Value, Is.EqualTo(2m));
        Assert.That(changed, Is.EqualTo(0));

        native.DownButton();
        Assert.That(input.Value, Is.EqualTo(1m));
        Assert.That(changed, Is.EqualTo(1));
    }

    [Test]
    public void MouseWheelRemainsOwnedByNativeNumericEditor()
    {
        using var form = CreateHost(out var input, out var native);
        input.Minimum = 0m;
        input.Maximum = 10m;
        input.Value = 5m;
        input.Increment = 1m;
        native.Focus();
        Application.DoEvents();

        RaiseProtectedControlEvent(
            native,
            "OnMouseWheel",
            new MouseEventArgs(MouseButtons.None, 0, 0, 0, 120));

        Assert.That(input.Value, Is.Not.EqualTo(5m));
    }

    private static Form CreateHost(out BootstrapNumericBox input, out NumericUpDown native)
    {
        var form = new Form { ShowInTaskbar = false, Width = 320, Height = 180 };
        input = new BootstrapNumericBox { Left = 20, Top = 20, Width = 180 };
        native = input.Controls.OfType<NumericUpDown>().Single();
        form.Controls.Add(input);
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static void RaiseProtectedControlEvent(Control control, string methodName, EventArgs args)
    {
        var method = typeof(Control).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Expected Control.{methodName} to exist.");
        method!.Invoke(control, new object[] { args });
    }

    private static void RaiseDeclaredProtectedEvent(Control control, string typeName, string methodName, EventArgs args)
    {
        var method = control.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, $"Expected {typeName}.{methodName} to be overridden.");
        method!.Invoke(control, new object[] { args });
    }
}
