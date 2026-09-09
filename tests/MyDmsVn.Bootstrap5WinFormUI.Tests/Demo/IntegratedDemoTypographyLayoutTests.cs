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
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
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
    public void MainShellChromeRemainsContainedAtTwelvePointBodyTypography()
    {
        using var form = new MainForm();
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
        var reducedMotion = settings.Controls.OfType<CheckBox>()
            .Single(checkBox => checkBox.Text == "Reduced motion");

        AssertContained(navigationToggle, header);
        AssertContained(titleBlock, header);
        AssertContained(settings, header);
        AssertContained(pageTitle, titleBlock);
        AssertContained(pageDescription, titleBlock);
        AssertContained(themeLabel, settings);
        AssertContained(themeMode, settings);
        AssertContained(reducedMotion, settings);
    }

    [Test]
    public void NativeLabelAndThemeFontButtonUseTwelvePointBodySizingWithoutClipping()
    {
        using var form = new ButtonDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var label = FindControls<Label>(form).First();
        var button = FindControls<BootstrapButton>(form).First();
        var preferredSize = button.GetPreferredSize(Size.Empty);
        var measuredText = TextRenderer.MeasureText(button.Text, button.Font);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(label.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
            Assert.That(button.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
            Assert.That(button.AutoSize, Is.True);
            Assert.That(preferredSize.Height, Is.GreaterThan(0));
            Assert.That(preferredSize.Height, Is.GreaterThanOrEqualTo(measuredText.Height));
        }));
    }

    [Test]
    public void ThemePageSummaryReportsDemoBodyTypographyAndHasUsableBounds()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var summary = FindControls<Label>(form)
            .Single(label => label.Text.Contains("Body Segoe UI 12"));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(summary.ClientSize.Width, Is.GreaterThan(0));
            Assert.That(summary.ClientSize.Height, Is.GreaterThan(0));
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
