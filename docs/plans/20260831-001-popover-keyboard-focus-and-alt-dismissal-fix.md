# BootstrapPopover Keyboard Focus and Alt Dismissal Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix `BootstrapPopover` so interactive content supports predictable `Tab`/`Shift+Tab` focus traversal and pressing `Alt` does not dismiss the popover, while preserving Escape dismissal, outside-click dismissal, caller-owned content lifetime, placement behavior, and dual-target compatibility.

**Architecture:** Keep the current `BootstrapPopover -> BootstrapOverlayDropDown -> ToolStripControlHost -> BootstrapOverlaySurface -> caller content` architecture for the first implementation pass, but explicitly take ownership of the keyboard behaviors that conflict with `ToolStripDropDown` menu semantics. Add regression-first coverage that proves the native close reason produced by `Alt`, cancel only the unwanted menu/keyboard-close path, and route dialog-key traversal into the hosted content tree without replacing native outside-click behavior. If the focused regression tests prove that `ToolStripDropDown` cannot distinguish the required cases safely, stop before introducing layered workarounds and move to the architecture fallback gate defined in Task 6.

**Tech Stack:** C#, Windows Forms, `ToolStripDropDown`, `ToolStripControlHost`, `Control.SelectNextControl`, `ToolStripDropDownClosingEventArgs`, NUnit 4, STA WinForms tests, targets `net48;net8.0-windows`.

**Spec:** `docs/plans/20260829-001-interactive-tooltip-popover-placement-engine.md` — specifically the interaction contract requiring focusable content, `Tab` navigation, Escape close/focus restore, and native outside-click dismissal. This plan also addresses the runtime defects reproduced in the integrated Feedback demo on 2026-08-31.

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; public APIs remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- The product and test projects must continue to compile for both `net48` and `net8.0-windows` from shared implementation code.
- Do not add a global mouse hook, keyboard hook, CBT hook, polling loop, or new NuGet dependency.
- Preserve `BootstrapPopover.CloseOnEscape == true` behavior: Escape closes and restores focus to a live target.
- Preserve `BootstrapPopover.CloseOnClickOutside == true` behavior: a real outside mouse click closes and native focus moves to the clicked control.
- Preserve `CloseOnClickOutside == false`: outside clicks must not close the popover.
- Pressing `Alt` alone must not close an open interactive popover.
- `Tab` must move forward through eligible focusable descendants inside popover content in WinForms tab order.
- `Shift+Tab` must move backward through eligible focusable descendants inside popover content.
- Disabled, invisible, `TabStop == false`, non-selectable, and label-only controls must be skipped.
- Traversal must work across nested containers, including the demo layout where `Apply` and `Close` live in a nested command `FlowLayoutPanel`.
- Do not turn the popover into a modal focus trap. At the content boundary, traversal must follow the explicit behavior defined in Task 3 rather than silently trapping forever.
- Keep caller ownership unchanged: `Target` and `Content` are never disposed by `BootstrapPopover`.
- Do not regress overlay placement, anchor tracking, theme/DPI handling, popup geometry, or 500-cycle lifecycle behavior.
- Add failing regression tests before changing production code.
- All popup/focus tests must run `[Apartment(ApartmentState.STA)]` and remain non-parallelizable.

---

## Current Failure Model

The current host chain is:

```text
BootstrapPopover
└── BootstrapOverlayDropDown : ToolStripDropDown
    └── ToolStripControlHost
        └── BootstrapOverlaySurface : Panel
            └── caller Content
                ├── TextBox
                ├── CheckBox
                └── nested command container
                    ├── Apply button
                    └── Close button
```

Current implementation already focuses the first eligible descendant when the popup opens, but it does not own the subsequent dialog-key traversal path. `BootstrapOverlayDropDown.ProcessCmdKey(...)` only special-cases Escape and delegates everything else to `ToolStripDropDown`, whose keyboard behavior is menu-oriented.

This produces two observable defects:

1. `Alt` enters ToolStrip/menu keyboard processing and closes the popover even though `CloseOnEscape` is the only configured keyboard dismissal policy.
2. `Tab`/`Shift+Tab` do not traverse the caller-hosted controls as a normal WinForms form/container would.

The existing test suite verifies initial focus but does not simulate the full keyboard sequence, so both defects can pass CI.

---

## File Structure and Responsibilities

### Files to modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
  - Own low-level popup keyboard and closing semantics.
  - Intercept unwanted Alt/menu close only after close-reason evidence is captured.
  - Route `Tab`/`Shift+Tab` to a caller-provided traversal callback or hosted-content traversal implementation.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
  - Own popover-level focus policy.
  - Provide hosted content root to the dropdown keyboard layer.
  - Keep Escape focus restore and outside-click behavior distinct.
  - Handle first/last-tab boundary semantics.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
  - Add end-to-end STA regression tests for Alt, Tab, Shift+Tab, nested content, skip rules, outside click, Escape, and focus boundary behavior.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`
  - Add focused host-level tests only if required to assert close-reason cancellation or callback routing in isolation.

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
  - Keep the existing interactive popover sample as the manual acceptance surface.
  - Change only if explicit `TabIndex` values are needed to make traversal intent deterministic; do not add demo-only key handlers.

- `docs/TESTING.md`
  - Record the interactive popover keyboard regression matrix and manual verification steps.

- `docs/plans/20260829-001-interactive-tooltip-popover-placement-engine.md`
  - Do not rewrite the historical plan. Only update completion/checklist wording if the project convention requires tracking fixes in-place; otherwise leave it unchanged and treat this plan as the corrective follow-up.

### No new public API by default

The preferred fix is internal. Do **not** add public `FocusTrap`, `KeyboardMode`, `CloseOnAlt`, or similar properties unless the architecture fallback is approved separately.

---

## Required Behavior Contract

### Keyboard matrix

| Input | Expected behavior while Popover is open |
| --- | --- |
| `Alt` press/release | Popover remains open; current focused child remains focused unless Windows changes focus for another explicit application reason |
| `Escape`, `CloseOnEscape = true` | Popover closes; focus returns to live `Target` |
| `Escape`, `CloseOnEscape = false` | Popover remains open |
| `Tab` | Move to next eligible content control in tab order |
| `Shift+Tab` | Move to previous eligible content control in tab order |
| Character typing in TextBox | TextBox receives input; popover remains open |
| Space on CheckBox/Button | Normal control activation; popover remains open unless application handler calls `Hide()` |
| Enter on Button | Normal button activation; popover remains open unless application handler calls `Hide()` |
| Outside mouse click, close enabled | Popover closes; clicked control owns resulting focus |
| Outside mouse click, close disabled | Popover remains open |

### Tab boundary semantics

This plan intentionally avoids a modal focus trap.

- Forward `Tab` from the last eligible popover descendant closes the popover **without restoring target focus**, then advances to the next selectable control after `Target` in the owning form/container.
- Backward `Shift+Tab` from the first eligible popover descendant closes the popover **without restoring target focus**, then advances to the previous selectable control before `Target`.
- If no next/previous selectable peer exists, close the popover and allow focus to fall back to `Target` only as a last resort.
- This boundary close is a focus-navigation close, not an Escape close; it must not set `_restoreFocusAfterClose = true`.

This preserves normal desktop tab order instead of trapping the user inside a non-modal popover.

---

### Task 1: Capture Native Close-Reason Evidence and Freeze Regression Tests

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Optional test-only instrumentation: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`

**Interfaces:**
- Consumes: current `BootstrapPopover.Show()`, `Hide()`, `IsOpen`, `CloseOnEscape`, `CloseOnClickOutside`.
- Produces: failing regression tests that distinguish `Alt`, Escape, and outside-click close paths.

- [ ] **Step 1: Add an STA helper that creates a real form + target + multi-control interactive popover**

Use a deterministic tree equivalent to the Feedback demo:

```csharp
private static (Form Form, Button Target, FlowLayoutPanel Content, TextBox Editor, CheckBox Option, Button Apply, Button Close, BootstrapPopover Popover)
    CreateInteractivePopoverFixture()
{
    var form = new Form
    {
        ShowInTaskbar = false,
        StartPosition = FormStartPosition.Manual,
        Bounds = new Rectangle(200, 200, 700, 500)
    };

    var target = new Button
    {
        Text = "Open",
        Location = new Point(30, 30),
        Size = new Size(120, 30),
        TabIndex = 0
    };

    var content = new FlowLayoutPanel
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        MinimumSize = new Size(280, 0)
    };

    var editor = new TextBox { Width = 220, TabIndex = 0 };
    var option = new CheckBox { AutoSize = true, Text = "Enable", TabIndex = 1 };
    var commands = new FlowLayoutPanel
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        TabIndex = 2,
        TabStop = false
    };
    var apply = new Button { Text = "Apply", TabIndex = 0 };
    var close = new Button { Text = "Close", TabIndex = 1 };
    commands.Controls.Add(apply);
    commands.Controls.Add(close);
    content.Controls.Add(editor);
    content.Controls.Add(option);
    content.Controls.Add(commands);

    form.Controls.Add(target);
    var popover = new BootstrapPopover
    {
        Target = target,
        Content = content,
        CloseOnEscape = true,
        CloseOnClickOutside = true
    };

    return (form, target, content, editor, option, apply, close, popover);
}
```

Keep disposal in each test explicit because caller-owned content is not disposed by `BootstrapPopover`.

- [ ] **Step 2: Add failing regression test `AltDoesNotDismissOpenPopover`**

Open the form, activate it, show the popover, assert the editor has focus, then inject the same Alt keyboard path the runtime receives. Prefer calling protected keyboard processing through a small test subclass only if direct `SendKeys.SendWait("%")` is unreliable in CI.

Expected assertions:

```csharp
Assert.That(popover.IsOpen, Is.True);
Assert.That(editor.Focused, Is.True);
```

The test must fail on current `main` by observing the popup close or focus leave unexpectedly.

- [ ] **Step 3: Add control tests proving Escape still closes and restores target focus**

```csharp
popover.Show();
Application.DoEvents();
Assert.That(editor.Focused, Is.True);

SendEscape();
Application.DoEvents();

Assert.That(popover.IsOpen, Is.False);
Assert.That(target.Focused, Is.True);
```

Also add `CloseOnEscape = false` and assert Escape leaves the popover open.

- [ ] **Step 4: Add outside-click regression coverage before changing production code**

Place a second `Button outside` on the form, open the popover, click that button through a real message path, and assert:

```csharp
Assert.That(popover.IsOpen, Is.False);
Assert.That(outside.Focused, Is.True);
```

Repeat with `CloseOnClickOutside = false` and assert the popover remains open.

- [ ] **Step 5: If Alt close reason is not observable from the public surface, add test-only close-reason capture to `BootstrapOverlayDropDownTests`**

Use a test subclass or internal visibility already available to record `ToolStripDropDownCloseReason` from `OnClosing`. The test should establish the exact close reason produced by Alt on supported WinForms runtimes rather than assuming it.

Do not implement cancellation yet.

- [ ] **Step 6: Run only the new close-path tests on both target frameworks**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "BootstrapPopoverTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "BootstrapPopoverTests"
```

Expected before implementation:
- Alt regression: FAIL.
- Existing initial-focus tests: PASS.
- Escape/outside-click control tests: PASS unless they reveal an additional existing defect.

- [ ] **Step 7: Commit the failing tests**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs
git commit -m "test: reproduce popover keyboard dismissal defects"
```

If the optional dropdown test file was not needed, omit it from `git add`.

---

### Task 2: Prevent Alt/Menu-Mode Close Without Breaking Real Outside Clicks

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Optional Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`

**Interfaces:**
- Consumes: exact Alt close reason captured in Task 1.
- Produces: internal closing policy that keeps Alt open while leaving Escape and native outside-click behavior intact.

- [ ] **Step 1: Add the smallest internal state required to identify the unwanted Alt close path**

Prefer close-reason + current keyboard state rather than a global hook. For example, if Task 1 proves Alt arrives as `Keyboard` while `ModifierKeys` includes `Alt`, add a narrow helper:

```csharp
private static bool IsAltMenuDismissal(ToolStripDropDownClosingEventArgs e)
{
    return e.CloseReason == ToolStripDropDownCloseReason.Keyboard
        && (ModifierKeys & Keys.Alt) == Keys.Alt;
}
```

If Task 1 reveals a different close reason or Alt-up sequence, encode the observed condition exactly. Do not generalize to cancel every keyboard close.

- [ ] **Step 2: Override `OnClosing` and cancel only the proven Alt/menu dismissal**

Structure:

```csharp
protected override void OnClosing(ToolStripDropDownClosingEventArgs e)
{
    if (IsAltMenuDismissal(e))
    {
        e.Cancel = true;
        return;
    }

    base.OnClosing(e);
}
```

If base event ordering is required by WinForms, call `base.OnClosing(e)` first and preserve the cancellation result; follow the behavior proven by tests rather than guessing.

- [ ] **Step 3: Keep Escape handling in `ProcessCmdKey` unchanged in semantics**

Do not convert Escape into generic close-reason logic. Preserve:

```csharp
if (CloseOnEscape && keyData == Keys.Escape && EscapeRequested is not null)
{
    EscapeRequested();
    return true;
}
```

This ensures `_restoreFocusAfterClose` is still set only by `BootstrapPopover.OnEscapeRequested()`.

- [ ] **Step 4: Run the Alt/Escape/outside-click regression set**

Expected:
- Alt test: PASS.
- Escape enabled: PASS and target focused.
- Escape disabled: PASS and popup remains open.
- Outside click enabled: PASS and outside control focused.
- Outside click disabled: PASS and popup remains open.

- [ ] **Step 5: Run existing overlay and popover tests on both frameworks**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "BootstrapPopoverTests|BootstrapOverlayDropDownTests|BootstrapOverlaySurfaceTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "BootstrapPopoverTests|BootstrapOverlayDropDownTests|BootstrapOverlaySurfaceTests"
```

- [ ] **Step 6: Commit the Alt dismissal fix**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "fix: keep popover open on alt menu activation"
```

---

### Task 3: Implement Forward and Backward Tab Traversal Through Hosted Content

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:**
- Consumes: `_content` tree owned by `BootstrapPopover`, existing `FindFirstFocusable(...)`, `_dropDown.ProcessCmdKey(...)`.
- Produces: an internal Tab routing contract; no public API.

- [ ] **Step 1: Add failing forward traversal test for the exact demo sequence**

After opening, verify:

```text
Editor -> Option -> Apply -> Close
```

Use explicit `TabIndex` values and a keyboard helper that exercises the popup dialog-key path.

Assertions after each key:

```csharp
Assert.That(editor.Focused, Is.True);
SendTab();
Assert.That(option.Focused, Is.True);
SendTab();
Assert.That(apply.Focused, Is.True);
SendTab();
Assert.That(close.Focused, Is.True);
```

The current implementation must fail before production changes.

- [ ] **Step 2: Add failing reverse traversal test**

Focus `close`, then verify:

```text
Close -> Apply -> Option -> Editor
```

using `Shift+Tab`.

- [ ] **Step 3: Add skip-rule test**

Insert controls with:

```csharp
Visible = false;
Enabled = false;
TabStop = false;
```

and a `Label`; verify forward/reverse traversal never selects them.

- [ ] **Step 4: Add an internal Tab callback on `BootstrapOverlayDropDown`**

Use a narrowly scoped internal delegate instead of embedding popover-specific knowledge in the generic dropdown host:

```csharp
public Func<bool, bool>? TabNavigationRequested { get; set; }
```

where:
- argument `forward == true` for `Tab`;
- argument `forward == false` for `Shift+Tab`;
- return `true` when the request was fully handled.

Clear it in `Dispose(bool)` alongside `EscapeRequested`.

- [ ] **Step 5: Intercept Tab in the dropdown key path before delegating to ToolStrip menu semantics**

Handle only plain Tab and Shift+Tab:

```csharp
var keyCode = keyData & Keys.KeyCode;
var modifiers = keyData & Keys.Modifiers;
if (keyCode == Keys.Tab
    && (modifiers == Keys.None || modifiers == Keys.Shift)
    && TabNavigationRequested is not null)
{
    var forward = modifiers != Keys.Shift;
    if (TabNavigationRequested(forward))
    {
        return true;
    }
}
```

Do not consume Ctrl+Tab or Alt+Tab.

If WinForms routes Tab through `ProcessDialogKey` rather than `ProcessCmdKey` in the real popup, move the interception to `ProcessDialogKey(Keys keyData)` and keep Escape where it already works. The failing tests determine the correct override.

- [ ] **Step 6: Wire `BootstrapPopover` to the Tab callback**

Constructor:

```csharp
_dropDown.TabNavigationRequested = OnTabNavigationRequested;
```

Dispose:

```csharp
_dropDown.TabNavigationRequested = null;
```

- [ ] **Step 7: Implement `OnTabNavigationRequested(bool forward)` using WinForms tab order**

The implementation must start from the actual focused descendant and use the hosted content root rather than manually sorting controls.

Preferred structure:

```csharp
private bool OnTabNavigationRequested(bool forward)
{
    var content = _content;
    if (content is null || content.IsDisposed)
    {
        return false;
    }

    var current = FindFocusedDescendant(content);
    if (current is null)
    {
        var first = forward ? FindFirstFocusable(content) : FindLastFocusable(content);
        return first?.Focus() == true;
    }

    if (content.SelectNextControl(current, forward, true, true, false))
    {
        return true;
    }

    return MoveFocusPastPopover(forward);
}
```

Use `nested: true`, `wrap: false` so nested command buttons participate but the popover does not become a focus trap.

- [ ] **Step 8: Add focused-descendant and reverse-start helpers**

Implement internal private helpers with exact semantics:

```csharp
private static Control? FindFocusedDescendant(Control root)
```

Return the deepest/selectable descendant whose `ContainsFocus` is true.

```csharp
private static Control? FindLastFocusable(Control root)
```

Walk WinForms tab order backward and apply the same eligibility rules as `FindFirstFocusable`.

Do not create a second eligibility policy; factor a single helper if needed:

```csharp
private static bool IsFocusable(Control control)
{
    return control.Visible
        && control.Enabled
        && control.TabStop
        && control.CanSelect;
}
```

- [ ] **Step 9: Implement boundary navigation outside the popover**

Add:

```csharp
private bool MoveFocusPastPopover(bool forward)
```

Algorithm:
1. Capture live `Target` and its parent container before hiding.
2. Call `Hide()` without setting `_restoreFocusAfterClose`.
3. Ask the target's parent container to select the next/previous control using:

```csharp
parent.SelectNextControl(target, forward, true, true, false)
```

4. If parent navigation fails, try the owning `Form` with equivalent traversal.
5. If no destination exists and target is still valid, focus target as fallback.
6. Return `true` because the Tab keystroke has been consumed by the navigation policy.

Do not call `OnEscapeRequested()` for this path.

- [ ] **Step 10: Run focused Tab tests on both frameworks**

Expected sequence must pass identically on `net48` and `net8.0-windows`.

- [ ] **Step 11: Commit keyboard traversal**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "fix: support popover tab focus traversal"
```

---

### Task 4: Verify Focus Boundary Behavior Against the Integrated Demo Model

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Modify only if necessary: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

**Interfaces:**
- Consumes: Task 3 `MoveFocusPastPopover(bool forward)` behavior.
- Produces: regression coverage proving the popover participates in surrounding form tab order rather than trapping focus.

- [ ] **Step 1: Add a target-next-control boundary test**

Create form controls in tab order:

```text
BeforeButton (TabIndex 0)
PopoverTarget (TabIndex 1)
AfterButton  (TabIndex 2)
```

Open popover, focus its last child, press `Tab`, then assert:

```csharp
Assert.That(popover.IsOpen, Is.False);
Assert.That(afterButton.Focused, Is.True);
```

- [ ] **Step 2: Add reverse boundary test**

Open popover, focus the first child, press `Shift+Tab`, then assert:

```csharp
Assert.That(popover.IsOpen, Is.False);
Assert.That(beforeButton.Focused, Is.True);
```

- [ ] **Step 3: Verify boundary close does not trigger Escape-style focus restoration**

Track focus changes or simply assert that `Target` does not regain final focus when a valid before/after peer exists.

- [ ] **Step 4: Verify nested demo controls have deterministic tab intent**

Inspect `CreateInteractivePopoverContent()` in `FeedbackDemoForm`.

If default WinForms `TabIndex` creation order already yields:

```text
TextBox -> CheckBox -> Apply -> Close
```

leave production demo code unchanged.

If not deterministic across target frameworks, set explicit `TabIndex` values only:

```csharp
editor.TabIndex = 0;
checkBox.TabIndex = 1;
commands.TabIndex = 2;
commands.TabStop = false;
apply.TabIndex = 0;
close.TabIndex = 1;
```

Do **not** add demo-specific KeyDown/PreviewKeyDown handlers.

- [ ] **Step 5: Extend `FeedbackDemoFormTests` to assert the interactive popover content contract**

The test should locate the expected text editor, checkbox, Apply button, and Close button and assert they remain focus-eligible in the intended order.

- [ ] **Step 6: Commit boundary and demo contract coverage**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
git commit -m "test: cover popover focus boundary behavior"
```

If the demo file did not need changes, omit it from the commit.

---

### Task 5: Harden Keyboard Handling Against Regression and Resource/Lifecycle Churn

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Modify if defects are found: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
- Modify if defects are found: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`

**Interfaces:**
- Consumes: completed Alt and Tab handling.
- Produces: stable keyboard behavior across repeated open/close, runtime focus changes, and disabled/removed controls.

- [ ] **Step 1: Add repeated keyboard-cycle test**

Run at least 100 cycles:

```text
Show -> Tab through children -> boundary close -> refocus target -> Show
```

and periodically:

```text
Show -> Alt -> assert still open -> Escape -> assert target focus
```

Assert no `ObjectDisposedException`, invalid cross-thread access, duplicate `Opened`/`Closed` events, or stale focus callbacks.

- [ ] **Step 2: Extend the existing 500 open/close lifecycle test with keyboard callback ownership assertions**

Verify the internal dropdown callback is not duplicated across open cycles. If direct internal observation is not available, assert exactly one focus transition per Tab key and exactly one close event per boundary/Escape action.

- [ ] **Step 3: Add runtime eligibility mutation test**

Open the popover, then disable or hide the currently next control before pressing Tab. Assert traversal skips it and moves to the next eligible control without closing unexpectedly.

- [ ] **Step 4: Add target disposal safety test while keyboard callback is active**

Open popover, dispose `Target`, process pending messages, then assert:

```csharp
Assert.That(popover.IsOpen, Is.False);
Assert.That(popover.Target, Is.Null);
```

and subsequent keyboard processing does not throw.

- [ ] **Step 5: Run the full test project on both frameworks**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows
```

Expected: all tests PASS.

- [ ] **Step 6: Run repository build scripts**

```powershell
.\build.ps1
.\test.ps1
```

Expected: both scripts complete successfully.

- [ ] **Step 7: Commit hardening changes**

```bash
git add src tests
git commit -m "test: harden popover keyboard lifecycle"
```

Only include production files if the hardening tests exposed a real defect requiring a fix.

---

### Task 6: Architecture Fallback Gate — Only If ToolStripDropDown Cannot Safely Meet the Contract

**Files:**
- No code changes unless this gate is triggered.
- Create a follow-up plan under `docs/plans/` if triggered.

**Interfaces:**
- Consumes: empirical results from Tasks 1–5.
- Produces: a stop/go architecture decision; this task must not silently redesign the host.

- [ ] **Step 1: Evaluate the fix against four non-negotiable behaviors**

The current `ToolStripDropDown` architecture is acceptable only if all four are simultaneously true:

```text
1. Alt does not close.
2. Tab/Shift+Tab traverse correctly.
3. Real outside clicks still close when enabled and preserve clicked-control focus.
4. Escape still closes and restores target focus.
```

- [ ] **Step 2: Stop if the implementation requires broad cancellation of `ToolStripDropDownCloseReason.Keyboard`**

Broadly cancelling all keyboard closes is not acceptable because it can mask legitimate native popup behavior and create future interaction regressions.

- [ ] **Step 3: Stop if Tab routing requires global `IMessageFilter`, keyboard hooks, or application-wide state**

Those mechanisms violate the original overlay constraints and would make a local component globally invasive.

- [ ] **Step 4: Stop if preserving outside-click dismissal requires reimplementing native mouse capture globally**

Do not replace a single host mismatch with global hook complexity.

- [ ] **Step 5: If any stop condition is met, create a separate architecture plan instead of stacking more patches**

The follow-up plan should compare at least:

```text
A. custom non-modal Form/NativeWindow host with explicit activation/focus behavior;
B. specialized popup host derived from ToolStripDropDown with Win32 style/message overrides;
C. a dedicated reusable overlay window abstraction shared by Popover and future interactive overlays.
```

The architecture plan must explicitly preserve outside-click semantics, DPI/multi-monitor placement, activation behavior, focus traversal, and caller-owned content lifetime.

- [ ] **Step 6: If all four behaviors are green without stop conditions, document that ToolStripDropDown remains the accepted v1 host**

No architecture change is required in this fix.

---

### Task 7: Documentation and Manual Acceptance Verification

**Files:**
- Modify: `docs/TESTING.md`
- Modify if project docs already document Popover behavior: `docs/COMPONENTS.md`
- No public API baseline changes expected.

**Interfaces:**
- Consumes: final tested keyboard behavior.
- Produces: documented regression matrix and manual acceptance procedure.

- [ ] **Step 1: Add a Popover keyboard regression section to `docs/TESTING.md`**

Document this exact manual sequence in the integrated demo:

```text
1. Open Feedback page.
2. Click "Open interactive Popover".
3. Verify "Draft value" editor receives focus.
4. Press Tab: focus moves to "Enable option".
5. Press Tab: focus moves to "Apply".
6. Press Tab: focus moves to "Close".
7. Press Shift+Tab repeatedly and verify reverse order.
8. Re-open, press Alt once and release: Popover remains visible.
9. Press Escape: Popover closes and target button regains focus.
10. Re-open and click a control outside: Popover closes and clicked control receives focus.
11. Disable "Outside close", re-open, click outside: Popover remains open.
```

- [ ] **Step 2: Document boundary Tab behavior**

State that the popover is non-modal and does not trap focus: Tab from the last child and Shift+Tab from the first child close the popover and continue through surrounding form tab order.

- [ ] **Step 3: Update component documentation only if it currently promises a different keyboard behavior**

Do not add new API documentation because this fix is internal.

- [ ] **Step 4: Run final manual verification in both Light and Dark themes**

Verify focus cues remain visible and theme switching while open does not alter keyboard behavior.

- [ ] **Step 5: Run final manual verification at a non-100% DPI setting if available**

Keyboard behavior should remain identical while overlay placement still follows existing DPI logic.

- [ ] **Step 6: Commit documentation**

```bash
git add docs/TESTING.md docs/COMPONENTS.md
git commit -m "docs: document popover keyboard regression checks"
```

If `docs/COMPONENTS.md` did not need changes, omit it.

---

## Final Verification Checklist

- [ ] `Alt` no longer dismisses an open `BootstrapPopover`.
- [ ] `Escape` still dismisses only when `CloseOnEscape == true`.
- [ ] Escape close restores focus to a live target.
- [ ] Forward Tab order works through nested popover controls.
- [ ] Reverse Shift+Tab order works through nested popover controls.
- [ ] Hidden, disabled, non-tab-stop, and non-selectable controls are skipped.
- [ ] Boundary Tab does not trap focus inside the non-modal popover.
- [ ] Boundary Tab does not incorrectly restore target focus when a valid peer exists.
- [ ] Native outside-click close remains correct when enabled.
- [ ] Outside click remains disabled when `CloseOnClickOutside == false`.
- [ ] Typing, checkbox activation, and button activation do not cause incidental close.
- [ ] Target/content disposal remains safe.
- [ ] Existing popup geometry tests remain green.
- [ ] Existing 500-cycle lifecycle test remains green.
- [ ] Full `net48` test suite passes.
- [ ] Full `net8.0-windows` test suite passes.
- [ ] `build.ps1` passes.
- [ ] `test.ps1` passes.
- [ ] Integrated Feedback demo manual matrix passes in Light and Dark themes.
- [ ] No new public API was introduced unless a separate architecture decision explicitly approved it.

## Expected Commit Sequence

```text
test: reproduce popover keyboard dismissal defects
fix: keep popover open on alt menu activation
fix: support popover tab focus traversal
test: cover popover focus boundary behavior
test: harden popover keyboard lifecycle
docs: document popover keyboard regression checks
```

Each commit must leave the repository in a reviewable state. The first test commit is intentionally allowed to contain the newly failing regression tests; subsequent commits should progressively return the affected test slice to green.
