using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Native decimal entry with a lossless pending candidate and a separate accepted value.</summary>
[DefaultProperty(nameof(Profile))]
[DefaultEvent(nameof(TextChanged))]
public class BootstrapDecimalInput : BootstrapTextBox
{
    private const int WmCopy = 0x0301;
    private const int WmCut = 0x0300;
    private const int WmPaste = 0x0302;
    private const int WmUndo = 0x0304;
    private const int WmImeStartComposition = 0x010D;
    private const int WmImeEndComposition = 0x010E;
    private const int WmChar = 0x0102;
    private const int WmKeyDown = 0x0100;

    private readonly FormattedTextHistory _history = new FormattedTextHistory();
    private readonly DecimalEditorWindow _nativeWindow;
    private DecimalInputProfile _profile = new DecimalInputProfile(CultureInfo.CurrentCulture, true, 4, decimal.MinValue, decimal.MaxValue);
    private DecimalInputProfile? _activeProfile;
    private DecimalProjectionResult? _projection;
    private string _pendingText = string.Empty;
    private decimal? _value;
    private bool _applyingDisplay;
    private bool _composing;
    private bool _pendingEdit;
    private FormattedTextSnapshot? _compositionStart;
    private NativeEditHint? _nativeEditHint;

    /// <summary>Creates a decimal input around the themed native editor.</summary>
    public BootstrapDecimalInput()
    {
        _nativeWindow = new DecimalEditorWindow(this);
        Editor.HandleCreated += HandleEditorHandleCreated;
        Editor.HandleDestroyed += HandleEditorHandleDestroyed;
        if (Editor.IsHandleCreated) _nativeWindow.AssignHandle(Editor.Handle);
        _projection = DecimalInputProjection.Project(string.Empty, _profile);
    }

    /// <summary>The profile used for the next session; a pending edit cannot be reinterpreted.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DecimalInputProfile Profile
    {
        get => _profile;
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (_pendingEdit) throw new InvalidOperationException("A pending decimal edit cannot change profile.");
            _profile = value;
            if (_activeProfile is null) ApplyCandidate(_pendingText, _pendingText.Length, 0, false, null);
        }
    }

    /// <summary>The exact lexical candidate, without control-generated separators.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PendingText => _pendingText;

    /// <summary>The last model value explicitly accepted by the host.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal? Value => _value;

    /// <summary>Occurs once when the pending lexical candidate changes through native editing.</summary>
    public event EventHandler? PendingTextChanged;

    /// <summary>Starts a fresh admitted edit session with the configured profile.</summary>
    public void BeginEditSession(decimal? currentValue) => BeginEditSession(currentValue, _profile);

    /// <summary>Starts a fresh admitted edit session and atomically selects its profile.</summary>
    public void BeginEditSession(decimal? currentValue, DecimalInputProfile profile)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        _profile = profile;
        _activeProfile = profile;
        _composing = false;
        _compositionStart = null;
        _pendingEdit = false;
        _history.Clear();
        _value = currentValue;
        var candidate = FormatValue(currentValue, profile);
        ApplyCandidate(candidate, candidate.Length, 0, false, null);
    }

    /// <summary>Projects an explicitly accepted model value without recording an edit.</summary>
    public void ProjectAcceptedValue(decimal? value)
    {
        _value = value;
        _pendingEdit = false;
        _composing = false;
        _compositionStart = null;
        _history.Clear();
        var candidate = FormatValue(value, ActiveProfile);
        ApplyCandidate(candidate, candidate.Length, 0, false, null);
    }

    /// <summary>Parses the pending lexical candidate without modifying the accepted value.</summary>
    public DecimalParseResult TryParsePending() => DecimalInputParser.TryParsePending(_pendingText, ActiveProfile);

    /// <inheritdoc />
    protected override void OnEditorTextChanged(EventArgs e)
    {
        if (_applyingDisplay) return;
        if (_composing)
        {
            var compositionChanged = !string.Equals(_pendingText, Editor.Text, StringComparison.Ordinal);
            _pendingText = Editor.Text;
            _projection = DecimalInputProjection.Project(_pendingText, ActiveProfile);
            if (compositionChanged)
            {
                _pendingEdit = true;
                PendingTextChanged?.Invoke(this, EventArgs.Empty);
            }
            base.OnEditorTextChanged(e);
            return;
        }

        var prior = CurrentProjection;
        var changed = _nativeEditHint.HasValue && TryHintChange(prior.DisplayText, Editor.Text,
                _nativeEditHint.Value, out var hinted)
            ? hinted
            : FindChange(prior.DisplayText, Editor.Text);
        var start = prior.ToCandidateBoundary(changed.OldStart);
        var end = prior.ToCandidateBoundary(changed.OldEnd);
        var insertion = Editor.Text.Substring(changed.NewStart, changed.NewEnd - changed.NewStart);
        var next = _pendingText.Remove(start, end - start).Insert(start, insertion);
        ApplyCandidate(next, start + insertion.Length, 0, true,
            new FormattedTextSnapshot(_pendingText, start, end - start), true);
    }

    /// <inheritdoc />
    protected override void OnEditorKeyDown(KeyEventArgs e)
    {
        base.OnEditorKeyDown(e);
        if (e.Handled || e.SuppressKeyPress || _composing || !Enabled) return;
        if (e.Control && !e.Alt && !e.Shift && (e.KeyCode == Keys.Z || e.KeyCode == Keys.Y))
        {
            if (!ReadOnly) RestoreHistory(e.KeyCode == Keys.Z);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        if (ReadOnly || e.Modifiers != Keys.None || Editor.SelectionLength != 0) return;
        var projection = CurrentProjection;
        var caret = Editor.SelectionStart;
        var isBack = e.KeyCode == Keys.Back;
        var isDelete = e.KeyCode == Keys.Delete;
        if (!isBack && !isDelete) return;
        var adjacent = isBack ? caret - 1 : caret;
        if (adjacent < 0 || adjacent >= projection.DisplayText.Length || !projection.IsGeneratedCharacter(adjacent)) return;
        var boundary = projection.ToCandidateBoundary(caret);
        var removeAt = isBack ? boundary - 1 : boundary;
        if (removeAt < 0 || removeAt >= _pendingText.Length) return;
        var snapshot = new FormattedTextSnapshot(_pendingText, boundary, 0);
        ApplyCandidate(_pendingText.Remove(removeAt, 1), removeAt, 0, true, snapshot, true);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Editor.HandleCreated -= HandleEditorHandleCreated;
            Editor.HandleDestroyed -= HandleEditorHandleDestroyed;
            if (_nativeWindow.Handle != IntPtr.Zero) _nativeWindow.ReleaseHandle();
            _history.Clear();
        }
        base.Dispose(disposing);
    }

    private DecimalInputProfile ActiveProfile => _activeProfile ?? _profile;
    private DecimalProjectionResult CurrentProjection => _projection ?? DecimalInputProjection.Project(_pendingText, ActiveProfile);

    private static string FormatValue(decimal? value, DecimalInputProfile profile) =>
        value.HasValue ? value.Value.ToString("G29", profile.Culture) : string.Empty;

    private void ApplyCandidate(string candidate, int selectionStart, int selectionLength, bool recordHistory,
        FormattedTextSnapshot? previous, bool notifyPending = false)
    {
        var priorDisplay = CurrentProjection.DisplayText;
        var changed = !string.Equals(_pendingText, candidate, StringComparison.Ordinal);
        if (recordHistory && changed) _history.Record(previous ?? CurrentSnapshot());
        _pendingText = candidate;
        if (notifyPending && changed) _pendingEdit = true;
        var projection = DecimalInputProjection.Project(candidate, ActiveProfile);
        _projection = projection;
        var boundedStart = Math.Max(0, Math.Min(candidate.Length, selectionStart));
        var boundedEnd = Math.Max(boundedStart, Math.Min(candidate.Length, selectionStart + selectionLength));
        if (!string.Equals(Editor.Text, projection.DisplayText, StringComparison.Ordinal))
        {
            _applyingDisplay = true;
            try { Editor.Text = projection.DisplayText; }
            finally { _applyingDisplay = false; }
        }
        var displayStart = projection.ToDisplayBoundary(boundedStart);
        var displayEnd = projection.ToDisplayBoundary(boundedEnd);
        Editor.Select(displayStart, displayEnd - displayStart);
        if (changed && notifyPending) PendingTextChanged?.Invoke(this, EventArgs.Empty);
        if (!string.Equals(priorDisplay, projection.DisplayText, StringComparison.Ordinal))
            base.OnEditorTextChanged(EventArgs.Empty);
    }

    private FormattedTextSnapshot CurrentSnapshot()
    {
        var projection = CurrentProjection;
        var start = projection.ToCandidateBoundary(Editor.SelectionStart);
        var end = projection.ToCandidateBoundary(Editor.SelectionStart + Editor.SelectionLength);
        return new FormattedTextSnapshot(_pendingText, start, end - start);
    }

    private void RestoreHistory(bool undo)
    {
        var current = CurrentSnapshot();
        FormattedTextSnapshot previous;
        var restored = undo ? _history.TryUndo(current, out previous) : _history.TryRedo(current, out previous);
        if (restored) ApplyCandidate(previous.RawValue, previous.RawSelectionStart, previous.RawSelectionLength, false, null, true);
    }

    private void HandleEditorHandleCreated(object? sender, EventArgs e)
    {
        if (_nativeWindow.Handle == IntPtr.Zero) _nativeWindow.AssignHandle(Editor.Handle);
    }

    private void HandleEditorHandleDestroyed(object? sender, EventArgs e)
    {
        if (_nativeWindow.Handle != IntPtr.Zero) _nativeWindow.ReleaseHandle();
    }

    private void ProcessNativeMessage(ref Message message, ActionRef dispatch)
    {
        if (!_composing && (message.Msg == WmChar ||
            message.Msg == WmKeyDown && message.WParam.ToInt32() == (int)Keys.Delete))
        {
            var priorHint = _nativeEditHint;
            _nativeEditHint = new NativeEditHint(Editor.SelectionStart, Editor.SelectionLength);
            try { dispatch(ref message); }
            finally { _nativeEditHint = priorHint; }
            return;
        }
        if (message.Msg == WmImeStartComposition)
        {
            var snapshot = CurrentSnapshot();
            _compositionStart = snapshot;
            _composing = true;
            _applyingDisplay = true;
            try { Editor.Text = _pendingText; }
            finally { _applyingDisplay = false; }
            Editor.Select(snapshot.RawSelectionStart, snapshot.RawSelectionLength);
            dispatch(ref message);
            return;
        }
        if (message.Msg == WmImeEndComposition)
        {
            dispatch(ref message);
            _composing = false;
            if (_compositionStart.HasValue &&
                !string.Equals(_compositionStart.Value.RawValue, _pendingText, StringComparison.Ordinal))
                _history.Record(_compositionStart.Value);
            _compositionStart = null;
            ApplyCandidate(_pendingText, Editor.SelectionStart, Editor.SelectionLength, false, null);
            return;
        }
        if (message.Msg == WmCopy || message.Msg == WmCut)
        {
            if (message.Msg == WmCut && (ReadOnly || !Enabled)) return;
            CopyOrCut(message.Msg == WmCut);
            return;
        }
        if (message.Msg == WmPaste)
        {
            if (ReadOnly || !Enabled) return;
            if (!Clipboard.ContainsText()) { dispatch(ref message); return; }
            ReplaceSelection(Clipboard.GetText(TextDataFormat.UnicodeText));
            return;
        }
        if (message.Msg == WmUndo)
        {
            if (!ReadOnly && Enabled) RestoreHistory(true);
            return;
        }
        dispatch(ref message);
    }

    private void CopyOrCut(bool cut)
    {
        var snapshot = CurrentSnapshot();
        if (snapshot.RawSelectionLength == 0) return;
        var selected = _pendingText.Substring(snapshot.RawSelectionStart, snapshot.RawSelectionLength);
        Clipboard.SetText(selected);
        if (cut)
        {
            var next = _pendingText.Remove(snapshot.RawSelectionStart, snapshot.RawSelectionLength);
            ApplyCandidate(next, snapshot.RawSelectionStart, 0, true, snapshot, true);
        }
    }

    private void ReplaceSelection(string inserted)
    {
        var snapshot = CurrentSnapshot();
        var next = _pendingText.Remove(snapshot.RawSelectionStart, snapshot.RawSelectionLength)
            .Insert(snapshot.RawSelectionStart, inserted);
        ApplyCandidate(next, snapshot.RawSelectionStart + inserted.Length, 0, true, snapshot, true);
    }

    private static TextChange FindChange(string oldText, string newText)
    {
        var prefix = 0;
        while (prefix < oldText.Length && prefix < newText.Length && oldText[prefix] == newText[prefix]) prefix++;
        var oldEnd = oldText.Length;
        var newEnd = newText.Length;
        while (oldEnd > prefix && newEnd > prefix && oldText[oldEnd - 1] == newText[newEnd - 1])
        {
            oldEnd--;
            newEnd--;
        }
        return new TextChange(prefix, oldEnd, prefix, newEnd);
    }

    private static bool TryHintChange(string oldText, string newText, NativeEditHint hint, out TextChange changed)
    {
        var insertionLength = newText.Length - oldText.Length + hint.DisplayLength;
        var oldEnd = hint.DisplayStart + hint.DisplayLength;
        if (hint.DisplayStart < 0 || oldEnd > oldText.Length || insertionLength < 0 ||
            hint.DisplayStart + insertionLength > newText.Length ||
            !newText.StartsWith(oldText.Substring(0, hint.DisplayStart), StringComparison.Ordinal) ||
            !newText.EndsWith(oldText.Substring(oldEnd), StringComparison.Ordinal))
        {
            changed = default;
            return false;
        }
        changed = new TextChange(hint.DisplayStart, oldEnd, hint.DisplayStart, hint.DisplayStart + insertionLength);
        return true;
    }

    private readonly struct NativeEditHint
    {
        internal NativeEditHint(int displayStart, int displayLength)
        {
            DisplayStart = displayStart;
            DisplayLength = displayLength;
        }
        internal int DisplayStart { get; }
        internal int DisplayLength { get; }
    }

    private readonly struct TextChange
    {
        internal TextChange(int oldStart, int oldEnd, int newStart, int newEnd)
        {
            OldStart = oldStart;
            OldEnd = oldEnd;
            NewStart = newStart;
            NewEnd = newEnd;
        }
        internal int OldStart { get; }
        internal int OldEnd { get; }
        internal int NewStart { get; }
        internal int NewEnd { get; }
    }

    private delegate void ActionRef(ref Message message);

    private sealed class DecimalEditorWindow : NativeWindow
    {
        private readonly BootstrapDecimalInput _owner;
        internal DecimalEditorWindow(BootstrapDecimalInput owner) => _owner = owner;
        protected override void WndProc(ref Message message) => _owner.ProcessNativeMessage(ref message, Dispatch);
        private void Dispatch(ref Message message) => base.WndProc(ref message);
    }
}
