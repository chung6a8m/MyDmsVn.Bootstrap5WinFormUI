using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTextBoxTests
{
    [Test]
    public void DefaultsMatchPhase8Contract()
    {
        using var input = new BootstrapTextBox();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.PlaceholderText, Is.EqualTo(string.Empty));
            Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(input.Icon, Is.Null);
            Assert.That(input.TrailingIcon, Is.Null);
            Assert.That(input.ShowClearButton, Is.False);
            Assert.That(input.ReadOnly, Is.False);
            Assert.That(input.UseSystemPasswordChar, Is.False);
            Assert.That(input.BorderRadius, Is.EqualTo(-1));
            Assert.That(input.TabStop, Is.True);
        }));
    }

    [Test]
    public void ComposesNativeTextBoxAndForwardsCoreTextSemantics()
    {
        using var input = new BootstrapTextBox();
        var native = input.Controls.OfType<TextBox>().Single();

        input.Text = "secret";
        input.ReadOnly = true;
        input.UseSystemPasswordChar = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(native.Text, Is.EqualTo("secret"));
            Assert.That(native.ReadOnly, Is.True);
            Assert.That(native.UseSystemPasswordChar, Is.True);
            Assert.That(native.TabStop, Is.False, "The BootstrapTextBox owns the single tab stop and forwards focus to the native editor.");
        }));
    }

    [Test]
    public void PlaceholderIsVisibleOnlyWhileTextIsEmpty()
    {
        using var input = new BootstrapTextBox { PlaceholderText = "Email address" };
        var placeholder = input.Controls.OfType<Label>().Single();

        Assert.That(placeholder.Visible, Is.True);

        input.Text = "developer@example.com";
        Assert.That(placeholder.Visible, Is.False);

        input.Clear();
        Assert.That(placeholder.Visible, Is.True);
    }

    [Test]
    public void ClearButtonClearsThroughTheNormalTextChangedPath()
    {
        using var input = new BootstrapTextBox
        {
            Text = "query",
            ShowClearButton = true
        };
        var changed = 0;
        input.TextChanged += (_, _) => changed++;
        input.PerformLayout();
        var clearButton = input.Controls.OfType<Button>().Single();

        Assert.That(clearButton.Visible, Is.True);
        clearButton.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Text, Is.EqualTo(string.Empty));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(clearButton.Visible, Is.False);
        }));
    }

    [Test]
    public void LeadingAndTrailingIconsReserveEditorSpace()
    {
        using var input = new BootstrapTextBox { Size = new Size(260, 32) };
        var native = input.Controls.OfType<TextBox>().Single();
        input.PerformLayout();
        var noIcons = native.Bounds;

        input.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        input.TrailingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);
        input.PerformLayout();
        var withIcons = native.Bounds;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(withIcons.Left, Is.GreaterThan(noIcons.Left));
            Assert.That(withIcons.Right, Is.LessThan(noIcons.Right));
        }));
    }

    [Test]
    public void ValidationAndFocusResolveFromThemeTokens()
    {
        var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(colors, BootstrapValidationState.None, containsFocus: false, enabled: true),
                Is.EqualTo(colors.Border));
            Assert.That(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(colors, BootstrapValidationState.None, containsFocus: true, enabled: true),
                Is.EqualTo(colors.Focus));
            Assert.That(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(colors, BootstrapValidationState.Valid, containsFocus: true, enabled: true),
                Is.EqualTo(colors.Success));
            Assert.That(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: true),
                Is.EqualTo(colors.Danger));
            Assert.That(
                BootstrapTextBoxRenderLogic.ResolveBorderColor(colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: false),
                Is.EqualTo(colors.Disabled));
        }));
    }

    [Test]
    public void BorderRadiusRejectsValuesBelowThemeSentinel()
    {
        using var input = new BootstrapTextBox();

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => input.BorderRadius = -2));
        Assert.DoesNotThrow((Action)(() => input.BorderRadius = -1));
        Assert.DoesNotThrow((Action)(() => input.BorderRadius = 0));
    }
}
