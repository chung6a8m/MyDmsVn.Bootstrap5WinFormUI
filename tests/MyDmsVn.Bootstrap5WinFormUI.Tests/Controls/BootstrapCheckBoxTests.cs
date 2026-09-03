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
public sealed class BootstrapCheckBoxTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void ContractUsesDirectNativeInheritanceAndAddsExactlyTwoPublicProperties()
    {
        using var control = new BootstrapCheckBox();
        var declared = typeof(BootstrapCheckBox).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapCheckBox).BaseType, Is.EqualTo(typeof(CheckBox)));
            Assert.That(declared.Select(property => property.Name), Is.EquivalentTo(new[] { "Variant", "ValidationState" }));
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(control.AutoSize, Is.True);
        }));
    }

    [Test]
    public void InvalidAppearanceEnumsThrowBeforeMutation()
    {
        using var control = new BootstrapCheckBox { Variant = BootstrapVariant.Info, ValidationState = BootstrapValidationState.Valid };
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.Variant = (BootstrapVariant)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => control.ValidationState = (BootstrapValidationState)99));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Info));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
        }));
    }

    [Test]
    public void NativeStateStorageAndEventCountsRemainAuthoritative()
    {
        using var control = new BootstrapCheckBox { ThreeState = false };
        var checkedChanged = 0;
        var stateChanged = 0;
        control.CheckedChanged += (_, _) => checkedChanged++;
        control.CheckStateChanged += (_, _) => stateChanged++;

        control.CheckState = CheckState.Indeterminate;
        control.CheckState = CheckState.Checked;
        control.Variant = BootstrapVariant.Danger;
        control.ValidationState = BootstrapValidationState.Invalid;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.CheckState, Is.EqualTo(CheckState.Checked));
            Assert.That(checkedChanged, Is.EqualTo(1));
            Assert.That(stateChanged, Is.EqualTo(2));
        }));
    }

    [Test]
    public void AutoCheckFalsePreservesCallerControlledState()
    {
        using var control = new TestBootstrapCheckBox { AutoCheck = false };
        control.Activate();
        Assert.That(control.Checked, Is.False);
        control.Checked = true;
        Assert.That(control.Checked, Is.True);
    }

    [Test]
    public void AppearanceAndEffectiveImagesUseNativePreferredSizeFallbackWithoutStateLoss()
    {
        using var image = new Bitmap(12, 12);
        using var font = new Font("Segoe UI", 9f);
        using var native = new CheckBox { Text = "Fallback", Appearance = Appearance.Button, AutoSize = true, Font = font };
        using var control = new BootstrapCheckBox { Text = "Fallback", Appearance = Appearance.Button, AutoSize = true, Font = font, Checked = true, Variant = BootstrapVariant.Success, ValidationState = BootstrapValidationState.Valid };
        Assert.That(control.GetPreferredSize(Size.Empty), Is.EqualTo(native.GetPreferredSize(Size.Empty)));

        control.Appearance = Appearance.Normal;
        control.Image = image;
        Assert.DoesNotThrow((Action)(() => Draw(control)));
        control.Image = null;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.Checked, Is.True);
            Assert.That(control.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.Valid));
        }));
    }

    [Test]
    public void FlatStyleSystemUsesNativePreferredSizeFallback()
    {
        using var font = new Font("Segoe UI", 9f);
        using var native = new CheckBox { Text = "System fallback", Font = font, FlatStyle = FlatStyle.System, AutoSize = true };
        using var control = new BootstrapCheckBox { Text = "System fallback", Font = font, FlatStyle = FlatStyle.System, AutoSize = true, Checked = true };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(control.GetPreferredSize(Size.Empty), Is.EqualTo(native.GetPreferredSize(Size.Empty)));
            Assert.DoesNotThrow((Action)(() => Draw(control)));
            Assert.That(control.Checked, Is.True);
        }));
    }

    [Test]
    public void TopCenterAutoSizeStacksIndicatorGapAndText()
    {
        using var control = new BootstrapCheckBox { Text = "Stacked label", CheckAlign = ContentAlignment.TopCenter, AutoSize = true };
        var dpi = control.DeviceDpi > 0 ? control.DeviceDpi : 96;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var flags = BootstrapCheckableRenderLogic.GetTextFormatFlags(control.TextAlign, control.UseMnemonic, false, control.AutoEllipsis, false);
        var textSize = TextRenderer.MeasureText(control.Text, control.Font, Size.Empty, flags);
        var expectedHeight = control.Padding.Vertical + metrics.IndicatorBoundsSize.Height + metrics.TextGap + textSize.Height + metrics.FocusWidth;

        Assert.That(control.GetPreferredSize(Size.Empty).Height, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void AutoSizeTextBoundsAreWideEnoughForPaintMetrics()
    {
        using var control = new BootstrapCheckBox { Text = "Wide italic-like WWW label", AutoSize = true };
        var dpi = control.DeviceDpi > 0 ? control.DeviceDpi : 96;
        var metrics = BootstrapCheckableRenderLogic.GetMetrics(BootstrapCheckableKind.CheckBox, BootstrapThemeManager.CurrentTheme.Metrics, dpi);
        var flags = BootstrapCheckableRenderLogic.GetTextFormatFlags(control.TextAlign, control.UseMnemonic, false, control.AutoEllipsis, false);
        var paintTextSize = TextRenderer.MeasureText(control.Text, control.Font, Size.Empty, flags);
        var layout = BootstrapCheckableRenderLogic.GetLayout(control.ClientRectangle, control.Padding, metrics, control.CheckAlign, false);

        Assert.That(layout.TextBounds.Width, Is.GreaterThanOrEqualTo(paintTextSize.Width));
    }

    [Test]
    public void NormalPaintingCoversAllStatesThemesAndTinyBoundsWithoutThrowing()
    {
        using var control = new BootstrapCheckBox { Text = "State", ThreeState = false, Size = new Size(140, 32) };
        foreach (BootstrapThemeMode mode in Enum.GetValues(typeof(BootstrapThemeMode)))
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            foreach (CheckState state in Enum.GetValues(typeof(CheckState)))
            {
                control.CheckState = state;
                Assert.DoesNotThrow((Action)(() => Draw(control)));
            }
        }

        control.Size = new Size(1, 1);
        Assert.DoesNotThrow((Action)(() => Draw(control)));
    }

    [Test]
    public void ThemeSubscriptionAndFontOwnershipAreDeterministic()
    {
        var baseline = GetThemeSubscriptionCount();
        var owned = new BootstrapCheckBox();
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline + 1));
        owned.Dispose();
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baseline));

        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var callerOwned = new BootstrapCheckBox { Font = callerFont };
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(callerOwned.Font, Is.SameAs(callerFont));
        callerOwned.Dispose();
        using var bitmap = new Bitmap(10, 10);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
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

    private sealed class TestBootstrapCheckBox : BootstrapCheckBox
    {
        public void Activate() => OnClick(EventArgs.Empty);
    }
}
