# BootstrapSelect Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved `BootstrapSelect` Select2-style WinForms control with single/multiple selection, local filtering, async paged providers, grouping, custom values, custom rendering, robust overlay behavior, keyboard/accessibility support, and dual-target compatibility.

**Architecture:** Add a new `BootstrapSelect : UserControl` without changing `BootstrapComboBox`. Keep selection, local result normalization, async search/paging, popup/overlay ownership, and painting as separate concerns. Reuse the existing overlay host/placement engine, use a real WinForms text editor for search, and render result rows through an owner-rendered viewport instead of child controls.

**Tech Stack:** C# 12, WinForms, `net48;net8.0-windows`, NUnit 4, existing theme/icon/rendering/overlay infrastructure, `System.Threading.Tasks`, `CancellationToken`, GDI+/`TextRenderer`.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

## Global Constraints

- Before product-code changes, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the relevant `docs/COMPONENTS.md` sections, and the approved spec.
- Preserve `BootstrapComboBox` as the native-backed `ComboBox`; do not change its inheritance, public API, popup behavior, or tests to implement this feature.
- Reuse `BootstrapOverlayDropDown`, `BootstrapOverlaySurface`, `BootstrapOverlayAnchorTracker`, `BootstrapOverlayPlacement`, `BootstrapOverlayCollisionBehavior`, and `BootstrapOverlayPlacementEngine`; do not add a second popup/placement engine.
- Preserve `BootstrapValidationState` exactly as it exists. This feature must not add `Warning` or any other enum value.
- Keep local `Items` and async `DataProvider` as alternate modes. `DataProvider != null` means async mode; local items remain stored but are ignored, never merged or destroyed.
- `BootstrapSelectItem.Value` is non-null, immutable, and the sole logical identity. Selection, result deduplication, and reconciliation use `ValueComparer`.
- Caller-provided provider/matcher/renderer/items/tags are caller-owned and are not disposed by `BootstrapSelect`.
- Result rows are owner-rendered with fixed DPI-scaled heights in v1. Never create one WinForms child control per item.
- Provider APIs are transport-agnostic; do not add URL/HTTP/header/JSON/network dependencies.
- Public types/members require XML documentation because the core project treats CS1591 as an error.
- Keep one shared code path for both TFMs; avoid APIs unavailable on `net48` unless an existing compatibility helper or necessary conditional implementation is used.
- Follow TDD for every task: add/extend the named test first, observe the expected failure, add the minimum implementation, and rerun the focused tests.
- Public API fingerprint changes are approved only in Task 12 after the intentional baseline failure prints the final exported surface.

### Locked v1 defaults

These defaults are part of the implementation contract and must be covered by `BootstrapSelectTests`:

```text
SelectionMode = Single
AllowClear = true
AllowCustomValues = false
SearchEnabled = true
MinimumSearchLength = 0
SearchDebounce = 250 ms
PageSize = 20
DropDownWidth = 0          // automatic, owner width is the minimum/default reference
MaxDropDownHeight = 320    // logical pixels before DPI scaling/working-area clamp
MaximumSelectionRows = 3
ValidationState = None
BorderRadius = -1          // use theme radius
Matcher = BootstrapSelectTextMatcher instance
Renderer = BootstrapSelectRenderer instance
DataProvider = null
```

`CloseOnSelect` has an effective mode default: `true` in Single mode and `false` in Multiple mode. Once explicitly assigned by the caller, that explicit value survives later mode changes.

---

## Task 1: Add public item, collection, mode, change-reason, and event contracts

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectChangeReason.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemCollectionTests.cs`

**Interfaces:** Consumes existing `IconDescriptor`; produces all basic public item/event contracts used later.

- [ ] **Step 1: Write failing item tests.** Require null guards, immutable `Value`, mutable `Text`/`Disabled`/`Group`/`Icon`/`Tag`, and exact enum values.

```csharp
[Test]
public void ItemRequiresValueAndTextAndKeepsIdentityImmutable()
{
    Assert.That(() => new BootstrapSelectItem(null!, "Alpha"), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => new BootstrapSelectItem(1, null!), Throws.TypeOf<ArgumentNullException>());

    var item = new BootstrapSelectItem(42, "Alpha") { Disabled = true, Group = "Customers", Tag = "domain" };

    Assert.Multiple((Action)(() =>
    {
        Assert.That(item.Value, Is.EqualTo(42));
        Assert.That(item.Text, Is.EqualTo("Alpha"));
        Assert.That(item.Disabled, Is.True);
        Assert.That(item.Group, Is.EqualTo("Customers"));
        Assert.That(typeof(BootstrapSelectItem).GetProperty(nameof(BootstrapSelectItem.Value))!.CanWrite, Is.False);
    }));
}
```

- [ ] **Step 2: Write failing collection tests.** `BootstrapSelectItemCollection` is a `Collection<BootstrapSelectItem>`-style public collection with a public parameterless constructor, null guards, and an internal owner-callback constructor used by `BootstrapSelect`. Insert/set/remove/clear notify once per mutation.

- [ ] **Step 3: Run the tests and verify compile/test failure for missing contracts.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectItem"
```

- [ ] **Step 4: Implement the contracts with XML docs.** Lock these declarations:

```csharp
public enum BootstrapSelectMode { Single = 0, Multiple = 1 }

public enum BootstrapSelectChangeReason
{
    Programmatic = 0,
    Mouse = 1,
    Keyboard = 2,
    Clear = 3,
    ChipRemove = 4,
    CustomValue = 5,
    ModeChange = 6
}

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

`BootstrapSelectEventArgs.cs` defines `BootstrapSelectItemEventArgs` and `BootstrapSelectItemCancelEventArgs`; both expose `Item` and `Reason`, and the cancellable type exposes `Cancel`. Constructors may remain internal because callers receive, rather than manufacture, control events.

- [ ] **Step 5: Run item tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectItem"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectItem"
```

Expected: pass.

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectMode.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectChangeReason.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItem.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItemCollection.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemCollectionTests.cs
git commit -m "feat: add BootstrapSelect item contracts"
```

---

## Task 2: Implement value identity and the internal selection engine

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionState.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionMutation.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSelectionStateTests.cs`

**Interfaces:** Consumes item/mode/comparer contracts; produces pure selection state/mutations with no WinForms-handle dependency.

- [ ] **Step 1: Write failing tests** for same-value/different-instance deduplication, custom comparer identity, single replacement, multiple insertion order, disabled-new-selection rejection, and disabled-existing-selection removal.

```csharp
[Test]
public void MultipleSelectionUsesValueComparerNotReferenceIdentity()
{
    var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
    Assert.That(state.TrySelect(new BootstrapSelectItem(7, "A"), BootstrapSelectChangeReason.Programmatic).Changed, Is.True);
    Assert.That(state.TrySelect(new BootstrapSelectItem(7, "B"), BootstrapSelectChangeReason.Programmatic).Changed, Is.False);
    Assert.That(state.SelectedItems, Has.Count.EqualTo(1));
}
```

- [ ] **Step 2: Add failing mode-transition and reconciliation tests.** Require Single→Multiple preservation, Multiple→Single first-item preservation, atomic preflight/commit support, batch-clear mutation, and metadata refresh for a same-value result without logical selection change.

- [ ] **Step 3: Run focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
```

- [ ] **Step 4: Implement pure selection logic.** Every lookup/dedup uses `ValueComparer`. The state holds a selected item snapshot independent from the current result set. It returns mutation descriptions; it does not raise public control events.

- [ ] **Step 5: Run both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionState.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionMutation.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSelectionStateTests.cs
git commit -m "feat: add BootstrapSelect selection engine"
```

---

## Task 3: Add local matching, exact-text matching, grouping, and normalized result rows

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectMatcher.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectTextMatcher.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultRow.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultBuilder.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectMatcherTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultSetTests.cs`

**Interfaces:** Consumes item sequences/matcher/selection predicate; produces drawing-independent logical rows.

- [ ] **Step 1: Write failing default matcher tests.** Require case-insensitive `Text.Contains`, empty-query match-all, null argument guards, and no implicit fuzzy/accent transformation.

- [ ] **Step 2: Write failing result-builder tests.** Cover grouped/ungrouped rows, hidden empty groups, disabled item preservation, selected-state projection, empty/instruction/error row construction, and adjacent group-header suppression across appended pages.

The internal row-kind enum is exactly:

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

- [ ] **Step 3: Lock exact-text custom-value suppression.**

```csharp
[Test]
public void ExactTextMatchIsIndependentFromMatcher()
{
    Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(
        new[] { new BootstrapSelectItem(1, "ABC Corporation") }, "abc"), Is.False);
    Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(
        new[] { new BootstrapSelectItem(2, "ABC") }, "abc"), Is.True);
}
```

- [ ] **Step 4: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
```

- [ ] **Step 5: Implement matcher and result normalization as pure logic.** Grouping remains `BootstrapSelectItem.Group`; do not add a public group tree. Builder inputs are generic loaded-item sequences so both local and remote modes reuse it.

- [ ] **Step 6: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectMatcher.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectTextMatcher.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultRow.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultBuilder.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectMatcherTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultSetTests.cs
git commit -m "feat: add BootstrapSelect local result model"
```

---

## Task 4: Add renderer contracts, selection layout, and the initial control shell

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectHitTestInfo.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLayoutTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`

**Interfaces:** Consumes theme/icon/validation/selection state; produces designer-safe control shell and public renderer extension point.

- [ ] **Step 1: Write failing geometry tests.** Cover placeholder/single text, clear/arrow hit targets, chip wrapping, 3-row limit, long-chip clamping, and RTL mirroring. Paint and hit-test must consume the same layout result.

- [ ] **Step 2: Write failing default-contract tests.** Assert every locked default above, plus `BootstrapSelect : UserControl`, empty selections, non-null matcher/renderer, and `DataProvider == null`.

- [ ] **Step 3: Run focused tests; expect missing-type/member failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
```

- [ ] **Step 4: Implement renderer API.** Export the approved operations:

```csharp
public interface IBootstrapSelectRenderer
{
    void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context);
    void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context);
    void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context);
    void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context);
}
```

`BootstrapSelectRenderContexts.cs` also defines the public `[Flags]` state needed by custom renderers (selected/highlighted/hot/disabled) without exposing internal row/controller types.

- [ ] **Step 5: Implement `BootstrapSelect` shell.** Enable double buffering, own the collection/selection state, validate properties, subscribe/unsubscribe theme changes, use only existing validation enum values, scale logical metrics once, paint single/multiple surfaces through the renderer, and remain designer-safe. Do not create a popup yet.

- [ ] **Step 6: Run both TFMs and core builds.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48
```

Expected: zero warnings/errors.

- [ ] **Step 7: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectHitTestInfo.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLayoutTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs
git commit -m "feat: add BootstrapSelect visual shell"
```

---

## Task 5: Wire public selection API, events, mode transitions, clear, and chip removal

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`

**Interfaces:** Consumes selection state/layout; produces complete public selection semantics before popup/search.

- [ ] **Step 1: Write failing tests** for:

```csharp
bool Select(BootstrapSelectItem item);
bool SelectValue(object value);
bool Deselect(BootstrapSelectItem item);
bool DeselectValue(object value);
void ClearSelection();
```

Require `SelectedItem`, `SelectedValue`, `SelectedItems`, and `SelectedValues` to remain coherent and caller read-only.

- [ ] **Step 2: Write event-order/cancellation tests.** Successful select is `Selecting -> commit -> Selected -> SelectionChanged`; cancelled pre-event produces no post-event. Mirror for deselect. Multi-clear raises per-item pre/post events but one final `SelectionChanged`.

- [ ] **Step 3: Write mode/default tests.** Require effective Single/Multiple `CloseOnSelect` defaults, explicit override persistence, Single→Multiple preservation, and atomic Multiple→Single first-item preservation when deselection cancellation is possible.

- [ ] **Step 4: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

- [ ] **Step 5: Implement public selection/events.** Public/programmatic changes use `Programmatic`; outer clear uses `Clear`; chip remove uses `ChipRemove`. Clear/chip hit targets must not simultaneously open the future popup.

- [ ] **Step 6: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs
git commit -m "feat: wire BootstrapSelect selection behavior"
```

---

## Task 6: Add local searchable popup and owner-rendered results viewport

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`

**Interfaces:** Consumes result model/renderer/matcher/selection and existing overlay infrastructure; produces complete local searchable behavior.

- [ ] **Step 1: Write failing pure viewport-layout tests.** Require fixed DPI row metrics, scroll-offset/visible-range calculations, hit testing, total height, and no item-child-control allocation.

```csharp
[Test]
public void VisibleRangeStartsAtScrollOffsetRow()
{
    var layout = BootstrapSelectResultLayout.Create(1000, rowHeight: 32, viewportHeight: 160, scrollOffset: 320);
    Assert.That(layout.FirstVisibleIndex, Is.EqualTo(10));
}
```

- [ ] **Step 2: Write failing popup tests.** Require lazy creation, reuse after close/open, owner-width default sizing, shared overlay composition, working-area placement, and `DropDownOpened`/`DropDownClosed`.

- [ ] **Step 3: Write failing keyboard/mouse tests.** Cover `Alt+Down`, `F4`, `Enter`, `Space`, printable-key open/forward, Up/Down, Home/End, PageUp/PageDown, Enter select/toggle, Esc close, Tab close/traverse, disabled/group skip, selected marker, mouse wheel containment, and selected item remaining visible in results.

- [ ] **Step 4: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

- [ ] **Step 5: Implement `BootstrapSelectResultsView`.** It owns logical rows, scroll offset, visible range, hot/highlight state, painting, hit test, navigation helpers, and a near-end signal reserved for paging. It never references/calls a data provider.

- [ ] **Step 6: Implement popup content with a real WinForms `TextBox`.** This preserves caret/clipboard/selection/IME/Unicode. `SearchEnabled=false` hides the editor; search text then remains empty. In async mode with `SearchEnabled=false`, the provider can auto-load only when `MinimumSearchLength == 0`.

- [ ] **Step 7: Implement popup controller using existing overlay types.** Compose `BootstrapOverlaySurface`, `BootstrapOverlayDropDown`, `BootstrapOverlayAnchorTracker`, and `BootstrapOverlayPlacementEngine`; bottom-start preferred, top-start fallback through existing flip/shift rules. No new `Form`, global hook, or collision engine.

- [ ] **Step 8: Wire local search.** `Items -> Matcher -> ResultBuilder -> ResultsView`, immediate with no debounce. Collection changes while open refresh results. Search text clears on close.

- [ ] **Step 9: Run Select + overlay tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs
git commit -m "feat: add BootstrapSelect local searchable popup"
```

---

## Task 7: Add async provider contracts, debounce, cancellation, generation safety, and search events

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectDataProvider.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectQuery.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectPage.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDebouncer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTestProviders.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSearchControllerTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs`

**Interfaces:** Consumes transport-agnostic provider/popup lifecycle/result builder; produces async page-1 loading/error/retry behavior.

- [ ] **Step 1: Write failing public contract tests.** Lock:

```csharp
public interface IBootstrapSelectDataProvider
{
    Task<BootstrapSelectPage> SearchAsync(BootstrapSelectQuery query, CancellationToken cancellationToken);
}

public sealed class BootstrapSelectQuery
{
    public BootstrapSelectQuery(string searchText, int page, int pageSize);
    public string SearchText { get; }
    public int Page { get; }
    public int PageSize { get; }
}

public sealed class BootstrapSelectPage
{
    public BootstrapSelectPage(IEnumerable<BootstrapSelectItem> items, bool hasMore);
    public IReadOnlyList<BootstrapSelectItem> Items { get; }
    public bool HasMore { get; }
}
```

Validate non-null search text/items, `page >= 1`, and `pageSize >= 1`.

- [ ] **Step 2: Add deterministic test providers** for immediate success, `TaskCompletionSource`-controlled completion, cancellation-honoring, cancellation-ignoring, exception failure, and query-history recording. Race tests must not depend on sleeps.

- [ ] **Step 3: Write mandatory stale-generation test.** Start `"a"`, start `"ab"`, complete `"ab"`, then complete cancellation-ignoring `"a"`; effective query/results remain `"ab"`.

- [ ] **Step 4: Write cancellation/error/minimum-length tests.** Require new query cancellation, close invalidation, provider replacement invalidation/restart, silent expected `OperationCanceledException`, `SearchFailed` for other exceptions, no call below minimum length, and empty page-1 load on open when minimum is zero.

- [ ] **Step 5: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests"
```

- [ ] **Step 6: Implement provider/query/page contracts and search controller.** Controller owns current query/generation/CTS/result state/error descriptor. New logical query sequence is: increment generation → cancel/dispose old CTS → clear previous-query results → UI-thread debounce timer → provider page 1 → generation/lifecycle check → UI-thread state publication. Cancellation alone is never the correctness guard.

- [ ] **Step 7: Add public search events.** `BootstrapSelectEventArgs.cs` adds `BootstrapSelectSearchEventArgs`, `BootstrapSelectSearchCompletedEventArgs`, and `BootstrapSelectSearchFailedEventArgs` carrying search text/page plus count/error data without leaking controllers. `BootstrapSelect` exposes `SearchStarted`, `SearchCompleted`, `SearchFailed`.

- [ ] **Step 8: Integrate async mode.** When `DataProvider != null`, local items are preserved but ignored. Replacing provider while open resets remote results, keeps selection, and restarts the current allowed query.

- [ ] **Step 9: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectPopupTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectPopupTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectDataProvider.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectQuery.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectPage.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDebouncer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTestProviders.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSearchControllerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs
git commit -m "feat: add BootstrapSelect async search provider"
```

---

## Task 8: Add infinite paging, deduplication, retry, and selection/group reconciliation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPagingTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs`

**Interfaces:** Consumes `HasMore`, near-end signal, comparer, selection state; produces stable multi-page query state.

- [ ] **Step 1: Write failing paging tests** for page-1 reset, authoritative `HasMore`, one active load-more, page advancement only after success, later-page error preserving prior items, retrying same page, duplicate values across pages, and `HasMore=false` suppression.

- [ ] **Step 2: Add group-boundary tests.** Same group ending page 1 and beginning page 2 renders one adjacent header, not two.

- [ ] **Step 3: Add selection reconciliation test.** Later item with same value/new text updates selected snapshot without count/order change or `SelectionChanged`.

- [ ] **Step 4: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests"
```

- [ ] **Step 5: Implement paging guards/merge/retry.** Near-end threshold remains internal. `ValueComparer` deduplicates pages. First-page failure uses full error state; later-page failure appends `LoadMoreError`. Retry uses exact failed query/page/page-size and does not advance page until success.

- [ ] **Step 6: Reconcile selections after successful merges** without publishing logical-selection events for metadata-only refreshes.

- [ ] **Step 7: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectSelectionStateTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectSelectionStateTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPagingTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs
git commit -m "feat: add BootstrapSelect infinite paging"
```

---

## Task 9: Add opt-in custom values and create-row interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultBuilder.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectCustomValueTests.cs`

**Interfaces:** Consumes `AllowCustomValues`, `CustomValueFactory`, query text/exact matcher; produces keyboard/mouse-accessible create action.

- [ ] **Step 1: Write failing tests** for disabled-by-default behavior, whitespace suppression, exact-text suppression, partial/fuzzy match still allowing create, null factory result causing no selection, successful factory result using `CustomValue` reason, and mode-sensitive close behavior.

- [ ] **Step 2: Run focused tests; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectCustomValueTests"
```

- [ ] **Step 3: Implement create-row generation.** Exact suppression uses dedicated case-insensitive `Text` equality, never the custom/fuzzy matcher.

- [ ] **Step 4: Implement mouse/keyboard activation.** Create row participates in highlight navigation. `Enter`/click invokes the synchronous factory; a returned item goes through normal selection preflight/events with `BootstrapSelectChangeReason.CustomValue`.

- [ ] **Step 5: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectCustomValueTests|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectCustomValueTests|FullyQualifiedName~BootstrapSelectInteractionTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultBuilder.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectCustomValueTests.cs
git commit -m "feat: add BootstrapSelect custom values"
```

---

## Task 10: Harden focus, theme, DPI, RTL, accessibility, handle recreation, and disposal

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLifecycleTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`

**Interfaces:** Consumes theme/DPI/accessibility/form lifecycle; produces production-safe desktop behavior.

- [ ] **Step 1: Write failing lifecycle race tests.** Use cancellation-ignoring controlled providers to prove late completion is ignored after close/dispose/provider replacement; hidden/disabled closes/cancels while preserving selection; handle recreation preserves state and overlay tracking recovers.

- [ ] **Step 2: Write failing focus tests.** Esc/internal close may restore focus; outside-click dismissal must not steal focus from destination; Tab closes and traverses; once search `TextBox` has focus, Ctrl+A/C/V/X and IME/composition paths are not intercepted by outer-key routing.

- [ ] **Step 3: Write failing DPI/RTL tests.** Cover 96/120/144/192 DPI and mirrored major affordances in `RightToLeft.Yes`.

- [ ] **Step 4: Write failing accessibility tests.** Require meaningful combo/select role/value/state, collapsed/expanded, disabled/focused, single selected text, multiple selected-count summary, and popup semantics where the target API supports them.

- [ ] **Step 5: Run hardening tests; expect failures.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLifecycleTests|FullyQualifiedName~BootstrapSelectAccessibilityTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
```

- [ ] **Step 6: Implement disposal/handle safety.** Dispose order: mark disposing → stop debounce → invalidate generation → cancel CTS → detach hooks → close popup → detach owner tracking → dispose owned popup/content/overlay → timers/CTS/GDI → base. Never dispose caller dependencies.

- [ ] **Step 7: Implement theme/DPI/RTL/accessibility refresh.** Open popup responds to theme changes and DPI moves without close/reopen. Metrics are logical and scaled once. RTL is layout-driven. Accessibility does not require real per-result child controls.

- [ ] **Step 8: Run all Select tests plus overlay/ComboBox regressions on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
```

Expected: pass.

- [ ] **Step 9: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLifecycleTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs
git commit -m "test: harden BootstrapSelect lifecycle and accessibility"
```

---

## Task 11: Add demo scenarios and user documentation

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoProvider.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` only if Advanced Inputs is not already reachable; otherwise do not edit it.
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `README.md`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`

**Interfaces:** Consumes final public API; produces discoverable demo/manual verification and supported usage examples.

- [ ] **Step 1: Write failing demo contract test.** Require discoverable scenarios for local single, local multi, grouped results, custom values, async single, async multi/paging, and failure/retry.

- [ ] **Step 2: Run demo test; expect failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
```

- [ ] **Step 3: Add local demo cases.** Include grouped customer/product-like data, disabled item, long text, multi chips, clear, and custom creation.

- [ ] **Step 4: Add `BootstrapSelectDemoProvider`.** Use at least 200 in-memory records, server-side-from-control-perspective filtering, page size 20, deterministic failure toggle, and cancellation-aware artificial latency.

- [ ] **Step 5: Add async/placement cases.** Include rapid typing, multi selection retained across queries, infinite paging, first/later-page retry, and a select positioned near lower/right demo edges to observe flip/shift.

- [ ] **Step 6: Document concrete examples.** `README.md`, `docs/PACKAGE_README.md`, and `docs/COMPONENTS.md` cover local single, multiple, `IBootstrapSelectDataProvider`, custom matcher, custom renderer, custom values, local-vs-provider mode rule, and when to prefer native-backed `BootstrapComboBox`.

- [ ] **Step 7: Run demo test/builds.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
```

- [ ] **Step 8: Run manual Windows demo validation.**

```powershell
dotnet run --project demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
```

Verify local search/select/clear; multi wrap/remove; create row; groups/disabled row; loading/debounce/rapid typing; paging; first/later-page retry; popup flip; light/dark live refresh; keyboard-only use; Vietnamese IME; 100/125/150/200% DPI; multi-monitor placement/DPI when available.

- [ ] **Step 9: Commit.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoProvider.cs docs/COMPONENTS.md docs/PACKAGE_README.md README.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs 2>$null
git commit -m "docs: add BootstrapSelect demo and usage"
```

If `MainForm.cs` is unchanged, the second `git add` is harmless and no no-op edit should be introduced.

---

## Task 12: Review/approve public API baseline and run full validation

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `CHANGELOG.md`
- Modify BootstrapSelect source only if exported-surface review discovers an API defect; rerun affected tasks before approval.

**Interfaces:** Consumes final assembly surface; produces intentional RC-line baseline approval and release-quality evidence.

- [ ] **Step 1: Build both core TFMs before fingerprint changes.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48
```

Expected: zero warnings/errors.

- [ ] **Step 2: Run the current baseline test without changing its hash.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: **intentional failure** showing reconstructed exported surface and proposed SHA-256.

- [ ] **Step 3: Review the export before accepting the hash.** Expected intentional public families are:

```text
BootstrapSelect
BootstrapSelectMode
BootstrapSelectChangeReason
BootstrapSelectItem
BootstrapSelectItemCollection
selection/search event args
IBootstrapSelectDataProvider
BootstrapSelectQuery
BootstrapSelectPage
IBootstrapSelectMatcher
BootstrapSelectTextMatcher
IBootstrapSelectRenderer
renderer contexts/state flags required to implement the renderer
```

These remain internal:

```text
selection/search state and mutations
result rows/kinds/layout/result set/builder
popup controller/content
results viewport
debouncer/retry descriptors
overlay implementation aliases
test/demo providers
```

Also verify `BootstrapValidationState` and `BootstrapComboBox` exported surfaces are unchanged. If an accidental public/protected member appears, fix it and repeat Steps 1–3; never approve a hash merely to make CI green.

- [ ] **Step 4: Update the reviewed fingerprint in the baseline test.** Do not weaken/bypass the test.

- [ ] **Step 5: Update `docs/PUBLIC_API_BASELINE.md`.** Record the intentional BootstrapSelect additions, internal helpers, unchanged existing signatures, unchanged `AssemblyVersion` `1.0.0.0`, and reviewed new fingerprint.

- [ ] **Step 6: Update `CHANGELOG.md` under `[Unreleased]`.** Describe only supported capabilities; do not claim DataSource binding, built-in HTTP/AJAX, per-row hosted controls, or ComboBox replacement semantics.

- [ ] **Step 7: Re-run baseline test on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: pass.

- [ ] **Step 8: Run Select + overlay + ComboBox regressions on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
```

- [ ] **Step 9: Run full tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48
```

Expected: all tests pass.

- [ ] **Step 10: Run repository build script when documented prerequisites are available.**

```powershell
./build.ps1
```

If a documented external prerequisite is unavailable, record the exact missing prerequisite and retain the explicit successful dual-target build/test evidence.

- [ ] **Step 11: Inspect final diff/scope.**

```powershell
git diff --check
git status --short
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapOverlayPlacementEngine.cs
```

Expected: clean whitespace; no unintended ComboBox edits; shared-overlay changes, if any, are minimal/backward-compatible/tested; no generated binaries/bin/obj are staged.

- [ ] **Step 12: Commit baseline/release docs.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md CHANGELOG.md
git commit -m "docs: approve BootstrapSelect public API"
```

- [ ] **Step 13: Record actual completion evidence** for net48/net8 core builds, both full test suites, baseline fingerprint review, manual demo checks, ComboBox regressions, and overlay regressions. Do not mark complete with any mandatory item unverified.

---

## Final Spec-Coverage Checklist

- [ ] Single selection and programmatic value selection.
- [ ] Multiple selection, ordered chips, chip removal, batch clear.
- [ ] Non-null immutable `Value` identity with `IEqualityComparer<object>`.
- [ ] Selection independent from filtering/page; metadata reconciliation without false selection events.
- [ ] Local items and replaceable matcher.
- [ ] Async paged provider with no networking coupling.
- [ ] Local/async mutual exclusion without destroying local items.
- [ ] 250 ms debounce, cancellation, generation guard, UI-thread safety.
- [ ] One-based paging, authoritative `HasMore`, near-end loading, no overlapping load-more.
- [ ] First-page error and later-page inline retry preserving loaded rows.
- [ ] Duplicate-value elimination across pages.
- [ ] Grouping via `Group`, including page-boundary reconciliation.
- [ ] Opt-in custom values, exact-text suppression, synchronous factory.
- [ ] Public renderer contexts/state without internal-row leakage.
- [ ] Real search `TextBox` and Vietnamese IME-safe path.
- [ ] Fixed-height owner-rendered viewport without per-item child controls.
- [ ] Keyboard/mouse parity including retry/create.
- [ ] Lazy/reused popup, click-outside/Esc/Tab, focus non-stealing.
- [ ] Shared overlay flip/shift/clamp plus owner/DPI repositioning.
- [ ] Theme, existing validation enum, DPI, RTL, accessibility, Designer, handle/disposal safety.
- [ ] `BootstrapComboBox` API/behavior unchanged.
- [ ] Public API fingerprint reviewed from intentional failure before update.
- [ ] Demo/docs cover local single/multi, async, matcher, renderer, tags, paging, errors.
- [ ] Both TFMs build and full tests pass.

## Placeholder and Type-Consistency Check

This plan intentionally leaves no implementation-choice placeholders. Keep these names/types consistent across tasks:

```text
BootstrapSelectItem.Value             object, non-null and read-only
BootstrapSelect.SelectedValue         object?; null means no selection
BootstrapSelect.SelectedValues        IReadOnlyList<object>
BootstrapSelect.ValueComparer         IEqualityComparer<object>
BootstrapSelect.ValidationState       existing BootstrapValidationState only
BootstrapSelectItem.Icon              IconDescriptor?
BootstrapSelectQuery.Page             one-based int
BootstrapSelectPage.HasMore           authoritative bool
```

Do not introduce `DataSource`, `DisplayMember`, `ValueMember`, built-in HTTP/AJAX, `IAsyncEnumerable`, arbitrary per-row child controls, variable-height result rows, a public group tree, or a second overlay engine.