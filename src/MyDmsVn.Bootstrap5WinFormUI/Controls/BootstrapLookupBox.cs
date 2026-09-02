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
    private object? _selectedItem;
    private object? _selectedValue;
    private string _committedDisplayText = string.Empty;
    private object? _highlightedItem;
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

    /// <summary>Initializes a designer-safe lookup editor.</summary>
    public BootstrapLookupBox()
    {
        _dropDownContent = new BootstrapLookupDropDownContent();
        _dropDownController = new BootstrapLookupDropDownController(this, _dropDownContent);
        _searchDebouncer = new BootstrapUiDebouncer();
        _dropDownAffordance = new BootstrapLookupDropDownAffordance();
        _dropDownAffordance.Activated += OnDropDownAffordanceActivated;
        SetFrameworkTrailingAccessory(_dropDownAffordance);
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
    public BootstrapLookupEmptyQueryBehavior EmptyQueryBehavior { get; set; } = BootstrapLookupEmptyQueryBehavior.ShowAll;

    /// <summary>Gets or sets typing popup behavior.</summary>
    [DefaultValue(BootstrapLookupTypingPopupBehavior.AutoOpen)]
    public BootstrapLookupTypingPopupBehavior TypingPopupBehavior { get; set; } = BootstrapLookupTypingPopupBehavior.AutoOpen;

    /// <summary>Gets or sets unmatched text resolution.</summary>
    [DefaultValue(BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection)]
    public BootstrapLookupUnmatchedTextBehavior UnmatchedTextBehavior { get; set; } = BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection;

    /// <summary>Gets or sets Enter behavior after a commit.</summary>
    [DefaultValue(BootstrapLookupEnterKeyBehavior.CommitSelection)]
    public BootstrapLookupEnterKeyBehavior EnterKeyBehavior { get; set; } = BootstrapLookupEnterKeyBehavior.CommitSelection;

    /// <summary>Gets or sets closed-popup Enter behavior.</summary>
    [DefaultValue(BootstrapLookupClosedEnterKeyBehavior.ResolvePendingText)]
    public BootstrapLookupClosedEnterKeyBehavior ClosedEnterKeyBehavior { get; set; } = BootstrapLookupClosedEnterKeyBehavior.ResolvePendingText;

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
        _searchDebouncer.Cancel();
        CloseDropDown();
        SynchronizeText(_committedDisplayText);
        _hasPendingText = false;
        ClearLookupValidation();
    }

    /// <inheritdoc />
    protected override void OnEditorTextChanged(EventArgs e)
    {
        base.OnEditorTextChanged(e);
        if (_synchronizingText) return;
        _hasPendingText = !string.Equals(Text, _committedDisplayText, StringComparison.Ordinal);
        ClearLookupValidation();
        ScheduleSearchForEditorText();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dropDownAffordance.Activated -= OnDropDownAffordanceActivated;
            _searchDebouncer.Dispose();
            _dropDownController.Dispose();
            DisposeDataAdapter();
            _dropDownContent.Dispose();
        }
        base.Dispose(disposing);
    }

    internal void SetHighlightedItem(object? item)
    {
        if (ReferenceEquals(_highlightedItem, item) || Equals(_highlightedItem, item)) return;
        var previous = _highlightedItem;
        _highlightedItem = item;
        HighlightedItemChanged?.Invoke(this, new BootstrapLookupHighlightedItemChangedEventArgs(previous, item));
    }

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
        _synchronizingText = true;
        try { Text = value ?? string.Empty; }
        finally { _synchronizingText = false; }
    }

    internal void RaiseResultsChanged() => ResultsChanged?.Invoke(this, EventArgs.Empty);
    internal void RaiseRefreshRequested(BootstrapLookupRefreshRequestedEventArgs e) => RefreshRequested?.Invoke(this, e);
    internal void RaiseAddNewRequested(BootstrapLookupAddNewRequestedEventArgs e) => AddNewRequested?.Invoke(this, e);
    internal void RaiseCreateItemFromText(BootstrapLookupCreateItemFromTextEventArgs e) => CreateItemFromText?.Invoke(this, e);

    private void OnDropDownAffordanceActivated(object? sender, EventArgs e)
    {
        if (!Enabled) return;
        Editor.Focus();
        if (IsDropDownOpen) CloseDropDown(); else OpenDropDown();
    }
}
