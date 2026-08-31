using System;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides Bootstrap-themed native text editing with deterministic display formatting and a canonical raw value.</summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(TextChanged))]
public class BootstrapFormattedTextBox : BootstrapTextBox
{
    private static readonly IInputFormatter IdentityFormatter = new IdentityInputFormatter();
    private readonly FormattedTextHistory _history = new FormattedTextHistory();
    private readonly BootstrapGeneralInputFormatter _generalFormatter;
    private readonly BootstrapNumeralInputFormatter _numeralFormatter;
    private readonly BootstrapDateInputFormatter _dateFormatter;
    private readonly BootstrapTimeInputFormatter _timeFormatter;
    private readonly BootstrapCreditCardInputFormatter _creditCardFormatter;
    private BootstrapInputFormatMode _formatMode;
    private IInputFormatter? _formatter;
    private string _rawValue = string.Empty;
    private string _displayValue = string.Empty;
    private BootstrapCreditCardType _creditCardType = BootstrapCreditCardType.General;
    private int _stableRawSelectionStart;
    private int _stableRawSelectionLength;
    private bool _applyingFormattedText;
    private bool _optionsSubscribed;

    /// <summary>Initializes a new formatted text box with identity formatting.</summary>
    public BootstrapFormattedTextBox()
    {
        GeneralOptions = new BootstrapGeneralFormatOptions();
        NumeralOptions = new BootstrapNumeralFormatOptions();
        DateOptions = new BootstrapDateFormatOptions();
        TimeOptions = new BootstrapTimeFormatOptions();
        CreditCardOptions = new BootstrapCreditCardFormatOptions();
        _generalFormatter = new BootstrapGeneralInputFormatter(GeneralOptions);
        _numeralFormatter = new BootstrapNumeralInputFormatter(NumeralOptions);
        _dateFormatter = new BootstrapDateInputFormatter(DateOptions);
        _timeFormatter = new BootstrapTimeInputFormatter(TimeOptions);
        _creditCardFormatter = new BootstrapCreditCardInputFormatter(CreditCardOptions);
        SubscribeOptions();
    }

    /// <summary>Gets or sets the active built-in or custom formatting mode.</summary>
    [Category("Behavior")]
    [DefaultValue(BootstrapInputFormatMode.None)]
    public BootstrapInputFormatMode FormatMode
    {
        get => _formatMode;
        set
        {
            InputFormatOptionValidation.ValidateEnum(value, nameof(value));
            if (_formatMode == value) return;
            _formatMode = value;
            Reformat();
        }
    }

    /// <summary>Gets or sets the canonical unformatted value.</summary>
    [Category("Data")]
    [DefaultValue("")]
    public string RawValue
    {
        get => _rawValue;
        set
        {
            _history.Clear();
            ApplyRawValue(value ?? string.Empty, (value ?? string.Empty).Length, 0, raiseEvents: true);
        }
    }

    /// <summary>Gets or sets the formatter used when <see cref="FormatMode"/> is <see cref="BootstrapInputFormatMode.Custom"/>.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue(null)]
    public IInputFormatter? Formatter
    {
        get => _formatter;
        set
        {
            if (ReferenceEquals(_formatter, value)) return;
            _formatter = value;
            Reformat();
        }
    }

    /// <summary>Gets the mutable options used by General mode.</summary>
    [Category("Formatting")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapGeneralFormatOptions GeneralOptions { get; }

    /// <summary>Gets the mutable options used by Numeral mode.</summary>
    [Category("Formatting")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapNumeralFormatOptions NumeralOptions { get; }

    /// <summary>Gets the mutable options used by Date mode.</summary>
    [Category("Formatting")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDateFormatOptions DateOptions { get; }

    /// <summary>Gets the mutable options used by Time mode.</summary>
    [Category("Formatting")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapTimeFormatOptions TimeOptions { get; }

    /// <summary>Gets the mutable options used by CreditCard mode.</summary>
    [Category("Formatting")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapCreditCardFormatOptions CreditCardOptions { get; }

    /// <summary>Gets the detected credit-card type in CreditCard mode, or General in every other mode.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapCreditCardType CreditCardType => _creditCardType;

    /// <inheritdoc />
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    [DefaultValue("")]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
    public override string Text
    {
        get => _displayValue;
        set
        {
            var candidate = value ?? string.Empty;
            _history.Clear();
            ApplyCandidateText(candidate, candidate.Length, 0, recordHistory: false, raiseEvents: true);
        }
    }

    /// <summary>Occurs when the canonical raw value changes.</summary>
    public event EventHandler? RawValueChanged;

    /// <summary>Occurs when the effective detected credit-card type changes.</summary>
    public event EventHandler? CreditCardTypeChanged;

    /// <summary>Recomputes canonical and display values with the current formatter and options without recording undo history.</summary>
    public void Reformat()
    {
        _history.Clear();
        ApplyRawValue(_rawValue, _stableRawSelectionStart, _stableRawSelectionLength, raiseEvents: true);
    }

    /// <inheritdoc />
    protected override void OnEditorTextChanged(EventArgs e)
    {
        if (_applyingFormattedText) return;
        var selectionStart = InferCandidateSelectionStart(_displayValue, Editor.Text, Editor.SelectionStart);
        ApplyCandidateText(Editor.Text, selectionStart, Editor.SelectionLength, recordHistory: true, raiseEvents: true);
    }

    /// <inheritdoc />
    protected override void OnEditorKeyDown(KeyEventArgs e)
    {
        base.OnEditorKeyDown(e);
        if (e.Handled || e.SuppressKeyPress) return;

        if (TryHandleUndoRedo(e) || TryHandleFormattingSeparatorDeletion(e))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && _optionsSubscribed)
        {
            GeneralOptions.Changed -= OnOptionsChanged;
            NumeralOptions.Changed -= OnOptionsChanged;
            DateOptions.Changed -= OnOptionsChanged;
            TimeOptions.Changed -= OnOptionsChanged;
            CreditCardOptions.Changed -= OnOptionsChanged;
            _optionsSubscribed = false;
        }

        base.Dispose(disposing);
    }

    private void ApplyCandidateText(string candidateText, int selectionStart, int selectionLength, bool recordHistory, bool raiseEvents)
    {
        var candidate = candidateText ?? string.Empty;
        if (TryApplyGeneralPrefixEdit(candidate, recordHistory, raiseEvents)) return;

        var formatter = GetEffectiveFormatter();
        var rawSelectionStart = InputCaretMapper.ToRawPosition(formatter, candidate, selectionStart);
        var rawSelectionEnd = InputCaretMapper.ToRawPosition(formatter, candidate, selectionStart + selectionLength);
        var canonicalRaw = formatter.Unformat(candidate) ?? string.Empty;
        var finalDisplay = formatter.Format(canonicalRaw) ?? string.Empty;
        canonicalRaw = formatter.Unformat(finalDisplay) ?? string.Empty;
        finalDisplay = formatter.Format(canonicalRaw) ?? string.Empty;
        ApplyStablePair(canonicalRaw, finalDisplay, rawSelectionStart, rawSelectionEnd - rawSelectionStart, recordHistory, raiseEvents);
    }

    private bool TryApplyGeneralPrefixEdit(string candidateText, bool recordHistory, bool raiseEvents)
    {
        var prefix = GeneralOptions.Prefix;
        if (!recordHistory ||
            _formatMode != BootstrapInputFormatMode.General ||
            prefix.Length == 0 ||
            !_displayValue.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var change = FindChangedRange(_displayValue, candidateText);
        if (change.PreviousStart >= prefix.Length) return false;

        var rawStart = InputCaretMapper.ToRawPosition(_generalFormatter, _displayValue, change.PreviousStart);
        var previousEnd = change.PreviousStart + change.PreviousLength;
        var rawEnd = InputCaretMapper.ToRawPosition(_generalFormatter, _displayValue, previousEnd);
        var insertedCandidate = candidateText.Substring(change.CandidateStart, change.CandidateLength);
        var insertedRaw = _generalFormatter.Unformat(prefix + insertedCandidate);
        var candidateRaw = _rawValue
            .Remove(rawStart, Math.Max(0, rawEnd - rawStart))
            .Insert(rawStart, insertedRaw);
        var finalDisplay = _generalFormatter.Format(candidateRaw) ?? string.Empty;
        var canonicalRaw = _generalFormatter.Unformat(finalDisplay) ?? string.Empty;
        finalDisplay = _generalFormatter.Format(canonicalRaw) ?? string.Empty;
        ApplyStablePair(
            canonicalRaw,
            finalDisplay,
            rawStart + insertedRaw.Length,
            0,
            recordHistory,
            raiseEvents);
        return true;
    }

    private void ApplyRawValue(string candidateRaw, int rawSelectionStart, int rawSelectionLength, bool raiseEvents)
    {
        var formatter = GetEffectiveFormatter();
        var finalDisplay = formatter.Format(candidateRaw ?? string.Empty) ?? string.Empty;
        var canonicalRaw = formatter.Unformat(finalDisplay) ?? string.Empty;
        finalDisplay = formatter.Format(canonicalRaw) ?? string.Empty;
        ApplyStablePair(canonicalRaw, finalDisplay, rawSelectionStart, rawSelectionLength, recordHistory: false, raiseEvents);
    }

    private void ApplyStablePair(string canonicalRaw, string finalDisplay, int rawSelectionStart, int rawSelectionLength, bool recordHistory, bool raiseEvents)
    {
        var rawChanged = _rawValue != canonicalRaw;
        var displayChanged = _displayValue != finalDisplay;
        var previous = new FormattedTextSnapshot(_rawValue, _stableRawSelectionStart, _stableRawSelectionLength);
        if (recordHistory && (rawChanged || displayChanged)) _history.Record(previous);

        var selection = new FormattedTextSnapshot(canonicalRaw, rawSelectionStart, rawSelectionLength);
        if (Editor.Text != finalDisplay)
        {
            _applyingFormattedText = true;
            try
            {
                Editor.Text = finalDisplay;
            }
            finally
            {
                _applyingFormattedText = false;
            }
        }

        var formattedStart = InputCaretMapper.ToFormattedPosition(GetEffectiveFormatter(), canonicalRaw, selection.RawSelectionStart);
        var formattedEnd = InputCaretMapper.ToFormattedPosition(GetEffectiveFormatter(), canonicalRaw, selection.RawSelectionStart + selection.RawSelectionLength);
        Editor.Select(formattedStart, Math.Max(0, formattedEnd - formattedStart));
        _rawValue = canonicalRaw;
        _displayValue = finalDisplay;
        _stableRawSelectionStart = selection.RawSelectionStart;
        _stableRawSelectionLength = selection.RawSelectionLength;

        var nextCardType = _formatMode == BootstrapInputFormatMode.CreditCard
            ? _creditCardFormatter.GetCardType(canonicalRaw)
            : BootstrapCreditCardType.General;
        var cardTypeChanged = _creditCardType != nextCardType;
        _creditCardType = nextCardType;

        if (!raiseEvents) return;
        if (displayChanged) base.OnEditorTextChanged(EventArgs.Empty);
        if (rawChanged) RawValueChanged?.Invoke(this, EventArgs.Empty);
        if (cardTypeChanged) CreditCardTypeChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool TryHandleUndoRedo(KeyEventArgs e)
    {
        if (!e.Control || e.Alt || e.Shift || (e.KeyCode != Keys.Z && e.KeyCode != Keys.Y)) return false;

        var current = GetCurrentSnapshot();
        FormattedTextSnapshot snapshot;
        var restored = e.KeyCode == Keys.Z
            ? _history.TryUndo(current, out snapshot)
            : _history.TryRedo(current, out snapshot);
        if (restored)
        {
            ApplyRawValue(snapshot.RawValue, snapshot.RawSelectionStart, snapshot.RawSelectionLength, raiseEvents: true);
        }

        return true;
    }

    private bool TryHandleFormattingSeparatorDeletion(KeyEventArgs e)
    {
        if (e.Modifiers != Keys.None || Editor.SelectionLength != 0 || (e.KeyCode != Keys.Back && e.KeyCode != Keys.Delete))
        {
            return false;
        }

        var caret = Editor.SelectionStart;
        var formatter = GetEffectiveFormatter();
        var rawPosition = InputCaretMapper.ToRawPosition(formatter, _displayValue, caret);
        int deletionIndex;
        if (e.KeyCode == Keys.Back)
        {
            if (caret <= 0 || rawPosition <= 0) return false;
            var previousRawPosition = InputCaretMapper.ToRawPosition(formatter, _displayValue, caret - 1);
            if (previousRawPosition != rawPosition) return false;
            deletionIndex = rawPosition - 1;
        }
        else
        {
            if (caret >= _displayValue.Length || rawPosition >= _rawValue.Length) return false;
            var nextRawPosition = InputCaretMapper.ToRawPosition(formatter, _displayValue, caret + 1);
            if (nextRawPosition != rawPosition) return false;
            deletionIndex = rawPosition;
        }

        if (deletionIndex < 0 || deletionIndex >= _rawValue.Length) return false;
        _history.Record(GetCurrentSnapshot());
        var nextRaw = _rawValue.Remove(deletionIndex, 1);
        ApplyRawValue(nextRaw, deletionIndex, 0, raiseEvents: true);
        return true;
    }

    private FormattedTextSnapshot GetCurrentSnapshot()
    {
        var formatter = GetEffectiveFormatter();
        var start = InputCaretMapper.ToRawPosition(formatter, _displayValue, Editor.SelectionStart);
        var end = InputCaretMapper.ToRawPosition(formatter, _displayValue, Editor.SelectionStart + Editor.SelectionLength);
        return new FormattedTextSnapshot(_rawValue, start, end - start);
    }

    private static int InferCandidateSelectionStart(string previousDisplay, string candidateDisplay, int nativeSelectionStart)
    {
        if (previousDisplay == candidateDisplay) return nativeSelectionStart;
        var change = FindChangedRange(previousDisplay, candidateDisplay);
        var inferred = change.CandidateStart + change.CandidateLength;
        return Math.Max(nativeSelectionStart, inferred);
    }

    private static TextChangeRange FindChangedRange(string previousDisplay, string candidateDisplay)
    {
        var prefix = 0;
        var commonLength = Math.Min(previousDisplay.Length, candidateDisplay.Length);
        while (prefix < commonLength && previousDisplay[prefix] == candidateDisplay[prefix]) prefix++;

        var previousSuffix = previousDisplay.Length - 1;
        var candidateSuffix = candidateDisplay.Length - 1;
        while (previousSuffix >= prefix && candidateSuffix >= prefix && previousDisplay[previousSuffix] == candidateDisplay[candidateSuffix])
        {
            previousSuffix--;
            candidateSuffix--;
        }

        var changedPreviousLength = Math.Max(0, previousSuffix - prefix + 1);
        var changedCandidateLength = Math.Max(0, candidateSuffix - prefix + 1);
        return new TextChangeRange(prefix, changedPreviousLength, prefix, changedCandidateLength);
    }

    private IInputFormatter GetEffectiveFormatter()
    {
        switch (_formatMode)
        {
            case BootstrapInputFormatMode.None: return IdentityFormatter;
            case BootstrapInputFormatMode.General: return _generalFormatter;
            case BootstrapInputFormatMode.Numeral: return _numeralFormatter;
            case BootstrapInputFormatMode.Date: return _dateFormatter;
            case BootstrapInputFormatMode.Time: return _timeFormatter;
            case BootstrapInputFormatMode.CreditCard: return _creditCardFormatter;
            case BootstrapInputFormatMode.Custom: return _formatter ?? IdentityFormatter;
            default: throw new InvalidOperationException("Unsupported input formatting mode.");
        }
    }

    private void SubscribeOptions()
    {
        GeneralOptions.Changed += OnOptionsChanged;
        NumeralOptions.Changed += OnOptionsChanged;
        DateOptions.Changed += OnOptionsChanged;
        TimeOptions.Changed += OnOptionsChanged;
        CreditCardOptions.Changed += OnOptionsChanged;
        _optionsSubscribed = true;
    }

    private void OnOptionsChanged(object? sender, EventArgs e) => Reformat();

    private readonly struct TextChangeRange
    {
        internal TextChangeRange(int previousStart, int previousLength, int candidateStart, int candidateLength)
        {
            PreviousStart = previousStart;
            PreviousLength = previousLength;
            CandidateStart = candidateStart;
            CandidateLength = candidateLength;
        }

        internal int PreviousStart { get; }

        internal int PreviousLength { get; }

        internal int CandidateStart { get; }

        internal int CandidateLength { get; }
    }

    private sealed class IdentityInputFormatter : IInputFormatter
    {
        public string Format(string rawValue) => rawValue ?? string.Empty;

        public string Unformat(string formattedValue) => formattedValue ?? string.Empty;
    }
}
