using System;
using System.Collections.Generic;
using System.Drawing;
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
public sealed class IntegratedDemoTypographyLayoutTests
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

    [TestCase((int)DemoTypographyPreset.Default, 9f, 900, 600)]
    [TestCase((int)DemoTypographyPreset.Default, 9f, 1280, 800)]
    [TestCase((int)DemoTypographyPreset.Base14Px, 10.5f, 900, 600)]
    [TestCase((int)DemoTypographyPreset.Base14Px, 10.5f, 1280, 800)]
    [TestCase((int)DemoTypographyPreset.Base16Px, 12f, 900, 600)]
    [TestCase((int)DemoTypographyPreset.Base16Px, 12f, 1280, 800)]
    public void MainShellChromeRemainsContainedForTypographyProfile(
        int presetValue,
        float expectedSize,
        int width,
        int height)
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            (DemoTypographyPreset)presetValue);
        using var form = new MainForm();
        form.ClientSize = new Size(width, height);
        form.CreateControl();
        form.PerformLayout();

        var pageTitle = FindControls<Label>(form)
            .Single(label => label.AccessibleName == "Current demo page title");
        var pageDescription = FindControls<Label>(form)
            .Single(label => label.AccessibleName == "Current demo page description");
        var titleBlock = pageTitle.Parent!;
        var header = titleBlock.Parent!;
        var navigationToggle = FindControls<Button>(form)
            .Single(button => button.AccessibleName == "Toggle demo navigation");
        var themeMode = FindControls<ComboBox>(form)
            .Single(combo => combo.Items.Contains("Light") && combo.Items.Contains("Dark"));
        var settings = themeMode.Parent!;
        var themeLabel = settings.Controls.OfType<Label>().Single(label => label.Text == "Theme");
        var baseFontLabel = settings.Controls.OfType<Label>().Single(label => label.Text == "Base font");
        var baseFont = settings.Controls.OfType<ComboBox>()
            .Single(combo => combo.AccessibleName == "Integrated demo base font profile");
        var reducedMotion = settings.Controls.OfType<CheckBox>()
            .Single(checkBox => checkBox.Text == "Reduced motion");

        AssertContained(navigationToggle, header);
        AssertContained(titleBlock, header);
        AssertContained(settings, header);
        AssertContained(pageTitle, titleBlock);
        AssertContained(pageDescription, titleBlock);
        AssertContained(themeLabel, settings);
        AssertContained(themeMode, settings);
        AssertContained(baseFontLabel, settings);
        AssertContained(baseFont, settings);
        AssertContained(reducedMotion, settings);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(pageTitle.Bounds.Width, Is.GreaterThan(0));
            Assert.That(pageTitle.Bounds.Height, Is.GreaterThan(0));
            Assert.That(pageDescription.Bounds.Width, Is.GreaterThan(0));
            Assert.That(pageDescription.Bounds.Height, Is.GreaterThan(0));
            Assert.That(form.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.01f));
            Assert.That(pageTitle.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.01f));
            Assert.That(pageTitle.Font.Bold, Is.True);
        }));
    }

    [TestCase((int)DemoTypographyPreset.Default, 9f)]
    [TestCase((int)DemoTypographyPreset.Base14Px, 10.5f)]
    [TestCase((int)DemoTypographyPreset.Base16Px, 12f)]
    public void NativeLabelAndThemeFontButtonUseProfileBodySizingWithoutClipping(
        int presetValue,
        float expectedSize)
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            (DemoTypographyPreset)presetValue);
        using var form = new ButtonDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var label = FindControls<Label>(form).First();
        var button = FindControls<BootstrapButton>(form).First();
        var preferredSize = button.GetPreferredSize(Size.Empty);
        var measuredText = TextRenderer.MeasureText(button.Text, button.Font);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(label.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.01f));
            Assert.That(button.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.01f));
            Assert.That(button.AutoSize, Is.True);
            Assert.That(preferredSize.Height, Is.GreaterThan(0));
            Assert.That(preferredSize.Height, Is.GreaterThanOrEqualTo(measuredText.Height));
        }));
    }

    [TestCase((int)DemoTypographyPreset.Default, "Body Segoe UI 9pt")]
    [TestCase((int)DemoTypographyPreset.Base14Px, "Body Segoe UI 10.5pt")]
    [TestCase((int)DemoTypographyPreset.Base16Px, "Body Segoe UI 12pt")]
    public void ThemePageSummaryReportsProfileBodyTypographyAndHasUsableBounds(
        int presetValue,
        string expectedText)
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            (DemoTypographyPreset)presetValue);
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var summary = FindControls<Label>(form)
            .Single(label => label.Text.Contains(expectedText));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(summary.ClientSize.Width, Is.GreaterThan(0));
            Assert.That(summary.ClientSize.Height, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void SwitchingBaseFontUpdatesExistingThemePageInstance()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var baseFont = FindControls<ComboBox>(form)
            .Single(combo => combo.AccessibleName == "Integrated demo base font profile");
        var summary = FindControls<Label>(form)
            .Single(label => label.Text.Contains("Body Segoe UI 12pt"));
        var page = FindAncestorForm(summary);

        baseFont.SelectedIndex = 1;
        AssertSameLivePage(form, page, summary, "Body Segoe UI 10.5pt", 10.5f);

        baseFont.SelectedIndex = 0;
        AssertSameLivePage(form, page, summary, "Body Segoe UI 9pt", 9f);
    }

    private static Form FindAncestorForm(Control control)
    {
        var current = control.Parent;
        while (current is not Form)
        {
            current = current?.Parent;
        }

        return (Form)current;
    }

    private static void AssertSameLivePage(
        MainForm shell,
        Form page,
        Label summary,
        string expectedText,
        float expectedShellSize)
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(page.IsDisposed, Is.False);
            Assert.That(FindControls<Label>(shell).Contains(summary), Is.True);
            Assert.That(summary.Text, Does.Contain(expectedText));
            Assert.That(shell.Font.SizeInPoints, Is.EqualTo(expectedShellSize).Within(0.01f));
        }));
    }

    private static void AssertContained(Control child, Control parent)
    {
        var bounds = child.Bounds;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(0), child.Name);
            Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(0), child.Name);
            Assert.That(bounds.Right, Is.LessThanOrEqualTo(parent.ClientSize.Width), child.Name);
            Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(parent.ClientSize.Height), child.Name);
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
