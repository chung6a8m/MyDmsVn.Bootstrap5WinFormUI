# BootstrapSelect Popup Lifecycle and Paging Navigation Regression Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task and `superpowers:verification-before-completion` before claiming the fix is complete. Use `superpowers:systematic-debugging` if any regression test fails for a reason different from the failure model frozen below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix two `BootstrapSelect` popup regressions: (1) an open searchable popup must close when the owning application/form loses activation, including Alt+Tab to another application, without undoing the existing rule that pressing Alt alone does not dismiss an interactive overlay; and (2) async paging completion must not reset keyboard highlight/scroll back to the first result after the user has navigated with Down/PageDown.

**Architecture:** Keep the existing shared `BootstrapOverlayDropDown` + `BootstrapOverlayAnchorTracker` + `BootstrapSelectDropDownController` architecture. Treat application switching as a **window lifecycle event**, not as an Alt+Tab key gesture: `BootstrapOverlayAnchorTracker` will subscribe to the owning `Form.Deactivate` event and request closure. This is a shared anchored-overlay lifecycle rule, independent of native outside-click policy: a `BootstrapPopover` also closes when its owning form deactivates even when `CloseOnClickOutside == false`; that property controls outside mouse clicks, not application deactivation. Keep the narrow Alt/menu close cancellation in `BootstrapOverlayDropDown` so Alt alone remains non-destructive. For result navigation, introduce explicit internal result-update semantics: **ResetNavigation** for a new logical result set/query and **PreserveNavigation** for incremental/selection-only refreshes. Reset discards previous navigation history, chooses the normal preferred highlight, then scrolls only as needed to reveal that new highlight; preserve restores the prior logical highlighted item by `BootstrapSelectItem.Value` using `ValueComparer` and keeps/clamps the prior viewport. Remote page 1 remains reset semantics; page 2+ completion/retry and selection-only refresh while the popup stays open use preserve semantics.

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

The existing Alt cancellation must **not** simply be removed. It preserves the valid behavior that pressing Alt alone must not dismiss interactive overlays. The missing signal is owning-form deactivation.

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
highlight jumps to reset target
```

The problem is not Down/PageDown key handling. Every refresh currently has replacement/reset semantics, including later-page completion and selection-only refreshes where the logical navigation context should remain stable.

---

## Required behavior contract

### Popup lifecycle matrix

| Scenario | Expected behavior |
| --- | --- |
| Press/release Alt while owning form remains active | Popup remains open; current child focus is preserved. |
| Alt+Tab / owning form deactivates | Anchored popup closes promptly. |
| BootstrapSelect form-deactivation close | Must use the non-focus-restoring path (`Close(false)`); the newly activated application/form retains activation. |
| BootstrapPopover form-deactivation close | Popover closes without Escape-style focus restoration. |
| BootstrapPopover with `CloseOnClickOutside == false`, then owning form deactivates | Popover still closes; `CloseOnClickOutside` governs outside mouse clicks, not application lifecycle. |
| Escape | Existing close/focus-restore behavior remains unchanged. |
| Outside click with native AutoClose enabled | Existing native close behavior remains unchanged. |
| Outside click with Popover `CloseOnClickOutside == false` while owning form remains active | Existing behavior remains unchanged; outside click does not close solely because of the new lifecycle fix. |
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
| Local collection structural change | `ResetNavigation` |
| Successful selection/deselection refresh while `CloseOnSelect == false` | `PreserveNavigation` |
| Async page 2+ completion | `PreserveNavigation` |
| Async page 2+ failure/retry completion | `PreserveNavigation` |

For `ResetNavigation`:

- Discard previous highlight and scroll history.
- Start from `_scrollOffset = 0` and choose `FindFirstSelectable(preferSelected: true)` exactly as the existing implementation does.
- Call `EnsureHighlightedVisible()` after choosing the reset highlight.
- **Do not require the final scroll offset to remain zero.** If the preferred selected item lies below the first viewport, revealing it may legitimately produce a non-zero final offset.
- Therefore, tests must distinguish “previous viewport was discarded” from “final viewport is always top”. With no selected item, first-selectable reset normally remains at zero; with an off-screen preferred selected item, reset may scroll to reveal it.

For `PreserveNavigation`:

- If the previously highlighted row is an item and an equivalent item still exists, restore highlight by `BootstrapSelectItem.Value` using the current `BootstrapSelect.ValueComparer`.
- Do not rely on raw row index as primary identity; grouped results can add headers and provider merge/dedup can replace item instances.
- Preserve the previous scroll offset, clamp it to the new valid range, and make the restored highlight visible only if necessary.
- If the old highlighted item no longer exists, choose the nearest selectable row around the previous index before falling back to the normal first-selectable rule.
- Clear hot/mouse-hover state on result replacement because prior mouse row geometry may no longer be valid.
- Selection-only refresh must preserve navigation because the logical result set/query did not change; only selected-state presentation changed.

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, and all related plans listed above before product-code changes.
- [ ] Preserve the existing public API. This corrective work requires **no new public property, event, enum, or method**.
- [ ] Preserve the shared implementation for `net48;net8.0-windows`.
- [ ] Keep `BootstrapOverlayDropDown` as the popup window implementation. Do not create a second popup type, top-most helper form, global keyboard hook, global mouse hook, CBT hook, polling timer, or Win32 Alt+Tab detector.
- [ ] Do not “fix” regression A by removing the current Alt keyboard-close cancellation wholesale. Alt alone must remain non-destructive.
- [ ] Do not detect Alt+Tab by checking `Keys.Tab` in `ProcessCmdKey`. The close signal is owning-form deactivation.
- [ ] Treat owning-form deactivation as a shared anchored-overlay lifecycle rule. It closes BootstrapSelect and BootstrapPopover even if a Popover has `CloseOnClickOutside == false`; this must be documented and regression-tested so the behavior is intentional rather than an accidental cross-component side effect.
- [ ] A BootstrapSelect deactivation-driven close must follow the existing non-focus-restoring tracker path: `Close(false)`.
- [ ] A BootstrapPopover deactivation-driven close must use `Hide()` without setting `_restoreFocusAfterClose`; Escape remains the only path that sets Escape-style target focus restoration.
- [ ] Keep Escape close/focus restore, active-form outside-click policy, Tab traversal, overlay geometry, DPI handling, rounded clipping, and placement behavior intact.
- [ ] Preserve the popup sizing correction from `20260901-002`: result refresh while open must still recompute/reposition bounds after rows change.
- [ ] Do not suppress `NearEndReached`, delay paging, increase `PageSize`, or change demo provider timing to hide regression B.
- [ ] Do not preserve navigation by raw row index alone. The primary restoration key for item rows is `ValueComparer.Equals(old.Item.Value, new.Item.Value)`.
- [ ] Do not use object reference identity for provider items; later pages may refresh/deduplicate an existing value with a new `BootstrapSelectItem` instance.
- [ ] Selection/deselection refresh with `CloseOnSelect == false` must use preserve semantics; it is not a new query/result set.
- [ ] `ResetNavigation` means previous navigation state is discarded, not that final `ScrollOffset` is unconditionally zero after the preferred reset highlight is revealed.
- [ ] Add automated coverage for both Down-style (`MoveHighlight`) and PageDown-style (`Page`) navigation; do not leave PageDown solely to manual acceptance.
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
  - Keep reset behavior compatible with the existing preferred-selected behavior without asserting a permanently zero final scroll offset.
  - Continue clearing hover state and checking near-end after the new state is coherent.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Forward explicit result-update mode and `ValueComparer` to the results view.
  - Expose narrow internal `MoveHighlight` and `Page` forwarding seams for deterministic integration tests if needed.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Accept an explicit refresh mode, defaulting to reset semantics.
  - Route successful selection/deselection refresh with `CloseOnSelect == false` to preserve semantics.
  - Preserve the `RefreshResults() -> Reposition()` invariant introduced by the popup-sizing fix.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
  - Select reset vs preserve semantics based on remote page number.
  - Page 1: reset; page 2+: preserve.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add at most narrow internal test/navigation forwarding seams (`MoveHighlightedResultForTest`, `PageHighlightedResultForTest`) if provider integration tests need deterministic navigation without `SendKeys`.
  - No public API change.

### Product file to inspect and regression-test, but not change by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
  - Keep the current narrow Alt keyboard-close cancellation unless a focused regression proves it needs refinement.
  - The planned fix for application switching is form lifecycle tracking, not broader ToolStrip keyboard surgery.

### Tests expected to change

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
  - Add deterministic `Form.Deactivate` close coverage and unsubscription/reparent coverage.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
  - Add popup-level owning-form deactivation regression coverage.
  - Add a selection-only refresh regression proving `CloseOnSelect == false` preserves current navigation.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
  - Add reset/preserve navigation unit tests, including logical-value identity replacement and reset-with-off-screen-selected-item behavior.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`
  - Add deterministic later-page completion tests for both MoveHighlight/Down-style and Page/PageDown-style navigation.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
  - Re-run current host-level tests; add a test only if needed to freeze Alt-close semantics at the shared host layer.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
  - Add owning-form deactivation coverage for both `CloseOnClickOutside == true` and `false`.
  - Re-run existing Alt/Tab/Escape/outside-click regressions because `BootstrapOverlayAnchorTracker` is shared infrastructure.

### Documentation/manual acceptance

- `docs/TESTING.md`
  - Record Alt-alone vs Alt+Tab behavior, the shared Popover deactivation rule, selection-only preservation, and async Down/PageDown paging navigation checks.

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

- [ ] Add a small test form subclass so the protected lifecycle event can be raised deterministically without relying on desktop/CI activation timing:

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

## Task 2: Lock BootstrapSelect and BootstrapPopover deactivation contracts without regressing Alt-alone semantics

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
- Product change expected from Task 1; change `BootstrapOverlayDropDown.cs` only if a focused test proves a distinct defect.

### Step 1: Add a BootstrapSelect popup-level deactivation regression

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

The popup controller already constructs the tracker with `() => Close(false)`, so this validates the full lifecycle path without adding BootstrapSelect-specific deactivation handlers.

### Step 2: Freeze the BootstrapSelect no-focus-restoration intent

- [ ] Keep the production callback in `BootstrapSelectDropDownController.Open()` exactly on the non-restoring path:

```csharp
_tracker = new BootstrapOverlayAnchorTracker(
    _owner,
    Reposition,
    () => Close(false));
```

- [ ] If the existing popup test harness can observe active control deterministically, assert deactivation closure does not call `owner.Focus()`. Do not add global activation hooks solely for this assertion; the `Close(false)` code contract plus manual Alt+Tab acceptance is sufficient if CI cannot own desktop activation deterministically.

### Step 3: Freeze the shared BootstrapPopover deactivation contract

- [ ] Add a deterministic Popover test using a form subclass with `RaiseDeactivate()`:

```text
OwningFormDeactivateClosesPopoverWithoutRestoringTargetFocus
```

Open a Popover, put focus in its hosted content, raise form deactivation, and assert the Popover closes.

- [ ] Add the explicit cross-policy test:

```text
OwningFormDeactivateClosesPopoverWhenCloseOnClickOutsideIsFalse
```

Configure:

```csharp
popover.CloseOnClickOutside = false;
```

Then raise form deactivation and assert `popover.IsOpen == false`.

This test freezes the intentional contract: outside-click policy does not keep an anchored popup alive after its owning application/form becomes inactive.

- [ ] Do not set `_restoreFocusAfterClose` for this path. Target-focus restoration remains Escape-specific.

### Step 4: Re-run Alt-alone/outside-click/keyboard regressions

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayDropDownTests|FullyQualifiedName~BootstrapPopoverTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

Expected:

- BootstrapSelect owning-form deactivation closes the popup.
- BootstrapPopover owning-form deactivation closes regardless of `CloseOnClickOutside`.
- Alt alone does not close an overlay while the owning form remains active.
- `CloseOnClickOutside == false` still prevents ordinary outside-click closure while the owning form remains active.
- Escape, Tab traversal, bounds correction, and clipping remain green.

### Step 5: Do not broaden `BootstrapOverlayDropDown` keyboard logic unless evidence requires it

- [ ] Leave this current policy unchanged by default:

```csharp
if (e.CloseReason == ToolStripDropDownCloseReason.Keyboard
    && (ModifierKeys & Keys.Alt) == Keys.Alt)
{
    e.Cancel = true;
    return;
}
```

The new `Form.Deactivate` signal resolves application switching while retaining Alt-alone behavior.

### Step 6: Run both TFMs and commit the integration guards

- [ ] Run the focused popup/popover tests on both TFMs.
- [ ] Commit the new regressions:

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "test: cover anchored overlay owner deactivation"
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
- [ ] Include a grouped-result helper for at least one test so preservation is not accidentally tied to item row index.

Example shape:

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

- [ ] Replace results with page 1 + page 2 using preserve semantics and assert the same logical item remains highlighted and the viewport does not jump to reset state.

```csharp
Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(highlightedValue));
Assert.That(view.ScrollOffset, Is.EqualTo(scrollOffset));
```

If a group/merge change requires a minimal reveal adjustment, assert stability/no top-jump rather than an invalid exact pixel value.

Suggested test name:

```text
PreserveNavigationKeepsHighlightedItemAndScrollWhenRowsAppend
```

### Step 3: Add a failing logical-identity replacement test

- [ ] Start with a highlighted item such as `Value = "ABC"` using an ordinal-ignore-case comparer.
- [ ] Replace the result set with a new `BootstrapSelectItem("abc", "Refreshed")` instance at a different row index because grouping/merge changed.
- [ ] Preserve navigation and assert the refreshed item is highlighted.

Suggested test name:

```text
PreserveNavigationMatchesRefreshedItemByValueComparer
```

This proves the implementation uses `ValueComparer`, not object reference or raw index.

### Step 4: Add reset-semantics control tests without assuming final scroll is always zero

- [ ] Add a no-selected-item reset test. Navigate/scroll away from top, replace with a new result set containing no selected row, and assert old navigation is discarded:

```csharp
Assert.That(view.HighlightedIndex, Is.EqualTo(/* first selectable index */));
Assert.That(view.ScrollOffset, Is.Zero);
```

Suggested name:

```text
ResetNavigationDiscardsPreviousViewportAndUsesFirstSelectable
```

- [ ] Add a second test where the preferred selected item is outside the first viewport. Reset and assert:

```csharp
Assert.That(view.HighlightedRow!.IsSelected, Is.True);
Assert.That(view.HighlightedRow!.Item!.Value, Is.EqualTo(expectedSelectedValue));
Assert.That(view.ScrollOffset, Is.GreaterThan(0));
```

The exact scroll value need not be hard-coded; the test only needs to prove the selected reset target is revealed and that “reset” does not mean “final offset must always be zero”.

Suggested name:

```text
ResetNavigationRevealsPreferredSelectedItemOutsideInitialViewport
```

### Step 5: Run the new tests and confirm current behavior is insufficient

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
```

Expected before implementation: preserve tests cannot pass because `SetResults()` unconditionally resets navigation. The reset control tests freeze the existing preferred-selected behavior while clarifying the final-scroll contract.

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

Keep this internal; it describes implementation semantics, not a user-facing setting.

### Step 7: Make `SetResults` mode-aware

- [ ] Keep an internal reset shorthand only if it reduces call-site churn:

```csharp
internal void SetResults(BootstrapSelectResultSet results)
{
    SetResults(
        results,
        BootstrapSelectResultsUpdateMode.ResetNavigation,
        EqualityComparer<object>.Default);
}
```

- [ ] Add the explicit path:

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

### Step 9: Implement `ResetNavigation` exactly as existing preferred-highlight behavior

- [ ] For reset mode:

```csharp
_scrollOffset = 0;
_highlightedIndex = FindFirstSelectable(preferSelected: true);
EnsureHighlightedVisible();
```

Important: `_scrollOffset = 0` is the reset **starting state**. `EnsureHighlightedVisible()` may produce a non-zero final offset if the preferred selected row is outside the initial viewport. Do not remove that behavior merely to satisfy a “top viewport” assertion.

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

- [ ] For preserve mode:

```csharp
_scrollOffset = Math.Max(0, previousScrollOffset);
ClampScroll();
EnsureHighlightedVisible();
```

The append/selection-refresh case should normally leave the old offset unchanged. `EnsureHighlightedVisible()` may make only the minimum correction needed after grouping/merge changes.

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
- Optional narrow test seam: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Regression-run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`

### Step 1: Forward update mode and comparer through content

- [ ] Add:

```csharp
internal void SetResults(
    BootstrapSelectResultSet results,
    BootstrapSelectResultsUpdateMode updateMode,
    IEqualityComparer<object> valueComparer)
{
    _resultsView.SetResults(results, updateMode, valueComparer);
}
```

- [ ] Keep `SetResults(results)` only if existing tests/call sites need a reset shorthand; its semantics must remain unambiguously reset.

### Step 2: Add narrow deterministic navigation seams once, for Task 4 and Task 5 tests

- [ ] If existing test access cannot move/page the result highlight deterministically, add internal content forwarding:

```csharp
internal bool MoveHighlight(int delta)
{
    return _resultsView.MoveHighlight(delta);
}

internal bool Page(int direction)
{
    return _resultsView.Page(direction);
}
```

- [ ] Add corresponding internal test helpers in `BootstrapSelect.Popup.cs`:

```csharp
internal bool MoveHighlightedResultForTest(int delta)
{
    return _dropDownController?.Content?.MoveHighlight(delta) == true;
}

internal bool PageHighlightedResultForTest(int direction)
{
    return _dropDownController?.Content?.Page(direction) == true;
}
```

Do not expose public API and do not use `SendKeys` for these integration regressions.

### Step 3: Make controller refresh semantics explicit

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

The final `Reposition()` is mandatory to preserve the popup-sizing invariant from `20260901-002`.

### Step 4: Add a failing selection-only preservation regression

- [ ] In `BootstrapSelectPopupTests`, create a Multiple Select (or explicitly `CloseOnSelect = false`) with enough rows to scroll.
- [ ] Open the popup, move highlight away from the first row, capture the highlighted logical value/text and current scroll state if exposed by the existing seam, then activate the highlighted row.
- [ ] Because `CloseOnSelect == false`, the popup remains open and refreshes selected-state presentation.
- [ ] Assert the same logical row remains highlighted and the viewport does not jump back to reset state.

Suggested test name:

```text
SelectionRefreshWithCloseOnSelectFalsePreservesNavigation
```

This test should fail before changing `OnRowActivated` because the current `RefreshResults()` call uses reset semantics.

### Step 5: Route selection-only refresh to preserve semantics

- [ ] Change only the stay-open branch:

```csharp
private void OnRowActivated(BootstrapSelectResultRow row, BootstrapSelectChangeReason reason)
{
    if (_owner.ActivateResultRow(row, reason))
    {
        if (_owner.CloseOnSelect)
        {
            Close(true);
        }
        else
        {
            RefreshResults(BootstrapSelectResultsUpdateMode.PreserveNavigation);
        }
    }
}
```

Selection/deselection changes `IsSelected` presentation but does not create a new logical query/result set, so reset semantics are incorrect here.

### Step 6: Audit every `RefreshResults()` caller and freeze intended policy

- [ ] Reset semantics:
  - initial `Open()`;
  - `OnSearchTextChanged()`;
  - local `OnItemsChanged()` structural changes;
  - DataProvider replacement / ValueComparer restart;
  - remote page-1 loading/new query paths;
  - page-1 completion/failure/retry.

- [ ] Preserve semantics:
  - successful selection/deselection refresh when `CloseOnSelect == false`;
  - remote page 2+ completion/failure/retry (wired in Task 5).

- [ ] Do not change page 2+ completion routing in this task; Task 5 owns the search-layer decision.

### Step 7: Run popup/content/sizing regressions

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectProviderIntegrationTests"
```

Expected: selection-only preservation is green and existing sizing tests prove `RefreshResults() -> Reposition()` remains intact.

### Step 8: Commit controller/content plumbing

- [ ] Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
```

If navigation test seams were required, also add:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs
```

Then commit:

```powershell
git commit -m "fix: preserve BootstrapSelect stay-open navigation"
```

---

## Task 5: Preserve navigation for async page 2+ completion, including PageDown

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
- Optional narrow internal seams already introduced in Task 4: `BootstrapSelect.Popup.cs`, `BootstrapSelectDropDownContent.cs`

### Step 1: Add a deterministic controlled-provider Down-style reproduction

- [ ] Reuse `RunOnIsolatedWinFormsThread`, `BootstrapSelectControlledProvider`, `WindowsFormsSynchronizationContext`, and `PumpUntil` already present in `BootstrapSelectProviderIntegrationTests`.
- [ ] Configure `SearchDebounce = TimeSpan.Zero`, `PageSize = 20`, and open the async popup.
- [ ] Complete page 1 with 20 deterministic items and `hasMore: true`.
- [ ] Use `MoveHighlightedResultForTest(...)` to navigate far enough that `CheckNearEnd()` requests page 2.

### Step 2: Freeze the Down-style failure before and after page 2 completion

- [ ] Capture:

```csharp
var highlightedBeforePageTwo = select.HighlightedResultTextForTest;
```

- [ ] Pump until the provider receives page 2 without completing it; assert highlight remains unchanged while pending.
- [ ] Complete page 2 and pump until visible item count reaches 40.
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
LaterPageCompletionPreservesMoveHighlightNavigation
```

### Step 3: Add an automated PageDown-style reproduction

- [ ] Add a separate integration test that reaches the near-end threshold through `PageHighlightedResultForTest(1)` rather than `MoveHighlightedResultForTest(...)`.
- [ ] Capture the highlight before page 2 completes, complete page 2, and assert the same logical highlight remains afterward.

Suggested test name:

```text
LaterPageCompletionPreservesPageDownNavigation
```

This test is required because PageDown is one of the reported reproduction paths; it must not be left solely to manual acceptance even though `Page()` currently delegates to `MoveHighlight()`.

### Step 4: Add a repeated-paging or refreshed-identity guard

- [ ] Add one deterministic guard, preferring whichever best fits current provider helpers:
  - page 3 completion also preserves the current highlight; or
  - page 2 refreshes an existing logical value with a new item instance and the highlight remains on the equivalent item.

The fix must not be a page-2-only special case.

### Step 5: Keep page 1 reset behavior explicitly covered

- [ ] Extend/reuse `FirstPageCompletionReflowsOpenPopupFromLoadingHeight` and `SearchCompletionReflowsOpenPopupForTwentyRaceMatches`.
- [ ] Verify a new search such as `race` resets to the preferred selected/selectable result for page 1 and does **not** preserve the old query viewport.
- [ ] Do not assert that final scroll is always zero when the preferred selected reset target lies outside the first viewport.

### Step 6: Run focused provider tests and confirm the pre-fix failure

- [ ] Before changing `BootstrapSelect.Search.cs`, run the two new tests:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests.LaterPageCompletionPreservesMoveHighlightNavigation|FullyQualifiedName~BootstrapSelectProviderIntegrationTests.LaterPageCompletionPreservesPageDownNavigation"
```

Expected before search-layer fix: result count grows but highlight returns to the reset target when page 2 completes.

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

Do not infer preserve mode from row count. Page number already carries the semantic distinction: page 1 replaces the query result set; page 2+ incrementally extends/updates it.

### Step 8: Confirm later-page failure/retry semantics

- [ ] Because `LoadRemoteAdditionalPageAsync(retry: true)` passes the failed page number to `PublishRemoteCompletion`, page 2+ retry completion automatically uses preserve mode.
- [ ] Add/adjust a test so a later-page failure result and subsequent retry do not send the viewport to the reset target.
- [ ] Keep page-1 failure/retry on reset semantics.

### Step 9: Run provider/paging/concurrency tests on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectFirstPageRetryTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectConcurrencyTests|FullyQualifiedName~BootstrapSelectFirstPageRetryTests"
```

Expected: stale-generation rejection, provider replacement, custom comparer restart, selected snapshot refresh, retry behavior, PageDown navigation, and popup resizing remain intact.

### Step 10: Commit the async routing fix

- [ ] Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs
git commit -m "fix: preserve BootstrapSelect highlight across paging"
```

If Task 4 did not already commit required narrow navigation seams, include those files in this commit.

---

## Task 6: Shared-overlay and BootstrapSelect regression sweep

**Files:** No product change expected. Fix only failures causally attributable to Tasks 1–5.

### Step 1: Run all BootstrapSelect tests on `net8.0-windows`

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

Pay specific attention to:

- popup lazy reuse/lifecycle;
- local filtering and selection;
- stay-open Multiple selection navigation;
- popup sizing first-open/reopen/search completion;
- async Down and PageDown paging/retry/concurrency;
- `ValueComparer` replacement;
- accessibility and keyboard behavior;
- visual rendering tests.

### Step 2: Run shared overlay/popover tests

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlay|FullyQualifiedName~BootstrapPopover|FullyQualifiedName~BootstrapTooltip"
```

Required invariants:

- Alt alone still does not dismiss interactive Popover while the owning form stays active.
- Owning-form deactivation closes anchored Popover even with `CloseOnClickOutside == false`.
- `CloseOnClickOutside == false` still prevents ordinary outside-click closure while the form remains active.
- Escape still closes according to existing policy and retains Escape-specific focus restoration.
- Tab/Shift+Tab traversal remains correct.
- Overlay movement/bounds/rounded clipping tests remain green.
- Form deactivation closes anchored overlay lifecycle through the tracker.

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

- [ ] Document:

```text
Anchored overlay lifecycle
- Open searchable BootstrapSelect; press/release Alt only -> popup remains open.
- Open searchable BootstrapSelect; Alt+Tab to another app -> popup disappears immediately.
- Alt+Tab back -> no stale Select popup remains.
- Open BootstrapPopover with CloseOnClickOutside=false; Alt+Tab -> Popover still closes because owner deactivation is an application lifecycle close, not an outside-click close.
- While the owning form remains active, CloseOnClickOutside=false still prevents ordinary outside-click dismissal.
- Escape -> existing focus-restore behavior remains unchanged.

BootstrapSelect stay-open selection navigation
- Open Multiple Select (or CloseOnSelect=false), navigate away from the first row, select/deselect the highlighted row -> popup stays open and highlight/viewport remain on the same logical row.

BootstrapSelect async paging navigation
- Open Async Single; wait for page 1.
- Keep focus in search textbox.
- Navigate with Down until near-end triggers page 2.
- When page 2 completes, highlighted logical item and viewport do not jump to reset state.
- Repeat through PageDown.
- Continue far enough to trigger page 3; behavior remains stable.
- Start a new search (for example `race`) -> page 1 discards previous-query navigation and chooses the normal preferred selected/selectable reset target. The final viewport may scroll to reveal that target.
```

### Step 2: Verify the integrated demo manually

- [ ] Run the demo on `net8.0-windows`.
- [ ] Verify local Single/Multiple and async Single/Multiple scenarios.
- [ ] Test both Light/Dark themes if available.
- [ ] Test at least 100% and one non-100% Windows scale if available; the fix must not depend on row pixel constants.
- [ ] For Alt+Tab, switch to at least one unrelated application window and confirm the Select popup never remains floating above it.
- [ ] Also exercise an interactive Popover with `CloseOnClickOutside == false` if the demo exposes one, confirming application deactivation still closes it.

### Step 3: Do not add demo-only workarounds

- [ ] No key handlers, deactivation handlers, page delays, or manual popup hiding belong in `BootstrapSelectDemoForm`. The demo validates library behavior unchanged.

### Step 4: Commit testing documentation

- [ ] Commit:

```powershell
git add docs/TESTING.md
git commit -m "docs: add anchored popup regression matrix"
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

Capture actual pass/fail counts in the implementation summary; do not state “all tests pass” without output from current HEAD.

### Step 3: Review the final diff against architectural boundaries

- [ ] Confirm:
  - no public API change;
  - no new package dependency;
  - no global hooks/polling;
  - Alt-alone cancellation remains narrow;
  - `Form.Deactivate` subscription is symmetric and disposed/rebuilt correctly;
  - shared deactivation behavior is intentionally covered for BootstrapPopover including `CloseOnClickOutside == false`;
  - BootstrapSelect tracker close path remains `Close(false)`;
  - Popover deactivation does not set Escape-style focus restoration;
  - page 1/new query uses reset semantics;
  - reset semantics discard old navigation but may scroll to reveal an off-screen preferred selected row;
  - successful stay-open selection refresh uses preserve semantics;
  - page 2+ completion/retry uses preserve semantics;
  - item identity restoration uses `ValueComparer`;
  - both MoveHighlight/Down-style and Page/PageDown-style paging regressions are automated;
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

### Step 5: Final implementation commit only if uncommitted corrective changes remain

- [ ] If verification required legitimate final corrections, commit them with a focused message. Otherwise do not create an empty “final” commit.

---

## Acceptance criteria

Implementation is complete only when all of the following are true:

- [ ] Pressing Alt alone while an interactive/searchable overlay is open does not dismiss it while the owning form remains active.
- [ ] Alt+Tab/application switching causes the owning form to deactivate and the BootstrapSelect popup to close.
- [ ] BootstrapSelect deactivation closure does not explicitly restore focus to the deactivated BootstrapSelect.
- [ ] Owning-form deactivation closes an anchored BootstrapPopover even when `CloseOnClickOutside == false`.
- [ ] Popover deactivation closure does not use Escape-style target focus restoration.
- [ ] `CloseOnClickOutside == false` still prevents ordinary outside-click closure while the owning form remains active.
- [ ] Escape, outside-click policy, Tab traversal, target hide/dispose, and overlay reposition behavior do not regress.
- [ ] Stay-open selection/deselection refresh (`CloseOnSelect == false`) preserves the current logical highlight and viewport rather than resetting navigation.
- [ ] Async keyboard navigation with Down does not jump back to the reset target when page 2+ completes.
- [ ] Async keyboard navigation with PageDown does not jump back to the reset target when page 2+ completes, and this path has automated integration coverage.
- [ ] The same preservation behavior remains correct on a later page, not only the first append.
- [ ] The highlighted item is restored by logical value using `ValueComparer`, even if the provider supplies a replacement item instance.
- [ ] Scroll position remains stable/clamped across preserve updates and does not reset without a semantic reset operation.
- [ ] A new query/page 1 discards previous-query navigation, starts reset processing from the top, chooses the normal preferred selected/selectable row, and may legitimately end with a non-zero scroll offset if required to reveal that preferred row.
- [ ] Popup geometry continues to update after result refreshes, preserving the `20260901-002` sizing fix.
- [ ] Provider cancellation/generation guards, deduplication, retries, selected snapshot refresh, and comparer replacement continue to pass.
- [ ] `net48` and `net8.0-windows` builds/tests pass from the same implementation code.
- [ ] `docs/TESTING.md` contains the shared lifecycle, stay-open selection, Alt/Alt+Tab, and async Down/PageDown navigation matrix.

---

## Implementation notes for reviewers

The reported bugs remain fixed at different architectural layers even though they share the BootstrapSelect popup surface:

1. **Application deactivation is a shared anchored-overlay lifecycle concern.** Fix it in `BootstrapOverlayAnchorTracker`; do not teach BootstrapSelect to parse Alt+Tab. Because this is shared infrastructure, the contract intentionally applies to BootstrapPopover too, including when `CloseOnClickOutside == false`; outside-click policy is not application-lifecycle policy.
2. **Paging and stay-open selection refresh are BootstrapSelect result-state concerns.** Fix them with explicit update semantics; do not change keyboard key handling or paging thresholds.
3. **Reset and preserve are semantic operations, not pixel assertions.** Reset discards previous navigation state and then reveals its newly chosen preferred highlight; preserve attempts to keep the prior logical highlight and viewport. A reset may therefore finish with a non-zero scroll offset when the preferred selected row is off-screen.
4. **Down and PageDown both require automated coverage.** Even though `Page()` currently delegates to `MoveHighlight()`, PageDown is a reported reproduction path and must remain frozen by an integration test.

That separation is part of the acceptance contract. A patch that merely hides symptoms—for example removing Alt cancellation, closing on every Alt key, disabling near-end paging, preserving only numeric row index, resetting stay-open selection refreshes, or forcing reset scroll to zero after selecting an off-screen preferred row—does not satisfy this plan.