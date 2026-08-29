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

    [Test]
    public void MenuRegionAndPublicMethodsShareDropdownLifecycleWithoutRaisingPrimaryClick()
    {
        using var split = CreateSplitButton();
        split.Text = "Save";
        var items = GetProperty<BootstrapDropdownItemCollection>(split, "Items");
        var nestedClicks = 0;
        var parent = new BootstrapDropdownItem { Text = "More" };
        var leaf = new BootstrapDropdownItem { Text = "Save as" };
        leaf.Click += (_, _) => nestedClicks++;
        parent.DropDownItems.Add(leaf);
        items.Add(parent);
        using var form = CreateHost(split);
        var (primary, menu) = GetRegions(split);
        var dropdown = GetOwnedDropdown(split);
        var native = GetNativeDropDown(dropdown);
        var primaryClicks = 0;
        var opened = 0;
        var closed = 0;
        object? openedSender = null;
        object? closedSender = null;
        split.Click += (_, _) => primaryClicks++;
        AddEventHandler(split, "Opened", (sender, _) => { opened++; openedSender = sender; });
        AddEventHandler(split, "Closed", (sender, _) => { closed++; closedSender = sender; });

        primary.PerformClick();
        Application.DoEvents();
        Assert.That(native.Visible, Is.False);

        menu.PerformClick();
        Application.DoEvents();
        Assert.That(native.Visible, Is.True);
        menu.PerformClick();
        Application.DoEvents();
        Assert.That(native.Visible, Is.False);

        InvokePublic(split, "ShowDropDown");
        Application.DoEvents();
        var nativeParent = (ToolStripMenuItem)native.Items[0];
        var nativeLeaf = (ToolStripMenuItem)nativeParent.DropDownItems[0];
        nativeLeaf.PerformClick();
        Application.DoEvents();
        InvokePublic(split, "CloseDropDown");
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(primaryClicks, Is.EqualTo(1));
            Assert.That(nestedClicks, Is.EqualTo(1));
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(2));
            Assert.That(openedSender, Is.SameAs(split));
            Assert.That(closedSender, Is.SameAs(split));
            Assert.That(dropdown.Target, Is.Null);
        }));
    }

    [Test]
    public void DropdownOpeningUsesOuterSplitAsFullWidthAnchor()
    {
        using var split = CreateSplitButton();
        split.Size = new Size(240, 44);

        var anchor = InvokePrivate<Control>(split, "ResolveDropDownAnchor");
        var location = InvokePrivate<Point>(split, "ResolveDropDownLocation");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(anchor, Is.SameAs(split));
            Assert.That(location, Is.EqualTo(new Point(0, split.Height)));
        }));
    }

    [Test]
    public void EmptyDisabledAndLoadingStatesDoNotOpenAndRuntimeStateClosesAnOpenMenu()
    {
        using var split = CreateSplitButton();
        using var form = CreateHost(split);
        var (_, menu) = GetRegions(split);
        var dropdown = GetOwnedDropdown(split);
        var native = GetNativeDropDown(dropdown);
        var opened = 0;
        AddEventHandler(split, "Opened", (_, _) => opened++);

        menu.PerformClick();
        InvokePublic(split, "ShowDropDown");
        Application.DoEvents();
        Assert.That(native.Visible, Is.False);

        GetProperty<BootstrapDropdownItemCollection>(split, "Items").Add(
            new BootstrapDropdownItem { Text = "Action" });
        menu.PerformClick();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Visible, Is.True);
            Assert.That(menu.Selected, Is.True);
        }));

        SetProperty(split, "MinimumWidth", 230);
        Assert.That(
            native.MinimumSize.Width,
            Is.EqualTo(BootstrapDropdown.ResolveMinimumWidth(230, menu.DeviceDpi)));

        SetProperty(split, "Loading", true);
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Visible, Is.False);
            Assert.That(menu.Selected, Is.False);
            Assert.That(menu.Enabled, Is.False);
        }));
        menu.PerformClick();
        InvokePublic(split, "ShowDropDown");
        Application.DoEvents();
        Assert.That(opened, Is.EqualTo(1));

        SetProperty(split, "Loading", false);
        InvokePublic(split, "ShowDropDown");
        Application.DoEvents();
        Assert.That(native.Visible, Is.True);
        split.Enabled = false;
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Visible, Is.False);
            Assert.That(menu.Selected, Is.False);
        }));
    }

    [Test]
    public void ParentDisposalOwnsChildAndDropdownCleanupWithoutASecondChildPath()
    {
        var split = CreateSplitButton();
        var children = split.Controls.Cast<Control>().ToArray();
        var dropdown = GetOwnedDropdown(split);
        GetProperty<BootstrapDropdownItemCollection>(split, "Items").Add(
            new BootstrapDropdownItem { Text = "Action" });
        using var form = CreateHost(split);
        InvokePublic(split, "ShowDropDown");
        Application.DoEvents();

        Assert.DoesNotThrow((Action)(() => split.Dispose()));
        Assert.DoesNotThrow((Action)(() => split.Dispose()));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(children.All(child => child.IsDisposed), Is.True);
            Assert.That(GetNativeDropDown(dropdown).IsDisposed, Is.True);
        }));
        Assert.Throws<ObjectDisposedException>((Action)(() => dropdown.Show()));
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

    private static Form CreateHost(Control control)
    {
        var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(300, 240),
            Size = new Size(600, 360)
        };
        control.Location = new Point(40, 40);
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static BootstrapDropdown GetOwnedDropdown(Control split)
    {
        var field = split.GetType().GetField("_dropdown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (BootstrapDropdown)field!.GetValue(split)!;
    }

    private static ToolStripDropDownMenu GetNativeDropDown(BootstrapDropdown dropdown)
    {
        var field = typeof(BootstrapDropdown).GetField("_dropDown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (ToolStripDropDownMenu)field!.GetValue(dropdown)!;
    }

    private static void AddEventHandler(Control target, string name, EventHandler handler)
    {
        var eventInfo = target.GetType().GetEvent(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(eventInfo, Is.Not.Null, $"Missing event {name}.");
        eventInfo!.AddEventHandler(target, handler);
    }

    private static void InvokePublic(Control target, string name)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, $"Missing method {name}.");
        Invoke(method!, target);
    }

    private static T InvokePrivate<T>(Control target, string name)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null, $"Missing private method {name}.");
        return (T)Invoke(method!, target)!;
    }

    private static object? Invoke(MethodInfo method, object target)
    {
        try
        {
            return method.Invoke(target, Array.Empty<object>());
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
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
