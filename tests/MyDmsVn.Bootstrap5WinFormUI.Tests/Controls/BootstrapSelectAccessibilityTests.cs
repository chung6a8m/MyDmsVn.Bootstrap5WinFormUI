using System;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
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
}
