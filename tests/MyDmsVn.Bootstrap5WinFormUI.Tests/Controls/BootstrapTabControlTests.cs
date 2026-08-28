using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTabControlTests
{
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
        tabs.Padding = new System.Drawing.Point(9, 4);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.HotTrack, Is.True);
            Assert.That(tabs.ShowToolTips, Is.True);
            Assert.That(tabs.ImageList, Is.SameAs(imageList));
            Assert.That(tabs.Padding, Is.EqualTo(new System.Drawing.Point(9, 4)));
        }));
    }
}
