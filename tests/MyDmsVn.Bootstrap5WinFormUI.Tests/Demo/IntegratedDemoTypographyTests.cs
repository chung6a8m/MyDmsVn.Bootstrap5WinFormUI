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
    public void DefaultPresetReusesExactFrameworkDefaultTypography()
    {
        var theme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Default);

        Assert.That(theme.Typography, Is.SameAs(BootstrapThemeTypography.Default));
    }

    [TestCase((int)DemoTypographyPreset.Base14Px, 10.5f, 9.1875f, 10.5f, 13.125f, 15.75f)]
    [TestCase((int)DemoTypographyPreset.Base16Px, 12f, 10.5f, 12f, 15f, 18f)]
    public void DemoThemeFactoryCreatesRequestedTypographyProfile(
        int presetValue,
        float body,
        float bodySmall,
        float label,
        float headingSmall,
        float headingMedium)
    {
        var theme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            (DemoTypographyPreset)presetValue);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(theme.Typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
            Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(body).Within(0.001f));
            Assert.That(theme.Typography.BodySmall.SizeInPoints, Is.EqualTo(bodySmall).Within(0.001f));
            Assert.That(theme.Typography.Label.SizeInPoints, Is.EqualTo(label).Within(0.001f));
            Assert.That(theme.Typography.Label.Style, Is.EqualTo(FontStyle.Bold));
            Assert.That(theme.Typography.HeadingSmall.SizeInPoints, Is.EqualTo(headingSmall).Within(0.001f));
            Assert.That(theme.Typography.HeadingMedium.SizeInPoints, Is.EqualTo(headingMedium).Within(0.001f));
        }));
    }

    [Test]
    public void ExistingPublicFactoryOverloadStillMeansBase16Px()
    {
        var theme = DemoThemeFactory.Create(BootstrapThemeMode.Dark, reducedMotion: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
            Assert.That(theme.ReducedMotion, Is.True);
            Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.001f));
        }));
    }

    [Test]
    public void FactoryCanPreserveArbitraryTypographyByReference()
    {
        var custom = CreateCustomTypography();

        var theme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            custom,
            reducedMotion: true);

        Assert.That(theme.Typography, Is.SameAs(custom));
    }

    [Test]
    public void FactoryRejectsUnknownDemoTypographyPreset()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
        {
            _ = DemoThemeFactory.Create(
                BootstrapThemeMode.Light,
                (DemoTypographyPreset)999);
        }));
    }

    [Test]
    public void DemoFormBaseTracksBodyTypographyAcrossRuntimeProfileChanges()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new ButtonDemoForm();
        var nativeLabel = FindControls<Label>(form).First();
        var bootstrapButton = FindControls<BootstrapButton>(form).First();

        AssertBodyTypography(form, nativeLabel, bootstrapButton, 12f);

        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base14Px);
        AssertBodyTypography(form, nativeLabel, bootstrapButton, 10.5f);

        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Default);
        AssertBodyTypography(form, nativeLabel, bootstrapButton, 9f);
    }

    [Test]
    public void DemoFormBaseDoesNotReplaceOwnedFontWhenTypographyTokenIsUnchanged()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base14Px);

        using var form = new ButtonDemoForm();
        var originalFont = form.Font;

        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypographyPreset.Base14Px,
            reducedMotion: true);

        Assert.That(form.Font, Is.SameAs(originalFont));
    }

    [Test]
    public void MainFormUsesBrowserEquivalentTwelvePointNativeBodyTypography()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

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
    public void MainFormExposesBaseFontSelectorBesideThemeSelector()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new MainForm();
        var settings = FindThemeModeCombo(form).Parent!;
        var controls = settings.Controls.Cast<Control>().ToArray();
        var baseFont = FindBaseFontCombo(form);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(baseFont.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
            Assert.That(baseFont.Items.Cast<string>(), Is.EqualTo(new[]
            {
                "Default",
                "Base 14px",
                "Base 16px"
            }));
            Assert.That(baseFont.SelectedIndex, Is.EqualTo(2));
            Assert.That(controls[0].Text, Is.EqualTo("Theme"));
            Assert.That(controls[1], Is.SameAs(FindThemeModeCombo(form)));
            Assert.That(controls[2].Text, Is.EqualTo("Base font"));
            Assert.That(controls[3], Is.SameAs(baseFont));
            Assert.That(controls[4], Is.SameAs(FindReducedMotionCheckBox(form)));
        }));
    }

    [Test]
    public void MainFormComposesThemeTypographyAndReducedMotionIndependently()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new MainForm();
        var themeMode = FindThemeModeCombo(form);
        var baseFont = FindBaseFontCombo(form);
        var reducedMotion = FindReducedMotionCheckBox(form);

        baseFont.SelectedIndex = 1;
        AssertTheme(BootstrapThemeMode.Light, 10.5f, false);

        themeMode.SelectedIndex = 1;
        AssertTheme(BootstrapThemeMode.Dark, 10.5f, false);
        Assert.That(baseFont.SelectedIndex, Is.EqualTo(1));

        reducedMotion.Checked = true;
        AssertTheme(BootstrapThemeMode.Dark, 10.5f, true);

        baseFont.SelectedIndex = 0;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography,
            Is.SameAs(BootstrapThemeTypography.Default));
        AssertTheme(BootstrapThemeMode.Dark, 9f, true);

        baseFont.SelectedIndex = 2;
        AssertTheme(BootstrapThemeMode.Dark, 12f, true);
    }

    [Test]
    public void MainFormPreservesUnknownTypographyAcrossUnrelatedSettingChanges()
    {
        var custom = CreateCustomTypography();
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            custom);

        using var form = new MainForm();
        var themeMode = FindThemeModeCombo(form);
        var baseFont = FindBaseFontCombo(form);
        var reducedMotion = FindReducedMotionCheckBox(form);

        Assert.That(baseFont.SelectedIndex, Is.EqualTo(-1));

        themeMode.SelectedIndex = 1;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));
        Assert.That(baseFont.SelectedIndex, Is.EqualTo(-1));

        reducedMotion.Checked = true;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography, Is.SameAs(custom));
        Assert.That(baseFont.SelectedIndex, Is.EqualTo(-1));

        baseFont.SelectedIndex = 1;
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography,
            Is.SameAs(DemoTypography.CreateThemeTypography(DemoTypographyPreset.Base14Px)));
    }

    [Test]
    public void MainFormPublishesExactlyOneThemeChangePerUserSettingChange()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new MainForm();
        var themeMode = FindThemeModeCombo(form);
        var baseFont = FindBaseFontCombo(form);
        var reducedMotion = FindReducedMotionCheckBox(form);
        var eventCount = 0;

        EventHandler<BootstrapThemeChangedEventArgs> handler = (_, _) => eventCount++;
        BootstrapThemeManager.ThemeChanged += handler;
        try
        {
            baseFont.SelectedIndex = 1;
            Assert.That(eventCount, Is.EqualTo(1), "Base font change");

            eventCount = 0;
            themeMode.SelectedIndex = 1;
            Assert.That(eventCount, Is.EqualTo(1), "Theme change");

            eventCount = 0;
            reducedMotion.Checked = true;
            Assert.That(eventCount, Is.EqualTo(1), "Reduced motion change");
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= handler;
        }
    }

    [Test]
    public void MainFormPageTitleTracksActiveLabelTypography()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new MainForm();
        var title = FindControls<Label>(form)
            .Single(label => label.AccessibleName == "Current demo page title");

        AssertTitleFont(title, 12f);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base14Px);
        AssertTitleFont(title, 10.5f);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypographyPreset.Default);
        AssertTitleFont(title, 9f);
    }

    [Test]
    public void PaginationSectionHeadingTracksActiveHeadingSmallTypography()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new PaginationDemoForm();
        var heading = FindControls<Label>(form)
            .Single(label => label.Text == "Button sizes");

        Assert.That(heading.Font.SizeInPoints, Is.EqualTo(15f).Within(0.001f));

        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base14Px);
        Assert.That(heading.Font.SizeInPoints, Is.EqualTo(13.125f).Within(0.001f));

        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypographyPreset.Default);
        Assert.That(heading.Font.SizeInPoints, Is.EqualTo(11f).Within(0.001f));
    }

    [Test]
    public void AccordionSemanticSectionHeadingTracksActiveLabelTypography()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = new AccordionDemoForm();
        var heading = FindControls<Label>(form)
            .Single(label => label.Text == "Single-open composition (normal bordered style)");

        AssertTitleFont(heading, 12f);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypographyPreset.Base14Px);
        AssertTitleFont(heading, 10.5f);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Default);
        AssertTitleFont(heading, 9f);
    }

    [TestCase(typeof(CollapseDemoForm), "BootstrapCollapse — auto measurement, fixed height, reversal and reduced motion")]
    [TestCase(typeof(NavigationDemoForm), "Tabs style")]
    public void OtherRetainedSemanticHeadingsTrackActiveLabelTypography(
        Type formType,
        string headingText)
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);

        using var form = (Form)Activator.CreateInstance(formType)!;
        var heading = FindControls<Label>(form).Single(label => label.Text == headingText);

        AssertTitleFont(heading, 12f);
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Dark,
            DemoTypographyPreset.Default);
        AssertTitleFont(heading, 9f);
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
    public void ThemeAndReducedMotionChangesPreserveKnownDemoTypography()
    {
        BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
            BootstrapThemeMode.Light,
            DemoTypographyPreset.Base16Px);
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
    public void IntegratedDemoShellAndPageFormsUseSharedDemoFormBase()
    {
        var demoAssembly = typeof(MainForm).Assembly;
        var offenders = demoAssembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "MyDmsVn.Bootstrap5WinFormUI.Demo" &&
                !type.IsAbstract &&
                typeof(Form).IsAssignableFrom(type) &&
                (type.Name == "MainForm" ||
                 type.Name == "DemoPageHostForm" ||
                 type.Name.EndsWith("DemoForm", StringComparison.Ordinal)))
            .Where(type => !typeof(DemoFormBase).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .OrderBy(name => name)
            .ToArray();

        Assert.That(offenders, Is.Empty);
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

    private static BootstrapThemeTypography CreateCustomTypography()
    {
        return new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", 10f),
            new BootstrapFontToken("Segoe UI", 9f),
            new BootstrapFontToken("Segoe UI", 10f, FontStyle.Bold),
            new BootstrapFontToken("Segoe UI", 12f, FontStyle.Bold),
            new BootstrapFontToken("Segoe UI", 15f, FontStyle.Bold));
    }

    private static ComboBox FindThemeModeCombo(Control root)
    {
        return FindControls<ComboBox>(root)
            .Single(combo => combo.Items.Contains("Light") && combo.Items.Contains("Dark"));
    }

    private static ComboBox FindBaseFontCombo(Control root)
    {
        return FindControls<ComboBox>(root)
            .Single(combo => combo.AccessibleName == "Integrated demo base font profile");
    }

    private static CheckBox FindReducedMotionCheckBox(Control root)
    {
        return FindControls<CheckBox>(root)
            .Single(checkBox => checkBox.Text == "Reduced motion");
    }

    private static void AssertTheme(
        BootstrapThemeMode expectedMode,
        float expectedBodySize,
        bool expectedReducedMotion)
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(theme.Mode, Is.EqualTo(expectedMode));
            Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(expectedBodySize).Within(0.001f));
            Assert.That(theme.ReducedMotion, Is.EqualTo(expectedReducedMotion));
        }));
    }

    private static void AssertTitleFont(Label title, float expectedSize)
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(title.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.001f));
            Assert.That(title.Font.Bold, Is.True);
        }));
    }

    private static void AssertBodyTypography(
        Form form,
        Label nativeLabel,
        BootstrapButton bootstrapButton,
        float expectedSize)
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(form.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.001f));
            Assert.That(nativeLabel.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.001f));
            Assert.That(bootstrapButton.Font.SizeInPoints, Is.EqualTo(expectedSize).Within(0.001f));
        }));
    }
}
