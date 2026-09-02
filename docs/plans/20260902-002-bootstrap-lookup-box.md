# BootstrapLookupBox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved `BootstrapLookupBox` / `BootstrapLookupColumn` subsystem as a native WinForms single-selection lookup editor with local multi-column search, deterministic Vietnamese-friendly ranking, dynamic-suggest `CommitAndAdd`, Refresh/Add New footer actions, robust keyboard/focus/activation behavior, and a real `IDataGridViewEditingControl` integration that works with `BindingSource` / `BindingList<T>` and native DataGridView new-row editing.

**Architecture:** `BootstrapLookupBox : BootstrapTextBox` owns committed selection, pending query text, and public configuration. Two shared `BootstrapTextBox` prerequisites are added first: an internal trailing-accessory slot so the lookup affordance can coexist with inherited clear/trailing visuals without overlay hacks, and a transient validation override so lookup-generated validation never erases application-owned validation. Local data is accessed through an internal adapter/member-access layer and searched through a pure search engine that never mutates the caller's `BindingSource`. A lookup-specific popup controller reuses the generic overlay infrastructure and hosts `BootstrapLookupDropDownContent`, which contains a read-only `BootstrapDataGridView` plus a footer. `BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl` is the actual grid editor; internal `BootstrapLookupCell` and public `BootstrapLookupColumn` provide native DataGridView integration. The implementation must not overlay a second editor over `DataGridViewTextBoxEditingControl` and must not use `SendKeys` for navigation.

**Tech Stack:** C# / WinForms, `net48;net8.0-windows`, NUnit 4, `BindingSource`, `BindingList<T>`, `IList`, `IListSource`, `PropertyDescriptor`, existing Bootstrap theme/rendering/overlay infrastructure, `System.Windows.Forms.Timer`, Windows message routing for interaction tests.

**Spec:** `docs/superpowers/specs/2026-09-02-bootstrap-lookup-box-design.md`

**Planning base:** `main` after spec commit `da3bca239abc4235510ba88d9d5a30e74c7c4fa0`.

**Review amendments in this plan:** This revision resolves the implementation-plan review findings. Where the written spec was underspecified, this plan locks two implementation clarifications without expanding V1 feature scope: ambiguous exact display matches never auto-commit, and aggregate multi-token ranking uses the exact tuple defined in Task 4. These clarifications must be treated as an approved supplement to the spec during execution and mirrored into the spec if the spec is edited later.

## Global Constraints

- Before modifying product code, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the relevant section of `docs/COMPONENTS.md`, and the approved lookup spec.
- The approved spec plus the explicit review amendments in this plan are the source of truth. Do not add multi-select, remote/async providers, paging, fuzzy matching, arbitrary popup content, generic `BootstrapLookupBox<T>`, or a public provider/ranking abstraction in V1.
- Preserve `BootstrapSelect` semantics. Reuse generic overlay primitives, but do not inherit/reuse `BootstrapSelectDropDownController` or `BootstrapSelectDropDownContent` as the lookup implementation.
- Reuse shared infrastructure instead of duplicating it. Extract the existing `BootstrapSelectDebouncer` into a generic internal UI debouncer before adding lookup debounce behavior.
- Do not implement the lookup dropdown affordance by overlaying a child over `BootstrapTextBox.Editor`. Extend `BootstrapTextBox` with an internal framework-owned trailing-accessory slot and let the base layout reserve the space.
- Lookup-generated validation is transient framework state. It must visually override application validation while active, but clearing it must reveal the latest application-assigned `ValidationState`, including assignments made while the lookup error was active.
- `BootstrapLookupBox` must keep keyboard focus in its inherited native `BootstrapTextBox.Editor` while the popup is open. `ResultsGrid`, Refresh, Add New, and the dropdown affordance must not become Tab stops.
- Typing, searching, highlighting, popup open/close, Refresh, and Alt+Tab must not change committed selection and must not dirty a DataGridView cell.
- `SelectedItem`, `SelectedValue`, and `CommittedDisplayText` always mean committed state. `Text`, `HasPendingText`, and `HighlightedItem` may represent transient editing/search state.
- Empty/whitespace text is a distinct resolver outcome: clear selection and commit `null`. It never passes through `UnmatchedTextBehavior`.
- Exact display-text resolution succeeds only when there is exactly one distinct logical value among exact matches. Multiple exact rows that map to the same logical value count as one logical match; two or more distinct logical values are ambiguous and must block commit/navigation with lookup validation. Ambiguity never invokes `CommitAndAdd` and never creates a duplicate.
- `CommitAndAdd` is atomic: unmatched raw text must never be committed unless a corresponding datasource item was successfully created/accepted. Predictable inability to add falls back to `KeepFocusWithValidationError`; unexpected application/source exceptions propagate after internal cleanup.
- Search must operate on a projection. Never implement lookup filtering by changing `BindingSource.Filter`, `BindingSource.Position`, or the caller's source order.
- `DataPropertyName` on `BootstrapLookupColumn` stores the raw lookup identity. `ValueMember` reads identity from the lookup item. `DisplayMember` supplies display text.
- Native DataGridView AddNew/new-row lifecycle is required. Do not use `DataTable`, manual `Rows.Add`, or a hidden native textbox with an overlaid lookup editor in the replacement demo.
- Reused DataGridView editing controls must detach all datasource/column/event state from the previous lookup column before applying the next cell's configuration.
- Every public type/member requires XML documentation; the core project treats CS1591 as an error.
- Every product change must compile on both `net48` and `net8.0-windows`. Avoid APIs unavailable on `net48` unless an existing compatibility helper or a justified conditional path is used.
- Follow TDD task-by-task: add/extend the named test first, run the focused test and observe the expected failure, implement the minimum behavior, rerun focused tests, then commit.
- Do not update the public API fingerprint until the final API-review task. Focused test filters are expected to avoid the baseline test until then.

## Locked V1 Defaults and Clarifications

```text
Selection                     = single only
DataSource API                 = object DataSource
SearchDebounceMilliseconds     = 150
MinimumSearchLength            = 0
EmptyQueryBehavior             = ShowAll
TypingPopupBehavior            = AutoOpen
UnmatchedTextBehavior          = RestorePreviousSelection
EnterKeyBehavior               = CommitSelection
ClosedEnterKeyBehavior         = ResolvePendingText
ShowRefreshButton              = false
ShowAddNewButton               = false
ShowColumnHeaders              = true
DropDownWidth                  = 0
MaxDropDownHeight              = 320
Search normalization           = Trim + case-insensitive + remove Unicode diacritics + Đ/đ -> D/d
Exact-match normalization      = Trim
Exact-match comparer           = CurrentCultureIgnoreCase
Empty text                     = clear / commit null
Ambiguous exact text           = block commit/navigation; preserve pending text/value; show lookup validation
Alt+Tab                        = close popup only; preserve pending edit
Reactivation                   = keep popup closed
Highlight refresh              = preserve previous highlighted logical item when possible
Search ranking                 = per-token Exact > StartsWith > WordStart > Contains,
                                 aggregate by worst token, then total quality,
                                 DisplayMember best-match count, member priority, source order
Multi-token search             = AND across tokens; tokens may match different SearchMembers
Footer buttons                 = not Tab stops
ResultsChanged                 = fires only when logical result projection/search state changes
```

---

## Task 1: Add reviewed public lookup contracts and designer-friendly collections

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupEmptyQueryBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupTypingPopupBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupUnmatchedTextBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupEnterKeyBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupClosedEnterKeyBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupCommitReason.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumnDefinition.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumnDefinitionCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupSearchMemberCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupContractsTests.cs`

**Interfaces:** Produces the public enums, designer collections, result-column definition, and event args consumed by all later tasks. Do not add public types not required by the approved spec/review amendments.

- [ ] **Step 1: Write failing enum-contract tests.** Lock exact names/values for:
  - `BootstrapLookupEmptyQueryBehavior`: `ShowAll`, `ShowNone`
  - `BootstrapLookupTypingPopupBehavior`: `AutoOpen`, `KeepCurrentState`
  - `BootstrapLookupUnmatchedTextBehavior`: `RestorePreviousSelection`, `KeepFocusWithValidationError`, `CommitAndAdd`
  - `BootstrapLookupEnterKeyBehavior`: `CommitSelection`, `CommitSelectionAndMoveNext`
  - `BootstrapLookupClosedEnterKeyBehavior`: `ResolvePendingText`, `DataGridViewDefault`
  - `BootstrapLookupCommitReason`: `Keyboard`, `Mouse`, `Programmatic`, `ExactMatch`, `CommitAndAdd`, `Clear`

- [ ] **Step 2: Write failing collection tests.** `BootstrapLookupColumnDefinitionCollection` supports designer content serialization and rejects null entries. `BootstrapLookupSearchMemberCollection` rejects null/empty/whitespace names and ordinal duplicates while preserving insertion order.

- [ ] **Step 3: Write failing column-definition tests.** Lock defaults: `Width = 100`, `MinimumWidth = 5`, `Visible = true`, `AutoSizeMode = None`, left alignment, empty format/member/header; validate invalid widths.

- [ ] **Step 4: Write failing event-args tests.** Cover selection committed, highlighted-item changed, Refresh, Add New, Create From Text, and grid-cell contextual event args. Writable outcome properties (`Cancel`, `NewItem`/`Item`) must match the spec.

- [ ] **Step 5: Run the focused test and confirm compile/test failure because contracts do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupContractsTests"
```

- [ ] **Step 6: Implement only the reviewed contracts with XML docs/designer attributes.** Keep `BootstrapLookupCell` internal.

- [ ] **Step 7: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupContractsTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupContractsTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookup*.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupContractsTests.cs
git commit -m "feat: add BootstrapLookup public contracts"
```

---

## Task 2: Extract a shared WinForms UI debouncer and keep BootstrapSelect behavior unchanged

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapUiDebouncer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
- Delete after migration: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDebouncer.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapUiDebouncerTests.cs`
- Test existing: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSearchTests.cs`

**Interfaces:** Generalizes the existing Select debouncer without changing Select behavior; later Lookup tasks reuse it.

- [ ] **Step 1: Write failing tests** for `Schedule(TimeSpan, Action)`, zero-delay immediate execution, replacement, `Cancel`, negative-delay guard, and disposal safety. Use STA.
- [ ] **Step 2: Run focused tests and confirm the generic type is missing.**
- [ ] **Step 3: Move the current implementation to `BootstrapUiDebouncer` without new semantics.**
- [ ] **Step 4: Replace Select references and remove `BootstrapSelectDebouncer.cs`.**
- [ ] **Step 5: Run generic-debouncer plus Select-search regressions on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapUiDebouncerTests|FullyQualifiedName~BootstrapSelectSearchTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapUiDebouncerTests|FullyQualifiedName~BootstrapSelectSearchTests"
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapUiDebouncer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapUiDebouncerTests.cs
git rm src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDebouncer.cs
git commit -m "refactor: share WinForms UI debounce infrastructure"
```

---

## Task 3: Implement cached member access and the local datasource adapter

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMemberAccessor.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSourceItem.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataAdapterTests.cs`

**Interfaces:** Consumes `object DataSource`, `DisplayMember`, `ValueMember`; produces stable source snapshots, display/value access, identity lookup, source-change notifications, refresh/reconciliation, and safe add capability. It must not perform search/ranking.

- [ ] **Step 1: Write source-shape tests** for direct `BindingList<T>`, `BindingSource -> BindingList<T>`, `List<T>`, arrays, `IListSource`, and `BindingList<string>`. Snapshot enumeration must preserve source order and `BindingSource.Position`.
- [ ] **Step 2: Write member-access tests.** `DisplayMember == ""` uses `item?.ToString() ?? ""`; `ValueMember == ""` uses the item; null display/search values become empty; invalid members fail early once metadata exists; cache `PropertyDescriptor`.
- [ ] **Step 3: Write identity/missing-value tests.** `FindByValue` distinguishes legitimate null raw data from “not found”; a committed raw value missing from the source is preserved.
- [ ] **Step 4: Write add-capability tests.** Writable `BindingSource`/`IList` accepts through the public source abstraction; arrays/read-only lists report `CanAdd == false`. Never bypass a `BindingSource` wrapper.
- [ ] **Step 5: Write source-change/disposal tests.** Subscribe only when available and unsubscribe on dispose.
- [ ] **Step 6: Run failing focused tests, implement the adapter/accessor, rerun both TFMs, and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataAdapterTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataAdapterTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMemberAccessor.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSourceItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataAdapterTests.cs
git commit -m "feat: add BootstrapLookup datasource adapter"
```

---

## Task 4: Implement pure local search normalization, token matching, and fully specified deterministic ranking

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchResult.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMatchQuality.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupTextNormalization.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchEngineTests.cs`

**Interfaces:** Pure logic. No handles, timers, popup, or source mutation.

- [ ] **Step 1: Write Vietnamese normalization tests.**

```csharp
[TestCase("Cà phê sữa", "ca phe sua")]
[TestCase("Đường trắng", "duong trang")]
public void DefaultSearchNormalizerIsVietnameseFriendly(string input, string expected)
{
    Assert.That(BootstrapLookupTextNormalization.NormalizeSearchText(input), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Write minimum-length and empty-query tests.** Minimum length is after normalization. Below minimum returns a distinct waiting state. With minimum 0, `ShowAll` preserves source order and `ShowNone` returns no rows.

- [ ] **Step 3: Write token tests.** Split normalized query by whitespace; every token must match; different tokens may match different search members.

- [ ] **Step 4: Lock per-token candidate selection.** Map quality to scores:

```text
NoMatch    = 0
Contains   = 1
WordStart  = 2
StartsWith = 3
Exact      = 4
```

For each token, evaluate all configured search members and choose its best candidate by:
1. higher quality score;
2. if equal, a candidate whose member is `DisplayMember`;
3. if still equal, earlier configured `SearchMembers` order.

If `SearchMembers` is empty, the only candidate is `DisplayMember`; if `DisplayMember` is empty, use item text with member priority 0.

- [ ] **Step 5: Lock aggregate ranking with adversarial tests.** Every matching item receives this exact comparison tuple, compared in order:

```text
1. MinTokenQualityScore          descending
2. SumTokenQualityScore          descending
3. DisplayMemberBestMatchCount   descending
4. SumBestMemberPriority         ascending
5. OriginalSourceIndex           ascending
```

This means `[StartsWith, StartsWith]` outranks `[Exact, Contains]` because the worst token is stronger; an Exact match on one token cannot hide a weak Contains match on another. Add explicit tests for that pair, all-equal-quality ties, DisplayMember ties, SearchMembers-order ties, and stable source-order ties.

- [ ] **Step 6: Write SearchMembers fallback/hidden-member tests.**
- [ ] **Step 7: Run focused tests, implement the pure engine, rerun both TFMs, and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupSearchEngineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupSearchEngineTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchResult.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMatchQuality.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupTextNormalization.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchEngineTests.cs
git commit -m "feat: add deterministic BootstrapLookup search engine"
```

---

## Task 5: Add a framework-owned trailing accessory slot to BootstrapTextBox before Lookup UI work

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxAccessoryTests.cs`
- Test existing: BootstrapTextBox/InputGroup tests that cover layout/theme/DPI.

**Interfaces:** Produces one **internal-only** base-layout hook used by `BootstrapLookupBox`; it must not add a new public/protected API surface.

Use this internal contract:

```csharp
internal void SetFrameworkTrailingAccessory(Control? accessory)
```

Rules:
- `null` removes the prior framework accessory;
- a non-null accessory is parented and owned by `BootstrapTextBox`, forced `TabStop = false`, and laid out by the base class; replacing/removing it disposes the prior accessory unless it is the same instance;
- the accessory gets the same DPI-scaled logical extent currently used for the clear/trailing icon (`theme.Metrics.SpacingLG`);
- layout order from right to left is: framework accessory, then clear button when visible, otherwise trailing icon, then editor/placeholder;
- the inherited clear-button-vs-trailing-icon rule remains unchanged;
- `PaintIcons` must use the accessory-reserved right edge so a trailing icon cannot paint underneath the accessory;
- no overlay positioning and no subclass duplicate of private `LayoutChildren()`.

- [ ] **Step 1: Write failing layout tests.** Attach a fake accessory and prove editor width is reduced and the accessory is at the far right.
- [ ] **Step 2: Write coexistence tests.** Accessory + `ShowClearButton=true` places clear button to its left; accessory + `TrailingIcon` places the icon to its left when clear is hidden. No rectangles overlap at 96/144/192 DPI.
- [ ] **Step 3: Write behavior tests.** Accessory is not a Tab stop; replacing/removing it disposes the prior owned accessory and leaves no stale child control.
- [ ] **Step 4: Run focused tests and existing TextBox/InputGroup regressions; confirm failure first.**
- [ ] **Step 5: Implement the internal slot in the existing base layout/paint path.** Do not make `LayoutChildren`, `_editor`, `_clearButton`, or private layout state protected/public.
- [ ] **Step 6: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxAccessoryTests|FullyQualifiedName~BootstrapTextBox|FullyQualifiedName~BootstrapInputGroup"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapTextBoxAccessoryTests|FullyQualifiedName~BootstrapTextBox|FullyQualifiedName~BootstrapInputGroup"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxAccessoryTests.cs
git commit -m "refactor: add BootstrapTextBox framework accessory slot"
```

---

## Task 6: Add transient framework validation layering to BootstrapTextBox

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxValidationLayerTests.cs`
- Test existing: BootstrapTextBox validation/rendering tests.

**Interfaces:** Lets lookup validation temporarily override the border state without losing application-owned `ValidationState`. No new public/protected member is introduced.

Internal contract:

```csharp
internal void SetTransientValidationStateOverride(BootstrapValidationState? state)
```

Required state model:

```text
application baseline = last value assigned through public ValidationState setter
transient override    = nullable framework-owned state
effective getter/render state = transient override ?? application baseline
```

- [ ] **Step 1: Write failing state-layer tests.** Baseline `Valid` -> transient `Invalid` => public getter/effective render state is `Invalid`; application sets `None` while transient remains active => effective stays `Invalid`; clearing transient => getter/render state becomes latest baseline `None`.
- [ ] **Step 2: Write no-override regression tests.** With no transient override, public `ValidationState` behavior is byte-for-byte semantically unchanged.
- [ ] **Step 3: Write replacement/clear tests.** Repeated transient set/clear is idempotent; invalid enum values are rejected; disposal does not retain transient state externally.
- [ ] **Step 4: Run focused tests and confirm failure.**
- [ ] **Step 5: Implement separate baseline and transient fields.** Public setter updates only the baseline even while an override is active and always invalidates when the effective state may change. `OnPaint` resolves the effective state through the new layer.
- [ ] **Step 6: Run both TFMs plus existing TextBox validation tests and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxValidationLayerTests|FullyQualifiedName~BootstrapTextBox"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapTextBoxValidationLayerTests|FullyQualifiedName~BootstrapTextBox"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxValidationLayerTests.cs
git commit -m "refactor: layer transient BootstrapTextBox validation"
```

---

## Task 7: Implement BootstrapLookupBox core binding, committed state, pending text, public defaults, and dropdown affordance

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Data.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownAffordance.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupBoxTests.cs`

**Interfaces:** Public standalone control inheriting `BootstrapTextBox`; establishes committed/pending state and uses Task 5's internal accessory slot. Popup/search behavior comes later.

- [ ] **Step 1: Write inheritance/default/designer/public-shape tests.** Require `BootstrapLookupBox : BootstrapTextBox`, default event/property, locked defaults, content-serialized `Columns`/`SearchMembers`, and explicitly lock these reviewed public members so they cannot be forgotten later:

```text
SelectedItem
SelectedValue
CommittedDisplayText
HasPendingText
HighlightedItem
SelectedValueChanged
SelectionCommitted
HighlightedItemChanged
ResultsChanged
RefreshRequested
AddNewRequested
CreateItemFromText
SearchTextNormalizer
TextNormalizer
TextComparer
InvalidTextMessage
ValidationMessage
CancelPendingEdit()
```

`CancelPendingEdit()` is implemented in this task for core state restoration (restore committed text and clear lookup-transient validation/message). Popup-close behavior is added in Task 10 and pending-debounce cancellation is added in Task 11. `OpenDropDown`, `CloseDropDown`, `RefreshResults`, and `ResultsGrid` are added in their owning popup/content tasks; Task 17 verifies the complete set.

- [ ] **Step 2: Write committed-vs-pending tests.** Programmatic Product 15 selection followed by user typing keeps committed item/value/display while `Text`/`HasPendingText` become transient.
- [ ] **Step 3: Write programmatic selection tests.** `SelectedValue`, `SelectValue`, `SelectItem`, `ClearSelection`; `SelectedValueChanged` only on logical value change.
- [ ] **Step 4: Write missing-source-item tests.** Preserve raw committed values that disappear from source.
- [ ] **Step 5: Write core `CancelPendingEdit()` tests.** With no popup/search integration yet, it restores `Text = CommittedDisplayText`, sets `HasPendingText = false`, clears only lookup-transient validation/message through Task 6, preserves committed item/value, and raises no selection event.
- [ ] **Step 6: Write affordance-layout/focus tests.** Lookup owns one always-visible non-selectable dropdown affordance through `SetFrameworkTrailingAccessory`; it coexists with inherited clear/trailing visuals and never focuses itself on mouse activation.
- [ ] **Step 7: Run failing tests, implement minimal state + core cancellation + affordance, rerun both TFMs, and commit.** Use synchronization guards so programmatic text updates are not pending edits.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupBoxTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupBoxTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Data.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownAffordance.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupBoxTests.cs
git commit -m "feat: add BootstrapLookupBox core state"
```

---

## Task 8: Implement the shared commit resolver, exact matching, ambiguity handling, validation fallback, and CommitAndAdd

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Commit.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCommitResult.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupCommitTests.cs`

**Interfaces:** One resolver later used by Tab, Enter, focus-leave validation, mouse activation, and DataGridView completion.

- [ ] **Step 1: Write empty-text tests.** Empty after `TextNormalizer` clears/commits null and bypasses unmatched logic.
- [ ] **Step 2: Write unique exact-match tests.** Default Trim + `CurrentCultureIgnoreCase`, separate from accent-insensitive search.
- [ ] **Step 3: Write ambiguous-exact tests.** Source contains two items with identical normalized `DisplayMember` but different logical `ValueMember`. Resolver must:
  - return an ambiguity/block-navigation outcome;
  - preserve pending text and prior committed state;
  - apply transient lookup `Invalid` state/message through Task 6;
  - never choose the first row silently;
  - never invoke `UnmatchedTextBehavior`;
  - never invoke/create through `CommitAndAdd`.
- [ ] **Step 4: Write duplicate-row/same-logical-value test.** Two exact rows that resolve to the same logical value count as one logical match; commit the first source occurrence deterministically.
- [ ] **Step 5: Write `RestorePreviousSelection` tests.**
- [ ] **Step 6: Write `KeepFocusWithValidationError` tests.** It applies only lookup transient validation. While the error is active, set inherited `ValidationState` from application code to another value; typing clears the lookup override and reveals that latest application value rather than erasing it.
- [ ] **Step 7: Write string/object `CommitAndAdd` tests** including duplicate prevention, read-only fallback, null ValueMember, and source-add verification.
- [ ] **Step 8: Write exception/event-order tests.** Successful order: commit state -> canonical text -> clear pending -> `SelectedValueChanged` if changed -> grid dirty callback later -> `SelectionCommitted`.
- [ ] **Step 9: Run focused tests, implement, rerun both TFMs, and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupCommitTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupCommitTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Commit.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCommitResult.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupCommitTests.cs
git commit -m "feat: add BootstrapLookup commit resolver"
```

---

## Task 9: Implement popup result content, multi-column ResultsGrid, and footer presentation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupResultBindingItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDropDownContentTests.cs`

**Interfaces:** Visual tree only; adds public read-only `ResultsGrid` escape hatch while keeping ownership/invariants internal.

- [ ] **Step 1: Write visual-tree/invariant tests.**

```text
ReadOnly = true
MultiSelect = false
SelectionMode = FullRowSelect
AllowUserToAddRows = false
AllowUserToDeleteRows = false
RowHeadersVisible = false
TabStop = false
```

Footer controls also remain outside Tab order.

- [ ] **Step 2: Write column-materialization tests.** Materialize text-backed columns only when column configuration changes, not each search.
- [ ] **Step 3: Write `ResultsGrid` API/escape-hatch tests.** Same instance is exposed `[Browsable(false)]`/serialization hidden; callers may format/paint but framework reasserts invariants and owns `DataSource`.
- [ ] **Step 4: Write footer-state tests.** Position/result count, `0 / 0`, minimum-length instruction, independent Refresh/Add New visibility; footer remains visible with no buttons.
- [ ] **Step 5: Implement theme/DPI-safe content/footer and run both TFMs.**
- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupResultBindingItem.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDropDownContentTests.cs
git commit -m "feat: add BootstrapLookup popup content"
```

---

## Task 10: Implement lookup popup controller on the shared overlay infrastructure

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownController.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPopupTests.cs`

**Interfaces:** Reuses `BootstrapOverlaySurface`, `BootstrapOverlayDropDown`, `BootstrapOverlayAnchorTracker`, `BootstrapOverlayPlacementEngine`, and `BootstrapOverlayActivationDomain`.

- [ ] **Step 1: Write public popup API tests.** Lock `OpenDropDown()`, `CloseDropDown()`, `RefreshResults()`, `CancelPendingEdit()`, and `IsDropDownOpen`.
- [ ] **Step 2: Write open/close tests.** `CloseDropDown()` is presentation-only.
- [ ] **Step 3: Extend `CancelPendingEdit()` popup semantics.** The public method already restores committed text and clears lookup-transient validation/message from Task 7. Here, add/assert popup close while preserving committed value and without selection events. Pending-debounce cancellation is added/tested in Task 11 once debounce is integrated.
- [ ] **Step 4: Write placement/lifecycle tests** using existing overlay primitives; no second placement engine.
- [ ] **Step 5: Write mouse result tests.** Row click commits; empty-area click does not; focus remains editor-owned.
- [ ] **Step 6: Write Refresh tests.** Raise `RefreshRequested`, reconcile source, rerun query, preserve committed/highlight where possible; exception-safe reentrancy.
- [ ] **Step 7: Write explicit Add New tests.** Cancellation preserves pending; success commits returned valid item; no automatic next-cell move.
- [ ] **Step 8: Write activation-domain tests.** Editor, popup, grid, Refresh, Add New, and dropdown affordance belong to one logical interaction domain.
- [ ] **Step 9: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupPopupTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupPopupTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPopupTests.cs
git commit -m "feat: add BootstrapLookup popup controller"
```

---

## Task 11: Integrate debounce/search results, ResultsChanged semantics, highlight preservation, and keyboard navigation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Search.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchIntegrationTests.cs`

**Interfaces:** Connects shared debouncer, pure engine, popup/controller, editor text, `ResultsChanged`, and highlighted logical identity.

- [ ] **Step 1: Write debounce tests.** 150 ms, zero delay, replacement, version/generation guard.
- [ ] **Step 2: Write AutoOpen/KeepCurrentState tests.**
- [ ] **Step 3: Write minimum-length/manual-open tests.**
- [ ] **Step 4: Write `ResultsChanged` semantic tests.** Define logical projection identity as `(SearchState, ordered result logical values/source identities)`. Raise exactly once after applying a projection that differs from the previous projection. Do **not** raise merely for:
  - popup open/close;
  - highlight movement;
  - executing the same query that yields the same ordered logical projection;
  - Refresh when reconciliation yields the same projection.
  Raise when query/source/minimum-state changes the ordered projection or transitions into/out of waiting-for-minimum state.
- [ ] **Step 5: Write highlight event/preservation tests.** `HighlightedItemChanged` fires only when logical highlighted item changes; preserve prior logical item if still present; initial highlight prefers committed item then first result.
- [ ] **Step 6: Write logical navigation tests.** Up/Down/Home/End/PageUp/PageDown alter only highlight/grid current row/scroll/footer, never committed value/text/pending flag.
- [ ] **Step 7: Write flush/cancellation tests.** Down/PageDown/Enter/Tab/F4 flush pending debounce. `CancelPendingEdit()` and Escape cancel pending debounce without executing stale search work; after cancellation no delayed `ResultsChanged` may arrive from the discarded query.
- [ ] **Step 8: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupSearchIntegrationTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupSearchIntegrationTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Search.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchIntegrationTests.cs
git commit -m "feat: integrate BootstrapLookup search and navigation"
```

---

## Task 12: Harden standalone keyboard, focus, mouse, Escape, and application activation lifecycle with real message routing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupInteractionTests.cs`

**Interfaces:** Locks Windows-message behavior before DataGridView integration. Use real Form/editor routing where possible.

- [ ] **Step 1: Build STA/nonparallel interaction fixture** following `BootstrapSelectInteractionTests`.
- [ ] **Step 2: Write focus-invariant tests.** Popup navigation keeps editor focused; result/footer/affordance remain unfocused/non-Tab-stop.
- [ ] **Step 3: Write Enter tests.** Popup-open commit/resolve; closed behavior matrix; standalone move-next uses normal traversal, never SendKeys.
- [ ] **Step 4: Write Tab/Escape tests.** Tab resolves; validation/ambiguity blocks traversal; Escape delegates to the same behavior as `CancelPendingEdit()`.
- [ ] **Step 5: Write Down/F4/Alt+Down and affordance mouse tests.**
- [ ] **Step 6: Write Alt+Tab/deactivation regressions.** Close presentation only, preserve pending/committed state, no validation/commit/navigation; reactivation stays closed.
- [ ] **Step 7: Write same-app focus-leave tests.** Normal resolver runs; validation/ambiguity can cancel focus transition.
- [ ] **Step 8: Run both TFMs, then existing overlay/Select interaction regressions.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupInteractionTests|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupInteractionTests"
```

- [ ] **Step 9: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupInteractionTests.cs
git commit -m "feat: harden BootstrapLookup interaction lifecycle"
```

---

## Task 13: Add the real DataGridView editing control, internal cell, public lookup column, and reuse-safe reconfiguration

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.Events.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewTests.cs`

**Interfaces:** `BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl` is the actual editor. The grid may reuse one editing control across cells/columns, so configuration must be transactional and detachable.

- [ ] **Step 1: Write type-contract tests.** Public `BootstrapLookupColumn : DataGridViewColumn`; internal cell/editing control remain non-exported.
- [ ] **Step 2: Write configuration-copy tests.** Copy DataSource, members, cloned collections, search/commit/keyboard/popup defaults, normalizers/comparers, invalid message on every begin-edit.
- [ ] **Step 3: Write raw/formatted value tests.** Raw ProductId stays raw; display comes from lookup mapping.
- [ ] **Step 4: Implement complete `IDataGridViewEditingControl` contract.**
- [ ] **Step 5: Lock key ownership.** Keep lookup keys when required; Tab/navigation coordinated with grid.
- [ ] **Step 6: Write dirty-state tests.** Transient actions never dirty; logical committed value changes dirty exactly once.
- [ ] **Step 7: Write column contextual-event tests.**
- [ ] **Step 8: Add the missing editing-control reuse regression matrix.** In one real grid:
  1. begin edit in LookupColumn A using DataSource A;
  2. close/end edit;
  3. begin edit in LookupColumn B using DataSource B and different members/collections/events;
  4. mutate DataSource A;
  5. trigger B search/Refresh/SelectionCommitted.

Assert:
  - B never refreshes from A mutations;
  - no A `BindingSource.ListChanged` or adapter callback reaches the reused editor;
  - A's forwarded SelectionCommitted/Refresh/AddNew/CreateFromText handlers are detached;
  - B's handlers fire exactly once with B's row/column context;
  - B has cloned configuration and no collection instance shared with A;
  - a pending debounce/popup from A is cancelled/closed before B configuration becomes active.

- [ ] **Step 9: Implement an explicit reconfiguration boundary.** Before applying a new column/cell config, the editing control must cancel debounce, close popup presentation, detach old source adapter/subscriptions and old column event forwarding, clear lookup-transient validation, then copy the new configuration and initialize raw value. Do not depend on final `Dispose()` for this cleanup.
- [ ] **Step 10: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataGridViewTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataGridViewTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.Events.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewTests.cs
git commit -m "feat: add reuse-safe BootstrapLookup DataGridView editor"
```

---

## Task 14: Validate DataGridView Tab/Enter/Escape, validation blocking, currency, and native new-row lifecycle with real interaction tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewInteractionTests.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`

- [ ] **Step 1: Build bound-grid host** with `BindingList<OrderLine>` + `BindingSource`, neighboring editable/read-only/hidden cells, STA/nonparallel.
- [ ] **Step 2: Write valid Tab tests.** Grid finds next editable visible cell; never hard-code `ColumnIndex + 1`.
- [ ] **Step 3: Write invalid and ambiguous Tab tests.** `KeepFocusWithValidationError` and ambiguous exact text keep CurrentCell/editor and avoid row-model mutation.
- [ ] **Step 4: Write Enter matrix.**
- [ ] **Step 5: Write Escape tests.** Pending edit cancels without rolling back a value already committed earlier.
- [ ] **Step 6: Write BindingSource currency tests.** Search/highlight do not change Position/current row.
- [ ] **Step 7: Write native new-row acceptance test.** Begin edit on placeholder -> lookup selection -> Tab -> one typed `OrderLine` created through native AddNew and placeholder remains.
- [ ] **Step 8: Write DataGridView Alt+Tab regression.**
- [ ] **Step 9: Add cross-column reuse interaction test** that begins editing A then B with real message routing and repeats the Task 13 stale-subscription assertions after actual grid end-edit/start-edit transitions.
- [ ] **Step 10: Run net8 then net48 and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataGridViewInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataGridViewInteractionTests"
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewInteractionTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs
git commit -m "test: harden BootstrapLookup DataGridView lifecycle"
```

---

## Task 15: Replace the old DataGridView + BootstrapSelect overlay demo with the native BootstrapLookupColumn workflow

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/DataGridSelectEditingDemoFormTests.cs`
- Modify only if needed: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

- [ ] **Step 1: Rewrite demo tests first.** No DataTable, hidden BootstrapSelect overlay, or `EditingControlShowing` editor workaround.
- [ ] **Step 2: Define typed Product/OrderLine models; use `INotifyPropertyChanged` where required.**
- [ ] **Step 3: Configure Product `BootstrapLookupColumn`** bound raw `ProductId`, multi-column results, SearchMembers, Refresh/Add New.
- [ ] **Step 4: Wire contextual `SelectionCommitted`** to update dependent ProductName/Unit/UnitPrice/LineTotal.
- [ ] **Step 5: Demonstrate string `CommitAndAdd` on Unit if it does not conflict with dependent updates.**
- [ ] **Step 6: Wire Refresh/Add New in-memory workflows.**
- [ ] **Step 7: Run demo tests on both TFMs.**
- [ ] **Step 8: Manual smoke:** keyboard-only search, native new row, Tab, ambiguous duplicate display names, Refresh/Add New, dynamic unit, Alt+Tab.
- [ ] **Step 9: Commit.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/DataGridSelectEditingDemoFormTests.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs
git commit -m "demo: replace select editor overlay with BootstrapLookup"
```

---

## Task 16: Harden theme/DPI/accessibility/disposal/performance behavior

**Files:**
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupLifecycleTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPerformanceTests.cs`

- [ ] **Step 1: Disposal tests.** Popup/timer/source/theme/form events all detach.
- [ ] **Step 2: Theme tests.** No hard-coded semantic colors/GDI leaks.
- [ ] **Step 3: DPI/layout tests.** Include the new inherited trailing accessory at 96/120/144/192 DPI, footer visibility, placement clamp/flip, DropDownWidth automatic/explicit behavior.
- [ ] **Step 4: Accessibility tests.** Owner/result/footer/affordance sensible roles/names; status and affordance do not steal keyboard focus.
- [ ] **Step 5: Performance regressions for 1k/5k/10k.** No brittle ms SLA; assert correct order, metadata/column reuse, stable currency, no structural rebuild per key.
- [ ] **Step 6: Reconfiguration leak stress.** Alternate editing between two lookup columns/sources at least 50 times, then mutate both sources; assert one active subscription path only and no multiplying event/refresh count.
- [ ] **Step 7: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupLifecycleTests|FullyQualifiedName~BootstrapLookupPerformanceTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupLifecycleTests|FullyQualifiedName~BootstrapLookupPerformanceTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupLifecycleTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPerformanceTests.cs
git commit -m "test: harden BootstrapLookup lifecycle and performance"
```

---

## Task 17: Document the component, review the exported API, update fingerprint, and run the full dual-target verification gate

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Optional: Create `docs/BOOTSTRAP_LOOKUP_BOX.md`

- [ ] **Step 1: Update docs** for datasource/result columns/SearchMembers/ranking tuple/ambiguous exact behavior/unmatched modes/Refresh/Add New/keyboard/Alt+Tab/DataGridView raw binding/V1 non-goals.
- [ ] **Step 2: Add standalone and `BootstrapLookupColumn` examples** using BindingList/BindingSource, not DataTable.
- [ ] **Step 3: Add focused API review assertions.** Require every reviewed public member, explicitly including:

```text
BootstrapLookupBox.ResultsChanged
BootstrapLookupBox.CancelPendingEdit()
BootstrapLookupBox.ResultsGrid
BootstrapLookupBox.SearchTextNormalizer
BootstrapLookupBox.TextNormalizer
BootstrapLookupBox.TextComparer
BootstrapLookupBox.ValidationMessage
```

Also assert these remain non-exported:

```text
BootstrapLookupCell
BootstrapLookupEditingControl
BootstrapLookupDropDownAffordance
BootstrapLookupDropDownController
BootstrapLookupDropDownContent
BootstrapLookupFooter
BootstrapLookupDataAdapter
BootstrapLookupSearchEngine
BootstrapLookupMemberAccessor
```

Also assert Task 5/6 did **not** accidentally add new public/protected `BootstrapTextBox` accessory/validation members; those hooks must remain internal.

- [ ] **Step 4: Run baseline and intentionally capture expected fingerprint failure.**
- [ ] **Step 5: Review printed API line-by-line before accepting fingerprint.**
- [ ] **Step 6: Update `ApprovedV1Fingerprint`, rerun release test on both TFMs.**
- [ ] **Step 7: Run complete build gate.**

```powershell
./build.ps1 -Configuration Release
```

- [ ] **Step 8: Run complete automated test gate.**

```powershell
./test.ps1 -Configuration Release -SkipBuild
```

- [ ] **Step 9: Final interactive/manual checklist on Windows.**

```text
Standalone:
- typing/debounce/AutoOpen
- Vietnamese multi-token search and locked aggregate ranking
- duplicate display text with distinct ValueMember blocks commit
- duplicate rows with same logical ValueMember resolve deterministically
- mouse result commit
- Up/Down/Home/End/PageUp/PageDown
- Enter modes
- Tab resolve/block
- Escape and public CancelPendingEdit have identical cancellation semantics
- ResultsChanged fires only for actual projection/search-state changes
- Refresh / Add New / CommitAndAdd
- clear/trailing icon + dropdown affordance do not overlap
- application ValidationState survives lookup transient validation
- Alt+Tab closes only and reactivation stays closed

DataGridView:
- existing raw ProductId edit and formatted display
- Tab across hidden/read-only cells
- Enter matrix
- validation/ambiguity blocks cell change
- native new-row AddNew
- BindingSource Position stable
- A -> B lookup-column editor reuse has no stale datasource/events/debounce/popup
- no duplicate active editor/focus visuals

Theme/lifecycle:
- Light/Dark
- resize/form move
- supported DPI matrix
- close/dispose while popup open
- repeated A/B editor reuse does not multiply subscriptions/events
```

- [ ] **Step 10: Commit documentation/API baseline.**

```powershell
git add docs/COMPONENTS.md docs/TESTING.md README.md docs/PACKAGE_README.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/BOOTSTRAP_LOOKUP_BOX.md
git commit -m "docs: finalize BootstrapLookup component contract"
```

If `docs/BOOTSTRAP_LOOKUP_BOX.md` was not created, omit it from `git add`.

---

## Execution Dependency Map

```text
Task 1  public contracts
  |
Task 2  shared debouncer extraction
  |
Task 3  datasource adapter/member access
  |
Task 4  pure search engine + fully specified aggregate rank
  |
Task 5  BootstrapTextBox internal trailing-accessory layout prerequisite
  |
Task 6  BootstrapTextBox transient validation-layer prerequisite
  |
Task 7  BootstrapLookupBox core state + dropdown affordance
  |
Task 8  commit resolver + ambiguity + CommitAndAdd
  |
Task 9  popup content/footer + ResultsGrid
  |
Task 10 popup controller + public CancelPendingEdit
  |
Task 11 search/debounce/ResultsChanged/highlight navigation
  |
Task 12 standalone real keyboard/focus/activation hardening
  |
Task 13 DataGridView column/cell/editor + reuse-safe reconfiguration
  |
Task 14 real DataGridView lifecycle/new-row/reuse interaction hardening
  |
Task 15 replacement integrated demo
  |
Task 16 theme/DPI/disposal/performance/reuse hardening
  |
Task 17 docs + public API review + full verification
```

Do not start Task 7 before Tasks 5 and 6 pass their existing `BootstrapTextBox` regressions. Do not start Task 13 before standalone lookup state/commit/popup/keyboard semantics are stable. Do not treat final disposal as a substitute for Task 13's per-cell reconfiguration cleanup.

## Definition of Done

Implementation is complete only when all of the following are true:

- `BootstrapLookupBox` works standalone and keeps committed state independent from pending query/highlight state.
- The dropdown affordance is hosted through the base TextBox layout slot, never overlays the editor, never overlaps clear/trailing visuals, and does not steal focus.
- Lookup-generated invalid state is transient and clearing it reveals the latest application-owned `ValidationState`.
- Local search supports approved Vietnamese normalization, multi-token AND matching, SearchMembers, the exact aggregate ranking tuple in Task 4, debounce, minimum length, and empty-query policies without mutating source currency/order.
- Exact text auto-resolves only one distinct logical value. Ambiguous exact display text never silently commits the first row and never triggers `CommitAndAdd`.
- `RestorePreviousSelection`, `KeepFocusWithValidationError`, and atomic `CommitAndAdd` behave exactly as specified.
- Public `CancelPendingEdit()` is implemented/tested and Escape uses equivalent semantics.
- Public `ResultsChanged` is implemented/tested and fires only when the logical projection/search state actually changes.
- Results popup uses `BootstrapDataGridView` with multi-column definitions and footer; keyboard focus stays in the main editor.
- Alt+Tab/app deactivation only closes presentation and never commits/rolls back/validates/moves; activation does not auto-open.
- `BootstrapLookupEditingControl` is the real DataGridView editor. There is no native textbox + overlay-editor workaround and no `SendKeys` navigation.
- `BootstrapLookupColumn` commits raw `ValueMember` values, exposes row/cell-context events, and preserves native DataGridView navigation/validation semantics.
- Reusing one editing control from lookup column/source A to B detaches A completely: no stale source callbacks, event forwarding, pending debounce, popup, or shared mutable column/search collections.
- Native `BindingSource` / `BindingList<T>` new-row editing works with `AllowUserToAddRows = true`.
- Search/highlight does not change `BindingSource.Position` and transient activity does not dirty the grid.
- The old DataGrid + BootstrapSelect overlay demo is replaced by the typed BindingList/BindingSource lookup workflow.
- Lifecycle, theme, DPI, performance, keyboard/focus/window interaction, validation-layer, accessory-layout, reuse, and disposal tests pass.
- Public API exports only the reviewed contract; new BootstrapTextBox framework hooks remain internal; fingerprint is updated only after explicit API inspection.
- `./build.ps1 -Configuration Release` passes for both TFMs.
- `./test.ps1 -Configuration Release -SkipBuild` passes for both TFMs.
