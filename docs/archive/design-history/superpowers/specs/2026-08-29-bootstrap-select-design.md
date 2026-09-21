# BootstrapSelect Design Specification

**Status:** Approved design
**Date:** 2026-08-29
**Component:** `BootstrapSelect`
**Project:** `MyDmsVn.Bootstrap5WinFormUI`
**Inspiration:** Select2-style searchable dropdown/select behavior adapted to native WinForms

## 1. Purpose

`BootstrapSelect` is a new composite WinForms control that provides an advanced searchable select experience comparable in capability to Select2 while following this repository's Bootstrap-inspired visual language, shared overlay infrastructure, dual-target compatibility requirements, and WinForms lifecycle conventions.

The control is intentionally separate from `BootstrapComboBox`.

`BootstrapComboBox` remains a native-backed `ComboBox` whose purpose is to preserve native WinForms item storage, binding, selection, editing, keyboard, auto-complete, and drop-down behavior. `BootstrapSelect` is a dedicated subsystem for richer behavior that a native `ComboBox` cannot support cleanly, including multiple selection, chips, custom result rendering, local filtering, async paged providers, grouping, custom values, loading/error states, and an owner-rendered result viewport.

This specification records the architecture approved before implementation. It is the source of truth for the implementation plan unless a later approved design change supersedes it.

## 2. Design goals

The first version must support:

- single selection;
- multiple selection with removable chips;
- a dedicated `BootstrapSelectItem` model;
- identity by immutable item value;
- local item mode;
- async provider mode;
- local search with replaceable matching logic;
- debounced async search;
- request cancellation and stale-result protection;
- paged async results and infinite scrolling;
- grouping by item metadata;
- disabled items;
- optional custom values/tags;
- custom result and selection rendering through a renderer abstraction;
- loading, empty, error, retry, and load-more states;
- keyboard and mouse parity;
- shared overlay placement/collision behavior;
- Bootstrap theme integration;
- DPI-aware geometry;
- RTL-aware layout foundation;
- WinForms Designer safety;
- lifecycle/disposal safety;
- compatibility with `net48` and `net8.0-windows`.

The objective is not to clone Select2's JavaScript API. The objective is to provide the same class of user experience with APIs and lifecycle behavior appropriate for WinForms.

## 3. Explicit non-goals for v1

The following are intentionally outside the first version:

- `DataSource`, `DisplayMember`, or `ValueMember` compatibility;
- built-in HTTP/AJAX configuration;
- REST URL, headers, HTTP methods, or JSON mapping;
- `IAsyncEnumerable` as the provider contract;
- arbitrary hosted WinForms controls per result row;
- variable-height or per-item-measured result rows;
- HTML-like templates;
- hierarchical/tree selection;
- nested groups;
- drag-and-drop chip reordering;
- automatic result caching shared across controls;
- server-side CRUD behavior;
- cursor-based paging abstractions;
- a native `ComboBox` compatibility mode;
- changes to the existing `BootstrapComboBox` architecture.

These exclusions prevent the first version from becoming a second networking framework, a general-purpose item-hosting framework, or a compatibility wrapper around unrelated controls.

## 4. Alternatives considered

### 4.1 Extend or compose `BootstrapComboBox`

Rejected. The current `BootstrapComboBox` contract deliberately preserves native `ComboBox` semantics. Multi-select, chips, custom popup rows, async paging, custom-value actions, and richer error/loading states would conflict with that contract and would make compatibility fragile.

### 4.2 Build the feature on `BootstrapDropdown`

Rejected as the primary architecture. `BootstrapDropdown` is menu-oriented. Menu item semantics are not a good fit for large searchable data sets, selected-state reconciliation, async paging, result virtualization, or text-entry focus behavior.

### 4.3 Dedicated select subsystem

Approved. `BootstrapSelect : UserControl` owns selection presentation and delegates popup placement to the existing overlay infrastructure. Search, selection, rendering, and popup behavior remain separate internal concerns.

## 5. High-level architecture

```text
BootstrapSelect : UserControl
|
+-- selection surface
|   +-- single selected value / placeholder
|   +-- multiple-selection chips
|   +-- clear action
|   +-- drop-down indicator
|
+-- selection state
|
+-- search/data controller
|
+-- drop-down controller
    |
    +-- existing Bootstrap overlay infrastructure
    |
    +-- BootstrapSelectDropDownContent
        +-- real WinForms search TextBox
        +-- BootstrapSelectResultsView
            +-- group headers
            +-- selectable item rows
            +-- create-custom-value row
            +-- loading rows
            +-- empty/instruction rows
            +-- error/retry rows
            +-- load-more state
```

The major boundaries are:

```text
Control     -> public API, orchestration, selection-surface behavior
Selection   -> identity, selected state, ordering, event-safe mutations
Provider    -> async data retrieval only
Matcher     -> local filtering only
Renderer    -> presentation only
ResultsView -> scrolling, row layout, hit testing, painting
Overlay     -> placement, collision, owner tracking, popup lifetime
```

## 6. Public model

### 6.1 `BootstrapSelect`

The public control is a `UserControl`, not a `ComboBox` subclass.

The approved public surface includes the following conceptual members. Exact XML documentation and repository-specific attributes are part of implementation work.

```csharp
public class BootstrapSelect : UserControl
{
    public BootstrapSelectMode SelectionMode { get; set; }

    public BootstrapSelectItemCollection Items { get; }
    public IBootstrapSelectDataProvider? DataProvider { get; set; }

    public BootstrapSelectItem? SelectedItem { get; set; }
    public IReadOnlyList<BootstrapSelectItem> SelectedItems { get; }

    public object? SelectedValue { get; set; }
    public IReadOnlyList<object> SelectedValues { get; }

    public IEqualityComparer<object> ValueComparer { get; set; }

    public string Placeholder { get; set; }
    public bool AllowClear { get; set; }
    public bool AllowCustomValues { get; set; }
    public bool CloseOnSelect { get; set; }
    public bool SearchEnabled { get; set; }

    public int MinimumSearchLength { get; set; }
    public TimeSpan SearchDebounce { get; set; }
    public int PageSize { get; set; }

    public int DropDownWidth { get; set; }
    public int MaxDropDownHeight { get; set; }
    public int ResultRowHeight { get; set; }
    public int MaximumSelectionRows { get; set; }

    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }

    public IBootstrapSelectMatcher Matcher { get; set; }
    public IBootstrapSelectRenderer Renderer { get; set; }

    public Func<string, BootstrapSelectItem?>? CustomValueFactory { get; set; }
}
```

The implementation may refine names only when necessary to follow existing repository conventions. Any such refinement must preserve the approved semantics.

### 6.2 Selection mode

```csharp
public enum BootstrapSelectMode
{
    Single,
    Multiple
}
```

`Single` is the default.

`CloseOnSelect` has a mode-sensitive effective default:

- Single: `true`;
- Multiple: `false`.

An explicit caller assignment overrides the mode default.

### 6.3 `BootstrapSelectItem`

Each item has a stable logical identity represented by `Value`.

```csharp
public class BootstrapSelectItem
{
    public BootstrapSelectItem(object value, string text);

    public object Value { get; }
    public string Text { get; set; }
    public bool Disabled { get; set; }
    public string? Group { get; set; }
    public IconDescriptor? Icon { get; set; }
    public object? Tag { get; set; }
}
```

Rules:

- `Value` is required and must not be `null`.
- `Value` is immutable after construction.
- `Text` is required.
- `Disabled` prevents a new selection but does not force removal of an existing selection.
- `Group` is presentation metadata and may be `null`.
- `Icon` uses the repository's existing source-neutral icon infrastructure.
- `Tag` is caller-owned metadata; the control does not interpret it.

The control does not own or dispose caller-created items.

## 7. Identity and selection semantics

Logical item identity is defined exclusively by:

```text
ValueComparer.Equals(existing.Value, candidate.Value)
```

Reference equality is never the selection identity rule.

This is required because remote searches and different pages may produce new `BootstrapSelectItem` instances for the same logical value.

### 7.1 Selection independence

Selection state exists independently from the current result set.

A selected item remains selected when:

- search text changes;
- the current remote page changes;
- the selected value is not present in the current result page;
- the popup closes;
- local results are filtered out.

The internal selection state stores a logical value plus an item snapshot sufficient to keep selected text/chips visible.

If a newer provider result has the same `Value` and updated presentation metadata, the selected snapshot may be refreshed without raising `SelectionChanged`, because the logical selection did not change.

### 7.2 Selection order

Multiple-selection chip order follows the order in which items were selected. Sorting or regrouping results does not reorder selected chips.

### 7.3 Programmatic operations

Approved imperative operations include:

```csharp
bool Select(BootstrapSelectItem item);
bool SelectValue(object value);
bool Deselect(BootstrapSelectItem item);
bool DeselectValue(object value);
void ClearSelection();
```

`SelectedItems` is read-only to callers so selection mutations cannot bypass identity checks, duplicate prevention, events, or repaint logic.

### 7.4 Mode changes

Single -> Multiple:

- preserve the current selected item, if any.

Multiple -> Single:

- preserve the first selected item;
- remove later selections through the normal deselection rules;
- perform the transition atomically so cancellation cannot leave a partially converted state.

## 8. Selection events

The public event model includes cancellable pre-change events, post-change item events, and a consolidated selection event.

Conceptually:

```csharp
Selecting
Selected
Deselecting
Deselected
SelectionChanged

DropDownOpened
DropDownClosed

SearchStarted
SearchCompleted
SearchFailed
```

Selection event ordering is deterministic.

### Select

```text
Selecting(item)
  -> cancel?
  -> commit state
  -> Selected(item)
  -> SelectionChanged
```

### Deselect

```text
Deselecting(item)
  -> cancel?
  -> commit state
  -> Deselected(item)
  -> SelectionChanged
```

### Clear multiple selection

A clear operation evaluates per-item cancellation, commits allowed removals, raises the corresponding `Deselected` events, and raises `SelectionChanged` once for the logical batch rather than once per removed item.

Event args should include an approved change-reason concept such as:

```text
Programmatic
Mouse
Keyboard
Clear
ChipRemove
CustomValue
ModeChange
```

The final enum/member names may be aligned with existing repository naming conventions during implementation planning.

## 9. Local and async modes

The control has two mutually exclusive data modes.

### 9.1 Local mode

```text
DataProvider == null
-> Items + Matcher
```

`Items` is a dedicated `BootstrapSelectItemCollection` that notifies the owning control internally when local items change.

Local search is immediate by default and is not debounced.

### 9.2 Async mode

```text
DataProvider != null
-> async provider mode
```

When a provider exists, local `Items` are preserved but ignored. The control never silently merges the two sources and never destroys the caller's local item collection merely because a provider was assigned.

Replacing the provider while the popup is open cancels the current query, invalidates the current generation, resets remote result state, preserves selection, and restarts the current search against the new provider when allowed by `MinimumSearchLength`.

## 10. Local matching

The default matcher performs case-insensitive text matching against `BootstrapSelectItem.Text`.

The caller may replace it with `IBootstrapSelectMatcher` to support cases such as:

- product code plus name;
- aliases;
- accent-insensitive Vietnamese search;
- custom normalization;
- custom ranking/matching policy.

The matcher applies only to local mode. Remote ranking/filtering belongs to the provider.

Conceptually:

```csharp
public interface IBootstrapSelectMatcher
{
    bool IsMatch(BootstrapSelectItem item, string searchText);
}
```

The built-in implementation is a text matcher, tentatively named `BootstrapSelectTextMatcher`.

## 11. Async provider contract

The control does not know about HTTP, JSON, REST, databases, or service transports.

```csharp
public interface IBootstrapSelectDataProvider
{
    Task<BootstrapSelectPage> SearchAsync(
        BootstrapSelectQuery query,
        CancellationToken cancellationToken);
}
```

The query contains at least:

```text
SearchText
Page
PageSize
```

Paging is one-based: page 1 is the first page.

The result contains:

```text
Items
HasMore
```

`HasMore` is authoritative. The control must not infer additional pages from `Items.Count == PageSize`.

The provider is caller-owned and is not disposed by `BootstrapSelect`.

## 12. Async lifecycle and concurrency

Async correctness is a first-class requirement.

### 12.1 State model

The drop-down/search subsystem has the following logical states:

```text
Closed
Opening
Idle
Debouncing
Loading
ShowingResults
LoadingMore
ShowingError
Closing
```

The exact internal enum is not public API.

### 12.2 Debounce

Remote search uses `SearchDebounce`. A WinForms UI-thread timer is preferred for the debounce boundary because it is a UI concern and avoids unnecessary cross-thread behavior.

Local filtering is immediate by default.

### 12.3 Cancellation

A new logical query cancels the prior query's `CancellationTokenSource`.

Cancellation also occurs when:

- the popup closes;
- the provider changes;
- the control is hidden/disabled in a way that closes the popup;
- the control is disposed.

Cancellation by itself is not sufficient to protect correctness.

### 12.4 Request generation

Every logical query is assigned a monotonically increasing generation. A continuation may update current state only if its generation is still current.

This protects against providers that ignore cancellation or complete after cancellation races.

Example:

```text
generation 12 -> "a"
generation 13 -> "ab"
generation 14 -> "abc"

result for 12 arrives last
-> discard
```

### 12.5 UI-thread boundary

Providers may complete on any thread. `BootstrapSelect` owns the responsibility to marshal UI-facing state changes back to the UI thread before modifying controls or raising UI-facing events.

A shared internal helper should centralize this behavior instead of scattering `InvokeRequired` logic throughout the subsystem.

### 12.6 Disposal safety

Async continuations must verify lifecycle and generation state before mutating UI.

Closing or disposing the popup increments/invalidate generation, cancels the active CTS, and stops debounce activity before UI handles are destroyed.

Core async methods should return `Task`. `async void` is restricted to WinForms event-handler boundaries.

## 13. Paging and infinite scroll

A new query always resets to page 1.

The control loads the next page when the viewport approaches the end of loaded results, using an internal threshold rather than waiting for the absolute final scroll position.

Guards prevent overlapping load-more requests.

When page 1 is loading:

- stale results from the previous query are cleared;
- the popup shows a loading state.

When a later page is loading:

- existing results remain visible;
- a load-more indicator is appended.

When a later page fails:

- existing pages remain usable;
- an inline retry state is shown;
- retry requests the same failed page;
- the current page advances only after a successful response.

Duplicate values returned across pages are deduplicated by `ValueComparer`.

## 14. Grouping

Grouping is represented by nullable `BootstrapSelectItem.Group` metadata.

The result normalizer produces group-header rows around matching items.

Rules:

- group headers are not selectable;
- a group with no matching local items is hidden;
- consecutive remote pages with the same group name do not render duplicate adjacent headers;
- grouping is presentation structure, not nested data ownership.

A separate `BootstrapSelectGroup` object hierarchy is intentionally not used in v1 because it complicates remote paging and merging.

## 15. Custom values/tags

Custom values are opt-in.

```text
AllowCustomValues = false by default
```

When enabled, the results may show an action conceptually equivalent to:

```text
+ Create "ABC"
```

Creation goes through:

```csharp
Func<string, BootstrapSelectItem?>? CustomValueFactory
```

Returning `null` rejects creation.

The factory is synchronous in v1. Async create/update server workflows are intentionally outside the first version.

The custom-value action is suppressed when an exact textual match already exists. Exact-match suppression does not use the potentially fuzzy/custom local matcher; it uses a dedicated case-insensitive textual equality rule.

## 16. Renderer architecture

Custom rendering uses an abstraction rather than per-item child controls or raw public owner-draw events.

Conceptually:

```csharp
public interface IBootstrapSelectRenderer
{
    void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context);
    void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context);
    void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context);
    void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context);
}
```

The renderer receives public render contexts containing semantic data, bounds, state, DPI, and theme-derived presentation information. It must not receive internal result-row implementation types.

Renderer responsibilities:

- visual presentation only.

Renderer non-responsibilities:

- selection mutation;
- popup opening/closing;
- search mutation;
- provider calls;
- scrolling;
- child-control creation;
- lifecycle ownership.

The default renderer uses the project's theme and icon infrastructure and must not hard-code semantic colors when equivalent theme tokens/roles already exist.

Caller-provided renderers are caller-owned and are not disposed by the control.

## 17. Visual structure

### 17.1 Single selection

```text
+--------------------------------------+ 
| icon?  Selected text            x  v |
+--------------------------------------+
```

When empty, the selected text area shows `Placeholder`.

Long selected text is ellipsized rather than increasing single-select height.

### 17.2 Multiple selection

```text
+------------------------------------------------+
| [Customer A x] [Customer B x] [Customer C x] v |
+------------------------------------------------+
```

Chips wrap to additional rows. The control may grow until `MaximumSelectionRows` is reached. Beyond that limit, the selection surface uses an internal overflow strategy instead of growing indefinitely.

A single overly long chip is clamped to available width and ellipsized.

Selected-item order is selection order.

### 17.3 Clear action

The clear action is visible only when:

```text
AllowClear
&& selection count > 0
&& Enabled
```

Its visual glyph may be small, but its hit target must remain large enough for reliable high-DPI interaction.

### 17.4 Search editor

The popup uses a real WinForms `TextBox`-style editor rather than a hand-painted text editor. This preserves caret behavior, clipboard support, text selection, IME, Unicode, and Vietnamese input.

Even in multiple mode, the approved v1 architecture keeps search input in the popup instead of embedding an editable text field between chips.

Typing a printable character while the closed control is focused opens the popup, focuses the search editor, and forwards the typed character.

## 18. Results viewport

The result list is an owner-rendered virtual viewport, tentatively `BootstrapSelectResultsView : Control`.

It does not create a WinForms child control for every row.

Responsibilities include:

- logical row layout;
- scroll position;
- visible-row calculation;
- hit testing;
- hot/highlighted state;
- keyboard navigation;
- painting;
- load-more threshold signaling;
- accessibility mapping where practical.

Logical row kinds include:

```text
GroupHeader
Item
CreateValue
Loading
LoadMoreError
Empty
Instruction
Error
```

### 18.1 Fixed row metrics

V1 uses one caller-configurable, DPI-scaled uniform height for every popup result row. `ResultRowHeight` is expressed in logical pixels at 96 DPI, defaults to `32`, and must be greater than zero. Its effective device height is calculated through `DpiScaler`. Item, group, loading, empty, instruction, error/retry, and create-value rows all use the same effective height for a given control.

Changing `ResultRowHeight` after popup creation reapplies presentation. If the popup is open, it remains open and its bounds are recomputed and repositioned without recreating the popup or resetting logical navigation state.

Variable-height or per-item-measured result rows remain intentionally excluded because one uniform metric keeps scrolling, hit testing, virtualization, PageUp/PageDown, and paging thresholds deterministic. The renderer contract does not include a measurement callback, hosted row controls, or an HTML-like template layer.

### 18.2 Result states

Item presentation can combine flags such as:

```text
Normal
Hot
Highlighted
Selected
Disabled
```

`Selected` and `Highlighted` are independent and may both apply to one item.

Selected values remain visible in results and receive a clear selected marker rather than being removed from the result list.

## 19. Popup and overlay behavior

`BootstrapSelect` reuses the repository's existing overlay infrastructure, including the shared placement/collision engine and overlay popup host. It must not implement a second Popper-like positioning engine.

The preferred placement is conceptually bottom-start with top-start as a fallback.

Expected behavior:

- show below the owner when space permits;
- flip above when below space is insufficient and above is more suitable;
- shift/clamp within the monitor working area;
- track owner move/resize;
- remeasure/reposition when DPI changes;
- close when clicking outside;
- close on `Esc`;
- close on successful single selection when effective `CloseOnSelect` is true;
- remain open by default after multiple selection;
- close when the owner becomes hidden/disabled/disposed as appropriate.

`DropDownWidth == 0` means automatic width with the owner control width as the minimum/default reference.

Popup creation is lazy. The popup and its content are reused while the owner control remains alive and are disposed with the owner; they are not recreated for every keystroke or every open/close cycle.

## 20. Focus behavior

Opening the popup moves input focus to the search editor.

Closing with `Esc` or an internal selection operation may restore focus to `BootstrapSelect`.

Closing because the user clicked another control must not steal focus back from that destination control.

Search text is cleared when the popup closes in v1. Selection remains unchanged.

When `MinimumSearchLength == 0`, opening an async select may immediately request page 1 using an empty query.

When `MinimumSearchLength > 0`, opening shows an instruction state until enough characters are entered.

## 21. Keyboard behavior

The design requires full keyboard parity for primary actions.

### Closed

- `Alt+Down`: open;
- `F4`: open;
- `Enter`: open;
- `Space`: open when the surface is focused;
- printable input: open and search;
- `Delete`: clear single selection when clear is allowed;
- `Backspace`: in multiple mode, operate on the last chip according to the focused-chip rule.

### Open

- `Down`: next selectable result;
- `Up`: previous selectable result;
- `Home`: first selectable result;
- `End`: last selectable currently loaded result;
- `PageDown` / `PageUp`: page the viewport;
- `Enter`: select or toggle the highlighted result;
- `Esc`: close;
- `Tab`: close and continue normal tab traversal;
- `Ctrl+A` inside the search editor selects search text rather than selecting all results.

Group headers, disabled rows, loading rows, error rows without an action, and other non-selectable rows are skipped by selection navigation.

A new search query resets highlight; after results arrive, the first selectable result may become highlighted without becoming selected.

Loading additional pages must not unexpectedly change the current highlight.

## 22. Mouse behavior

Clicking most of the selection surface opens the popup.

Dedicated hit targets such as clear and chip-remove actions perform their own operations without also opening the popup.

Single mode:

- clicking an unselected result selects it;
- clicking the already-selected logical value does not raise a false `SelectionChanged`.

Multiple mode:

- clicking an unselected result selects it;
- clicking a selected result deselects it.

Mouse wheel input over the open result viewport scrolls that viewport and does not leak into the parent form.

## 23. Validation, theme, DPI, and RTL

### 23.1 Validation

`BootstrapSelect` uses the repository's existing `BootstrapValidationState` contract. Validation affects the outer selection surface and does not recolor the entire popup as an error surface.

### 23.2 Theme

All visual states use semantic theme roles/tokens where available. The control should reuse established values for background, foreground, border, focus, selected, disabled, spacing, control height, border thickness, radius, and icons rather than embedding duplicate constants.

A theme change while the popup is open must repaint both the owner surface and popup content without requiring the user to close/reopen the popup.

### 23.3 DPI

All geometry is expressed in logical metrics and scaled once for the current DPI.

DPI-sensitive elements include:

- border thickness;
- radius;
- icon sizes;
- clear and arrow glyph areas;
- chip padding;
- chip remove hit areas;
- result row height;
- group header height;
- search editor height;
- popup spacing.

Moving a form between monitors with different DPI while the popup is open must trigger remeasurement and placement recalculation.

The popup controller owns one effective DPI for each refresh. An owner DPI transition reapplies search-host, result-row, renderer-context, surface, and theme metrics before computing and moving popup bounds. The implementation subscribes internally to the inherited owner `DpiChangedAfterParent` event and does not add a declared public or protected DPI member to `BootstrapSelect`.

### 23.4 RTL

Layout code must not hard-code left-to-right geometry. `RightToLeft.Yes` should reverse the major horizontal selection-surface affordances and chip flow appropriately. Full localization infrastructure is not part of this component, but the architecture must not block RTL support.

## 24. Accessibility

Because this is a custom composite control, accessibility is part of the design rather than an afterthought.

The outer control should expose combo/select-like semantics including:

- focused state;
- expanded/collapsed state;
- disabled state;
- has-popup semantics;
- selected value summary.

Single mode should expose the selected text as its accessible value where appropriate.

Multiple mode should expose a useful selected-count summary and logical accessible children for selected chips/results where practical.

Logical state transitions such as loading complete, no results, and result-count changes should be announced at meaningful boundaries rather than on every repaint.

Every primary mouse action must have a keyboard-accessible equivalent, including retry and custom-value creation.

## 25. Internal component breakdown

The approved internal split is conceptually:

```text
BootstrapSelect
|
+-- BootstrapSelectSelectionState
+-- BootstrapSelectSearchController
+-- BootstrapSelectDropDownController
+-- BootstrapSelectSelectionLayout
|
+-- popup
    +-- BootstrapSelectDropDownContent
        +-- search editor
        +-- BootstrapSelectResultsView
            +-- BootstrapSelectResultLayout
            +-- BootstrapSelectResultRow model
```

Additional helpers may include:

```text
BootstrapSelectValueIdentity
BootstrapSelectSearchState
BootstrapSelectResultSet
BootstrapSelectHitTestInfo
BootstrapSelectChipLayout
```

These types are internal unless they are required by an approved public extension point.

The implementation must not expose internal state merely to make testing easier. Use the repository's existing internal-test access pattern where needed.

## 26. Ownership and disposal

`BootstrapSelect` owns and disposes only resources it creates.

Caller-owned dependencies are not disposed by the control:

- `DataProvider`;
- custom `Matcher`;
- custom `Renderer`;
- caller-created `BootstrapSelectItem` instances;
- item `Tag` objects.

Owned resources include:

- internal timers;
- active cancellation token sources;
- popup controller/content;
- results view;
- overlay instances created for this control;
- owned GDI resources;
- internal event subscriptions.

Recommended disposal order:

1. mark disposing;
2. stop debounce activity;
3. invalidate search generation;
4. cancel active query CTS;
5. detach event hooks;
6. close the popup;
7. detach owner/form tracking;
8. dispose popup content and owned overlay resources;
9. dispose timers/CTS/GDI objects;
10. call base disposal.

Closing the popup is distinct from disposing it. Normal user close/open cycles reuse the popup resources.

## 27. Designer and handle lifecycle

Designer construction must not require initialized application/global runtime state.

In design mode the control must not:

- invoke a provider;
- start runtime search timers;
- create/show overlays;
- require application startup initialization;
- install runtime-only hooks unnecessarily.

The control must tolerate handle recreation. Overlay tracking cannot assume one handle is created once for the entire lifetime.

When the control becomes hidden or disabled while the popup is open, interaction is cancelled and the popup closes while logical selection is preserved.

## 28. Layout consistency

Painting and hit testing must consume the same calculated layout output.

The selection surface should calculate a reusable layout result containing areas such as:

```text
ContentBounds
TextBounds
ClearButtonBounds
ArrowBounds
ChipBounds[]
RequiredHeight
```

The result viewport likewise maintains a layout/visible-range cache that is invalidated by result changes, DPI changes, font/theme metric changes, and viewport resize.

Hover/highlight changes should not require rebuilding structural row layout.

## 29. Error model

`IBootstrapSelectDataProvider.SearchAsync` reports failures by throwing exceptions.

`OperationCanceledException` resulting from expected cancellation is silent and does not raise `SearchFailed`.

Other exceptions produce an error state and raise `SearchFailed`.

The control intentionally does not expose network-specific status semantics.

### First-page failure

Show a full result-area failure state with a retry action.

### Later-page failure

Keep existing loaded results and append a load-more failure/retry row.

Retry uses the exact failed query/page descriptor.

## 30. Search/result invariants

The implementation must preserve these invariants:

1. `Value` is the sole logical selection identity.
2. Selection state is independent of current result state.
3. Local items and async provider results are never silently merged.
4. Provider code retrieves data only; it does not control UI.
5. Renderer code presents state only; it does not control behavior.
6. A stale async generation can never mutate current UI state.
7. Only one page request for the active query runs at a time.
8. A load-more failure never discards already loaded pages.
9. Cancellation is not reported as search failure.
10. Duplicate values never create duplicate logical selections or duplicate effective result rows.
11. Popup placement reuses shared overlay infrastructure.
12. Result virtualization does not create a child WinForms control per item.
13. `BootstrapComboBox` remains unchanged by this feature.
14. Public API must remain valid on both target frameworks.

## 31. Testing strategy

Testing is divided into five layers.

### 31.1 Pure logic tests

Cover without UI handles where possible:

- identity/comparer behavior;
- single/multiple selection;
- duplicate prevention;
- selection ordering;
- mode transitions;
- event ordering helpers;
- local matching;
- group normalization;
- remote page merge;
- deduplication;
- retry page semantics;
- generation rejection.

### 31.2 Controller tests

Use fake providers to cover:

- immediate success;
- delayed success;
- provider failure;
- cancellation honored;
- cancellation ignored;
- out-of-order completion;
- duplicate items across pages;
- empty pages;
- `HasMore` transitions;
- provider replacement;
- popup-close invalidation;
- disposal safety.

A mandatory race test is:

```text
search "a" starts
search "ab" starts
"ab" completes
"a" completes later
-> effective state remains "ab"
```

### 31.3 WinForms interaction tests

Cover on STA infrastructure where practical:

- open/close;
- keyboard navigation;
- Enter selection;
- Escape close;
- Tab traversal;
- clear;
- chip removal;
- mouse hit testing;
- focus restoration/non-stealing behavior.

### 31.4 Layout/render tests

Prefer pure geometry tests for:

- single layout;
- chip wrapping;
- chip clamping;
- RTL layout;
- hit-target geometry;
- result visible range;
- DPI scaling;
- preferred popup sizing.

Visual/manual tests complement, rather than replace, these tests.

### 31.5 Integration/demo tests

The demo application must expose scenarios for:

- local single select;
- local multiple select;
- local grouping;
- custom values;
- async single select;
- async multiple select;
- delayed provider;
- paged provider;
- provider failure/retry;
- rapid typing;
- popup placement near screen edges;
- light/dark themes;
- 100%, 125%, 150%, and 200% DPI;
- multi-monitor movement where available.

## 32. Acceptance criteria

### Selection

- Single mode never has more than one logical value.
- Multiple mode never contains duplicate values according to `ValueComparer`.
- `SelectedItem`/`SelectedValue` and multiple-selection views remain coherent.
- Selected state survives filtering, paging, and popup close/open.
- Cancellable selection events work.
- Clear batches `SelectionChanged` appropriately.
- Disabled items cannot be newly selected but can be removed if already selected.

### Local search

- Search updates immediately.
- Built-in matching is case-insensitive.
- A custom matcher can replace default matching.
- Group headers reflect only matching item groups.
- Mutating `Items` while open refreshes the local result view.
- Custom-value creation obeys exact-match suppression.

### Async search

- Debounce works.
- Old queries are cancelled when superseded.
- A provider that ignores cancellation cannot overwrite a newer query.
- Closing/disposal prevents stale continuations from updating UI.
- Query changes reset to page 1.
- `HasMore` controls whether more pages are requested.
- Only one load-more request is active at once.
- Later-page failures preserve prior results.
- Retry requests the failed page again.
- `OperationCanceledException` is not reported as failure.
- Provider completion on non-UI threads is safe.

### Popup

- Default width is at least the owner width.
- Placement flips/shifts/clamps through the shared engine.
- Owner move/resize and DPI changes reposition correctly.
- click-outside and `Esc` close correctly.
- single and multiple default close behavior matches the approved mode semantics.
- open/close cycles reuse rather than constantly recreate popup infrastructure.

### Keyboard/accessibility

A keyboard-only user can:

```text
focus -> open -> search -> navigate -> select/deselect -> clear -> retry/create -> close
```

IME/Vietnamese text input must not be broken by keyboard routing.

### Rendering/DPI

At 100%, 125%, 150%, and 200% DPI there must be no systematic clipping, overlapping chips, misaligned hit targets, stale popup dimensions, or theme-state regressions.

## 33. Performance expectations

V1 does not require extreme benchmarking, but it should remain practical for common desktop workloads.

Local mode should remain usable with approximately 1,000-5,000 items using the built-in matcher.

Remote mode should scale to large backing data sets because only pages loaded for the active query are retained.

The result viewport must render only the visible logical range rather than allocating a child control for every result.

Rapid typing must not cause stale result repaint storms.

## 34. Implementation phases

The implementation plan should preserve the following sequence unless repository dependencies require a small justified adjustment.

### Phase 1 - Core models and selection engine

- `BootstrapSelectItem`;
- `BootstrapSelectItemCollection`;
- selection mode;
- value comparer/identity;
- selection state;
- selection events;
- mode transitions;
- unit tests.

### Phase 2 - Outer control and local results

- `BootstrapSelect` shell;
- placeholder and clear affordance;
- single selection presentation;
- chip layout;
- local items;
- matcher;
- theme/DPI foundation;
- local selection-surface tests.

### Phase 3 - Popup and result viewport

- drop-down controller/content;
- shared overlay integration;
- real search editor;
- owner-rendered result viewport;
- group rows;
- scrolling;
- keyboard navigation;
- local searchable select end-to-end.

### Phase 4 - Async provider

- query/page/provider contracts;
- search controller;
- debounce;
- cancellation;
- generation protection;
- loading/failure/retry;
- UI-thread marshaling.

### Phase 5 - Pagination and advanced remote states

- infinite scrolling;
- load-more states;
- result deduplication;
- group reconciliation across pages;
- selection snapshot reconciliation;
- out-of-order/race hardening.

### Phase 6 - Custom values, accessibility, and lifecycle hardening

- custom-value action/factory;
- accessibility behavior;
- RTL verification;
- IME verification;
- DPI transitions;
- handle recreation;
- disposal-race tests.

### Phase 7 - Demo, documentation, and API baseline

- demo scenarios;
- usage documentation;
- XML docs for public API;
- public API review;
- compatibility baseline update;
- full dual-target validation.

## 35. Expected source layout

The exact folder structure may be adjusted to match the repository's existing conventions, but responsibilities should remain separated.

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/
  BootstrapSelect.cs
  BootstrapSelectItem.cs
  BootstrapSelectItemCollection.cs
  BootstrapSelectMode.cs
  BootstrapSelectEventArgs.cs
  BootstrapSelectQuery.cs
  BootstrapSelectPage.cs
  IBootstrapSelectDataProvider.cs
  IBootstrapSelectMatcher.cs
  BootstrapSelectTextMatcher.cs
  IBootstrapSelectRenderer.cs
  BootstrapSelectRenderer.cs
  BootstrapSelectRenderContext.cs

  Internal/
    BootstrapSelectSelectionState.cs
    BootstrapSelectSearchController.cs
    BootstrapSelectSearchState.cs
    BootstrapSelectDropDownController.cs
    BootstrapSelectDropDownContent.cs
    BootstrapSelectResultsView.cs
    BootstrapSelectResultRow.cs
    BootstrapSelectResultLayout.cs
    BootstrapSelectSelectionLayout.cs
    BootstrapSelectHitTestInfo.cs
```

Do not create a new assembly/project solely for this component.

## 36. Documentation examples required before completion

The final component documentation must include examples for:

### Local single

```csharp
var select = new BootstrapSelect();
select.Items.Add(new BootstrapSelectItem(1, "Customer A"));
select.Items.Add(new BootstrapSelectItem(2, "Customer B"));
```

### Multiple

```csharp
select.SelectionMode = BootstrapSelectMode.Multiple;
```

### Async provider

An example implementation of `IBootstrapSelectDataProvider` that demonstrates the contract without coupling the control to HTTP.

### Custom matcher

An example matching both a code and display text.

### Custom renderer

An example adding secondary presentation/icon behavior.

### Custom values

An example using `AllowCustomValues` and `CustomValueFactory`.

## 37. Definition of done

The component is complete only when all of the following are true:

- implementation matches the approved scope;
- both `net48` and `net8.0-windows` build;
- relevant automated tests pass;
- demo/manual interaction checks pass;
- async lifecycle and disposal behavior are verified;
- theme and DPI scenarios are verified;
- public members have XML documentation;
- public API is reviewed and the repository baseline is intentionally updated;
- component documentation is updated;
- no regression is introduced in `BootstrapComboBox`;
- no duplicate overlay/placement infrastructure is introduced.

## 38. Future extension points, not v1 commitments

The architecture should leave room for later approved additions such as:

- secondary result descriptions;
- badges and richer metadata rendering;
- resolve-selected-values provider APIs;
- recent/favorite results;
- cache-provider decorators;
- more sophisticated scoring/matchers;
- selection limits;
- additional popup width constraints;
- async custom-value creation;
- cursor-based provider contracts;
- variable row metrics if a demonstrated use case justifies the added complexity.

These are not part of the v1 implementation scope and must not be implemented speculatively.
