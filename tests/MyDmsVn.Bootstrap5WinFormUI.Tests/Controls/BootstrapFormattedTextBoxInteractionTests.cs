using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapFormattedTextBoxInteractionTests
{
    [TestCase(BootstrapInputFormatMode.General, "12345678", 2, "9", "12934567")]
    [TestCase(BootstrapInputFormatMode.Numeral, "1234567.89", 2, "9", "19234567.89")]
    [TestCase(BootstrapInputFormatMode.CreditCard, "4111111111111111", 2, "9", "4191111111111111")]
    public void MiddleInsertionKeepsCaretAdjacentToInsertedRawCharacter(BootstrapInputFormatMode mode, string raw, int displayPosition, string inserted, string expectedRaw)
    {
        using var input = CreateProbe(mode);
        input.RawValue = raw;
        input.NativeEditor.Select(displayPosition, 0);

        input.NativeEditor.SelectedText = inserted;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo(expectedRaw));
            Assert.That(input.NativeEditor.SelectionStart, Is.LessThan(input.Text.Length));
            Assert.That(input.NativeEditor.SelectionStart, Is.GreaterThan(0));
            Assert.That(input.Text[input.NativeEditor.SelectionStart - 1], Is.EqualTo(inserted[0]));
        }));
    }

    [Test]
    public void SelectionReplacementAcrossDelimiterFormatsOnce()
    {
        using var input = CreateProbe(BootstrapInputFormatMode.General);
        input.RawValue = "12345678";
        var textEvents = 0;
        var rawEvents = 0;
        input.TextChanged += (_, _) => textEvents++;
        input.RawValueChanged += (_, _) => rawEvents++;
        input.NativeEditor.Select(3, 3);

        input.NativeEditor.SelectedText = "AB9-";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("123AB967"));
            Assert.That(input.Text, Is.EqualTo("123A-B967"));
            Assert.That(textEvents, Is.EqualTo(1));
            Assert.That(rawEvents, Is.EqualTo(1));
            Assert.That(input.NativeEditor.SelectionStart, Is.InRange(0, input.Text.Length));
        }));
    }

    [TestCase(BootstrapInputFormatMode.Date)]
    [TestCase(BootstrapInputFormatMode.Time)]
    public void PasteLikeEditIgnoresUnicodeDecimalDigits(BootstrapInputFormatMode mode)
    {
        using var input = CreateProbe(mode);

        input.NativeEditor.SelectedText = "３1";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("1"));
            Assert.That(input.Text, Is.EqualTo("1"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(1));
        }));
    }

    [Test]
    public void InsertionInsideGeneralPrefixMapsToRawStartWithoutPromotingDecoration()
    {
        using var input = CreatePrefixedGeneralProbe();
        var textChanges = 0;
        var rawChanges = 0;
        input.TextChanged += (_, _) => textChanges++;
        input.RawValueChanged += (_, _) => rawChanges++;
        input.NativeEditor.Select(1, 0);

        input.NativeEditor.SelectedText = "X";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("X1234567"));
            Assert.That(input.Text, Is.EqualTo("VNX123-4567"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
            Assert.That(textChanges, Is.EqualTo(1));
            Assert.That(rawChanges, Is.EqualTo(1));
        }));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void DeletingGeneralPrefixDecorationRestoresDisplayWithoutLogicalChange(int prefixPosition)
    {
        using var input = CreatePrefixedGeneralProbe();
        var textChanges = 0;
        var rawChanges = 0;
        input.TextChanged += (_, _) => textChanges++;
        input.RawValueChanged += (_, _) => rawChanges++;
        input.NativeEditor.Select(prefixPosition, 1);

        input.NativeEditor.SelectedText = string.Empty;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("12345678"));
            Assert.That(input.Text, Is.EqualTo("VN1234-5678"));
            Assert.That(textChanges, Is.Zero);
            Assert.That(rawChanges, Is.Zero);
        }));
    }

    [Test]
    public void ReplacementAcrossGeneralPrefixBoundaryUsesRawRangeAndRemainsUndoable()
    {
        using var input = CreatePrefixedGeneralProbe();
        input.NativeEditor.Select(1, 3);

        input.NativeEditor.SelectedText = "AB";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("AB345678"));
            Assert.That(input.Text, Is.EqualTo("VNAB34-5678"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(4));
        }));

        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("12345678"));
            Assert.That(input.Text, Is.EqualTo("VN1234-5678"));
        }));
    }

    [Test]
    public void BackspaceAndDeleteAdjacentToFormattingDelimiterDeleteRawCharacters()
    {
        using var input = CreateProbe(BootstrapInputFormatMode.General);
        input.RawValue = "12345678";
        input.NativeEditor.Select(5, 0);
        var backspace = new KeyEventArgs(Keys.Back);
        input.RaiseEditorKeyDown(backspace);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("1235678"));
            Assert.That(input.Text, Is.EqualTo("1235-678"));
            Assert.That(backspace.Handled, Is.True);
            Assert.That(backspace.SuppressKeyPress, Is.True);
        }));

        input.RawValue = "12345678";
        input.NativeEditor.Select(4, 0);
        var delete = new KeyEventArgs(Keys.Delete);
        input.RaiseEditorKeyDown(delete);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.RawValue, Is.EqualTo("1234678"));
            Assert.That(input.Text, Is.EqualTo("1234-678"));
            Assert.That(delete.Handled, Is.True);
        }));
    }

    [Test]
    public void FormattedHistorySupportsUndoRedoAndClearsRedoAfterBranchEdit()
    {
        using var input = CreateProbe(BootstrapInputFormatMode.General);
        InsertAtEnd(input, "1");
        InsertAtEnd(input, "2");
        InsertAtEnd(input, "3");

        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.RawValue, Is.EqualTo("12"));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.RawValue, Is.EqualTo("1"));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Y));
        Assert.That(input.RawValue, Is.EqualTo("12"));

        InsertAtEnd(input, "9");
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Y));
        Assert.That(input.RawValue, Is.EqualTo("129"));
    }

    [Test]
    public void AltAndNativeClipboardShortcutsAreNotConsumedByFormattingCode()
    {
        using var input = CreateProbe(BootstrapInputFormatMode.General);
        input.RawValue = "1234";
        var original = input.RawValue;

        foreach (var keys in new[] { Keys.Alt | Keys.Menu, Keys.Control | Keys.A, Keys.Control | Keys.C, Keys.Control | Keys.X, Keys.Control | Keys.V })
        {
            var args = new KeyEventArgs(keys);
            input.RaiseEditorKeyDown(args);
            Assert.That(args.Handled || args.SuppressKeyPress, Is.False, keys.ToString());
        }

        Assert.That(input.RawValue, Is.EqualTo(original));
    }

    [Test]
    public void CompositeRemainsOneTabStopAndCanBeLeftInBothDirections()
    {
        using var form = new Form { ShowInTaskbar = false, Width = 360, Height = 180 };
        var before = new Button { Left = 20, Top = 20, TabIndex = 0 };
        var input = CreateProbe(BootstrapInputFormatMode.General);
        input.SetBounds(20, 55, 180, 32);
        input.TabIndex = 1;
        var after = new Button { Left = 20, Top = 100, TabIndex = 2 };
        form.Controls.AddRange(new Control[] { before, input, after });
        form.Show();
        Application.DoEvents();

        before.Focus();
        Assert.That(form.SelectNextControl(before, true, true, true, true), Is.True);
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.NativeEditor.Focused, Is.True);
            Assert.That(input.TabStop, Is.True);
            Assert.That(input.NativeEditor.TabStop, Is.False);
        }));

        Assert.That(form.SelectNextControl(input, true, true, true, true), Is.True);
        Application.DoEvents();
        Assert.That(after.Focused, Is.True);
        Assert.That(form.SelectNextControl(after, false, true, true, true), Is.True);
        Application.DoEvents();
        Assert.That(input.NativeEditor.Focused, Is.True);
    }

    private static FormattedProbe CreateProbe(BootstrapInputFormatMode mode)
    {
        var input = new FormattedProbe { FormatMode = mode };
        input.GeneralOptions.Blocks = new[] { 4, 4 };
        input.GeneralOptions.Delimiter = "-";
        return input;
    }

    private static FormattedProbe CreatePrefixedGeneralProbe()
    {
        var input = CreateProbe(BootstrapInputFormatMode.General);
        input.GeneralOptions.Prefix = "VN";
        input.RawValue = "12345678";
        return input;
    }

    private static void InsertAtEnd(FormattedProbe input, string value)
    {
        input.NativeEditor.Select(input.NativeEditor.TextLength, 0);
        input.NativeEditor.SelectedText = value;
    }

    private sealed class FormattedProbe : BootstrapFormattedTextBox
    {
        public TextBox NativeEditor => Editor;

        public void RaiseEditorKeyDown(KeyEventArgs e) => OnEditorKeyDown(e);
    }
}
