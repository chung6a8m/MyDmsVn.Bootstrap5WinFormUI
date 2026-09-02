using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>Provides a DataGridView column that edits raw values through a Bootstrap lookup.</summary>
public partial class BootstrapLookupColumn : DataGridViewColumn
{
    private BootstrapLookupColumnDefinitionCollection _lookupColumns = new BootstrapLookupColumnDefinitionCollection();
    private BootstrapLookupSearchMemberCollection _searchMembers = new BootstrapLookupSearchMemberCollection();
    private BootstrapLookupUnmatchedTextBehavior _unmatchedTextBehavior;
    private BootstrapLookupEmptyQueryBehavior _emptyQueryBehavior;
    private BootstrapLookupTypingPopupBehavior _typingPopupBehavior;
    private BootstrapLookupEnterKeyBehavior _enterKeyBehavior;
    private BootstrapLookupClosedEnterKeyBehavior _closedEnterKeyBehavior;
    private int _searchDebounceMilliseconds = 150;
    private int _minimumSearchLength;
    private int _dropDownWidth;
    private int _maxDropDownHeight = 320;
    private Func<string, string> _searchTextNormalizer = BootstrapLookupTextNormalization.NormalizeSearchText;
    private Func<string, string> _textNormalizer = value => (value ?? string.Empty).Trim();
    private IEqualityComparer<string> _textComparer = StringComparer.CurrentCultureIgnoreCase;

    /// <summary>Initializes a lookup column.</summary>
    public BootstrapLookupColumn() : base(new BootstrapLookupCell()) { }

    /// <summary>Gets or sets the lookup data source.</summary>
    [DefaultValue(null)] public object? DataSource { get; set; }
    /// <summary>Gets or sets the lookup display member.</summary>
    [DefaultValue("")] public string DisplayMember { get; set; } = string.Empty;
    /// <summary>Gets or sets the lookup value member.</summary>
    [DefaultValue("")] public string ValueMember { get; set; } = string.Empty;
    /// <summary>Gets result column definitions.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)] public BootstrapLookupColumnDefinitionCollection LookupColumns => _lookupColumns;
    /// <summary>Gets ordered searchable members.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)] public BootstrapLookupSearchMemberCollection SearchMembers => _searchMembers;
    /// <summary>Gets or sets unmatched-text behavior.</summary>
    [DefaultValue(BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection)] public BootstrapLookupUnmatchedTextBehavior UnmatchedTextBehavior { get => _unmatchedTextBehavior; set { ValidateEnum(value); _unmatchedTextBehavior = value; } }
    /// <summary>Gets or sets empty-query behavior.</summary>
    [DefaultValue(BootstrapLookupEmptyQueryBehavior.ShowAll)] public BootstrapLookupEmptyQueryBehavior EmptyQueryBehavior { get => _emptyQueryBehavior; set { ValidateEnum(value); _emptyQueryBehavior = value; } }
    /// <summary>Gets or sets typing popup behavior.</summary>
    [DefaultValue(BootstrapLookupTypingPopupBehavior.AutoOpen)] public BootstrapLookupTypingPopupBehavior TypingPopupBehavior { get => _typingPopupBehavior; set { ValidateEnum(value); _typingPopupBehavior = value; } }
    /// <summary>Gets or sets Enter behavior.</summary>
    [DefaultValue(BootstrapLookupEnterKeyBehavior.CommitSelection)] public BootstrapLookupEnterKeyBehavior EnterKeyBehavior { get => _enterKeyBehavior; set { ValidateEnum(value); _enterKeyBehavior = value; } }
    /// <summary>Gets or sets closed Enter behavior.</summary>
    [DefaultValue(BootstrapLookupClosedEnterKeyBehavior.ResolvePendingText)] public BootstrapLookupClosedEnterKeyBehavior ClosedEnterKeyBehavior { get => _closedEnterKeyBehavior; set { ValidateEnum(value); _closedEnterKeyBehavior = value; } }
    /// <summary>Gets or sets debounce milliseconds.</summary>
    [DefaultValue(150)] public int SearchDebounceMilliseconds { get => _searchDebounceMilliseconds; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _searchDebounceMilliseconds = value; } }
    /// <summary>Gets or sets minimum search length.</summary>
    [DefaultValue(0)] public int MinimumSearchLength { get => _minimumSearchLength; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _minimumSearchLength = value; } }
    /// <summary>Gets or sets explicit popup width.</summary>
    [DefaultValue(0)] public int DropDownWidth { get => _dropDownWidth; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _dropDownWidth = value; } }
    /// <summary>Gets or sets maximum popup height.</summary>
    [DefaultValue(320)] public int MaxDropDownHeight { get => _maxDropDownHeight; set { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _maxDropDownHeight = value; } }
    /// <summary>Gets or sets whether column headers are visible.</summary>
    [DefaultValue(true)] public bool ShowColumnHeaders { get; set; } = true;
    /// <summary>Gets or sets whether Refresh is visible.</summary>
    [DefaultValue(false)] public bool ShowRefreshButton { get; set; }
    /// <summary>Gets or sets whether Add New is visible.</summary>
    [DefaultValue(false)] public bool ShowAddNewButton { get; set; }
    /// <summary>Gets or sets search normalization.</summary>
    [Browsable(false)] public Func<string, string> SearchTextNormalizer { get => _searchTextNormalizer; set => _searchTextNormalizer = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets exact-match normalization.</summary>
    [Browsable(false)] public Func<string, string> TextNormalizer { get => _textNormalizer; set => _textNormalizer = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets exact-match comparison.</summary>
    [Browsable(false)] public IEqualityComparer<string> TextComparer { get => _textComparer; set => _textComparer = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets invalid-text message.</summary>
    [DefaultValue("Please select a valid value.")] public string InvalidTextMessage { get; set; } = "Please select a valid value.";

    /// <inheritdoc />
    public override object Clone()
    {
        var clone = (BootstrapLookupColumn)base.Clone();
        clone._lookupColumns = new BootstrapLookupColumnDefinitionCollection();
        clone._searchMembers = new BootstrapLookupSearchMemberCollection();
        clone.DataSource = DataSource;
        clone.DisplayMember = DisplayMember;
        clone.ValueMember = ValueMember;
        clone.CopyConfigurationFrom(this);
        return clone;
    }

    internal string ResolveDisplayText(object? value)
    {
        using var adapter = new BootstrapLookupDataAdapter(DataSource, DisplayMember, ValueMember);
        return value is not null && adapter.TryFindByValue(value, out var item) ? item!.DisplayText : string.Empty;
    }

    private void CopyConfigurationFrom(BootstrapLookupColumn source)
    {
        _lookupColumns.Clear();
        foreach (var definition in source.LookupColumns) _lookupColumns.Add(CloneDefinition(definition));
        _searchMembers.Clear();
        foreach (var member in source.SearchMembers) _searchMembers.Add(member);
        UnmatchedTextBehavior = source.UnmatchedTextBehavior; EmptyQueryBehavior = source.EmptyQueryBehavior;
        TypingPopupBehavior = source.TypingPopupBehavior; EnterKeyBehavior = source.EnterKeyBehavior;
        ClosedEnterKeyBehavior = source.ClosedEnterKeyBehavior; SearchDebounceMilliseconds = source.SearchDebounceMilliseconds;
        MinimumSearchLength = source.MinimumSearchLength; DropDownWidth = source.DropDownWidth; MaxDropDownHeight = source.MaxDropDownHeight;
        ShowColumnHeaders = source.ShowColumnHeaders; ShowRefreshButton = source.ShowRefreshButton; ShowAddNewButton = source.ShowAddNewButton;
        SearchTextNormalizer = source.SearchTextNormalizer; TextNormalizer = source.TextNormalizer; TextComparer = source.TextComparer;
        InvalidTextMessage = source.InvalidTextMessage;
    }

    internal static BootstrapLookupColumnDefinition CloneDefinition(BootstrapLookupColumnDefinition source) => new BootstrapLookupColumnDefinition
    {
        DataPropertyName = source.DataPropertyName, HeaderText = source.HeaderText, Width = source.Width,
        MinimumWidth = source.MinimumWidth, Visible = source.Visible, AutoSizeMode = source.AutoSizeMode, Alignment = source.Alignment,
        Format = source.Format, ValueType = source.ValueType
    };

    private static void ValidateEnum<T>(T value) where T : struct
    {
        if (!Enum.IsDefined(typeof(T), value)) throw new InvalidEnumArgumentException(nameof(value), Convert.ToInt32(value), typeof(T));
    }
}
