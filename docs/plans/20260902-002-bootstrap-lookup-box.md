# BootstrapLookupBox Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development when executing this plan.

**Goal:** Implement the approved `BootstrapLookupBox` / `BootstrapLookupColumn` subsystem as a native WinForms single-selection lookup editor with local multi-column search, dynamic-suggest `CommitAndAdd`, Refresh/Add New footer actions, robust keyboard/focus/activation behavior, and a real `IDataGridViewEditingControl` integration that works with `BindingSource` / `BindingList<T>` and native DataGridView new-row editing.

**Architecture:** `BootstrapLookupBox : BootstrapTextBox` owns committed selection, pending query text, and public configuration. Local data is accessed through an internal adapter/member-access layer and searched through a pure search engine that never mutates the caller's `BindingSource`. A new lookup-specific popup controller reuses the repository's generic overlay infrastructure and hosts `BootstrapLookupDropDownContent`, which contains a read-only `BootstrapDataGridView` plus a footer. `BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl` is the actual grid editor; `BootstrapLookupCell` and public `BootstrapLookupColumn` provide native DataGridView integration. The implementation must not overlay a second editor over `DataGridViewTextBoxEditingControl` and must not use `SendKeys` for navigation.

**Tech Stack:** C# / WinForms, `net48;net8.0-windows`, NUnit 4, `BindingSource`, `BindingList<T>`, `IList`, `IListSource`, `PropertyDescriptor`, existing Bootstrap theme/rendering/overlay infrastructure, `System.Windows.Forms.Timer`, Windows message routing for interaction tests.

**Spec:** `docs/superpowers/specs/2026-09-02-bootstrap-lookup-box-design.md`

**Planning base:** `main` after spec commit `da3bca239abc4235510ba88d9d5a30e74c7c4fa0`.

## Global Constraints

- Before modifying product code, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the relevant section of `docs/COMPONENTS.md`, and the approved lookup spec.
- The approved spec is the source of truth. Do not add multi-select, remote/async providers, paging, fuzzy matching, arbitrary popup content, generic `BootstrapLookupBox<T>`, or a public provider/ranking abstraction in V1.
- Preserve `BootstrapSelect` semantics. Reuse generic overlay primitives, but do not inherit/reuse `BootstrapSelectDropDownController` or `BootstrapSelectDropDownContent` as the lookup implementation.
- Reuse shared infrastructure instead of duplicating it. In particular, extract the existing `BootstrapSelectDebouncer` into a generic internal UI debouncer before adding lookup debounce behavior.
- `BootstrapLookupBox` must keep keyboard focus in its inherited native `BootstrapTextBox.Editor` while the popup is open. `ResultsGrid`, Refresh, and Add New must not become Tab stops.
- Typing, searching, highlighting, popup open/close, Refresh, and Alt+Tab must not change committed selection and must not dirty a DataGridView cell.
- `SelectedItem`, `SelectedValue`, and `CommittedDisplayText` always mean committed state. `Text`, `HasPendingText`, and `HighlightedItem` may represent transient editing/search state.
- Empty/whitespace text is a distinct resolver outcome: clear selection and commit `null`. It never passes through `UnmatchedTextBehavior`.
- `CommitAndAdd` is atomic: unmatched raw text must never be committed unless a corresponding datasource item was successfully created/accepted. Predictable inability to add falls back to `KeepFocusWithValidationError`; unexpected application/source exceptions propagate after internal cleanup.
- Search must operate on a projection. Never implement lookup filtering by changing `BindingSource.Filter`, `BindingSource.Position`, or the caller's source order.
- `DataPropertyName` on `BootstrapLookupColumn` stores the raw lookup identity. `ValueMember` reads identity from the lookup item. `DisplayMember` supplies display text.
- Native DataGridView AddNew/new-row lifecycle is required. Do not use `DataTable`, manual `Rows.Add`, or a hidden native textbox with an overlaid lookup editor in the replacement demo.
- Every public type/member requires XML documentation; the core project treats CS1591 as an error.
- Every product change must compile on both `net48` and `net8.0-windows`. Avoid APIs unavailable on `net48` unless an existing compatibility helper or a justified conditional path is used.
- Follow TDD task-by-task: add/extend the named test first, run the focused test and observe the expected failure, implement the minimum behavior, rerun focused tests, then commit.
- Do not update the public API fingerprint until the final API-review task. Focused test filters are expected to avoid the baseline test until then.

## Locked V1 Defaults

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
Alt+Tab                        = close popup only; preserve pending edit
Reactivation                   = keep popup closed
Highlight refresh              = preserve previous highlighted logical item when possible
Search ranking                 = Exact > StartsWith > WordStart > Contains
Multi-token search             = AND across tokens; tokens may match different SearchMembers
Footer buttons                 = not Tab stops
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

**Interfaces:** Produces the public enums, collections, result-column definition, and event args consumed by all later tasks. Do not add public types not required by the approved spec.

- [ ] **Step 1: Write failing enum/default-contract tests.** Lock enum values and names exactly as approved.

```csharp
[Test]
public void LookupBehaviorEnumsExposeOnlyReviewedValues()
{
    Assert.That(Enum.GetValues(typeof(BootstrapLookupUnmatchedTextBehavior)), Is.EqualTo(new[]
    {
        BootstrapLookupUnmatchedTextBehavior.RestorePreviousSelection,
        BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError,
        BootstrapLookupUnmatchedTextBehavior.CommitAndAdd
    }));
}
```

Also lock:

```text
BootstrapLookupEmptyQueryBehavior: ShowAll, ShowNone
BootstrapLookupTypingPopupBehavior: AutoOpen, KeepCurrentState
BootstrapLookupEnterKeyBehavior: CommitSelection, CommitSelectionAndMoveNext
BootstrapLookupClosedEnterKeyBehavior: ResolvePendingText, DataGridViewDefault
BootstrapLookupCommitReason: Keyboard, Mouse, Programmatic, ExactMatch, CommitAndAdd, Clear
```

- [ ] **Step 2: Write failing collection tests.** `BootstrapLookupColumnDefinitionCollection` must support designer content serialization and reject null entries. `BootstrapLookupSearchMemberCollection` must reject null/empty/whitespace names and duplicate member names using ordinal member-name semantics; preserve insertion order because order affects ranking.

- [ ] **Step 3: Write failing column-definition tests.** Lock defaults (`Width = 100`, `MinimumWidth = 5`, `Visible = true`, `AutoSizeMode = None`, left alignment, empty format/member/header) and range validation for invalid widths.

- [ ] **Step 4: Write failing event-args tests.** Cover selection-committed, highlighted-item-changed, Refresh, Add New, Create From Text, and grid-cell contextual event args. Constructors may be internal where callers only receive events; writable outcome properties (`Cancel`, `NewItem`/`Item`) must match the spec.

- [ ] **Step 5: Run the focused tests and confirm compile/test failure because the contracts do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupContractsTests"
```

- [ ] **Step 6: Implement only the reviewed contracts with XML docs and designer attributes.** Keep `BootstrapLookupCell` out of this public-contract task; it remains internal unless DataGridView construction proves technically impossible without exporting it.

- [ ] **Step 7: Run focused tests on both target frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupContractsTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupContractsTests"
```

- [ ] **Step 8: Commit.**

```powershell
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

**Interfaces:** Generalizes the existing `BootstrapSelectDebouncer` without changing Select behavior; later Lookup tasks reuse the shared class.

- [ ] **Step 1: Write failing tests for the generic debouncer.** Require `Schedule(TimeSpan, Action)`, zero-delay immediate execution, later-schedule replacement, `Cancel`, negative-delay guard, and disposal safety. Use STA because `System.Windows.Forms.Timer` is UI-thread based.

- [ ] **Step 2: Run focused test and confirm the generic type is missing.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapUiDebouncerTests"
```

- [ ] **Step 3: Move the current debouncer implementation to `BootstrapUiDebouncer` without adding new semantics.** Preserve the current WinForms timer ownership and cleanup behavior.

- [ ] **Step 4: Replace `BootstrapSelectDebouncer` references in `BootstrapSelect.Search.cs` with `BootstrapUiDebouncer`, then remove the Select-specific file.** No public Select API changes are allowed.

- [ ] **Step 5: Run debouncer plus Select search regression tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapUiDebouncerTests|FullyQualifiedName~BootstrapSelectSearchTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapUiDebouncerTests|FullyQualifiedName~BootstrapSelectSearchTests"
```

Expected: all pass; existing Select debounce behavior is unchanged.

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

**Interfaces:** Consumes `object DataSource`, `DisplayMember`, `ValueMember`; produces stable source snapshots, display/value access, exact member validation, identity lookup, source-change notifications, refresh/reconciliation, and safe add capability. It must not perform search/ranking.

- [ ] **Step 1: Write failing source-shape tests.** Cover direct `BindingList<T>`, `BindingSource -> BindingList<T>`, `List<T>`, arrays, `IListSource`, and `BindingList<string>`. Assert source order is preserved and enumeration does not change `BindingSource.Position`.

```csharp
[Test]
public void SnapshotDoesNotMoveBindingSourcePosition()
{
    var items = new BindingList<Product> { Product.Create(1, "A"), Product.Create(2, "B") };
    using var source = new BindingSource { DataSource = items, Position = 1 };
    using var adapter = new BootstrapLookupDataAdapter(source, "Name", "Id");

    var snapshot = adapter.GetSnapshot();

    Assert.That(snapshot, Has.Count.EqualTo(2));
    Assert.That(source.Position, Is.EqualTo(1));
}
```

- [ ] **Step 2: Write failing member-access tests.** Require `DisplayMember == ""` to use `item?.ToString() ?? ""`; `ValueMember == ""` to use the item itself; null property values to become empty display/search text; invalid members to fail early once metadata is available. Prefer cached `PropertyDescriptor` metadata over repeated reflection.

- [ ] **Step 3: Write failing identity/missing-value tests.** `FindByValue` must distinguish a legitimate null raw value from “not found” using a success/result contract. Preserve a committed raw value even when no source item currently maps to it; do not auto-clear it.

- [ ] **Step 4: Write failing add-capability tests.** Writable `BindingSource`/`IList` accepts a new item through the public source abstraction; arrays/read-only lists report `CanAdd == false` without mutation. Verify add does not bypass `BindingSource` when `DataSource` itself is a `BindingSource`.

- [ ] **Step 5: Write source-change/disposal tests.** Subscribe to `BindingSource.ListChanged` / equivalent notifications only when available, surface one adapter change notification, and unsubscribe on dispose.

- [ ] **Step 6: Run focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataAdapterTests"
```

- [ ] **Step 7: Implement the adapter and accessor.** Keep these responsibilities separate:

```text
BootstrapLookupMemberAccessor -> metadata resolution/get-value only
BootstrapLookupDataAdapter    -> source enumeration/currency/add/change subscriptions
BootstrapLookupSourceItem     -> item + original source index + display text + logical value
```

Do not add WinForms popup/search logic here.

- [ ] **Step 8: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataAdapterTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataAdapterTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMemberAccessor.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSourceItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataAdapterTests.cs
git commit -m "feat: add BootstrapLookup datasource adapter"
```

---

## Task 4: Implement pure local search normalization, token matching, and deterministic ranking

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchResult.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMatchQuality.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupTextNormalization.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchEngineTests.cs`

**Interfaces:** Pure logic; consumes a source snapshot plus query/search-member configuration and returns ranked result items. No Control handle, BindingSource mutation, popup, or timer dependency.

- [ ] **Step 1: Write failing Vietnamese normalization tests.** Lock Trim, case-insensitive search normalization, Unicode combining-diacritic removal, and `Đ/đ -> D/d` behavior.

```csharp
[TestCase("Cà phê sữa", "ca phe sua")]
[TestCase("Đường trắng", "duong trang")]
public void DefaultSearchNormalizerIsVietnameseFriendly(string input, string expected)
{
    Assert.That(BootstrapLookupTextNormalization.NormalizeSearchText(input), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Write failing minimum-length and empty-query tests.** `MinimumSearchLength` is applied after normalization. Below the minimum produces a distinct waiting/instruction result, not ordinary “zero matches.” With minimum 0, `ShowAll` returns original source order and `ShowNone` returns none.

- [ ] **Step 3: Write failing token tests.** Split normalized query by whitespace, require every token to match, and allow different tokens to match different search members.

- [ ] **Step 4: Write failing ranking tests.** Lock per-token quality `Exact > StartsWith > WordStart > Contains`; aggregate deterministically; tie-break by DisplayMember match, then configured SearchMembers order, then original source order. Do not add fuzzy/edit-distance behavior.

- [ ] **Step 5: Write SearchMembers fallback tests.** An empty search-member collection searches only `DisplayMember`; if `DisplayMember` is empty, search the item string representation. SearchMembers may include hidden members such as Barcode.

- [ ] **Step 6: Run focused tests, implement minimum pure engine, rerun both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupSearchEngineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupSearchEngineTests"
```

- [ ] **Step 7: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchResult.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupMatchQuality.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupTextNormalization.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchEngineTests.cs
git commit -m "feat: add BootstrapLookup local search engine"
```

---

## Task 5: Implement BootstrapLookupBox core binding, committed state, pending text, and public defaults

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Data.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupBoxTests.cs`

**Interfaces:** Public standalone control inheriting `BootstrapTextBox`; consumes the adapter/contracts and establishes the source-of-truth state model. Popup and advanced keyboard behavior are added later.

- [ ] **Step 1: Write failing inheritance/default/designer tests.** Require `BootstrapLookupBox : BootstrapTextBox`, `[DefaultEvent(nameof(SelectionCommitted))]`, `[DefaultProperty(nameof(DisplayMember))]`, locked defaults from this plan, `Columns` and `SearchMembers` content-serialized collections, and `ResultsGrid` hidden from designer serialization once available.

- [ ] **Step 2: Write failing committed-vs-pending state tests.** Programmatically select Product 15, then simulate user text editing and assert:

```text
SelectedItem        remains Product 15
SelectedValue       remains 15
CommittedDisplayText remains canonical display text
Text                becomes pending query
HasPendingText      becomes true
```

Do not mark a selection change merely because `TextChanged` fires.

- [ ] **Step 3: Write failing programmatic selection tests.** `SelectedValue`, `SelectValue`, `SelectItem`, and `ClearSelection` must resolve against the logical source, synchronize text without creating pending state, and raise `SelectedValueChanged` only when the committed logical value changes. `SelectItem` returns false for an item outside the source.

- [ ] **Step 4: Write missing-source-item tests.** Initializing a raw committed value that no longer exists in datasource must preserve the raw value and best available committed display text; it must not silently clear the model.

- [ ] **Step 5: Run focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupBoxTests"
```

- [ ] **Step 6: Implement the minimal partial control.** Reuse protected `BootstrapTextBox.Editor` and override `OnEditorTextChanged` / `OnEditorKeyDown` only through the existing protected extension points. Use synchronization guards so programmatic text updates do not become pending user queries.

Conceptual state fields:

```csharp
private object? _selectedItem;
private object? _selectedValue;
private string _committedDisplayText = string.Empty;
private bool _hasPendingText;
private bool _synchronizingText;
```

Keep all configuration validation in property setters; invalid enum/range/null comparer/normalizer values fail fast.

- [ ] **Step 7: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupBoxTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupBoxTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Data.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupBoxTests.cs
git commit -m "feat: add BootstrapLookupBox core state"
```

---

## Task 6: Implement the shared commit resolver, exact matching, validation fallback, and CommitAndAdd

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Commit.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCommitResult.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupCommitTests.cs`

**Interfaces:** Adds one resolver used later by Tab, Enter, focus-leave validation, mouse activation, and DataGridView edit completion.

- [ ] **Step 1: Write failing empty-text tests.** Empty/whitespace after normal lookup text normalization clears committed selection, commits null, clears pending state, and does not invoke `UnmatchedTextBehavior` or Create From Text.

- [ ] **Step 2: Write failing exact-match tests.** Default exact behavior is Trim + `StringComparer.CurrentCultureIgnoreCase`, independent from accent-insensitive search normalization. Existing `"Hà Nội"` must match `" hà nội "`; `"ca phe"` is not automatically exact-equal to `"Cà phê"` unless caller customizes exact normalization/comparison.

- [ ] **Step 3: Write `RestorePreviousSelection` tests.** Unmatched pending text restores canonical committed display text, preserves the old value, clears pending state, and does not raise `SelectedValueChanged` when value is unchanged.

- [ ] **Step 4: Write `KeepFocusWithValidationError` tests.** Resolver reports blocked navigation, preserves pending text, sets lookup-generated invalid state/message (`InvalidTextMessage` default from spec), and leaves committed value untouched. Further user typing clears only the transient lookup-generated error, not unrelated externally assigned validation.

- [ ] **Step 5: Write string `CommitAndAdd` tests.** For `BindingList<string>`, unmatched `" Chai "` with no factory handler adds `"Chai"`, selects it, commits it, and raises `SelectionCommitted` with reason `CommitAndAdd`. An existing normalized exact match must be selected without adding a duplicate.

- [ ] **Step 6: Write object `CommitAndAdd` tests.** `CreateItemFromText` receives both OriginalText and NormalizedText. Successful returned item is added through the adapter then committed. No handler/null item/read-only source/capability unavailable falls back to validation and does not commit raw text.

- [ ] **Step 7: Write exception and event-order tests.** Unexpected handler/source mutation exceptions propagate after flags are reset. Successful order is: mutate committed state -> synchronize Text -> clear pending -> `SelectedValueChanged` if changed -> owner dirty callback later -> `SelectionCommitted`. Event handlers must see fully consistent state.

- [ ] **Step 8: Run focused tests, implement the resolver, rerun both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupCommitTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupCommitTests"
```

- [ ] **Step 9: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Commit.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCommitResult.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupCommitTests.cs
git commit -m "feat: add BootstrapLookup commit resolver"
```

---

## Task 7: Implement popup result content, multi-column ResultsGrid, and footer presentation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupResultBindingItem.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDropDownContentTests.cs`

**Interfaces:** Builds the popup visual tree only. It receives a result projection and highlighted identity from the owner/controller; it does not own committed selection or search logic.

- [ ] **Step 1: Write failing visual-tree/invariant tests.** Content contains `BootstrapDataGridView` docked Fill and footer docked Bottom. Lock result-grid invariants:

```text
ReadOnly = true
MultiSelect = false
SelectionMode = FullRowSelect
AllowUserToAddRows = false
AllowUserToDeleteRows = false
RowHeadersVisible = false
TabStop = false
```

Footer buttons also have `TabStop = false`.

- [ ] **Step 2: Write failing column-materialization tests.** Declarative `BootstrapLookupColumnDefinition` entries materialize as text-backed `DataGridViewTextBoxColumn` instances exactly once per column configuration change, not on every search. Test DataPropertyName, HeaderText, Width, MinimumWidth, Visible, AutoSizeMode, alignment, format, ValueType, and `ShowColumnHeaders`.

- [ ] **Step 3: Write `ResultsGrid` escape-hatch tests.** The owner exposes the same grid instance for formatting/painting customization, but content/controller reapplies framework-owned invariants when opening/reconfiguring; caller must not replace its DataSource.

- [ ] **Step 4: Write footer-state tests.** With 128 results and highlight index 2, status is `3 / 128`. With no results, `0 / 0`. Waiting-for-minimum state displays instruction text rather than pretending a normal empty result. Refresh/Add New visibility tracks independent booleans while footer remains visible when both are false.

- [ ] **Step 5: Run focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDropDownContentTests"
```

- [ ] **Step 6: Implement content/footer with theme/DPI-safe layout.** Reuse theme tokens/metrics; do not hard-code semantic colors. Keep footer outside the scrolling grid so constrained popup height scrolls only ResultsGrid.

- [ ] **Step 7: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDropDownContentTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDropDownContentTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupResultBindingItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDropDownContentTests.cs
git commit -m "feat: add BootstrapLookup popup content"
```

---

## Task 8: Implement lookup popup controller on the shared overlay infrastructure

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownController.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPopupTests.cs`

**Interfaces:** Reuses `BootstrapOverlaySurface`, `BootstrapOverlayDropDown`, `BootstrapOverlayAnchorTracker`, `BootstrapOverlayPlacementEngine`, and `BootstrapOverlayActivationDomain`. Owns popup lifetime/placement, content synchronization, mouse result activation, Refresh/Add New button dispatch, and activation-domain membership.

- [ ] **Step 1: Write failing open/close tests.** `OpenDropDown()` creates/positions the popup and exposes `IsDropDownOpen`; `CloseDropDown()` is presentation-only and must not commit, validate, rollback, clear pending text, or move focus/cell.

- [ ] **Step 2: Write placement/lifecycle tests.** Anchor moves/resizes/DPI changes cause reposition through existing overlay infrastructure; constrained working area clamps/flip-placement while footer stays visible. Do not create a second placement engine.

- [ ] **Step 3: Write mouse result tests.** Clicking a result row commits that logical item with reason `Mouse`, synchronizes canonical display text, closes popup, and does not rely on ResultsGrid focus. A click on non-row/empty area does not commit.

- [ ] **Step 4: Write Refresh tests.** Button and `RefreshResults()` invoke `RefreshRequested`, reconcile adapter/source, rerun current query, preserve committed selection, preserve highlight if still present, and do not move/commit. Re-entrancy/double-trigger protection must not leave the button disabled after exceptions.

- [ ] **Step 5: Write explicit Add New tests.** `AddNewRequested` receives current QueryText. Cancellation preserves pending query and committed value. Success auto-selects/commits `NewItem`; source reconciliation is attempted but explicit Add New may commit the returned item by ValueMember even before source refresh sees it. Do not auto-move to the next cell in V1.

- [ ] **Step 6: Write activation-domain tests.** Editor, popup surface, ResultsGrid, Refresh, and Add New count as one lookup activation domain. Clicking inside it must not trigger normal edit-end resolution.

- [ ] **Step 7: Run focused tests, implement controller/owner bridge, rerun both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupPopupTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupPopupTests"
```

- [ ] **Step 8: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPopupTests.cs
git commit -m "feat: add BootstrapLookup popup controller"
```

---

## Task 9: Integrate debounce/search results, highlight preservation, and keyboard result navigation into BootstrapLookupBox

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Search.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchIntegrationTests.cs`

**Interfaces:** Connects the shared debouncer, pure search engine, popup controller, editor text changes, and highlighted logical identity. Does not yet implement DataGridView-specific editing contract.

- [ ] **Step 1: Write debounce tests.** `150 ms` schedules one search after rapid typing; `0` searches immediately; each new text change replaces pending work. Internal query generation/version must reject stale result application if future async work is introduced, even though V1 search is synchronous/local.

- [ ] **Step 2: Write AutoOpen/KeepCurrentState tests.** `AutoOpen` opens only after non-empty query reaches `MinimumSearchLength`; `KeepCurrentState` updates an already-open popup but does not open a closed one. Clearing text while popup is closed must not auto-open.

- [ ] **Step 3: Write minimum-length and manual-open tests.** When below minimum, manually opening via F4/Down displays `Type at least N characters` and no source rows; `MinimumSearchLength > 0` takes precedence over `EmptyQueryBehavior`.

- [ ] **Step 4: Write highlight preservation tests.** Search refresh preserves prior highlighted logical item if still present. Initial popup population prefers the committed item if present, otherwise first ranked result. If prior highlight disappears, fall back to first ranked result.

- [ ] **Step 5: Write logical navigation tests.** Up/Down/Home/End/PageUp/PageDown mutate only highlighted item/index, ResultsGrid current row/scroll position, and footer. They must not mutate `SelectedValue`, `Text`, or `HasPendingText`.

- [ ] **Step 6: Write flush tests.** Down/PageDown/Enter/Tab/F4 must flush a pending debounce before using results. Escape cancels pending debounce without running a stale search.

- [ ] **Step 7: Run focused tests, implement integration, rerun both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupSearchIntegrationTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupSearchIntegrationTests"
```

- [ ] **Step 8: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Search.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupSearchIntegrationTests.cs
git commit -m "feat: integrate BootstrapLookup search and highlight navigation"
```

---

## Task 10: Harden standalone keyboard, focus, mouse, Escape, and application activation lifecycle with real message routing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupInteractionTests.cs`

**Interfaces:** Locks user-visible interaction invariants before DataGridView integration. Tests must route real/native messages to the inherited editor where possible; do not merely call internal commit/open helpers.

- [ ] **Step 1: Build an STA/nonparallel interaction fixture.** Follow the repository pattern in `BootstrapSelectInteractionTests`: host a real Form, show it, call `Application.DoEvents()`, and route key messages to the native `TextBox` editor. Any truly desktop-dependent SendInput test should be `[Explicit]` with an interactive-Windows explanation.

- [ ] **Step 2: Write focus-invariant tests.** Open popup, send Down/PageDown/Home/End, and assert editor remains focused while ResultsGrid remains unfocused. Footer buttons must not appear in Tab order.

- [ ] **Step 3: Write Enter tests.** Popup-open Enter commits highlighted item; popup-open with no highlight resolves pending text. Popup-closed behavior follows `ClosedEnterKeyBehavior`: ResolvePendingText vs owner/default behavior. `CommitSelection` stays; `CommitSelectionAndMoveNext` uses normal WinForms traversal for standalone control, not SendKeys.

- [ ] **Step 4: Write Tab/Escape tests.** Tab resolves pending text before traversal; successful resolve moves normally. `KeepFocusWithValidationError` blocks traversal, opens/reopens popup, highlights best candidate, and keeps editor focus. Escape discards pending text, restores `CommittedDisplayText`, closes popup, and leaves committed value unchanged.

- [ ] **Step 5: Write Down/F4/Alt+Down tests.** Closed popup opens and flushes current search. Mouse click on dropdown affordance opens without moving focus to popup children.

- [ ] **Step 6: Write Alt+Tab/deactivation regression tests.** Simulate owner/application deactivation separately from same-app focus change. Deactivation must only close popup; preserve Text, `HasPendingText`, `SelectedItem`, `SelectedValue`; no validation/commit/rollback/navigation. Reactivation must not auto-open. A later Down/F4 reopens using the preserved query.

- [ ] **Step 7: Write same-app focus-leave tests.** Clicking/focusing a different control in the same form attempts normal resolver/end-edit semantics rather than using app-deactivation behavior. Validation mode blocks the focus transition and restores editor focus.

- [ ] **Step 8: Run focused interaction tests on net8 first, implement fixes, then net48.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupInteractionTests"
```

- [ ] **Step 9: Run existing overlay and BootstrapSelect interaction regression tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapSelectInteractionTests"
```

- [ ] **Step 10: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupInteractionTests.cs
git commit -m "feat: harden BootstrapLookup interaction lifecycle"
```

---

## Task 11: Add the real DataGridView editing control, internal cell, and public lookup column

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.Events.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewTests.cs`

**Interfaces:** `BootstrapLookupEditingControl : BootstrapLookupBox, IDataGridViewEditingControl` becomes the actual editor created by the cell. `BootstrapLookupColumn` is public and owns reusable configuration; the cell/editor remain internal implementation details.

- [ ] **Step 1: Write failing type-contract tests.** Assert public `BootstrapLookupColumn : DataGridViewColumn`; its CellTemplate is an internal lookup cell; cell `EditType` resolves to the lookup editing control; `BootstrapLookupEditingControl` implements `IDataGridViewEditingControl` and inherits `BootstrapLookupBox`.

- [ ] **Step 2: Write configuration-copy tests.** Column copies DataSource, DisplayMember, ValueMember, LookupColumns, SearchMembers, all search/unmatched/keyboard/popup defaults, dimensions, footer visibility, normalizers/comparers allowed by the spec, and invalid-text message into the reused editing control every time a cell begins edit. Clone/copy collections so editing one cell cannot mutate column configuration.

- [ ] **Step 3: Write raw/formatted value tests.** With `DataPropertyName = ProductId`, `ValueMember = Id`, `DisplayMember = Name`, raw cell value `125` displays the matching Product name while edit control committed value remains 125. `GetFormattedValue` must not store ProductName as raw cell value.

- [ ] **Step 4: Implement the complete `IDataGridViewEditingControl` contract.** Cover `EditingControlDataGridView`, formatted value get/set, row index, `EditingControlValueChanged`, cursor, reposition flag, style application, `PrepareEditingControlForEdit`, and `EditingControlWantsInputKey`.

- [ ] **Step 5: Lock key ownership.** `EditingControlWantsInputKey` keeps arrows/PageUp/PageDown/Home/End/F4/Enter/Escape when lookup semantics require them. Tab/navigation remains coordinated with DataGridView; do not simulate keys.

- [ ] **Step 6: Write dirty-state tests.** Typing/search/highlight/popup/Refresh/Alt+Tab/restoring same value leave `EditingControlValueChanged == false`. Different committed value, clearing non-null, `CommitAndAdd`, and explicit Add New set it and call `DataGridView.NotifyCurrentCellDirty(true)` exactly when needed.

- [ ] **Step 7: Write column contextual-event tests.** SelectionCommitted/AddNew/CreateItem/Refresh raised from the editing control are forwarded through the column with `DataGridView`, `RowIndex`, and `ColumnIndex`; event handlers can update dependent row model properties.

- [ ] **Step 8: Run focused tests, implement minimum grid integration, rerun both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataGridViewTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataGridViewTests"
```

- [ ] **Step 9: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupColumn.Events.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewTests.cs
git commit -m "feat: add BootstrapLookup DataGridView column and editor"
```

---

## Task 12: Validate DataGridView Tab/Enter/Escape, validation blocking, BindingSource currency, and native new-row lifecycle with real interaction tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewInteractionTests.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs`

**Interfaces:** Hardens the native DataGridView lifecycle. These are regression/acceptance tests for the architecture; do not bypass them by calling internal popup methods directly.

- [ ] **Step 1: Build a bound-grid test host.** Use `BindingList<OrderLine>` + `BindingSource`, `BootstrapDataGridView`, `AutoGenerateColumns = false`, a `BootstrapLookupColumn` bound to `OrderLine.ProductId`, and neighboring editable/read-only/hidden columns. `[Apartment(ApartmentState.STA)]` + `[NonParallelizable]`.

- [ ] **Step 2: Write valid Tab tests.** Start real editing in Product lookup, type/select, route Tab through the actual editing control, assert raw ProductId commits and DataGridView chooses the next editable visible cell. Include hidden/read-only intermediate columns to prove navigation is not `ColumnIndex + 1`.

- [ ] **Step 3: Write invalid Tab tests.** Under `KeepFocusWithValidationError`, unmatched text must keep CurrentCell on Product, keep/reacquire editor focus, reopen popup, and avoid row-model mutation.

- [ ] **Step 4: Write Enter behavior matrix.** `CommitSelection` commits but remains in the current cell; `CommitSelectionAndMoveNext` delegates to normal grid navigation. With popup closed + `DataGridViewDefault`, lookup does not steal default Enter semantics. With popup closed + ResolvePendingText, the normal lookup resolver runs first.

- [ ] **Step 5: Write Escape tests.** Pending query restores committed text/value without dirtying the grid. A selection already committed earlier must not be rolled back by a later unrelated Escape owned by DataGridView.

- [ ] **Step 6: Write BindingSource currency tests.** Search/highlight movement must not change `BindingSource.Position` or current row. Selecting a lookup result changes only the cell's bound property when committed.

- [ ] **Step 7: Write native new-row acceptance test.** Setup:

```csharp
var lines = new BindingList<OrderLine>();
using var source = new BindingSource { DataSource = lines };
grid.DataSource = source;
grid.AllowUserToAddRows = true;
```

Route interaction through the new-row placeholder: begin edit -> type/search -> select Product -> Tab. Assert `lines.Count == 1`, `lines[0].ProductId` is the selected raw value, new-row placeholder remains available, and no manual `Rows.Add`/DataTable path is involved.

- [ ] **Step 8: Write DataGridView Alt+Tab/deactivation regression.** Popup closes and pending text survives without dirtying, committing, validating, or moving CurrentCell. Reactivation keeps popup closed. Reopen by Down/F4 and continue the same query.

- [ ] **Step 9: Run focused tests on net8, fix only native lifecycle issues, then run net48.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupDataGridViewInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupDataGridViewInteractionTests"
```

- [ ] **Step 10: Commit.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupDataGridViewInteractionTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupEditingControl.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupCell.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Keyboard.cs
git commit -m "test: harden BootstrapLookup DataGridView lifecycle"
```

---

## Task 13: Replace the old DataGridView + BootstrapSelect overlay demo with the native BootstrapLookupColumn workflow

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/DataGridSelectEditingDemoFormTests.cs`
- Modify only if the demo title/navigation text requires it: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:** Demonstrates the production integration rather than the rejected overlay workaround. Preserve the existing demo route/form name to avoid unnecessary navigation churn, but change its content/title/instructions to BootstrapLookup.

- [ ] **Step 1: Rewrite demo tests first.** Assert the form no longer owns a `DataTable`, no longer contains a hidden `BootstrapSelect` editor, and no longer wires `EditingControlShowing` to overlay an editor. Assert grid uses `BootstrapLookupColumn`, `AllowUserToAddRows = true`, and DataSource is a `BindingSource` backed by `BindingList<OrderLine>`.

- [ ] **Step 2: Define typed demo models.** In the demo file or small dedicated demo-model files, use `OrderLine : INotifyPropertyChanged` and a typed Product model. `OrderLine` includes ProductId, ProductName, Unit, Quantity, UnitPrice, and computed/updated LineTotal. Keep ProductName/Unit/UnitPrice as dependent row-model fields; raw lookup cell binds ProductId.

- [ ] **Step 3: Configure the Product `BootstrapLookupColumn`.** Bind `DataPropertyName = ProductId`, `DisplayMember = Name`, `ValueMember = Id`; result columns show at least Code/Name/Unit/UnitPrice; SearchMembers include Code, Name, Barcode or equivalent sample member; enable Refresh and Add New in the demo.

- [ ] **Step 4: Wire `SelectionCommitted` contextual event.** Resolve the selected Product and update ProductName, Unit, UnitPrice, and LineTotal on the current `OrderLine`. Do not rely on cell formatted text as persistence state.

- [ ] **Step 5: Demonstrate dynamic suggest on Unit if it does not conflict with dependent Product updates.** Use a separate `BootstrapLookupColumn`/source backed by `BindingList<string>` with `UnmatchedTextBehavior = CommitAndAdd`; typing a new unit and Tab adds it to the suggestion source and commits it. If Product selection supplies Unit, the user may still edit it afterwards.

- [ ] **Step 6: Wire Refresh/Add New demo events.** Refresh rebuilds/reconciles the in-memory product source while preserving query. Add New creates an in-memory Product from QueryText (or a tiny local create dialog if already justified by demo conventions), returns it through `NewItem`, and verifies auto-select/commit. Do not introduce external persistence dependencies.

- [ ] **Step 7: Run demo tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~DataGridSelectEditingDemoFormTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~DataGridSelectEditingDemoFormTests"
```

- [ ] **Step 8: Manual demo smoke check.** Run the demo, edit existing Product rows, use keyboard-only search, add a new row through the native placeholder, trigger Refresh/Add New, use dynamic Unit suggest, Tab across cells, and Alt+Tab with popup open.

- [ ] **Step 9: Commit.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/DataGridSelectEditingDemoFormTests.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs
git commit -m "demo: replace select editor overlay with BootstrapLookup"
```

---

## Task 14: Harden theme/DPI/accessibility/disposal/performance behavior

**Files:**
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupLifecycleTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPerformanceTests.cs`

**Interfaces:** Hardening only; do not add new feature scope or public abstractions.

- [ ] **Step 1: Write disposal tests.** Open popup, attach a BindingSource, schedule debounce, then dispose owner/control. Assert popup is closed/disposed, timer stops, source/theme/form events are unsubscribed, and later source changes do not touch disposed controls.

- [ ] **Step 2: Write theme tests.** Light/Dark theme changes repaint owner/content/footer/result grid using existing theme tokens; no hard-coded semantic colors or leaked GDI objects.

- [ ] **Step 3: Write DPI/layout tests.** Popup/result/footer sizes scale through existing DPI helpers and placement engine. Footer remains visible when vertical space is constrained. DropDownWidth 0 uses the approved automatic behavior; explicit width is scaled/clamped appropriately.

- [ ] **Step 4: Write accessibility/basic metadata tests.** Lookup owner has appropriate role/name/description inherited/overridden from BootstrapTextBox conventions; result grid/footer controls have sensible accessible names; noninteractive status does not steal focus.

- [ ] **Step 5: Add performance regression tests for 1k/5k/10k local items.** Do not assert brittle hard millisecond SLAs. Instead assert search completes, result order is correct, repeated searches reuse member metadata/column structure, source position remains unchanged, and allocations/reflection are not pathologically multiplied by rebuilding structural objects each keystroke.

- [ ] **Step 6: Run lifecycle/performance tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapLookupLifecycleTests|FullyQualifiedName~BootstrapLookupPerformanceTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapLookupLifecycleTests|FullyQualifiedName~BootstrapLookupPerformanceTests"
```

- [ ] **Step 7: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapLookupBox.Popup.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupFooter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupDataAdapter.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapLookupSearchEngine.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupLifecycleTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapLookupPerformanceTests.cs
git commit -m "test: harden BootstrapLookup lifecycle and performance"
```

---

## Task 15: Document the component, review the exported API, update the fingerprint, and run the full dual-target verification gate

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Optional if repository docs convention prefers a dedicated guide: Create `docs/BOOTSTRAP_LOOKUP_BOX.md`

**Interfaces:** Final public API and documentation review. No new implementation features in this task.

- [ ] **Step 1: Update component documentation from the approved spec.** Document responsibility, public surface, local datasource support, result columns, SearchMembers, ranking/defaults, unmatched-text modes, Refresh/Add New, dynamic suggest, keyboard behavior, Alt+Tab semantics, and DataGridView raw-value binding. Explicitly state V1 non-goals.

- [ ] **Step 2: Add usage examples.** Include a standalone lookup example and a `BootstrapLookupColumn` example using `BindingList<OrderLine>` + `BindingSource`, ProductId raw binding, multi-column results, and optional `CommitAndAdd` string suggestions. Do not use DataTable in the recommended example.

- [ ] **Step 3: Add/extend a focused public API review test before changing the fingerprint.** In `Phase16PublicApiBaselineTests`, assert exported lookup-related type names and key public/protected member names match the reviewed spec. Explicitly assert implementation types remain non-exported:

```text
BootstrapLookupEditingControl
BootstrapLookupCell
BootstrapLookupDropDownController
BootstrapLookupDropDownContent
BootstrapLookupFooter
BootstrapLookupDataAdapter
BootstrapLookupSearchEngine
BootstrapLookupMemberAccessor
```

If DataGridView mechanics unexpectedly require exporting `BootstrapLookupCell`, stop and request a design update instead of silently expanding API.

- [ ] **Step 4: Run the public API baseline intentionally and capture the expected fingerprint failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: `ExportedApiMatchesApprovedV1Baseline` fails and prints the full API plus new SHA-256 fingerprint; all focused lookup API-shape assertions pass.

- [ ] **Step 5: Review the printed API line-by-line against `docs/superpowers/specs/2026-09-02-bootstrap-lookup-box-design.md`.** Remove accidental aliases, extra setters, public internals, or missing XML docs before accepting the fingerprint. Do not update the fingerprint until this review is clean.

- [ ] **Step 6: Update `ApprovedV1Fingerprint` to the reviewed value, then rerun the release test on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: pass.

- [ ] **Step 7: Run the complete build gate.**

```powershell
./build.ps1 -Configuration Release
```

Expected: src, tests, and demo build for both `net48` and `net8.0-windows` with zero warnings/errors.

- [ ] **Step 8: Run the complete automated test gate.**

```powershell
./test.ps1 -Configuration Release -SkipBuild
```

Expected: all non-explicit tests pass on both target frameworks.

- [ ] **Step 9: Run final interactive/manual regression checklist on Windows.** Verify:

```text
Standalone lookup:
- typing/debounce/AutoOpen
- accent-insensitive multi-token search
- ranking and highlight preservation
- mouse result commit
- keyboard Up/Down/Home/End/PageUp/PageDown
- Enter modes
- Tab resolve
- Escape restore
- Refresh
- Add New
- CommitAndAdd
- Alt+Tab closes only and reactivation stays closed

DataGridView:
- edit existing ProductId cell
- raw value vs formatted display
- Tab across hidden/read-only cells
- Enter policy matrix
- validation blocks cell change
- native new-row AddNew with BindingSource/BindingList
- dynamic Unit suggestion
- BindingSource Position does not move from search/highlight
- no duplicated active editor/focus visuals

Themes/lifecycle:
- Light/Dark
- resize/form move
- supported DPI matrix
- close/dispose while popup open
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
Task 4  pure search engine
  |
Task 5  BootstrapLookupBox core state
  |
Task 6  commit resolver + CommitAndAdd
  |
Task 7  popup content/footer
  |
Task 8  popup controller/overlay
  |
Task 9  search/debounce/highlight integration
  |
Task 10 standalone real keyboard/focus/activation hardening
  |
Task 11 DataGridView column/cell/editing-control integration
  |
Task 12 real DataGridView lifecycle/new-row interaction hardening
  |
Task 13 replacement integrated demo
  |
Task 14 theme/DPI/disposal/performance hardening
  |
Task 15 docs + public API review + full verification
```

Do not start Task 11 before standalone lookup state/commit/popup/keyboard semantics are stable; otherwise DataGridView-specific behavior will hide core-state defects and recreate the lifecycle coupling the design is intended to avoid.

## Definition of Done

Implementation is complete only when all of the following are true:

- `BootstrapLookupBox` works as a standalone single-selection lookup and keeps committed state independent from pending query/highlight state.
- Local search supports approved Vietnamese normalization, multi-token AND matching, SearchMembers, ranking, debounce, minimum length, and empty-query policies without mutating source currency/order.
- `RestorePreviousSelection`, `KeepFocusWithValidationError`, and atomic `CommitAndAdd` behave exactly as specified.
- Results popup uses `BootstrapDataGridView` with multi-column definitions and footer status/Refresh/Add New; keyboard focus stays in the main editor.
- Alt+Tab/app deactivation only closes presentation and never commits/rolls back/validates/moves; activation does not auto-open.
- `BootstrapLookupEditingControl` is the real DataGridView editor. There is no native textbox + overlay-editor workaround and no `SendKeys` navigation.
- `BootstrapLookupColumn` commits raw `ValueMember` values, exposes row/cell-context events, and preserves native DataGridView navigation/validation semantics.
- Native `BindingSource` / `BindingList<T>` new-row editing works with `AllowUserToAddRows = true`.
- Search/highlight does not change `BindingSource.Position` and transient activity does not dirty the grid.
- The old DataGrid + BootstrapSelect overlay demo is replaced by the new typed BindingList/BindingSource lookup workflow.
- Lifecycle, theme, DPI, performance-regression, keyboard/focus/window interaction, and disposal tests pass.
- Public API exports only the reviewed contract and the baseline fingerprint is updated only after explicit API inspection.
- `./build.ps1 -Configuration Release` passes for both TFMs.
- `./test.ps1 -Configuration Release -SkipBuild` passes for both TFMs.
