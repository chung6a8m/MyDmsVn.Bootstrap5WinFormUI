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
public sealed class BootstrapSwitchTests
{
    [Test]
    public void ContractUsesDirectCheckBoxInheritanceAndAddsExactlyTwoProperties()
    {
        using var control = new BootstrapSwitch();
        var declared = typeof(BootstrapSwitch).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapSwitch).BaseType, Is.EqualTo(typeof(CheckBox)));
            Assert.That(declared.Select(property => property.Name), Is.EquivalentTo(new[] { "Variant", "ValidationState" }));
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        }));
    }

    [Test]
    public void ActualCheckStateAndNativeEventsRemainAuthoritativeAcrossRapidChanges()
    {
        using var control = new BootstrapSwitch { ThreeState = false };
        var checkedChanges = 0;
        var stateChanges = 0;
        control.CheckedChanged += (_, _) => checkedChanges++;
        control.CheckStateChanged += (_, _) => stateChanges++;
        control.CheckState = CheckState.Indeterminate;
        control.CheckState = CheckState.Checked;
        control.CheckState = CheckState.Unchecked;
        control.CheckState = CheckState.Indeterminate;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.CheckState, Is.EqualTo(CheckState.Indeterminate));
            Assert.That(checkedChanges, Is.EqualTo(3));
            Assert.That(stateChanges, Is.EqualTo(4));
        }));
        Assert.DoesNotThrow((Action)(() => Draw(control)));
    }

    [Test]
    public void AutoCheckFalsePreservesCallerControlledState()
    {
        using var control = new TestSwitch { AutoCheck = false };
        control.Activate();
        Assert.That(control.Checked, Is.False);
        control.CheckState = CheckState.Indeterminate;
        Assert.That(control.CheckState, Is.EqualTo(CheckState.Indeterminate));
    }

    [Test]
    public void ValidationRtlAlignmentAndAllStatesPaintWithoutThrowing()
    {
        using var control = new BootstrapSwitch { Text = "Switch", Size = new Size(160, 34), CheckAlign = ContentAlignment.MiddleRight };
        foreach (var rtl in new[] { RightToLeft.No, RightToLeft.Yes })
        foreach (BootstrapValidationState validation in Enum.GetValues(typeof(BootstrapValidationState)))
        foreach (CheckState state in Enum.GetValues(typeof(CheckState)))
        {
            control.RightToLeft = rtl;
            control.ValidationState = validation;
            control.CheckState = state;
            Assert.DoesNotThrow((Action)(() => Draw(control)));
        }
    }

    [Test]
    public void NativeFallbackCanBeEnteredAndExitedWithoutCorruptingState()
    {
        using var font = new Font("Segoe UI", 9f);
        using var image = new Bitmap(12, 12);
        using var native = new CheckBox { Text = "Switch", Font = font, Appearance = Appearance.Button, AutoSize = true };
        using var control = new BootstrapSwitch { Text = "Switch", Font = font, Appearance = Appearance.Button, AutoSize = true, CheckState = CheckState.Indeterminate, Variant = BootstrapVariant.Warning, ValidationState = BootstrapValidationState.Invalid };
        Assert.That(control.GetPreferredSize(Size.Empty), Is.EqualTo(native.GetPreferredSize(Size.Empty)));
        control.Appearance = Appearance.Normal;
        control.Image = image;
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        control.Image = null;
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.CheckState, Is.EqualTo(CheckState.Indeterminate));
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Warning));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.Invalid));
        }));
    }

    [Test]
    public void InvalidEnumsThrowBeforeMutationAndDisposalDetachesTheme()
    {
        var baseline = GetThemeSubscriptionCount();
        var control = new BootstrapSwitch { Variant = BootstrapVariant.Info, ValidationState = BootstrapValidationState.Valid };
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 1));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.Variant = (BootstrapVariant)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.ValidationState = (BootstrapValidationState)99));
        control.Dispose();
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline));
    }

    private static void Draw(Control control)
    {
        using var bitmap = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
    }

    private static int GetThemeSubscriptionCount()
    {
        var field = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        var handler = field?.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class TestSwitch : BootstrapSwitch
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }
}
