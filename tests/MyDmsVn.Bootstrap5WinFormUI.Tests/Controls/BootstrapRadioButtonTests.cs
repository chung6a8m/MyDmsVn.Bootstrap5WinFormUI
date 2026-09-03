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
public sealed class BootstrapRadioButtonTests
{
    [Test]
    public void ContractUsesDirectNativeInheritanceAndAddsExactlyTwoProperties()
    {
        using var control = new BootstrapRadioButton();
        var declared = typeof(BootstrapRadioButton).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapRadioButton).BaseType, Is.EqualTo(typeof(RadioButton)));
            Assert.That(declared.Select(property => property.Name), Is.EquivalentTo(new[] { "Variant", "ValidationState" }));
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        }));
    }

    [Test]
    public void NativeSameParentGroupingAndSeparateParentIsolationArePreserved()
    {
        using var firstParent = new Panel();
        using var secondParent = new Panel();
        using var first = new TestRadio();
        using var second = new TestRadio();
        using var isolated = new TestRadio();
        firstParent.Controls.Add(first);
        firstParent.Controls.Add(second);
        secondParent.Controls.Add(isolated);

        first.Activate();
        isolated.Activate();
        second.Activate();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Checked, Is.False);
            Assert.That(second.Checked, Is.True);
            Assert.That(isolated.Checked, Is.True);
        }));
    }

    [Test]
    public void AutoCheckFalseAllowsCallerManagedMultipleCheckedStateAndActivationDoesNotChangeIt()
    {
        using var parent = new Panel();
        using var first = new TestRadio { AutoCheck = false, Checked = true };
        using var second = new TestRadio { AutoCheck = false, Checked = true };
        parent.Controls.Add(first);
        parent.Controls.Add(second);
        var changes = 0;
        first.CheckedChanged += (_, _) => changes++;

        first.Activate();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Checked, Is.True);
            Assert.That(second.Checked, Is.True);
            Assert.That(changes, Is.Zero);
        }));
    }

    [Test]
    public void ReparentingUsesNativeGroupingWithoutFrameworkBookkeeping()
    {
        using var firstParent = new Panel();
        using var secondParent = new Panel();
        using var first = new TestRadio { Checked = true };
        using var second = new TestRadio();
        firstParent.Controls.Add(first);
        firstParent.Controls.Add(second);
        secondParent.Controls.Add(second);
        second.Activate();
        Assert.That(first.Checked, Is.True);
        Assert.That(second.Checked, Is.True);
    }

    [Test]
    public void InvalidEnumsThrowBeforeMutationAndPresentationDoesNotRaiseStateEvents()
    {
        using var control = new BootstrapRadioButton { Variant = BootstrapVariant.Info, ValidationState = BootstrapValidationState.Valid };
        var changes = 0;
        control.CheckedChanged += (_, _) => changes++;
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.Variant = (BootstrapVariant)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.ValidationState = (BootstrapValidationState)99));
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void NativeFallbackAndNormalPaintingAreSafeWithoutStateLoss()
    {
        using var font = new Font("Segoe UI", 9f);
        using var image = new Bitmap(12, 12);
        using var native = new RadioButton { Text = "Radio", Font = font, Appearance = Appearance.Button, AutoSize = true };
        using var control = new BootstrapRadioButton { Text = "Radio", Font = font, Appearance = Appearance.Button, AutoSize = true, Checked = true };
        Assert.That(control.GetPreferredSize(Size.Empty), Is.EqualTo(native.GetPreferredSize(Size.Empty)));
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        control.Appearance = Appearance.Normal;
        control.Image = image;
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        control.Image = null;
        control.RightToLeft = RightToLeft.Yes;
        control.CheckAlign = ContentAlignment.MiddleRight;
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        Assert.That(control.Checked, Is.True);
    }

    [Test]
    public void DisposalDetachesThemeSubscription()
    {
        var baseline = GetThemeSubscriptionCount();
        var control = new BootstrapRadioButton();
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 1));
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

    private sealed class TestRadio : BootstrapRadioButton
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }
}
