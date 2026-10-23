using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDecimalInputInteractionTests
{
    private const int WmChar = 0x0102;
    private const int WmKeyDown = 0x0100;
    private const int WmPaste = 0x0302;
    private const int WmCopy = 0x0301;
    private const int WmCut = 0x0300;
    private const int WmImeStartComposition = 0x010D;
    private const int WmImeEndComposition = 0x010E;

    [Test]
    public void NativeFirstKeysAndMiddleReplacementRetainCandidateDisplayCaretAndFocus()
    {
        using var host = CreateHost(out var input);
        foreach (var ch in "1234") SendMessage(input.NativeEditor.Handle, WmChar, (IntPtr)ch, IntPtr.Zero);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.PendingText, Is.EqualTo("1234"));
            Assert.That(input.Text, Is.EqualTo("1,234"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(5));
            Assert.That(input.NativeEditor.Focused, Is.True);
            Assert.That(input.Value, Is.Null);
        }));

        input.NativeEditor.Select(2, 2);
        SendMessage(input.NativeEditor.Handle, WmChar, (IntPtr)'9', IntPtr.Zero);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.PendingText, Is.EqualTo("194"));
            Assert.That(input.Text, Is.EqualTo("194"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(2));
            Assert.That(input.NativeEditor.SelectionLength, Is.Zero);
            Assert.That(input.NativeEditor.Focused, Is.True);
        }));
    }

    [Test]
    public void GeneratedSeparatorDeletionTargetsAdjacentCandidateDigitAndIsUndoable()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(1234m);
        input.NativeEditor.Select(2, 0);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Back, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("234"));
        Assert.That(input.Value, Is.EqualTo(1234m));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.PendingText, Is.EqualTo("1234"));
        Assert.That(input.Text, Is.EqualTo("1,234"));

        input.NativeEditor.Select(1, 0);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Delete, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("134"));
    }

    [Test]
    public void IdenticalDisplayExternalGroupedPastePreservesRejectedSourceCandidate()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(1234m);
        var pendingEvents = 0;
        var displayEvents = 0;
        input.PendingTextChanged += (_, _) => pendingEvents++;
        input.TextChanged += (_, _) => displayEvents++;
        WithClipboardText("1,234", () =>
        {
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmPaste, IntPtr.Zero, IntPtr.Zero);
        });
        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Text, Is.EqualTo("1,234"));
            Assert.That(input.PendingText, Is.EqualTo("1,234"));
            Assert.That(input.TryParsePending().State, Is.EqualTo(InputParseState.Invalid));
            Assert.That(input.Value, Is.EqualTo(1234m));
            Assert.That(pendingEvents, Is.EqualTo(1));
            Assert.That(displayEvents, Is.Zero);
        }));
    }

    [Test]
    public void ClipboardCopyAndCutUsePendingLexicalSelectionIncludingInvalidCharacters()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(1234m);
        WithClipboardText("initial", () =>
        {
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmCopy, IntPtr.Zero, IntPtr.Zero);
            Assert.That(Clipboard.GetText(), Is.EqualTo("1234"));
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmCut, IntPtr.Zero, IntPtr.Zero);
            Assert.That(Clipboard.GetText(), Is.EqualTo("1234"));
            Assert.That(input.PendingText, Is.Empty);

            input.NativeEditor.SelectedText = "12x4";
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmCopy, IntPtr.Zero, IntPtr.Zero);
            Assert.That(Clipboard.GetText(), Is.EqualTo("12x4"));
            SendMessage(input.NativeEditor.Handle, WmCut, IntPtr.Zero, IntPtr.Zero);
            Assert.That(input.PendingText, Is.Empty);
            Assert.That(input.Value, Is.EqualTo(1234m));
        });
    }

    [Test]
    public void InvalidPasteAndUndoRedoRetainExactCandidateAndAcceptedValue()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(12m);
        var pendingEvents = 0;
        input.PendingTextChanged += (_, _) => pendingEvents++;
        WithClipboardText("1e2", () =>
        {
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmPaste, IntPtr.Zero, IntPtr.Zero);
        });
        Assert.That(input.PendingText, Is.EqualTo("1e2"));
        Assert.That(input.TryParsePending().State, Is.EqualTo(InputParseState.Invalid));
        Assert.That(input.Value, Is.EqualTo(12m));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.PendingText, Is.EqualTo("12"));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Y));
        Assert.That(input.PendingText, Is.EqualTo("1e2"));
        Assert.That(pendingEvents, Is.EqualTo(3), "Paste, undo and redo each publish one lexical change.");
    }

    [Test]
    public void ImeCompositionDefersProjectionUntilCompositionEnds()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(1234m);
        var pendingEvents = 0;
        input.PendingTextChanged += (_, _) => pendingEvents++;
        SendMessage(input.NativeEditor.Handle, WmImeStartComposition, IntPtr.Zero, IntPtr.Zero);
        input.NativeEditor.Select(input.NativeEditor.TextLength, 0);
        input.NativeEditor.SelectedText = "あ";
        Assert.That(input.Text, Is.EqualTo("1234あ"));
        Assert.That(input.PendingText, Is.EqualTo("1234あ"));
        SendMessage(input.NativeEditor.Handle, WmImeEndComposition, IntPtr.Zero, IntPtr.Zero);
        Assert.That(input.TryParsePending().State, Is.EqualTo(InputParseState.Invalid));
        Assert.That(pendingEvents, Is.EqualTo(1));
        input.RaiseEditorKeyDown(new KeyEventArgs(Keys.Control | Keys.Z));
        Assert.That(input.PendingText, Is.EqualTo("1234"));
        Assert.That(pendingEvents, Is.EqualTo(2));
    }

    [Test]
    public void ReadOnlyAndDisabledNativePasteCutDoNotMutatePendingOrModel()
    {
        using var host = CreateHost(out var input);
        input.BeginEditSession(1234m);
        WithClipboardText("9", () =>
        {
            input.ReadOnly = true;
            input.NativeEditor.SelectAll();
            SendMessage(input.NativeEditor.Handle, WmPaste, IntPtr.Zero, IntPtr.Zero);
            SendMessage(input.NativeEditor.Handle, WmCut, IntPtr.Zero, IntPtr.Zero);
            Assert.That(input.PendingText, Is.EqualTo("1234"));
            input.ReadOnly = false;
            input.Enabled = false;
            SendMessage(input.NativeEditor.Handle, WmPaste, IntPtr.Zero, IntPtr.Zero);
            Assert.That(input.PendingText, Is.EqualTo("1234"));
            Assert.That(input.Value, Is.EqualTo(1234m));
        });
    }

    [Test]
    public void ThemeSwitchPreservesPendingCaretSelectionAndFocus()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var host = CreateHost(out var input);
            input.BeginEditSession(1234m);
            input.NativeEditor.Select(1, 3);
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Application.DoEvents();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(input.PendingText, Is.EqualTo("1234"));
                Assert.That(input.Text, Is.EqualTo("1,234"));
                Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(1));
                Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(3));
                Assert.That(input.NativeEditor.Focused, Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static Form CreateHost(out DecimalProbe input)
    {
        var host = new Form { ShowInTaskbar = false, Width = 360, Height = 120 };
        input = new DecimalProbe { Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), false, 4, 0m, 99999999999999.9999m) };
        input.SetBounds(20, 20, 240, 32);
        host.Controls.Add(input);
        host.Show();
        input.Focus();
        Application.DoEvents();
        return host;
    }

    private static void WithClipboardText(string text, Action action)
    {
        var previous = Clipboard.GetDataObject();
        try
        {
            Clipboard.SetText(text);
            action();
        }
        finally
        {
            if (previous is not null) Clipboard.SetDataObject(previous, true);
            else Clipboard.Clear();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private sealed class DecimalProbe : BootstrapDecimalInput
    {
        internal TextBox NativeEditor => Editor;
        internal void RaiseEditorKeyDown(KeyEventArgs e) => OnEditorKeyDown(e);
    }
}
