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
public sealed class InputGroupDemoFormTests
{
    [Test]
    public void DemoContainsRepresentativeScenariosAndOnlySingleSelects()
    {
        using var form = new InputGroupDemoForm();
        form.CreateControl();
        form.PerformLayout();
        var groups = FindControls<BootstrapInputGroup>(form).ToArray();

        Assert.That(groups.Length, Is.GreaterThanOrEqualTo(14));
        Assert.That(groups.SelectMany(group => FindControls<BootstrapNumericBox>(group)).Any(), Is.True);
        Assert.That(groups.SelectMany(group => FindControls<BootstrapFormattedTextBox>(group)).Any(), Is.True);
        Assert.That(groups.SelectMany(group => FindControls<BootstrapSplitButton>(group)).Any(), Is.True);
        Assert.That(groups.SelectMany(group => FindControls<BootstrapSelect>(group)).All(select => select.SelectionMode == BootstrapSelectMode.Single), Is.True);
        Assert.That(groups.All(group => group.Controls.Cast<Control>().All(child => child.Left >= 0 && child.Right <= group.ClientSize.Width)), Is.True);
    }

    private static IEnumerable<T> FindControls<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in FindControls<T>(child)) yield return nested;
        }
    }
}
