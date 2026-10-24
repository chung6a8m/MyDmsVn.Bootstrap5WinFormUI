using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Lossless native date text entry composed with the themed calendar picker.</summary>
[DefaultProperty(nameof(Profile))]
[DefaultEvent(nameof(PendingTextChanged))]
public class BootstrapDateInput : UserControl
{
    private readonly DateEntryTextBox _entry;
    private readonly BootstrapCalendarPicker _calendar;
    private readonly DateImeMonitor _imeMonitor;
    private DateInputProfile _profile = new DateInputProfile(false, DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
    private DateInputProfile? _activeProfile;
    private Func<DateTime> _referenceDateProvider = () => DateTime.Today;
    private DateTime _referenceDate;
    private DateTime? _value;
    private string _pendingText = string.Empty;
    private string? _popupSnapshot;
    private int _popupCaret;
    private bool _popupAccepted;
    private bool _applying;
    private bool _pendingEdit;
    private bool _synchronizingCalendar;
    private bool _imeComposing;

    /// <summary>Creates a designer-safe date input.</summary>
    public BootstrapDateInput()
    {
        _entry = new DateEntryTextBox(this) { Dock = DockStyle.Fill, AccessibleDescription = "Type day, day/month or day/month/four-digit-year." };
        _imeMonitor = new DateImeMonitor(this);
        _calendar = new BootstrapCalendarPicker { Dock = DockStyle.Right, Width = 38, TabStop = false, AccessibleName = "Open calendar" };
        var preferredHeight = Math.Max(_entry.Height, _calendar.Height);
        Controls.Add(_entry);
        Controls.Add(_calendar);
        _entry.TextChanged += OnEntryTextChanged;
        _entry.KeyDown += OnEntryKeyDown;
        _entry.NativeEditor.HandleCreated += OnEditorHandleCreated;
        _entry.NativeEditor.HandleDestroyed += OnEditorHandleDestroyed;
        if (_entry.NativeEditor.IsHandleCreated) _imeMonitor.AssignHandle(_entry.NativeEditor.Handle);
        _calendar.SelectionChanged += OnCalendarSelectionChanged;
        _calendar.SelectionAccepted += OnCalendarSelectionAccepted;
        _calendar.Opened += OnCalendarOpened;
        _calendar.Closed += OnCalendarClosed;
        AccessibleRole = AccessibleRole.Text;
        Size = new Size(240, preferredHeight);
        _referenceDate = DateTime.Today;
    }

    /// <summary>Configuration captured when the next edit session starts.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateInputProfile Profile
    {
        get => _profile;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_pendingEdit) throw new InvalidOperationException("A pending date edit cannot change profile.");
            if (value.Minimum < DateTimePicker.MinimumDateTime.Date || value.Maximum > DateTimePicker.MaximumDateTime.Date)
                throw new ArgumentOutOfRangeException(nameof(value), "Calendar bounds must fit the native calendar range.");
            _profile = value;
        }
    }

    /// <summary>Clock captured for each new pending candidate or calendar opening without pending input.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<DateTime> ReferenceDateProvider
    {
        get => _referenceDateProvider;
        set => _referenceDateProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Exact candidate text, without completing missing date components.</summary>
    [Browsable(false)]
    public string PendingText => _pendingText;

    /// <summary>The last value accepted or projected by the host.</summary>
    [Browsable(false)]
    public DateTime? Value => _value;

    /// <summary>Whether the native text differs from the accepted display.</summary>
    [Browsable(false)]
    public bool HasPendingEdit => _pendingEdit;

    /// <summary>Gets or sets the current lexical candidate.</summary>
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
    public override string Text
    {
        get => _entry?.Text ?? string.Empty;
        set => _entry.Text = value ?? string.Empty;
    }

    /// <summary>Controls whether native text can be edited or a calendar opened.</summary>
    public bool ReadOnly
    {
        get => _entry.ReadOnly;
        set { _entry.ReadOnly = value; _calendar.Enabled = !value && Enabled; }
    }

    /// <summary>Validation presentation shared by the text and calendar affordance.</summary>
    public BootstrapValidationState ValidationState
    {
        get => _entry.ValidationState;
        set { _entry.ValidationState = value; _calendar.ValidationState = value; }
    }

    /// <summary>Raised when lexical candidate text changes, including invalid text.</summary>
    public event EventHandler? PendingTextChanged;

    /// <summary>Raised once when a different accepted date replaces the prior value.</summary>
    public event EventHandler? ValueChanged;

    /// <summary>Captures the configured profile and local reference date for a new admitted edit.</summary>
    public void BeginEditSession(DateTime? currentValue)
    {
        if (_calendar.IsDisposed) throw new ObjectDisposedException(nameof(BootstrapDateInput));
        _calendar.CloseDropDown();
        _activeProfile = _profile;
        _referenceDate = _referenceDateProvider().Date;
        _popupSnapshot = null;
        ProjectAcceptedValue(currentValue);
    }

    /// <summary>Replaces presentation from an authoritative model value without recapturing the clock.</summary>
    public void ProjectAcceptedValue(DateTime? value)
    {
        _value = value.HasValue ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Unspecified) : (DateTime?)null;
        _pendingEdit = false;
        SetCandidate(Format(_value), false);
    }

    /// <summary>Purely parses the current candidate using this session's captured reference.</summary>
    public DateParseResult TryParsePending() => QuickDateParser.TryParsePending(_pendingText, ActiveProfile, _referenceDate);

    /// <summary>Accepts a valid pending date exactly once, retaining invalid text.</summary>
    public bool TryCommitPending()
    {
        var parsed = TryParsePending();
        if (parsed.State != InputParseState.Valid) return false;
        var changed = _value != parsed.Value;
        ProjectAcceptedValue(parsed.Value);
        if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Opens the themed calendar without committing the typed candidate.</summary>
    public void ShowCalendar()
    {
        if (ReadOnly || !Enabled || _popupSnapshot != null) return;
        if (!_pendingEdit) _referenceDate = _referenceDateProvider().Date;
        PrepareCalendarSnapshot();
        try { _calendar.ShowDropDown(); }
        catch { _popupSnapshot = null; throw; }
    }

    private void PrepareCalendarSnapshot()
    {
        if (_popupSnapshot != null) return;
        _popupSnapshot = _pendingText;
        _popupCaret = _entry.NativeEditor.SelectionStart;
        _popupAccepted = false;
        _synchronizingCalendar = true;
        try
        {
            if (ActiveProfile.Minimum > _calendar.MaxDate) _calendar.MaxDate = ActiveProfile.Maximum;
            if (ActiveProfile.Maximum < _calendar.MinDate) _calendar.MinDate = ActiveProfile.Minimum;
            _calendar.MinDate = ActiveProfile.Minimum;
            _calendar.MaxDate = ActiveProfile.Maximum;
            var parsed = TryParsePending();
            _calendar.SelectedDate = parsed.State == InputParseState.Valid ? parsed.Value : _value;
        }
        finally { _synchronizingCalendar = false; }
    }

    internal TextBox NativeEditor => _entry.NativeEditor;
    internal BootstrapCalendarPicker Calendar => _calendar;

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _calendar.Enabled = Enabled && !ReadOnly;
        if (!Enabled) _calendar.CloseDropDown();
    }

    /// <inheritdoc />
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        if (!_entry.ContainsFocus) _entry.NativeEditor.Focus();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _entry.TextChanged -= OnEntryTextChanged;
            _entry.KeyDown -= OnEntryKeyDown;
            _entry.NativeEditor.HandleCreated -= OnEditorHandleCreated;
            _entry.NativeEditor.HandleDestroyed -= OnEditorHandleDestroyed;
            _imeMonitor.ReleaseHandle();
            _calendar.SelectionChanged -= OnCalendarSelectionChanged;
            _calendar.SelectionAccepted -= OnCalendarSelectionAccepted;
            _calendar.Opened -= OnCalendarOpened;
            _calendar.Closed -= OnCalendarClosed;
        }
        base.Dispose(disposing);
    }

    private DateInputProfile ActiveProfile => _activeProfile ?? _profile;
    private static string Format(DateTime? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

    private void SetCandidate(string candidate, bool pending)
    {
        _applying = true;
        try { _entry.Text = candidate; }
        finally { _applying = false; }
        _pendingText = candidate;
        _pendingEdit = pending;
    }

    private void OnEntryTextChanged(object? sender, EventArgs e)
    {
        if (_applying) return;
        var candidate = _entry.Text;
        if (_pendingText == candidate) return;
        if (!_pendingEdit) _referenceDate = _referenceDateProvider().Date;
        _pendingText = candidate;
        _pendingEdit = true;
        PendingTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (_imeComposing) return;
        if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
        {
            ShowCalendar();
            e.Handled = e.SuppressKeyPress = true;
            return;
        }
        if (e.KeyCode == Keys.Escape)
        {
            if (_popupSnapshot != null) _calendar.CloseDropDown();
            else ProjectAcceptedValue(_value);
            e.Handled = e.SuppressKeyPress = true;
            return;
        }
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = e.SuppressKeyPress = true;
            TryCommitPending();
            return;
        }
        if (!e.Alt && e.Modifiers == Keys.None && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
        {
            var parsed = TryParsePending();
            if (parsed.State != InputParseState.Valid || !parsed.Value.HasValue) return;
            var step = QuickDateParser.Step(parsed.Value.Value, CurrentSegment(), e.KeyCode == Keys.Up ? 1 : -1, ActiveProfile);
            if (step.State == InputParseState.Valid)
            {
                SetCandidate(Format(step.Value), true);
                PendingTextChanged?.Invoke(this, EventArgs.Empty);
            }
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    private void OnEditorHandleCreated(object? sender, EventArgs e) => _imeMonitor.AssignHandle(_entry.NativeEditor.Handle);

    private void OnEditorHandleDestroyed(object? sender, EventArgs e)
    {
        _imeMonitor.ReleaseHandle();
        _imeComposing = false;
    }

    private DateSegment CurrentSegment()
    {
        var caret = _entry.NativeEditor.SelectionStart;
        var first = _pendingText.IndexOf('/');
        var second = first < 0 ? -1 : _pendingText.IndexOf('/', first + 1);
        if (first < 0 || caret <= first) return DateSegment.Day;
        return second < 0 || caret <= second ? DateSegment.Month : DateSegment.Year;
    }

    private void OnCalendarSelectionChanged(object? sender, EventArgs e)
    {
        if (_synchronizingCalendar || _popupSnapshot == null || !_calendar.SelectedDate.HasValue) return;
        PreviewCalendarSelection();
    }

    private void OnCalendarOpened(object? sender, EventArgs e)
    {
        if (!ReadOnly && Enabled) PrepareCalendarSnapshot();
    }

    private void OnCalendarSelectionAccepted(object? sender, EventArgs e)
    {
        if (_popupSnapshot == null || !_calendar.SelectedDate.HasValue) return;
        _popupAccepted = true;
        PreviewCalendarSelection();
        TryCommitPending();
    }

    private void OnCalendarClosed(object? sender, EventArgs e)
    {
        var snapshot = _popupSnapshot;
        _popupSnapshot = null;
        if (snapshot == null || _popupAccepted) return;
        var changed = !string.Equals(_pendingText, snapshot, StringComparison.Ordinal);
        SetCandidate(snapshot, !string.Equals(snapshot, Format(_value), StringComparison.Ordinal));
        _entry.NativeEditor.SelectionStart = Math.Min(_popupCaret, _entry.NativeEditor.TextLength);
        if (changed) PendingTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PreviewCalendarSelection()
    {
        var candidate = Format(_calendar.SelectedDate);
        if (string.Equals(candidate, _pendingText, StringComparison.Ordinal)) return;
        SetCandidate(candidate, true);
        PendingTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class DateEntryTextBox : BootstrapTextBox
    {
        private readonly BootstrapDateInput _owner;
        internal DateEntryTextBox(BootstrapDateInput owner) => _owner = owner;
        internal TextBox NativeEditor => Editor;
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab && (_owner._imeComposing || !_owner.TryCommitPending())) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private sealed class DateImeMonitor : NativeWindow
    {
        private readonly BootstrapDateInput _owner;
        internal DateImeMonitor(BootstrapDateInput owner) => _owner = owner;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x010D) _owner._imeComposing = true; // WM_IME_STARTCOMPOSITION
            if (message.Msg == 0x010E) _owner._imeComposing = false; // WM_IME_ENDCOMPOSITION
            base.WndProc(ref message);
        }
    }
}
