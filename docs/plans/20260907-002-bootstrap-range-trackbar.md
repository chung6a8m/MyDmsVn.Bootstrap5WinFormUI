# BootstrapRange / TrackBar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapRange` that applies Bootstrap 5 range-control visual language to the native WinForms `TrackBar` while preserving the native range/value model, mouse and keyboard behavior, scrolling semantics, orientation, tick configuration, RTL layout, accessibility, handle lifecycle, and inherited events.

**Architecture:** `BootstrapRange` derives directly from `System.Windows.Forms.TrackBar`. The native TrackBar remains the value, hit-testing, thumb movement, mouse capture, keyboard, tick, orientation, RTL, accessibility, and event engine. The framework owns only Bootstrap-aware presentation and minimal transient state required for painting. Because managed `TrackBar` has no `OwnerDraw` API, implementation is gated on verified Win32 `NM_CUSTOMDRAW` support. The preferred path intercepts the TrackBar's reflected custom-draw notification in the subclass, requests item notifications, and paints the native channel, thumb, and tick parts using the rectangles/state supplied by the native control. Do not replace the native TrackBar with a from-scratch slider unless the gate proves the supported native custom-draw path impossible and a separate architectural decision explicitly approves that scope change.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, GDI+/`Graphics`, Win32 TrackBar `NM_CUSTOMDRAW`, NUnit, existing WinForms test infrastructure, integrated demo application.

**Spec:** `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, plus the explicit public/native/visual contracts in this plan.

## Global Constraints

- Namespace remains `MyDmsVn.Bootstrap5WinFormUI`.
- Production library target frameworks remain exactly `net48;net8.0-windows`.
- Public control name is `BootstrapRange`; its native base class is `System.Windows.Forms.TrackBar`.
- Do not add a second `BootstrapTrackBar` alias in V1. `BootstrapRange` is the Bootstrap-facing name; inherited TrackBar terminology remains authoritative for native properties/events.
- Preserve native `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `TickFrequency`, `TickStyle`, `Orientation`, `RightToLeft`, `RightToLeftLayout`, `AutoSize`, `Scroll`, `ValueChanged`, keyboard behavior, mouse capture, and accessibility semantics.
- Do not introduce a custom value model, floating-point range model, snapping model, tooltip model, labels collection, custom tick collection, or replacement accessibility tree in V1.
- Do not synthesize `Scroll`/`ValueChanged`; let the native TrackBar raise them.
- Reuse `BootstrapThemeManager`, theme tokens, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, and existing rendering conventions rather than creating range-local theme infrastructure.
- Do not require FontAwesome.Sharp or any new external package.
- Do not use `Math.Clamp` or APIs unavailable on `net48` unless an existing compatibility abstraction already covers them.
- Do not leak or retain native HDCs. A `Graphics` created from an `NM_CUSTOMDRAW` HDC is framework-owned and must be disposed without releasing the underlying HDC.
- Any unmanaged declarations must be pointer-size correct for x86/x64 and valid on both target frameworks.
- Handle recreation, reparenting, theme switching, DPI changes, orientation changes, RTL changes, and disposal must not alter `Value` or caller range configuration.
- Test code that creates HWNDs must use the repository's WinForms test environment, run on STA, host the TrackBar under a real parent HWND, pump messages only at bounded synchronization points, dispose the host deterministically, and fail without displaying modal UI.
- Any focused raw `dotnet test` command that can execute WinForms/handle-based tests must include `--blame-hang --blame-hang-timeout 5m` (or another explicitly justified bounded timeout). The full suite must run through `./test.ps1`.
- Keep V1 public API intentionally small. Native properties are not renamed merely to sound Bootstrap-like.

---

## Rendering Decision Gate — Mandatory Before Production Code

`TrackBar` has no managed owner-draw event/property, but the underlying Windows common control supports `NM_CUSTOMDRAW`. TrackBar custom draw identifies three native parts through `NMCUSTOMDRAW.dwItemSpec`:

- `TBCD_CHANNEL`
- `TBCD_THUMB`
- `TBCD_TICS`

The notification is sent to the parent as `WM_NOTIFY`; Windows Forms normally reflects child notifications back to the originating control. The implementation must **prove the reflected notification path on both target frameworks** before committing to the production renderer.

The gate probe itself must be structurally valid for a parent-notification control: execute from an NUnit STA fixture, add the derived TrackBar to a real `Form`/host control, create both parent and child handles before forcing paint, process only bounded message-pump synchronization points, and close/dispose the host deterministically. Creating an unparented child handle is not sufficient evidence for accepting or rejecting the reflected-notification architecture.

Primary references:

- `TrackBar`: https://learn.microsoft.com/dotnet/api/system.windows.forms.trackbar
- TrackBar `Orientation`: https://learn.microsoft.com/dotnet/api/system.windows.forms.trackbar.orientation
- TrackBar `RightToLeftLayout`: https://learn.microsoft.com/dotnet/api/system.windows.forms.trackbar.righttoleftlayout
- Win32 TrackBar reference: https://learn.microsoft.com/windows/win32/controls/trackbar-control-reference
- `NM_CUSTOMDRAW (trackbar)`: https://learn.microsoft.com/windows/win32/controls/nm-customdraw-trackbar
- TrackBar custom-draw part values: https://learn.microsoft.com/windows/win32/controls/custom-draw-values

### Gate acceptance

Before Task 3 begins, a focused automated/integration spike must establish all of these:

1. A derived managed `TrackBar`, hosted under a real parent HWND on an STA test thread, can observe the reflected `NM_CUSTOMDRAW` notification from its own `WndProc` on `net48` and `net8.0-windows`.
2. `CDDS_PREPAINT` can return `CDRF_NOTIFYITEMDRAW` and subsequently receive item prepaint callbacks.
3. Item callbacks identify channel, thumb, and ticks using `TBCD_CHANNEL`, `TBCD_THUMB`, and `TBCD_TICS` when those parts are present.
4. The provided `NMCUSTOMDRAW.rc`/HDC are sufficient to custom paint channel and thumb without altering native hit testing or movement.
5. Returning `CDRF_SKIPDEFAULT` only for parts painted by the framework suppresses the native drawing for that part without breaking interaction.
6. Horizontal, vertical, tickless, and ticked controls still behave natively after the custom-draw interception is enabled.
7. `NMCUSTOMDRAW.uItemState` is characterized for the thumb under normal, focused, hot/hover, pressed/dragging, and disabled conditions on both TFMs. The gate must record which state flags are reliable enough to consume in production. Missing or inconsistent hot/pressed state does **not** fail the rendering gate by itself; it means Task 5 must use the smallest possible framework bookkeeping for those states.

### Gate fallback

If the gate fails on either target framework:

- stop implementation work;
- record the exact failing message/TFM/Windows behavior in this plan or a companion decision note;
- do **not** silently switch to a fully custom slider;
- first verify the probe used an STA fixture, a real parent HWND, created parent/child handles, and bounded message processing so an invalid host setup cannot produce a false-negative architectural decision;
- evaluate a narrowly scoped parent `NativeWindow` notification hook only if it can be lifetime-safe across reparenting and handle recreation;
- if that is also unsuitable, create an ADR/proposal for a composite/custom-control implementation with explicit acknowledgement that keyboard, pointer, accessibility, and range semantics would need to be re-owned and re-tested.

---

## Reference Behavior and WinForms Adaptation

Bootstrap's web range control is the visual reference; WinForms TrackBar remains the behavioral reference.

The V1 visual adaptation intentionally does **not** add a progress-filled/selected segment. Bootstrap's standard range styling is represented by a neutral rail plus accent thumb and focus treatment. Avoid inventing a progress semantic that the native TrackBar does not expose as a separate concept.

Native rules to preserve:

1. `Minimum`/`Maximum`/`Value` remain integer TrackBar semantics, including native validation and coercion behavior.
2. For horizontal orientation, increasing value moves the thumb left-to-right in the normal layout; for vertical orientation, increasing value moves bottom-to-top. RTL layout remains native and must not be recreated from framework math.
3. Arrow keys use native line/small-change behavior; PageUp/PageDown/channel interaction use native page/large-change behavior.
4. `TickStyle` and `TickFrequency` remain inherited native properties; do not replace them with Bootstrap-specific aliases.
5. `TickStyle.None` is the closest visual match to a web range, but V1 does **not** change the inherited TrackBar default behind the designer's back. Demo examples may explicitly set `TickStyle.None`.
6. `AutoSize`, `PreferredSize`, orientation sizing, and control handle styles remain native.
7. `RightToLeft` and `RightToLeftLayout` remain native. Painting must anchor to native part rectangles instead of building a separate logical-to-pixel layout engine.
8. Native mouse capture and thumb hit targets remain unchanged even if the framework paints a circular Bootstrap-like thumb inside the native thumb rectangle.
9. Native accessibility/UI Automation/MSAA identity remains the TrackBar identity; no wrapper child elements or replacement accessible object are introduced in V1.
10. Theme or variant changes invalidate presentation only; they must not write `Value` or range-related properties.

---

## Public Contract to Implement

```csharp
public class BootstrapRange : TrackBar
{
    public BootstrapVariant Variant { get; set; } = BootstrapVariant.Primary;
}
```

### Public API rules

- Default `Variant` is `BootstrapVariant.Primary`.
- Changing `Variant` invalidates presentation only.
- The `Variant` setter must validate the incoming enum value before mutating control state, using the same resolver/validation convention as existing controls. An undefined enum value must throw synchronously and leave the previous `Variant` unchanged; it must not be stored and fail later during a paint callback.
- All range/value/tick/orientation/RTL APIs are inherited directly from `TrackBar`.
- Do not shadow `Value`, `Minimum`, `Maximum`, `TickStyle`, `TickFrequency`, `Orientation`, or `RightToLeftLayout` with `new` properties.
- Do not expose implementation details such as `CustomDrawEnabled`, `NativeRenderMode`, `ChannelBounds`, `ThumbBounds`, or raw Win32 handles.
- If the rendering gate requires an internal feature switch for tests, keep it `internal` and do not make it part of the supported API.

### Explicit V1 exclusions

Do **not** add these public APIs in V1:

```text
BootstrapTrackBar alias
Min / Max aliases
Step alias               // SmallChange/LargeChange remain native
decimal/double Value
RangeValue / dual thumbs
ShowValue / ValueText
ValueFormatter
Tooltip / ValueTooltip
TickLabels
Custom tick collection
SnapPoints
ProgressFill / FillToValue
TrackThickness
ThumbSize
ThumbShape
BorderRadius
Custom renderer delegate
Animation duration/easing
```

Rationale:

- V1 should prove a reliable native-backed visual layer before growing styling surface area.
- Dual-thumb ranges are a different interaction/accessibility model and must not be smuggled into a TrackBar subclass.
- Floating-point values should be handled by caller mapping or a future dedicated abstraction, not by weakening native integer semantics.
- Value labels/tooltips are useful but orthogonal composition features and should not complicate the rendering/native-notification foundation.

---

## Visual Contract

### Background and rail

- Paint/control background follows the active theme's surface treatment and existing native-backed control conventions.
- Channel/rail is a thin, rounded, DPI-scaled neutral line using theme border/secondary-surface tokens.
- Rail painting stays inside the native `TBCD_CHANNEL` bounds so native layout and hit testing stay authoritative.
- Do not paint over sibling controls or outside the TrackBar client area.

### Thumb

Resolve the accent with:

```csharp
var accent = BootstrapVariantColorResolver.Resolve(theme.Colors, Variant);
```

- Normal thumb uses the resolved variant color.
- The Bootstrap-like thumb is circular/rounded and centered inside the native `TBCD_THUMB` rectangle.
- Hover treatment may use a subtle theme-derived change only if hover can be detected without altering native hit testing.
- Pressed/dragging treatment uses a stronger theme-derived state while native mouse capture remains authoritative.
- Disabled thumb uses a muted theme treatment with adequate contrast against the rail/background.
- Do not resize the native thumb hit target merely to match the painted circle.

### Focus

- When the TrackBar has keyboard focus, draw a Bootstrap-like focus halo around the painted thumb using DPI-scaled metrics and existing color utilities.
- Focus painting must not change focus itself, consume keyboard messages, or interfere with native `TabStop` behavior.
- Verify focus remains visible in both light and dark themes.

### Ticks

- Respect inherited `TickStyle.None`, `TopLeft`, `BottomRight`, and `Both`.
- If custom tick drawing is enabled, derive physical intermediate tick positions from the native TrackBar (`TBM_GETNUMTICS`/`TBM_GETTICPOS`) and derive endpoint positions from the native channel/thumb travel bounds. Do not recompute tick positions from `Minimum`/`Maximum` with independent layout math.
- Render ticks with subdued theme border/muted colors.
- If a specific OS/common-controls configuration does not expose a safe tick custom-draw path, prefer native ticks over a geometrically incorrect framework reconstruction; document that limitation explicitly.

### Orientation and RTL

- Horizontal and vertical painting use the native channel/thumb rectangles directly.
- Do not assume that increasing value always means increasing client X/Y. Native orientation and `RightToLeftLayout` determine placement.
- Rendering tests must cover `Orientation.Horizontal`, `Orientation.Vertical`, and RTL layout combinations.

### DPI

- Framework-only thicknesses, inset, corner radius, focus halo, and tick stroke metrics are expressed at 96 DPI and scaled through `DpiScaler`/existing DPI primitives.
- Native rectangles are already in device/client pixels; do not scale native `NMCUSTOMDRAW.rc` a second time.
- Recompute framework metrics when DPI changes and invalidate without changing `Value`.

---

## Internal Design

### `BootstrapRangeRenderLogic`

Create a deterministic internal render-logic helper so most style/geometry decisions can be tested without HWNDs. It should accept native part rectangles, DPI, theme colors, variant, orientation, enabled/focused/hot/pressed state and return framework drawing primitives such as:

```text
rail rectangle + radius/color
thumb ellipse/rounded rectangle + color
focus halo rectangle + thickness/color
tick color/thickness
```

Rules:

- no `Control` dependency in pure geometry/color calculations when practical;
- no HDC ownership;
- no static mutable theme state;
- no value-to-pixel engine;
- native part rectangles are authoritative inputs.

### Native custom-draw bridge

Keep Win32 details isolated in an internal helper, for example `BootstrapRangeNativeMethods.cs`:

- `NMCUSTOMDRAW` declaration with pointer-size-correct fields;
- message/stage/result constants used by TrackBar custom draw;
- `TBCD_CHANNEL`, `TBCD_THUMB`, `TBCD_TICS`;
- only the TrackBar messages actually required for reliable tick/thumb diagnostics;
- helper predicates/parsing to keep raw integer constants out of `BootstrapRange` painting logic.

Do not create a general-purpose Win32 interop framework as part of this feature.

### Message handling

Preferred control flow:

```text
native TrackBar paint
    -> WM_NOTIFY to parent
    -> WinForms reflected notify to BootstrapRange
    -> CDDS_PREPAINT
         return CDRF_NOTIFYITEMDRAW
    -> CDDS_ITEMPREPAINT
         TBCD_CHANNEL -> paint rail -> CDRF_SKIPDEFAULT
         TBCD_THUMB   -> paint thumb/focus -> CDRF_SKIPDEFAULT
         TBCD_TICS    -> paint verified tick path -> CDRF_SKIPDEFAULT
         unknown      -> CDRF_DODEFAULT
```

- Call the base `WndProc` for messages the framework does not own.
- Do not suppress unknown custom-draw stages or parts.
- Avoid recursive `Invalidate`/paint loops from inside custom draw.
- Cache only transient native bounds/state needed to invalidate hover/pressed visuals; never cache `Value` as an alternate source of truth.

---

### Task 1: Lock the native contract and prove the custom-draw gate

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeNativeCustomDrawTests.cs`
- Create initially as spike, then keep production declarations in Task 2: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeNativeMethods.cs`
- Reference: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsTestEnvironment.cs`

- [ ] **Step 1: Write a failing, structurally valid HWND probe fixture.** Mark the fixture `[Apartment(ApartmentState.STA)]`; create a real `Form` (or equivalent HWND-owning test host), add the derived TrackBar to it, create both parent and child handles, and record reflected notifications while forcing paint. Use `Application.DoEvents()` only at finite, known synchronization points; close/dispose the host deterministically. Do not use an unparented `TrackBar.Handle` as the rendering-gate probe.
- [ ] **Step 2: Prove `NM_CUSTOMDRAW` reflection** on `net48` and `net8.0-windows`; assert `CDDS_PREPAINT` is observed through the child `WndProc` after the parent notification/reflection path is active.
- [ ] **Step 3: Return `CDRF_NOTIFYITEMDRAW`** and assert item callbacks expose `TBCD_CHANNEL` and `TBCD_THUMB`; assert `TBCD_TICS` when ticks are enabled.
- [ ] **Step 4: Prove safe part suppression** by painting a diagnostic channel/thumb and returning `CDRF_SKIPDEFAULT` only for that part while `Value`, keyboard movement, and mouse movement continue to work.
- [ ] **Step 5: Characterize native item state.** Record `NMCUSTOMDRAW.uItemState` for the thumb under normal, focused, hot/hover, pressed/dragging, and disabled conditions on both TFMs. Assert only states that are demonstrably stable; record missing/inconsistent hot/pressed flags as an implementation constraint rather than treating them as a rendering-gate failure.
- [ ] **Step 6: Cover horizontal/vertical and tickless/ticked configurations.**
- [ ] **Step 7: Record the gate result** in test names/comments and stop the plan if the supported path is not reliable on either TFM after validating the STA/parent-HWND host setup.
- [ ] **Step 8: Run focused tests with bounded hang detection:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter BootstrapRangeNativeCustomDrawTests

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net48 `
  --blame-hang --blame-hang-timeout 5m `
  --filter BootstrapRangeNativeCustomDrawTests
```

- [ ] **Step 9: Commit:** `test: prove native TrackBar custom draw contract`

---

### Task 2: Add deterministic range rendering logic and interop isolation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeRenderLogic.cs`
- Finalize: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeNativeMethods.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeRenderLogicTests.cs`

- [ ] **Step 1: Write failing pure tests** for theme/variant resolution, disabled state, focus halo, horizontal/vertical geometry, small native rectangles, and DPI scaling.
- [ ] **Step 2: Implement minimal render-logic types** that transform native part rectangles + state into drawing primitives.
- [ ] **Step 3: Verify native rectangles are never double-scaled.** Only framework metrics use `DpiScaler`.
- [ ] **Step 4: Add interop declarations** for the verified notification stages/return flags/part identifiers using pointer-size-correct layouts.
- [ ] **Step 5: Add tests for parsing/classification** of channel/thumb/tick/unknown custom-draw parts without needing a visible window.
- [ ] **Step 6: Keep raw constants internal** and centralized; do not scatter P/Invoke values through the control.
- [ ] **Step 7: Run both TFMs with bounded hang detection because the filter includes the HWND custom-draw fixture:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter "BootstrapRangeRenderLogicTests|BootstrapRangeNativeCustomDrawTests"

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net48 `
  --blame-hang --blame-hang-timeout 5m `
  --filter "BootstrapRangeRenderLogicTests|BootstrapRangeNativeCustomDrawTests"
```

- [ ] **Step 8: Commit:** `feat: add BootstrapRange render primitives`

---

### Task 3: Implement the native-backed `BootstrapRange` shell

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRange.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeTests.cs`

- [ ] **Step 1: Write failing public-contract tests** asserting `BootstrapRange : TrackBar`, default `Variant.Primary`, inherited native range properties, absence of shadow range/value APIs, and synchronous rejection of an undefined `BootstrapVariant` value without mutating the previously valid `Variant`.
- [ ] **Step 2: Implement `BootstrapRange`** with the single V1 `Variant` property. Validate the incoming enum through `BootstrapVariantColorResolver.Resolve(...)` (or the same established framework validation convention) before assigning the backing field, so invalid values cannot survive until a later custom-draw callback.
- [ ] **Step 3: Subscribe to theme changes** using the same lifetime pattern as existing controls; theme/variant changes invalidate only.
- [ ] **Step 4: Handle disposal and handle recreation** without leaked subscriptions or native hooks.
- [ ] **Step 5: Verify changing `Variant` or theme does not change `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `TickFrequency`, `TickStyle`, or `Orientation`; verify a rejected invalid `Variant` leaves both native state and the previous valid `Variant` unchanged.
- [ ] **Step 6: Verify caller event subscriptions** to `Scroll` and `ValueChanged` remain native and are not duplicated.
- [ ] **Step 7: Run focused tests on both TFMs using the repository's required bounded `--blame-hang` options for any handle-based fixture.**
- [ ] **Step 8: Commit:** `feat: add BootstrapRange native control shell`

---

### Task 4: Implement channel and thumb custom drawing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRange.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeRenderLogicTests.cs`

- [ ] **Step 1: Write failing render tests** for rail and thumb colors/bounds under light/dark themes and each `BootstrapVariant` used elsewhere in the framework.
- [ ] **Step 2: In reflected `NM_CUSTOMDRAW` prepaint**, request item-level draw notifications using the verified gate path.
- [ ] **Step 3: Paint `TBCD_CHANNEL`** as the Bootstrap-like neutral rail inside the native channel rectangle, then suppress only the native channel draw.
- [ ] **Step 4: Paint `TBCD_THUMB`** as the accent thumb centered within native thumb bounds, then suppress only the native thumb draw.
- [ ] **Step 5: Leave unknown parts/stages native** and prove no blank/disappearing regions occur during invalidation or handle recreation.
- [ ] **Step 6: Verify no progress-filled segment is introduced** in V1.
- [ ] **Step 7: Run repeated invalidation and value-change tests** to catch GDI/HDC lifetime mistakes.
- [ ] **Step 8: Commit:** `feat: theme BootstrapRange channel and thumb`

---

### Task 5: Add interaction presentation states without reimplementing interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRange.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeRenderLogicTests.cs`

- [ ] **Step 1: Add failing tests** for normal, hover, pressed/dragging, focused, and disabled thumb presentation.
- [ ] **Step 2: Consume only the native custom-draw item-state flags that Task 1 proved reliable on both TFMs.** For states Task 1 recorded as missing or inconsistent, add only the minimal mouse/capture/focus bookkeeping needed to fill those gaps; do not assume `uItemState` provides hot/pressed semantics merely because the flags exist in Win32 definitions.
- [ ] **Step 3: If explicit hover tracking is needed**, hit-test against the current native thumb rectangle (`TBM_GETTHUMBRECT` or the verified current native part bounds). Do not change `Value`, capture, or native mouse processing.
- [ ] **Step 4: Clear transient pressed/hot state** on mouse leave, capture loss, disable, handle destruction, and disposal.
- [ ] **Step 5: Draw focus halo** around the native thumb when focused; keep native Tab/keyboard behavior untouched.
- [ ] **Step 6: Test arrow keys, Home/End where native supports them, PageUp/PageDown, channel click, thumb drag, Tab focus transfer, and event sequencing.** Assertions should target native values/events, not synthetic framework behavior.
- [ ] **Step 7: Confirm no `MessageBox`, dialog, timer, `Thread.Sleep`, or polling loop is introduced.**
- [ ] **Step 8: Commit:** `feat: add BootstrapRange interaction states`

---

### Task 6: Theme ticks, orientation, RTL, and DPI

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRange.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeNativeMethods.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRangeRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeRenderLogicTests.cs`

- [ ] **Step 1: Write failing tests** for `TickStyle.None`, `TopLeft`, `BottomRight`, `Both`, horizontal/vertical orientation, RTL layout, and 96/144/192 DPI framework metrics.
- [ ] **Step 2: Implement tick painting only through the verified native path.** Use native physical tick positions; do not build a second logical TrackBar layout engine.
- [ ] **Step 3: Treat first/last ticks carefully** because Win32's indexed tick-position APIs do not expose them like intermediate ticks; anchor endpoints to native travel/channel geometry rather than extrapolating from value percentages.
- [ ] **Step 4: Verify `RightToLeftLayout` visually and behaviorally.** Framework painting follows native rectangles and must not reverse `Value` itself.
- [ ] **Step 5: Handle DPI changes** by refreshing only framework metrics and invalidating; native rectangles remain device-pixel inputs.
- [ ] **Step 6: Exercise runtime changes** to `Orientation`, `TickStyle`, `TickFrequency`, `RightToLeft`, and `RightToLeftLayout`, including any native handle recreation they trigger.
- [ ] **Step 7: If custom tick drawing proves unreliable on a supported configuration, document and keep native ticks rather than shipping incorrect tick geometry.**
- [ ] **Step 8: Commit:** `feat: harden BootstrapRange layout and ticks`

---

### Task 7: Accessibility, designer, lifecycle, and regression hardening

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRange.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/BootstrapRangeTests.cs`
- Reference: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsTestEnvironment.cs`

- [ ] **Step 1: Verify native accessible role/value behavior** remains TrackBar-derived and updates when `Value` changes.
- [ ] **Step 2: Verify `AccessibleName`, `AccessibleDescription`, `TabStop`, focus navigation, keyboard changes, and inherited events** work with custom drawing enabled.
- [ ] **Step 3: Add lifecycle tests** for create/destroy handle, reparent, orientation/RTL property changes that recreate the handle, repeated theme changes, and disposal.
- [ ] **Step 4: Verify designer-friendly defaults**: `Variant` has correct metadata/default serialization; native properties remain browsable through the inherited TrackBar surface.
- [ ] **Step 5: Add a stress loop** that changes values/themes and invalidates repeatedly, checking for exceptions and deterministic completion rather than pixel snapshots tied to one Windows version.
- [ ] **Step 6: Keep GUI tests fail-fast.** Use existing `WinFormsTestEnvironment`, STA fixtures, a real parent HWND for notification-dependent tests, deterministic host disposal, and bounded message synchronization; no automated test may require a user to dismiss a window/dialog.
- [ ] **Step 7: Run both TFMs with the repository's bounded hang protection.**
- [ ] **Step 8: Commit:** `test: harden BootstrapRange native behavior`

---

### Task 8: Add integrated demo coverage

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/RangeDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Add/modify demo tests under: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/`

- [ ] **Step 1: Add a `RangeDemoForm`** showing at least:
  - Bootstrap-like horizontal range with `TickStyle.None`;
  - horizontal ticked TrackBar mode;
  - vertical orientation;
  - variants;
  - disabled range;
  - light/dark theme switching;
  - RTL + `RightToLeftLayout`;
  - live `Value`/`Scroll`/`ValueChanged` diagnostics.
- [ ] **Step 2: Register `Range` in `MainForm.ConfigurePages()`** near the other input controls.
- [ ] **Step 3: Include keyboard/manual diagnostics** for Tab, arrows, PageUp/PageDown, channel clicks, and thumb drag.
- [ ] **Step 4: Include DPI-friendly layouts** so 100%, 150%, and 200% manual checks do not clip the thumb/focus halo/ticks.
- [ ] **Step 5: Add a demo construction/smoke test** that opens/closes the range page through the existing demo-test conventions without modal UI.
- [ ] **Step 6: Commit:** `demo: showcase BootstrapRange`

---

### Task 9: Documentation and final validation

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `README.md` if the current component inventory/API table requires it
- Modify: `CHANGELOG.md` only if the repository's current unreleased-section convention requires feature entries during implementation

- [ ] **Step 1: Document `BootstrapRange`** as a native-backed `TrackBar` with Bootstrap-aware rendering.
- [ ] **Step 2: Document the one-property V1 extension (`Variant`)** and explicitly direct users to inherited TrackBar APIs for range, value, orientation, ticks, RTL, and events.
- [ ] **Step 3: Document the V1 exclusions** most likely to surprise users: integer value semantics, single thumb, no built-in value tooltip/labels, no progress-filled segment.
- [ ] **Step 4: Document native custom-draw dependency/fallback behavior** if implementation discovered any Windows/common-controls limitations.
- [ ] **Step 5: Run Release build plus the repository full-suite entry point:**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
./test.ps1 -HangTimeoutMinutes 5
```

Do not replace the full-suite command with unbounded raw `dotnet test` invocations. If a focused raw test run is needed while diagnosing a failure, include `--blame-hang --blame-hang-timeout 5m` and the relevant TFM/filter.

- [ ] **Step 6: Launch the integrated demo manually** and verify light/dark, horizontal/vertical, ticks, RTL, disabled, focus, mouse drag, channel click, keyboard, and runtime theme switching.
- [ ] **Step 7: Check repository diff** for accidental generated/binary files and ensure no unrelated public API changes were introduced.
- [ ] **Step 8: Commit:** `docs: document BootstrapRange`

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- [ ] `BootstrapRange` derives directly from `System.Windows.Forms.TrackBar`.
- [ ] The public V1 extension is intentionally minimal: `Variant` plus inherited TrackBar API.
- [ ] Undefined `BootstrapVariant` values are rejected synchronously before state mutation, and the previous valid `Variant` remains unchanged after the exception.
- [ ] `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `TickFrequency`, `TickStyle`, `Orientation`, `RightToLeft`, and `RightToLeftLayout` remain native contracts.
- [ ] `Scroll` and `ValueChanged` remain native events with no framework duplication/synthesis.
- [ ] Horizontal and vertical controls render a Bootstrap-like neutral rail and accent thumb.
- [ ] Light/dark theme and supported variants update at runtime without changing value/range state.
- [ ] Focus, hover/pressed where reliably detectable, and disabled states are visually distinct.
- [ ] Native thumb hit target, capture, keyboard behavior, Tab navigation, and accessibility are preserved.
- [ ] Ticks remain correct for supported `TickStyle`/`TickFrequency`; if a safe custom tick path is unavailable in a documented configuration, native ticks are preserved rather than replaced with incorrect geometry.
- [ ] RTL layout follows native `RightToLeft`/`RightToLeftLayout` behavior.
- [ ] Framework metrics scale at 96/144/192 DPI without double-scaling native rectangles.
- [ ] No custom value/selection/accessibility engine is introduced.
- [ ] No new external package is added.
- [ ] No HDC/GDI resource leak is introduced by custom drawing.
- [ ] Notification-dependent HWND tests run on STA under a real parent HWND, use bounded synchronization, and dispose their host deterministically.
- [ ] Automated HWND tests use the repository WinForms test environment and cannot hang on modal UI.
- [ ] Focused raw GUI test commands use bounded `--blame-hang` protection, and the full suite runs via `./test.ps1`.
- [ ] Build and tests pass on both `net48` and `net8.0-windows`.
- [ ] Integrated demo includes range scenarios and manual interaction diagnostics.

---

## Risk Register

| Risk | Mitigation |
|---|---|
| Managed `TrackBar` does not expose owner draw | Mandatory `NM_CUSTOMDRAW` gate before implementation |
| Notification is sent to parent, not directly to TrackBar | Verify WinForms reflected-notify path on both TFMs with an STA fixture and a real parent HWND; never infer failure from an unparented child handle |
| Native custom-draw item-state flags vary by TFM/common-controls state | Characterize `uItemState` in Task 1 and consume only proven flags; use minimal framework bookkeeping for missing hot/pressed state |
| Custom-draw return flags suppress too much native painting | Suppress only verified parts/stages; unknown parts use `CDRF_DODEFAULT` |
| x86/x64 interop layout mismatch | Pointer-size-correct `NMCUSTOMDRAW`; native-handle tests on supported builds |
| Custom thumb visual disagrees with native hit target | Paint inside native `TBCD_THUMB` rectangle; do not alter native hit testing |
| Tick geometry drifts from native layout | Use native physical tick positions and channel/thumb bounds; fall back to native ticks if uncertain |
| RTL/vertical direction is reversed by framework math | Never implement a separate value-to-pixel engine; follow native rectangles/layout |
| DPI metrics are double-scaled | Scale framework-only metrics; never rescale native custom-draw rectangles |
| Invalid `Variant` survives until paint and fails asynchronously | Validate through the established variant resolver before mutating the backing field; test that rejected values leave prior state unchanged |
| OS/common-controls visual differences make screenshots brittle | Prefer deterministic render-logic assertions + focused HWND smoke tests; use manual demo for visual acceptance |
| GUI tests hang on hidden/modal WinForms UI | Reuse `WinFormsTestEnvironment`, STA/parent-HWND hosting, fail-fast infrastructure, bounded synchronization, `--blame-hang`, and `./test.ps1` |
| Feature grows into dual-thumb/labels/tooltips | Keep explicit V1 exclusions and require separate plans for those abstractions |

---

## Definition of Done

- [ ] Rendering decision gate passed on both target frameworks using a structurally valid STA + parent-HWND probe, or implementation stopped with a documented architectural decision.
- [ ] Task 1 records which `NMCUSTOMDRAW.uItemState` flags are reliable for normal/focused/hot/pressed/disabled presentation on both TFMs.
- [ ] Tasks 1–9 completed in order with focused tests kept green.
- [ ] Public API reviewed against the V1 contract and exclusions, including synchronous invalid-`Variant` rejection without state mutation.
- [ ] Native behavior regression suite passes for value, events, keyboard, mouse, orientation, ticks, RTL, focus, accessibility, handle recreation, and disposal.
- [ ] Release build succeeds and `./test.ps1 -HangTimeoutMinutes 5` passes for `net48` and `net8.0-windows`; any focused raw GUI test runs used during development include bounded `--blame-hang` protection.
- [ ] Demo verified manually in light/dark at common DPI scales.
- [ ] Documentation reflects actual implemented behavior, including any discovered native custom-draw limitations.
