using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSplitButtonTests
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
    public void DefaultsAndDeclaredPublicSurfaceMatchPlannedContract()
    {
        using var split = CreateSplitButton();
        var type = split.GetType();
        var items = GetProperty<BootstrapDropdownItemCollection>(split, "Items");
        var declaredProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(TypeDescriptor.GetDefaultProperty(type)?.Name, Is.EqualTo("Text"));
            Assert.That(TypeDescriptor.GetDefaultEvent(type)?.Name, Is.EqualTo("Click"));
            Assert.That(declaredProperties, Is.EqualTo(new[]
            {
                "BorderRadius", "ButtonSize", "Icon", "IconPosition", "IconRenderer", "Items",
                "Loading", "LoadingText", "MinimumWidth", "Outline", "Text", "Variant"
            }));
            Assert.That(GetProperty<string>(split, "Text"), Is.Empty);
            Assert.That(GetProperty<BootstrapVariant>(split, "Variant"), Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(GetProperty<bool>(split, "Outline"), Is.False);
            Assert.That(GetProperty<BootstrapButtonSize>(split, "ButtonSize"), Is.EqualTo(BootstrapButtonSize.Default));
            Assert.That(GetProperty<IconDescriptor?>(split, "Icon"), Is.Null);
            Assert.That(GetProperty<BootstrapIconPosition>(split, "IconPosition"), Is.EqualTo(BootstrapIconPosition.Left));
            Assert.That(GetProperty<IIconRenderer>(split, "IconRenderer"), Is.Not.Null);
            Assert.That(GetProperty<int>(split, "BorderRadius"), Is.EqualTo(-1));
            Assert.That(GetProperty<bool>(split, "Loading"), Is.False);
            Assert.That(GetProperty<string>(split, "LoadingText"), Is.Empty);
            Assert.That(items, Is.SameAs(GetProperty<BootstrapDropdownItemCollection>(split, "Items")));
            Assert.That(items, Is.Empty);
            Assert.That(GetProperty<int>(split, "MinimumWidth"), Is.Zero);
            Assert.That(split.TabStop, Is.False);
            Assert.That(split.Controls.OfType<BootstrapButton>().Count(), Is.EqualTo(2));
            Assert.That(split.Controls.OfType<BootstrapButton>().All(button => button.TabStop), Is.True);
            Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(property => property.PropertyType == typeof(BootstrapButton)), Is.False);
            Assert.That(type.GetProperty("Font", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Null);
            Assert.That(type.GetProperty("AccessibleName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Null);
        }));

        Assert.Throws<ArgumentNullException>((Action)(() => SetProperty(split, "IconRenderer", null)));
    }

    [Test]
    public void AppearancePropertiesForwardToExpectedButtonRegions()
    {
        using var split = CreateSplitButton();
        var icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus);
        var renderer = new RecordingIconRenderer();
        SetProperty(split, "Text", "Save");
        SetProperty(split, "Variant", BootstrapVariant.Success);
        SetProperty(split, "Outline", true);
        SetProperty(split, "ButtonSize", BootstrapButtonSize.Large);
        SetProperty(split, "Icon", icon);
        SetProperty(split, "IconPosition", BootstrapIconPosition.Right);
        SetProperty(split, "IconRenderer", renderer);
        SetProperty(split, "BorderRadius", 10);
        SetProperty(split, "LoadingText", "Saving");
        var (primary, menu) = GetRegions(split);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(primary.Text, Is.EqualTo("Save"));
            Assert.That(primary.Icon, Is.SameAs(icon));
            Assert.That(primary.IconPosition, Is.EqualTo(BootstrapIconPosition.Right));
            Assert.That(primary.LoadingText, Is.EqualTo("Saving"));
            Assert.That(menu.Text, Is.Empty);
            Assert.That(menu.Icon?.SourceKind, Is.EqualTo(IconSourceKind.FrameworkVector));
            Assert.That(menu.Icon?.Value, Is.EqualTo(FrameworkIconGlyph.ChevronDown.ToString()));
            Assert.That(primary.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(menu.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(primary.Outline, Is.True);
            Assert.That(menu.Outline, Is.True);
            Assert.That(primary.ButtonSize, Is.EqualTo(BootstrapButtonSize.Large));
            Assert.That(menu.ButtonSize, Is.EqualTo(BootstrapButtonSize.Large));
            Assert.That(primary.IconRenderer, Is.SameAs(renderer));
            Assert.That(menu.IconRenderer, Is.SameAs(renderer));
            Assert.That(primary.BorderRadius, Is.EqualTo(10));
            Assert.That(menu.BorderRadius, Is.EqualTo(10));
        }));
    }

    [Test]
    public void PrimaryActivationRaisesOuterClickExactlyOnceAndLoadingOrDisabledSuppressesIt()
    {
        using var split = CreateSplitButton();
        var (primary, menu) = GetRegions(split);
        var clicks = 0;
        object? sender = null;
        split.Click += (actualSender, _) =>
        {
            clicks++;
            sender = actualSender;
        };

        primary.PerformClick();
        menu.PerformClick();
        SetProperty(split, "Loading", true);
        primary.PerformClick();
        SetProperty(split, "Loading", false);
        split.Enabled = false;
        primary.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(sender, Is.SameAs(split));
            Assert.That(menu.Enabled, Is.False);
        }));
    }

    [Test]
    public void PreferredAndCustomWidthLayoutKeepsConnectedRegionsValid()
    {
        using var split = CreateSplitButton();
        SetProperty(split, "Text", "A primary action");
        SetProperty(split, "BorderRadius", 9);
        var preferred = split.GetPreferredSize(Size.Empty);
        split.AutoSize = false;
        split.Size = new Size(preferred.Width + 80, preferred.Height);
        split.PerformLayout();
        var (primary, menu) = GetRegions(split);
        var overlap = BootstrapConnectedButtonLayoutLogic.ResolveSeamOverlap(
            BootstrapThemeManager.CurrentTheme.Metrics,
            split.DeviceDpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(preferred.Width, Is.GreaterThan(primary.GetPreferredSize(Size.Empty).Width));
            Assert.That(primary.Height, Is.EqualTo(menu.Height));
            Assert.That(menu.Width, Is.EqualTo(menu.GetPreferredSize(Size.Empty).Width));
            Assert.That(menu.Left, Is.EqualTo(primary.Right - overlap));
            Assert.That(primary.GroupCornerRadius, Is.EqualTo(new CornerRadius(9f, 0f, 0f, 9f)));
            Assert.That(menu.GroupCornerRadius, Is.EqualTo(new CornerRadius(0f, 9f, 9f, 0f)));
        }));

        split.Size = new Size(1, 1);
        split.PerformLayout();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(primary.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(menu.Width, Is.GreaterThanOrEqualTo(0));
            Assert.That(primary.Height, Is.GreaterThanOrEqualTo(0));
            Assert.That(menu.Height, Is.GreaterThanOrEqualTo(0));
        }));
    }

    [Test]
    public void ThemeFontRemainsDynamicUntilCallerAssignsCustomFont()
    {
        using var split = CreateSplitButton();
        var (primary, menu) = GetRegions(split);
        BootstrapThemeManager.CurrentTheme = CreateThemeWithBodySize(10.5f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(primary.Font.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
            Assert.That(menu.Font.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
            Assert.That(split.Font.SizeInPoints, Is.EqualTo(primary.Font.SizeInPoints).Within(0.01f));
        }));

        using var customFont = new Font("Segoe UI", 12f, FontStyle.Italic);
        split.Font = customFont;
        BootstrapThemeManager.CurrentTheme = CreateThemeWithBodySize(8f);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(split.Font, Is.SameAs(customFont));
            Assert.That(primary.Font, Is.SameAs(customFont));
            Assert.That(menu.Font, Is.SameAs(customFont));
        }));
    }

    [Test]
    public void RegionAccessibilityNamesResolveDynamicallyFromOuterMetadata()
    {
        using var split = CreateSplitButton();
        split.Text = "Save";
        var (primary, menu) = GetRegions(split);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(primary.AccessibilityObject.Name, Is.EqualTo("Save"));
            Assert.That(menu.AccessibilityObject.Name, Is.EqualTo("Save menu"));
            Assert.That(menu.AccessibilityObject.Description, Does.Contain("additional commands").IgnoreCase);
        }));

        Control asControl = split;
        asControl.AccessibleName = "Record actions";
        asControl.Text = "Changed text";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(primary.AccessibilityObject.Name, Is.EqualTo("Record actions"));
            Assert.That(menu.AccessibilityObject.Name, Is.EqualTo("Record actions menu"));
        }));
    }

    private static Control CreateSplitButton()
    {
        var type = typeof(BootstrapButton).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSplitButton");
        Assert.That(type, Is.Not.Null, "BootstrapSplitButton must exist.");
        return (Control)Activator.CreateInstance(type!)!;
    }

    private static (BootstrapButton Primary, BootstrapButton Menu) GetRegions(Control split)
    {
        var buttons = split.Controls.OfType<BootstrapButton>().ToArray();
        Assert.That(buttons.Length, Is.EqualTo(2));
        var menu = buttons.Single(button => button.Icon?.Value == FrameworkIconGlyph.ChevronDown.ToString());
        return (buttons.Single(button => !ReferenceEquals(button, menu)), menu);
    }

    private static T GetProperty<T>(object target, string name)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Missing property {name}.");
        return (T)property!.GetValue(target)!;
    }

    private static void SetProperty(object target, string name, object? value)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Missing property {name}.");
        try
        {
            property!.SetValue(target, value);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static BootstrapTheme CreateThemeWithBodySize(float bodySize)
    {
        var typography = BootstrapThemeTypography.Default;
        return new BootstrapTheme(
            BootstrapThemeMode.Light,
            BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light),
            BootstrapThemeMetrics.Default,
            new BootstrapThemeTypography(
                new BootstrapFontToken("Segoe UI", bodySize),
                typography.BodySmall,
                typography.Label,
                typography.HeadingSmall,
                typography.HeadingMedium));
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            return true;
        }
    }
}
