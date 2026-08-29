# BootstrapSelect Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved `BootstrapSelect` Select2-style WinForms control with single/multiple selection, local filtering, async paged providers, grouping, custom values, custom rendering, robust overlay behavior, keyboard/accessibility support, and dual-target compatibility.

**Architecture:** Add a new `BootstrapSelect : UserControl` without changing `BootstrapComboBox`. Keep selection, local result normalization, async search/paging, popup/overlay ownership, and painting as separate concerns. Reuse the existing overlay host/placement engine, use a real WinForms text editor for search, and render result rows through an owner-rendered viewport instead of child controls.

**Tech Stack:** C# 12, WinForms, `net48;net8.0-windows`, NUnit 4, existing theme/icon/rendering/overlay infrastructure, `System.Threading.Tasks`, `CancellationToken`, GDI+/`TextRenderer`.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

## Global Constraints

- Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the relevant sections of `docs/COMPONENTS.md`, and the approved spec before editing product code.
- Preserve `BootstrapComboBox` as the existing native-backed `ComboBox`; do not change its inheritance, public API, popup behavior, or tests to implement this feature.
- Reuse `BootstrapOverlayDropDown`, `BootstrapOverlaySurface`, `BootstrapOverlayAnchorTracker`, `BootstrapOverlayPlacement`, `BootstrapOverlayCollisionBehavior`, and `BootstrapOverlayPlacementEngine`; do not introduce a second popup/placement engine.
- Preserve the existing `BootstrapValidationState` contract exactly. Do not add a `Warning` member as part of this feature.
- Keep local `Items` mode and async `DataProvider` mode mutually exclusive at runtime. When `DataProvider != null`, preserve but ignore local `Items`; never merge them.
- `BootstrapSelectItem.Value` is non-null, immutable, and the sole logical identity. All deduplication and selection reconciliation use `ValueComparer`.
- Caller-injected `DataProvider`, `Matcher`, `Renderer`, items, icons, and tags are caller-owned and must not be disposed by `BootstrapSelect`.
- Result rows use fixed DPI-scaled heights in v1 and are owner-rendered. Never create one WinForms child control per result item.
- The provider is transport-agnostic. Do not add URL, HTTP method, headers, JSON mapping, or networking dependencies.
- Public types and members require XML documentation because the core project treats CS1591 as an error.
- Keep nullable annotations valid on both target frameworks and avoid runtime APIs unavailable on `net48` unless guarded by existing compatibility helpers.
- Follow TDD: create or extend the named tests first, observe the expected failure, add the smallest implementation, then rerun the focused tests.
- After each task, build/test both target frameworks where that task has public/product impact. All commands are intended to run on Windows.
- Do not update the public API fingerprint until the final exported surface has been reviewed from the intentional compatibility-test failure.

---

## Task 1: Add the public item, collection, mode, change-reason, and event contracts

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectChangeReason.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectItemCollectionTests.cs`

**Interfaces:**
- Consumes: `IconDescriptor` from the existing icon infrastructure.
- Produces: stable public item/collection/event types used by every later task.

- [ ] **Step 1: Write failing item-contract tests.**

Add tests that require non-null `Value`, non-null `Text`, immutable `Value`, mutable presentation metadata, and no ownership/disposal behavior:

```csharp
[Test]
public void ItemRequiresValueAndTextAndKeepsIdentityImmutable()
{
    Assert.That(() => new BootstrapSelectItem(null!, "Alpha"), Throws.TypeOf<ArgumentNullException>());
    Assert.That(() => new BootstrapSelectItem(1, null!), Throws.TypeOf<ArgumentNullException>());

    var item = new BootstrapSelectItem(42, "Alpha")
    {
        Disabled = true,
        Group = "Customers",
        Tag = "domain-object"
    };

    Assert.Multiple((Action)(() =>
    {
        Assert.That(item.Value, Is.EqualTo(42));
        Assert.That(item.Text, Is.EqualTo("Alpha"));
        Assert.That(item.Disabled, Is.True);
        Assert.That(item.Group, Is.EqualTo("Customers"));
        Assert.That(item.Tag, Is.EqualTo("domain-object"));
        Assert.That(typeof(BootstrapSelectItem).GetProperty(nameof(BootstrapSelectItem.Value))!.CanWrite, Is.False);
    }));
}
```

- [ ] **Step 2: Write failing collection tests.**

Require null guards and deterministic change notification to the owner through an internal callback while keeping a normal `IList<BootstrapSelectItem>` surface:

```csharp
[Test]
public void CollectionRejectsNullAndNotifiesOnMutation()
{
    var changes = 0;
    var items = new BootstrapSelectItemCollection(() => changes++);

    Assert.That(() => items.Add(null!), Throws.TypeOf<ArgumentNullException>());
    items.Add(new BootstrapSelectItem(1, "One"));
    items[0] = new BootstrapSelectItem(2, "Two");
    items.RemoveAt(0);

    Assert.That(changes, Is.EqualTo(3));
}
```

If the constructor receiving the callback must remain internal, keep the public parameterless constructor and use the repository's existing internal-test access mechanism; do not make the callback public merely for tests.

- [ ] **Step 3: Run the new tests and verify they fail because the types do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectItem"
```

Expected: compile/test failure naming missing `BootstrapSelectItem`, `BootstrapSelectItemCollection`, or related contracts.

- [ ] **Step 4: Implement the public contracts with XML documentation.**

Use these exact semantic contracts:

```csharp
public enum BootstrapSelectMode
{
    Single = 0,
    Multiple = 1
}

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

`BootstrapSelectEventArgs.cs` must define the event-argument types needed by the approved event model, including cancellable item changes and non-cancellable post-change events. Each item-change event args instance must expose `Item` and `Reason`; cancellable args expose `Cancel`.

- [ ] **Step 5: Run item/collection tests on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectItem"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectItem"
```

Expected: all focused tests pass.

- [ ] **Step 6: Commit the contracts.**

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

**Interfaces:**
- Consumes: `BootstrapSelectItem`, `BootstrapSelectMode`, `IEqualityComparer<object>`.
- Produces: internal deterministic selection mutations for the public control to apply and publish as events later.

- [ ] **Step 1: Write failing tests for identity, duplicate prevention, and order.**

Cover same-value/different-instance items, a custom comparer, single replacement, multiple insertion order, and disabled candidates:

```csharp
[Test]
public void MultipleSelectionUsesValueComparerInsteadOfReferenceIdentity()
{
    var state = new BootstrapSelectSelectionState(BootstrapSelectMode.Multiple, EqualityComparer<object>.Default);
    var first = new BootstrapSelectItem(7, "Seven A");
    var sameValue = new BootstrapSelectItem(7, "Seven B");

    Assert.That(state.TrySelect(first, BootstrapSelectChangeReason.Programmatic).Changed, Is.True);
    Assert.That(state.TrySelect(sameValue, BootstrapSelectChangeReason.Programmatic).Changed, Is.False);
    Assert.That(state.SelectedItems, Has.Count.EqualTo(1));
    Assert.That(state.SelectedItems[0].Value, Is.EqualTo(7));
}
```

- [ ] **Step 2: Write failing tests for mode changes and batch clear.**

Require:

```text
Single -> Multiple: preserve selection
Multiple -> Single: preserve first selection
Clear: produce one batch mutation containing the allowed removals
Disabled selected item: may be deselected
Disabled unselected item: may not be newly selected
```

Keep public event cancellation out of this internal type; the state engine should expose enough mutation detail for `BootstrapSelect` to perform preflight cancellation before commit.

- [ ] **Step 3: Run the focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
```

Expected: compile/test failure for missing internal selection types.

- [ ] **Step 4: Implement selection state as pure logic.**

The state object must expose read-only snapshots and operations that never depend on WinForms handles. Use `ValueComparer` for every lookup and duplicate check. Keep selected presentation snapshots independent from the current result set.

The mutation result must distinguish:

```text
no change
single selection replacement
item addition
item removal
batch removal
mode conversion
metadata refresh without logical selection change
```

Do not raise public events from the pure state engine.

- [ ] **Step 5: Add reconciliation tests and implementation.**

A provider/local result with the same logical `Value` may refresh selected presentation metadata without changing selected order or reporting a logical selection change:

```csharp
[Test]
public void ReconcileSameValueRefreshesPresentationWithoutLogicalChange()
{
    var state = CreateMultipleState();
    state.TrySelect(new BootstrapSelectItem(42, "Old name"), BootstrapSelectChangeReason.Programmatic);

    var mutation = state.Reconcile(new BootstrapSelectItem(42, "New name"));

    Assert.That(mutation.SelectionChanged, Is.False);
    Assert.That(state.SelectedItems.Single().Text, Is.EqualTo("New name"));
}
```

- [ ] **Step 6: Run selection tests on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectSelectionStateTests"
```

Expected: pass.

- [ ] **Step 7: Commit selection logic.**

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

**Interfaces:**
- Consumes: local item collection, matcher, selected-value predicate, `AllowCustomValues`, current search text.
- Produces: immutable/logical result-row sequence for the viewport, independent of drawing.

- [ ] **Step 1: Write failing matcher tests.**

Require case-insensitive `Text` matching and safe empty-query behavior:

```csharp
[TestCase("Customer Alpha", "alpha", true)]
[TestCase("Customer Alpha", "ALPHA", true)]
[TestCase("Customer Alpha", "supplier", false)]
[TestCase("Customer Alpha", "", true)]
public void DefaultMatcherUsesCaseInsensitiveTextContains(string text, string query, bool expected)
{
    var matcher = new BootstrapSelectTextMatcher();
    Assert.That(matcher.IsMatch(new BootstrapSelectItem(1, text), query), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Write failing result normalization tests.**

Cover grouped and ungrouped rows, hidden empty groups, disabled item preservation, selected-state projection, and adjacent group-header suppression across appended pages.

The internal row-kind enum must contain exactly the v1 logical categories required by the spec:

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

- [ ] **Step 3: Write the exact-match custom-value test.**

Prove exact matching does not use a fuzzy custom matcher:

```csharp
[Test]
public void CustomValueSuppressionUsesTextEqualityNotMatcher()
{
    var item = new BootstrapSelectItem(1, "ABC Corporation");
    Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(new[] { item }, "abc"), Is.False);
    Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(new[] { new BootstrapSelectItem(2, "ABC") }, "abc"), Is.True);
}
```

- [ ] **Step 4: Run the focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
```

Expected: compile/test failure for missing matcher/result types.

- [ ] **Step 5: Implement matcher and result normalization as pure logic.**

Keep grouping as `BootstrapSelectItem.Group` metadata. A group header is not selectable. Do not create a public group model. The result builder must accept already-loaded remote items as an input later, so it must not depend directly on `BootstrapSelectItemCollection`.

- [ ] **Step 6: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectMatcherTests|FullyQualifiedName~BootstrapSelectResultSetTests"
```

Expected: pass.

- [ ] **Step 7: Commit local search/result logic.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectMatcher.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectTextMatcher.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultRow.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultBuilder.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectMatcherTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultSetTests.cs
git commit -m "feat: add BootstrapSelect local result model"
```

---

## Task 4: Add renderer contracts, selection-surface layout, and the first `BootstrapSelect` shell

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectHitTestInfo.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLayoutTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`

**Interfaces:**
- Consumes: theme manager/tokens, `IconDescriptor`/`IIconRenderer`, `BootstrapValidationState`, selection state.
- Produces: designer-safe public control surface and stateless public renderer extension point.

- [ ] **Step 1: Write failing geometry tests before painting.**

Cover single text/placeholder bounds, clear hit target, arrow bounds, chip wrapping, maximum selection rows, long-chip clamping, and RTL mirroring. Assert geometry, not screenshot pixels.

Example:

```csharp
[Test]
public void MultipleLayoutWrapsChipsAndKeepsClearAndArrowHitTargetsInsideBounds()
{
    var layout = BootstrapSelectSelectionLayout.Calculate(
        new Rectangle(0, 0, 240, 80),
        CreateMetrics(dpi: 96),
        CreateItems("Alpha", "Beta", "Gamma", "Delta"),
        showClear: true,
        rightToLeft: false,
        maximumRows: 3);

    Assert.That(layout.ChipBounds.Count, Is.EqualTo(4));
    Assert.That(layout.ClearButtonBounds.Right, Is.LessThanOrEqualTo(240));
    Assert.That(layout.ArrowBounds.Right, Is.LessThanOrEqualTo(240));
    Assert.That(layout.RequiredHeight, Is.GreaterThan(0));
}
```

- [ ] **Step 2: Write failing default-control contract tests.**

Require at least:

```text
SelectionMode = Single
Items non-null
DataProvider = null
SelectedItem = null
SelectedValue = null
SelectedItems empty
SelectedValues empty
AllowClear = true or the approved implementation default documented in test
AllowCustomValues = false
SearchEnabled = true
MinimumSearchLength = 0
PageSize > 0
DropDownWidth = 0
MaxDropDownHeight > 0
MaximumSelectionRows = 3
ValidationState = None
BorderRadius = -1
Matcher non-null
Renderer non-null
```

Also assert `BootstrapSelect` derives from `UserControl`, not `ComboBox`.

- [ ] **Step 3: Run the focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
```

Expected: missing types/members.

- [ ] **Step 4: Implement public renderer contracts.**

The public interface must expose the approved four presentation operations:

```csharp
public interface IBootstrapSelectRenderer
{
    void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context);
    void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context);
    void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context);
    void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context);
}
```

Public contexts expose only semantic item/group data, bounds, state flags, DPI, font, and theme-derived presentation data. Never expose internal row/controller types.

- [ ] **Step 5: Implement layout and control shell.**

`BootstrapSelect` must:

- enable user painting/double buffering;
- own `BootstrapSelectItemCollection` and internal selection state;
- expose the approved properties with validation;
- subscribe/unsubscribe theme notifications consistently with existing controls;
- use `BootstrapValidationState.None/Valid/Invalid` only;
- scale geometry once for current DPI;
- render single selection/placeholder, multi chips, clear affordance, arrow, focus/open state through the renderer;
- use one calculated layout result for both paint and hit testing;
- remain safe when instantiated in the WinForms Designer.

Do not create the popup yet.

- [ ] **Step 6: Run layout/control tests on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectTests"
```

Expected: pass.

- [ ] **Step 7: Build the core project on both TFMs.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48
```

Expected: zero warnings and zero errors.

- [ ] **Step 8: Commit the visual shell.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectHitTestInfo.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLayoutTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs
git commit -m "feat: add BootstrapSelect visual shell"
```

---

## Task 5: Wire the public selection API, events, mode transitions, clear, and chip removal

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`

**Interfaces:**
- Consumes: selection engine and surface hit testing.
- Produces: complete public selection semantics before popup/search is introduced.

- [ ] **Step 1: Write failing tests for imperative selection and selected-value properties.**

Require these signatures and semantics:

```csharp
public bool Select(BootstrapSelectItem item);
public bool SelectValue(object value);
public bool Deselect(BootstrapSelectItem item);
public bool DeselectValue(object value);
public void ClearSelection();
```

`SelectedItem`/`SelectedValue` are meaningful in single mode; `SelectedItems`/`SelectedValues` reflect all selections in either mode without exposing mutable backing storage.

- [ ] **Step 2: Write failing event-order tests.**

Capture event names in a list and assert:

```text
Selecting
Selected
SelectionChanged
```

for a successful select, and no post-change events when `Selecting.Cancel = true`. Do the equivalent for deselection.

For multi-clear, require all cancellable preflight events, successful removals, and exactly one final `SelectionChanged`.

- [ ] **Step 3: Write failing mode/default tests.**

Require mode-sensitive effective `CloseOnSelect` behavior:

```text
new control + Single -> true
switch to Multiple without explicit CloseOnSelect assignment -> false
caller explicitly sets CloseOnSelect -> retain explicit value across later mode switches
```

Multiple-to-single conversion must preserve the first selected value and be atomic if any required deselection is cancelled.

- [ ] **Step 4: Run the focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: failures for missing selection API/event wiring.

- [ ] **Step 5: Implement selection orchestration in `BootstrapSelect`.**

Public events:

```csharp
public event EventHandler<BootstrapSelectItemCancelEventArgs>? Selecting;
public event EventHandler<BootstrapSelectItemEventArgs>? Selected;
public event EventHandler<BootstrapSelectItemCancelEventArgs>? Deselecting;
public event EventHandler<BootstrapSelectItemEventArgs>? Deselected;
public event EventHandler? SelectionChanged;
```

All public/programmatic calls use `BootstrapSelectChangeReason.Programmatic`. Mouse clear/chip removal pass `Clear` or `ChipRemove`. The later popup task will pass `Mouse`/`Keyboard`.

- [ ] **Step 6: Wire clear and chip-remove hit testing.**

Clear must not also open the popup. Chip removal must remove exactly the hit chip, remain possible when the item's latest metadata is disabled, and preserve selection order of remaining chips.

- [ ] **Step 7: Run focused tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: pass.

- [ ] **Step 8: Commit public selection behavior.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs
git commit -m "feat: wire BootstrapSelect selection behavior"
```

---

## Task 6: Add the local searchable popup and owner-rendered result viewport

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`

**Interfaces:**
- Consumes: result rows, renderer, matcher, selected-value predicate, existing overlay classes/placement engine.
- Produces: complete local searchable select experience with keyboard/mouse interaction.

- [ ] **Step 1: Write failing pure result-layout tests.**

Require fixed DPI-scaled row/group heights, visible-range calculation from `ScrollOffset`, hit testing by logical row, and total content height without allocating child controls.

```csharp
[Test]
public void VisibleRangeOnlyCoversRowsIntersectingViewport()
{
    var layout = BootstrapSelectResultLayout.Create(rowCount: 1000, rowHeight: 32, viewportHeight: 160, scrollOffset: 320);
    Assert.That(layout.FirstVisibleIndex, Is.EqualTo(10));
    Assert.That(layout.LastVisibleIndex, Is.EqualTo(14));
}
```

Adapt expected last-index semantics consistently if the implementation intentionally includes a partially visible trailing row; keep that choice locked by tests.

- [ ] **Step 2: Write failing popup integration tests.**

Require lazy popup creation, reuse across open/close, owner-width default sizing, and use of the existing overlay host. Reuse the STA/non-parallel test style already used by overlay tests.

- [ ] **Step 3: Write failing local search and keyboard tests.**

Cover:

```text
open -> real TextBox receives focus
printable key on closed control -> open + character in search editor
Up/Down -> selectable rows only
Home/End -> first/last selectable loaded row
Enter -> select/toggle highlighted item
Esc -> close
Tab -> close without stealing tab navigation
selected result stays in list and renders selected state
group headers and disabled rows skipped
mouse wheel scroll stays in results viewport
```

- [ ] **Step 4: Run focused tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: missing popup/view behavior.

- [ ] **Step 5: Implement the results viewport.**

`BootstrapSelectResultsView : Control` must own only viewport concerns:

```text
logical rows
scroll offset
visible range
hot/highlight index
paint
hit test
keyboard-navigation helpers
load-more threshold signal reserved for later task
```

It must never hold/call `IBootstrapSelectDataProvider`.

- [ ] **Step 6: Implement popup content with a real search editor.**

`BootstrapSelectDropDownContent` composes:

```text
TextBox searchEditor
BootstrapSelectResultsView resultsView
```

Use native text editing for IME/clipboard/caret. `SearchEnabled = false` hides/skips the editor and lets results consume the content area.

- [ ] **Step 7: Implement drop-down controller by composing existing overlay infrastructure.**

Use `BootstrapOverlaySurface` + `BootstrapOverlayDropDown` + `BootstrapOverlayAnchorTracker` + `BootstrapOverlayPlacementEngine`. Preferred placement is bottom-start with top-start fallback and existing flip/shift behavior. No new top-level `Form`, no global hook, no duplicate collision engine.

- [ ] **Step 8: Connect local query refresh and popup events.**

When local search text changes:

```text
current Items -> Matcher -> result builder -> results view
```

No debounce. `Items` mutation while open immediately rebuilds local results. Search text clears on close.

Expose and raise:

```csharp
public event EventHandler? DropDownOpened;
public event EventHandler? DropDownClosed;
```

- [ ] **Step 9: Run popup/local tests on both frameworks plus overlay regression tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay"
```

Expected: all focused Select/overlay tests pass.

- [ ] **Step 10: Commit the local popup.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs
git commit -m "feat: add BootstrapSelect local searchable popup"
```

---

## Task 7: Add async provider contracts, debounce, cancellation, generation protection, and error events

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

**Interfaces:**
- Consumes: transport-agnostic provider, popup lifecycle, result builder/view.
- Produces: page-1 async search with loading/error/retry lifecycle and stale-result safety.

- [ ] **Step 1: Write failing provider-contract tests.**

Lock immutable/read-only query semantics and page validation:

```csharp
public interface IBootstrapSelectDataProvider
{
    Task<BootstrapSelectPage> SearchAsync(
        BootstrapSelectQuery query,
        CancellationToken cancellationToken);
}
```

`BootstrapSelectQuery` contains `SearchText`, one-based `Page`, and positive `PageSize`. `BootstrapSelectPage` contains a read-only item snapshot and `HasMore`.

- [ ] **Step 2: Create deterministic test providers.**

`BootstrapSelectTestProviders.cs` must include test-only implementations capable of:

```text
immediate success
TaskCompletionSource-controlled delayed completion
cancellation honored
cancellation ignored
exception failure
recorded query history
```

Use `TaskCompletionSource<T>` instead of real sleeps for race tests.

- [ ] **Step 3: Write the mandatory out-of-order completion test.**

```csharp
[Test]
public async Task OlderGenerationCannotOverwriteNewerQueryWhenProviderIgnoresCancellation()
{
    // start query "a"
    // start query "ab"
    // complete "ab" first
    // complete "a" second
    // assert effective SearchText/results are still for "ab"
}
```

Implement the full test with controlled `TaskCompletionSource<BootstrapSelectPage>` instances; do not use `Task.Delay` to manufacture ordering.

- [ ] **Step 4: Write cancellation/error tests.**

Require:

```text
new query cancels old CTS
popup close invalidates generation
provider replacement invalidates generation and restarts current allowed query
OperationCanceledException -> no SearchFailed
other Exception -> ShowingError + SearchFailed
MinimumSearchLength not reached -> no provider call
MinimumSearchLength == 0 + open -> empty-text page 1 allowed
```

- [ ] **Step 5: Run focused async tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests"
```

Expected: missing provider/search-controller contracts.

- [ ] **Step 6: Implement provider/query/page contracts with XML docs.**

Do not add any networking-specific member. Keep `HasMore` authoritative.

- [ ] **Step 7: Implement the search controller.**

The controller owns:

```text
SearchText
current generation
active CancellationTokenSource
page 1 state
current result items
HasMore
last error/retry descriptor
```

New logical query sequence:

```text
increment generation
cancel/dispose old CTS
clear prior-query results
debounce on UI-thread WinForms Timer
call SearchAsync(page 1)
validate generation/lifecycle
marshal result to UI thread
publish loading/results/error state
```

Cancellation alone is never considered sufficient; generation must also match.

- [ ] **Step 8: Wire public async events.**

Expose:

```csharp
public event EventHandler<BootstrapSelectSearchEventArgs>? SearchStarted;
public event EventHandler<BootstrapSelectSearchCompletedEventArgs>? SearchCompleted;
public event EventHandler<BootstrapSelectSearchFailedEventArgs>? SearchFailed;
```

Event args expose search text/page and result/error information needed by callers without leaking controller internals.

- [ ] **Step 9: Integrate async mode into `BootstrapSelect`.**

When `DataProvider != null`, ignore local `Items` for result generation. Preserve local items in memory. Replacing the provider while open resets remote result state, preserves selection, and restarts the current query if `MinimumSearchLength` permits it.

- [ ] **Step 10: Run async tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectPopupTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectSearchControllerTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

Expected: pass without timing-dependent flakiness.

- [ ] **Step 11: Commit async search.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectDataProvider.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectQuery.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectPage.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDebouncer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectEventArgs.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTestProviders.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectSearchControllerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs
git commit -m "feat: add BootstrapSelect async search provider"
```

---

## Task 8: Add infinite paging, deduplication, retry, grouping across pages, and selection reconciliation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchState.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultSet.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPagingTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs`

**Interfaces:**
- Consumes: `HasMore`, viewport near-end signal, `ValueComparer`, selection reconciliation.
- Produces: stable multi-page query state and inline load-more error/retry behavior.

- [ ] **Step 1: Write failing paging tests.**

Cover all of these assertions explicitly:

```text
new query starts at page 1
page 2 starts only when HasMore == true
only one load-more request may be active
current page advances only after success
page-2 failure preserves page-1 items
retry requests page 2 again, not page 3
duplicate Value across pages appears once
empty page + HasMore true does not break state
HasMore false prevents later requests
```

- [ ] **Step 2: Write grouping-across-page tests.**

Require no duplicate adjacent group header when page 1 ends and page 2 begins with the same `Group` value.

- [ ] **Step 3: Write selection-reconciliation tests.**

Select value 42 with old text, append a later page containing value 42 with new text, then assert:

```text
selection count unchanged
selection order unchanged
selected snapshot text updated
SelectionChanged not raised
```

- [ ] **Step 4: Run paging tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests"
```

Expected: paging/retry behaviors not yet implemented.

- [ ] **Step 5: Implement load-more state and guards.**

The viewport raises a near-end signal before the absolute bottom. The controller checks:

```text
HasMore
!IsLoading
!IsLoadingMore
current generation still active
no unresolved load-more retry blocking the next page
```

Do not expose the threshold as public API in v1.

- [ ] **Step 6: Implement page merging and deduplication.**

Deduplicate using the control's `ValueComparer`. If a later item has the same logical value, prefer the newer presentation snapshot while preserving the original row/order position unless group normalization requires a deterministic rebuild.

- [ ] **Step 7: Implement inline load-more error/retry.**

First-page error remains a full error state. Later-page error appends `LoadMoreError` after existing results. Retry reuses the exact failed `SearchText`, `Page`, and `PageSize` descriptor.

- [ ] **Step 8: Reconcile selection snapshots after successful merges.**

Call selection reconciliation for each effective logical result without publishing `SelectionChanged` when only metadata changed.

- [ ] **Step 9: Run paging tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectSelectionStateTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectSelectionStateTests"
```

Expected: pass.

- [ ] **Step 10: Commit paging.**

```powershell
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

**Interfaces:**
- Consumes: `AllowCustomValues`, `CustomValueFactory`, current search text, exact-text matching.
- Produces: keyboard/mouse-accessible create action in local and async result modes.

- [ ] **Step 1: Write failing custom-value tests.**

Require:

```text
AllowCustomValues false -> no CreateValue row
empty/whitespace query -> no CreateValue row
exact text match -> no CreateValue row
partial/fuzzy match only -> CreateValue row remains allowed
factory returns null -> no selection change
factory returns item -> normal Selecting/Selected/SelectionChanged pipeline with reason CustomValue
multiple mode retains popup by default
single mode closes by default after successful creation
```

- [ ] **Step 2: Run tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectCustomValueTests"
```

Expected: create-row behavior absent.

- [ ] **Step 3: Implement create-row generation.**

Normalize the query used for display/factory invocation consistently. Do not use `Matcher.IsMatch` to suppress the create row; use dedicated case-insensitive exact `Text` equality.

- [ ] **Step 4: Implement keyboard and mouse activation.**

The create row is actionable and participates in highlight navigation. `Enter` or mouse click invokes `CustomValueFactory`; a returned item then goes through normal selection preflight/events using `BootstrapSelectChangeReason.CustomValue`.

- [ ] **Step 5: Run custom-value and interaction tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectCustomValueTests|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectCustomValueTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: pass.

- [ ] **Step 6: Commit custom values.**

```powershell
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

**Interfaces:**
- Consumes: theme notifications, DPI events, WinForms accessibility, form/owner lifecycle.
- Produces: production-safe composite control behavior across desktop environments.

- [ ] **Step 1: Write failing lifecycle race tests.**

Use a cancellation-ignoring controlled provider to require:

```text
close popup -> late completion ignored
dispose control -> late completion ignored, no BeginInvoke/ObjectDisposedException
replace provider -> old provider completion ignored
Visible=false while open -> close + cancel, selection preserved
Enabled=false while open -> close + cancel, selection preserved
handle recreation -> selection/data state preserved and overlay hooks recover
```

- [ ] **Step 2: Write failing focus tests.**

Require:

```text
Esc/internal close may restore focus to BootstrapSelect
clicking another control to dismiss popup must not steal focus back
Tab closes and advances normally
search TextBox keeps Ctrl+A/C/V/X and IME path
```

Do not intercept printable/composition keys after the real search editor owns focus.

- [ ] **Step 3: Write failing theme/DPI/RTL geometry tests.**

Cover 96, 120, 144, and 192 DPI. Assert logical scaling of border/radius/chip padding/row heights/hit targets and mirrored major horizontal affordances for `RightToLeft.Yes`.

- [ ] **Step 4: Write failing accessibility tests.**

Require a meaningful accessible object/value/state for:

```text
collapsed/expanded
focused/disabled
single selected text
multiple selected-count summary
has-popup semantics where supported by the target framework
```

Keep target-specific accessibility implementation behind conditional compilation only when the platform APIs genuinely differ.

- [ ] **Step 5: Run hardening tests and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLifecycleTests|FullyQualifiedName~BootstrapSelectAccessibilityTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
```

Expected: lifecycle/accessibility gaps fail before hardening.

- [ ] **Step 6: Implement lifecycle-safe disposal and handle management.**

Dispose in this order:

```text
mark disposing
stop debounce
invalidate generation
cancel active CTS
detach events
close popup
detach owner tracking
dispose owned popup/content/overlay
dispose timers/CTS/GDI resources
base.Dispose
```

Never dispose caller-injected provider/matcher/renderer/items.

- [ ] **Step 7: Implement theme/DPI refresh and popup reposition.**

A theme change while open repaints owner + search + results. A per-monitor DPI change remeasures owner metrics and popup bounds, then reruns shared placement. Do not cache already-scaled constants across DPI changes.

- [ ] **Step 8: Implement RTL and accessibility.**

Use mirrored layout calculations rather than ad-hoc painting branches. Expose accessible selection summary and expanded/collapsed state without creating one real child control per result row.

- [ ] **Step 9: Run all `BootstrapSelect` tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: pass.

- [ ] **Step 10: Run overlay and ComboBox regression suites.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
```

Expected: existing overlay and native ComboBox behavior remains green.

- [ ] **Step 11: Commit hardening.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSelectionLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLifecycleTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs
git commit -m "test: harden BootstrapSelect lifecycle and accessibility"
```

---

## Task 11: Add demo scenarios and documentation examples

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoProvider.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` only if `AdvancedInputsDemoForm` is not already reachable from navigation; otherwise leave `MainForm.cs` unchanged.
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `README.md`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`

**Interfaces:**
- Consumes: finished public `BootstrapSelect` API.
- Produces: discoverable manual validation surface and supported usage documentation.

- [ ] **Step 1: Write a failing demo contract test.**

Require `AdvancedInputsDemoForm` to expose controls/scenarios discoverable by the existing demo-test style for at least:

```text
local single select
local multiple select
grouped local results
custom value creation
async single select
async multiple select with paging
provider failure/retry
```

Do not assert private pixel layout details from the demo test.

- [ ] **Step 2: Run the demo contract test and confirm failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
```

Expected: demo scenarios absent.

- [ ] **Step 3: Add deterministic local demo scenarios.**

Use representative customer/product-like items with groups and at least one disabled item. Include single, multiple/chips, clear, custom values, and a long-text item for ellipsis/wrapping verification.

- [ ] **Step 4: Add a demo-only async provider.**

`BootstrapSelectDemoProvider` implements `IBootstrapSelectDataProvider`, uses an in-memory data set of at least 200 logical items, applies search server-side from the control's perspective, returns pages of 20 by default, and can deterministically simulate a failure mode toggled by the demo UI. Artificial latency may use `Task.Delay` here because this is demo behavior, but must honor the passed cancellation token.

- [ ] **Step 5: Add placement/hardening demo arrangements.**

Place at least one select near the lower/right edge of the demo content area so manual validation can observe overlay flip/shift. Include UI instructions for rapid typing, failure/retry, theme switching, and multi-selection.

- [ ] **Step 6: Document the supported API.**

`docs/COMPONENTS.md`, `docs/PACKAGE_README.md`, and `README.md` must include concrete compilable examples for:

```csharp
var select = new BootstrapSelect();
select.Items.Add(new BootstrapSelectItem(1, "Customer A"));
select.Items.Add(new BootstrapSelectItem(2, "Customer B"));
```

```csharp
select.SelectionMode = BootstrapSelectMode.Multiple;
```

and complete examples for `IBootstrapSelectDataProvider`, a custom matcher, a custom renderer, and `AllowCustomValues`/`CustomValueFactory`. Explicitly document that local `Items` and `DataProvider` are alternate modes and that `BootstrapComboBox` remains the native-backed choice when native binding/autocomplete semantics are desired.

- [ ] **Step 7: Run demo test and build demo on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
```

Expected: pass with zero warnings/errors.

- [ ] **Step 8: Manually verify the demo on Windows.**

Run:

```powershell
dotnet run --project demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
```

Verify:

```text
local single search/select/clear
local multiple chip wrap/remove/clear
custom create row
group headers/disabled row
async loading/debounce/rapid typing
async multi selection retained across queries
infinite page loading
first-page failure/retry
later-page failure/retry retaining earlier rows
popup below and flipped above near edge
light/dark theme refresh while open
keyboard-only open/search/navigation/select/deselect/close
Vietnamese IME typing in search editor
100%, 125%, 150%, 200% DPI when available
multi-monitor DPI/placement when available
```

- [ ] **Step 9: Commit demo and docs.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoProvider.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs docs/COMPONENTS.md docs/PACKAGE_README.md README.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs
git commit -m "docs: add BootstrapSelect demo and usage"
```

If `MainForm.cs` was intentionally not changed because the advanced-inputs page was already reachable, omit it from `git add` rather than making a no-op edit.

---

## Task 12: Review and approve the public API baseline, then run full validation

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `CHANGELOG.md`
- Modify: any `BootstrapSelect` source file only if the exported-surface review identifies a naming/nullability/visibility defect; rerun all affected tests before baseline approval.

**Interfaces:**
- Consumes: final exported assembly surface.
- Produces: intentional compatibility-baseline update and release-quality verification evidence.

- [ ] **Step 1: Build both core target frameworks before touching the fingerprint.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48
```

Expected: zero warnings, zero errors.

- [ ] **Step 2: Run the existing public API baseline test without changing its approved hash.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: **FAIL intentionally** because `BootstrapSelect` and its approved public contracts add exported surface. Capture the reconstructed exported surface and proposed hash from the failure output.

- [ ] **Step 3: Review the reconstructed exported surface against the approved spec before accepting any hash.**

Verify every new exported symbol is intentional. The expected families are:

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
public renderer contexts/state flags required by the renderer extension point
```

Confirm these are **not** exported:

```text
selection/search state internals
result rows/kinds/layout
popup controller/content
results viewport
retry descriptors
debouncer
overlay implementation aliases
provider test/demo helpers
```

Confirm `BootstrapValidationState` has not changed and `BootstrapComboBox` exported surface is unchanged.

If the review finds an accidental public/protected member, fix visibility/API design and return to Step 1. Do not approve the hash until the surface is correct.

- [ ] **Step 4: Update the baseline test with the reviewed hash.**

Replace only the approved fingerprint constant/mechanism used by `Phase16PublicApiBaselineTests`. Do not weaken or bypass the test.

- [ ] **Step 5: Update `docs/PUBLIC_API_BASELINE.md`.**

Add a BootstrapSelect section that records:

```text
why the addition is compatible on the RC line
all intentional exported types/members at a reviewable level
which helpers remain internal
that no existing exported signature changed
that AssemblyVersion remains 1.0.0.0
the reviewed new SHA-256 fingerprint
```

- [ ] **Step 6: Add an unreleased/RC changelog entry.**

Describe `BootstrapSelect` capability without claiming unsupported `DataSource`, built-in HTTP/AJAX, arbitrary hosted item controls, or changes to `BootstrapComboBox`.

- [ ] **Step 7: Re-run the baseline test on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: pass.

- [ ] **Step 8: Run all BootstrapSelect, overlay, and ComboBox regression tests on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect|FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapComboBox"
```

Expected: pass.

- [ ] **Step 9: Run the entire test suite on both frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48
```

Expected: all tests pass.

- [ ] **Step 10: Run the repository build script if the documented local environment prerequisites are satisfied.**

```powershell
./build.ps1
```

Expected: repository-level build/package validation succeeds. If the script requires a documented external prerequisite that is unavailable, record that exact prerequisite and retain the successful explicit dual-target build/test evidence from the prior steps.

- [ ] **Step 11: Inspect the final diff for forbidden scope expansion.**

```powershell
git diff --check
git status --short
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapOverlayPlacementEngine.cs
```

Expected:

- `git diff --check` is clean;
- no unintended `BootstrapComboBox` edits;
- any shared overlay edits are minimal, backward-compatible, justified by the approved architecture, and covered by existing/new overlay regressions;
- no generated binaries or `bin/obj` files are staged.

- [ ] **Step 12: Commit baseline and release documentation.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md CHANGELOG.md
git commit -m "docs: approve BootstrapSelect public API"
```

- [ ] **Step 13: Record final verification evidence in the implementation handoff.**

The completion report must state the actual results for:

```text
net48 core build
net8.0-windows core build
net48 full tests
net8.0-windows full tests
public API fingerprint review
manual BootstrapSelect demo checks
BootstrapComboBox regression checks
overlay regression checks
```

Do not mark the implementation complete if any mandatory item is unverified.

---

## Final Spec-Coverage Checklist

Before declaring the plan implementation complete, verify every approved design area has a corresponding passing test or explicit manual verification:

- [ ] Single selection and programmatic value selection.
- [ ] Multiple selection, ordered chips, chip removal, batch clear.
- [ ] Identity by non-null immutable `Value` and configurable `ValueComparer`.
- [ ] Selection independence from filtering/current page and metadata reconciliation.
- [ ] Local `Items` + default/custom matcher.
- [ ] Async provider mode with no networking coupling.
- [ ] Local/async mutual exclusion without destroying `Items`.
- [ ] Debounce, cancellation, generation protection, UI-thread safety.
- [ ] One-based paging, `HasMore`, near-end loading, no overlapping page requests.
- [ ] First-page error and later-page inline retry semantics.
- [ ] Duplicate-value elimination across pages.
- [ ] Grouping by `Group` metadata, including page-boundary group reconciliation.
- [ ] Opt-in custom values with exact-text suppression and synchronous factory.
- [ ] Public custom renderer with no internal-row leakage.
- [ ] Real WinForms search editor and Vietnamese IME-safe input path.
- [ ] Owner-rendered fixed-height result viewport without per-item child controls.
- [ ] Keyboard and mouse parity, including retry/create actions.
- [ ] Popup lazy creation/reuse, click-outside/Esc/Tab behavior, focus non-stealing.
- [ ] Shared overlay flip/shift/clamp and owner/DPI repositioning.
- [ ] Theme, validation, DPI, RTL, accessibility, Designer safety, handle recreation, disposal.
- [ ] `BootstrapComboBox` behavior/API unchanged.
- [ ] Public API baseline intentionally reviewed and updated only after inspecting the failure output.
- [ ] Documentation and demo cover local single, multi, async, matcher, renderer, custom values, paging, and errors.
- [ ] Both target frameworks build and full test suites pass.

## Placeholder and Type-Consistency Check

Before execution handoff, the implementer must confirm this plan contains no unresolved implementation placeholders and that names introduced by earlier tasks are used consistently by later tasks. In particular:

- `BootstrapSelectItem.Value` is `object`, never nullable in its declared contract.
- `SelectedValue` may be `null` only to represent no selection; `SelectedValues` contains values from selected items.
- `ValueComparer` is `IEqualityComparer<object>`.
- `BootstrapValidationState` is reused unchanged.
- `IconDescriptor` is the item icon type.
- `BootstrapSelectQuery.Page` is one-based.
- `BootstrapSelectPage.HasMore` is the paging authority.
- The renderer contexts are public only because callers need them to implement `IBootstrapSelectRenderer`; result rows/controllers remain internal.
- No task introduces `DataSource`, `DisplayMember`, `ValueMember`, built-in HTTP/AJAX configuration, `IAsyncEnumerable`, arbitrary per-row child controls, or variable-height rows.
