using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCheckableInteractionTests
{
    [Test]
    public void NativeCheckBoxAllowsProgrammaticIndeterminateRegardlessOfThreeStateAndRaisesNativeEvents()
    {
        using var checkBox = new CheckBox { ThreeState = false };
        var checkedChanged = 0;
        var stateChanged = 0;
        checkBox.CheckedChanged += (_, _) => checkedChanged++;
        checkBox.CheckStateChanged += (_, _) => stateChanged++;

        checkBox.CheckState = CheckState.Indeterminate;
        checkBox.CheckState = CheckState.Checked;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checkBox.CheckState, Is.EqualTo(CheckState.Checked));
            Assert.That(checkedChanged, Is.EqualTo(1));
            Assert.That(stateChanged, Is.EqualTo(2));
        }));
    }

    [Test]
    public void NativeRadioAutoCheckFalseAllowsCallerManagedMultipleCheckedState()
    {
        using var parent = new Panel();
        using var first = new RadioButton { AutoCheck = false };
        using var second = new RadioButton { AutoCheck = false };
        parent.Controls.Add(first);
        parent.Controls.Add(second);

        first.Checked = true;
        second.Checked = true;
        first.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Checked, Is.True);
            Assert.That(second.Checked, Is.True);
        }));
    }

    [Test]
    public void BootstrapCheckBoxAndSwitchActivationUseOneNativeStateEventPath()
    {
        using var checkBox = new TestCheckBox();
        using var toggle = new TestSwitch();
        var checkEvents = 0;
        var switchEvents = 0;
        checkBox.CheckedChanged += (_, _) => checkEvents++;
        toggle.CheckedChanged += (_, _) => switchEvents++;

        checkBox.Activate();
        toggle.Activate();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checkBox.Checked, Is.True);
            Assert.That(toggle.Checked, Is.True);
            Assert.That(checkEvents, Is.EqualTo(1));
            Assert.That(switchEvents, Is.EqualTo(1));
        }));
    }

    [Test]
    public void BootstrapRadioGroupingMatchesNativeAndHasNoStaticRegistry()
    {
        using var parent = new Panel();
        using var first = new TestRadio();
        using var second = new TestRadio();
        parent.Controls.Add(first);
        parent.Controls.Add(second);
        first.Activate();
        second.Activate();

        var staticFields = typeof(BootstrapRadioButton).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Checked, Is.False);
            Assert.That(second.Checked, Is.True);
            Assert.That(staticFields, Is.Empty);
        }));
    }

    [Test]
    public void AccessibilityMetadataRemainsCallerOwnedAndNoControlAddsHiddenChildren()
    {
        using var checkBox = new BootstrapCheckBox { AccessibleName = "Accept", AccessibleDescription = "Accept terms" };
        using var radio = new BootstrapRadioButton { AccessibleName = "Plan" };
        using var toggle = new BootstrapSwitch { AccessibleName = "Notifications" };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checkBox.AccessibleName, Is.EqualTo("Accept"));
            Assert.That(checkBox.AccessibleDescription, Is.EqualTo("Accept terms"));
            Assert.That(radio.AccessibleName, Is.EqualTo("Plan"));
            Assert.That(toggle.AccessibleName, Is.EqualTo("Notifications"));
            Assert.That(checkBox.Controls, Is.Empty);
            Assert.That(radio.Controls, Is.Empty);
            Assert.That(toggle.Controls, Is.Empty);
        }));
    }

    [Test]
    public void RepeatedFallbackTransitionsPreserveStateAndDoNotEmitStateEvents()
    {
        using var image = new Bitmap(8, 8);
        using var checkBox = new BootstrapCheckBox { Checked = true, Variant = BootstrapVariant.Success, ValidationState = BootstrapValidationState.Valid };
        using var radio = new BootstrapRadioButton { Checked = true, Variant = BootstrapVariant.Warning, ValidationState = BootstrapValidationState.Invalid };
        using var toggle = new BootstrapSwitch { CheckState = CheckState.Indeterminate, Variant = BootstrapVariant.Info, ValidationState = BootstrapValidationState.Valid };
        var events = 0;
        checkBox.CheckedChanged += (_, _) => events++;
        radio.CheckedChanged += (_, _) => events++;
        toggle.CheckStateChanged += (_, _) => events++;

        for (var index = 0; index < 25; index++)
        {
            checkBox.Appearance = Appearance.Button;
            radio.Appearance = Appearance.Button;
            toggle.Appearance = Appearance.Button;
            checkBox.Appearance = Appearance.Normal;
            radio.Appearance = Appearance.Normal;
            toggle.Appearance = Appearance.Normal;
            checkBox.Image = image;
            radio.Image = image;
            toggle.Image = image;
            checkBox.Image = null;
            radio.Image = null;
            toggle.Image = null;
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(events, Is.Zero);
            Assert.That(checkBox.Checked, Is.True);
            Assert.That(radio.Checked, Is.True);
            Assert.That(toggle.CheckState, Is.EqualTo(CheckState.Indeterminate));
            Assert.That(checkBox.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(radio.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
        }));
    }

    [TestCase(typeof(BootstrapCheckBox))]
    [TestCase(typeof(BootstrapRadioButton))]
    [TestCase(typeof(BootstrapSwitch))]
    public void OwnerDrawnTextHonorsTopAndBottomTextAlign(Type controlType)
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            var top = RenderTextInkRows(controlType, ContentAlignment.TopLeft);
            var bottom = RenderTextInkRows(controlType, ContentAlignment.BottomLeft);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(top.First(), Is.LessThan(10), $"{controlType.Name} must render top-aligned text at the top edge.");
                Assert.That(bottom.Last(), Is.GreaterThan(50), $"{controlType.Name} must render bottom-aligned text at the bottom edge.");
                Assert.That(top.Last(), Is.LessThan(bottom.First()), $"{controlType.Name} top and bottom text positions must be distinct.");
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static int[] RenderTextInkRows(Type controlType, ContentAlignment textAlign)
    {
        using var host = new Panel { Size = new Size(220, 64), BackColor = Color.White };
        using var control = (ButtonBase)Activator.CreateInstance(controlType)!;
        control.AutoSize = false;
        control.Bounds = new Rectangle(0, 0, 220, 64);
        control.Text = "Alignment";
        control.TextAlign = textAlign;
        if (control is RadioButton radio)
        {
            radio.CheckAlign = ContentAlignment.MiddleLeft;
        }
        else
        {
            ((CheckBox)control).CheckAlign = ContentAlignment.MiddleLeft;
        }

        host.Controls.Add(control);
        host.CreateControl();
        control.CreateControl();
        using var bitmap = new Bitmap(host.Width, host.Height);
        host.DrawToBitmap(bitmap, host.ClientRectangle);

        return Enumerable.Range(0, bitmap.Height)
            .Where(y => Enumerable.Range(48, bitmap.Width - 48)
                .Any(x => bitmap.GetPixel(x, y).GetBrightness() < 0.65f))
            .ToArray();
    }

    private sealed class TestCheckBox : BootstrapCheckBox
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }

    private sealed class TestSwitch : BootstrapSwitch
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }

    private sealed class TestRadio : BootstrapRadioButton
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }
}
