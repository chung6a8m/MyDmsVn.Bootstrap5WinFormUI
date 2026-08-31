using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectAccessibilityTests
{
    [Test]
    public void SingleSelectionReportsComboBoxRoleValueAndCollapsedState()
    {
        using var select = new BootstrapSelect { AccessibleName = "Customer" };
        select.Select(new BootstrapSelectItem(1, "Contoso"));

        var accessible = select.AccessibilityObject;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(accessible.Role, Is.EqualTo(AccessibleRole.ComboBox));
            Assert.That(accessible.Name, Is.EqualTo("Customer"));
            Assert.That(accessible.Value, Is.EqualTo("Contoso"));
            Assert.That(accessible.State.HasFlag(AccessibleStates.Collapsed), Is.True);
            Assert.That(accessible.State.HasFlag(AccessibleStates.Focusable), Is.True);
        }));
    }

    [Test]
    public void MultipleSelectionReportsStableCountSummary()
    {
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };
        select.Select(new BootstrapSelectItem(1, "Alpha"));
        select.Select(new BootstrapSelectItem(2, "Beta"));

        Assert.That(select.AccessibilityObject.Value, Is.EqualTo("2 selected"));
    }

    [Test]
    public void PopupSearchCompositionExposesOneLogicalAccessibleTextEditor()
    {
        using var content = new BootstrapSelectDropDownContent
        {
            Size = new Size(340, 180)
        };
        content.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            96);
        content.PerformLayout();

        var search = Descendants(content).OfType<BootstrapTextBox>().Single();
        var native = Descendants(search).OfType<TextBox>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(search.AccessibilityObject.Role, Is.Not.EqualTo(AccessibleRole.Text));
            Assert.That(native.AccessibilityObject.Role, Is.EqualTo(AccessibleRole.Text));
            Assert.That(native.AccessibilityObject.Name, Is.EqualTo("Search"));
            Assert.That(
                Descendants(search).Count(control => control.AccessibilityObject.Role == AccessibleRole.Text),
                Is.EqualTo(1));
        }));
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
