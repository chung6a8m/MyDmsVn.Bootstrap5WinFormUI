using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCheckableInteractionTests
{
    [Test]
    public void NativeCheckBoxAllowsProgrammaticIndeterminateRegardlessOfThreeStateAndRaisesNativeEvents()
    {
        using var checkBox = new CheckBox { ThreeState = false };
        var checkedChanged = 0;
        var stateChanged = 0;
        checkBox.CheckedChanged += (_, _) => checkedChanged++;
        checkBox.CheckStateChanged += (_, _) => stateChanged++;

        checkBox.CheckState = CheckState.Indeterminate;
        checkBox.CheckState = CheckState.Checked;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checkBox.CheckState, Is.EqualTo(CheckState.Checked));
            Assert.That(checkedChanged, Is.EqualTo(1));
            Assert.That(stateChanged, Is.EqualTo(2));
        }));
    }

    [Test]
    public void NativeRadioAutoCheckFalseAllowsCallerManagedMultipleCheckedState()
    {
        using var parent = new Panel();
        using var first = new RadioButton { AutoCheck = false };
        using var second = new RadioButton { AutoCheck = false };
        parent.Controls.Add(first);
        parent.Controls.Add(second);

        first.Checked = true;
        second.Checked = true;
        first.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Checked, Is.True);
            Assert.That(second.Checked, Is.True);
        }));
    }
}
