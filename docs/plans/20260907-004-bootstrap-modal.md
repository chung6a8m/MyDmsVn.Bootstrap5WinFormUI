# BootstrapModal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapModal` that preserves native WinForms modal-dialog behavior (`ShowDialog`, owner disabling, `DialogResult`, `AcceptButton`, `CancelButton`, focus, keyboard routing, `FormClosing`/`FormClosed`, accessibility, and message-loop semantics) while adding Bootstrap-inspired dialog chrome, theme-aware rendering, owner-aware backdrop behavior, size presets, DPI-aware positioning, and reduced-motion-aware transitions.

**Architecture:** `BootstrapModal` derives directly from `System.Windows.Forms.Form`. The native `Form` remains the behavioral/modal-loop source of truth. The framework owns only Bootstrap presentation, the dialog layout shell, a dedicated internal backdrop window, owner/monitor geometry, framework-originated dismissal policy, theme/DPI synchronization, and transition state. The backdrop is presentation infrastructure, not a second modality engine: native `ShowDialog(owner)` still owns modality and owner disabling. Because the WinForms modal loop disables already-visible top-level WinForms windows on the UI thread, the backdrop must not be shown before that disable snapshot; Task 1 must prove a lifecycle point from which the backdrop can be created/shown while remaining enabled and clickable. Owner discovery is also internal and post-native-ownership: the control must not shadow `ShowDialog` merely to capture its argument. Pure/internal helpers resolve layout, colors, size constraints, placement, and lifecycle decisions so correctness can be tested without opening a real modal for every case. GUI integration tests use an STA message-loop harness that deterministically schedules dismissal before calling `ShowDialog`; no unattended test is allowed to leave a modal waiting for human input.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapTheme`, `BootstrapVariant`/theme tokens, `DpiScaler`, shared Rendering primitives, existing Animation infrastructure (`BootstrapAnimation`, `AnimationOwnerLifecycle`, frame scheduler), NUnit, integrated demo application.

---

## Global Constraints

- Preserve repository dependency direction from `AGENTS.md` and `docs/ARCHITECTURE.md`: Modal may depend on Theme/Rendering/Animation/Compatibility, but shared infrastructure must not depend on `BootstrapModal`.
- Keep native `System.Windows.Forms.Form` as the behavioral source of truth. Do **not** build a custom message loop, fake `DialogResult`, application-wide focus manager, or parallel owner-disable system.
- The supported modal path is inherited `ShowDialog()` / `ShowDialog(IWin32Window owner)`. Modeless `Show()` is outside the V1 contract unless implementation work proves it can be supported without weakening modal semantics.
- Do **not** hide/shadow (`new`) the inherited `ShowDialog` overloads just to capture the owner argument. Owner resolution must occur internally after native `Form.ShowDialog` has established the dialog/native owner relationship.
- Preserve inherited native APIs and events, including at minimum: `Text`, `Icon`, `AcceptButton`, `CancelButton`, `DialogResult`, `Owner`, `ActiveControl`, `ShowDialog`, `Close`, `Load`, `Shown`, `FormClosing`, `FormClosed`, `Activated`, `Deactivate`, `Size`, `ClientSize`, `MinimumSize`, `MaximumSize`, and standard WinForms focus/validation behavior.
- Framework-originated **dismissal** is not the same contract as caller-invoked inherited `Close()`. Header close, dismissible backdrop, and Escape-without-native-cancel request native-equivalent modal dismissal; inherited `Close()` remains untouched and must not be rerouted through a framework dismiss state machine.
- Constructor defaults may set framework-required inherited properties such as `FormBorderStyle = None`, `ShowInTaskbar = false`, `StartPosition = Manual`, `MinimizeBox = false`, and `MaximizeBox = false`. V1 must not shadow inherited properties merely to pretend callers cannot change them. Mutating framework-required window-style properties is unsupported when it invalidates Bootstrap rendering assumptions.
- Do not implement a second generic popup/overlay system. Reuse low-level shared helpers where appropriate, but do not force `BootstrapModal` through `BootstrapOverlayDropDown` or `BootstrapOverlaySurface`; those abstractions serve anchored overlays, not modal top-level windows.
- Backdrop lifecycle must never become the modality mechanism. If backdrop creation/showing fails, the modal must fail predictably or continue without leaking/stranding a top-level window; it must never leave the owner permanently disabled.
- A visible same-thread WinForms backdrop created **before** native modal entry is expected to be included in WinForms' modal-disable snapshot. Do not ship a strategy that depends on such a pre-existing backdrop remaining clickable. Task 1 must prove the post-snapshot timing/ownership strategy on both target frameworks.
- V1 supports one active `BootstrapModal` in a normal owner interaction flow. Nested Bootstrap modals are **not a supported contract**, matching Bootstrap guidance. Do not add a global modal stack/coordinator merely to police every possible native `Form.ShowDialog` nesting scenario.
- Do not add fullscreen modal variants in V1. Small/Default/Large/ExtraLarge desktop presets plus `Custom` sizing are sufficient. Fullscreen can be considered later if concrete application scenarios require it.
- Do not add draggable/resizable custom chrome, maximize/minimize, arbitrary title-bar commands, MDI integration, taskbar ownership, acrylic/Mica, layered-window blur, per-pixel alpha, or platform-specific DWM effects in V1.
- Do not hardcode Bootstrap hex values. Resolve framework-owned colors from the current theme.
- All framework-owned geometry is expressed in logical pixels and scaled through `DpiScaler`. Do not use APIs unavailable on `net48` such as `Math.Clamp` without a compatibility abstraction.
- Theme subscriptions, owner subscriptions, backdrop events, animation callbacks, timers/schedulers, and native handles must be detached/released deterministically during close/disposal.
- Do not dispose caller-owned controls, `AcceptButton`, `CancelButton`, icons, images, fonts explicitly assigned by the caller, or the owner form.
- Tests that instantiate/show WinForms windows run STA and non-parallel where they mutate global theme state.
- Every test that enters a modal message loop must arrange its own deterministic close/dismiss action **before** `ShowDialog` begins and must have a bounded hang timeout. No automated test may call an unattended `ShowDialog()` and wait for input.
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
- `Form.Close`: https://learn.microsoft.com/dotnet/api/system.windows.forms.form.close
- `Screen.WorkingArea`: https://learn.microsoft.com/dotnet/api/system.windows.forms.screen.workingarea

Map Bootstrap concepts as follows:

| Bootstrap concept | WinForms adaptation |
|---|---|
| `.modal` / modal JS | native `BootstrapModal : Form` + inherited `ShowDialog` |
| modal title | inherited `Text` rendered in framework header |
| modal body | `BodyPanel` |
| modal footer | `FooterPanel`, visible when it has visible content |
| close button | framework-owned header button requesting modal dismissal |
| `backdrop: true` | `BackdropMode = Dismissible` |
| `backdrop: 'static'` | `BackdropMode = Static` |
| `backdrop: false` | `BackdropMode = None` |
| `keyboard: true/false` | `CloseOnEscape` |
| focus on shown modal | native modal activation + optional `InitialFocusControl` |
| scrolling long content | body-level AutoScroll under constrained working-area height |
| optional sizes | `BootstrapModalSize.Small/Default/Large/ExtraLarge`; `Custom` is the WinForms sizing opt-out |
| fade transition | shared Animation infrastructure; reduced motion resolves immediately |
| one modal at a time | nested Bootstrap modals are unsupported in V1 |

Important adaptation rules:

1. Inherited `ShowDialog(owner)` remains responsible for disabling/re-enabling the owner and running the nested modal message loop. The library must not reproduce this with `Enabled = false` bookkeeping.
2. `AcceptButton` and `CancelButton` stay native. Enter/Escape behavior already handled by native dialog buttons must not cause duplicate clicks or duplicate dismissal requests.
3. `FormClosing` cancellation stays authoritative for both native close paths and framework-originated dismissal.
4. Header close, dismissible backdrop, and Escape-without-native-`CancelButton` converge on one **framework dismiss request**, not on caller `Close()`. The dismiss request must behave like a normal user dismiss of a modal dialog: by default it returns `DialogResult.Cancel`, raises the normal close-validation lifecycle exactly once, and remains open if `FormClosing` is canceled. Task 1 must baseline the native-equivalent mechanism before the framework implementation is selected.
5. Caller-invoked inherited `Close()` retains native `Form.Close()` behavior. Do not override, hide, delay, or translate it to framework dismissal merely for animation consistency.
6. `DialogResult` is not replaced with a custom result enum. Callers use native `System.Windows.Forms.DialogResult`.
7. The framework may provide a default close button, but it must not silently overwrite a caller-assigned `CancelButton` or `AcceptButton`.
8. A long modal must remain inside the target monitor working area. Body scrolling is preferred over placing title/footer off-screen.
9. The backdrop is shown above the owner and below the dialog. It is never `TopMost` globally by default and must not float above unrelated applications.
10. Alt+Tab / application deactivation must not leave the backdrop or modal incorrectly above another application. Ownership/z-order tests are required because this project has already encountered top-level popup issues on app deactivation.
11. Closing or disposing the owner while the modal is active must not leave a backdrop window behind.
12. V1 does not promise nested Bootstrap modal behavior. If a consumer opens another native dialog from a `BootstrapModal`, normal WinForms behavior applies to that native dialog, but the framework does not introduce a modal stack abstraction.
13. Do not infer whether caller `Size`/`ClientSize` was "explicit" by observing inherited size-change events. The public sizing mode decides authority deterministically: preset modes own framework width/content sizing; `Custom` delegates requested size to inherited WinForms sizing subject only to safety clamping.

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
    ExtraLarge,
    Custom
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
- Inherited `AcceptButton`, `CancelButton`, `DialogResult`, `Close()`, `Size`, `ClientSize`, `MinimumSize`, and `MaximumSize` remain real native APIs. Do not add aliases for them.
- In preset modes (`Small`, `Default`, `Large`, `ExtraLarge`), `ModalSize` is authoritative for the framework-resolved preferred width. The shell computes content-driven height and may replace caller-assigned `Size.Width`/`ClientSize.Width` when preparing each modal show or reacting to a DPI change. Callers that require native width/height ownership must use `Custom`; do not silently infer custom mode from a `Size` assignment.
- In `Custom` mode, inherited `Size`/`ClientSize` is the caller's requested dialog size. Framework code may clamp the final rectangle so title/footer remain reachable within the target working area, and may constrain/scroll the body rather than allowing chrome off-screen, but it must not reapply a preset width during theme/content/DPI refresh.
- `MinimumSize`/`MaximumSize` participate in both preset and `Custom` resolution, but safety clamping to the usable working area wins when caller constraints are physically impossible on the target monitor.
- Changing `ModalSize` while the modal is already visible applies on the next layout pass only if doing so can remain deterministic and non-destructive; Task 2 must choose/test one exact behavior. Prefer immediate framework-only reflow for preset-to-preset changes and preserve `Custom` requested size without inventing hidden size history.
- Do not expose the internal backdrop window publicly.
- Do not expose internal transition/lifecycle state publicly.
- Do not expose a second public owner property or a framework-specific `ShowDialog` wrapper.

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
- `Dismissible`: click on backdrop requests framework modal dismissal.
- `Static`: click on backdrop does not dismiss the modal. It may trigger a short attention cue only after the core lifecycle is correct; the cue must respect reduced motion.
- Backdrop must not activate as the primary application window, appear in the taskbar, or become globally topmost.
- Backdrop covers the resolved owner bounds when a usable owner exists. If no usable owner can be resolved, cover the target monitor working area.
- Backdrop geometry tracks a managed owner `Form` move/resize while the modal is visible without stealing focus. For non-`Form` native owners, use current HWND geometry when required rather than inventing managed event subscriptions.
- The backdrop must be created/shown only at the Task-1-proven **post-modal-disable-snapshot** lifecycle point. A same-thread backdrop visible before native modal entry is not a supported strategy.

### Size presets

Use logical-width presets as implementation constants resolved by pure layout logic. Initial target values may mirror Bootstrap-like desktop proportions but must be validated in the demo before API freeze:

```text
Small      ≈ 300 logical px
Default    ≈ 500 logical px
Large      ≈ 800 logical px
ExtraLarge ≈ 1140 logical px
Custom     = caller/native Size or ClientSize request
```

Rules:

- Presets are **preferred dialog widths**, not a license to extend past the monitor working area.
- Apply DPI scaling before final monitor clamping.
- Preset modes deliberately own preferred width; callers use `Custom` when inherited `Size`/`ClientSize` must own width.
- `Custom` does not mean "never clamp": reachable title/footer and usable working-area safety remain mandatory.
- Respect caller `MinimumSize`/`MaximumSize` when physically satisfiable; document and test the safety-clamp result when constraints conflict with the working area.
- Preset-mode height is content-driven up to the available working-area budget. When content exceeds the budget, constrain the body and enable body scrolling; title and footer remain reachable.
- In `Custom`, preserve the requested height when possible, but constrain the final shell/body when the requested rectangle cannot fit the target working area.
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

### Native owner resolution

`Form.ShowDialog(IWin32Window owner)` is inherited and is not a framework interception point. Do not `new`/shadow it. Resolve an internal owner context only after native dialog ownership has been established:

1. When inherited `Owner` is a usable managed `Form`, use it for managed lifecycle subscriptions and geometry.
2. Otherwise, once the modal HWND exists in the modal-show lifecycle, query the modal's current native owner HWND through a narrow `Compatibility` helper (for example `GetWindow(..., GW_OWNER)` or equivalent supported native owner query).
3. For parameterless `ShowDialog()`, allow native WinForms to select the active owner; then resolve that established native owner the same way rather than guessing `Form.ActiveForm` beforehand.
4. For an arbitrary caller-provided `IWin32Window` that is not a `Form`, keep the native HWND as the owner identity/geometry source. Do not require a managed `Form` wrapper.
5. Do not persist an owner HWND beyond the active modal lifetime. Revalidate before native operations; invalid handles during teardown are no-ops/cleanup signals.
6. Do not use private-field reflection into `Form` to read its internal dialog-owner property.

A small internal model may be introduced, for example:

```csharp
internal readonly struct BootstrapModalOwnerContext
{
    internal Form? ManagedOwner { get; }
    internal IntPtr OwnerHandle { get; }
    internal Rectangle OwnerBounds { get; }
}
```

The exact shape is internal and may differ, but it must support managed `Form`, arbitrary `IWin32Window`, and parameterless `ShowDialog()` without changing the public API.

### Backdrop timing architecture gate

WinForms modal entry disables visible/enabled top-level WinForms windows that already exist on the current UI thread. Therefore:

- A backdrop shown before `ShowDialog(owner)` is a **negative baseline**, not the intended architecture: tests should demonstrate that it can be captured by the modal-disable snapshot and become non-clickable.
- Register any `Shown`/handle/lifecycle callback needed **before** entering `ShowDialog`, but create/show the actual backdrop only from a callback/queued UI action that Task 1 proves runs after the native disable snapshot.
- The expected candidate is a modal lifecycle point such as `Shown`/`BeginInvoke`, but do not hardcode that assumption into production until baseline tests prove ordering on both `net48` and `net8.0-windows`.
- The post-snapshot backdrop must remain enabled/clickable while the owner remains disabled by native modality.
- If no native-compatible timing/ownership arrangement can keep `owner < backdrop < modal`, clickable backdrop semantics, Alt+Tab behavior, and cleanup correct, stop before shipping and revise the **internal** backdrop strategy. Do not replace native modality and do not implement a parallel owner-disable system to rescue the backdrop design.

### Windowing rules

- Resolve the real owner through the internal post-native-ownership mechanism above; do not create a parallel public owner API.
- Create/show the backdrop only at the proven post-modal-disable-snapshot point and guarantee the modal is above it.
- Preserve unrelated application z-order. Alt+Tab away from the application must allow another app to appear above both modal and backdrop.
- Subscribe only to managed-owner events actually needed for geometry/lifecycle (`LocationChanged`, `SizeChanged`, `VisibleChanged`, `FormClosed` where the resolved owner is a `Form`) and unsubscribe deterministically.
- For a non-`Form` native owner, query HWND geometry/liveness only when needed; do not simulate unavailable managed events with a polling timer in V1.
- Clamp the dialog rectangle to `Screen.WorkingArea` for the resolved target owner/monitor.
- Default position is centered relative to the owner usable rectangle; without a usable owner, center in target screen working area.
- Do not continuously recenter the dialog on every owner move after the user has started interacting unless the chosen native ownership behavior requires it. The backdrop must track managed owner moves; dialog movement policy should remain stable and tested.
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

- If a native `CancelButton` consumes Escape, preserve its behavior/result and do not invoke a second framework dismiss path.
- Otherwise, when `CloseOnEscape == true`, request framework modal dismissal exactly once.
- When `CloseOnEscape == false`, Escape does not dismiss the modal. If `BackdropMode == Static`, an optional `DismissPrevented`-style internal attention cue may run; do not add a public event in V1 unless an application use case appears.
- Escape handling must not swallow unrelated Alt/system-key behavior.

### Framework dismiss request

Introduce one internal dismiss path (name may vary, for example `RequestDismiss`) with these semantics:

- Header close, dismissible backdrop, and Escape-without-native-cancel use this path.
- It represents user modal dismissal, not caller `Close()`.
- Unless a native command has already established another result, the native-equivalent result is `DialogResult.Cancel`.
- It must trigger normal validation/`FormClosing` behavior exactly once and respect cancellation.
- If `FormClosing` is canceled, the modal remains visible and the result/lifecycle state is restored to the native open state required for the modal loop to continue.
- Repeated framework dismiss inputs while a framework dismiss transition is active are coalesced.
- Caller `Close()`, owner/application shutdown, Windows shutdown, and unrelated native close reasons are not rewritten to this path.
- Task 1 must first baseline which native mechanism best reproduces user modal dismiss semantics on both TFMs; production code should use the least invasive mechanism supported by those baselines rather than assuming `Close()` is equivalent.

### Close button and backdrop click

- Header close button requests framework modal dismissal.
- Dismissible backdrop requests the same framework modal dismissal.
- Static backdrop consumes only the backdrop click; it does not disable the dialog.
- Every framework dismiss path respects `FormClosing` cancellation.
- Repeated clicks/keys during a framework dismiss transition are coalesced into one attempt.

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
- Recompute border radius, border width, header/footer padding, close-button hit target, preset dialog width, body constraints, backdrop bounds, and positioning after DPI changes.
- In `Custom` size mode, preserve native/caller size authority through DPI lifecycle; do not silently switch back to a preset width.
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
- Focus is moved into the modal on show and returns through native modal-owner behavior on close/dismiss.

---

## Animation Contract

Reuse shared Animation infrastructure; do not create a dedicated WinForms timer.

V1 transition target:

- Opening: short fade + small vertical translate when normal motion is enabled.
- Framework dismissal: short fade/translate only if it can be implemented while preserving cancellable `FormClosing`, native `DialogResult`, and exactly-once dismiss semantics.
- Caller/native `Close()` and shutdown paths are not required to animate and must not be delayed just to reuse the framework dismiss transition.
- Reduced motion: apply final visual state immediately, no repeated frame scheduling.
- Static-backdrop feedback: optional small shake/attention animation only after core open/dismiss transitions are proven deterministic.

Lifecycle rules:

- Never delay entering native `ShowDialog` behind an asynchronous task that would return control to the caller prematurely.
- Do not mutate public `Opacity`/`Location` permanently after a transition; final state must be stable.
- A framework dismiss canceled by `FormClosing` must restore the fully visible/open state and continue the native modal loop.
- A second framework dismiss request while dismissing is ignored/coalesced.
- Caller-invoked `Close()` must remain a native close path; do not hold it pending behind the framework dismiss animation state machine.
- Disposal during animation stops scheduler callbacks safely.
- Theme/reduced-motion changes during transition do not cause stale callbacks after close.
- If safe animated dismissal requires intercepting `FormClosing`, use a small explicit internal state machine and tests; do not recursively call `Close()` without a reentrancy guard, and do not turn all native close reasons into framework dismissal.

Suggested framework-transition states:

```text
Closed -> Opening -> Open -> Dismissing -> Closed
                    ^             |
                    |-------------|  (FormClosing canceled)
```

Native/caller close may leave `Open` through normal Form lifecycle without entering `Dismissing`. No public state enum is required.

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

The helper may add overloads for parameterless `ShowDialog()` and arbitrary `IWin32Window` owner cases needed by owner-resolution tests.

Requirements:

- Runs on STA.
- Registers/schedules deterministic UI work before entering `ShowDialog`, then drives it from `Shown`/`BeginInvoke` or the Task-1-proven modal lifecycle point.
- Captures exceptions from scheduled UI work and rethrows them to NUnit after the modal loop exits.
- Has a watchdog timeout that closes/fails deterministically rather than leaving `dotnet test`/Codex hanging.
- Cleans up owner, modal, backdrop, handlers, and queued callbacks in `finally`.
- Never uses `Thread.Abort`.
- Does not swallow `Application.ThreadException`, `UnhandledException`, or control exceptions.
- Supports tests that intentionally cancel `FormClosing`, then schedule a second deterministic dismiss/close.
- Supports asserting state **during** the native modal loop: owner enabled state, modal/backdrop enabled state, native owner HWND, z-order, and focus.

No test may depend on a human clicking the modal.

---

# Task 1: Lock Native Modal Baselines, Owner/Backdrop Architecture, and Public API

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalNativeBehaviorTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/ModalTestHost.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/ModalTestHostTests.cs`
- Create or extend narrow native helper as required: `src/MyDmsVn.Bootstrap5WinFormUI/Compatibility/NativeWindow*.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalSize.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`

- [ ] Add STA native `Form.ShowDialog(owner)` baseline tests before framework behavior is implemented: owner disabled while open, owner re-enabled after modal exit, focus remains in dialog, `AcceptButton`, `CancelButton`, programmatic `DialogResult`, `FormClosing` cancellation, and normal result return.
- [ ] Add a native baseline distinguishing **user modal dismissal** from caller `Close()`: record `DialogResult`, `FormClosing`/`FormClosed`, visibility/disposal/reuse behavior, and cancellation behavior. Use these results to define the internal framework dismiss mechanism; do not assume `Close()` is equivalent.
- [ ] Add a negative backdrop baseline: show a separate same-thread WinForms backdrop before `ShowDialog(owner)` and prove whether native modal entry disables it. Treat a disabled/non-clickable result as expected evidence that pre-show backdrop is invalid architecture, not as a failure to work around with custom modality.
- [ ] Add candidate post-snapshot backdrop baselines using lifecycle callbacks registered before `ShowDialog` (for example `Shown` and/or `BeginInvoke`): determine the earliest deterministic point at which a new backdrop can be created/shown while owner remains natively disabled, backdrop remains enabled/clickable, modal remains above it, and focus stays in the modal.
- [ ] Run the backdrop timing baseline on both `net48` and `net8.0-windows`; Task 4 must use only a strategy proven on both TFMs.
- [ ] Treat backdrop timing/z-order as an architecture gate. If no tested native strategy provides `owner < backdrop < modal`, clickable backdrop semantics, Alt+Tab correctness, and deterministic cleanup, revise the internal backdrop host before public behavior; do **not** replace native modality.
- [ ] Add owner-resolution baselines for: `ShowDialog(Form owner)`, `ShowDialog(IWin32Window owner)` where owner is not a `Form`, and parameterless `ShowDialog()`. After native ownership is established, verify what inherited `Owner` exposes and verify a narrow native owner-HWND query from the modal handle.
- [ ] Lock the owner strategy: never hide/shadow `ShowDialog`; use managed `Owner` when available and otherwise resolve current native owner HWND after native ownership is established. No private-field reflection.
- [ ] Implement `ModalTestHost` with deterministic modal-loop driving, exception propagation, cleanup, and watchdog timeout.
- [ ] Add self-tests proving `ModalTestHost` completes scheduled modal exit, propagates a scheduled exception, handles a canceled first dismissal followed by a second deterministic exit, supports during-loop assertions, and fails boundedly instead of hanging.
- [ ] Add enum contracts for `BootstrapModalSize` (`Small`, `Default`, `Large`, `ExtraLarge`, `Custom`) and `BootstrapModalBackdropMode`.
- [ ] Add the minimal `BootstrapModal : Form` shell with the V1 public properties and constructor defaults, but no final rendering/windowing yet.
- [ ] Add reflection/default-value tests for the public contract and inherited property preservation, including proving inherited `ShowDialog`, `Close`, `Size`, and `ClientSize` are not hidden by duplicate framework members.
- [ ] Lock sizing precedence tests: preset modes own preferred width; `Custom` preserves caller/native `Size`/`ClientSize` subject to safety clamping; setting `Size` alone does not silently switch `ModalSize` to `Custom`.
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

- [ ] Write failing tests for Small/Default/Large/ExtraLarge logical width resolution and `Custom` requested-size preservation.
- [ ] Test precedence explicitly: preset width beats inherited requested width; `Custom` requested width/height beats preset logic; final safety clamping still wins when the requested rectangle cannot fit.
- [ ] Test `MinimumSize`/`MaximumSize` interactions in both preset and `Custom` modes, including impossible constraints versus a tiny working area.
- [ ] Test DPI scaling at 96/120/144/168/192 DPI. Presets scale from logical constants; `Custom` must not be converted back to a preset during DPI resolution.
- [ ] Test owner-centered placement and final clamping to working area on all four edges.
- [ ] Test dialogs wider/taller than the available working area; resolved chrome must remain reachable and body must become the constrained region.
- [ ] Test owner rectangles partially off-screen and owner rectangles on secondary monitors.
- [ ] Test no-owner placement using an explicit target working area input rather than consulting global `Screen` state inside pure logic.
- [ ] Define and test header/body/footer metrics, border thickness, close target size, content padding, footer gap, and radius resolution from theme metrics.
- [ ] Define/test semantic visual state for light/dark theme surfaces, border, text, muted text, and backdrop color/opacity.
- [ ] Decide and test visible-time `ModalSize` changes: prefer immediate preset-to-preset reflow; switching to `Custom` preserves the current resolved/requested rectangle without inventing hidden historical `Size`; switching from `Custom` to a preset applies that preset on the next layout pass.
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
- [ ] Implement the close affordance as a framework dismiss request without overwriting caller `AcceptButton`/`CancelButton` and without routing through inherited `Close()`.
- [ ] Use the Task-1-proven native-equivalent dismiss mechanism so header close returns native-equivalent `DialogResult.Cancel` by default and respects a canceled `FormClosing`.
- [ ] Make footer visibility collapse when it contains no visible controls and recompute preferred size when actions are added/removed/shown/hidden.
- [ ] Preserve caller controls, events, `DialogResult`, validation, `CausesValidation`, and normal button behavior.
- [ ] Verify buttons with native `DialogResult.OK`, `Cancel`, etc. end the modal exactly once and return the native result.
- [ ] Verify caller `Close()` follows native behavior independently from framework dismiss; do not make header-close implementation redefine `Close()`.
- [ ] Verify a consumer-canceled `FormClosing` keeps the modal open and does not corrupt result/visual/lifecycle state for both native close and framework dismiss baselines where cancellation is applicable.
- [ ] Apply radius/border clipping safely and dispose framework-owned `Region`/GDI resources deterministically.
- [ ] Do not introduce layered windows/DWM effects merely for shadow.
- [ ] Add constructor/designer-safety tests: creating `BootstrapModal` must not require `Application.Run` or application bootstrap.
- [ ] Run focused tests on both TFMs.
- [ ] Commit, e.g. `feat: implement BootstrapModal dialog shell`.

---

# Task 4: Implement Post-Snapshot Backdrop, Owner Tracking, and Z-Order Lifecycle

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalBackdropWindow.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalOwnerTracker.cs`
- Create as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModalOwnerContext.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapModal.cs`
- Modify/create narrow Compatibility owner-HWND helper from Task 1 as needed
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalBackdropTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapModalOwnerLifecycleTests.cs`

- [ ] Implement the dedicated borderless, taskbar-hidden, non-global-topmost backdrop using **only** the Task-1-proven post-modal-disable-snapshot timing and native ownership strategy.
- [ ] Add a regression test that a backdrop is not made visible before the modal-disable snapshot; do not regress to the pre-show disabled-backdrop architecture.
- [ ] Resolve internal owner context after native ownership is established. Prefer managed `Owner` when it is a usable `Form`; otherwise use the current native owner HWND.
- [ ] Verify owner resolution for `Form`, arbitrary `IWin32Window`, and parameterless `ShowDialog()` without hiding/shadowing inherited `ShowDialog`.
- [ ] Implement `BackdropMode.None` with zero backdrop-window creation.
- [ ] Implement `Dismissible` backdrop click as one framework dismiss request, not a caller `Close()` call.
- [ ] Implement `Static` backdrop click as no dismissal request.
- [ ] Guarantee modal z-order above backdrop and backdrop above owner while the application is active.
- [ ] Verify the post-snapshot backdrop stays enabled/clickable while native modality keeps the owner disabled.
- [ ] Verify Alt+Tab/application deactivation allows unrelated applications to cover modal/backdrop; no `TopMost` leakage.
- [ ] Track managed owner move/resize and update backdrop geometry without activating it.
- [ ] For non-`Form` native owners, resolve current HWND bounds when needed without introducing a polling timer or global hook.
- [ ] Handle owner hide/close/dispose while modal is active; backdrop is hidden/disposed exactly once. For native HWND owners that disappear, treat invalid handle as lifecycle cleanup rather than dereferencing stale ownership.
- [ ] Handle modal exit/dispose/exceptions during show; backdrop is cleaned in `finally`-equivalent lifecycle paths.
- [ ] Verify repeated show/dismiss cycles do not accumulate hidden backdrop forms in `Application.OpenForms`.
- [ ] Verify `BackdropMode` changes before show are respected; decide and test whether changes while open apply immediately or are deferred to the next show. Prefer immediate only if lifecycle remains simple/deterministic.
- [ ] Add handle recreation/lifecycle tests where practical; never cache a stale owner HWND across modal lifetimes.
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
- [ ] Verify Escape with inherited `CancelButton` preserves native cancel/result semantics exactly once and does not also enter the framework dismiss path.
- [ ] Verify Escape without `CancelButton` requests framework dismissal once when `CloseOnEscape = true`, returns native-equivalent cancel result by default, and respects `FormClosing` cancellation.
- [ ] Verify Escape does not dismiss when `CloseOnEscape = false`.
- [ ] Verify repeated Escape/header-close/backdrop clicks during an in-progress framework dismissal cannot produce duplicate lifecycle callbacks.
- [ ] Verify caller `Close()` remains independent from Escape/header/backdrop dismissal coalescing.
- [ ] Verify multiline editors and controls with their own Enter behavior are not broken by modal key interception.
- [ ] Verify Alt and Alt+Tab do not accidentally dismiss the modal or corrupt focus state.
- [ ] Verify focus returns to the owner through native modal behavior after modal exit.
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
- [ ] Verify preset widths re-resolve from logical constants on DPI change while `Custom` does not silently revert to a preset.
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
- [ ] Implement opening transition only after modal and the post-snapshot backdrop are in their correct final z-order relationship.
- [ ] Implement reduced-motion open as immediate final state with no repeated scheduling.
- [ ] Implement an animated **framework dismissal** with an explicit reentrancy state machine only if `FormClosing` cancellation and native `DialogResult` remain correct.
- [ ] Do not delay/intercept caller `Close()`, owner/application shutdown, or other native close reasons merely to force them through the framework dismissal animation.
- [ ] Test canceled `FormClosing` during animated framework dismissal restores stable visible state and the modal loop continues.
- [ ] Test repeated framework dismiss requests while dismissing coalesce.
- [ ] Test caller `Close()` during an opening/dismiss transition follows native lifecycle without recursive close/dismiss behavior.
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

- [ ] Test show/dismiss/show on the same modal instance where native modal lifecycle permits it; preserve native reuse behavior rather than emulating a different lifetime.
- [ ] Separately test caller `Close()` behavior so reuse/disposal expectations are not conflated with framework dismiss behavior.
- [ ] Test modal exit from `Shown`, `BeginInvoke`, native button `DialogResult`, header dismiss, backdrop dismiss, Escape, owner close, caller `Close()`, and programmatic `DialogResult` assignment.
- [ ] Test a `FormClosing` handler that cancels the first framework dismiss and allows the next; also retain native baseline coverage for caller `Close()` cancellation behavior.
- [ ] Test disposal before first show, while shown, during transition, and after modal exit.
- [ ] Test exceptions from caller `Shown`/button handlers propagate through the configured test environment rather than opening a WinForms error dialog or hanging.
- [ ] Test no backdrop/window leak after exceptional paths.
- [ ] Test no stale theme/owner/animation callbacks after disposal.
- [ ] Test managed `Form` owner, arbitrary `IWin32Window` owner, parameterless `ShowDialog()`, owner on another monitor, and owner bounds changing near screen edges.
- [ ] Test native owner HWND becoming invalid during teardown without stale-handle access.
- [ ] Test very small working areas, oversized preferred content, `Custom` requested size, impossible min/max constraints, empty body, empty footer, long title, and dynamic body/footer changes while open.
- [ ] Add a regression test ensuring `ShowInTaskbar` remains false and modal/backdrop do not become unintended app windows.
- [ ] Add a regression test documenting nested `BootstrapModal` as unsupported; do not introduce a global stack manager merely to make the test pass.
- [ ] Add a regression test proving inherited `ShowDialog` overloads remain unshadowed and inherited `Close()` semantics remain independent from framework dismiss.
- [ ] Run the complete modal test subset multiple times to detect intermittent z-order/message-loop hangs.
- [ ] Commit, e.g. `test: harden BootstrapModal lifecycle`.

---

# Task 9: Add Integrated Demo and Manual Verification Matrix

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ModalDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create or modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/ModalDemoTests.cs`

- [ ] Add a **Modal** page to integrated demo navigation.
- [ ] Demonstrate Default, Small, Large, ExtraLarge, and `Custom` sizing; make it visible that `Custom` honors caller/native requested size while presets own preferred width.
- [ ] Demonstrate dismissible, static, and no-backdrop modes.
- [ ] Demonstrate `AcceptButton`/`CancelButton` and show returned `DialogResult` visibly in the demo.
- [ ] Demonstrate header close/backdrop/Escape dismissal returning the native-equivalent cancel result, and separately demonstrate caller `Close()` only in a diagnostic scenario so the semantic distinction remains visible.
- [ ] Demonstrate `CloseOnEscape = false`.
- [ ] Demonstrate long scrollable body content with fixed reachable footer.
- [ ] Demonstrate `InitialFocusControl`, Tab/Shift+Tab, Enter, Escape, Alt, and Alt+Tab behavior.
- [ ] Demonstrate runtime Light/Dark and Reduced motion using the existing global demo controls.
- [ ] Demonstrate dynamic content/footer changes while open.
- [ ] Add a diagnostic scenario near monitor edge / constrained owner size to inspect clamping.
- [ ] Add diagnostics for owner discovery when practical: normal managed owner and parameterless show; arbitrary native `IWin32Window` remains primarily automated-test coverage if demo plumbing would add noise.
- [ ] Add RTL scenario.
- [ ] Manual-test on supported Windows scaling: 100%, 125%, 150%, 175%, 200%.
- [ ] Manual-test owner move/resize, minimize/restore where applicable, Alt+Tab to another application, and closing the owner.
- [ ] Confirm backdrop is enabled/clickable while modal is active, owner remains natively disabled, and no hidden backdrop remains in task switcher/taskbar after closing every demo scenario.
- [ ] Commit, e.g. `demo: add BootstrapModal scenarios`.

---

# Task 10: Document Contract and Run Final Verification

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `README.md` if the public control catalog is listed there
- Modify: `CHANGELOG.md` when implementation is release-ready
- Modify: `docs/TESTING.md` if `ModalTestHost` becomes shared test infrastructure

- [ ] Add the finalized `BootstrapModal` responsibility/public contract to `docs/COMPONENTS.md` using the exact shipped names.
- [ ] Document that native `Form`, inherited `ShowDialog`, `DialogResult`, `AcceptButton`, `CancelButton`, `Close()`, and `FormClosing` remain authoritative.
- [ ] Document the distinction between framework modal dismissal (header/backdrop/Escape) and inherited caller `Close()`.
- [ ] Document that owner discovery is internal/post-native-ownership and that the framework does not shadow `ShowDialog` or expose a duplicate owner API.
- [ ] Document V1 backdrop modes and the post-modal-disable-snapshot backdrop architecture; do not describe the backdrop as the mechanism that disables the owner.
- [ ] Document `ModalSize` precedence: preset modes own preferred width; `Custom` delegates requested size to inherited `Size`/`ClientSize` subject to safety clamping.
- [ ] Document focus behavior, reduced motion, DPI, RTL behavior, and supported owner forms.
- [ ] Document V1 exclusions: nested Bootstrap modals, fullscreen, draggable/resizable custom chrome, DWM effects, MDI-special behavior, and modeless contract.
- [ ] If `ModalTestHost` is generally useful, document the mandatory deterministic-action + timeout rule in `docs/TESTING.md` and reuse it for future dialog tests.
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
| Native modality | owner disabled only through native inherited `ShowDialog`; restored after modal exit |
| ShowDialog API | inherited overloads remain unshadowed; no duplicate framework owner/show API |
| Owner resolution | managed `Form`, arbitrary `IWin32Window`, and parameterless `ShowDialog()` resolve the established native owner safely |
| Result semantics | native `DialogResult`, AcceptButton, CancelButton preserved |
| Framework dismiss | header/backdrop/Escape-without-cancel use one native-equivalent dismiss path; default result Cancel; exactly once |
| Caller Close | inherited `Close()` remains native and is not translated to framework dismiss |
| Close cancellation | consumer `FormClosing` cancellation remains authoritative; canceled framework dismiss restores open modal state |
| Backdrop timing | pre-show backdrop is not used; actual backdrop is created/shown only from the proven post-modal-disable-snapshot lifecycle point |
| Backdrop none | no backdrop window created |
| Backdrop dismissible | backdrop remains enabled/clickable and requests one framework dismiss |
| Backdrop static | backdrop click does not dismiss |
| Escape enabled | framework dismisses once when no native cancel action owns Escape |
| Escape disabled | remains open |
| Focus | initial focus resolved safely; Tab/Shift+Tab stay in modal |
| Alt/Alt+Tab | no accidental dismissal/topmost leakage |
| Preset sizes | Small/Default/Large/ExtraLarge own preferred width and clamp correctly |
| Custom size | inherited Size/ClientSize remains requested-size source; no preset silently reapplied |
| Size constraints | min/max honored when possible; impossible constraints resolve to reachable safe geometry |
| Long content | body scrolls while title/footer remain reachable |
| Theme | live Light/Dark switch updates modal + backdrop |
| Reduced motion | no continuous transition scheduling |
| DPI | 96/120/144/168/192 pure geometry + real supported checks; preset/custom authority preserved |
| RTL | header/footer/body presentation remains usable |
| Owner lifecycle | move/resize/close/dispose or invalid native owner leaves no orphan backdrop/stale HWND |
| Reentrancy | repeated framework dismiss inputs do not duplicate lifecycle events |
| Exceptions | fail test deterministically; never open an unattended error dialog |
| Disposal | no stale theme/owner/animation subscriptions or top-level windows |
| TFMs | build/tests pass on `net48` and `net8.0-windows` |

---

## Explicit V1 Non-Goals

Do **not** expand implementation scope to include:

- Nested `BootstrapModal` orchestration or a global modal stack.
- Fullscreen responsive modal variants.
- Modeless `BootstrapModal` contract.
- A hidden/shadowed replacement for inherited `ShowDialog` or a duplicate public owner API.
- Draggable/resizable custom title bar.
- Minimize/maximize/custom system-menu emulation.
- Acrylic/Mica/blur/DWM-specific styling.
- Arbitrary custom header commands or custom title-bar layout framework.
- A new application-wide focus manager or message filter.
- A new animation scheduler/timer.
- A new generic overlay/popup infrastructure.
- A parallel owner-disable/modality system created to make the backdrop clickable.
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
    > dismiss-vs-Close semantic correctness
    > theme / DPI / accessibility
    > backdrop and sizing fidelity
    > animation polish
```

If a visual feature requires replacing native modality, shadowing native `ShowDialog`, changing caller `Close()` semantics, introducing global input hooks, or weakening unattended test determinism, omit that feature from V1 rather than compromising the dialog contract.
