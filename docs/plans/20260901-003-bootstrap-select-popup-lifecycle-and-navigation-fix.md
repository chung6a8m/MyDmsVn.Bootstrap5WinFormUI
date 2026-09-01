# BootstrapSelect Popup Lifecycle and Paging Navigation Regression Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task and `superpowers:verification-before-completion` before claiming the fix is complete. Use `superpowers:systematic-debugging` if any regression test fails for a reason different from the failure model frozen below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix two `BootstrapSelect` popup regressions: (1) an open searchable popup must close when the owning application/form loses activation, including Alt+Tab to another application, without undoing the existing rule that pressing Alt alone does not dismiss an interactive overlay; and (2) async paging completion must not reset keyboard highlight/scroll back to the first result after the user has navigated with Down/PageDown.

**Architecture:** Keep the existing shared `BootstrapOverlayDropDown` + `BootstrapOverlayAnchorTracker` + `BootstrapSelectDropDownController` architecture. Treat application switching as a **window lifecycle event**, not as an Alt+Tab key gesture: `BootstrapOverlayAnchorTracker` will subscribe to the owning `Form.Deactivate` event and request a non-focus-restoring close. Keep the narrow Alt/menu close cancellation in `BootstrapOverlayDropDown` so Alt alone remains non-destructive. For result navigation, introduce an internal explicit results-update semantic: **ResetNavigation** for a new logical result set/query and **PreserveNavigation** for later-page append/update completion. The results view will preserve the highlighted item by logical `BootstrapSelectItem.Value` using the owner’s `ValueComparer`, preserve/clamp scroll position, and fall back deterministically when the previous row no longer exists. Remote page 1 remains reset semantics; page 2+ completion/retry uses preserve semantics.

**Tech Stack:** C# 12, Windows Forms, `ToolStripDropDown`, `Form.Deactivate`, existing overlay infrastructure, `net48;net8.0-windows`, NUnit 4, controlled async providers, STA/non-parallel WinForms tests.

**Related design and corrective plans:**

- `docs/plans/20260829-005-bootstrap-select.md`
- `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`
- `docs/plans/20260831-001-popover-keyboard-focus-and-alt-dismissal-fix.md`
- `docs/plans/20260901-002-bootstrap-select-popup-sizing-fix.md`

---

## Reported regressions to lock down

### Regression A — popup survives Alt+Tab/application deactivation

Reproduction:

1. Open a searchable `BootstrapSelect` popup.
2. Keep focus inside the popup search textbox.
3. Press Alt+Tab to activate another application window.
4. The `BootstrapSelect` popup remains visible and can appear above the other application.

Current failure chain:

```text
search editor owns focus
        ↓
Alt+Tab starts ToolStrip/menu keyboard processing
        ↓
BootstrapOverlayDropDown.OnClosing(...)
CloseReason == Keyboard && Alt is held
        ↓
e.Cancel = true
        ↓
Windows activates another application
        ↓
owning Form deactivates
        ↓
BootstrapOverlayAnchorTracker has no Form.Deactivate subscription
        ↓
popup remains visible
```

The existing Alt cancellation must **not** simply be removed. It was introduced to preserve the valid behavior that pressing Alt alone must not dismiss interactive overlays. The missing piece is lifecycle closure when the owning top-level form actually loses activation.

### Regression B — async page completion resets highlight to first item

Reproduction:

1. Open the async Single `BootstrapSelect` demo.
2. Keep focus in the search textbox.
3. Navigate results with Down or PageDown.
4. Once navigation reaches the near-end threshold, page 2 starts loading.
5. When page 2 completes, highlight and viewport jump back to the first result.
6. Navigation appears more stable afterward only because the expanded result set means the next near-end threshold is farther away; the same defect can recur on later pages.

Current failure chain:

```text
Down/PageDown
    ↓
BootstrapSelectResultsView.MoveHighlight/Page
    ↓
EnsureHighlightedVisible
    ↓
CheckNearEnd
    ↓
NearEndReached / RequestRemoteNextPage
    ↓
page 2 completes
    ↓
BootstrapSelect.Search.PublishRemoteCompletion
    ↓
BootstrapSelectDropDownController.RefreshResults
    ↓
BootstrapSelectDropDownContent.SetResults
    ↓
BootstrapSelectResultsView.SetResults
    ↓
_scrollOffset = 0
_highlightedIndex = FindFirstSelectable(...)
    ↓
highlight jumps to top
```

The problem is not the Down/PageDown key handling. The problem is that every result refresh currently has replacement/reset semantics even when the logical operation is an append/update of a later async page.

---

## Required behavior contract

### Popup lifecycle matrix

| Scenario | Expected behavior |
| --- | --- |
| Press/release Alt while owning form remains active | Popup remains open; current child focus is preserved. |
| Alt+Tab / owning form deactivates | Popup closes promptly. |
| Form deactivation close | Must not explicitly restore focus to `BootstrapSelect`; the newly activated application/form must retain activation. |
| Escape | Existing close/focus-restore behavior remains unchanged. |
| Outside click with native AutoClose enabled | Existing native close behavior remains unchanged. |
| Target hidden/disposed | Existing tracker close behavior remains unchanged. |
| Form move/resize/ancestor scroll | Existing reposition behavior remains unchanged. |

### Result refresh/navigation matrix

| Result change | Navigation policy |
| --- | --- |
| Initial popup open | `ResetNavigation` |
| Search text changes / new query begins | `ResetNavigation` |
| Page 1 loading state | `ResetNavigation` |
| Page 1 completion or page-1 failure/retry result | `ResetNavigation` |
| DataProvider replacement / ValueComparer restart | `ResetNavigation` |
| Local filter/result replacement | `ResetNavigation` |
| Async page 2+ completion | `PreserveNavigation` |
| Async page 2+ failure/retry completion | `PreserveNavigation` |

For `PreserveNavigation`:

- If the previously highlighted row is an item and an equivalent item still exists, restore highlight by `BootstrapSelectItem.Value` using the current `BootstrapSelect.ValueComparer`.
- Do not rely on raw row index as primary identity; grouped results can add headers and provider merge/dedup can replace item instances.
- Preserve the previous scroll offset, clamp it to the new valid range, and make the restored highlight visible only if necessary.
- If the old highlighted item no longer exists, choose the nearest selectable row around the previous index before falling back to the normal first-selectable rule.
- Clear hot/mouse-hover state on a result-set replacement because the previous mouse row geometry may no longer be valid.
- `ResetNavigation` must keep the existing behavior: scroll to top and choose `FindFirstSelectable(preferSelected: true)`.

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, and all related plans listed above before product-code changes.
- [ ] Preserve the existing public API. This corrective work requires **no new public property, event, enum, or method**.
- [ ] Preserve the shared implementation for `net48;net8.0-windows`.
- [ ] Keep `BootstrapOverlayDropDown` as the popup window implementation. Do not create a second popup type, top-most helper form, global keyboard hook, global mouse hook, CBT hook, polling timer, or Win32 Alt+Tab detector.
- [ ] Do not “fix” regression A by removing the current Alt keyboard-close cancellation wholesale. Alt alone must remain non-destructive.
- [ ] Do not detect Alt+Tab by checking `Keys.Tab` in `ProcessCmdKey`. The close signal is owning-form deactivation.
- [ ] A deactivation-driven close must follow the same non-focus-restoring path already used by tracker lifecycle closure: `Close(false)`.
- [ ] Keep Escape close/focus restore, outside-click AutoClose, Tab traversal, overlay geometry, DPI handling, rounded clipping, and placement behavior intact.
- [ ] Preserve the popup sizing correction from `20260901-002`: result refresh while open must still recompute/reposition bounds after rows change.
- [ ] Do not suppress `NearEndReached`, delay paging, increase `PageSize`, or change demo provider timing to hide regression B.
- [ ] Do not preserve navigation by raw row index alone. The primary restoration key for item rows is `ValueComparer.Equals(old.Item.Value, new.Item.Value)`.
- [ ] Do not use object reference identity for provider items; later pages may refresh/deduplicate an existing value with a new `BootstrapSelectItem` instance.
- [ ] Use controlled providers/message pumping for async integration tests. Do not add arbitrary `Thread.Sleep` delays to make race timing pass.
- [ ] Add the failing regression test before each corresponding product change, run it to observe the expected failure, make the minimum correction, then rerun focused and broader tests.
- [ ] WinForms focus/activation tests must run STA and non-parallel where activation state or popup windows are involved.

---

## File structure and responsibilities

### Product files expected to change

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`
  - Add owning-form deactivation tracking.
  - Request close when the anchor’s owning form loses activation.
  - Subscribe/unsubscribe symmetrically when the target is reparented or tracker is disposed.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsUpdateMode.cs` **(new internal type)**
  - Define `ResetNavigation` and `PreserveNavigation` semantics explicitly.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
  - Make result replacement aware of update mode.
  - Capture/restore highlight identity and scroll for preserve updates.
  - Continue clearing hover state and checking near-end after the new state is coherent.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Forward explicit result-update mode and `ValueComparer` to the results view.
  - Expose the existing navigation operation internally if needed by the existing test seam; do not expose a new public API.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Accept an explicit refresh mode, defaulting to reset semantics.
  - Preserve the `RefreshResults() -> Reposition()` invariant introduced by the popup-sizing fix.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
  - Select reset vs preserve semantics based on remote page number.
  - Page 1: reset; page 2+: preserve.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add at most a narrow internal test/navigation forwarding seam if the provider integration test needs to move highlight without unreliable `SendKeys`.
  - No public API change.

### Product file to inspect and regression-test, but not change by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
  - Keep the current narrow Alt keyboard-close cancellation unless a new focused regression proves it needs refinement.
  - The planned fix for application switching is form lifecycle tracking, not broader ToolStrip keyboard surgery.

### Tests expected to change

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
  - Add deterministic `Form.Deactivate` close coverage and unsubscription coverage.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
  - Add popup-level owning-form deactivation regression coverage.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
  - Add reset/preserve navigation unit tests, including logical-value identity replacement.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`
  - Add deterministic page-2 completion test that reproduces the keyboard highlight reset.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
  - Re-run current host-level tests; add a test only if needed to freeze Alt-close semantics at the shared host layer.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
  - Re-run existing Alt/Tab/Escape/outside-click regressions because `BootstrapOverlayAnchorTracker` is shared infrastructure.

### Documentation/manual acceptance

- `docs/TESTING.md`
  - Record Alt-alone vs Alt+Tab behavior and async paging keyboard navigation checks.

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
  - Use as the manual acceptance surface. Do not add demo-only key handlers or timing workarounds.

---

## Task 1: Freeze and fix owning-form deactivation at the shared tracker layer

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`
- Inspect only: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`

**Interfaces:** Internal shared overlay lifecycle only. Constructor signature remains `BootstrapOverlayAnchorTracker(Control target, Action reposition, Action close)`.

### Step 1: Add a deterministic failing `Form.Deactivate` tracker test

- [ ] Add a small test form subclass so the protected lifecycle event can be raised deterministically without relying on desktop/CI window activation timing:

```csharp
private sealed class TestForm : Form
{
    internal void RaiseDeactivate()
    {
        OnDeactivate(EventArgs.Empty);
    }
}
```

- [ ] Add:

```csharp
[Test]
public void FormDeactivateRequestsClose()
{
    using var form = new TestForm();
    using var target = new Button();
    form.Controls.Add(target);
    var reposition = 0;
    var close = 0;
    using var tracker = new BootstrapOverlayAnchorTracker(
        target,
        () => reposition++,
        () => close++);

    form.RaiseDeactivate();

    Assert.That(close, Is.EqualTo(1));
}
```

This test must fail on current `main` because the tracker subscribes to `Move`, `Resize`, and `FormClosed`, but not `Deactivate`.

### Step 2: Extend disposal/reparent coverage before changing product code

- [ ] Extend the test suite so a disposed tracker no longer reacts to `Form.Deactivate`.
- [ ] Add or extend a reparent test where the target moves from one form to another; deactivating the old form must not request close, while deactivating the new owning form must request close exactly once.

These assertions protect against leaked form subscriptions during `RebuildAncestorSubscriptions()`.

### Step 3: Run the focused failing tests

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayAnchorTrackerTests"
```

Expected before implementation: the new deactivation test fails; existing geometry/visibility/disposal tests pass.

### Step 4: Subscribe and unsubscribe `Form.Deactivate`

- [ ] In `RebuildAncestorSubscriptions()`, when `_form` is available, add:

```csharp
_form.Deactivate += OnFormDeactivate;
```

- [ ] Add the narrow handler:

```csharp
private void OnFormDeactivate(object? sender, EventArgs e)
{
    RequestClose();
}
```

- [ ] In `UnsubscribeAncestors()`, remove the same handler before clearing `_form`:

```csharp
_form.Deactivate -= OnFormDeactivate;
```

Do not add activation polling or inspect `ModifierKeys` here. `Deactivate` is already the semantic lifecycle event required by this fix.

### Step 5: Run tracker tests on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayAnchorTrackerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapOverlayAnchorTrackerTests"
```

Expected: all pass.

### Step 6: Commit the shared lifecycle correction

- [ ] Commit only Task 1 files:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs
git commit -m "fix: close anchored overlays when owner deactivates"
```

---

## Task 2: Lock BootstrapSelect deactivation behavior without regressing Alt-alone semantics

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Product change expected from Task 1; change `BootstrapOverlayDropDown.cs` only if a focused test proves a distinct defect.

### Step 1: Add a popup-level deactivation regression

- [ ] Add a test form subclass in `BootstrapSelectPopupTests` with `RaiseDeactivate()` as above.
- [ ] Create a real shown form + searchable `BootstrapSelect`, open the popup, and assert it is open before deactivation.
- [ ] Raise owning-form deactivation and pump messages.
- [ ] Assert:

```csharp
Assert.That(select.IsDropDownOpenForTest, Is.False);
```

Also count `DropDownClosed` and assert it fires once.

Suggested test name:

```text
OwningFormDeactivateClosesOpenPopup
```

The popup controller already constructs the tracker with `() => Close(false)`, so this test validates the full path from form lifecycle event to BootstrapSelect popup closure without adding BootstrapSelect-specific deactivation handlers.

### Step 2: Freeze the no-focus-restoration intent

- [ ] Keep the production callback in `BootstrapSelectDropDownController.Open()` exactly on the non-restoring path:

```csharp
_tracker = new BootstrapOverlayAnchorTracker(
    _owner,
    Reposition,
    () => Close(false));
```

- [ ] If the existing popup test harness can observe active control deterministically, add an assertion that deactivation closure does not call `owner.Focus()`. Do not introduce global activation hooks solely for this assertion; the code-level `Close(false)` contract plus manual Alt+Tab acceptance below is sufficient if CI cannot deterministically own desktop activation.

### Step 3: Re-run existing Alt-alone/keyboard overlay regressions

- [ ] Run shared popup/popover tests before changing any `BootstrapOverlayDropDown` logic:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayDropDownTests|FullyQualifiedName~BootstrapPopoverTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

Expected:

- BootstrapSelect owning-form deactivation test passes after Task 1.
- Existing Popover Alt regression remains green: Alt alone does not close the overlay.
- Escape, Tab traversal, outside-click behavior, bounds correction, and clipping remain green.

### Step 4: Do not broaden the fix unless evidence requires it

- [ ] Leave this current `BootstrapOverlayDropDown.OnClosing()` policy unchanged by default:

```csharp
if (e.CloseReason == ToolStripDropDownCloseReason.Keyboard
    && (ModifierKeys & Keys.Alt) == Keys.Alt)
{
    e.Cancel = true;
    return;
}
```

The new `Form.Deactivate` signal resolves the reported application-switch lifecycle hole while retaining the Alt-alone behavior.

### Step 5: Run both TFMs and commit the integration guard

- [ ] Run the focused popup tests on both TFMs.
- [ ] Commit the new regression test:

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "test: cover BootstrapSelect owner deactivation"
```

---

## Task 3: Introduce explicit reset vs preserve result-update semantics

**Files:**

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsUpdateMode.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`

**Interfaces:** Internal-only enum and overload. No public BootstrapSelect API change.

### Step 1: Add unit helpers that build deterministic selectable result sets

- [ ] Extend `BootstrapSelectResultsViewTests` with helpers that create item rows with stable logical values.
- [ ] Include a grouped-result helper for at least one test so the preservation logic is not accidentally tied to item row index.

Example test data shape:

```text
Group A header
Customer 001 (Value = 1)
...
Customer 020 (Value = 20)
Group B header
Customer 021 (Value = 21)
...
```

### Step 2: Add a failing preservation test for append semantics

- [ ] Size the view to a deterministic multi-row viewport, load page-1 rows, move highlight near the end, and record:

```csharp
var highlightedValue = view.HighlightedRow!.Item!.Value;
var scrollOffset = view.ScrollOffset;
```

- [ ] Replace results with page 1 + page 2 using preserve semantics and assert:

```csharp
Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(highlightedValue));
Assert.That(view.ScrollOffset, Is.EqualTo(scrollOffset));
```

If the expanded result layout requires a minimal scroll adjustment to keep the row visible, assert that the viewport did not jump back to zero rather than hard-coding an invalid pixel value.

Suggested test name:

```text
PreserveNavigationKeepsHighlightedItemAndScrollWhenRowsAppend
```

### Step 3: Add a failing logical-identity replacement test

- [ ] Start with a highlighted item such as `Value = "ABC"` using an ordinal-ignore-case comparer.
- [ ] Replace the result set with a new `BootstrapSelectItem("abc", "Refreshed")` instance at a potentially different row index because a group header/merge changed.
- [ ] Preserve navigation and assert the refreshed item is highlighted.

This proves the implementation uses the supplied `ValueComparer`, not object reference or raw index.

Suggested test name:

```text
PreserveNavigationMatchesRefreshedItemByValueComparer
```

### Step 4: Add a reset-semantics control test

- [ ] Navigate/scroll away from the top, replace with a new query result set using reset semantics, and assert:

```csharp
Assert.That(view.ScrollOffset, Is.Zero);
Assert.That(view.HighlightedRow, Is.SameAs(/* first selectable/preferred selected row */));
```

Suggested test name:

```text
ResetNavigationReturnsToFirstSelectableForNewResultSet
```

### Step 5: Run the new tests and confirm current behavior is insufficient

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
```

Expected before implementation: preserve tests cannot pass because `SetResults()` unconditionally sets `_scrollOffset = 0` and recomputes the first highlight.

### Step 6: Add the internal update-mode enum

- [ ] Create:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapSelectResultsUpdateMode
{
    ResetNavigation,
    PreserveNavigation
}
```

Keep this internal; it describes implementation semantics, not a user-facing configuration option.

### Step 7: Make `SetResults` mode-aware

- [ ] Keep an internal reset shorthand if it reduces churn in existing unit tests:

```csharp
internal void SetResults(BootstrapSelectResultSet results)
{
    SetResults(
        results,
        BootstrapSelectResultsUpdateMode.ResetNavigation,
        EqualityComparer<object>.Default);
}
```

- [ ] Add the explicit path used by the popup controller:

```csharp
internal void SetResults(
    BootstrapSelectResultSet results,
    BootstrapSelectResultsUpdateMode updateMode,
    IEqualityComparer<object> valueComparer)
```

Validate `results` and `valueComparer` for null.

### Step 8: Capture old navigation before replacing rows

- [ ] Before assigning `_results`, capture:

```csharp
var previousRow = HighlightedRow;
var previousIndex = _highlightedIndex;
var previousScrollOffset = _scrollOffset;
```

Then assign the new result set and always reset `_hotIndex = -1`.

### Step 9: Implement `ResetNavigation` exactly as the existing behavior

- [ ] For reset mode:

```csharp
_scrollOffset = 0;
_highlightedIndex = FindFirstSelectable(preferSelected: true);
EnsureHighlightedVisible();
```

This keeps new-query/initial-open behavior stable.

### Step 10: Implement `PreserveNavigation` by logical value first

- [ ] Add a helper equivalent to:

```csharp
private int FindEquivalentItemIndex(
    BootstrapSelectResultRow? previousRow,
    IEqualityComparer<object> valueComparer)
{
    if (previousRow?.Kind != BootstrapSelectResultRowKind.Item
        || previousRow.Item is null)
    {
        return -1;
    }

    for (var i = 0; i < _results.Rows.Count; i++)
    {
        var candidate = _results.Rows[i];
        if (candidate.Kind == BootstrapSelectResultRowKind.Item
            && candidate.Item is not null
            && IsSelectable(candidate)
            && valueComparer.Equals(previousRow.Item.Value, candidate.Item.Value))
        {
            return i;
        }
    }

    return -1;
}
```

- [ ] If a logical item match is found, restore that index.
- [ ] If no logical match exists, choose the nearest selectable row around the clamped previous index; only fall back to `FindFirstSelectable(preferSelected: true)` if no nearby selectable row exists.

### Step 11: Restore and clamp scroll without top-jumping

- [ ] For preserve mode, restore the prior scroll offset first:

```csharp
_scrollOffset = Math.Max(0, previousScrollOffset);
ClampScroll();
EnsureHighlightedVisible();
```

The append case should normally leave the old offset unchanged because adding rows increases the valid range. `EnsureHighlightedVisible()` may make a minimal correction if merge/group changes moved the logical item.

- [ ] Finish both modes with:

```csharp
Invalidate();
CheckNearEnd();
```

Only call `CheckNearEnd()` after highlight and scroll state are coherent.

### Step 12: Run unit tests on both TFMs and commit

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
```

- [ ] Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsUpdateMode.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs
git commit -m "fix: preserve BootstrapSelect result navigation state"
```

---

## Task 4: Route explicit update semantics through popup content/controller

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`

### Step 1: Forward update mode and comparer through content

- [ ] Add an explicit content overload:

```csharp
internal void SetResults(
    BootstrapSelectResultSet results,
    BootstrapSelectResultsUpdateMode updateMode,
    IEqualityComparer<object> valueComparer)
{
    _resultsView.SetResults(results, updateMode, valueComparer);
}
```

- [ ] Keep the existing reset-only `SetResults(results)` shorthand only if existing tests or local call sites use it. Its semantics must be unambiguously reset, not preserve.

### Step 2: Make controller refresh semantics explicit

- [ ] Change the controller to:

```csharp
internal void RefreshResults(
    BootstrapSelectResultsUpdateMode updateMode = BootstrapSelectResultsUpdateMode.ResetNavigation)
{
    if (_content is null) return;

    var results = _owner.BuildCurrentPopupResultSet(
        _content.SearchEnabled ? _content.SearchText : string.Empty);

    _content.SetResults(results, updateMode, _owner.ValueComparer);

    if (_isOpen)
    {
        Reposition();
    }
}
```

The final `Reposition()` is mandatory. It preserves the popup-sizing invariant from `20260901-002` and ensures additional pages/search results still synchronize current geometry.

### Step 3: Audit every `RefreshResults()` caller and record its intended policy

- [ ] Verify these existing callers remain reset semantics by using the default or an explicit `ResetNavigation`:
  - initial `Open()`;
  - `OnSearchTextChanged()`;
  - local `OnItemsChanged()`;
  - selection refresh when `CloseOnSelect == false` unless a regression test demonstrates a different contract;
  - DataProvider/ValueComparer restart paths;
  - remote page-1 loading/new query paths.

- [ ] Do **not** change page 2+ completion here yet; Task 5 owns the search-layer decision.

### Step 4: Run popup/content/sizing regressions

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectProviderIntegrationTests"
```

Expected: existing popup sizing tests remain green, proving the new navigation semantics did not remove the `RefreshResults() -> Reposition()` behavior.

### Step 5: Commit controller/content plumbing

- [ ] Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs
git commit -m "refactor: make BootstrapSelect result refresh semantics explicit"
```

---

## Task 5: Preserve navigation specifically for async page 2+ completion

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
- Optional narrow internal seam: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
- Optional narrow internal seam: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`

### Step 1: Add a deterministic controlled-provider reproduction

- [ ] Reuse `RunOnIsolatedWinFormsThread`, `BootstrapSelectControlledProvider`, `WindowsFormsSynchronizationContext`, and `PumpUntil` already present in `BootstrapSelectProviderIntegrationTests`.
- [ ] Configure `SearchDebounce = TimeSpan.Zero`, `PageSize = 20`, and open the async popup.
- [ ] Complete page 1 with 20 deterministic items and `hasMore: true`.
- [ ] Move highlight far enough down that `CheckNearEnd()` requests page 2. Prefer a direct internal navigation seam over `SendKeys` so CI does not depend on desktop keyboard injection.

If a seam is required, add only:

```csharp
// BootstrapSelectDropDownContent
internal bool MoveHighlight(int delta)
{
    return _resultsView.MoveHighlight(delta);
}
```

and the existing test-style forwarding helper:

```csharp
// BootstrapSelect.Popup.cs
internal bool MoveHighlightedResultForTest(int delta)
{
    return _dropDownController?.Content?.MoveHighlight(delta) == true;
}
```

This matches the existing internal `SetSearchTextForTest` / `ActivateHighlightedResultForTest` pattern and does not expand public API.

### Step 2: Freeze the failure before page 2 completes

- [ ] After moving near the end, capture:

```csharp
var highlightedBeforePageTwo = select.HighlightedResultTextForTest;
```

- [ ] Pump until the controlled provider receives page 2, but do not complete it yet.
- [ ] Assert highlight still equals the captured item while page 2 is pending.

### Step 3: Complete page 2 and assert navigation continuity

- [ ] Complete page 2 with 20 more items and `hasMore: true` or `false`.
- [ ] Pump until the visible result count grows to 40.
- [ ] Assert:

```csharp
Assert.Multiple((Action)(() =>
{
    Assert.That(select.IsDropDownOpenForTest, Is.True);
    Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(40));
    Assert.That(select.HighlightedResultTextForTest, Is.EqualTo(highlightedBeforePageTwo));
}));
```

Suggested test name:

```text
LaterPageCompletionPreservesKeyboardHighlight
```

This test must fail against the pre-fix search routing because `PublishRemoteCompletion()` calls reset-style `RefreshResults()` for every page.

### Step 4: Add a second test for repeated paging or refreshed identity

- [ ] Add one of these deterministic guards in the same integration file, preferring the one that best matches existing provider helpers:
  - page 3 completion also preserves the current highlight; or
  - page 2 refreshes an existing value with a new item instance and the highlight remains on the logically equivalent item.

The purpose is to prove the fix is not a one-time page-2 special case.

### Step 5: Keep page 1 reset behavior explicitly covered

- [ ] Extend or reuse the current `FirstPageCompletionReflowsOpenPopupFromLoadingHeight` and `SearchCompletionReflowsOpenPopupForTwentyRaceMatches` tests.
- [ ] Verify a new search such as `race` still resets to the first selectable result when page 1 completes. This prevents preservation state from leaking across queries.

### Step 6: Run the focused provider integration test and confirm failure

- [ ] Run before changing `BootstrapSelect.Search.cs`:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests.LaterPageCompletionPreservesKeyboardHighlight"
```

Expected before search-layer fix: result count becomes 40 but highlight returns to the first selectable row.

### Step 7: Route later-page completion to preserve mode

- [ ] Change `PublishRemoteCompletion(...)` to choose semantics from the actual page number:

```csharp
private void PublishRemoteCompletion(
    BootstrapSelectSearchController controller,
    string searchText,
    int page)
{
    var updateMode = page > 1
        ? BootstrapSelectResultsUpdateMode.PreserveNavigation
        : BootstrapSelectResultsUpdateMode.ResetNavigation;

    _dropDownController?.RefreshResults(updateMode);

    if (controller.LastError is Exception error && controller.FailedPage == page)
    {
        SearchFailed?.Invoke(
            this,
            new BootstrapSelectSearchFailedEventArgs(searchText, page, error));
    }
    else
    {
        SearchCompleted?.Invoke(
            this,
            new BootstrapSelectSearchCompletedEventArgs(
                searchText,
                page,
                controller.LoadedItems.Count,
                controller.HasMore));
    }
}
```

Do not infer preserve mode from row count. The semantic discriminator already exists: page 1 is query replacement; page 2+ is incremental paging.

### Step 8: Confirm later-page retry/failure semantics

- [ ] Because `LoadRemoteAdditionalPageAsync(retry: true)` passes the failed page number into `PublishRemoteCompletion`, page 2+ retry completion automatically uses preserve mode.
- [ ] Add/adjust a test if necessary so a later-page failure result and subsequent retry do not send the viewport back to the first item.
- [ ] Keep page-1 failure/retry on reset semantics.

### Step 9: Run provider/paging/concurrency tests on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectFirstPageRetryTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectFirstPageRetryTests"
```

Expected: all pass; stale-generation rejection, provider replacement, custom comparer restart, selected snapshot refresh, retry behavior, and popup resizing remain intact.

### Step 10: Commit the async routing fix

- [ ] Commit only the Task 5 implementation/test files:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs
```

If the narrow internal navigation seam was required, also add:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs
```

Then commit:

```powershell
git commit -m "fix: preserve BootstrapSelect highlight across paging"
```

---

## Task 6: Shared-overlay and BootstrapSelect regression sweep

**Files:** No product change expected. Fix only failures that are causally attributable to Tasks 1–5.

### Step 1: Run all BootstrapSelect tests on `net8.0-windows`

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

Pay specific attention to:

- popup lazy reuse/lifecycle;
- local filtering and selection;
- popup sizing first-open/reopen/search completion;
- async paging/retry/concurrency;
- `ValueComparer` replacement;
- accessibility and keyboard behavior;
- visual rendering tests.

### Step 2: Run shared overlay/popover tests

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapPopover|FullyQualifiedName~BootstrapTooltip"
```

Required invariants:

- Alt alone still does not dismiss interactive Popover.
- Escape still closes according to existing policy.
- Tab/Shift+Tab traversal remains correct.
- Outside click behavior remains correct.
- Overlay movement/bounds/rounded clipping tests remain green.
- Form deactivation now closes anchored overlay lifecycle through the tracker.

### Step 3: Repeat focused suites on `net48`

- [ ] Run equivalent BootstrapSelect and shared-overlay filters with `-f net48`.

### Step 4: Run the complete test project on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

Do not treat focused tests as sufficient completion evidence.

---

## Task 7: Manual runtime acceptance and testing documentation

**Files:**

- Modify: `docs/TESTING.md`
- Manual-only acceptance surface: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`

### Step 1: Add the regression matrix to `docs/TESTING.md`

- [ ] Document the following checks:

```text
BootstrapSelect popup lifecycle
- Open searchable Select; press/release Alt only -> popup remains open.
- Open searchable Select; Alt+Tab to another app -> popup disappears immediately.
- Alt+Tab back -> no stale popup remains.
- Escape -> closes with existing focus-restore behavior.
- Outside click -> existing AutoClose behavior remains unchanged.

BootstrapSelect async paging navigation
- Open Async Single; wait for page 1.
- Keep focus in search textbox.
- Navigate with Down until near-end triggers page 2.
- When page 2 completes, highlighted logical item and viewport do not jump to top.
- Repeat with PageDown.
- Continue far enough to trigger page 3; behavior remains stable.
- Start a new search (for example `race`) -> new-query page 1 intentionally resets navigation to first selectable result.
```

### Step 2: Verify the integrated demo manually

- [ ] Run the demo on `net8.0-windows`.
- [ ] Verify local Single/Multiple and async Single/Multiple scenarios.
- [ ] Test both Light/Dark themes if the integrated shell exposes theme switching.
- [ ] Test at least 100% and one non-100% Windows scale if available; the fix must not depend on row pixel constants.
- [ ] For Alt+Tab, switch to at least one unrelated application window and confirm the Select popup never remains floating above it.

### Step 3: Do not add demo-only behavior workarounds

- [ ] No key handlers, deactivation handlers, page delays, or manual popup hiding belong in `BootstrapSelectDemoForm`. The demo should validate library behavior unchanged.

### Step 4: Commit testing documentation

- [ ] Commit:

```powershell
git add docs/TESTING.md
git commit -m "docs: add BootstrapSelect popup regression matrix"
```

---

## Task 8: Final verification and completion gate

### Step 1: Build the solution in Release

- [ ] Run:

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

Expected: zero errors on all projects/TFMs.

### Step 2: Run the complete test project on both supported TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-restore
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-restore
```

Capture the actual pass/fail counts in the implementation summary; do not state “all tests pass” without command output from the current HEAD.

### Step 3: Review the final diff against the architectural boundaries

- [ ] Confirm:
  - no public API change;
  - no new package dependency;
  - no global hooks/polling;
  - Alt-alone cancellation remains narrow;
  - `Form.Deactivate` subscription is symmetric and disposed/rebuilt correctly;
  - tracker close path remains `Close(false)` for BootstrapSelect;
  - page 1/new query uses reset semantics;
  - page 2+ completion/retry uses preserve semantics;
  - item identity restoration uses `ValueComparer`;
  - popup sizing still repositions on refresh;
  - stale async generations/provider replacement remain protected;
  - docs describe the real behavior implemented.

### Step 4: Inspect repository cleanliness

- [ ] Run:

```powershell
git status --short
git diff --check
```

Expected: no unintended artifacts, whitespace errors, screenshots, build outputs, or temporary files.

### Step 5: Final implementation commit only if any uncommitted corrective changes remain

- [ ] If the verification pass required legitimate final corrections, commit them with a focused message. Otherwise do not create an empty “final” commit.

---

## Acceptance criteria

Implementation is complete only when all of the following are true:

- [ ] Pressing Alt alone while an interactive/searchable overlay is open does not dismiss it.
- [ ] Alt+Tab/application switching causes the owning form to deactivate and the BootstrapSelect popup to close.
- [ ] Deactivation closure does not explicitly restore focus to the deactivated BootstrapSelect.
- [ ] Escape, outside click, Tab traversal, target hide/dispose, and overlay reposition behavior do not regress.
- [ ] Async keyboard navigation with Down does not jump back to the first row when page 2+ completes.
- [ ] Async keyboard navigation with PageDown does not jump back to the first row when page 2+ completes.
- [ ] The same preservation behavior remains correct on a later page, not only the first append.
- [ ] The highlighted item is restored by logical value using `ValueComparer`, even if the provider supplies a replacement item instance.
- [ ] Scroll position remains stable/clamped across append updates and does not reset to zero without a semantic new-query reset.
- [ ] A new query/page 1 intentionally resets navigation to the first selected/selectable row and top viewport.
- [ ] Popup geometry continues to update after result refreshes, preserving the `20260901-002` sizing fix.
- [ ] Provider cancellation/generation guards, deduplication, retries, selected snapshot refresh, and comparer replacement continue to pass.
- [ ] `net48` and `net8.0-windows` builds/tests pass from the same implementation code.
- [ ] `docs/TESTING.md` contains the manual Alt/Alt+Tab and async paging navigation matrix.

---

## Implementation notes for reviewers

The two reported bugs are intentionally fixed at different layers even though they share the BootstrapSelect popup surface:

1. **Application deactivation is a shared overlay lifecycle concern.** Fix it in `BootstrapOverlayAnchorTracker`; do not teach BootstrapSelect to parse Alt+Tab.
2. **Paging highlight reset is a BootstrapSelect result-state concern.** Fix it with explicit update semantics; do not change keyboard key handling or paging thresholds.

That separation is part of the acceptance contract. A patch that merely hides the symptoms—for example removing Alt cancellation, closing on every Alt key, disabling near-end paging, or preserving only the numeric row index—does not satisfy this plan.