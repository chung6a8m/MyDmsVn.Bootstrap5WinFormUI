# BootstrapCheckBox, BootstrapRadioButton, and BootstrapSwitch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Bootstrap 5.3-inspired `BootstrapCheckBox`, `BootstrapRadioButton`, and `BootstrapSwitch` controls that retain native WinForms checked state, events, keyboard behavior, radio grouping, focus, accessibility, Designer behavior, and inherited fallback presentation while adopting the framework's theme, semantic variants, validation states, DPI scaling, and Bootstrap-like normal form-check presentation.

**Architecture:** Use native inheritance rather than composition: `BootstrapCheckBox : CheckBox`, `BootstrapRadioButton : RadioButton`, and `BootstrapSwitch : CheckBox`. Native controls remain authoritative for `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, state-change events, mnemonic/Space activation, radio grouping, and accessibility state. Framework code owns only the normal text-label form-check presentation, theme/font lifecycle, DPI-aware layout, and two additive public appearance properties (`Variant`, `ValidationState`). One internal pure `BootstrapCheckableRenderLogic` owns palette/metrics/layout/glyph calculations. Unsupported inherited visual modes (`Appearance.Button` and effective image presentation) deliberately fall back to native base rendering and native preferred-size behavior instead of being partially reimplemented.

**Tech Stack:** C#, native Windows Forms `CheckBox` / `RadioButton`, `System.Drawing`, existing Theme / Rendering / DPI infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `BootstrapValidationState`, `DpiScaler`, `ColorUtil`, NUnit 4, STA WinForms tests, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** This plan implements the user's 2026-09-03 request and the review findings applied on the same day. It must remain consistent with `AGENTS.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. Bootstrap 5.3 form checks/radios/switches are a visual/behavior reference only; native WinForms semantics remain authoritative.

## Global Constraints

- Keep root namespace `MyDmsVn.Bootstrap5WinFormUI`; all three public controls live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product, tests, and demo must compile and run from one shared codebase for both `net48` and `net8.0-windows`.
- `BootstrapCheckBox` must inherit native `CheckBox`; `BootstrapRadioButton` must inherit native `RadioButton`; `BootstrapSwitch` must inherit native `CheckBox`.
- Do **not** wrap RadioButton in a `UserControl`; same-parent grouping and native keyboard behavior must remain native.
- Native checked-state members remain inherited and authoritative. Do not redeclare or mirror `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, `CheckedChanged`, `CheckStateChanged`, `Appearance`, `CheckAlign`, `TextAlign`, or `AutoSize`.
- Do not introduce a second checked-state model, radio registry/group manager, keyboard engine, mnemonic processor, accessibility state model, timer, global hook, or message filter.
- Public V1 additions are exactly `Variant` and `ValidationState`. Do not add custom color, size, radius, animation, group, indicator-size, switch-width, or thumb APIs.
- `Variant` defaults to `Primary` and supplies the enabled semantic accent only when `ValidationState == None`.
- `ValidationState` defaults to `None`. `Valid` resolves to Success and `Invalid` resolves to Danger. Validation accent overrides `Variant` while enabled.
- **Validation is visible even while unchecked.** For enabled controls with `Valid`/`Invalid`, the indicator/track border and label text use the effective validation accent. Checked/Indeterminate fill also uses that accent. `ValidationState.None` uses normal text and the configured Variant only for active checked/selected presentation.
- Disabled presentation has highest precedence over Variant/Validation and uses current theme disabled/muted tokens without changing native state.
- Focus is a separate concern: focus indication uses the existing theme Focus token and does not replace validation accent or mutate state.
- CheckBox/RadioButton use a 16 logical-pixel indicator derived from `SpacingLG`; text gap uses `SpacingSM`; border/focus widths reuse existing theme metrics.
- Switch uses a 32x16 logical track (`SpacingLG * 2` by `SpacingLG`) with a DPI-scaled inset thumb.
- CheckBox uses the theme small radius clamped to indicator bounds; RadioButton is circular; Switch is pill-shaped. These are characteristic shapes, not public radius APIs.
- **Painting always follows the actual native `CheckState`, regardless of `ThreeState`.** Programmatic `CheckState = Indeterminate` must render Indeterminate even when `ThreeState == false`. `ThreeState` only controls which states native user interaction cycles through.
- `BootstrapSwitch` preserves inherited `ThreeState`/`CheckState`; Indeterminate uses a centered thumb and distinct mixed-state cue.
- V1 transitions are immediate. No thumb/check animation and no `AnimationDuration` API.
- Preserve native `AutoCheck`. Framework painting must never toggle state itself.
- **RadioButton `AutoCheck = false` preserves native manual-group semantics.** Multiple same-parent radios may be programmatically `Checked = true`; clicking one does not automatically uncheck siblings. Framework code must not restore exclusivity manually.
- Preserve native RadioButton grouping only when native `AutoCheck` provides it. Never traverse siblings to synchronize checked state.
- Preserve standard focus, Tab, Space, arrow-key, Alt/mnemonic behavior. Custom visual-state tracking may observe input but must not replace/consume native activation.
- Respect `UseMnemonic`, keyboard-cue visibility, `CheckAlign`, `TextAlign`, `RightToLeft`, `Padding`, `AutoEllipsis`, and host-assigned bounds.
- **Do not assume RTL means “mirror `CheckAlign` again.”** Before locking custom layout, characterize native `CheckBox` and `RadioButton` for representative `CheckAlign` × `RightToLeft` combinations on both TFMs. The framework layout must match that observed slot-placement behavior or document one deliberate deviation. Switch thumb direction is resolved only after the indicator slot is fixed: RTL mirrors logical start/end inside the switch track and must not double-mirror the indicator slot.
- Tiny/malformed client rectangles must never produce negative drawing rectangles or throw.
- `Appearance`, `Image`, `ImageList`, `ImageIndex`, `ImageKey`, and `TextImageRelation` remain caller-owned inherited API.
- **Native visual fallback contract:** when `Appearance != Appearance.Normal` or an effective image is configured (`Image != null`, or `ImageList != null` with an effective `ImageIndex`/`ImageKey`), the framework does not attempt Bootstrap form-check painting. It delegates painting and preferred-size calculation to native/base behavior while preserving `Variant`/`ValidationState` values for when normal form-check mode resumes.
- Fallback mode must not mutate inherited appearance/image properties and must not throw when toggled repeatedly at runtime.
- Runtime theme changes repaint framework-owned normal form-check presentation and update only framework-owned theme fonts. Caller-assigned fonts remain caller-owned.
- Designer construction is parameterless and requires no application bootstrap.
- Dispose every owned GDI object deterministically; no per-paint cached `Graphics`, `Brush`, `Pen`, `GraphicsPath`, or bitmap leaks.
- No control creates an independent WinForms timer.
- InputGroup integration is outside this plan.
- Every new public/protected member requires XML documentation.
- The public API fingerprint must fail intentionally after the new types are added and must be updated only after exact exported-surface review.
- No TODO/TBD placeholders, prototype namespaces, aliases, duplicate theme infrastructure, or target-specific public APIs may remain.

---

## Reference Behavior and Deliberate WinForms Adaptation

### Bootstrap behaviors preserved conceptually

- CheckBox uses a square indicator, check mark, and mixed-state mark.
- RadioButton uses a circular indicator and selected center dot.
- Switch uses checkbox semantics with a pill track and movable thumb.
- Disabled state is visually muted.
- Valid/Invalid states visibly affect the input border and associated label presentation, including while unchecked.
- Focus remains visible and keyboard interaction remains first-class.

### Native WinForms adaptations

- Native `CheckBox` / `RadioButton` state, event ordering, user cycling, and grouping are not reimplemented.
- `CheckState` can be programmatically Indeterminate independently from `ThreeState`; rendering follows `CheckState`, while `ThreeState` only affects native user cycling.
- Radio grouping follows native same-parent + `AutoCheck` behavior. `AutoCheck=false` is a true caller-managed state mode, not a framework-managed radio group.
- `CheckAlign`, `TextAlign`, `RightToLeft`, and host layout are desktop concerns and remain supported.
- Button/image appearance is not partially themed: it uses native fallback presentation.
- Accessibility uses the native control's checked-state semantics.
- Switch visual motion is deferred in V1.

---

## Public Contract to Implement

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public class BootstrapCheckBox : CheckBox
{
    public BootstrapVariant Variant { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
}

public class BootstrapRadioButton : RadioButton
{
    public BootstrapVariant Variant { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
}

public class BootstrapSwitch : CheckBox
{
    public BootstrapVariant Variant { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
}
```

Contract rules:

- Constructors are parameterless and Designer-safe.
- `Variant = Primary`; `ValidationState = None`.
- Undefined `BootstrapVariant` / `BootstrapValidationState` assignments throw before mutation/invalidation.
- Existing shared enums are unchanged.
- Checked/radio state properties and events remain inherited.
- The only declared public instance properties added by each class are `Variant` and `ValidationState`.
- Required protected owner-paint/lifecycle overrides are reviewed through the existing API fingerprint gate.

---

## Shared Internal Rendering Contract

```csharp
internal enum BootstrapCheckableKind
{
    CheckBox,
    RadioButton,
    Switch
}

internal static class BootstrapCheckableRenderLogic
{
    // Pure metric, palette, layout, glyph/thumb geometry, preferred-size,
    // validation and native-fallback decision helpers.
}
```

Mandatory boundaries:

- Internal only; no dependency on concrete controls, handles, `Graphics`, static theme manager, timers, or global input state.
- Reuse `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, theme tokens, and existing rendering helpers.
- Palette precedence: Disabled > Valid/Invalid > Variant/neutral state.
- Enabled Valid/Invalid changes indicator/track border **and label text** even when unchecked.
- Checked/Indeterminate fill uses effective semantic/validation accent; glyph/thumb foreground uses contrast helper.
- Focus uses the theme Focus token independently from validation accent.
- Render state consumes actual `CheckState`; it never infers Indeterminate eligibility from `ThreeState`.
- Radio callers pass only Unchecked/Checked.
- Layout accepts already-resolved native-compatible indicator placement semantics so RTL does not double-mirror `CheckAlign`.
- Switch thumb start/end direction is separate from indicator-slot placement.
- Geometry clamps malformed/tiny rectangles to safe contained output.
- A pure/native-fallback helper may evaluate `Appearance` plus effective image metadata, but must not inspect concrete control instances.

---

## File Structure and Responsibilities

### Product files

- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs`
  - Shared internal palette, metrics, layout, preferred-size, state/glyph/thumb geometry, validation precedence, and fallback-decision logic.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs`
  - Native CheckBox subclass; normal form-check owner paint, native fallback, theme/font lifecycle, transient visual state.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs`
  - Native RadioButton subclass; normal form-check owner paint, native fallback, native grouping/AutoCheck semantics.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs`
  - Native CheckBox subclass; switch track/thumb normal presentation, native fallback, native CheckState semantics.

### Tests

- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableRenderLogicTests.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckBoxTests.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapRadioButtonTests.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSwitchTests.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableInteractionTests.cs`

### Demo/docs/API files

- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Extend demo smoke/navigation tests where supported.
- Modify `README.md`, `CHANGELOG.md`, `docs/COMPONENTS.md`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`.
- Modify `docs/PUBLIC_API_BASELINE.md` and `Phase16PublicApiBaselineTests.cs` only after deliberate API review.

---

## Task 1 — Characterize native semantics and lock pure rendering/layout contracts

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableRenderLogicTests.cs`
- Create characterization coverage in the same fixture or `BootstrapCheckableInteractionTests.cs` if a real STA control is required.

- [ ] **Step 1: Characterize native state semantics before custom code.**
  - Compare native `CheckBox` behavior for `ThreeState=false; CheckState=Indeterminate` and `ThreeState=true; CheckState=Indeterminate`.
  - Record event counts for programmatic `Unchecked -> Indeterminate -> Checked` changes.
  - Confirm `ThreeState` affects user cycling, not whether a programmatic Indeterminate value can exist.
  - Characterize native `RadioButton.AutoCheck=false`: same-parent radios may both be programmatically checked; clicking one does not restore exclusivity.

- [ ] **Step 2: Characterize native RTL + `CheckAlign` placement.**
  - Use native `CheckBox` and `RadioButton` peers for `MiddleLeft` and `MiddleRight` under `RightToLeft.No` and `RightToLeft.Yes`.
  - Run characterization on both `net48` and `net8.0-windows`.
  - Lock one native-compatible indicator-slot rule from the observed behavior; do not independently mirror both `CheckAlign` and the resolved slot.

- [ ] **Step 3: Write failing pure metric tests.**
  - 96 DPI CheckBox/Radio indicator = 16px, text gap = 8px, border = 1px, focus width = 2px.
  - Switch track = 32x16px; thumb remains contained after inset/stroke.
  - Scale 96/120/144/168/192 DPI through `DpiScaler`.
  - CheckBox radius uses scaled `RadiusSmall` clamped to bounds; Radio circular; Switch pill radius = half track height.
  - Reject null theme inputs, undefined kind, non-positive DPI.

- [ ] **Step 4: Write failing palette-precedence tests.**
  - Every semantic Variant under Light/Dark.
  - `ValidationState.None`: unchecked uses neutral border/text; checked uses Variant accent.
  - `Valid`: unchecked border + label are Success; checked/indeterminate border/fill + label are Success.
  - `Invalid`: unchecked border + label are Danger; checked/indeterminate border/fill + label are Danger.
  - Disabled state ignores Variant/Validation and uses disabled/muted tokens.
  - Focus indicator uses theme Focus independently from the validation/variant accent.
  - Undefined Variant/Validation values throw.

- [ ] **Step 5: Write failing actual-CheckState visual tests.**
  - Unchecked/Checked/Indeterminate resolve distinct states.
  - Indeterminate resolution does not receive or depend on `ThreeState`.
  - Switch thumb: unchecked=start, checked=end, indeterminate=center.
  - RTL reverses switch thumb logical start/end **inside the already-resolved track slot only**.

- [ ] **Step 6: Write failing layout/preferred-size/fallback tests.**
  - Native-compatible left/right indicator-slot rules from Step 2.
  - Representative top/middle/bottom alignments remain contained.
  - TextAlign honored in remaining text space.
  - Empty/long text, padding, zero/tiny/malformed rectangles.
  - Preferred height reserves max(text, indicator/track) plus focus/padding.
  - Switch reserves 32px track.
  - Fallback predicate is true for `Appearance.Button` and effective image presentation; false for normal text-only form-check mode.

- [ ] **Step 7: Run tests and confirm failure before implementation.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckable"
```

- [ ] **Step 8: Implement the minimal pure helper and rerun on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckable"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckable"
```

- [ ] **Step 9: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "feat: add checkable rendering contracts"
```

---

## Task 2 — Implement `BootstrapCheckBox` on native CheckBox semantics

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckBoxTests.cs`

- [ ] **Step 1: Write failing contract/default tests.**
  - Direct `CheckBox` inheritance; Designer-safe constructor.
  - Defaults `Variant=Primary`, `ValidationState=None`.
  - Exactly two new declared public properties.
  - No redeclared native state/alignment events/properties.
  - Invalid enum assignment throws before mutation.

- [ ] **Step 2: Write native state tests including the `ThreeState` correction.**
  - `Checked`/`CheckState` use inherited storage/events.
  - `ThreeState=true` user cycle supports Indeterminate.
  - `ThreeState=false; CheckState=Indeterminate` remains a valid programmatic state and paints mixed state.
  - `AutoCheck=false` blocks native user toggling but not programmatic assignment.
  - Theme/paint/DPI changes never raise extra state events.

- [ ] **Step 3: Implement theme/font lifecycle and transient hover/pressed state.**
  - Preserve base input processing and event ordering.
  - Theme subscriptions detach on dispose; caller-owned Font remains caller-owned.
  - Clear transient visual state on release/capture loss/disable/hide/focus loss as appropriate.

- [ ] **Step 4: Implement normal form-check painting.**
  - Square indicator, checked vector mark, centered mixed-state bar.
  - Palette/layout come from Task 1.
  - Valid/Invalid affects unchecked border + label and checked/indeterminate fill/border + label.
  - Focus ring uses Focus token without erasing validation accent.
  - `TextRenderer` respects mnemonics, keyboard cues, AutoEllipsis, TextAlign, native-compatible CheckAlign/RTL slot placement, Padding.
  - Dispose all GDI resources.

- [ ] **Step 5: Implement explicit native visual fallback.**
  - Detect `Appearance != Normal` or an effective image configuration.
  - In fallback mode, delegate paint to `base.OnPaint(e)` and preferred size to `base.GetPreferredSize(proposedSize)`; do not draw framework indicator/text over native output.
  - Toggling fallback on/off at runtime preserves `Checked`, Variant, ValidationState, image/appearance properties, and event counts.

- [ ] **Step 6: Add paint/preferred-size/lifecycle/fallback tests.**
  - Light/Dark, all variants, Valid/Invalid, Disabled, all CheckState values.
  - Programmatic Indeterminate with `ThreeState=false`.
  - Appearance.Button fallback compared with a native peer for no-throw/state/preferred-size semantics.
  - Effective Image/ImageList fallback compared with native peer semantics.
  - Repeated fallback switching, theme switching, disposal.

- [ ] **Step 7: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapCheckable"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapCheckable"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "feat: add bootstrap checkbox"
```

---

## Task 3 — Implement `BootstrapRadioButton` without replacing native grouping

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapRadioButtonTests.cs`

- [ ] **Step 1: Write contract/default tests.**
  - Direct `RadioButton` inheritance, Designer-safe constructor, two new public properties only.
  - No framework group/sibling API.

- [ ] **Step 2: Lock native grouping and `AutoCheck=false` semantics.**
  - Same-parent default-AutoCheck radios are mutually exclusive.
  - Different-parent radios do not affect each other.
  - With `AutoCheck=false`, two same-parent radios can both be programmatically `Checked=true`.
  - Clicking/keyboard activating an `AutoCheck=false` radio does not automatically uncheck a checked sibling or force itself checked.
  - CheckedChanged counts match native peers.
  - Reparenting changes grouping naturally without framework bookkeeping.

- [ ] **Step 3: Implement normal owner painting and lifecycle.**
  - Circular indicator + center dot when checked.
  - Validation affects unchecked border + label and checked border/dot/fill accent.
  - Focus/hover/pressed/disabled resolved through shared helper.
  - No sibling traversal or manual unchecking.

- [ ] **Step 4: Preserve keyboard/focus/mnemonic behavior.**
  - Arrow/Space/Tab/mnemonic routes remain native; visual hooks call base and never consume activation.

- [ ] **Step 5: Implement the same native visual fallback contract as CheckBox.**
  - Appearance.Button/effective image presentation delegates paint/preferred size to base behavior.
  - Runtime fallback switching does not disturb grouping/state/events.

- [ ] **Step 6: Add validation/RTL/fallback/grouping/lifecycle tests and run both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapCheckable"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapCheckable"
```

- [ ] **Step 7: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "feat: add bootstrap radio button"
```

---

## Task 4 — Implement `BootstrapSwitch` as native CheckBox semantics with switch presentation

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSwitchTests.cs`

- [ ] **Step 1: Write contract/default/native state tests.**
  - Direct `CheckBox` inheritance and two new public properties only.
  - Native Checked/CheckState/ThreeState/AutoCheck/events remain inherited.
  - `ThreeState=false; CheckState=Indeterminate` is programmatically valid and must render centered mixed-state thumb.
  - `AutoCheck=false` blocks native user toggling but preserves caller-controlled state.

- [ ] **Step 2: Implement lifecycle and immediate state presentation.**
  - No timer/animation.
  - Theme/DPI/state changes resolve thumb position directly from actual CheckState.

- [ ] **Step 3: Implement normal switch painting.**
  - 32x16 logical pill track with inset thumb.
  - Unchecked neutral track + logical-start thumb.
  - Checked effective accent track + logical-end thumb.
  - Indeterminate effective accent/mixed track + centered thumb/cue.
  - Valid/Invalid affects unchecked track border + label and checked/indeterminate accent + label.
  - Focus uses theme Focus token.
  - Resolve track slot using native-compatible CheckAlign/RTL behavior first; then mirror thumb logical start/end inside the track for RTL.

- [ ] **Step 4: Implement native fallback for Appearance/image modes.**
  - Same contract as BootstrapCheckBox: base paint/preferred-size and no framework switch overlay while fallback is active.

- [ ] **Step 5: Add tests.**
  - All CheckState values including programmatic Indeterminate with ThreeState=false.
  - Validation while unchecked and checked.
  - RTL track-slot vs thumb-direction tests proving no double mirror.
  - Appearance/image fallback and runtime return to normal switch mode.
  - Rapid programmatic state changes settle on final state with native event counts.

- [ ] **Step 6: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSwitch|FullyQualifiedName~BootstrapCheckable"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSwitch|FullyQualifiedName~BootstrapCheckable"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls
git commit -m "feat: add bootstrap switch"
```

---

## Task 5 — Harden keyboard, mouse, grouping, accessibility, RTL, fallback, and lifecycle

**Files:**
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableInteractionTests.cs`
- Modify the three controls only where failing tests prove defects.

- [ ] **Step 1: Compare keyboard behavior with native peers.**
  - CheckBox/Switch: Tab then Space toggles once with native event counts.
  - Radio: Space and same-group arrow navigation match native peers when AutoCheck=true.
  - AutoCheck=false Radio activation does not introduce framework exclusivity.
  - Shift+Tab exits normally; Alt/mnemonics do not leave stale pressed state.

- [ ] **Step 2: Mouse/capture regressions.**
  - Click inside activates through base path once.
  - Drag/release outside, capture loss, disable/hide/dispose clear only transient visuals.
  - No duplicate CheckedChanged/CheckStateChanged.

- [ ] **Step 3: Grouping/reparenting regressions.**
  - Native default exclusivity, separate-parent isolation, AutoCheck=false multi-checked state, and reparent behavior.
  - Assert no static registry/subscription exists.

- [ ] **Step 4: Accessibility assertions.**
  - Checked accessibility state follows inherited native state, including Indeterminate where exposed by native CheckBox.
  - AccessibleName/Description remain caller-owned.
  - No hidden child controls.

- [ ] **Step 5: RTL/alignment characterization regression.**
  - Preserve the Task 1 native-characterized indicator placement on both TFMs.
  - Test MiddleLeft/MiddleRight × LTR/RTL for CheckBox/Radio.
  - Test Switch slot placement separately from thumb direction.

- [ ] **Step 6: Fallback transition stress.**
  - Normal -> Appearance.Button -> Normal.
  - Normal -> Image/ImageList -> Normal.
  - Repeated transitions preserve state/events/Variant/Validation/font ownership and do not layer framework painting over native fallback.

- [ ] **Step 7: Run the family on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckable|FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapSwitch"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckable|FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapSwitch"
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs
git commit -m "test: harden checkable interactions"
```

---

## Task 6 — Add integrated demo coverage and manual verification matrix

**Files:**
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify/add relevant demo tests where supported.

- [ ] **Step 1: Add demo navigation/smoke coverage.**
  - Main demo exposes **Checks / Radios / Switches** and constructs all three controls without exceptions.

- [ ] **Step 2: Implement compact demo sections.**
  - CheckBox: Unchecked, Checked, Indeterminate with ThreeState=true, programmatic Indeterminate with ThreeState=false, Valid unchecked, Invalid unchecked, Disabled, AutoCheck=false.
  - Radio: normal same-parent group, separate-parent group, and an AutoCheck=false pair showing caller-managed/multi-checked state.
  - Switch: Off, On, Indeterminate, programmatic Indeterminate with ThreeState=false, Valid unchecked, Invalid unchecked, Disabled, AutoCheck=false.
  - Show all semantic variants for one representative checked control.
  - Include event counters.
  - Include one Appearance.Button/effective-image fallback sample so native fallback is manually observable.

- [ ] **Step 3: Verify live theme switching.**
  - Normal form-check presentation changes live without state loss.
  - Native fallback remains usable and returns to framework rendering when caller restores normal text-only mode.

- [ ] **Step 4: Manual keyboard/focus/RTL checks.**
  - Tab/Shift+Tab/Space/Radio arrows/mnemonics/Alt.
  - AutoCheck=false radio behavior.
  - LTR/RTL with representative left/right CheckAlign; Switch thumb direction must not double-mirror track placement.

- [ ] **Step 5: Manual real-DPI checks at 100/125/150/175/200%.**
  - Check/radio/switch geometry centered; validation visible while unchecked; focus ring unclipped; no bitmap-stretched glyphs.

- [ ] **Step 6: Commit.**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo
git commit -m "demo: showcase check radio and switch controls"
```

---

## Task 7 — Document final contracts and review public API baseline

**Files:**
- Modify `README.md`, `CHANGELOG.md`, `docs/COMPONENTS.md`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/PUBLIC_API_BASELINE.md`.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`.

- [ ] **Step 1: Document exact component behavior.**
  - Direct native inheritance and only `Variant`/`ValidationState` additions.
  - Validation affects unchecked border + label as well as active states.
  - Painting follows actual CheckState independently from ThreeState; ThreeState controls user cycling only.
  - Radio default native grouping vs AutoCheck=false caller-managed/multi-checked semantics.
  - Native RTL/CheckAlign-compatible placement and separate Switch thumb direction.
  - Appearance/image native fallback contract.
  - V1 non-goals: animation, custom colors/sizes/radius, custom radio grouping, InputGroup integration.

- [ ] **Step 2: Update architecture/testing/development docs and README/CHANGELOG.**
  - Add shared checkable rendering primitive and both pure + native-peer characterization matrices.

- [ ] **Step 3: Run API fingerprint and intentionally observe failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

- [ ] **Step 4: Review exported surface before updating hash.**
  - Exactly three new exported classes.
  - Each declares only Variant/ValidationState publicly plus minimum protected overrides.
  - No internal render/fallback/layout types exported.
  - Shared enums unchanged; AssemblyVersion remains `1.0.0.0`.

- [ ] **Step 5: Add explicit release-contract test, update reviewed fingerprint/docs, then rerun Phase16 tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

- [ ] **Step 6: Commit.**

```bash
git add README.md CHANGELOG.md docs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: finalize checkable control contracts"
```

---

## Task 8 — Full dual-target verification and final hardening

- [ ] **Step 1: Release build.**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

- [ ] **Step 2: Full net48 tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build
```

- [ ] **Step 3: Full net8.0-windows tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build
```

- [ ] **Step 4: Execute manual matrix.**
  - Light/Dark; all variants; Valid/Invalid while unchecked and checked.
  - Unchecked/Checked/Indeterminate, including programmatic Indeterminate with ThreeState=false.
  - hover/pressed/focus; Tab/Shift+Tab/Space/Radio arrows/mnemonics/Alt.
  - default radio grouping and AutoCheck=false multi-checked caller-managed behavior.
  - native-characterized RTL/CheckAlign combinations and Switch thumb direction.
  - Appearance.Button/image fallback and return to normal mode.
  - 100/125/150/175/200% real Windows DPI.
  - repeated theme/fallback/state changes.

- [ ] **Step 5: Inspect lifecycle/resources and scope.**
  - Theme subscription removed on dispose; caller fonts/accessibility metadata remain caller-owned.
  - No timer, animation state, bitmap cache, hidden child control, radio registry, global hook, or duplicate semantic resolver.
  - No unguarded `net48`-incompatible API.
  - No InputGroup modification or unreviewed public API.

- [ ] **Step 6: Commit only focused verification fixes and finish with clean working tree.**

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- `BootstrapCheckBox`, `BootstrapRadioButton`, and `BootstrapSwitch` exist under `MyDmsVn.Bootstrap5WinFormUI.Controls` and directly inherit native WinForms controls.
- All native checked-state/event/keyboard behavior remains authoritative.
- Painting uses actual `CheckState`; programmatic Indeterminate renders correctly even with `ThreeState=false`; `ThreeState` only controls native user cycling.
- Default RadioButton same-parent exclusivity remains native, while `AutoCheck=false` permits caller-managed/multi-checked state without framework sibling synchronization.
- Each control adds only `Variant` and `ValidationState` publicly.
- Enabled Valid/Invalid is visible on unchecked border/track **and label text**, and controls active fill when checked/indeterminate; disabled presentation wins; focus remains a separate Focus-token indicator.
- RTL/CheckAlign behavior is based on native characterization on both TFMs; Switch thumb direction is applied inside the resolved track slot without double mirroring.
- `Appearance.Button` and effective image presentation use native/base painting and preferred-size fallback rather than partial Bootstrap painting.
- Fallback can be entered/exited at runtime without state/event/font/property corruption.
- Shared internal `BootstrapCheckableRenderLogic` owns palette/metrics/layout/state/fallback calculations with no exported helper types.
- Geometry is DPI-aware at 100–200% and safe for tiny/malformed bounds.
- Light/Dark switching works without state loss or caller-font corruption.
- No external dependency, timer, animation engine, message hook, second checked-state model, radio registry, or public base class is introduced.
- Demo covers normal states, validation while unchecked, programmatic Indeterminate with ThreeState=false, AutoCheck=false radio semantics, RTL, and native fallback.
- Public API fingerprint is deliberately reviewed and updated only after exact exported-surface inspection.
- Full Release build and tests pass for both `net48` and `net8.0-windows`.
- Documentation/changelog match the final behavior and V1 non-goals.

## Recommended Execution Order

Execute Tasks 1–8 sequentially. Task 1 must characterize native state and RTL/alignment behavior before the pure custom layout is finalized. Tasks 2–4 implement one control at a time against that contract. Task 5 protects cross-control native semantics and fallback transitions. Task 6 makes the corrected behaviors observable. Task 7 freezes documentation/API only after implementation. Task 8 is the final dual-target gate.