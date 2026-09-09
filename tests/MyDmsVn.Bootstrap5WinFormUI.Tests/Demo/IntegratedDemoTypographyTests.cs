using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class IntegratedDemoTypographyTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
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
    public void MainFormUsesBrowserEquivalentTwelvePointNativeBodyTypography()
    {
        using var form = new MainForm();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(form.AutoScaleMode, Is.EqualTo(AutoScaleMode.Dpi));
            Assert.That(form.Font.Name, Is.EqualTo("Segoe UI"));
            Assert.That(form.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
        }));

        var settings = FindControls<FlowLayoutPanel>(form).Single(panel =>
            panel.Controls.OfType<ComboBox>()
                .Any(combo => combo.Items.Contains("Light") && combo.Items.Contains("Dark")));
        var themeLabel = settings.Controls.OfType<Label>().Single(label => label.Text == "Theme");
        Assert.That(themeLabel.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    }

    [Test]
    public void ConstructingDemoFormDoesNotReplaceApplicationTheme()
    {
        var installed = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);
        BootstrapThemeManager.CurrentTheme = installed;

        using var form = new ButtonDemoForm();

        Assert.That(BootstrapThemeManager.CurrentTheme, Is.SameAs(installed));
    }

    [Test]
    public void ThemeAndReducedMotionChangesPreserveDemoTypography()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var form = new MainForm();

        var themeMode = FindControls<ComboBox>(form)
            .Single(combo => combo.Items.Contains("Light") && combo.Items.Contains("Dark"));
        var reducedMotion = FindControls<CheckBox>(form)
            .Single(checkBox => checkBox.Text == "Reduced motion");

        themeMode.SelectedIndex = 1;
        reducedMotion.Checked = true;

        var theme = BootstrapThemeManager.CurrentTheme;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
            Assert.That(theme.ReducedMotion, Is.True);
            Assert.That(theme.Typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
            Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
            Assert.That(theme.Typography.BodySmall.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
            Assert.That(theme.Typography.Label.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
            Assert.That(theme.Typography.HeadingSmall.SizeInPoints, Is.EqualTo(15f).Within(0.01f));
            Assert.That(theme.Typography.HeadingMedium.SizeInPoints, Is.EqualTo(18f).Within(0.01f));
        }));

        using var themedButton = new BootstrapButton();
        Assert.That(themedButton.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    }

    [Test]
    public void FrameworkDefaultTypographyRemainsCompactAndIsNotRewrittenForTheDemo()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapThemeTypography.Default.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
            Assert.That(BootstrapThemeTypography.Default.Body.SizeInPoints, Is.EqualTo(9f).Within(0.01f));
        }));
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
