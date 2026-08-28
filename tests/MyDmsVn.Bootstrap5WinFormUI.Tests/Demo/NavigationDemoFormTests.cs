using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class NavigationDemoFormTests
{
    [Test]
    public void DemoCoversAllStylesFillNativeImagesTooltipsDisabledAndAllVariants()
    {
        using var form = new NavigationDemoForm();
        var tabs = Descendants(form).OfType<BootstrapTabControl>().ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tabs.Select(tab => tab.TabStyle).Distinct(), Is.SupersetOf(new[]
            {
                BootstrapTabStyle.Tabs,
                BootstrapTabStyle.Pills,
                BootstrapTabStyle.Underline
            }));
            Assert.That(tabs.Any(tab => tab.Fill), Is.True);
            Assert.That(tabs.Any(tab => tab.ImageList is not null), Is.True);
            Assert.That(tabs.Any(tab => tab.ShowToolTips), Is.True);
            Assert.That(tabs.SelectMany(tab => tab.TabPages.Cast<TabPage>()).Any(page => !page.Enabled), Is.True);
            Assert.That(tabs.SelectMany(tab => tab.TabPages.Cast<TabPage>()).Any(page => page.Text.Length > 50), Is.True);
            Assert.That(tabs.Select(tab => tab.Variant).Distinct().Count(), Is.EqualTo(8));
            Assert.That(Descendants(form).OfType<Label>().Any(label => label.AccessibleName == "Selected tab status"), Is.True);
            Assert.That(Descendants(form).OfType<TextBox>().Any(), Is.True, "Tabs demo should expose a focusable TextBox for Tab/Shift+Tab verification.");
            Assert.That(Descendants(form).OfType<Button>().Any(), Is.True, "Tabs demo should expose a focusable Button for Tab/Shift+Tab verification.");
            Assert.That(Descendants(form).OfType<CheckBox>().Any(), Is.True, "Tabs demo should expose a focusable CheckBox for Tab/Shift+Tab verification.");
        }));
    }

    [Test]
    public void DemoCoversBasicIconStateLongAndStressDropdownScenarios()
    {
        using var form = new NavigationDemoForm();
        var dropdownsField = typeof(NavigationDemoForm).GetField("_dropdowns", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(dropdownsField, Is.Not.Null);
        var dropdowns = ((IEnumerable<BootstrapDropdown>)dropdownsField!.GetValue(form)!).ToArray();
        var byTargetName = dropdowns.ToDictionary(dropdown => dropdown.Target!.AccessibleName!, StringComparer.Ordinal);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdowns.Length, Is.EqualTo(5));
            Assert.That(byTargetName.Keys, Is.SupersetOf(new[]
            {
                "Dropdown basic",
                "Dropdown icons",
                "Dropdown states",
                "Dropdown long menu",
                "Dropdown stress"
            }));

            Assert.That(byTargetName["Dropdown basic"].Items.Count, Is.EqualTo(3));
            Assert.That(byTargetName["Dropdown basic"].Items.All(item => item.Kind == BootstrapDropdownItemKind.Item && item.Enabled), Is.True);

            Assert.That(byTargetName["Dropdown icons"].Items.Count, Is.EqualTo(3));
            Assert.That(byTargetName["Dropdown icons"].Items.All(item => item.Icon is not null), Is.True);

            Assert.That(byTargetName["Dropdown states"].Items.Any(item => item.Checked), Is.True);
            Assert.That(byTargetName["Dropdown states"].Items.Any(item => !item.Enabled), Is.True);
            Assert.That(byTargetName["Dropdown states"].Items.Any(item => item.Kind == BootstrapDropdownItemKind.Separator), Is.True);
            Assert.That(byTargetName["Dropdown states"].Items.Any(item => item.Kind == BootstrapDropdownItemKind.Item && item.Enabled && !item.Checked), Is.True);

            Assert.That(byTargetName["Dropdown long menu"].MinimumWidth, Is.GreaterThan(0));
            Assert.That(byTargetName["Dropdown long menu"].Items.Any(item => item.Text.Length > 50), Is.True);

            Assert.That(byTargetName["Dropdown stress"].Items.Any(item => item.Text.IndexOf("Toggle Light / Dark", StringComparison.Ordinal) >= 0), Is.True);
            Assert.That(Descendants(form).OfType<Label>().Any(label => label.AccessibleName == "Dropdown manual verification matrix"), Is.True);
        }));
    }

    [Test]
    public void IntegratedDemoKeepsOneSharedNavigationRouteAndNoDropdownOnlyRoute()
    {
        using var form = new MainForm();
        var navigationField = typeof(MainForm).GetField("_navigation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(navigationField, Is.Not.Null);
        var navigation = (BootstrapSidebar)navigationField!.GetValue(form)!;
        var rootItems = navigation.Items.ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(rootItems.Count(item => item.Text.StartsWith("Navigation", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(rootItems.Any(item => item.Text.IndexOf("Dropdown", StringComparison.Ordinal) >= 0), Is.False);
        }));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child))
            {
                yield return nested;
            }
        }
    }
}
