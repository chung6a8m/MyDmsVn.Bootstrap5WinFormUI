using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ChecksDemoFormTests
{
    [Test]
    public void DemoConstructsAllCheckableFamiliesAndRequiredStates()
    {
        using var form = new ChecksDemoForm();
        form.CreateControl();
        var checks = Find<BootstrapCheckBox>(form).ToArray();
        var radios = Find<BootstrapRadioButton>(form).ToArray();
        var switches = Find<BootstrapSwitch>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checks.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(radios.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(switches.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(checks.Any(control => control.CheckState == CheckState.Indeterminate && !control.ThreeState), Is.True);
            Assert.That(radios.Count(control => !control.AutoCheck && control.Checked), Is.GreaterThanOrEqualTo(2));
            Assert.That(switches.Any(control => control.CheckState == CheckState.Indeterminate && !control.ThreeState), Is.True);
            Assert.That(checks.Any(control => control.Appearance == Appearance.Button), Is.True);
        }));
    }

    [Test]
    public void DemoIncludesValidationRtlAndEventFeedback()
    {
        using var form = new ChecksDemoForm();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(Find<BootstrapCheckBox>(form).Any(control => control.ValidationState == BootstrapValidationState.Valid && !control.Checked), Is.True);
            Assert.That(Find<BootstrapSwitch>(form).Any(control => control.ValidationState == BootstrapValidationState.Invalid && !control.Checked), Is.True);
            Assert.That(Find<Control>(form).Any(control => control.RightToLeft == RightToLeft.Yes), Is.True);
            Assert.That(Find<Label>(form).Any(label => label.AccessibleName == "Checkable event counters"), Is.True);
        }));
    }

    [Test]
    public void DemoReleasesItsOwnedSectionFontOnDispose()
    {
        var form = new ChecksDemoForm();
        var sectionFont = Find<Label>(form).First(label => label.Text == "CheckBox states").Font;

        form.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", sectionFont)));
    }

    private static IEnumerable<T> Find<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in Find<T>(child)) yield return nested;
        }
    }
}
