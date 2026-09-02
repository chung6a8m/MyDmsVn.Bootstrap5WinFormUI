# BootstrapLookupBox Design Specification

**Status:** Approved in chat; written-spec review pending  
**Date:** 2026-09-02  
**Component:** `BootstrapLookupBox` and `BootstrapLookupColumn`  
**Project:** `MyDmsVn.Bootstrap5WinFormUI`  
**Targets:** `net48`, `net8.0-windows`

## 1. Purpose

`BootstrapLookupBox` is a new single-selection lookup/editing subsystem for WinForms. It combines the existing `BootstrapTextBox` editing experience with a reusable lookup popup whose results are rendered by `BootstrapDataGridView`. It is designed both for standalone form editing and for native `DataGridView` cell editing through a real `IDataGridViewEditingControl` implementation.

The component is intentionally separate from `BootstrapSelect`. `BootstrapSelect` remains a select-oriented control whose popup owns a search editor and custom result viewport. `BootstrapLookupBox` instead keeps the text editor as the active input surface while the popup is open, supports multi-column lookup results, and integrates directly with the `DataGridView` editing lifecycle.

The design specifically avoids the overlay-editing workaround used by the earlier DataGridView demo where a `BootstrapSelect` was placed over a native `DataGridViewTextBoxEditingControl`. The lookup editor itself must be the real DataGridView editing control.

This document is the source of truth for the implementation plan unless a later approved design change supersedes it.

## 2. Design goals

V1 must support:

- single selection only;
- `object DataSource` with WinForms-style `DisplayMember` and `ValueMember`;
- `BindingSource`, `BindingList<T>`, `IList`, `IListSource`, arrays, and equivalent local list sources;
- standalone `BootstrapLookupBox : BootstrapTextBox`;
- multi-column popup results rendered by `BootstrapDataGridView`;
- configurable result-column definitions;
- advanced result-grid customization through a read-only `ResultsGrid` property;
- configurable `SearchMembers` independent from visible result columns;
- local accent-insensitive Vietnamese-friendly search;
- multi-token AND matching across different search members;
- deterministic result ranking;
- configurable debounce and minimum search length;
- configurable empty-query and typing-popup behavior;
- strict lookup modes and dynamic-suggest `CommitAndAdd` mode;
- explicit Refresh and Add New footer actions;
- highlighted-row position / result-total status in the footer;
- a built-in mouse dropdown affordance;
- full keyboard navigation while focus remains in the main editor;
- explicit Enter, Tab, Escape, F4, Alt+Down, arrow, Home/End, and PageUp/PageDown behavior;
- application-deactivation handling that closes the popup without committing or rolling back pending text;
- native `DataGridView` editing through `BootstrapLookupColumn`, `BootstrapLookupCell`, and `BootstrapLookupEditingControl`;
- native `AllowUserToAddRows` behavior with `BindingSource` / `BindingList<T>`;
- correct dirty-state notification only when the committed lookup value changes;
- Bootstrap theme, DPI, overlay placement/collision, lifecycle, and disposal integration;
- compatibility with `net48` and `net8.0-windows`.

## 3. Explicit non-goals for V1

V1 does not include:

- multiple selection;
- async or remote search providers;
- server paging;
- virtual-mode lookup providers;
- fuzzy/edit-distance search;
- arbitrary popup content;
- arbitrary custom footer controls/actions;
- custom result column types beyond text-backed DataGridView columns;
- a generic `BootstrapLookupBox<T>` API;
- a public lookup-provider abstraction;
- a public ranking/matcher strategy abstraction;
- use of `DataTable` as a required or preferred data model;
- `SendKeys`-based DataGridView navigation;
- overlaying a second editor over a native DataGridView textbox editor.

## 4. Alternatives considered

### 4.1 Reuse `BootstrapSelect` directly as the grid editor

Rejected. `BootstrapSelect` has a different interaction contract: its popup owns the search textbox and its current drop-down controller is strongly coupled to `BootstrapSelect`. Reusing it in a DataGridView through `EditingControlShowing` leaves the native textbox as the actual editor and recreates the focus/keyboard/lifecycle fragility already observed.

### 4.2 Use only an external DataGridView controller with `EditingControlShowing`

Rejected as the primary integration. A controller may still be useful as an optional helper later, but it cannot replace the correctness of a real `IDataGridViewEditingControl`.

### 4.3 Standalone lookup control plus native DataGridView adapter

Approved. `BootstrapLookupBox` is the reusable primitive. `BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl` is the grid-specific adapter. `BootstrapLookupCell` and `BootstrapLookupColumn` provide native DataGridView integration and declarative reuse.

## 5. High-level architecture

```text
BootstrapLookupBox : BootstrapTextBox
|
+-- committed selection state
|   +-- SelectedItem
|   +-- SelectedValue
|   +-- CommittedDisplayText
|
+-- pending query state
|   +-- Text
|   +-- HasPendingText
|
+-- public lookup configuration
|   +-- DataSource / DisplayMember / ValueMember
|   +-- Columns / SearchMembers
|   +-- search, keyboard, popup, unmatched-text policies
|
+-- BootstrapLookupDropDownController (internal)
    |
    +-- existing Bootstrap overlay infrastructure
    |
    +-- BootstrapLookupDropDownContent (internal)
        +-- BootstrapDataGridView ResultsGrid
        +-- BootstrapLookupFooter
            +-- highlighted position / current result total
            +-- Refresh button
            +-- Add New button

DataGridView integration
|
+-- BootstrapLookupColumn
+-- BootstrapLookupCell
+-- BootstrapLookupEditingControl
    +-- BootstrapLookupBox
    +-- IDataGridViewEditingControl
```

Supporting internal services:

```text
BootstrapLookupDataAdapter
BootstrapLookupMemberAccessor
BootstrapLookupSearchEngine
BootstrapLookupSearchResult
```

The existing overlay surface, anchor tracker, placement/collision engine, and activation-domain concepts are reused. `BootstrapSelectDropDownController` and `BootstrapSelectDropDownContent` are not inherited or reused directly because their responsibilities are select-specific.

## 6. Ownership and state boundaries

`BootstrapLookupBox` is the source of truth. Popup controls and the result grid are views of lookup state and must not own business state.

Three state groups are kept separate:

```text
Committed state
- SelectedItem
- SelectedValue
- CommittedDisplayText

Pending edit state
- Text
- HasPendingText
- query/search state

Popup/result state
- IsDropDownOpen
- CurrentResults
- HighlightedItem
- HighlightedIndex
```

Typing, searching, and moving the highlight never change committed state.

Example:

```text
Committed value: ProductId 15 / "Cà phê rang"
Current Text:    "ca phe s"
Highlighted:     ProductId 21 / "Cà phê sữa"

SelectedValue remains 15 until a commit action succeeds.
```

## 7. Public `BootstrapLookupBox` API

Conceptual declaration:

```csharp
[DefaultEvent(nameof(SelectionCommitted))]
[DefaultProperty(nameof(DisplayMember))]
public class BootstrapLookupBox : BootstrapTextBox
{
}
```

Exact XML comments and repository-specific attributes are implementation details, but semantics below are fixed.

### 7.1 Data binding

```csharp
public object? DataSource { get; set; }
public string DisplayMember { get; set; } = string.Empty;
public string ValueMember { get; set; } = string.Empty;

public object? SelectedItem { get; }
public object? SelectedValue { get; set; }
public string CommittedDisplayText { get; }
public bool HasPendingText { get; }
public object? HighlightedItem { get; }

public bool SelectItem(object? item);
public bool SelectValue(object? value);
public void ClearSelection();
```

Rules:

- `DisplayMember == ""` means display `item?.ToString() ?? ""`.
- `ValueMember == ""` means the item itself is the logical value.
- `SelectedValue` and `SelectedItem` always represent committed state.
- manual typing changes `Text` and `HasPendingText`, but does not mutate committed selection.
- `SelectItem` verifies that the item belongs to the logical lookup source before committing it.
- V1 logical value identity uses `EqualityComparer<object>.Default`; there is no separate public value-comparer strategy in V1.
- `null` is reserved for the cleared-selection state. When `ValueMember` is non-empty, an item whose resolved value is `null` may be displayed/searched but cannot be committed as a valid selection.

`DataPropertyName` belongs to `BootstrapLookupColumn`, not to `BootstrapLookupBox`.

### 7.2 Result columns

```csharp
public BootstrapLookupColumnDefinitionCollection Columns { get; }

[Browsable(false)]
[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
public BootstrapDataGridView ResultsGrid { get; }
```

`Columns` is the declarative source of truth. `ResultsGrid` is an advanced customization escape hatch.

Conceptual column definition:

```csharp
public sealed class BootstrapLookupColumnDefinition
{
    public string DataPropertyName { get; set; } = string.Empty;
    public string HeaderText { get; set; } = string.Empty;
    public int Width { get; set; } = 100;
    public int MinimumWidth { get; set; } = 5;
    public bool Visible { get; set; } = true;
    public DataGridViewAutoSizeColumnMode AutoSizeMode { get; set; };
    public DataGridViewContentAlignment Alignment { get; set; };
    public string Format { get; set; } = string.Empty;
    public Type? ValueType { get; set; }
}
```

V1 materializes definitions as text-backed DataGridView columns. The framework owns result-grid invariants:

```text
ReadOnly = true
MultiSelect = false
SelectionMode = FullRowSelect
AllowUserToAddRows = false
AllowUserToDeleteRows = false
RowHeadersVisible = false
TabStop = false
```

Application code may use `ResultsGrid` for formatting/painting/style customization but must not change those invariants or set its `DataSource` directly.

### 7.3 Search members

```csharp
public BootstrapLookupSearchMemberCollection SearchMembers { get; }
```

The collection contains property/member names in priority order. If empty, search falls back to `DisplayMember`; if both `SearchMembers` and `DisplayMember` are empty, search uses `item?.ToString() ?? ""`. Search members do not need to be visible result columns; e.g. Barcode may be searchable but hidden.

### 7.4 Search configuration

```csharp
public int SearchDebounceMilliseconds { get; set; } = 150;
public int MinimumSearchLength { get; set; } = 0;

public BootstrapLookupEmptyQueryBehavior EmptyQueryBehavior { get; set; }
    = BootstrapLookupEmptyQueryBehavior.ShowAll;

public BootstrapLookupTypingPopupBehavior TypingPopupBehavior { get; set; }
    = BootstrapLookupTypingPopupBehavior.AutoOpen;

[Browsable(false)]
public Func<string, string> SearchTextNormalizer { get; set; }
```

`SearchDebounceMilliseconds` and `MinimumSearchLength` reject negative values.

The default search normalizer trims, performs case-insensitive normalization, removes Unicode combining diacritics, and maps Vietnamese `Đ/đ` to `D/d`. This normalization is only for searching/ranking.

### 7.5 Exact-match and unmatched-text configuration

Exact matching is deliberately separate from search normalization:

```csharp
[Browsable(false)]
public Func<string, string> TextNormalizer { get; set; }

[Browsable(false)]
public IEqualityComparer<string> TextComparer { get; set; }

public string InvalidTextMessage { get; set; }

[Browsable(false)]
public string ValidationMessage { get; }
```

Default exact semantics are Trim + `StringComparer.CurrentCultureIgnoreCase`. `InvalidTextMessage` defaults to `"Please select a valid value."`. `ValidationMessage` exposes the current lookup-generated transient validation message; it is empty when no lookup-generated validation failure is active.

```csharp
public enum BootstrapLookupUnmatchedTextBehavior
{
    RestorePreviousSelection,
    KeepFocusWithValidationError,
    CommitAndAdd
}
```

Default:

```text
RestorePreviousSelection
```

Empty/whitespace text after applying `TextNormalizer` is a separate case: it clears the selection and commits `null`; it does not enter `UnmatchedTextBehavior`.

`CommitAndAdd` must first perform exact-match detection against `DisplayMember` (or item text when `DisplayMember` is empty). If an existing item matches, that item is selected and no duplicate is added.

### 7.6 Keyboard configuration

```csharp
public enum BootstrapLookupEnterKeyBehavior
{
    CommitSelection,
    CommitSelectionAndMoveNext
}

public enum BootstrapLookupClosedEnterKeyBehavior
{
    ResolvePendingText,
    DataGridViewDefault
}
```

Defaults:

```text
EnterKeyBehavior = CommitSelection
ClosedEnterKeyBehavior = ResolvePendingText
```

`CommitSelectionAndMoveNext` delegates navigation to the owning container/DataGridView lifecycle; it must not use `SendKeys`.

### 7.7 Popup configuration

```csharp
public int DropDownWidth { get; set; } = 0;
public int MaxDropDownHeight { get; set; } = 320;
public bool ShowColumnHeaders { get; set; } = true;
public bool ShowRefreshButton { get; set; } = false;
public bool ShowAddNewButton { get; set; } = false;

public bool IsDropDownOpen { get; }

public void OpenDropDown();
public void CloseDropDown();
public void RefreshResults();
public void CancelPendingEdit();
```

V1 includes an always-visible lookup dropdown affordance on the editor surface; clicking it opens/toggles the lookup popup without committing the current text. Configurability of that affordance is deferred beyond V1.

`CloseDropDown()` is presentation-only: it does not commit, validate, or roll back. `CancelPendingEdit()` restores committed display text, clears lookup-generated transient validation, and closes the popup.

## 8. Footer design

`BootstrapLookupDropDownContent` contains:

```text
ResultsGrid : Dock.Fill
Footer      : Dock.Bottom
```

The footer remains visible while the result grid scrolls or popup height is constrained.

V1 footer content:

```text
left:  highlighted position / current filtered result count
       example: "3 / 128"
right: optional [Refresh] [Add New]
```

The result count refers to the current logical result set, not the source collection total before filtering. The internal footer boundary should allow a future remote provider to distinguish loaded count from server total without redesigning the popup, but V1 exposes only local-result semantics.

The buttons are independently optional and default to hidden. The footer itself remains visible even when both buttons are hidden. Footer buttons and `ResultsGrid` do not participate in Tab navigation; keyboard focus remains in the editor.

## 9. Search pipeline and ranking

Search runs over a projection; it must not mutate/filter the caller's original `BindingSource` or change `BindingSource.Position`.

Pipeline:

```text
raw Text
-> SearchTextNormalizer
-> MinimumSearchLength check
-> empty-query policy
-> whitespace tokenization
-> require every token to match at least one SearchMember
-> rank each token's best match
-> aggregate item rank
-> deterministic stable sort
```

Per-token match quality:

```text
Exact
> StartsWith
> WordStart
> Contains
> NoMatch
```

A token may match a different member from another token. For example query `"cf rang"` may match Code=`CF001` and Name=`Cà phê rang`.

Tie-breaks are deterministic:

1. better aggregate token match;
2. a match on `DisplayMember` is preferred;
3. earlier `SearchMembers` entries are preferred;
4. original datasource order is preserved.

When query is empty and `ShowAll` applies, ranking is not applied; original source order is used.

When applying a new result set, preserve the currently highlighted logical item if it still exists. On first/open population, prefer the committed selection if it exists in the result set; otherwise highlight the first result.

## 10. Debounce and minimum search length

Default debounce is 150 ms; 0 means search immediately. A WinForms UI-thread timer is preferred for V1.

Keyboard operations that require current results (`Down`, `PageDown`, `Enter`, `Tab`, `F4`, etc.) flush any pending debounce before resolving or navigating.

`MinimumSearchLength` is applied after search normalization and before tokenization. If the normalized query is shorter than the minimum:

```text
SearchState = WaitingForMinimumLength
ResultsGrid = empty
Footer/status = instruction such as "Type at least N characters"
```

When `MinimumSearchLength > 0`, it takes precedence over `EmptyQueryBehavior`. Manually opening the popup before the minimum is reached shows the instruction rather than the full source.

With `TypingPopupBehavior.AutoOpen`, typing auto-opens only after the query reaches `MinimumSearchLength`. Clearing text while the popup is closed does not auto-open it.

## 11. Commit resolver

All commit/navigation paths share one resolver:

```text
ResolvePendingText
1. empty after TextNormalizer?
   -> clear selection, commit null
2. exact DisplayMember/item-text match?
   -> select existing item
3. unmatched
   -> apply UnmatchedTextBehavior
```

### 11.1 RestorePreviousSelection

Discard pending text, restore `Text = CommittedDisplayText`, keep the previous committed value, close the popup, and allow navigation. No selection-changed event is raised if the value did not change.

### 11.2 KeepFocusWithValidationError

Do not commit. Set inherited `ValidationState = BootstrapValidationState.Invalid`, set `ValidationMessage = InvalidTextMessage`, open or reopen the popup, highlight the best current candidate, retain editor focus, and cancel DataGridView end-edit/navigation. Typing again clears only this lookup-generated transient validation state/message.

### 11.3 CommitAndAdd

If exact matching finds an existing item, select that item without adding a duplicate.

Otherwise raise `CreateItemFromText` for object data sources. For a string-item source, if no handler exists, the built-in shortcut may create `OriginalText.Trim()` directly.

On successful item creation:

```text
adapter.Add(item)
-> verify logical source contains/accepts the item
-> select item
-> commit value
-> refresh result projection
-> navigate if the triggering action requires navigation
```

If creation is unavailable, no item is returned, the source is read-only, the add capability is unavailable, or the created item's resolved `ValueMember` is null, `CommitAndAdd` falls back to `KeepFocusWithValidationError`. Raw unmatched text is never committed without a corresponding datasource item.

Unexpected exceptions thrown by application event handlers or actual `Add()` operations are not silently converted into validation failures; they propagate after internal busy-state cleanup.

## 12. Public events

The lookup keeps event semantics intentionally smaller than `BootstrapSelect` because selection is single-only.

Conceptual events:

```csharp
public event EventHandler? SelectedValueChanged;
public event EventHandler<BootstrapLookupSelectionCommittedEventArgs>? SelectionCommitted;
public event EventHandler<BootstrapLookupHighlightedItemChangedEventArgs>? HighlightedItemChanged;
public event EventHandler? ResultsChanged;
public event EventHandler<BootstrapLookupRefreshRequestedEventArgs>? RefreshRequested;
public event EventHandler<BootstrapLookupAddNewRequestedEventArgs>? AddNewRequested;
public event EventHandler<BootstrapLookupCreateItemFromTextEventArgs>? CreateItemFromText;
```

`SelectedValueChanged` fires only when the committed logical value actually changes. Typing, search, highlighting, popup open/close, refresh, Alt+Tab, or restoration to the same committed value do not raise it.

`SelectionCommitted` is raised after all committed state is internally consistent. Conceptual reason values include Keyboard, Mouse, Programmatic, ExactMatch, CommitAndAdd, and Clear.

`CreateItemFromText` event args expose at least `OriginalText`, `NormalizedText`, `Item`, and cancellation state.

`AddNewRequested` event args expose at least `QueryText`, `NewItem`, and cancellation state.

## 13. Refresh and explicit Add New behavior

### 13.1 Refresh

Refresh keeps the current query and committed state:

```text
RefreshRequested
-> refresh/reconcile adapter/source
-> execute current query
-> preserve highlighted item if possible
-> update footer
```

Refresh never commits, rolls back, clears text, or moves the current DataGridView cell.

### 13.2 Add New

Footer Add New is an explicit application-owned create workflow, distinct from `CommitAndAdd`.

Typical flow:

```text
user clicks Add New
-> popup presentation may close
-> preserve pending query/edit state
-> raise AddNewRequested(QueryText)
-> application opens Product/Customer/etc. editor
```

On cancellation, no committed state changes and pending query is preserved.

On success, `NewItem` is automatically selected and committed. The lookup refreshes/reconciles its source, but explicit Add New does not require the framework itself to own business datasource insertion because the application create workflow may already have persisted/refreshed that entity. If source reconciliation cannot immediately find the returned item, the returned `NewItem` may still be committed by its non-null `ValueMember` (or by the item itself when `ValueMember` is empty); a later refresh can reconcile the source view. If the returned item cannot produce a valid non-null logical value, no commit occurs and the lookup remains in its prior committed state.

V1 does not automatically move to the next cell after explicit footer Add New.

## 14. Keyboard, mouse, and focus lifecycle

Hard invariant: while the popup is open for keyboard interaction, focus remains in `BootstrapLookupBox.Editor`.

`ResultsGrid`, Refresh, and Add New are not Tab stops. The controller changes result-grid current row/scroll state without focusing it.

### 14.1 Navigation keys

When popup is open:

```text
Up/Down        -> previous/next result
Home/End       -> first/last result
PageUp/PageDown-> page navigation
Enter          -> commit highlighted item, or resolve pending text if no highlight
Escape         -> discard pending text and restore committed display
```

When popup is closed:

```text
Down/F4/Alt+Down -> flush search as needed and open popup
Enter            -> ResolvePendingText or DataGridView default, based on ClosedEnterKeyBehavior
```

### 14.2 Mouse result activation

A left-click on a selectable `ResultsGrid` row commits that row immediately and closes the popup. Mouse highlighting/hover that does not activate a row must not mutate committed state. Footer clicks remain inside the lookup activation domain.

### 14.3 Enter

After a successful commit:

```text
CommitSelection            -> remain in current cell/control
CommitSelectionAndMoveNext -> delegate next-focus/cell navigation to the owner
```

If resolve fails and validation blocks the edit, navigation never occurs.

### 14.4 Tab

Tab always resolves pending text before allowing navigation:

```text
no pending text -> allow native navigation
empty           -> clear/commit null -> navigate
exact match     -> commit existing -> navigate
unmatched       -> apply unmatched-text policy
```

No implementation may hard-code `ColumnIndex + 1`; DataGridView must retain responsibility for finding the next editable cell.

### 14.5 Escape

Escape cancels pending debounce, closes the popup, restores `Text = CommittedDisplayText`, clears lookup-generated transient validation, and leaves committed selection unchanged.

## 15. Activation-domain behavior

The lookup activation domain includes the editor, popup surface, result grid, Refresh, and Add New controls. Clicking inside this domain must not be mistaken for ending the lookup edit session.

Application deactivation such as Alt+Tab has presentation-only semantics:

```text
close popup
preserve Text
preserve HasPendingText
preserve SelectedItem / SelectedValue
preserve committed value
no validation
no commit
no rollback
no navigation
```

When the application is activated again, the popup remains closed. It reopens only after a new user trigger such as typing under AutoOpen, Down, F4, Alt+Down, or clicking the dropdown affordance.

Clicking another control/cell inside the same application is different: it is an attempt to end the edit and therefore runs the normal pending-text resolver. `KeepFocusWithValidationError` cancels that transition and returns focus to the editor.

## 16. Data adapter and member access

`BootstrapLookupDataAdapter` is internal and centralizes:

- enumeration of supported local sources;
- member metadata access;
- logical identity/value extraction;
- exact-match lookup;
- add capability detection and mutation;
- source refresh/reconciliation;
- source-change subscriptions.

Supported V1 shapes include `BindingSource`, `BindingList<T>`, `IList`, `IListSource`, arrays, and equivalent local list implementations.

The adapter must not implement search by assigning `BindingSource.Filter`, because plain `BindingList<T>` does not naturally implement `IBindingListView`, and because filtering the caller's source would mutate currency/position.

Member access should prefer cached `PropertyDescriptor` metadata where appropriate rather than repeated reflection on every search keystroke. Null display/search member values are treated as empty text and are not errors. A null resolved logical value is not a valid selection value because null is reserved for the cleared-selection state.

Invalid `DisplayMember`, `ValueMember`, or `SearchMembers` should fail early when metadata is available. Setting a member before `DataSource` may defer validation until the source is assigned.

## 17. Validation and errors

Configuration errors such as invalid enum values, negative debounce/minimum length, or null required comparer/normalizer values fail fast with appropriate argument exceptions.

Predictable capability limitations are handled as lookup outcomes rather than UI-thread crashes. Example: `CommitAndAdd` against a read-only source falls back to validation.

Application/business exceptions thrown from `CreateItemFromText`, `AddNewRequested`, Refresh handlers, or unexpected source mutation operations are not swallowed. Internal reentrancy/busy flags must be restored using `try/finally` before exceptions propagate.

Lookup-generated validation is tracked separately from external/application validation so that new typing can clear only the transient lookup error without erasing unrelated business validation. `ValidationMessage` refers only to the lookup-generated transient message; the inherited `ValidationState` remains the visual validation state.

A committed value that is no longer present in the lookup datasource is not automatically cleared. The raw committed value is preserved until the user explicitly changes/clears it.

## 18. DataGridView integration

### 18.1 `BootstrapLookupEditingControl`

```csharp
internal sealed class BootstrapLookupEditingControl
    : BootstrapLookupBox, IDataGridViewEditingControl
```

It implements the normal WinForms editing-control contract, including the owning DataGridView, formatted value, row index, value-changed flag, `PrepareEditingControlForEdit`, style application, and `EditingControlWantsInputKey`.

`EditingControlWantsInputKey` must retain lookup keys such as arrows, PageUp/PageDown, Home/End, F4, Enter, and Escape when lookup semantics require them. Tab and navigation remain coordinated with DataGridView rather than simulated.

### 18.2 `BootstrapLookupCell`

`BootstrapLookupCell` is an internal implementation detail in V1. The public `BootstrapLookupColumn` creates and owns its cell template; consumers are not expected to instantiate or subclass the lookup cell directly.

The cell sets `EditType` to `BootstrapLookupEditingControl`, initializes the editor with the raw cell value, and maps raw value <-> formatted display text using `ValueMember` / `DisplayMember`.

### 18.3 `BootstrapLookupColumn`

`BootstrapLookupColumn : DataGridViewColumn` is the normal consumer-facing grid API.

It exposes lookup configuration such as:

```text
DataSource
DisplayMember
ValueMember
LookupColumns
SearchMembers
UnmatchedTextBehavior
EmptyQueryBehavior
TypingPopupBehavior
EnterKeyBehavior
ClosedEnterKeyBehavior
SearchDebounceMilliseconds
MinimumSearchLength
DropDownWidth
MaxDropDownHeight
ShowRefreshButton
ShowAddNewButton
```

Column configuration is copied into the reused editing control when a cell enters edit mode.

`DataPropertyName` is the property on the row model that stores the raw lookup identity. `ValueMember` is the property on each lookup item that provides that identity. `DisplayMember` is presentation text.

Example:

```text
DataPropertyName = OrderLine.ProductId
ValueMember      = Product.Id
DisplayMember    = Product.Name

cell.Value          = 125
cell.FormattedValue = "Cà phê rang xay"
```

### 18.4 Column events

Grid-column events include row/cell context so one column can serve all rows. Relevant conceptual events mirror selection committed, Add New, Create From Text, and Refresh, with `DataGridView`, `RowIndex`, and `ColumnIndex` in event args.

## 19. DataGridView new-row lifecycle

The intended data model is native WinForms binding:

```csharp
BindingList<OrderLine> lines = new();
BindingSource source = new() { DataSource = lines };
grid.DataSource = source;
grid.AllowUserToAddRows = true;
```

New-row flow:

```text
new-row placeholder
-> BeginEdit
-> BindingSource.AddNew / BindingList item creation
-> BootstrapLookupEditingControl initialized with null value
-> user searches/selects
-> committed SelectedValue changes
-> EditingControlValueChanged = true
-> DataGridView.NotifyCurrentCellDirty(true)
-> DataGridView parses/pushes raw value into row model
-> native row lifecycle continues
```

No `DataTable`, manual `Rows.Add`, or manual creation of DataGridView editing rows is required.

## 20. Dirty-state rules

Do not mark the grid dirty for:

- typing only;
- search result changes;
- highlight changes;
- popup open/close;
- Refresh;
- Alt+Tab/deactivation;
- restoring the same previous committed selection.

Mark the grid dirty only when committed logical value changes, including selecting a different item, clearing a non-null value, `CommitAndAdd`, or successful explicit Add New.

Value changes are compared with `EqualityComparer<object>.Default` in V1 before `EditingControlValueChanged` and `NotifyCurrentCellDirty(true)` are raised.

## 21. Commit ordering and reentrancy

Successful commit ordering is fixed:

```text
1. resolve target item/value
2. validate target
3. update committed lookup state
4. synchronize Text / CommittedDisplayText
5. set HasPendingText = false
6. raise SelectedValueChanged if value changed
7. notify DataGridView dirty if applicable
8. raise SelectionCommitted
9. close popup
10. navigate only if the active policy requires it
```

Event handlers therefore observe a fully consistent committed state.

Internal guards should distinguish operations such as committing selection, synchronizing text, refreshing results, and creating items. A single generic `_busy` flag is insufficient because these operations have different reentrancy semantics.

## 22. Disposal and lifecycle safety

Disposal must:

- stop/dispose debounce timer;
- unsubscribe datasource events;
- unsubscribe theme and form/application lifecycle events;
- close/dispose popup controller/content;
- release overlay/activation-domain subscriptions;
- prevent late source-change callbacks from touching disposed controls.

Popup placement must use the existing DPI-aware overlay placement/collision infrastructure. If available height is constrained, the result grid scrolls while the footer remains visible.

## 23. Public vs internal extensibility boundary

Public in V1:

```text
BootstrapLookupBox
BootstrapLookupColumn
BootstrapLookupColumnDefinition
BootstrapLookupColumnDefinitionCollection
BootstrapLookupSearchMemberCollection
public enums and event args
```

Internal in V1:

```text
BootstrapLookupCell
BootstrapLookupEditingControl
BootstrapLookupDropDownController
BootstrapLookupDropDownContent
BootstrapLookupFooter
BootstrapLookupDataAdapter
BootstrapLookupSearchEngine
BootstrapLookupSearchResult
BootstrapLookupMemberAccessor
```

No public async provider, ranking strategy, tokenizer, generic lookup type, or custom-footer action model is introduced until a real use case requires it.

## 24. Testing strategy

Testing is split into four layers.

### 24.1 Pure logic tests

Search-engine tests cover:

- empty query ShowAll / ShowNone;
- minimum search length;
- accent-insensitive Vietnamese search;
- case-insensitive search;
- multi-token AND matching;
- tokens matching different members;
- Exact / StartsWith / WordStart / Contains ranking;
- DisplayMember priority;
- SearchMembers priority;
- stable source order;
- null member values.

Exact-match tests separately cover default Trim + case-insensitive semantics, custom `TextNormalizer`, custom `TextComparer`, and the rule that search normalization does not change exact-match semantics.

### 24.2 Control/state tests

Cover:

- typing preserves committed `SelectedValue`;
- highlight changes do not commit;
- Enter/mouse commit;
- Escape restoration;
- clear-to-null;
- all unmatched-text modes;
- `CommitAndAdd` success, duplicate prevention, and failure fallback;
- event ordering and fully consistent state observed by handlers.

### 24.3 Binding and DataGridView integration tests

Datasource matrix includes:

```text
BindingList<T>
BindingSource -> BindingList<T>
List<T>
array
BindingList<string>
read-only source
```

Assertions include:

- lookup search/highlight does not unexpectedly change `BindingSource.Position`;
- correct raw `ValueMember` is committed;
- dependent row-model updates can be performed from column events;
- Tab skips hidden/read-only cells according to native DataGridView behavior;
- Enter behavior honors both policies;
- new-row placeholder creates a real `BindingList` item through native AddNew lifecycle.

### 24.4 Real keyboard/focus/window interaction tests

This layer is mandatory for bug classes involving Windows message routing. Tests must send/route actual keyboard input to the real editor rather than directly calling internal popup methods.

Required keys:

```text
Down
Up
Home
End
PageDown
PageUp
Enter
Escape
Tab
F4
Alt+Down
```

Required assertions include focused control, popup visibility, highlighted row, committed value, DataGridView current cell, and dirty state.

Regression tests must cover:

- focus remains in the editor while arrows/PageDown navigate popup results;
- valid Tab commits and moves to next editable cell;
- invalid Tab under `KeepFocusWithValidationError` keeps the same cell focused and reopens popup;
- Alt+Tab closes popup but preserves pending text and committed value;
- reactivation does not auto-open popup;
- Down/F4 after reactivation reopens and rebuilds/preserves current query results;
- clicking ResultsGrid/Refresh/Add New stays inside the lookup edit activation domain;
- clicking another control in the same app resolves pending text;
- application deactivation performs presentation-only close;
- disposal while popup is open leaves no popup/timer/source-event callbacks behind.

Rule: if a defect can exist only because of WinForms/Windows message routing, its regression test must also exercise that routing.

## 25. Performance baseline

V1 local search should remain usable for approximately 1,000, 5,000, and 10,000-item local sources without pathological allocation/reflection behavior.

Implementation should avoid:

- uncached reflection/member lookup for every member on every keystroke;
- rebuilding result-grid columns for every search;
- mutating the original `BindingSource` to filter;
- marking the DataGridView dirty for transient query/highlight state.

No hard millisecond SLA is part of V1, but these sizes form a regression baseline.

## 26. Acceptance criteria

The feature is not complete merely because a demo renders. V1 acceptance requires all of the following:

- keyboard-only lookup flow works;
- mouse selection works;
- editor focus remains stable while popup keyboard navigation occurs;
- Tab, Enter, Escape, F4, Alt+Down, arrows, PageUp/PageDown, Home/End behave as specified;
- strict lookup cannot silently commit unmatched raw text;
- `CommitAndAdd` is atomic: committed text always corresponds to a real datasource item;
- explicit Add New auto-selects and commits the returned item;
- Refresh preserves query and does not commit;
- footer position/total reflects the current result set;
- `BindingSource.Position` is not changed merely by search/highlight;
- native DataGridView new-row editing works with `BindingSource`/`BindingList<T>`;
- pending typing does not dirty the row/cell;
- actual committed value changes do notify DataGridView dirty state;
- Alt+Tab closes popup without commit/rollback/validation and reactivation does not auto-open;
- no `SendKeys` navigation is used;
- no editor-overlay workaround is used;
- lifecycle/disposal tests pass on both target frameworks where applicable.

## 27. Approved default configuration summary

```text
Selection                  = Single only
DataSource API              = non-generic object DataSource
Logical value comparer      = EqualityComparer<object>.Default
SearchDebounceMilliseconds  = 150
MinimumSearchLength         = 0
EmptyQueryBehavior          = ShowAll
TypingPopupBehavior         = AutoOpen
UnmatchedTextBehavior       = RestorePreviousSelection
EnterKeyBehavior            = CommitSelection
ClosedEnterKeyBehavior      = ResolvePendingText
ShowRefreshButton           = false
ShowAddNewButton            = false
Dropdown affordance         = always visible in V1
Search normalization        = Trim + case-insensitive + remove diacritics + Đ/đ mapping
Exact-match normalization   = Trim
Exact-match comparer        = CurrentCultureIgnoreCase
Empty text                  = clear/commit null
Null logical value          = reserved for cleared selection
Alt+Tab                     = close popup only; preserve pending edit
Reactivation                = keep popup closed
Highlight refresh           = preserve previous highlight when possible
Search ranking              = Exact > StartsWith > WordStart > Contains
Multi-token search          = AND across tokens; tokens may match different members
```

These defaults are part of the approved V1 UX and should not be changed during implementation without an explicit design update.
