using System;
using System.Collections.Generic;
using System.Linq;
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
