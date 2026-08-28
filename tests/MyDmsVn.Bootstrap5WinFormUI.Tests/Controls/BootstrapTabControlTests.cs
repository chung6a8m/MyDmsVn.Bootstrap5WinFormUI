using System;
using System.ComponentModel;
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
public sealed class BootstrapTabControlTests
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
    public void DefaultsMatchNativeBackedTabContract()
    {
        using var tabs = new BootstrapTabControl();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs, Is.InstanceOf<TabControl>());
            Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Tabs));
            Assert.That(tabs.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(tabs.Fill, Is.False);
            Assert.That(tabs.BorderRadius, Is.EqualTo(-1));
            Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
            Assert.That(tabs.SizeMode, Is.EqualTo(TabSizeMode.Fixed));
            Assert.That(tabs.Alignment, Is.EqualTo(TabAlignment.Top));
            Assert.That(tabs.Multiline, Is.False);
        }));

        AssertDefaultValue(nameof(BootstrapTabControl.TabStyle), BootstrapTabStyle.Tabs);
        AssertDefaultValue(nameof(BootstrapTabControl.Variant), BootstrapVariant.Primary);
        AssertDefaultValue(nameof(BootstrapTabControl.Fill), false);
        AssertDefaultValue(nameof(BootstrapTabControl.BorderRadius), -1);
    }

    [Test]
    public void PublicDeclaredSurfaceContainsOnlyPlannedMembers()
    {
        var type = typeof(BootstrapTabControl);
        var publicDeclared = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Constructor or MemberTypes.Event or MemberTypes.Property or MemberTypes.Method)
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(publicDeclared, Is.EqualTo(new[]
        {
            ".ctor",
            "BorderRadius",
            "Fill",
            "TabStyle",
            "Variant"
        }));
    }

    [Test]
    public void SelectedIndexChangedRemainsTheDefaultEvent()
    {
        var attribute = typeof(BootstrapTabControl).GetCustomAttribute<DefaultEventAttribute>();

        Assert.That(attribute?.Name, Is.EqualTo(nameof(TabControl.SelectedIndexChanged)));
    }

    [Test]
    public void NativeTabPagesRemainTheCompositionSurfaceAndSelectionEventRemainsNative()
    {
        using var tabs = new BootstrapTabControl();
        var first = new TabPage("General");
        var second = new TabPage("Advanced");
        var eventCount = 0;
        tabs.SelectedIndexChanged += (_, _) => eventCount++;

        tabs.TabPages.Add(first);
        tabs.TabPages.Add(second);
        eventCount = 0;
        tabs.SelectedTab = second;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.TabPages.Count, Is.EqualTo(2));
            Assert.That(tabs.TabPages[0], Is.SameAs(first));
            Assert.That(tabs.TabPages[1], Is.SameAs(second));
            Assert.That(tabs.SelectedTab, Is.SameAs(second));
            Assert.That(tabs.SelectedIndex, Is.EqualTo(1));
            Assert.That(eventCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void FrameworkPropertiesDoNotMutateNativePageCollectionOrSelection()
    {
        using var tabs = new BootstrapTabControl();
        tabs.TabPages.Add(new TabPage("One"));
        tabs.TabPages.Add(new TabPage("Two"));
        tabs.SelectedIndex = 1;

        tabs.TabStyle = BootstrapTabStyle.Pills;
        tabs.Variant = BootstrapVariant.Success;
        tabs.Fill = true;
        tabs.BorderRadius = 10;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.TabPages.Count, Is.EqualTo(2));
            Assert.That(tabs.SelectedIndex, Is.EqualTo(1));
            Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
            Assert.That(tabs.SizeMode, Is.EqualTo(TabSizeMode.Fixed));
        }));
    }

    [Test]
    public void FrameworkPropertyValidationOccursBeforeMutation()
    {
        using var tabs = new BootstrapTabControl();

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tabs.TabStyle = (BootstrapTabStyle)999));
            Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Tabs));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tabs.Variant = (BootstrapVariant)999));
            Assert.That(tabs.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tabs.BorderRadius = -2));
            Assert.That(tabs.BorderRadius, Is.EqualTo(-1));
        }));
    }

    [Test]
    public void NativeInheritedKnobsRemainCallerOwned()
    {
        using var tabs = new BootstrapTabControl();
        using var imageList = new ImageList();

        tabs.HotTrack = true;
        tabs.ShowToolTips = true;
        tabs.ImageList = imageList;
        tabs.Padding = new Point(9, 4);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.HotTrack, Is.True);
            Assert.That(tabs.ShowToolTips, Is.True);
            Assert.That(tabs.ImageList, Is.SameAs(imageList));
            Assert.That(tabs.Padding, Is.EqualTo(new Point(9, 4)));
        }));
    }

    [Test]
    public void FillUsesOneUniformFixedHeaderWidthAndRetainsNativePageOrder()
    {
        using var tabs = new BootstrapTabControl
        {
            Width = 600,
            Height = 180,
            Fill = true
        };
        tabs.TabPages.Add("One");
        tabs.TabPages.Add("Two");
        tabs.TabPages.Add("Three");

        var expected = Math.Max(54, tabs.ClientSize.Width / tabs.TabCount);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.ItemSize.Width, Is.EqualTo(expected));
            Assert.That(tabs.ItemSize.Height, Is.EqualTo(32));
            Assert.That(tabs.TabPages.Cast<TabPage>().Select(page => page.Text), Is.EqualTo(new[] { "One", "Two", "Three" }));
        }));
    }

    [Test]
    public void NonFillWidthTracksWidestNativeTabText()
    {
        using var tabs = new BootstrapTabControl { Width = 800, Height = 180 };
        var page = new TabPage("Short");
        tabs.TabPages.Add(page);
        tabs.TabPages.Add(new TabPage("Peer"));
        var initialWidth = tabs.ItemSize.Width;

        page.Text = "A much longer native tab title used to verify deterministic fixed sizing";

        Assert.That(tabs.ItemSize.Width, Is.GreaterThan(initialWidth));
    }

    [Test]
    public void OwnerDrawSmokeSupportsEveryStyleThemeImagesLongTextAndDisabledPages()
    {
        using var host = new Form { ClientSize = new Size(900, 280) };
        using var images = new ImageList { ImageSize = new Size(16, 16) };
        images.Images.Add("info", SystemIcons.Information.ToBitmap());

        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            foreach (var style in new[] { BootstrapTabStyle.Tabs, BootstrapTabStyle.Pills, BootstrapTabStyle.Underline })
            {
                using var tabs = new BootstrapTabControl
                {
                    Bounds = new Rectangle(10, 10, 840, 180),
                    TabStyle = style,
                    Variant = BootstrapVariant.Info,
                    ImageList = images,
                    ShowToolTips = true
                };
                var imagePage = new TabPage("Image") { ImageKey = "info", ToolTipText = "Native tooltip" };
                var longPage = new TabPage("A deliberately long native tab title for ellipsis behavior");
                var disabledPage = new TabPage("Disabled") { Enabled = false };
                tabs.TabPages.Add(imagePage);
                tabs.TabPages.Add(longPage);
                tabs.TabPages.Add(disabledPage);
                host.Controls.Add(tabs);
                host.CreateControl();
                tabs.CreateControl();

                using var bitmap = new Bitmap(tabs.Width, tabs.Height);
                Assert.DoesNotThrow((Action)(() => tabs.DrawToBitmap(bitmap, tabs.ClientRectangle)), $"{mode}/{style}");
                Assert.Multiple((Action)(() =>
                {
                    Assert.That(imagePage.ImageKey, Is.EqualTo("info"));
                    Assert.That(imagePage.ToolTipText, Is.EqualTo("Native tooltip"));
                    Assert.That(disabledPage.Enabled, Is.False);
                }));

                host.Controls.Remove(tabs);
            }
        }
    }

    [Test]
    public void RuntimeThemeSwitchUpdatesThemeOwnedFontInPlace()
    {
        using var tabs = new BootstrapTabControl();
        var initialReference = tabs;
        var baseTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        var typography = new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
            baseTheme.Typography.BodySmall,
            baseTheme.Typography.Label,
            baseTheme.Typography.HeadingSmall,
            baseTheme.Typography.HeadingMedium);

        BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
            BootstrapThemeMode.Dark,
            baseTheme.Colors,
            baseTheme.Metrics,
            typography);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs, Is.SameAs(initialReference));
            Assert.That(tabs.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
            Assert.That(tabs.Font.Style, Is.EqualTo(FontStyle.Bold));
        }));
    }

    [Test]
    public void CallerOwnedFontRemainsCallerOwnedAcrossThemeSwitchAndDisposal()
    {
        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var tabs = new BootstrapTabControl { Font = callerFont };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(tabs.Font, Is.SameAs(callerFont));

        tabs.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void DisposalReleasesThemeSubscriptionAndThemeOwnedFont()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var tabs = new BootstrapTabControl();
        var ownedFont = tabs.Font;

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        tabs.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)));

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", ownedFont)));
    }

    [Test]
    public void RepeatedLifecycleStressDoesNotLeakStaticThemeHandlers()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();

        for (var index = 0; index < 100; index++)
        {
            using var tabs = new BootstrapTabControl
            {
                Fill = index % 2 == 0,
                TabStyle = (BootstrapTabStyle)(index % 3),
                Variant = (BootstrapVariant)(index % 8)
            };
            tabs.TabPages.Add(new TabPage("One"));
            tabs.TabPages.Add(new TabPage("Two"));
            tabs.SelectedIndex = 1;

            if (index % 20 == 0)
            {
                var mode = BootstrapThemeManager.CurrentTheme.Mode == BootstrapThemeMode.Light
                    ? BootstrapThemeMode.Dark
                    : BootstrapThemeMode.Light;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            }
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
    }

    private static void AssertDefaultValue(string propertyName, object expected)
    {
        var property = TypeDescriptor.GetProperties(typeof(BootstrapTabControl))[propertyName];
        Assert.That(property, Is.Not.Null);
        var attribute = (DefaultValueAttribute?)property!.Attributes[typeof(DefaultValueAttribute)];
        Assert.That(attribute, Is.Not.Null, $"{propertyName} should declare DefaultValueAttribute.");
        Assert.That(attribute!.Value, Is.EqualTo(expected));
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }
}
