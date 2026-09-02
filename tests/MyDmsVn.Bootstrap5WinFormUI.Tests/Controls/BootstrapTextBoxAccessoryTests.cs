using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapTextBoxAccessoryTests
{
    [Test]
    public void AccessoryOccupiesFarRightAndReducesEditorWidth()
    {
        using var input = new TestTextBox { Size = new Size(240, 40) };
        input.PerformLayout();
        var originalWidth = input.EditorBounds.Width;
        var accessory = new Button();

        input.SetFrameworkTrailingAccessory(accessory);
        input.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(accessory.Parent, Is.SameAs(input));
            Assert.That(accessory.TabStop, Is.False);
            Assert.That(accessory.Right, Is.GreaterThan(input.EditorBounds.Right));
            Assert.That(input.EditorBounds.Width, Is.LessThan(originalWidth));
        }));
    }

    [Test]
    public void AccessoryCoexistsWithClearAndTrailingIconWithoutOverlap()
    {
        using var input = new TestTextBox { Size = new Size(280, 40), ShowClearButton = true, Text = "value" };
        var accessory = new Button();
        input.SetFrameworkTrailingAccessory(accessory);
        input.PerformLayout();
        var clear = input.Controls.OfType<Button>().Single(control => !ReferenceEquals(control, accessory));
        Assert.That(clear.Right, Is.LessThanOrEqualTo(accessory.Left));

        input.ShowClearButton = false;
        input.TrailingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);
        input.PerformLayout();
        Assert.That(input.EditorBounds.Right, Is.LessThan(accessory.Left));
    }

    [Test]
    public void ReplacingAndRemovingAccessoryDisposesOwnedControls()
    {
        using var input = new TestTextBox();
        var first = new Button();
        var second = new Button();
        input.SetFrameworkTrailingAccessory(first);
        input.SetFrameworkTrailingAccessory(second);
        Assert.That(first.IsDisposed, Is.True);
        input.SetFrameworkTrailingAccessory(null);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(input.Controls.Cast<Control>(), Has.None.SameAs(second));
            Assert.That(input.Controls, Has.Count.EqualTo(3));
        }));
    }

    private sealed class TestTextBox : BootstrapTextBox
    {
        internal Rectangle EditorBounds => Editor.Bounds;
    }
}
