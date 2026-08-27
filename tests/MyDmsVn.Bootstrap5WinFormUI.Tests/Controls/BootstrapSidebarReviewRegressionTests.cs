using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSidebarReviewRegressionTests
{
    [Test]
    public void CollapsedNestedSectionRemovesChildRowsFromTabOrder()
    {
        using var sidebar = new BootstrapSidebar();
        var reports = new BootstrapSidebarItem { Text = "Reports", Expanded = false };
        var sales = new BootstrapSidebarItem { Text = "Sales" };
        reports.Items.Add(sales);
        sidebar.Items.Add(reports);

        var childButton = FindButton(sidebar, sales);

        Assert.That(childButton.TabStop, Is.False, "A row clipped by a collapsed section must not remain in the WinForms tab sequence.");

        reports.Expanded = true;
        Assert.That(FindButton(sidebar, sales).TabStop, Is.True);

        sidebar.Collapse();
        Assert.That(FindButton(sidebar, sales).TabStop, Is.False);
    }

    [Test]
    public void SidebarContentForegroundMatchesBaseButtonPressedPalette()
    {
        var theme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        using var button = new BootstrapSidebarItemButton(BootstrapIconRenderer.CreateDefault())
        {
            Variant = BootstrapVariant.Primary,
            Outline = true,
            Enabled = true,
            Selected = false
        };

        var pressedField = typeof(BootstrapButton).GetField("_pressed", BindingFlags.Instance | BindingFlags.NonPublic);
        var resolveForeground = typeof(BootstrapSidebarItemButton).GetMethod("ResolveForeground", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(pressedField, Is.Not.Null);
        Assert.That(resolveForeground, Is.Not.Null);

        pressedField!.SetValue(button, true);
        var actual = (Color)resolveForeground!.Invoke(button, new object[] { theme })!;
        var expected = BootstrapButtonRenderLogic.ResolvePalette(
            theme.Colors,
            button.Variant,
            button.Outline,
            button.Enabled,
            button.Selected,
            BootstrapButtonVisualState.Pressed).Foreground;

        Assert.That(actual.ToArgb(), Is.EqualTo(expected.ToArgb()));
    }

    [Test]
    public void ReplacingNestedBindingListItemRebuildsVisualTreeAndSubscriptions()
    {
        using var sidebar = new BootstrapSidebar();
        var reports = new BootstrapSidebarItem { Text = "Reports", Expanded = true };
        var original = new BootstrapSidebarItem { Text = "Original" };
        reports.Items.Add(original);
        sidebar.Items.Add(reports);
        sidebar.SelectedItem = original;

        var replacement = new BootstrapSidebarItem { Text = "Replacement" };
        reports.Items[0] = replacement;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(sidebar.SelectedItem, Is.Null, "Replacing the selected model item must clear the now-foreign selection.");
            Assert.That(FindButtons(sidebar).Any(button => ReferenceEquals(button.Tag, original)), Is.False);
            Assert.That(FindButtons(sidebar).Any(button => ReferenceEquals(button.Tag, replacement)), Is.True);
        }));

        replacement.Text = "Updated replacement";
        Assert.That(FindButton(sidebar, replacement).AccessibleName, Is.EqualTo("Updated replacement"));
    }

    private static BootstrapButton FindButton(Control root, BootstrapSidebarItem item)
    {
        return FindButtons(root).Single(button => ReferenceEquals(button.Tag, item));
    }

    private static IEnumerable<BootstrapButton> FindButtons(Control root)
    {
        return Descendants(root).OfType<BootstrapButton>();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
