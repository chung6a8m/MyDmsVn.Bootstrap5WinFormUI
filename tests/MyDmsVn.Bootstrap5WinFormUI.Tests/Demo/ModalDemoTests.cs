using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ModalDemoTests
{
    [Test]
    public void DemoOffersAllSizeAndBackdropScenarios()
    {
        using var form = new ModalDemoForm();
        var captions = FindButtons(form).Select(button => button.Text).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(captions, Does.Contain("Small"));
            Assert.That(captions, Does.Contain("Default"));
            Assert.That(captions, Does.Contain("Large"));
            Assert.That(captions, Does.Contain("Extra large"));
            Assert.That(captions, Does.Contain("Custom"));
            Assert.That(captions, Does.Contain("Static backdrop"));
            Assert.That(captions, Does.Contain("No backdrop"));
            Assert.That(captions, Does.Contain("Framework dismiss"));
            Assert.That(captions, Does.Contain("Caller Close diagnostic"));
            Assert.That(captions, Does.Contain("Dynamic content"));
            Assert.That(captions, Does.Contain("Parameterless owner"));
            Assert.That(captions, Does.Contain("Monitor edge"));
            Assert.That(captions, Does.Contain("RTL"));
        }));
    }

    private static System.Collections.Generic.IEnumerable<Button> FindButtons(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Button button) yield return button;
            foreach (var nested in FindButtons(child)) yield return nested;
        }
    }
}
