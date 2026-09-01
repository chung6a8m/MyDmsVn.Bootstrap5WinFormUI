# BootstrapSelect Mouse-Open Popup Lifecycle Regression Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task, `superpowers:systematic-debugging` if observed Win32 activation messages differ from the failure model below, and `superpowers:verification-before-completion` before claiming the fix is complete. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `BootstrapSelect` regression where a left-click on the Select briefly shows the popup and then immediately closes it, while preserving the existing Alt-only, Alt+Tab/application-deactivation, same-application window-switch, Escape, Tab-navigation, outside-click, async paging, sizing, and custom-rendering behavior.

**Architecture:** Keep the existing `BootstrapSelect -> BootstrapSelectDropDownController -> BootstrapOverlayDropDown` architecture and keep mouse opening on the existing `MouseDown` path. Refine `BootstrapSelectDropDownController.OnWindowDeactivated(IntPtr)` so `WM_ACTIVATE/WA_INACTIVE` is treated as an activation-domain signal rather than “every non-owner-form HWND means close”: transitions back to the owner form, to an owner child/control, to the popup itself, or to popup-hosted content remain open; a non-zero activation target belonging to another top-level window closes immediately; an ambiguous `IntPtr.Zero` target is deferred by one WinForms message-loop turn and closes only when neither the owner activation domain nor popup content still owns focus/activation. `WM_ACTIVATEAPP` remains the authoritative immediate close signal for switching to another application, so the previous Alt+Tab fix is preserved.

**Tech Stack:** C# 12, Windows Forms, `ToolStripDropDown`, Win32 `WM_ACTIVATE` / `WM_ACTIVATEAPP`, existing `BootstrapOverlayDropDown` and `BootstrapOverlayAnchorTracker`, `net48;net8.0-windows`, NUnit 4, STA/non-parallel WinForms tests.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Related plans and evidence:**

- `docs/plans/20260829-005-bootstrap-select.md`
- `docs/plans/20260901-002-bootstrap-select-popup-sizing-fix.md`
- `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md`
- `docs/plans/20260901-004-bootstrap-select-custom-result-rendering.md`
- Regression-introducing area: commit `44f1b16` (`fix: close overlays on same-app window activation`), which added `BootstrapOverlayDropDown.WindowDeactivated` handling and `BootstrapSelectDropDownController.OnWindowDeactivated`.

---

## Reported regression to lock down

### User-visible reproduction

1. Run the demo and navigate to a `BootstrapSelect`.
2. Left-click the selection surface or arrow.
3. The popup becomes visible for a fraction of a second.
4. The popup immediately closes without the user selecting an item or clicking outside.

The symptom is specifically the mouse-open path. Programmatic tests that call `OpenDropDownInternal()` directly can remain green because they bypass the real owner `MouseDown -> ShowAt -> FocusSearch -> native activation transition` sequence.

### Current failure chain

```text
BootstrapSelect left MouseDown
        ↓
OnPopupSurfaceMouseDown
        ↓
OpenDropDownInternal
        ↓
BootstrapSelectDropDownController.Open
        ↓
BootstrapOverlayDropDown.ShowAt
        ↓
_content.FocusSearch()
        ↓
popup/native activation changes during the same interaction
        ↓
BootstrapOverlayDropDown receives WM_ACTIVATE / WA_INACTIVE
        ↓
WindowDeactivated(activatedWindow)
        ↓
BootstrapSelectDropDownController.OnWindowDeactivated
        ↓
only ownerForm.Handle is treated as safe
        ↓
activatedWindow is transient/ambiguous/popup-related rather than exactly ownerForm.Handle
        ↓
Close(false)
        ↓
popup flashes and disappears
```

The existing mouse-open event itself is not the regression: `BootstrapSelect` has opened from `MouseDown` since the searchable popup was introduced. Do **not** change to `Click`, `MouseUp`, a timer, or delayed-open workaround unless a failing regression test proves the activation fix below is insufficient.

---

## Required behavior contract

| Scenario | Expected behavior |
| --- | --- |
| Left-click Content / Arrow / Chip on an enabled visible Select | Popup opens once and remains visible. |
| The popup focuses its native search editor immediately after opening | Popup remains visible. |
| `WM_ACTIVATE/WA_INACTIVE` names the owning `Form` | Popup remains visible. |
| `WM_ACTIVATE/WA_INACTIVE` names a child/control whose `FindForm()` is the owning `Form` | Popup remains visible. |
| `WM_ACTIVATE/WA_INACTIVE` names the popup window or a popup-hosted control | Popup remains visible. |
| `WM_ACTIVATE/WA_INACTIVE` has `lParam == IntPtr.Zero` while popup content still contains focus | Do not close synchronously; deferred validation keeps the popup open. |
| `WM_ACTIVATE/WA_INACTIVE` has `lParam == IntPtr.Zero` while the owner form is still active/focused and popup has not yet completed focus transfer | Deferred validation keeps the popup open. |
| `WM_ACTIVATE/WA_INACTIVE` names another Form/window in the same application | Popup closes through `Close(false)`. |
| Ambiguous zero-target deactivation settles on another same-app Form | Deferred validation closes through `Close(false)`. |
| `WM_ACTIVATEAPP` indicates application deactivation / Alt+Tab to another app | Popup closes promptly through the existing `ApplicationDeactivated` path. |
| Press/release Alt while the application stays active | Popup remains open. |
| Escape | Existing close + focus-restore behavior remains unchanged. |
| Tab / Shift+Tab from popup search | Existing owner-relative traversal remains unchanged. |
| Native outside click while AutoClose is enabled | Existing ToolStripDropDown AutoClose behavior remains unchanged. |
| Owner hidden, disabled, disposed, form closed, or anchor invalidated | Existing close behavior remains unchanged. |
| Owner move/resize/scroll/DPI change | Existing reposition/presentation behavior remains unchanged. |
| Programmatic `OpenDropDownInternal()` | Existing lazy creation/reuse/event behavior remains unchanged. |

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, the Select design spec, and the related plans above before changing product code.
- [ ] Preserve the existing public/protected API. This is a corrective change and must add **no public property, event, enum, method, or protected override**.
- [ ] Preserve the shared implementation for `net48;net8.0-windows`.
- [ ] Keep `BootstrapOverlayDropDown` as the popup host. Do not replace it with a top-most `Form`, custom native window, global hook, polling timer, or second overlay system.
- [ ] Keep the current `MouseDown` opening contract unless focused tests prove it is independently incorrect. Do not move opening to `Click`/`MouseUp` merely to avoid the activation race.
- [ ] Keep `AutoClose = true`; do not disable native outside-click closing as a workaround.
- [ ] Keep the existing Alt-key cancellation in `BootstrapOverlayDropDown.OnClosing`; Alt alone must remain non-destructive.
- [ ] Keep `WM_ACTIVATEAPP` handling. Alt+Tab/application deactivation must still close the popup even when focus is inside the popup search editor.
- [ ] Keep same-application window switching closed. The fix must distinguish popup/owner activation from a genuinely different Form/window; it must not simply ignore `WindowDeactivated`.
- [ ] Treat `activatedWindow == IntPtr.Zero` as ambiguous, not as proof of external activation. Resolve it on the next message-loop turn using current owner/popup focus/activation state.
- [ ] Never restore focus to the Select for deactivation-driven closes. These paths use `Close(false)` so the newly activated window/application retains activation.
- [ ] Do not add `Thread.Sleep`, arbitrary timers, or fixed millisecond delays to product code or automated tests.
- [ ] Keep popup sizing/reposition semantics from `20260901-002` and reset/preserve navigation semantics from `20260901-003` unchanged.
- [ ] Keep result-row/DPI/custom-rendering behavior from `20260901-004` unchanged.
- [ ] Activation/focus tests must run STA and non-parallel and must pump the WinForms message queue deterministically with `Application.DoEvents()` where required.
- [ ] Use TDD for each behavior correction: failing focused test -> observe expected failure -> minimal implementation -> focused pass -> broader regression pass.

---

## File structure and responsibilities

### Product file expected to change

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Refine `OnWindowDeactivated(IntPtr activatedWindow)`.
  - Add narrow private helpers to classify owner-domain and popup-domain HWNDs.
  - Add a one-message-loop deferred check for `IntPtr.Zero` activation targets.
  - Cancel/ignore deferred work after close/disposal.

### Product files to inspect and preserve by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Keep `OnPopupSurfaceMouseDown` opening behavior unchanged.
  - Add only an internal test seam if a deterministic mouse-open test cannot be expressed through the existing test subclass.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
  - Keep `WM_ACTIVATE`, `WM_ACTIVATEAPP`, `AutoClose`, Alt cancellation, sizing correction, and events unchanged unless a focused host-level test demonstrates a host defect independent of Select policy.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`
  - Keep form-deactivation close/reposition behavior unchanged.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
  - No product change expected. Re-run Popover activation regressions because it consumes the same overlay host events.

### Tests expected to change

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
  - Add deterministic `WM_ACTIVATE` classification/deferred-zero tests.
  - Preserve existing `WM_ACTIVATEAPP`, same-app deactivation, lazy creation/reuse, sizing, DPI, and result lifecycle coverage.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
  - Add a mouse-open regression test that enters through the Select `MouseDown` path and verifies the popup remains open after the transient activation sequence is pumped.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
  - Re-run existing activation tests; add a regression only if the shared host behavior changes unexpectedly.

### Documentation expected to change

- `docs/TESTING.md`
  - Add the mouse-open flash regression to the BootstrapSelect manual acceptance matrix and keep Alt-only / Alt+Tab / same-app-window-switch cases adjacent so future lifecycle changes are reviewed together.

No public API baseline documentation update is expected.

---

## Task 1: Freeze the mouse-open and native activation regressions

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`

**Interfaces:**

- Consumes existing internal test accessors: `IsDropDownOpenForTest`, `DropDownHandleForTest`, `DropDownContentForTest`.
- No product interface changes in this task.

- [ ] **Step 1: Add deterministic Win32 constants/helper to `BootstrapSelectPopupTests`**

Use the existing `SendMessage` P/Invoke and add only the constants required for `WM_ACTIVATE` testing:

```csharp
private const int WmActivate = 0x0006;
private const int WaInactive = 0;
```

Send an inactive message to the popup with:

```csharp
SendMessage(
    select.DropDownHandleForTest,
    WmActivate,
    (IntPtr)WaInactive,
    activatedWindow);
Application.DoEvents();
```

- [ ] **Step 2: Add a failing ambiguous-zero regression test**

Create `PopupDeactivateWithNoReplacementWhilePopupKeepsFocusDoesNotClose()`:

```csharp
using var form = new Form { ShowInTaskbar = false };
using var select = new BootstrapSelect { SearchEnabled = true };
select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
form.Controls.Add(select);
form.Show();
form.Activate();
select.Focus();
Application.DoEvents();

select.OpenDropDownInternal();
Application.DoEvents();
Assert.That(select.IsDropDownOpenForTest, Is.True);

SendMessage(
    select.DropDownHandleForTest,
    WmActivate,
    (IntPtr)WaInactive,
    IntPtr.Zero);
Application.DoEvents();

Assert.That(
    select.IsDropDownOpenForTest,
    Is.True,
    "An ambiguous WA_INACTIVE transition must not close a popup that still owns its interaction focus.");
```

Expected before the fix: FAIL because current `OnWindowDeactivated(IntPtr.Zero)` immediately calls `Close(false)`.

- [ ] **Step 3: Add an owner-domain regression test**

Create `PopupDeactivateToOwnerControlKeepsPopupOpen()` and send `select.Handle` as the activation target. The expected state is open. This freezes the rule that owner child HWNDs are in the same activation domain even when they are not byte-for-byte equal to `ownerForm.Handle`.

- [ ] **Step 4: Keep a same-app external-window close test**

Use two shown Forms. Open the Select popup on the first Form, then send `WM_ACTIVATE/WA_INACTIVE` with the second Form handle as `lParam`:

```csharp
SendMessage(
    select.DropDownHandleForTest,
    WmActivate,
    (IntPtr)WaInactive,
    secondForm.Handle);
Application.DoEvents();

Assert.That(select.IsDropDownOpenForTest, Is.False);
```

This test must already pass or continue to pass after the fix. It prevents the implementation from solving the flash by ignoring `WindowDeactivated` wholesale.

- [ ] **Step 5: Add a mouse-entry-path test in `BootstrapSelectInteractionTests`**

Extend the existing `TestBootstrapSelect` with a test-only helper:

```csharp
internal void RaiseLeftMouseDownForTest(Point location)
{
    OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0));
}
```

Test sequence:

```csharp
Assert.That(select.Focus(), Is.True);
select.RaiseLeftMouseDownForTest(new Point(select.Width / 2, select.Height / 2));
Application.DoEvents();
Assert.That(select.IsDropDownOpenForTest, Is.True);

SendMessage(
    select.DropDownHandleForTest,
    WmActivate,
    (IntPtr)WaInactive,
    IntPtr.Zero);
Application.DoEvents();

Assert.That(select.IsDropDownOpenForTest, Is.True);
```

This intentionally enters through `OnPopupSurfaceMouseDown` rather than calling `OpenDropDownInternal()` directly.

- [ ] **Step 6: Run focused tests and observe the intended failure**

Run both target-framework test projects using the repository’s documented commands from `docs/TESTING.md`, filtering to:

```text
BootstrapSelectPopupTests
BootstrapSelectInteractionTests
```

Expected before product changes:

- ambiguous-zero test: FAIL because popup closes;
- owner-control test: FAIL if current exact-form-handle comparison rejects the control HWND;
- same-app-other-form test: PASS;
- existing `WM_ACTIVATEAPP` test: PASS.

- [ ] **Step 7: Commit the failing regression tests**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs
git commit -m "test: reproduce BootstrapSelect mouse popup activation regression"
```

---

## Task 2: Classify owner/popup activation correctly and defer ambiguous zero-target deactivation

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`

**Interfaces:**

- Existing event handler remains `private void OnWindowDeactivated(IntPtr activatedWindow)`.
- Add private helpers only; no public/internal API is required for production behavior.

- [ ] **Step 1: Add deferred-check state**

Add one field beside the existing lifecycle flags:

```csharp
private bool _windowDeactivationCheckQueued;
```

It represents at most one queued validation for an ambiguous zero-target `WM_ACTIVATE` transition.

- [ ] **Step 2: Add owner activation-domain classification**

Add:

```csharp
private bool IsOwnerActivationWindow(IntPtr window)
{
    if (window == IntPtr.Zero)
    {
        return false;
    }

    var ownerForm = _owner.FindForm();
    if (ownerForm?.IsHandleCreated != true)
    {
        return false;
    }

    if (window == ownerForm.Handle)
    {
        return true;
    }

    var control = Control.FromChildHandle(window);
    return control?.FindForm() == ownerForm;
}
```

This preserves the exact Form-handle case and also treats a WinForms control/child belonging to that Form as the same owner activation domain.

- [ ] **Step 3: Add popup activation-domain classification**

Add:

```csharp
private bool IsPopupActivationWindow(IntPtr window)
{
    if (window == IntPtr.Zero || _dropDown?.IsHandleCreated != true)
    {
        return false;
    }

    if (window == _dropDown.Handle)
    {
        return true;
    }

    var control = Control.FromChildHandle(window);
    if (control is null)
    {
        return false;
    }

    return ReferenceEquals(control, _dropDown)
        || (_content is not null
            && (ReferenceEquals(control, _content) || _content.Contains(control)));
}
```

Do not broaden this to arbitrary windows in the current process; a second Form in the same process must still close the popup.

- [ ] **Step 4: Replace the exact-handle-only `OnWindowDeactivated` policy**

Use this decision order:

```csharp
private void OnWindowDeactivated(IntPtr activatedWindow)
{
    if (_disposed || !_isOpen)
    {
        return;
    }

    if (IsOwnerActivationWindow(activatedWindow)
        || IsPopupActivationWindow(activatedWindow))
    {
        return;
    }

    if (activatedWindow != IntPtr.Zero)
    {
        Close(false);
        return;
    }

    QueueWindowDeactivationCheck();
}
```

A known other window still closes immediately. Only the ambiguous zero-target case is deferred.

- [ ] **Step 5: Implement one-turn deferred validation**

Add:

```csharp
private void QueueWindowDeactivationCheck()
{
    if (_windowDeactivationCheckQueued
        || _dropDown is null
        || _dropDown.IsDisposed
        || !_dropDown.IsHandleCreated)
    {
        return;
    }

    _windowDeactivationCheckQueued = true;

    try
    {
        _dropDown.BeginInvoke((Action)(() =>
        {
            _windowDeactivationCheckQueued = false;

            if (_disposed || !_isOpen)
            {
                return;
            }

            var popupStillOwnsFocus = _dropDown?.ContainsFocus == true
                || _content?.ContainsFocus == true;

            var ownerForm = _owner.FindForm();
            var ownerStillActive = ownerForm?.IsHandleCreated == true
                && (ownerForm.ContainsFocus || Form.ActiveForm == ownerForm);

            if (popupStillOwnsFocus || ownerStillActive)
            {
                return;
            }

            Close(false);
        }));
    }
    catch (ObjectDisposedException)
    {
        _windowDeactivationCheckQueued = false;
    }
    catch (InvalidOperationException)
    {
        _windowDeactivationCheckQueued = false;
    }
}
```

The check is message-loop deferred, not time-based. It allows `ShowAt()` / `FocusSearch()` / native activation to settle without weakening true same-app or cross-app deactivation behavior.

- [ ] **Step 6: Clear deferred state during terminal lifecycle paths**

Set:

```csharp
_windowDeactivationCheckQueued = false;
```

in `Dispose()` and `CompleteClose()` before releasing popup/tracker state. A delegate already queued before disposal must remain harmless because it first checks `_disposed || !_isOpen`.

- [ ] **Step 7: Run focused tests**

Run:

```text
BootstrapSelectPopupTests
BootstrapSelectInteractionTests
```

Expected after implementation: all new tests PASS, including same-app-other-form closure and existing `ApplicationDeactivateMessageAfterPopupFocusClosesOpenPopup`.

- [ ] **Step 8: Commit the minimal product fix**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs
git commit -m "fix: preserve BootstrapSelect popup during mouse activation"
```

---

## Task 3: Protect shared overlay lifecycle behavior from regressions

**Files:**

- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:** No product interfaces change.

- [ ] **Step 1: Verify Alt-only remains non-destructive**

Run the existing overlay/Popover tests that freeze the `ToolStripDropDownCloseReason.Keyboard + Alt` cancellation. Do not change `BootstrapOverlayDropDown.OnClosing` unless one of those existing tests demonstrates an independent host bug.

- [ ] **Step 2: Verify cross-application deactivation still closes**

Run the existing `WM_ACTIVATEAPP` Select and Popover tests. The new zero-target deferral is only in the Select `WM_ACTIVATE` path and must not delay or cancel `ApplicationDeactivated` closure.

- [ ] **Step 3: Verify same-application Form switching still closes**

Run the new second-Form test and any existing Popover same-app activation test. A concrete non-zero other Form handle must still close synchronously.

- [ ] **Step 4: Verify Escape and Tab focus behavior**

Run the existing Select interaction tests covering:

```text
Escape close / focus restore
Tab from native search -> next owner control
Shift+Tab -> previous owner control
first/last tab-stop no-wrap behavior
nested-container traversal
```

The activation fix must not insert focus restoration into deactivation close paths.

- [ ] **Step 5: Verify popup sizing, DPI, navigation, and custom result rendering**

Run the focused suites affected by the previous three BootstrapSelect plans:

```text
BootstrapSelectPopupTests
BootstrapSelectResultsViewTests
BootstrapSelectProviderIntegrationTests
BootstrapSelectDropDownContentTests
BootstrapSelectInteractionTests
```

Confirm popup creation count remains stable, open-popup reflow still works, Down/PageDown navigation preservation still works, and result-row/custom-rendering geometry remains unchanged.

- [ ] **Step 6: Commit only if regression-test adjustments were required**

If no test source needed changes beyond Task 1, do not create an empty commit. If a shared regression assertion needed strengthening, commit only that test change:

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "test: harden BootstrapSelect overlay lifecycle coverage"
```

---

## Task 4: Document manual acceptance and run release-level verification

**Files:**

- Modify: `docs/TESTING.md`
- Inspect only: `docs/PUBLIC_API_BASELINE.md`
- Inspect only: release/API baseline tests

**Interfaces:** Documentation and verification only; no API delta.

- [ ] **Step 1: Add a BootstrapSelect popup activation regression matrix to `docs/TESTING.md`**

Record these manual checks together:

```text
1. Click Select Content -> popup opens and remains visible.
2. Click Select Arrow -> popup opens and remains visible.
3. With SearchEnabled=true, search textbox receives focus and popup remains visible.
4. Press/release Alt -> popup remains visible.
5. Alt+Tab to another application -> popup closes and does not remain topmost.
6. Activate another Form in the same application -> popup closes.
7. Reopen, press Escape -> popup closes and Select focus is restored according to existing behavior.
8. Reopen, press Tab/Shift+Tab in search -> owner-relative focus traversal still works.
9. Reopen and click outside -> native AutoClose behavior still works.
10. Repeat on Local Single, Local Multiple, Async Single, and custom product-result Select demos.
```

- [ ] **Step 2: Run the complete automated test suite for both target frameworks**

Use the exact repository commands documented in `docs/TESTING.md`. Do not rely only on filtered tests before completion.

Expected: zero failures on both `net48` and `net8.0-windows` test runs supported by the repository.

- [ ] **Step 3: Run build/package/release checks required by the repo**

Run the repository’s normal solution build plus release/public-API verification. Because no public/protected member changes are planned, the approved public API fingerprint must remain unchanged.

- [ ] **Step 4: Perform the demo manual acceptance matrix**

Use the integrated demo on Windows and explicitly verify the original user-visible symptom is gone: a normal left click must not produce even a visible “flash then close” cycle.

Also verify Alt+Tab to a different application still closes the popup promptly.

- [ ] **Step 5: Commit documentation**

```bash
git add docs/TESTING.md
git commit -m "docs: add BootstrapSelect mouse popup regression checks"
```

---

## Final verification checklist

- [ ] A normal left-click opens the BootstrapSelect popup and it stays open.
- [ ] Search focus transfer does not close the popup.
- [ ] Owner-form and owner-control activation targets do not close the popup.
- [ ] Popup/popup-content activation targets do not close the popup.
- [ ] Ambiguous zero-target `WM_ACTIVATE` is resolved after one message-loop turn, not closed synchronously.
- [ ] A different same-application Form still closes the popup.
- [ ] `WM_ACTIVATEAPP` / Alt+Tab to another application still closes the popup.
- [ ] Alt alone still does not dismiss the popup.
- [ ] Escape behavior is unchanged.
- [ ] Tab/Shift+Tab traversal is unchanged.
- [ ] Native outside-click AutoClose is unchanged.
- [ ] Popup sizing, DPI, navigation-preservation, async paging, and custom result rendering regressions remain green.
- [ ] No timer, sleep, global hook, top-most Form, or delayed-open workaround was introduced.
- [ ] No public/protected API changed and the API baseline remains unchanged.
- [ ] Both target frameworks build and all automated tests pass.
- [ ] Integrated demo manual acceptance passes for Local Single, Local Multiple, Async Single, and custom-rendering Selects.
