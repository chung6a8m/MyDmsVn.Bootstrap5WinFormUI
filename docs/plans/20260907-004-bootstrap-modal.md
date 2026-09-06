# BootstrapModal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapModal` that preserves native WinForms modal-dialog behavior (`ShowDialog`, owner disabling, `DialogResult`, `AcceptButton`, `CancelButton`, focus, keyboard routing, `FormClosing`/`FormClosed`, accessibility, and message-loop semantics) while adding Bootstrap-inspired dialog chrome, theme-aware rendering, owner-aware backdrop behavior, size presets, DPI-aware positioning, and reduced-motion-aware transitions.

**Architecture:** `BootstrapModal` derives directly from `System.Windows.Forms.Form`. The native `Form` remains the behavioral/modal-loop source of truth. The framework owns only Bootstrap presentation, the dialog layout shell, a dedicated internal backdrop window, owner/monitor geometry, dismissal policy, theme/DPI synchronization, and transition state. The backdrop is presentation infrastructure, not a second modality engine: `ShowDialog(owner)` still owns modality and owner disabling. Pure/internal helpers resolve layout, colors, size constraints, placement, and lifecycle decisions so correctness can be tested without opening a real modal for every case. GUI integration tests use an STA message-loop harness that deterministically schedules dismissal before calling `ShowDialog`; no unattended test is allowed to leave a modal waiting for human input.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapTheme`, `BootstrapVariant`/theme tokens, `DpiScaler`, shared Rendering primitives, existing Animation infrastructure (`BootstrapAnimation`, `AnimationOwnerLifecycle`, frame scheduler), NUnit, integrated demo application.

---

## Global Constraints

- Preserve repository dependency direction from `AGENTS.md` and `docs/ARCHITECTURE.md`: Modal may depend on Theme/Rendering/Animation/Compatibility, but shared infrastructure must not depend on `BootstrapModal`.
- Keep native `System.Windows.Forms.Form` as the behavioral source of truth. Do **not** build a custom message loop, fake `DialogResult`, application-wide focus manager, or parallel owner-disable system.
- The supported modal path is `ShowDialog()` / `ShowDialog(IWin32Window owner)`. Modeless `Show()` is outside the V1 contract unless implementation work proves it can be supported without weakening modal semantics.
- Preserve inherited native APIs and events, including at minimum: `Text`, `Icon`, `AcceptButton`, `CancelButton`, `DialogResult`, `Owner`, `ActiveControl`, `ShowDialog`, `Close`, `Load`, `Shown`, `FormClosing`, `FormClosed`, `Activated`, `Deactivate`, and standard WinForms focus/validation behavior.
- Constructor defaults may set framework-required inherited properties such as `FormBorderStyle = None`, `ShowInTaskbar = false`, `StartPosition = Manual`, `MinimizeBox = false`, and `MaximizeBox = false`. V1 must not shadow inherited properties merely to pretend callers cannot change them. Mutating framework-required window-style properties is unsupported when it invalidates Bootstrap rendering assumptions.
- Do not implement a second generic popup/overlay system. Reuse low-level shared helpers where appropriate, but do not force `BootstrapModal` through `BootstrapOverlayDropDown` or `BootstrapOverlaySurface`; those abstractions serve anchored overlays, not modal top-level windows.
- Backdrop lifecycle must never become the modality mechanism. If backdrop creation/showing fails, the modal must fail predictably or continue without leaking/stranding a top-level window; it must never leave the owner permanently disabled.
- V1 supports one active `BootstrapModal` in a normal owner interaction flow. Nested Bootstrap modals are **not a supported contract**, matching Bootstrap guidance. Do not add a global modal stack/coordinator merely to police every possible native `Form.ShowDialog` nesting scenario.
- Do not add fullscreen modal variants in V1. Small/Default/Large/ExtraLarge desktop presets are sufficient. Fullscreen can be considered later if concrete application scenarios require it.
- Do not add draggable/resizable custom chrome, maximize/minimize, arbitrary title-bar commands, MDI integration, taskbar ownership, acrylic/Mica, layered-window blur, per-pixel alpha, or platform-specific DWM effects in V1.
- Do not hardcode Bootstrap hex values. Resolve framework-owned colors from the current theme.
- All framework-owned geometry is expressed in logical pixels and scaled through `DpiScaler`. Do not use APIs unavailable on `net48` such as `Math.Clamp` without a compatibility abstraction.
- Theme subscriptions, owner subscriptions, backdrop events, animation callbacks, timers/schedulers, and native handles must be detached/released deterministically during close/disposal.
- Do not dispose caller-owned controls, `AcceptButton`, `CancelButton`, icons, images, fonts explicitly assigned by the caller, or the owner form.
- Tests that instantiate/show WinForms windows run STA and non-parallel where they mutate global theme state.
- Every test that enters a modal message loop must arrange its own deterministic close action **before** `ShowDialog` begins and must have a bounded hang timeout. No automated test may call an unattended `ShowDialog()` and wait for input.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5.3 Modal is the visual/interaction reference, but WinForms `Form` remains authoritative for desktop modality.

Reference:

- Bootstrap Modal: https://getbootstrap.com/docs/5.3/components/modal/
- `Form.ShowDialog`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.showdialog
- `Form.DialogResult`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.dialogresult
- `Form.AcceptButton`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.acceptbutton
- `Form.CancelButton`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.cancelbutton
- `Form.FormClosing`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.formclosing
- `Screen.WorkingArea`: https://learn.microsoft.com/dotnet/api/system.windows.forms.screen.workingarea

Map Bootstrap concepts as follows:

| Bootstrap concept | WinForms adaptation |
|---|---|
| `.modal` / modal JS | native `BootstrapModal : Form` + `ShowDialog` |
| modal title | inherited `Text` rendered in framework header |
| modal body | `BodyPanel` |
| modal footer | `FooterPanel`, visible when it has visible content |
| close button | framework-owned header close button |
| `backdrop: true` | `BackdropMode = Dismissible` |
| `backdrop: 'static'` | `BackdropMode = Static` |
| `backdrop: false` | `BackdropMode = None` |
| `keyboard: true/false` | `CloseOnEscape` |
| focus on shown modal | native modal activation + optional `InitialFocusControl` |
| scrolling long content | body-level AutoScroll under constrained working-area height |
| optional sizes | `BootstrapModalSize.Small/Default/Large/ExtraLarge` |
| fade transition | shared Animation infrastructure; reduced motion resolves immediately |
| one modal at a time | nested Bootstrap modals are unsupported in V1 |

Important adaptation rules:

1. `ShowDialog(owner)` remains responsible for disabling/re-enabling the owner and running the nested modal message loop. The library must not reproduce this with `Enabled = false` bookkeeping.
2. `AcceptButton` and `CancelButton` stay native. Enter/Escape behavior already handled by native dialog buttons must not cause duplicate clicks or duplicate close requests.
3. `FormClosing` cancellation stays authoritative. Backdrop click, close-button click, Escape, and API `Close()` all converge on one close-request path and must respect a consumer canceling `FormClosing`.
4. `DialogResult` is not replaced with a custom result enum. Callers use native `System.Windows.Forms.DialogResult`.
5. The framework may provide a default close button, but it must not silently overwrite a caller-assigned `CancelButton` or `AcceptButton`.
6. A long modal must remain inside the target monitor working area. Body scrolling is preferred over placing title/footer off-screen.
7. The backdrop is shown above the owner and below the dialog. It is never `TopMost` globally by default and must not float above unrelated applications.
8. Alt+Tab / application deactivation must not leave the backdrop or modal incorrectly above another application. Ownership/z-order tests are required because this project has already encountered top-level popup issues on app deactivation.
9. Closing or disposing the owner while the modal is active must not leave a backdrop window behind.
10. V1 does not promise nested Bootstrap modal behavior. If a consumer opens another native dialog from a BootstrapModal, normal WinForms behavior applies to that native dialog, but the framework does not introduce a modal stack abstraction.

---

## Public Contract to Implement

Keep the V1 public surface intentionally small:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapModalSize
{
    Small,
    Default,
    Large,
    ExtraLarge
}

public enum BootstrapModalBackdropMode
{
    None,
    Dismissible,
    Static
}

public class BootstrapModal : Form
{
    [Category("Appearance")]
    [DefaultValue(BootstrapModalSize.Default)]
    public BootstrapModalSize ModalSize { get; set; }

    [Category("Behavior")]
    [DefaultValue(BootstrapModalBackdropMode.Dismissible)]
    public BootstrapModalBackdropMode BackdropMode { get; set; }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool CloseOnEscape { get; set; }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowCloseButton { get; set; }

    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius { get; set; }

    [Browsable(false)]
    public Control? InitialFocusControl { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel BodyPanel { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public FlowLayoutPanel FooterPanel { get; }
}
```

### Public API rules

- `ModalSize` defaults to `Default`.
- `BackdropMode` defaults to `Dismissible`.
- `CloseOnEscape` defaults to `true`.
- `ShowCloseButton` defaults to `true`.
- `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical pixels; values below `-1` throw `ArgumentOutOfRangeException` before mutation.
- `InitialFocusControl = null` means native/select-next behavior chooses the first usable control. A non-null value must be a non-disposed descendant of the modal when focus is applied; an invalid value must not crash the modal loop.
- `BodyPanel` and `FooterPanel` are framework-owned container controls but caller-owned child controls placed inside them are not disposed independently by framework code. Normal parent disposal semantics still apply when the modal itself is disposed.
- `FooterPanel` hides/collapses when it contains no visible controls; callers may add `BootstrapButton` or native buttons and set native `DialogResult` on those buttons.
- Inherited `Text` is the modal title. Do not add a duplicate `Title` property.
- Inherited `AcceptButton`, `CancelButton`, and `DialogResult` remain the command/result contract. Do not add `PrimaryButton`, `SecondaryButton`, `ModalResult`, or equivalent aliases in V1.
- Do not expose the internal backdrop window publicly.
- Do not expose internal transition/lifecycle state publicly.

### API review gate

Before implementation is considered complete, verify whether `DesignerSerializationVisibility.Content` on read-only `BodyPanel`/`FooterPanel` behaves correctly on both supported designer/runtime targets. If Visual Studio designer serialization cannot support this shape safely without a custom designer, keep runtime properties public but document V1 as programmatic composition and defer custom designer support rather than adding fragile design-time dependencies.

---

## Visual Contract

### Dialog shell

- Surface background: current theme surface.
- Foreground/title: current theme text.
- Border: current theme border token, DPI-scaled.
- Corner radius: current theme radius or explicit `BorderRadius`.
- Header/body/footer separators use theme border/muted border tokens.
- Header contains inherited `Text` and optional close button.
- Close glyph uses a framework vector primitive or existing source-neutral icon infrastructure; do not add a package dependency.
- Footer aligns actions to the trailing edge in LTR and leading/visual-trailing edge in RTL using normal WinForms layout semantics.
- Shadow/elevation is optional for V1 only if it can be implemented with existing rendering/window primitives without layered-window/DWM complexity. Correct modality/lifecycle is higher priority than shadow parity.

### Backdrop

- Backdrop color derives from theme (normally a dark neutral) with a controlled opacity; do not hardcode Bootstrap CSS values into rendering code.
- `None`: no backdrop window is created.
- `Dismissible`: click on backdrop requests modal close.
- `Static`: click on backdrop does not close the modal. It may trigger a short attention cue only after the core lifecycle is correct; the cue must respect reduced motion.
- Backdrop must not activate as the primary application window, appear in the taskbar, or become globally topmost.
- Backdrop covers the owner bounds when an owner is supplied. If there is no usable owner, resolve the target monitor working area for the modal and cover that working area.
- Backdrop geometry tracks owner move/resize while the modal is visible without stealing focus.

### Size presets

Use logical-width presets as implementation constants resolved by pure layout logic. Initial target values may mirror Bootstrap-like desktop proportions but must be validated in the demo before API freeze:

```text
Small      ≈ 300 logical px
Default    ≈ 500 logical px
Large      ≈ 800 logical px
ExtraLarge ≈ 1140 logical px
```

Rules:

- Presets are **maximum/preferred dialog widths**, not a license to extend past the monitor working area.
- Apply DPI scaling before final monitor clamping.
- Respect caller `MinimumSize`/`MaximumSize` only when they do not contradict the framework requirement to keep dialog chrome reachable on-screen.
- Height is content-driven up to the available working-area budget. When content exceeds the budget, constrain the body and enable body scrolling; title and footer remain reachable.
- Do not mutate caller child-control sizes merely to force a preset.

---

## Window Ownership, Placement, and Z-Order Contract

Introduce a dedicated internal `BootstrapModalBackdropWindow : Form` rather than reusing anchored overlay hosts.

Backdrop window defaults:

```text
FormBorderStyle = None
ShowInTaskbar = false
ControlBox = false
MinimizeBox = false
MaximizeBox = false
StartPosition = Manual
TopMost = false
```

Windowing rules:

- Resolve the real owner from the `IWin32Window` passed to `ShowDialog(owner)` / inherited owner relationship without creating a parallel public owner API.
- Show the backdrop before the modal becomes visible and guarantee the modal is above it.
- Preserve unrelated application z-order. Alt+Tab away from the application must allow another app to appear above both modal and backdrop.
- If the native owned-window relationship proves unable to guarantee clickable backdrop + correct z-order while `ShowDialog(owner)` disables the owner, stop before shipping and revise the **internal** backdrop-host strategy. Do not replace native modality to save the first backdrop design.
- Subscribe only to owner events actually needed for geometry/lifecycle (`LocationChanged`, `SizeChanged`, `VisibleChanged`, `FormClosed` where the owner is a `Form`) and unsubscribe deterministically.
- Clamp the dialog rectangle to `Screen.WorkingArea` for the target owner/monitor.
- Default position is centered relative to the owner usable rectangle; without owner, center in target screen working area.
- Do not continuously recenter the dialog on every owner move after the user has started interacting unless the chosen window ownership behavior requires it. The backdrop must track; dialog movement policy should remain stable and tested.
- Monitor/DPI transitions must recompute constraints without sending the dialog off-screen.

Create pure/internal helpers such as:

```csharp
internal static class BootstrapModalLayoutLogic
{
    internal static int ResolvePreferredWidth(...);
    internal static Size ResolveDialogSize(...);
    internal static Rectangle CenterAndClamp(...);
    internal static Rectangle ResolveBackdropBounds(...);
}
```

---

## Focus, Keyboard, and Dismissal Contract

Native dialog behavior has priority over Bootstrap-like decoration.

### Initial focus

On first shown/activated state:

1. If `InitialFocusControl` is a visible, enabled, selectable descendant, focus it.
2. Otherwise preserve a valid caller/native `ActiveControl`.
3. Otherwise select the first selectable descendant using native container navigation.
4. The close button must not steal initial focus merely because it is in the header.

### Tab navigation

- Tab/Shift+Tab use native WinForms dialog navigation.
- Focus must not land on the non-interactive backdrop.
- The framework does not implement a custom focus-trap engine unless baseline tests prove native modal navigation can escape the modal (which should not happen in normal `ShowDialog` behavior).
- Disabled/hidden controls are skipped by native navigation.

### Enter

- Preserve native `AcceptButton` behavior.
- Do not intercept Enter globally when a child editor or multiline control owns it.
- One Enter key press must never cause two command activations.

### Escape

- If a native `CancelButton` consumes Escape, preserve its behavior/result and do not invoke a second close path.
- Otherwise, when `CloseOnEscape == true`, request close exactly once.
- When `CloseOnEscape == false`, Escape does not close the modal. If `BackdropMode == Static`, an optional `DismissPrevented`-style internal attention cue may run; do not add a public event in V1 unless an application use case appears.
- Escape handling must not swallow unrelated Alt/system-key behavior.

### Close button and backdrop click

- Header close button requests the same close path as a normal modal dismiss action.
- Dismissible backdrop requests that same path.
- Static backdrop consumes only the backdrop click; it does not disable the dialog.
- Every path respects `FormClosing` cancellation.
- Repeated clicks/keys during close transition are coalesced into one close attempt.

---

## Theme, DPI, RTL, and Accessibility Contract

### Theme

- Apply `BootstrapThemeManager.CurrentTheme` at construction/handle creation without requiring application bootstrap.
- Subscribe to `BootstrapThemeManager.ThemeChanged` while alive; refresh surface, border, title, close glyph, separators, footer, backdrop, metrics, and owned framework fonts.
- Theme changes while the modal is open must repaint immediately without closing/reopening or losing focus.
- Reduced-motion changes while open must take effect on the next transition/attention cue and must not strand an active animation.

### DPI

- Resolve all framework dimensions from logical pixels with `DpiScaler`.
- Cover at least 96, 120, 144, 168, and 192 DPI in pure layout tests.
- Handle real `DeviceDpi`/DPI-change lifecycle on supported Windows without accumulating scale error.
- Recompute border radius, border width, header/footer padding, close-button hit target, dialog width preset, body constraints, backdrop bounds, and positioning after DPI changes.
- Do not dispose caller fonts or resize caller content destructively during DPI transitions.

### RTL

- Respect inherited `RightToLeft`/`RightToLeftLayout` behavior where applicable.
- Header close button moves to the visual leading/trailing position consistent with Windows/Bootstrap expectations after verification in the demo.
- Footer action flow reverses appropriately without callers manually reordering controls.
- Text alignment and focus/tab order remain logical and keyboard-usable.

### Accessibility

- Modal exposes dialog/window semantics through native Form accessibility; set an appropriate `AccessibleRole` only if native defaults are insufficient and tests prove the change is safe.
- Accessible name should fall back to inherited `Text` when caller has not assigned one.
- Close button has an accessible name such as `Close` and is keyboard reachable only when doing so does not disturb normal body/action tab order.
- Backdrop is not exposed as a meaningful interactive task/window to accessibility users.
- Focus is moved into the modal on show and returns through native modal-owner behavior on close.

---

## Animation Contract

Reuse shared Animation infrastructure; do not create a dedicated WinForms timer.

V1 transition target:

- Opening: short fade + small vertical translate when normal motion is enabled.
- Closing: short fade/translate only if it can be implemented while preserving cancellable `FormClosing`, native `DialogResult`, and exactly-once close semantics.
- Reduced motion: apply final visual state immediately, no repeated frame scheduling.
- Static-backdrop feedback: optional small shake/attention animation only after core open/close transitions are proven deterministic.

Lifecycle rules:

- Never delay entering native `ShowDialog` behind an asynchronous task that would return control to the caller prematurely.
- Do not mutate public `Opacity`/`Location` permanently after a transition; final state must be stable.
- A close canceled by `FormClosing` must restore the fully visible/open state.
- A second close request while closing is ignored/coalesced.
- Disposal during animation stops scheduler callbacks safely.
- Theme/reduced-motion changes during transition do not cause stale callbacks after close.
- If safe animated close requires intercepting `FormClosing`, use a small explicit internal state machine and tests; do not recursively call `Close()` without a reentrancy guard.

Suggested internal states:

```text
Closed -> Opening -> Open -> Closing -> Closed
                    ^          |
                    |----------|  (FormClosing canceled)
```

No public state enum is required.

---

## Test Infrastructure Contract

The repository already configures WinForms unhandled exceptions to propagate in the test process. Modal tests add a deterministic message-loop harness instead of weakening that fail-fast policy.

Create or extend test infrastructure with a helper conceptually similar to:

```csharp
internal static class ModalTestHost
{
    public static DialogResult ShowAndDrive(
        BootstrapModal modal,
        Form owner,
        Action<BootstrapModal> afterShown,
        TimeSpan timeout);
}
```

Requirements:

- Runs on STA.
- Schedules `afterShown` from `Shown`/`BeginInvoke` before entering `ShowDialog`.
- Captures exceptions from scheduled UI work and rethrows them to NUnit after the modal loop exits.
- Has a watchdog timeout that closes/fails deterministically rather than leaving `dotnet test`/Codex hanging.
- Cleans up owner, modal, backdrop, handlers, and queued callbacks in `finally`.
- Never uses `Thread.Abort`.
- Does not swallow `Application.ThreadException`, `UnhandledException`, or control exceptions.
- Supports tests that intentionally cancel `FormClosing`, then schedule a second deterministic close.

No test may depend on a human clicking the modal.

---

# Task 1: Lock Native Modal Baselines and Public API

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalNativeBehaviorTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/ModalTestHost.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/ModalTestHostTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalSize.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`

- [ ] Add STA native `Form.ShowDialog(owner)` baseline tests before framework behavior is implemented: owner disabled while open, owner re-enabled after close, focus remains in dialog, `AcceptButton`, `CancelButton`, programmatic `Close()`, `DialogResult`, and `FormClosing` cancellation.
- [ ] Add a baseline experiment for a separate owned backdrop `Form` shown before `ShowDialog(owner)`: prove whether it remains clickable while the owner is disabled, remains below the modal, and does not activate globally.
- [ ] Treat the backdrop baseline as an architecture gate. If WinForms ownership semantics disprove the proposed dedicated backdrop strategy, revise the internal host before adding public behavior; do **not** replace native modality.
- [ ] Implement `ModalTestHost` with deterministic `Shown`/`BeginInvoke` driving, exception propagation, cleanup, and watchdog timeout.
- [ ] Add self-tests proving `ModalTestHost` completes a scheduled close, propagates a scheduled exception, handles a canceled first close followed by a second close, and fails boundedly instead of hanging.
- [ ] Add enum contracts for `BootstrapModalSize` and `BootstrapModalBackdropMode`.
- [ ] Add the minimal `BootstrapModal : Form` shell with the V1 public properties and constructor defaults, but no final rendering/windowing yet.
- [ ] Add reflection/default-value tests for the public contract and inherited property preservation.
- [ ] Verify direct `Show()` is either explicitly guarded/unsupported or harmlessly documented; do not accidentally create a second modeless contract.
- [ ] Run focused tests on `net8.0-windows` and `net48`.
- [ ] Commit, e.g. `feat: establish BootstrapModal native contract`.

---

# Task 2: Implement Pure Layout, Sizing, and Visual-State Logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalLayoutLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalLayoutLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalRenderLogicTests.cs`

- [ ] Write failing tests for Small/Default/Large/ExtraLarge logical width resolution.
- [ ] Test DPI scaling at 96/120/144/168/192 DPI.
- [ ] Test owner-centered placement and final clamping to working area on all four edges.
- [ ] Test dialogs wider/taller than the available working area; resolved chrome must remain reachable and body must become the constrained region.
- [ ] Test owner rectangles partially off-screen and owner rectangles on secondary monitors.
- [ ] Test no-owner placement using an explicit target working area input rather than consulting global `Screen` state inside pure logic.
- [ ] Define and test header/body/footer metrics, border thickness, close target size, content padding, footer gap, and radius resolution from theme metrics.
- [ ] Define/test semantic visual state for light/dark theme surfaces, border, text, muted text, and backdrop color/opacity.
- [ ] Keep helpers independent from real HWND creation and `Screen` calls.
- [ ] Avoid allocations in frequently repeated geometry resolution where simple structs suffice.
- [ ] Run focused pure tests on both TFMs.
- [ ] Commit, e.g. `feat: add BootstrapModal layout logic`.

---

# Task 3: Build the Dialog Shell and Native Command Composition

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalSurface.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalHeader.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalTests.cs`

- [ ] Compose the modal shell from framework-owned header, scrollable body host, and footer host while keeping the top-level object a real `Form`.
- [ ] Expose read-only `BodyPanel` and `FooterPanel` according to the public contract.
- [ ] Render inherited `Text` in the header and keep it synchronized when changed at runtime.
- [ ] Implement the close affordance without overwriting caller `AcceptButton`/`CancelButton`.
- [ ] Make footer visibility collapse when it contains no visible controls and recompute preferred size when actions are added/removed/shown/hidden.
- [ ] Preserve caller controls, events, `DialogResult`, validation, `CausesValidation`, and normal button behavior.
- [ ] Verify buttons with native `DialogResult.OK`, `Cancel`, etc. close the modal exactly once and return the native result.
- [ ] Verify a consumer-canceled `FormClosing` keeps the modal open and does not corrupt visual/lifecycle state.
- [ ] Apply radius/border clipping safely and dispose framework-owned `Region`/GDI resources deterministically.
- [ ] Do not introduce layered windows/DWM effects merely for shadow.
- [ ] Add constructor/designer-safety tests: creating `BootstrapModal` must not require `Application.Run` or application bootstrap.
- [ ] Run focused tests on both TFMs.
- [ ] Commit, e.g. `feat: implement BootstrapModal dialog shell`.

---

# Task 4: Implement Backdrop, Owner Tracking, and Z-Order Lifecycle

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropWindow.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalOwnerTracker.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalBackdropTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalOwnerLifecycleTests.cs`

- [ ] Implement the dedicated borderless, taskbar-hidden, non-global-topmost backdrop window using the Task 1 ownership strategy proven by baseline tests.
- [ ] Implement `BackdropMode.None` with zero backdrop-window creation.
- [ ] Implement `Dismissible` backdrop click as one close request.
- [ ] Implement `Static` backdrop click as no close request.
- [ ] Guarantee modal z-order above backdrop and backdrop above owner while the application is active.
- [ ] Verify Alt+Tab/application deactivation allows unrelated applications to cover modal/backdrop; no `TopMost` leakage.
- [ ] Track owner move/resize and update backdrop geometry without activating it.
- [ ] Handle owner hide/close/dispose while modal is active; backdrop is hidden/disposed exactly once.
- [ ] Handle modal close/dispose/exceptions during show; backdrop is cleaned in `finally`-equivalent lifecycle paths.
- [ ] Verify repeated show/close cycles do not accumulate hidden backdrop forms in `Application.OpenForms`.
- [ ] Verify `BackdropMode` changes before show are respected; decide and test whether changes while open apply immediately or are deferred to the next show. Prefer immediate only if lifecycle remains simple/deterministic.
- [ ] Add handle recreation/lifecycle tests where practical.
- [ ] Run focused modal integration tests with watchdog timeouts on both TFMs.
- [ ] Commit, e.g. `feat: add BootstrapModal backdrop lifecycle`.

---

# Task 5: Implement Keyboard, Focus, and Dismissal Semantics

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalFocusLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalKeyboardTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalFocusTests.cs`

- [ ] Write failing tests for initial focus priority: valid `InitialFocusControl` -> valid `ActiveControl` -> first selectable child.
- [ ] Test invalid/disposed/non-descendant `InitialFocusControl` falls back safely.
- [ ] Verify Tab/Shift+Tab stay within the native modal dialog and traverse caller controls in normal `TabIndex` order.
- [ ] Verify the backdrop never receives keyboard focus.
- [ ] Verify Enter activates inherited `AcceptButton` exactly once.
- [ ] Verify Escape with inherited `CancelButton` preserves native cancel/result semantics exactly once.
- [ ] Verify Escape without `CancelButton` closes once when `CloseOnEscape = true`.
- [ ] Verify Escape does not close when `CloseOnEscape = false`.
- [ ] Verify repeated Escape/close-button/backdrop clicks during an in-progress close cannot produce duplicate close callbacks.
- [ ] Verify multiline editors and controls with their own Enter behavior are not broken by modal key interception.
- [ ] Verify Alt and Alt+Tab do not accidentally dismiss the modal or corrupt focus state.
- [ ] Verify focus returns to the owner through native modal behavior after close.
- [ ] Avoid a custom application-wide `IMessageFilter` unless a failing baseline proves local Form key processing insufficient.
- [ ] Run focused keyboard/focus tests on both TFMs.
- [ ] Commit, e.g. `feat: complete BootstrapModal keyboard and focus behavior`.

---

# Task 6: Complete Theme, DPI, RTL, and Accessibility Support

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalSurface.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalHeader.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropWindow.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalThemeDpiTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalAccessibilityTests.cs`

- [ ] Subscribe/unsubscribe to runtime theme changes deterministically.
- [ ] Test Light/Dark changes while the modal is already open: shell/backdrop update without losing focus or changing result state.
- [ ] Scale all framework-owned metrics through `DpiScaler`; add pure tests across the project DPI matrix.
- [ ] Test real handle-backed DPI/layout changes where the test environment exposes them safely.
- [ ] Ensure body scrolling activates when DPI/content growth exceeds working-area height and header/footer remain reachable.
- [ ] Test `RightToLeft.Yes` / `RightToLeftLayout` presentation: title, close button, footer actions, and content remain coherent.
- [ ] Verify accessible name fallback from `Text` and close-button accessible name.
- [ ] Verify backdrop does not appear as an application command/task surface for keyboard/accessibility navigation.
- [ ] Verify theme-owned fonts/resources are disposed without touching caller-assigned fonts.
- [ ] Verify disposal after theme changes produces no callbacks to disposed controls.
- [ ] Run focused tests on both TFMs.
- [ ] Commit, e.g. `feat: harden BootstrapModal theme dpi and accessibility`.

---

# Task 7: Add Reduced-Motion-Aware Modal Transitions

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalTransitionController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropWindow.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalTransitionTests.cs`

- [ ] Reuse existing `BootstrapAnimation`/scheduler abstractions; do not create a private Timer.
- [ ] Add deterministic tests using fake animation clock/scheduler where the shared infrastructure permits.
- [ ] Implement opening transition only after the modal/backdrop are in their correct final z-order relationship.
- [ ] Implement reduced-motion open as immediate final state with no repeated scheduling.
- [ ] Implement close transition with an explicit reentrancy state machine only if `FormClosing` cancellation and native `DialogResult` remain correct.
- [ ] Test canceled `FormClosing` during animated close restores stable visible state.
- [ ] Test repeated close requests while closing coalesce.
- [ ] Test dispose/owner-close during transition stops callbacks and releases backdrop.
- [ ] Test runtime reduced-motion change does not leave a partially transparent/offset modal.
- [ ] Add optional static-backdrop attention feedback only after all lifecycle tests are green; reduced motion must suppress movement.
- [ ] If fade via top-level `Form.Opacity` introduces target-framework/window-style instability, prefer a simpler framework-owned content transition rather than platform-specific hacks.
- [ ] Run focused transition tests on both TFMs.
- [ ] Commit, e.g. `feat: animate BootstrapModal lifecycle`.

---

# Task 8: Harden Exceptional, Repeated, and Unsupported Lifecycles

**Files:**
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal*.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Hardening/BootstrapModalHardeningTests.cs`

- [ ] Test open/close/open on the same modal instance where native Form lifecycle permits it; if disposed dialogs cannot be reused, preserve/document native behavior rather than emulating reuse.
- [ ] Test close from `Shown`, `BeginInvoke`, button click, backdrop click, Escape, owner close, and programmatic `DialogResult` assignment.
- [ ] Test a `FormClosing` handler that cancels the first request and allows the next.
- [ ] Test disposal before first show, while shown, during transition, and after close.
- [ ] Test exceptions from caller `Shown`/button handlers propagate through the configured test environment rather than opening a WinForms error dialog or hanging.
- [ ] Test no backdrop/window leak after exceptional close paths.
- [ ] Test no stale theme/owner/animation callbacks after disposal.
- [ ] Test owner on another monitor and owner bounds changing near screen edges.
- [ ] Test very small working areas, oversized preferred content, empty body, empty footer, long title, and dynamic body/footer changes while open.
- [ ] Add a regression test ensuring `ShowInTaskbar` remains false and modal/backdrop do not become unintended app windows.
- [ ] Add a regression test documenting nested `BootstrapModal` as unsupported; do not introduce a global stack manager merely to make the test pass.
- [ ] Run the complete modal test subset multiple times to detect intermittent z-order/message-loop hangs.
- [ ] Commit, e.g. `test: harden BootstrapModal lifecycle`.

---

# Task 9: Add Integrated Demo and Manual Verification Matrix

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ModalDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create or modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/ModalDemoTests.cs`

- [ ] Add a **Modal** page to integrated demo navigation.
- [ ] Demonstrate Default, Small, Large, and ExtraLarge sizes.
- [ ] Demonstrate dismissible, static, and no-backdrop modes.
- [ ] Demonstrate `AcceptButton`/`CancelButton` and show returned `DialogResult` visibly in the demo.
- [ ] Demonstrate `CloseOnEscape = false`.
- [ ] Demonstrate long scrollable body content with fixed reachable footer.
- [ ] Demonstrate `InitialFocusControl`, Tab/Shift+Tab, Enter, Escape, Alt, and Alt+Tab behavior.
- [ ] Demonstrate runtime Light/Dark and Reduced motion using the existing global demo controls.
- [ ] Demonstrate dynamic content/footer changes while open.
- [ ] Add a diagnostic scenario near monitor edge / constrained owner size to inspect clamping.
- [ ] Add RTL scenario.
- [ ] Manual-test on supported Windows scaling: 100%, 125%, 150%, 175%, 200%.
- [ ] Manual-test owner move/resize, minimize/restore where applicable, Alt+Tab to another application, and closing the owner.
- [ ] Confirm no hidden backdrop remains in task switcher/taskbar after closing every demo scenario.
- [ ] Commit, e.g. `demo: add BootstrapModal scenarios`.

---

# Task 10: Document Contract and Run Final Verification

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `README.md` if the public control catalog is listed there
- Modify: `CHANGELOG.md` when implementation is release-ready
- Modify: `docs/TESTING.md` if ModalTestHost becomes shared test infrastructure

- [ ] Add the finalized `BootstrapModal` responsibility/public contract to `docs/COMPONENTS.md` using the exact shipped names.
- [ ] Document that native `Form`, `ShowDialog`, `DialogResult`, `AcceptButton`, `CancelButton`, and `FormClosing` remain authoritative.
- [ ] Document V1 backdrop modes, size presets, focus behavior, reduced motion, DPI, and RTL behavior.
- [ ] Document V1 exclusions: nested Bootstrap modals, fullscreen, draggable/resizable custom chrome, DWM effects, MDI-special behavior, and modeless contract.
- [ ] If `ModalTestHost` is generally useful, document the mandatory deterministic-close + timeout rule in `docs/TESTING.md` and reuse it for future dialog tests.
- [ ] Run formatting/style checks required by the repository.
- [ ] Run `dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release`.
- [ ] Run the full test project for `net8.0-windows` with the repository hang-timeout settings.
- [ ] Run the full test project for `net48` with the repository hang-timeout settings.
- [ ] Verify no test-runner child process or modal dialog remains after the suite.
- [ ] Run the integrated demo and complete the manual verification matrix.
- [ ] Review the final public API for duplicate/native aliases and remove any API not justified by tests/demo scenarios.
- [ ] Commit, e.g. `docs: document BootstrapModal contract`.

---

## Required Acceptance Matrix

Implementation is complete only when these behaviors are evidenced:

| Area | Required evidence |
|---|---|
| Native modality | owner disabled only through native `ShowDialog`; restored after close |
| Result semantics | native `DialogResult`, AcceptButton, CancelButton preserved |
| Close cancellation | consumer `FormClosing` cancellation remains authoritative |
| Backdrop none | no backdrop window created |
| Backdrop dismissible | backdrop click requests one close |
| Backdrop static | backdrop click does not close |
| Escape enabled | closes once when no native cancel action owns Escape |
| Escape disabled | remains open |
| Focus | initial focus resolved safely; Tab/Shift+Tab stay in modal |
| Alt/Alt+Tab | no accidental dismissal/topmost leakage |
| Sizes | Small/Default/Large/ExtraLarge resolve and clamp correctly |
| Long content | body scrolls while title/footer remain reachable |
| Theme | live Light/Dark switch updates modal + backdrop |
| Reduced motion | no continuous transition scheduling |
| DPI | 96/120/144/168/192 pure geometry + real supported checks |
| RTL | header/footer/body presentation remains usable |
| Owner lifecycle | move/resize/close/dispose leaves no orphan backdrop |
| Reentrancy | repeated close inputs do not duplicate lifecycle events |
| Exceptions | fail test deterministically; never open an unattended error dialog |
| Disposal | no stale theme/owner/animation subscriptions or top-level windows |
| TFMs | build/tests pass on `net48` and `net8.0-windows` |

---

## Explicit V1 Non-Goals

Do **not** expand implementation scope to include:

- Nested BootstrapModal orchestration or a global modal stack.
- Fullscreen responsive modal variants.
- Modeless BootstrapModal contract.
- Draggable/resizable custom title bar.
- Minimize/maximize/custom system-menu emulation.
- Acrylic/Mica/blur/DWM-specific styling.
- Arbitrary custom header commands or custom title-bar layout framework.
- A new application-wide focus manager or message filter.
- A new animation scheduler/timer.
- A new generic overlay/popup infrastructure.
- Business confirmation APIs such as `ConfirmAsync(...)` or message-box replacements.
- Async modal result APIs (`Task<DialogResult>`) until a separate design proves they compose correctly with both target frameworks and WinForms message-loop semantics.

These can be planned separately after the native V1 contract is stable.

---

## Final Implementation Principle

`BootstrapModal` should look and feel like Bootstrap, but it must behave like a correct WinForms modal dialog.

The priority order is:

```text
native modal correctness
    > deterministic lifecycle / no hangs
    > owner + focus + keyboard correctness
    > theme / DPI / accessibility
    > backdrop and sizing fidelity
    > animation polish
```

If a visual feature requires replacing native modality, introducing global input hooks, or weakening unattended test determinism, omit that feature from V1 rather than compromising the dialog contract.
