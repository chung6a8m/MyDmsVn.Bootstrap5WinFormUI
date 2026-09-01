# BootstrapSelect Mouse-Open Popup Lifecycle Regression Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task, `superpowers:systematic-debugging` when the observed native activation sequence differs from the model below, and `superpowers:verification-before-completion` before claiming the fix is complete. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `BootstrapSelect` regression where a normal left-click briefly shows the popup and then immediately closes it, while preserving Alt-only, Alt+Tab/application-deactivation, same-application window-switch, Escape, Tab-navigation, outside-click, async paging, sizing, DPI, and custom-rendering behavior; apply the same activation-domain correction to `BootstrapPopover`, which currently uses the same exact-owner-handle policy.

**Architecture:** Keep the existing `BootstrapSelect -> BootstrapSelectDropDownController -> BootstrapOverlayDropDown` and `BootstrapPopover -> BootstrapOverlayDropDown` architectures. Keep mouse opening on the existing `MouseDown` path and keep `WM_ACTIVATEAPP` as the authoritative immediate cross-application close signal. Introduce one small internal activation-domain helper that classifies owner-form HWNDs and the complete popup surface/control tree; Select and Popover keep their own lifecycle/close semantics but both defer only ambiguous `WM_ACTIVATE/WA_INACTIVE` transitions whose `lParam == IntPtr.Zero`. Deferred work is guarded by a lifecycle generation token so a callback queued by an old open cycle can never close a later reopened popup.

**Tech Stack:** C# 12, Windows Forms, `ToolStripDropDown`, Win32 `WM_ACTIVATE` / `WM_ACTIVATEAPP`, `SendMessage`, optional interactive `SendInput` diagnostic coverage, existing `BootstrapOverlayDropDown` and `BootstrapOverlayAnchorTracker`, `net48;net8.0-windows`, NUnit 4, STA/non-parallel WinForms tests.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Related plans and evidence:**

- `docs/plans/20260829-005-bootstrap-select.md`
- `docs/plans/20260901-002-bootstrap-select-popup-sizing-fix.md`
- `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md`
- `docs/plans/20260901-004-bootstrap-select-custom-result-rendering.md`
- Regression-introducing area: commit `44f1b16` (`fix: close overlays on same-app window activation`), which added `BootstrapOverlayDropDown.WindowDeactivated` handling to both `BootstrapSelectDropDownController` and `BootstrapPopover`.

---

## Reported regression and corrected failure model

### User-visible reproduction

1. Run the integrated demo and navigate to a `BootstrapSelect`.
2. Left-click the selection surface or arrow.
3. The popup becomes visible for a fraction of a second.
4. The popup immediately closes without the user selecting an item or clicking outside.

Programmatic tests that call `OpenDropDownInternal()` directly are insufficient because they bypass the real owner mouse-entry path and do not prove which activation message/window caused the close.

### Current close chain to verify

```text
BootstrapSelect left mouse input
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
WM_ACTIVATE / WA_INACTIVE during popup/focus transition
        ↓
BootstrapOverlayDropDown.WindowDeactivated(activatedWindow)
        ↓
consumer accepts only ownerForm.Handle
        ↓
transient zero / owner child / popup surface / popup child is classified as external
        ↓
Close(false) / Hide()
        ↓
popup flashes and disappears
```

`BootstrapPopover.OnWindowDeactivated()` currently has the same exact-owner-form-handle rule, so the plan must freeze and correct equivalent owner/popup-domain transitions there as well rather than assuming Popover is unaffected.

The existing Select `MouseDown` event is not itself the regression. Do **not** move opening to `Click`/`MouseUp`, disable `AutoClose`, add a timer, or introduce a delayed-open workaround unless the native-input diagnostic proves an independent mouse-event bug.

---

## Required behavior contract

| Scenario | BootstrapSelect | BootstrapPopover |
| --- | --- | --- |
| Normal left-click/toggle opens overlay | Remains open | Remains open |
| Popup immediately focuses hosted interactive content | Remains open | Remains open |
| `WM_ACTIVATE/WA_INACTIVE` names owning `Form` | Remains open | Remains open |
| `WM_ACTIVATE/WA_INACTIVE` names an owner child/control | Remains open | Remains open |
| `WM_ACTIVATE/WA_INACTIVE` names popup HWND | Remains open | Remains open |
| `WM_ACTIVATE/WA_INACTIVE` names `BootstrapOverlaySurface` or any hosted descendant | Remains open | Remains open |
| `WM_ACTIVATE/WA_INACTIVE`, `lParam == IntPtr.Zero`, popup still owns focus | Deferred validation keeps open | Deferred validation keeps open |
| Zero-target transition settles back on owner form | Deferred validation keeps open | Deferred validation keeps open |
| Zero-target transition settles on a different same-app Form | Deferred validation closes via non-focus-restoring path | Deferred validation hides without Escape-style focus restore |
| Non-zero other same-app Form/window | Close immediately | Hide immediately |
| `WM_ACTIVATEAPP` application deactivation / Alt+Tab | Close immediately | Hide immediately |
| Press/release Alt while application remains active | Remains open | Remains open |
| Escape | Existing close + focus restore | Existing policy + target focus restore |
| Tab / Shift+Tab | Existing owner-relative traversal | Existing content/owner traversal |
| Native outside click | Existing `AutoClose` behavior | Existing `CloseOnClickOutside` behavior |
| Deferred callback from a prior open cycle runs after reopen | Must not close new popup | Must not close new popover |

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, the Select design spec, and all related plans above before product changes.
- [ ] Preserve the existing public/protected API. This corrective work adds no public property, event, enum, method, or protected override.
- [ ] Preserve one shared implementation for `net48;net8.0-windows`.
- [ ] Keep `BootstrapOverlayDropDown` as the popup host. Do not introduce a top-most Form, second overlay type, global mouse/keyboard hook, polling timer, or fixed-delay workaround.
- [ ] Keep Select opening on `MouseDown` unless the explicit native-input diagnostic proves an independent issue.
- [ ] Keep Select `AutoClose = true`.
- [ ] Keep Popover `CloseOnClickOutside` semantics unchanged; application/window lifecycle close is independent from outside-click policy.
- [ ] Keep the existing Alt keyboard-close cancellation in `BootstrapOverlayDropDown.OnClosing`.
- [ ] Keep `WM_ACTIVATEAPP`; cross-application deactivation must remain immediate and must not wait for zero-target deferred validation.
- [ ] A concrete non-zero activation target outside the owner/popup domains still closes immediately.
- [ ] Treat only `activatedWindow == IntPtr.Zero` as ambiguous and defer it by one message-loop turn, not by elapsed time.
- [ ] Any deferred callback must be tied to the exact overlay open generation that queued it. A callback from an earlier generation must be a no-op even when a later generation is currently open.
- [ ] Select deactivation-driven closes use `Close(false)` and never restore focus to the Select.
- [ ] Popover deactivation-driven hides must not set `_restoreFocusAfterClose`; Escape remains the focus-restoring path.
- [ ] Owner/popup-domain classification must include `BootstrapOverlaySurface` and all controls hosted below it, not only the logical content root.
- [ ] Do not add `Thread.Sleep`, arbitrary timers, or fixed millisecond delays to product code or automated tests.
- [ ] Activation/focus tests run STA and non-parallel and pump the WinForms queue deterministically with `Application.DoEvents()` when needed.
- [ ] Keep sizing/reposition behavior from `20260901-002`, navigation-preservation behavior from `20260901-003`, and DPI/custom-result behavior from `20260901-004` unchanged.
- [ ] Use TDD: failing focused test -> observe intended failure -> minimal implementation -> focused pass -> broader regression pass.

---

## File structure and responsibilities

### New internal helper

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapOverlayActivationDomain.cs`
  - Classify whether an HWND belongs to the owning Form/control tree.
  - Classify whether an HWND belongs to the popup root, `BootstrapOverlaySurface`, or any control below that surface.
  - Contain classification only; do not own close policy, timing, or focus restoration.

### Product files to modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Replace exact-owner-handle policy with shared activation-domain classification.
  - Add generation-safe one-turn deferred validation for zero-target `WM_ACTIVATE`.
  - Invalidate queued generations on open/close/dispose.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
  - Apply the same owner/popup-domain classification.
  - Add generation-safe zero-target deferral while preserving Popover-specific `Hide()` and focus semantics.

### Product files to inspect and preserve by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Keep `OnPopupSurfaceMouseDown` unchanged.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
  - Keep `WM_ACTIVATE`, `WM_ACTIVATEAPP`, `AutoClose`, Alt cancellation, size correction, and event publication unchanged unless diagnostic evidence identifies an independent host bug.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`
  - Keep current form-deactivation and reposition behavior; explicitly verify it is not the remaining flash-close source.

### Tests to modify

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
  - Owner/popup/surface/zero-target/same-app/generation regressions.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
  - Mouse-entry-path test plus an explicit interactive native-input diagnostic/acceptance test.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
  - Equivalent owner/popup/surface/zero-target/same-app/generation regressions.

- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`

### Documentation

- Modify: `docs/TESTING.md`
  - Keep Select and Popover activation matrices together, including real mouse input, Alt-only, Alt+Tab, same-app Form switch, and reopen-after-deferred-message cases.

No public API baseline documentation change is expected.

---

## Task 1: Freeze the real mouse-entry sequence and both consumer regressions

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Temporary diagnostic only, do not commit unless independently justified: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`, `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`

**Interfaces:** Existing internal test handles/accessors only. No production API change.

- [ ] **Step 1: Add deterministic `WM_ACTIVATE` constants and helper usage**

Use the existing `SendMessage` P/Invoke with:

```csharp
private const int WmActivate = 0x0006;
private const int WaInactive = 0;
```

The deterministic regression shape is:

```csharp
SendMessage(
    popupHandle,
    WmActivate,
    (IntPtr)WaInactive,
    activatedWindow);
Application.DoEvents();
```

- [ ] **Step 2: Add Select zero-target regression**

Add `PopupDeactivateWithNoReplacementWhilePopupKeepsFocusDoesNotClose()` using a shown active Form, `SearchEnabled = true`, and a focused native search editor. Send `WM_ACTIVATE/WA_INACTIVE` with `lParam = IntPtr.Zero`, pump the queue, and assert the popup remains open.

Expected before fix: FAIL because current Select handler closes every target except the exact owner Form handle.

- [ ] **Step 3: Add Select owner-control and popup-surface regressions**

Add:

```text
PopupDeactivateToOwnerControlKeepsPopupOpen
PopupDeactivateToPopupSurfaceKeepsPopupOpen
```

The owner-control test sends `select.Handle`.

For the popup-surface case, add the narrowest internal test accessor required to obtain the existing `BootstrapOverlaySurface` handle; do not expose a public member. Send both the surface handle and one hosted search/content-control handle in separate assertions or test cases. These tests freeze the full popup-domain contract rather than only `_content` descendants.

- [ ] **Step 4: Keep concrete same-app external-window closure**

Show a second Form and send its handle as `lParam`. Assert immediate close. This prevents a fix that simply ignores `WindowDeactivated`.

- [ ] **Step 5: Add Popover equivalents before changing Popover product code**

Add deterministic tests:

```text
PopupDeactivateWithNoReplacementWhileContentKeepsFocusDoesNotClosePopover
PopupDeactivateToTargetControlKeepsPopoverOpen
PopupDeactivateToPopoverSurfaceOrHostedControlKeepsPopoverOpen
PopupDeactivateToSecondApplicationFormClosesPopover
```

Run them before product changes. The first three are expected to expose the same exact-owner-handle weakness currently present in `BootstrapPopover.OnWindowDeactivated()`.

- [ ] **Step 6: Add a synthetic Select mouse-entry-path regression**

Extend the existing test subclass with:

```csharp
internal void RaiseLeftMouseDownForTest(Point location)
{
    OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0));
}
```

Enter through `OnMouseDown`, verify the popup opens, then apply the deterministic activation cases above. This test validates the Select entry path but is **not** accepted as proof of the real native activation sequence by itself.

- [ ] **Step 7: Capture one real native mouse-open sequence on an interactive Windows desktop**

Before implementing the classifier, reproduce the original demo failure with actual mouse input. Use either the integrated demo manually under the debugger or an `[Explicit]` interactive test that uses `SendInput` to click the screen coordinates of the Select. If temporary instrumentation is needed, add `Debug.WriteLine` only around:

```text
BootstrapOverlayDropDown.WndProc: WM_ACTIVATE / WM_ACTIVATEAPP, wParam, lParam
BootstrapOverlayAnchorTracker.OnFormDeactivate: Form.ContainsFocus
BootstrapSelectDropDownController.OnWindowDeactivated: activatedWindow
```

Do not commit diagnostic logging. Record the observed category in the regression test comments: zero target, owner child, popup/surface child, or another concrete HWND. If the real trace identifies an additional deterministic category, add that test before product changes.

- [ ] **Step 8: Verify `BootstrapOverlayAnchorTracker.Form.Deactivate` is not independently closing a valid popup transition**

During the same native-input reproduction, confirm whether `OnFormDeactivate` fires. If it fires while popup content owns focus, verify its existing `ContainsFocus` guard prevents closure. If the tracker itself closes the overlay in the real failing sequence, stop and use `superpowers:systematic-debugging`; do not proceed with only the `WindowDeactivated` fix until a failing tracker test freezes that independent defect.

- [ ] **Step 9: Run focused tests and observe the intended failures**

Run both target frameworks with filters covering:

```text
BootstrapSelectPopupTests
BootstrapSelectInteractionTests
BootstrapPopoverTests
```

Before product changes, expected results are:

- Select zero-target: FAIL.
- Select owner-control: FAIL with current exact-form-handle policy.
- Select popup/surface target: FAIL when not equal to owner Form handle.
- Select concrete second Form: PASS.
- Existing Select `WM_ACTIVATEAPP`: PASS.
- Equivalent Popover owner/popup/zero-target cases: expected FAIL where the same policy applies.
- Existing Popover second Form and `WM_ACTIVATEAPP`: PASS.

- [ ] **Step 10: Commit only permanent regression tests**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "test: reproduce overlay activation-domain regressions"
```

---

## Task 2: Add shared owner/popup activation-domain classification

**Files:**

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapOverlayActivationDomain.cs`

**Interfaces:** New internal static helper only; no public/protected API delta.

- [ ] **Step 1: Implement owner-domain classification**

```csharp
internal static class BootstrapOverlayActivationDomain
{
    internal static bool IsOwnerWindow(IntPtr window, Form? ownerForm)
    {
        if (window == IntPtr.Zero || ownerForm?.IsHandleCreated != true)
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

- [ ] **Step 2: Implement popup-domain classification from the surface root**

Continue the helper with:

```csharp
    internal static bool IsPopupWindow(
        IntPtr window,
        BootstrapOverlayDropDown? dropDown,
        BootstrapOverlaySurface? surface)
    {
        if (window == IntPtr.Zero
            || dropDown?.IsHandleCreated != true
            || surface is null)
        {
            return false;
        }

        if (window == dropDown.Handle)
        {
            return true;
        }

        var control = Control.FromChildHandle(window);
        return control is not null
            && (ReferenceEquals(control, dropDown)
                || ReferenceEquals(control, surface)
                || surface.Contains(control));
    }
}
```

This explicitly covers `_surface` and every hosted descendant, including Select search controls and Popover caller content. Do not classify arbitrary same-process windows as popup-domain windows.

- [ ] **Step 3: Compile before consumer changes**

Build both target frameworks. Expected: helper compiles without public API baseline changes.

- [ ] **Step 4: Commit helper**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapOverlayActivationDomain.cs
git commit -m "refactor: classify overlay activation domains"
```

---

## Task 3: Fix BootstrapSelect with generation-safe zero-target deferral

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`

**Interfaces:** Existing `private void OnWindowDeactivated(IntPtr activatedWindow)` remains; private lifecycle fields/helpers only.

- [ ] **Step 1: Add lifecycle-generation state**

Use generation state rather than a standalone queued boolean:

```csharp
private int _activationGeneration;
private int _queuedWindowDeactivationGeneration = -1;
```

Each distinct open lifecycle receives a different generation.

- [ ] **Step 2: Advance generation on open and terminal close/dispose**

Immediately before marking a new popup lifecycle open:

```csharp
_activationGeneration++;
_isOpen = true;
```

In `CompleteClose()` and `Dispose()` invalidate pending work:

```csharp
_activationGeneration++;
_queuedWindowDeactivationGeneration = -1;
```

Do not rely only on `_isOpen`; a stale callback can run after an old popup closes and a new popup reopens.

- [ ] **Step 3: Replace exact-handle policy**

```csharp
private void OnWindowDeactivated(IntPtr activatedWindow)
{
    if (_disposed || !_isOpen)
    {
        return;
    }

    var ownerForm = _owner.FindForm();
    if (BootstrapOverlayActivationDomain.IsOwnerWindow(activatedWindow, ownerForm)
        || BootstrapOverlayActivationDomain.IsPopupWindow(activatedWindow, _dropDown, _surface))
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

- [ ] **Step 4: Queue a generation-bound one-turn validation**

```csharp
private void QueueWindowDeactivationCheck()
{
    if (_dropDown is null
        || _dropDown.IsDisposed
        || !_dropDown.IsHandleCreated)
    {
        return;
    }

    var generation = _activationGeneration;
    if (_queuedWindowDeactivationGeneration == generation)
    {
        return;
    }

    _queuedWindowDeactivationGeneration = generation;

    try
    {
        _dropDown.BeginInvoke((Action)(() =>
        {
            if (_queuedWindowDeactivationGeneration == generation)
            {
                _queuedWindowDeactivationGeneration = -1;
            }

            if (_disposed || !_isOpen || generation != _activationGeneration)
            {
                return;
            }

            var popupStillOwnsFocus = _dropDown?.ContainsFocus == true
                || _surface?.ContainsFocus == true
                || _content?.ContainsFocus == true;

            var ownerForm = _owner.FindForm();
            var ownerStillActive = ownerForm?.IsHandleCreated == true
                && (ownerForm.ContainsFocus || Form.ActiveForm == ownerForm);

            if (!popupStillOwnsFocus && !ownerStillActive)
            {
                Close(false);
            }
        }));
    }
    catch (ObjectDisposedException)
    {
        if (_queuedWindowDeactivationGeneration == generation)
        {
            _queuedWindowDeactivationGeneration = -1;
        }
    }
    catch (InvalidOperationException)
    {
        if (_queuedWindowDeactivationGeneration == generation)
        {
            _queuedWindowDeactivationGeneration = -1;
        }
    }
}
```

The captured `generation` is mandatory. Clearing a boolean in `CompleteClose()` is insufficient because the already-queued delegate still exists and could otherwise act on a later reopened popup.

- [ ] **Step 5: Add stale-callback/reopen regression**

Add `QueuedZeroDeactivationFromPreviousOpenDoesNotCloseReopenedPopup()`:

1. Open Select popup.
2. Send zero-target `WM_ACTIVATE` so one deferred check is queued.
3. Close the current popup synchronously before pumping that queued callback.
4. Reopen immediately.
5. Pump the queue.
6. Assert the new popup remains open and creation count is unchanged.

This test must fail against an implementation that guards only with `_isOpen`/a boolean and must pass with the generation token.

- [ ] **Step 6: Run Select-focused tests**

Run:

```text
BootstrapSelectPopupTests
BootstrapSelectInteractionTests
```

Expected: owner Form/control, popup/surface/content, zero-target, stale-callback/reopen, concrete second Form, and `WM_ACTIVATEAPP` all pass.

- [ ] **Step 7: Commit Select fix**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "fix: preserve BootstrapSelect popup activation lifecycle"
```

---

## Task 4: Apply equivalent activation policy to BootstrapPopover

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:** Public Popover API remains unchanged. `Hide()` and Escape focus semantics remain unchanged.

- [ ] **Step 1: Add Popover lifecycle-generation state**

Add:

```csharp
private int _activationGeneration;
private int _queuedWindowDeactivationGeneration = -1;
```

Advance the generation when `Show()` begins a new visible lifecycle and when the dropdown closes/disposes. Invalidation must happen even when the old deferred delegate is still queued.

- [ ] **Step 2: Replace Popover exact-owner-handle policy**

Use the same shared classifier:

```csharp
private void OnWindowDeactivated(IntPtr activatedWindow)
{
    if (_disposed || !IsOpen)
    {
        return;
    }

    var ownerForm = _target?.FindForm();
    if (BootstrapOverlayActivationDomain.IsOwnerWindow(activatedWindow, ownerForm)
        || BootstrapOverlayActivationDomain.IsPopupWindow(activatedWindow, _dropDown, _surface))
    {
        return;
    }

    if (activatedWindow != IntPtr.Zero)
    {
        Hide();
        return;
    }

    QueueWindowDeactivationCheck();
}
```

- [ ] **Step 3: Add generation-safe deferred validation**

Mirror the Select generation pattern, but use Popover state:

```csharp
var popupStillOwnsFocus = _dropDown.ContainsFocus
    || _surface.ContainsFocus
    || _content?.ContainsFocus == true;

var ownerForm = _target?.FindForm();
var ownerStillActive = ownerForm?.IsHandleCreated == true
    && (ownerForm.ContainsFocus || Form.ActiveForm == ownerForm);

if (!popupStillOwnsFocus && !ownerStillActive)
{
    Hide();
}
```

Do not set `_restoreFocusAfterClose` in this path. Only Escape-driven closure retains target-focus restoration.

- [ ] **Step 4: Add Popover stale-callback/reopen regression**

Add `QueuedZeroDeactivationFromPreviousOpenDoesNotCloseReopenedPopover()` with the same close/reopen-before-message-pump sequence as the Select test.

Also keep both `CloseOnClickOutside = true` and `false` lifecycle coverage. Window/application deactivation policy remains independent from outside-click policy.

- [ ] **Step 5: Run Popover-focused tests**

Expected passing matrix:

```text
Alt alone remains open
WM_ACTIVATEAPP closes
owner Form/control remains open
popup/surface/hosted content remains open
zero-target with popup focus remains open
concrete second Form closes
stale callback cannot close reopened Popover
Escape restores target focus exactly as before
CloseOnClickOutside true/false semantics unchanged
```

- [ ] **Step 6: Commit Popover fix**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "fix: preserve Popover popup activation lifecycle"
```

---

## Task 5: Protect shared overlay lifecycle and previous BootstrapSelect fixes

**Files:**

- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
- Inspect/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
- Test/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Test/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Test/run: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:** No API changes.

- [ ] **Step 1: Verify Alt-only and `WM_ACTIVATEAPP` behavior**

Run existing host/consumer tests freezing Alt cancellation and application deactivation. Zero-target deferral must not alter the immediate `ApplicationDeactivated` path.

- [ ] **Step 2: Verify `BootstrapOverlayAnchorTracker` behavior**

Run tracker tests covering Form.Deactivate, target disposal/visibility, reparenting, move/resize/scroll, and disposal unsubscription. Confirm no tracker change was required by the real native-click trace. If a new tracker regression was discovered in Task 1, implement it as a separate focused TDD change before continuing.

- [ ] **Step 3: Verify Escape and Tab focus behavior**

Run Select and Popover tests for:

```text
Escape close/focus restore
Tab and Shift+Tab traversal
first/last no-wrap behavior
nested-container traversal
```

Neither activation consumer may restore focus on a lifecycle-driven close.

- [ ] **Step 4: Verify previous BootstrapSelect correction suites**

Run:

```text
BootstrapSelectPopupTests
BootstrapSelectResultsViewTests
BootstrapSelectProviderIntegrationTests
BootstrapSelectDropDownContentTests
BootstrapSelectInteractionTests
```

Confirm popup creation reuse, sizing/reflow, DPI refresh, Down/PageDown preservation, async paging, and custom-result geometry remain unchanged.

- [ ] **Step 5: Verify no stale-generation state leaks across repeated cycles**

Add or run repeated open -> zero-target queue -> close -> reopen cycles for both Select and Popover. After several cycles, each overlay must remain reusable, lifecycle events must fire once per actual transition, and no old callback may close the current generation.

- [ ] **Step 6: Commit only if additional regression assertions were necessary**

Do not create an empty commit. If test hardening was required:

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "test: harden overlay activation lifecycle coverage"
```

---

## Task 6: Document manual acceptance and run release-level verification

**Files:**

- Modify: `docs/TESTING.md`
- Inspect only: `docs/PUBLIC_API_BASELINE.md`
- Inspect/run: release/API baseline tests

**Interfaces:** Documentation/verification only.

- [ ] **Step 1: Add a combined Select/Popover activation matrix to `docs/TESTING.md`**

Record at least:

```text
BootstrapSelect
1. Real mouse click on Content -> popup opens and remains visible.
2. Real mouse click on Arrow -> popup opens and remains visible.
3. SearchEnabled=true -> native search editor receives focus and popup remains visible.
4. Press/release Alt -> remains open.
5. Alt+Tab to another application -> closes promptly and does not remain topmost.
6. Activate another Form in the same application -> closes.
7. Escape -> existing focus-restore behavior.
8. Tab/Shift+Tab -> existing owner-relative traversal.
9. Outside click -> native AutoClose behavior.
10. Repeat Local Single, Local Multiple, Async Single, custom product-result demos.
11. Queue an ambiguous deactivation, close/reopen, pump messages -> reopened popup remains open.

BootstrapPopover
1. Click target -> popover opens and focused content remains usable.
2. Press/release Alt -> remains open.
3. Alt+Tab -> closes.
4. Activate another same-app Form -> closes.
5. CloseOnClickOutside=false does not disable application/window lifecycle closure.
6. Escape retains target-focus restoration.
7. Close/reopen around an ambiguous deferred transition -> reopened popover remains open.
```

- [ ] **Step 2: Run complete automated tests for both target frameworks**

Use the exact repository commands from `docs/TESTING.md`. Do not stop at filtered tests.

Expected: zero failures for supported `net48` and `net8.0-windows` runs.

- [ ] **Step 3: Run solution build/package/release/API checks**

Because only internal/private implementation is added, the approved public/protected API fingerprint must remain unchanged.

- [ ] **Step 4: Perform integrated demo acceptance with real mouse input**

The original Select symptom is the release gate: a normal click must not produce any visible flash-then-close cycle. Also validate Alt+Tab, second same-app Form activation, Escape, Tab, and outside-click behavior.

Exercise interactive Popover content in the same session to confirm the equivalent classifier does not introduce new dismissal/focus regressions.

- [ ] **Step 5: Commit documentation**

```bash
git add docs/TESTING.md
git commit -m "docs: add overlay activation regression checks"
```

---

## Final verification checklist

- [ ] Real native mouse input opens BootstrapSelect and the popup stays open.
- [ ] The actual native activation sequence was observed before implementation; deterministic tests cover every relevant category seen in that trace.
- [ ] `BootstrapOverlayAnchorTracker.Form.Deactivate` was verified not to be an unfrozen independent source of the original flash regression.
- [ ] Owner Form and owner-control activation targets remain open for Select and Popover.
- [ ] Popup HWND, `BootstrapOverlaySurface`, and hosted descendant HWNDs remain open for Select and Popover.
- [ ] Ambiguous zero-target `WM_ACTIVATE` is resolved after one message-loop turn, not closed synchronously.
- [ ] Deferred callbacks are generation-bound and cannot close a later reopened Select or Popover.
- [ ] Concrete different same-app Forms still close immediately.
- [ ] `WM_ACTIVATEAPP` / Alt+Tab still closes immediately.
- [ ] Alt alone remains non-destructive.
- [ ] Select Escape and Tab/Shift+Tab behavior is unchanged.
- [ ] Popover Escape/Tab behavior and target-focus restoration are unchanged.
- [ ] Select native outside-click AutoClose is unchanged.
- [ ] Popover `CloseOnClickOutside` true/false semantics are unchanged.
- [ ] Sizing, DPI, navigation preservation, async paging, and custom result rendering remain green.
- [ ] No timer, sleep, global hook, top-most Form, delayed-open workaround, or second overlay system was introduced.
- [ ] No public/protected API changed and API baseline remains unchanged.
- [ ] Both target frameworks build and all automated tests pass.
- [ ] Integrated demo manual acceptance passes for BootstrapSelect and interactive BootstrapPopover.
