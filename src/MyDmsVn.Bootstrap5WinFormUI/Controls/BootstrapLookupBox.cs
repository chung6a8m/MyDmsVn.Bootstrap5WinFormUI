using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides a Bootstrap-themed single-selection local lookup editor.</summary>
[DefaultEvent(nameof(SelectionCommitted))]
[DefaultProperty(nameof(DisplayMember))]
public partial class BootstrapLookupBox : BootstrapTextBox
{
    private readonly BootstrapLookupColumnDefinitionCollection _columns = new BootstrapLookupColumnDefinitionCollection();
    private readonly BootstrapLookupSearchMemberCollection _searchMembers = new BootstrapLookupSearchMemberCollection();
    private readonly BootstrapLookupDropDownAffordance _dropDownAffordance;
    private readonly BootstrapLookupDropDownContent _dropDownContent;
    private readonly BootstrapLookupDropDownController _dropDownController;
    private readonly BootstrapUiDebouncer _searchDebouncer;
    private bool _synchronizingText;
    private string _synchronizingTextTarget = string.Empty;
    private object? _selectedItem;
    private object? _selectedValue;
    private string _committedDisplayText = string.Empty;
    private object? _highlightedItem;
    private object? _highlightedValue;
    private bool _hasHighlightedItem;
    private bool _hasPendingText;
    private string _validationMessage = string.Empty;
    private Func<string, string> _searchTextNormalizer = BootstrapLookupTextNormalization.NormalizeSearchText;
    private Func<string, string> _textNormalizer = value => (value ?? string.Empty).Trim();
    private IEqualityComparer<string> _textComparer = StringComparer.CurrentCultureIgnoreCase;
    private string _invalidTextMessage = "Please select a valid value.";
    private int _searchDebounceMilliseconds = 150;
    private int _minimumSearchLength;
    private int _dropDownWidth;
    private int _maxDropDownHeight = 320;
    private BootstrapLookupEmptyQueryBehavior _emptyQueryBehavior;
    private BootstrapLookupTypingPopupBehavior _typingPopupBehavior;
    private BootstrapLookupUnmatchedTextBehavior _unmatchedTextBehavior;
    private BootstrapLookupEnterKeyBehavior _enterKeyBehavior;
    private BootstrapLookupClosedEnterKeyBehavior _closedEnterKeyBehavior;
    private int _leaveResolutionGeneration;
    private int _suspendedLeaveResolutionCount;
    private int _applicationWorkflowGeneration;
    private int _activeCreateWorkflowGeneration = -1;
    private int _highlightGeneration;
    private int _resultSynchronizationGeneration;

    /// <summary>Initializes a designer-safe lookup editor.</summary>
    public BootstrapLookupBox()
    {
        _searchMembers.MemberValidator = ValidateSearchMember;
        _dropDownContent = new BootstrapLookupDropDownContent();
        _dropDownController = new BootstrapLookupDropDownController(this, _dropDownContent);
        _searchDebouncer = new BootstrapUiDebouncer();
        _dropDownAffordance = new BootstrapLookupDropDownAffordance();
        _dropDownAffordance.Activated += OnDropDownAffordanceActivated;
        Editor.ReadOnlyChanged += OnEditorReadOnlyChanged;
        Enter += OnLookupEnter;
        SetFrameworkTrailingAccessory(_dropDownAffordance);
        UpdateInteractionState();
        AccessibleRole = System.Windows.Forms.AccessibleRole.ComboBox;
        AccessibleDescription = "Searchable single-selection lookup.";
    }

    /// <summary>Gets or sets the local lookup data source.</summary>
    [Category("Data")]
    [DefaultValue(null)]
    public object? DataSource { get => _dataSource; set => SetDataSource(value); }

    /// <summary>Gets or sets the member used as display text.</summary>
    [Category("Data")]
    [DefaultValue("")]
    public string DisplayMember { get => _displayMember; set => SetDisplayMember(value); }

    /// <summary>Gets or sets the member used as logical identity.</summary>
    [Category("Data")]
    [DefaultValue("")]
    public string ValueMember { get => _valueMember; set => SetValueMember(value); }

    /// <summary>Gets declarative result columns.</summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapLookupColumnDefinitionCollection Columns => _columns;

    /// <summary>Gets ordered searchable source members.</summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapLookupSearchMemberCollection SearchMembers => _searchMembers;

    /// <summary>Gets the framework-owned result grid for advanced presentation customization.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapDataGridView ResultsGrid => _dropDownContent.ResultsGrid;

    /// <summary>Gets the committed source item.</summary>
    [Browsable(false)]
    public object? SelectedItem => _selectedItem;

    /// <summary>Gets or sets the committed logical value.</summary>
    [Browsable(false)]
    public object? SelectedValue { get => _selectedValue; set => SetSelectedValue(value); }

    /// <summary>Gets the canonical display text for committed state.</summary>
    [Browsable(false)]
    public string CommittedDisplayText => _committedDisplayText;

    /// <summary>Gets whether editor text differs from committed display state.</summary>
    [Browsable(false)]
    public bool HasPendingText => _hasPendingText;

    /// <summary>Gets the transiently highlighted result item.</summary>
    [Browsable(false)]
    public object? HighlightedItem => _highlightedItem;

    /// <summary>Gets or sets lookup search debounce in milliseconds.</summary>
    [DefaultValue(150)]
    public int SearchDebounceMilliseconds { get => _searchDebounceMilliseconds; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _searchDebounceMilliseconds = value; } }

    /// <summary>Gets or sets the normalized minimum search length.</summary>
    [DefaultValue(0)]
    public int MinimumSearchLength { get => _minimumSearchLength; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _minimumSearchLength = value; } }

    /// <summary>Gets or sets empty-query presentation.</summary>
    [DefaultValue(BootstrapLookupEmptyQueryBehavior.ShowAll)]
    public BootstrapLookupEmptyQueryBehavior EmptyQueryBehavior { get => _emptyQueryBehavior; set { ValidateEnum(value, nameof(value)); _emptyQueryBehavior = value; } }

    /// <summary>Gets or sets typing popup behavior.</summary>
    [DefaultValue(BootstrapLookupTypingPopupBehavior.AutoOpen)]
    public BootstrapLookupTypingPopupBehavior TypingPopupBehavior { get => _typingPopupBehavior; set { ValidateEnum(value, nameof(value)); _typingPopupBehavior = value; } }

    /// <summary>Gets or sets unmatched text resolution.</summary>
    [DefaultValue(BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection)]
    public BootstrapLookupUnmatchedTextBehavior UnmatchedTextBehavior { get => _unmatchedTextBehavior; set { ValidateEnum(value, nameof(value)); _unmatchedTextBehavior = value; } }

    /// <summary>Gets or sets Enter behavior after a commit.</summary>
    [DefaultValue(BootstrapLookupEnterKeyBehavior.CommitSelection)]
    public BootstrapLookupEnterKeyBehavior EnterKeyBehavior { get => _enterKeyBehavior; set { ValidateEnum(value, nameof(value)); _enterKeyBehavior = value; } }

    /// <summary>Gets or sets closed-popup Enter behavior.</summary>
    [DefaultValue(BootstrapLookupClosedEnterKeyBehavior.ResolvePendingText)]
    public BootstrapLookupClosedEnterKeyBehavior ClosedEnterKeyBehavior { get => _closedEnterKeyBehavior; set { ValidateEnum(value, nameof(value)); _closedEnterKeyBehavior = value; } }

    /// <summary>Gets or sets an explicit logical popup width, or zero for automatic width.</summary>
    [DefaultValue(0)]
    public int DropDownWidth { get => _dropDownWidth; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _dropDownWidth = value; } }

    /// <summary>Gets or sets the maximum logical popup height.</summary>
    [DefaultValue(320)]
    public int MaxDropDownHeight { get => _maxDropDownHeight; set { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _maxDropDownHeight = value; } }

    /// <summary>Gets or sets whether result column headers are shown.</summary>
    [DefaultValue(true)]
    public bool ShowColumnHeaders { get; set; } = true;

    /// <summary>Gets or sets whether the footer Refresh action is shown.</summary>
    [DefaultValue(false)]
    public bool ShowRefreshButton { get; set; }

    /// <summary>Gets or sets whether the footer Add New action is shown.</summary>
    [DefaultValue(false)]
    public bool ShowAddNewButton { get; set; }

    /// <summary>Gets or sets search-only text normalization.</summary>
    [Browsable(false)]
    public Func<string, string> SearchTextNormalizer { get => _searchTextNormalizer; set => _searchTextNormalizer = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets exact-match text normalization.</summary>
    [Browsable(false)]
    public Func<string, string> TextNormalizer { get => _textNormalizer; set => _textNormalizer = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets exact-match text comparison.</summary>
    [Browsable(false)]
    public IEqualityComparer<string> TextComparer { get => _textComparer; set => _textComparer = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the lookup-generated invalid-text message.</summary>
    [DefaultValue("Please select a valid value.")]
    public string InvalidTextMessage { get => _invalidTextMessage; set => _invalidTextMessage = value ?? string.Empty; }

    /// <summary>Gets the current lookup-generated validation message.</summary>
    [Browsable(false)]
    public string ValidationMessage => _validationMessage;

    /// <summary>Occurs when the committed logical value changes.</summary>
    public event EventHandler? SelectedValueChanged;
    /// <summary>Occurs after a selection commit is internally consistent.</summary>
    public event EventHandler<BootstrapLookupSelectionCommittedEventArgs>? SelectionCommitted;
    /// <summary>Occurs when the transient highlighted item changes.</summary>
    public event EventHandler<BootstrapLookupHighlightedItemChangedEventArgs>? HighlightedItemChanged;
    /// <summary>Occurs when the logical search projection changes.</summary>
    public event EventHandler? ResultsChanged;
    /// <summary>Occurs when refresh is requested.</summary>
    public event EventHandler<BootstrapLookupRefreshRequestedEventArgs>? RefreshRequested;
    /// <summary>Occurs when explicit Add New is requested.</summary>
    public event EventHandler<BootstrapLookupAddNewRequestedEventArgs>? AddNewRequested;
    /// <summary>Occurs when unmatched text requests a new item.</summary>
    public event EventHandler<BootstrapLookupCreateItemFromTextEventArgs>? CreateItemFromText;

    /// <summary>Selects and commits an item from the current source.</summary>
    public bool SelectItem(object? item) => SelectItemCore(item, BootstrapLookupCommitReason.Programmatic);

    /// <summary>Selects and commits an item by logical value.</summary>
    public bool SelectValue(object? value) => SelectValueCore(value, BootstrapLookupCommitReason.Programmatic);

    /// <summary>Clears the committed selection.</summary>
    public void ClearSelection() => CommitSelection(null, null, string.Empty, BootstrapLookupCommitReason.Clear);

    /// <summary>Discards pending editor text and restores committed display state.</summary>
    public void CancelPendingEdit()
    {
        CancelPendingSearch();
        CloseDropDown();
        SynchronizeText(_committedDisplayText);
        _hasPendingText = false;
        ClearLookupValidation();
    }

    /// <inheritdoc />
    protected override void OnEditorTextChanged(EventArgs e)
    {
        var publishedText = Text;
        var isFrameworkSynchronization = _synchronizingText &&
            string.Equals(publishedText, _synchronizingTextTarget, StringComparison.Ordinal);
        UpdateTextDerivedState(!isFrameworkSynchronization);
        base.OnEditorTextChanged(e);
        if (!string.Equals(Text, publishedText, StringComparison.Ordinal))
            UpdateTextDerivedState(true);
    }

    private void UpdateTextDerivedState(bool scheduleSearch)
    {
        _hasPendingText = !string.Equals(Text, _committedDisplayText, StringComparison.Ordinal);
        ClearLookupValidation();
        if (scheduleSearch) ScheduleSearchForEditorText();
    }

    /// <inheritdoc />
    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        CancelPendingSearch();
        QueueLeaveResolution();
    }

    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) { CancelPendingSearch(); CloseDropDown(); }
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        UpdateInteractionState();
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        CancelPendingSearch(); CloseDropDown();
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dropDownAffordance.Activated -= OnDropDownAffordanceActivated;
            Editor.ReadOnlyChanged -= OnEditorReadOnlyChanged;
            Enter -= OnLookupEnter;
            _searchDebouncer.Dispose();
            _dropDownController.Dispose();
            DisposeDataAdapter();
            _dropDownContent.Dispose();
        }
        base.Dispose(disposing);
    }

    internal void SetHighlightedSourceItem(BootstrapLookupSourceItem? sourceItem)
    {
        SetHighlightedItem(sourceItem?.Item, sourceItem?.Value, sourceItem is not null);
    }

    internal void SetHighlightedItem(object? item, object? value, bool hasItem)
    {
        var physicalItemChanged = _hasHighlightedItem != hasItem ||
            (hasItem && !ReferenceEquals(_highlightedItem, item));
        var previous = _highlightedItem;
        _highlightedItem = item;
        _highlightedValue = value;
        _hasHighlightedItem = hasItem;
        var generation = ++_highlightGeneration;
        if (!physicalItemChanged) return;
        var handlers = HighlightedItemChanged;
        if (handlers is null) return;
        var args = new BootstrapLookupHighlightedItemChangedEventArgs(previous, item);
        foreach (EventHandler<BootstrapLookupHighlightedItemChangedEventArgs> handler in handlers.GetInvocationList())
        {
            if (generation != _highlightGeneration) return;
            handler(this, args);
        }
    }

    internal bool IsHighlightedSourceItem(BootstrapLookupSourceItem sourceItem) => _hasHighlightedItem &&
        EqualityComparer<object?>.Default.Equals(_highlightedValue, sourceItem.Value);

    internal void SetLookupValidation(string message)
    {
        _validationMessage = message ?? string.Empty;
        SetTransientValidationStateOverride(BootstrapValidationState.Invalid);
    }

    internal void ClearLookupValidation()
    {
        if (_validationMessage.Length == 0 && ValidationState != BootstrapValidationState.Invalid) return;
        _validationMessage = string.Empty;
        SetTransientValidationStateOverride(null);
    }

    internal void SynchronizeText(string value)
    {
        var normalizedValue = value ?? string.Empty;
        if (string.Equals(Text, normalizedValue, StringComparison.Ordinal)) return;

        _synchronizingText = true;
        _synchronizingTextTarget = normalizedValue;
        try { Text = normalizedValue; }
        finally
        {
            _synchronizingText = false;
            _synchronizingTextTarget = string.Empty;
        }
        _projectionDirty = true;
    }

    internal void RaiseResultsChanged() => ResultsChanged?.Invoke(this, EventArgs.Empty);
    internal void RaiseRefreshRequested(BootstrapLookupRefreshRequestedEventArgs e) => RefreshRequested?.Invoke(this, e);
    internal void RaiseAddNewRequested(BootstrapLookupAddNewRequestedEventArgs e) => AddNewRequested?.Invoke(this, e);
    internal void RaiseCreateItemFromText(BootstrapLookupCreateItemFromTextEventArgs e) => CreateItemFromText?.Invoke(this, e);

    private void OnDropDownAffordanceActivated(object? sender, EventArgs e)
    {
        if (!Enabled || ReadOnly) return;
        Editor.Focus();
        if (IsDropDownOpen) CloseDropDown(); else OpenDropDown();
    }

    private void OnEditorReadOnlyChanged(object? sender, EventArgs e) => UpdateInteractionState();

    private void OnLookupEnter(object? sender, EventArgs e) => _leaveResolutionGeneration++;

    private void UpdateInteractionState()
    {
        _dropDownAffordance.Enabled = Enabled && !ReadOnly;
        if (!Enabled || ReadOnly)
        {
            CancelPendingSearch();
            CloseDropDown();
        }
    }

    private void QueueLeaveResolution()
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return;
        var generation = ++_leaveResolutionGeneration;
        try
        {
            BeginInvoke((Action)(() => ResolveDeferredLeave(generation)));
        }
        catch (InvalidOperationException) { }
    }

    private void ResolveDeferredLeave(int generation)
    {
        if (generation != _leaveResolutionGeneration || _suspendedLeaveResolutionCount > 0 ||
            IsDisposed || Disposing || ContainsFocus || !Enabled || ReadOnly) return;
        if (_dropDownController.ContainsFocus || !ApplicationHasFocus()) return;
        var resolution = ResolvePendingText(BootstrapLookupCommitReason.ExactMatch);
        if (!resolution.NavigationAllowed) OpenDropDown();
        else CloseDropDown();
    }

    internal void InvalidateApplicationWorkflows()
    {
        _applicationWorkflowGeneration++;
        _leaveResolutionGeneration++;
    }

    private int BeginApplicationWorkflow()
    {
        _suspendedLeaveResolutionCount++;
        _leaveResolutionGeneration++;
        return _applicationWorkflowGeneration;
    }

    private void EndApplicationWorkflow()
    {
        _suspendedLeaveResolutionCount--;
        _leaveResolutionGeneration++;
    }

    private bool IsApplicationWorkflowCurrent(int generation) =>
        generation == _applicationWorkflowGeneration && !IsDisposed && !Disposing;

    private static bool ApplicationHasFocus()
    {
        if (Form.ActiveForm is not null) return true;
        foreach (Form form in Application.OpenForms)
        {
            if (form.ContainsFocus) return true;
        }
        return false;
    }

    private static void ValidateEnum<T>(T value, string parameterName) where T : struct
    {
        if (!Enum.IsDefined(typeof(T), value)) throw new InvalidEnumArgumentException(parameterName, Convert.ToInt32(value), typeof(T));
    }
}
