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
public sealed class TextBoxCardDemoFormTests
{
    [Test]
    public void Phase8DemoExposesRequiredInputAndCardStates()
    {
        using var form = new TextBoxCardDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var inputs = FindControls<BootstrapTextBox>(form).ToArray();
        var cards = FindControls<BootstrapCard>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(inputs.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(inputs.Any(input => input.ValidationState == BootstrapValidationState.Valid), Is.True);
            Assert.That(inputs.Any(input => input.ValidationState == BootstrapValidationState.Invalid), Is.True);
            Assert.That(inputs.Any(input => input.ReadOnly), Is.True);
            Assert.That(inputs.Any(input => input.UseSystemPasswordChar), Is.True);
            Assert.That(inputs.Any(input => input.ShowClearButton), Is.True);
            Assert.That(inputs.Any(input => !input.Enabled), Is.True);
            Assert.That(inputs.Any(input => input.Icon is not null), Is.True);
            Assert.That(inputs.Any(input => input.TrailingIcon is not null), Is.True);

            Assert.That(cards.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(cards.Any(card => card.ShowShadow), Is.True);
            Assert.That(cards.Any(card => !card.ShowBorder), Is.True);
            Assert.That(cards.Any(card => card.BorderRadius >= 0), Is.True);
            Assert.That(cards.Any(card => card.Header.Controls.Count > 0 && card.Footer.Controls.Count > 0), Is.True);
        }));
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
