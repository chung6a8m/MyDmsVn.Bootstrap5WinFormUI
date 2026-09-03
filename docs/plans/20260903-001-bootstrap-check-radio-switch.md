# BootstrapCheckBox, BootstrapRadioButton, and BootstrapSwitch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Bootstrap 5.3-inspired `BootstrapCheckBox`, `BootstrapRadioButton`, and `BootstrapSwitch` controls that retain native WinForms checked state, events, keyboard behavior, radio grouping, focus, accessibility, and Designer behavior while adopting the framework's theme, semantic variants, validation states, DPI scaling, and Bootstrap-like presentation.

**Architecture:** Use native inheritance rather than composition: `BootstrapCheckBox : CheckBox`, `BootstrapRadioButton : RadioButton`, and `BootstrapSwitch : CheckBox`. The native controls remain authoritative for `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, `CheckedChanged`, `CheckStateChanged`, mnemonic/Space activation, and RadioButton container grouping. Framework code owns only painting, theme/font lifecycle, DPI-aware preferred-size/layout calculations, hover/pressed/focus presentation, and two additive public appearance properties (`Variant` and `ValidationState`). Share palette, metrics, indicator geometry, content layout, and glyph geometry through one internal pure `BootstrapCheckableRenderLogic`; do not introduce a public base class or duplicate three rendering engines.

**Tech Stack:** C#, native Windows Forms `CheckBox` / `RadioButton`, `System.Drawing`, existing Theme / Rendering / DPI infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `BootstrapValidationState`, `DpiScaler`, `ColorUtil`, `RoundedPath`/`GraphicsPath` helpers where applicable, NUnit 4, STA WinForms tests, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** This plan implements the user's 2026-09-03 request and must remain consistent with `AGENTS.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. Bootstrap 5.3 form checks/radios/switches are the visual/behavior reference at `https://getbootstrap.com/docs/5.3/forms/checks-radios/`; Bootstrap is inspiration only and native WinForms semantics remain authoritative.

## Global Constraints

- Keep root namespace `MyDmsVn.Bootstrap5WinFormUI`; all three public controls live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product, tests, and demo must compile and run from one shared codebase for both `net48` and `net8.0-windows`.
- `BootstrapCheckBox` must inherit native `CheckBox`; `BootstrapRadioButton` must inherit native `RadioButton`; `BootstrapSwitch` must inherit native `CheckBox`.
- Do **not** wrap RadioButton in a `UserControl`; doing so would break native same-container radio grouping and keyboard behavior.
- Native checked-state members remain inherited and authoritative. Do not redeclare or mirror `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, `CheckedChanged`, `CheckStateChanged`, `Appearance`, `CheckAlign`, `TextAlign`, `AutoSize`, or RadioButton grouping behavior.
- Do not implement a second checked-state model, group manager, keyboard-navigation engine, mnemonic processor, accessibility state model, timer, global hook, or message filter.
- Public V1 appearance additions are intentionally small: `Variant` and `ValidationState` only. Do not add `CheckedColor`, `UncheckedColor`, `TrackColor`, `ThumbColor`, `IndicatorSize`, `SwitchWidth`, `BorderRadius`, or size enums in V1.
- `Variant` defaults to `BootstrapVariant.Primary` and influences the enabled checked/selected accent only when `ValidationState == None`.
- `ValidationState` defaults to `BootstrapValidationState.None`. `Valid` uses the Success semantic accent; `Invalid` uses Danger; validation accent overrides `Variant` while enabled.
- Disabled presentation takes precedence over Variant/Validation accent and uses current theme muted/disabled tokens without changing native state.
- CheckBox and RadioButton use a 16 logical-pixel indicator based on `BootstrapThemeMetrics.SpacingLG`; the text gap uses `SpacingSM`; normal/focus stroke widths reuse `BorderWidth`/`FocusBorderWidth`.
- Switch uses a 32x16 logical track (`SpacingLG * 2` by `SpacingLG`) with a DPI-scaled inset thumb; the track is pill-shaped and the thumb position is derived from native `CheckState`.
- CheckBox uses the theme small radius (`RadiusSmall`) clamped to the indicator bounds. RadioButton always renders circular geometry. Switch always renders pill track/thumb geometry. These characteristic shapes are not caller-radius APIs.
- `CheckState.Unchecked`, `Checked`, and `Indeterminate` must paint deterministically for CheckBox and Switch. RadioButton uses its native boolean `Checked` contract.
- `BootstrapSwitch` preserves inherited `ThreeState`/`CheckState` rather than forcing a Boolean-only model. In indeterminate state the thumb is centered and the track uses the effective accent with a visually distinct indeterminate glyph/state.
- V1 state transitions are immediate; no thumb/check animation and no new `AnimationDuration` API. A later additive plan may add animation using shared `BootstrapAnimation` after the state/keyboard contract is stable.
- Preserve native `AutoCheck`. When `AutoCheck = false`, mouse/keyboard activation must not mutate checked state; custom painting must reflect caller-controlled state only.
- Preserve native RadioButton same-parent exclusivity and arrow/Space behavior. Never manually uncheck sibling radio controls.
- Preserve standard WinForms focus and Tab behavior. Do not make labels or internal render helpers separate focus targets.
- Preserve `UseMnemonic` and keyboard cues. Text painting must respect mnemonic visibility rather than always drawing or always hiding ampersands.
- Respect `CheckAlign`, `TextAlign`, `RightToLeft`, `Padding`, `AutoEllipsis`, and host-assigned bounds in framework-owned layout. Tiny/malformed client rectangles must never produce negative drawing rectangles or throw.
- Do not silently rewrite inherited caller-owned `FlatStyle`, `Appearance`, `Image`, `ImageList`, or `TextImageRelation`. V1 custom form-check rendering targets normal text-label usage; image/button-appearance rendering is explicitly non-goal behavior and must be documented rather than emulated through a second Button renderer.
- If inherited `Appearance == Appearance.Button`, checked state/events must still work and painting must not throw, but V1 does not promise BootstrapButton visual parity. Do not add a second public appearance mode to compensate.
- Runtime theme changes must repaint and update only framework-owned theme font resources. Caller-assigned `Font` remains caller-owned and must survive theme changes/disposal.
- Designer construction is parameterless and requires no application bootstrap or initialized global state.
- Every owned GDI object is disposed deterministically; no per-paint cached `Graphics`, `Brush`, `Pen`, `GraphicsPath`, or bitmap may leak.
- No component creates an independent WinForms timer.
- The controls are not automatically added to `BootstrapInputGroup` in this plan. Checkbox/radio Input Group addons remain a separate future composition decision.
- Every new public/protected member requires XML documentation.
- The public API fingerprint must fail intentionally after adding these controls and must be updated only after the exact exported surface is reviewed.
- No TODO/TBD placeholders, prototype namespaces, aliases, duplicate theme infrastructure, or target-specific public APIs may remain at completion.

---

## Reference Behavior and WinForms Adaptation

### Bootstrap behaviors to preserve conceptually

- CheckBox displays a square indicator with a check mark when selected.
- Indeterminate CheckBox displays a distinct mixed-state mark.
- RadioButton displays a circular indicator with a filled center when selected.
- Switch presents checkbox semantics with a pill track and movable thumb.
- Disabled state is visually muted and non-interactive according to the native control's `Enabled` behavior.
- Validation can present success/danger semantic emphasis.
- Focus remains visible and keyboard interaction remains first-class.

### Deliberate native WinForms adaptations

- Native `CheckBox` / `RadioButton` state and events are not reimplemented.
- Radio grouping follows WinForms parent-container rules rather than a framework group model.
- `ThreeState` and `CheckState` remain available on CheckBox/Switch because they are useful desktop semantics even where a typical Bootstrap sample is Boolean.
- `CheckAlign`, `TextAlign`, `RightToLeft`, and host layout are desktop concerns and remain supported.
- There is no CSS class engine, DOM `role="switch"`, browser pseudo-state, or web transition runtime.
- Accessibility uses native WinForms control semantics; custom painting must not replace the underlying accessible checked state.
- Switch visual motion is deferred in V1 so checked-state notification and painting remain deterministic without adding a transition lifecycle to a primitive input.

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
- `Variant = Primary` and `ValidationState = None` on all three controls.
- Undefined `BootstrapVariant` and `BootstrapValidationState` values are rejected before mutation/invalidation.
- Existing `BootstrapValidationState` remains exactly `None`, `Valid`, `Invalid`; do not add enum values.
- Existing `BootstrapVariant` remains the shared semantic variant enum; do not add a check-specific variant enum.
- All checked/radio state properties and events remain inherited; the new classes do not redeclare them.
- `BootstrapSwitch` intentionally remains a `CheckBox` subtype so existing WinForms APIs accepting CheckBox/Control continue to work.
- The only declared public instance properties added by each class are `Variant` and `ValidationState`.
- Protected overrides needed for owner painting/lifecycle are reviewed as exported subclass surface before the API fingerprint is updated.

---

## Shared Internal Rendering Contract

Create one internal pure render helper rather than three independent visual algorithms:

```csharp
internal enum BootstrapCheckableKind
{
    CheckBox,
    RadioButton,
    Switch
}

internal readonly struct BootstrapCheckableMetrics
{
    // DPI-scaled indicator/track/thumb/gap/stroke/radius/focus metrics.
}

internal readonly struct BootstrapCheckablePalette
{
    // Surface, border, accent, glyph/thumb, text, focus colors.
}

internal readonly struct BootstrapCheckableLayout
{
    public Rectangle IndicatorBounds { get; }
    public Rectangle TextBounds { get; }
    public Rectangle FocusBounds { get; }
}

internal static class BootstrapCheckableRenderLogic
{
    public static BootstrapCheckableMetrics ResolveMetrics(
        BootstrapThemeMetrics themeMetrics,
        BootstrapCheckableKind kind,
        int dpi);

    public static BootstrapCheckablePalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        BootstrapValidationState validationState,
        bool enabled,
        bool focused,
        bool hovered,
        bool pressed,
        CheckState checkState);

    public static BootstrapCheckableLayout ResolveLayout(
        Rectangle clientBounds,
        Size textSize,
        Padding padding,
        ContentAlignment checkAlign,
        ContentAlignment textAlign,
        RightToLeft rightToLeft,
        BootstrapCheckableMetrics metrics);

    public static Size ResolvePreferredSize(
        Size textSize,
        Padding padding,
        BootstrapCheckableKind kind,
        BootstrapCheckableMetrics metrics);
}
```

Exact internal struct fields may be adjusted during implementation, but the following boundaries are mandatory:

- The helper is internal and has no dependency on concrete controls, handles, `Graphics`, static `BootstrapThemeManager`, or timers.
- Palette resolution uses `BootstrapVariantColorResolver` and existing theme colors; no Bootstrap hex values are embedded in the controls.
- Geometry uses `DpiScaler` and existing rendering helpers; no duplicate DPI utility is introduced.
- Layout saturates/clamps malformed/tiny geometry to non-negative contained rectangles.
- Render logic takes `CheckState` so CheckBox/Switch mixed state is testable without creating a WinForms handle.
- RadioButton callers pass only Unchecked/Checked.
- Focus color comes from the existing theme Focus token; validation changes semantic accent rather than inventing a second focus system.
- Disabled text uses `MutedText`/disabled basis and is independent from semantic Variant.
- Glyph/thumb color uses existing contrast selection when drawn on an accent surface.
- Layout handles RTL and `CheckAlign`/`TextAlign` explicitly rather than assuming indicator-left/text-right forever.

---

## File Structure and Responsibilities

### New product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs`
  - Internal kind, palette, metrics, layout, preferred-size calculations, validation, and pure geometry/state resolution.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs`
  - Native CheckBox subclass; theme/font lifecycle; mouse/key visual-state tracking; square/mixed-state custom paint; preferred-size integration.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs`
  - Native RadioButton subclass; theme/font lifecycle; mouse/key visual-state tracking; circular custom paint while preserving native grouping/navigation.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs`
  - Native CheckBox subclass; theme/font lifecycle; mouse/key visual-state tracking; track/thumb/mixed-state custom paint.

### New tests

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableRenderLogicTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckBoxTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapRadioButtonTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSwitchTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableInteractionTests.cs`

### Demo/docs/API files to modify

- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs`.
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` to expose a **Checks / Radios / Switches** demo page.
- Extend the relevant tests under `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/` for demo navigation/smoke coverage.
- Modify `README.md` control list/basic examples.
- Modify `docs/COMPONENTS.md` with final contracts for all three controls.
- Modify `docs/ARCHITECTURE.md` to add the checkable primitive family and shared internal render dependency.
- Modify `docs/TESTING.md` with pure and STA coverage matrices.
- Modify `docs/DEVELOPMENT_PLAN.md` deferred/expansion roadmap as appropriate.
- Modify `CHANGELOG.md` under the current unreleased/release-candidate section.
- Modify `docs/PUBLIC_API_BASELINE.md` only after API review.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` only after reviewing the intentional fingerprint change.

---

## Task 1 — Lock the pure rendering, palette, and geometry contract

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableRenderLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeColors`, `BootstrapThemeMetrics`, `BootstrapVariant`, `BootstrapValidationState`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, `Padding`, `ContentAlignment`, `RightToLeft`, `CheckState`.
- Produces: internal palette/metrics/layout/preferred-size calculations used identically by all three public controls.

- [ ] **Step 1: Write failing pure tests for metric resolution.**
  - 96 DPI baseline: CheckBox/Radio indicator = 16px, text gap = 8px, border = 1px, focus width = 2px.
  - Switch 96 DPI baseline: track = 32x16px and thumb is contained by the track after inset/stroke.
  - Scale 96/120/144/168/192 DPI through `DpiScaler`.
  - Radius behavior: CheckBox uses scaled `RadiusSmall` clamped to half indicator size; Radio is circular; Switch track is pill radius = half track height.
  - Null theme inputs, undefined kind, and non-positive DPI are rejected before usable output is returned.

- [ ] **Step 2: Write failing pure tests for palette precedence.**
  - Every semantic `BootstrapVariant` under Light/Dark.
  - `ValidationState.None` uses Variant accent.
  - `Valid` overrides Variant with Success.
  - `Invalid` overrides Variant with Danger.
  - Disabled presentation is independent from Variant/Validation and uses muted/disabled tokens.
  - Checked surface/border use effective accent and glyph/thumb foreground uses contrast helper.
  - Unchecked surface stays theme Surface and border uses neutral theme border with hover/pressed emphasis resolved from existing theme tokens.
  - Focus uses the theme Focus token without mutating checked/validation state.
  - Undefined Variant/Validation enum values are rejected.

- [ ] **Step 3: Write failing pure tests for CheckState visuals.**
  - Unchecked, Checked, Indeterminate produce distinct render state.
  - Radio path never relies on Indeterminate.
  - Switch Unchecked resolves thumb at start, Checked at end, Indeterminate at center.
  - RTL mirrors start/end geometry where appropriate without changing `CheckState`.

- [ ] **Step 4: Write failing pure tests for content layout/preferred size.**
  - Default indicator-left/text-right layout.
  - Indicator-right layout.
  - Top/Middle/Bottom + Left/Center/Right `CheckAlign` combinations remain contained.
  - `TextAlign` is honored inside remaining text space.
  - `RightToLeft.Yes` produces deterministic mirrored content.
  - Empty text, long text, padding, zero/tiny/malformed client rectangles.
  - Preferred height reserves the larger of text line and indicator/track plus focus allowance/padding.
  - Switch preferred width uses track width rather than CheckBox indicator width.

- [ ] **Step 5: Run the new test fixture and confirm it fails because the helper does not exist.**

Run on modern target:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckableRenderLogicTests"
```

Expected: compile/type failure before implementation.

- [ ] **Step 6: Implement minimal pure render logic.**
  - Keep all helper types internal.
  - Use `DpiScaler` and theme tokens only.
  - Reuse `BootstrapVariantColorResolver`/`ColorUtil`.
  - Use guarded arithmetic and contained rectangles.
  - Do not reference concrete checkable controls.

- [ ] **Step 7: Run the fixture on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckableRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckableRenderLogicTests"
```

Expected: PASS on both.

- [ ] **Step 8: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckableRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableRenderLogicTests.cs
git commit -m "feat: add checkable rendering logic"
```

---

## Task 2 — Implement `BootstrapCheckBox` on native CheckBox semantics

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckBoxTests.cs`

**Interfaces:**
- Consumes: Task 1 `BootstrapCheckableRenderLogic`, existing Theme/Rendering/DPI lifecycle patterns.
- Produces: `BootstrapCheckBox : CheckBox` with only `Variant` and `ValidationState` as new public properties.

- [ ] **Step 1: Write failing public-contract/default tests.**
  - Type derives directly from `System.Windows.Forms.CheckBox`.
  - Parameterless construction is Designer-safe.
  - `Variant == Primary`; `ValidationState == None`.
  - Exactly two declared public instance properties are added: `Variant`, `ValidationState`.
  - No redeclared `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, `CheckedChanged`, `CheckStateChanged`, `CheckAlign`, or `TextAlign`.
  - Undefined Variant/Validation assignment throws before mutation.
  - Native accessibility role/state remains CheckBox-compatible.

- [ ] **Step 2: Write failing native-state forwarding tests.**
  - `Checked` and `CheckState` use inherited storage.
  - `ThreeState = true` supports Indeterminate.
  - Effective programmatic state change raises inherited events with normal native counts.
  - Same-value assignment is a native no-op where applicable.
  - `AutoCheck = false` prevents user activation from changing state while programmatic assignment remains valid.

- [ ] **Step 3: Implement theme/font lifecycle and styles.**
  - Enable owner painting, double buffering, transparent-background support, resize redraw, and selectable behavior without replacing native input processing.
  - Subscribe once to `BootstrapThemeManager.ThemeChanged`; detach on disposal.
  - Follow existing caller-owned/theme-owned font pattern.
  - Theme changes invalidate/recompute preferred rendering without mutating Checked/CheckState.

- [ ] **Step 4: Implement visual-state tracking without an input engine.**
  - Track only `_hovered` / `_pressed` presentation flags from mouse/key/capture/focus callbacks.
  - Always call base methods so native CheckBox owns activation and event ordering.
  - Space key may affect `_pressed` presentation, but state mutation is never done by framework code.
  - Clear transient pressed state on key up, mouse up, capture loss, disable, focus loss where appropriate.

- [ ] **Step 5: Implement painting.**
  - Resolve metrics/palette/layout from Task 1.
  - Square indicator + border; Checked draws a simple framework-owned vector check mark; Indeterminate draws a centered bar.
  - Use anti-aliasing only for indicator/glyph geometry; use `TextRenderer` for label text.
  - Respect `UseMnemonic`, keyboard cue visibility, `AutoEllipsis`, `CheckAlign`, `TextAlign`, `RightToLeft`, `Padding`.
  - Do not use `CheckBoxRenderer`, OS visual-style colors, or hard-coded Bootstrap hex values.
  - Dispose all temporary GDI objects.

- [ ] **Step 6: Override preferred-size/DPI/theme-sensitive hooks only as required.**
  - `GetPreferredSize(Size)` uses Task 1 calculations and caller font/text/padding.
  - Parent-DPI change invalidates/recalculates layout.
  - Host-assigned fixed size remains respected; no forced AutoSize policy.

- [ ] **Step 7: Add paint smoke and lifecycle tests.**
  - Light/Dark; all variants; Valid/Invalid; Enabled/Disabled.
  - Unchecked/Checked/Indeterminate.
  - focus, hover, pressed transitions.
  - 96/120/144/168/192 synthetic render metrics plus real control paint smoke.
  - repeated theme changes and disposal without unusable caller font or duplicate subscriptions.
  - `Appearance.Button` and image-related inherited settings do not throw; document that V1 does not promise themed button/image composition.

- [ ] **Step 8: Run focused tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckBoxTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckBoxTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
```

- [ ] **Step 9: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckBoxTests.cs
git commit -m "feat: add bootstrap checkbox"
```

---

## Task 3 — Implement `BootstrapRadioButton` without breaking native grouping

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapRadioButtonTests.cs`

**Interfaces:**
- Consumes: shared Task 1 render logic.
- Produces: native RadioButton subclass with framework presentation only.

- [ ] **Step 1: Write failing contract/default tests.**
  - Direct `RadioButton` inheritance.
  - Designer-safe constructor.
  - `Variant = Primary`, `ValidationState = None`.
  - Only those two properties are newly declared publicly.
  - No framework sibling/group collection API exists.

- [ ] **Step 2: Characterize native grouping before custom behavior.**
  - Put two/three `BootstrapRadioButton` instances under the same parent Panel/Form.
  - Selecting one unselects the previously selected sibling exactly as native RadioButton does.
  - Radios under different parent containers do not affect each other.
  - `AutoCheck = false` preserves caller-controlled state.
  - `CheckedChanged` counts match native peer behavior for effective changes.

- [ ] **Step 3: Implement owner paint/theme/font lifecycle using the same pattern as CheckBox.**
  - Circular indicator outer border.
  - Checked state draws a centered circular dot using effective accent.
  - Unchecked state remains neutral surface/border.
  - Focus/hover/pressed/disabled/validation resolve through shared palette.
  - No custom radio-group coordination and no manual sibling traversal for checked state.

- [ ] **Step 4: Preserve native keyboard and focus.**
  - Do not override arrow-navigation/group-selection logic.
  - If key hooks are used for pressed visuals, call base and never consume the command.
  - Tab, Space, mnemonic activation, and native same-group arrow behavior remain reachable.

- [ ] **Step 5: Add alignment/RTL/DPI/paint/lifecycle tests.**
  - Indicator-left/right and representative top/bottom alignments.
  - `RightToLeft.Yes`.
  - Light/Dark; all semantic variants; Valid/Invalid; Disabled.
  - focus/hover/pressed.
  - 96/120/144/168/192 metrics and paint smoke.
  - repeated grouping/reparenting without framework event subscriptions between siblings.

- [ ] **Step 6: Run focused tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapRadioButtonTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapRadioButtonTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
```

- [ ] **Step 7: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapRadioButtonTests.cs
git commit -m "feat: add bootstrap radio button"
```

---

## Task 4 — Implement `BootstrapSwitch` as native CheckBox semantics with switch presentation

**Files:**
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSwitchTests.cs`

**Interfaces:**
- Consumes: shared Task 1 render logic.
- Produces: native CheckBox subtype whose indicator is rendered as a Bootstrap-like switch track/thumb.

- [ ] **Step 1: Write failing contract/default tests.**
  - Direct `CheckBox` inheritance.
  - `Variant = Primary`, `ValidationState = None`.
  - Only those two properties are newly declared publicly.
  - Inherited `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, and events remain available and are not redeclared.

- [ ] **Step 2: Write failing state-semantic tests.**
  - Unchecked/Checked use native CheckBox state/event ordering.
  - `ThreeState = true` supports Indeterminate.
  - `AutoCheck = false` prevents framework/user activation from changing caller-owned state.
  - No extra event caused by paint/theme/DPI/hover updates.

- [ ] **Step 3: Implement theme/font/interaction lifecycle.**
  - Same ownership rules as CheckBox/RadioButton.
  - No animation/timer.
  - Theme/DPI changes reposition thumb immediately from current CheckState.

- [ ] **Step 4: Implement switch painting.**
  - Track = DPI-scaled 32x16 logical pixels from Task 1 metrics.
  - Track is pill-shaped.
  - Unchecked thumb sits at logical start on neutral track.
  - Checked thumb sits at logical end on effective accent track.
  - Indeterminate thumb is centered with distinct mixed-state cue and effective accent.
  - RTL mirrors start/end positions without changing Checked semantics.
  - Focus ring surrounds track without clipping; label layout uses the same shared content logic as other checkables.
  - Do not draw text inside the track in V1.

- [ ] **Step 5: Add paint/preferred-size/DPI/lifecycle tests.**
  - All three CheckState values.
  - all Variants in Light/Dark.
  - Valid/Invalid, disabled, hover/pressed/focus.
  - RTL and indicator-right alignment.
  - preferred width proves Switch reserves 32px track rather than 16px checkbox slot.
  - repeated rapid programmatic state changes settle exactly on final thumb position with inherited event counts and no animation state.

- [ ] **Step 6: Run focused tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSwitchTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSwitchTests|FullyQualifiedName~BootstrapCheckableRenderLogicTests"
```

- [ ] **Step 7: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSwitchTests.cs
git commit -m "feat: add bootstrap switch"
```

---

## Task 5 — Harden keyboard, focus, radio grouping, accessibility, RTL, and lifecycle across the family

**Files:**
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableInteractionTests.cs`
- Modify the three new controls only where failing tests prove shared interaction defects.

**Interfaces:**
- Consumes: completed native-backed controls.
- Produces: regression coverage proving custom painting did not replace native interaction semantics.

- [ ] **Step 1: Add STA keyboard tests against native peer behavior.**
  - CheckBox: Tab focus then Space toggles once and raises normal event counts.
  - Switch: same Space semantics as CheckBox.
  - RadioButton: Space and same-parent arrow navigation/selection behave like native peers.
  - Shift+Tab exits normally.
  - Alt/mnemonic handling does not leave stale pressed state or trap focus.
  - `AutoCheck = false` prevents automatic state mutation for all applicable controls.

- [ ] **Step 2: Add mouse/capture regression tests.**
  - Mouse down/up inside activates through native path once.
  - Mouse down then drag/release outside does not leave pressed visuals stuck.
  - Disable, hide, dispose, capture loss, and focus loss clear transient visual flags safely.
  - No duplicate `CheckedChanged`/`CheckStateChanged` caused by framework state tracking.

- [ ] **Step 3: Add radio grouping/reparenting regression tests.**
  - Three radios in one parent remain mutually exclusive.
  - Moving a radio to another parent changes native grouping naturally without framework bookkeeping.
  - Disposing one radio does not alter siblings except through normal native state rules.
  - No static group subscription/registry exists.

- [ ] **Step 4: Add accessibility assertions.**
  - CheckBox/Switch accessible checked state follows inherited native state.
  - RadioButton accessible checked state follows inherited native state.
  - `AccessibleName`/`AccessibleDescription` remain caller-assignable and are not overwritten on theme changes.
  - Custom painting does not create hidden focusable child controls.

- [ ] **Step 5: Add RTL/alignment/font stress tests.**
  - `RightToLeft.Yes` with default and indicator-right alignment.
  - long labels, empty labels, custom fonts, caller padding, fixed tiny sizes.
  - caller-owned font remains usable after repeated Light/Dark switching and control disposal.

- [ ] **Step 6: Run the entire new checkable test family on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapCheckable|FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapSwitch"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapCheckable|FullyQualifiedName~BootstrapCheckBox|FullyQualifiedName~BootstrapRadioButton|FullyQualifiedName~BootstrapSwitch"
```

- [ ] **Step 7: Commit.**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCheckableInteractionTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCheckBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapRadioButton.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSwitch.cs
git commit -m "test: harden checkable interactions"
```

---

## Task 6 — Add integrated demo coverage and manual verification matrix

**Files:**
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify/add relevant tests under `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/`

- [ ] **Step 1: Add failing demo-navigation/smoke test if the current demo test harness supports page discovery.**
  - Main demo exposes one **Checks / Radios / Switches** entry.
  - Opening the page creates all three control types without exceptions.

- [ ] **Step 2: Implement a compact desktop demo page.**
  - CheckBox section: Unchecked, Checked, Indeterminate (`ThreeState = true`), Valid, Invalid, Disabled, AutoCheck=false sample.
  - Radio section: at least one same-parent group with 3 options demonstrating native exclusivity, plus a second separate group proving parent-container isolation.
  - Switch section: Off, On, Indeterminate, Valid, Invalid, Disabled, AutoCheck=false sample.
  - Variant section: show all eight semantic variants for at least one representative checked control without creating an excessively large page.
  - Include a live label/counter for inherited `CheckedChanged`/`CheckStateChanged` so state behavior is observable.
  - Use framework layout controls/native WinForms layout only; no special demo-only behavior inside product controls.

- [ ] **Step 3: Ensure global demo theme switching applies live.**
  - Light/Dark changes update every checkable without recreating them.
  - Caller-selected checked states remain unchanged across theme switches.

- [ ] **Step 4: Manual keyboard/focus checks.**
  - Tab/Shift+Tab through the page.
  - Space toggles CheckBox/Switch.
  - Radio keyboard movement remains native.
  - Mnemonics work when labels include `&` and do not strand pressed visuals after Alt.

- [ ] **Step 5: Manual DPI checks on real Windows scaling.**
  - 100%, 125%, 150%, 175%, 200%.
  - Check mark/radio dot/switch thumb stay centered.
  - focus ring is not clipped.
  - labels remain aligned and no glyph is stretched/blurry due to bitmap scaling.

- [ ] **Step 6: Commit.**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo
git commit -m "demo: showcase check radio and switch controls"
```

---

## Task 7 — Document component contracts and review the public API baseline

**Files:**
- Modify `README.md`
- Modify `docs/COMPONENTS.md`
- Modify `docs/ARCHITECTURE.md`
- Modify `docs/TESTING.md`
- Modify `docs/DEVELOPMENT_PLAN.md`
- Modify `CHANGELOG.md`
- Modify `docs/PUBLIC_API_BASELINE.md`
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

- [ ] **Step 1: Update component contracts before approving the fingerprint.**
  - State direct native inheritance for each control.
  - Record only `Variant` and `ValidationState` as additive public properties.
  - Document inherited state/event authority and radio grouping.
  - Document CheckBox/Switch indeterminate behavior.
  - Document V1 non-goals: animation, custom colors/sizes/radius, button appearance parity, image composition, InputGroup integration.

- [ ] **Step 2: Update architecture/testing/development docs.**
  - Add the three checkable primitives and shared internal render helper to architecture.
  - Add pure render + STA interaction matrices to TESTING.
  - Move/add check/radio/switch in the post-foundation expansion roadmap without disturbing historical phase order.

- [ ] **Step 3: Update README/CHANGELOG.**
  - Add control availability and minimal usage examples.
  - Mention native checked/radio semantics, semantic Variant, ValidationState, Light/Dark, and dual-target support.

- [ ] **Step 4: Run the existing API fingerprint test and intentionally observe failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL and print the reconstructed exported API plus actual fingerprint.

- [ ] **Step 5: Review the exported surface before changing the approved hash.**
  - Confirm exactly three new exported classes.
  - Confirm each class declares only `Variant` and `ValidationState` publicly plus the minimum required protected overrides.
  - Confirm no internal render/palette/metrics/layout kind leaks as exported type.
  - Confirm `BootstrapValidationState` and `BootstrapVariant` did not gain members.
  - Confirm no inherited state member was accidentally redeclared.
  - Confirm `AssemblyVersion` remains `1.0.0.0`.

- [ ] **Step 6: Add an explicit release test for the reviewed contract.**
  - Assert the three type names are exported.
  - Assert their direct base types.
  - Assert declared public property/event/method sets.
  - Assert internal `BootstrapCheckable*` implementation types are not exported.

- [ ] **Step 7: Update `ApprovedV1Fingerprint` to the reviewed actual SHA-256 and record it in `docs/PUBLIC_API_BASELINE.md`.**
  - Do not compute or guess the hash in this plan; use the test output from the completed implementation.

- [ ] **Step 8: Re-run all Phase16 public API baseline tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: PASS on both.

- [ ] **Step 9: Commit.**

```bash
git add README.md CHANGELOG.md docs/COMPONENTS.md docs/ARCHITECTURE.md docs/TESTING.md docs/DEVELOPMENT_PLAN.md docs/PUBLIC_API_BASELINE.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: finalize checkable control contracts"
```

---

## Task 8 — Full dual-target verification and final hardening

**Files:**
- No planned product changes; fix only defects exposed by the verification matrix.

- [ ] **Step 1: Build the solution in Release.**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

Expected: PASS; both `net48` and `net8.0-windows` product/test targets compile.

- [ ] **Step 2: Run the complete test suite for `net48`.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --no-build
```

Expected: PASS.

- [ ] **Step 3: Run the complete test suite for `net8.0-windows`.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --no-build
```

Expected: PASS.

- [ ] **Step 4: Run the integrated demo and execute the manual matrix.**
  - Light and Dark.
  - all semantic variants.
  - Unchecked/Checked/Indeterminate.
  - Valid/Invalid/Disabled.
  - hover/pressed/focus.
  - Tab/Shift+Tab/Space/Radio arrows/mnemonics/Alt.
  - `AutoCheck=false`.
  - same-parent and separate-parent radio groups.
  - RTL representative samples.
  - 100/125/150/175/200% real Windows DPI.
  - repeated theme changes and rapid programmatic state changes.

- [ ] **Step 5: Inspect lifecycle/resource ownership.**
  - Theme subscription removed on dispose.
  - Theme-owned fonts disposed/replaced safely.
  - caller fonts/accessibility metadata remain caller-owned.
  - no timer, bitmap cache, hidden child control, radio registry, or global event hook.
  - no GDI object survives paint scope unless explicitly owned and disposed by the control.

- [ ] **Step 6: Scan for prohibited scope creep.**
  - No new package dependency.
  - No new public size/radius/color/group APIs.
  - No animation scheduler usage.
  - No InputGroup modification.
  - No `Math.Clamp` or other unguarded runtime API incompatible with `net48`.
  - No duplicated semantic color resolver.

- [ ] **Step 7: Commit any verification-only fixes as focused commits, then require a clean final working tree.**

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- `BootstrapCheckBox`, `BootstrapRadioButton`, and `BootstrapSwitch` exist under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Each directly inherits the corresponding native WinForms control and retains native checked state/events/keyboard behavior.
- RadioButton same-parent grouping is native and no framework radio registry/group engine exists.
- CheckBox/Switch support native Unchecked/Checked/Indeterminate semantics where `ThreeState` allows them.
- All three expose only `Variant` and `ValidationState` as new V1 public properties.
- Variant/validation/disabled precedence is consistent and theme-token based.
- Check/radio/switch geometry is DPI-aware at 100–200% and tiny geometry never produces negative rectangles/exceptions.
- Light/Dark switches repaint live without state loss or caller-font corruption.
- Keyboard, mnemonics, focus, RTL, alignment, and accessibility remain usable.
- Shared pure `BootstrapCheckableRenderLogic` owns palette/metrics/layout calculations; no three-way copy/paste rendering engine is introduced.
- No new external dependency, timer, animation engine, message hook, second checked-state model, or public base class is introduced.
- Demo page covers representative states and native radio grouping.
- Public API fingerprint is deliberately reviewed and updated only after exact exported-surface inspection.
- Full `net48` and `net8.0-windows` Release builds/tests pass.
- Documentation and changelog reflect the final behavior and V1 non-goals.

## Recommended Execution Order

Execute Tasks 1–8 sequentially. Task 1 establishes the pure shared rendering contract; Tasks 2–4 add each native-backed control independently; Task 5 protects cross-control interaction semantics; Task 6 makes manual behavior observable; Task 7 freezes the documented/public contract; Task 8 is the final dual-target gate. Do not update the public API fingerprint early merely to make CI green.