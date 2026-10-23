using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDecimalInputTests
{
    [Test]
    public void BeginAndAcceptedProjectionKeepValueSeparateFromPendingEdits()
    {
        using var input = new DecimalProbe();
        input.Profile = Profile("en-US", false);
        input.BeginEditSession(1234m);
        var changes = 0;
        input.PendingTextChanged += (_, _) => changes++;
        input.NativeEditor.SelectAll();
        input.NativeEditor.SelectedText = "12x";
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.PendingText, Is.EqualTo("12x"));
            Assert.That(input.TryParsePending().State, Is.EqualTo(InputParseState.Invalid));
            Assert.That(input.Value, Is.EqualTo(1234m));
            Assert.That(changes, Is.EqualTo(1));
        }));

        input.ProjectAcceptedValue(56m);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.PendingText, Is.EqualTo("56"));
            Assert.That(input.Value, Is.EqualTo(56m));
            Assert.That(changes, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ProfileIsFrozenDuringPendingEditAndAtomicNewSessionResetsIt()
    {
        using var input = new DecimalProbe();
        var en = Profile("en-US", false);
        var vi = Profile("vi-VN", true);
        input.BeginEditSession(1234m, en);
        input.NativeEditor.SelectAll();
        input.NativeEditor.SelectedText = "1.2";
        Assert.Throws<InvalidOperationException>((Action)(() => input.Profile = vi));
        Assert.That(input.TryParsePending().Value, Is.EqualTo(1.2m));

        input.BeginEditSession(null, vi);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Profile, Is.SameAs(vi));
            Assert.That(input.PendingText, Is.Empty);
            Assert.That(input.Value, Is.Null);
            Assert.That(input.TryParsePending().State, Is.EqualTo(InputParseState.Valid));
        }));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.PendingText, Is.Empty, "The previous cell's undo history must be gone.");
    }

    [Test]
    public void ReadOnlyAndDisabledPreserveNativeSelectionAndAcceptedValue()
    {
        using var input = new DecimalProbe();
        input.BeginEditSession(1234m, Profile("en-US", false));
        input.ReadOnly = true;
        input.NativeEditor.Select(0, input.NativeEditor.TextLength);
        Assert.That(input.NativeEditor.SelectedText, Is.EqualTo("1,234"));
        Assert.That(input.NativeEditor.ReadOnly, Is.True);
        input.Enabled = false;
        Assert.That(input.NativeEditor.Enabled, Is.False);
        Assert.That(input.Value, Is.EqualTo(1234m));
        Assert.That(input.PendingText, Is.EqualTo("1234"));
    }

    [Test]
    public void GroupingPreservesCultureAndExactValueBounds()
    {
        using var input = new DecimalProbe();
        input.BeginEditSession(1234.5m, Profile("vi-VN", false));
        Assert.That(input.Text, Is.EqualTo("1.234,5"));
        Assert.That(input.PendingText, Is.EqualTo("1234,5"));
        Assert.That(input.TryParsePending().Value, Is.EqualTo(1234.5m));
    }

    [Test]
    public void SessionProjectionOfTinyExactDecimalNeverUsesExponentSyntax()
    {
        using var input = new DecimalProbe();
        var tiny = 0.0000000000000000000000000001m;
        input.BeginEditSession(tiny, new DecimalInputProfile(
            CultureInfo.GetCultureInfo("en-US"), false, 28, 0m, 1m));
        Assert.That(input.PendingText, Is.EqualTo("0.0000000000000000000000000001"));
        Assert.That(input.TryParsePending().Value, Is.EqualTo(tiny));
    }

    private static DecimalInputProfile Profile(string culture, bool nullable) =>
        new DecimalInputProfile(CultureInfo.GetCultureInfo(culture), nullable, 4, 0m, 99999999999999.9999m);

    private sealed class DecimalProbe : BootstrapDecimalInput
    {
        internal TextBox NativeEditor => Editor;
        internal void RaiseEditorKeyDown(KeyEventArgs e) => OnEditorKeyDown(e);
    }
}
