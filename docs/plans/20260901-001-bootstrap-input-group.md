# BootstrapInputGroup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap 5.3-inspired `BootstrapInputGroup` composition control that connects text/addon surfaces, native-backed framework inputs, buttons, and split buttons into one coherent horizontal input surface with shared size, seam, corner, focus, theme, DPI, keyboard, validation, demo, documentation, and reviewed public API behavior.

**Architecture:** Treat Input Group as a composition container, not as prefix/suffix properties on `BootstrapTextBox`. Generalize the existing internal connected-button geometry into reusable connected-control infrastructure, then let each supported primitive expose an internal composition-only presentation contract for per-corner geometry, Small/Default/Large sizing, and safe minimum-height measurement without mutating caller-owned public properties. `BootstrapInputGroup` owns canonical child order, two-pass connected measurement/layout, size overrides, seam overlap, visible-first/last corner assignment, and visual stacking. Canonical order follows caller/designer `Controls` operations including `SetChildIndex`; any internal z-order changes used only for active-border stacking must be isolated and must not rewrite that canonical order. Child controls remain responsible for native editing/selection/command behavior and their own validation state.

**Tech Stack:** C#, native Windows Forms `Panel` / `Control`, existing Theme / Rendering / Icons / DPI infrastructure, `CornerRadius`, `RoundedPath`, `DpiScaler`, `BootstrapTextBox`, `BootstrapFormattedTextBox`, `BootstrapNumericBox`, `BootstrapSelect`, `BootstrapButton`, `BootstrapSplitButton`, NUnit 4, STA WinForms interaction tests, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** This plan formalizes the approved 2026-09-01 Input Group analysis and must remain consistent with `AGENTS.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/PUBLIC_API_BASELINE.md`, and the Bootstrap 5.3 Input Group behavior reference at `https://getbootstrap.com/docs/5.3/forms/input-group/`. Bootstrap is a behavior/visual reference only; this remains a native WinForms implementation.

## Global Constraints

- Keep root namespace `MyDmsVn.Bootstrap5WinFormUI`; public Input Group types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product, tests, and demo must build from shared code for both `net48` and `net8.0-windows`.
- Input Group is a composition container. Do **not** add `PrefixText`, `SuffixText`, `LeftAddon`, or `RightAddon` properties to `BootstrapTextBox`.
- Do **not** introduce Bootstrap CSS/JS, WebView, DOM abstractions, or a new package dependency.
- Reuse Theme, Rendering, Icons, DPI, and existing primitive controls. Do not copy connected-border/radius algorithms into another helper.
- Preserve all standalone public semantics of child controls. Group composition must not rewrite caller-owned `BorderRadius`, `ButtonSize`, `ValidationState`, selection/value/text, `Enabled`, `ReadOnly`, `TabIndex`, or other public state.
- Composition-only corner and size adjustments are internal nullable overrides and must be cleared when a child leaves the group.
- All `IBootstrapConnectedControl` members on public controls must be implemented **explicitly**. They must never appear as public properties/methods on `BootstrapButton`, `BootstrapTextBox`, `BootstrapNumericBox`, `BootstrapSelect`, `BootstrapSplitButton`, or `BootstrapInputGroupText`.
- Group height is resolved in two passes: apply/resolve connected size context, measure every visible child's safe minimum height, then assign one common row height equal to the maximum of the theme target and all safe minimums. A native-backed child must never resize itself against the group during the final layout pass.
- `BootstrapInputGroup` itself is non-focusable (`TabStop = false`). Addon text is non-focusable. Interactive child controls preserve their normal Tab/Shift+Tab, Enter/Space, editing, popup, and accessibility behavior.
- Validation remains child-owned. Do not add `BootstrapInputGroup.ValidationState`.
- V1 is horizontal and non-wrapping. Do not add public `Orientation`, `Wrap`, `FillWeight`, responsive-breakpoint, or row-layout APIs.
- V1 supports multiple addons, multiple stretch inputs, multiple buttons, and mixed supported children.
- Stretch inputs share remaining width equally after fixed-width children and seam overlap are accounted for. No public weighting API in v1.
- Width allocation must define deterministic behavior even when the client width is smaller than the sum of child soft minimum widths; no negative or out-of-client rectangles are permitted.
- Visible children define first/middle/last connected geometry. Invisible children must not leave stale rounded corners or gaps.
- Canonical connected order follows caller/designer `Control.Controls` ordering, including `Controls.SetChildIndex(...)`. Visual z-order used for seam stacking is a separate implementation concern and must not change canonical order.
- `InputGroupSize` is group-owned and uses existing theme metrics: Small → `ControlHeightSmall`/`RadiusSmall`, Default → `ControlHeight`/`Radius`, Large → `ControlHeightLarge`/`RadiusLarge`.
- Group sizing must not mutate `BootstrapButton.ButtonSize` or equivalent caller-owned child size properties.
- Runtime theme and DPI changes re-resolve target height, safe minimum height, radius, padding, seam overlap, addon preferred widths, and child overrides.
- Focused, pressed, and hovered child borders must remain visibly continuous at seams. Do not use an order-dependent `BringToFront()` implementation that also changes logical layout order. If z-order is required, use guarded internal stacking with deterministic state priority and independently stored canonical order.
- Unsupported arbitrary WinForms controls are outside v1. Fail fast with a clear exception during addition rather than silently producing a visually broken group.
- `BootstrapComboBox`, `BootstrapDatePicker`, standalone `BootstrapDropdown`, checkbox/radio addons, file input, and arbitrary controls are deferred unless an implementation task below explicitly promotes them after tests prove the existing contract can support connected corners safely.
- `BootstrapSelect` is supported in V1 only when `SelectionMode == BootstrapSelectMode.Single` while connected. A Multiple-mode Select is rejected on addition, and changing a connected Select to Multiple must fail before mutation with a clear exception. This avoids conflicting with the Input Group's one-row height contract without silently discarding multi-row selection content.
- Every pure geometry/sizing helper gets ordinary unit tests. Focus, Tab, popup, child add/remove/reorder, theme/DPI, hover/press stacking, and state transitions get STA tests where applicable.
- Every new public/protected member requires XML documentation.
- The public API fingerprint must intentionally fail before approval. Update `docs/PUBLIC_API_BASELINE.md` and the approved hash only after reviewing the exact exported surface.
- No placeholders, TODOs, temporary aliases, prototype namespaces, or duplicated infrastructure may remain at completion.

---

## Reference Behavior and Deliberate WinForms Adaptation

### Bootstrap behaviors to preserve conceptually

- Text/addon content may appear before or after an input.
- Multiple addons and multiple inputs may coexist.
- Buttons and segmented/split buttons may be connected directly to inputs.
- Small/default/large sizing is applied at the group level.
- Connected children visually share seams, with only the outermost corners rounded.
- Focus and validation belong to the actual form control rather than to a synthetic group value.

### Deliberate WinForms adaptations

- V1 does not wrap. A desktop data-entry field must not unexpectedly split into multiple rows when its host resizes.
- The group itself is not a tab stop; native/foundation child controls retain keyboard ownership.
- Layout uses explicit child bounds instead of CSS flexbox.
- Group-level sizing uses internal presentation overrides rather than modifying public child properties.
- Native-backed controls participate in an explicit safe-minimum-height measurement phase before final bounds are assigned.
- Caller/designer `Controls` ordering defines the canonical connected sequence. Internal active-border z-order changes, if needed, are not allowed to redefine that sequence.
- `BootstrapSelect` Multiple mode is deliberately deferred for grouped use because its current multi-row chip contract conflicts with a one-row Input Group.
- No web-only ARIA/CSS mechanisms are copied; WinForms accessibility roles/names remain on the actual child controls.
- No group validation API is added because multiple child inputs can have independent states.

---

## Public Contract to Implement

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapInputGroupSize
{
    Small,
    Default,
    Large
}

public class BootstrapInputGroup : Panel
{
    public BootstrapInputGroupSize InputGroupSize { get; set; }
}

[DefaultProperty(nameof(Text))]
public class BootstrapInputGroupText : Control
{
    public override string Text { get; set; }
    public IconDescriptor? Icon { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer { get; set; }

    public ContentAlignment TextAlign { get; set; }
    public int BorderRadius { get; set; }

    public override Size GetPreferredSize(Size proposedSize);
}
```

Contract rules:

- `BootstrapInputGroup.InputGroupSize` defaults to `Default`.
- `BootstrapInputGroup` defaults `TabStop = false`, transparent background, horizontal non-wrapping layout, and a useful designer-safe width with current default theme height.
- `BootstrapInputGroupText.Text` normalizes null to empty.
- `BootstrapInputGroupText.Icon` defaults null; `IconRenderer` uses the framework default and rejects null.
- `BootstrapInputGroupText.TextAlign` defaults `MiddleCenter`.
- `BootstrapInputGroupText.BorderRadius = -1` uses the current theme radius when standalone. Values below `-1` throw before mutation.
- Addon surface uses theme `SurfaceSecondary`, text/muted/disabled tokens, shared border token, current Body font, and DPI-scaled spacing.
- Input Group adds no public collection wrapper. Normal `Control.Controls` remains the composition surface.
- `BootstrapInputGroup` may override protected WinForms plumbing such as `CreateControlsInstance()` to enforce admission/order semantics. Such overrides are not new convenience APIs, but because protected members participate in the repository API fingerprint they must be included in the final API review.
- Adding an unsupported direct child throws `NotSupportedException` with the child type in the message and leaves the previous parent/order and group state consistent.
- V1 direct supported children:
  - `BootstrapInputGroupText`
  - `BootstrapTextBox` (therefore `BootstrapFormattedTextBox`)
  - `BootstrapNumericBox`
  - `BootstrapSelect` only while `SelectionMode == BootstrapSelectMode.Single`
  - `BootstrapButton`
  - `BootstrapSplitButton`
- `BootstrapButtonGroup` is not a direct child contract in v1; multiple buttons are added directly.
- The group may contain one child. That child receives all four outer corners and the selected group size.
- `Controls.SetChildIndex(...)` is a supported caller/designer reorder operation and immediately changes canonical connected order and corresponding first/middle/last radii.
- When a child is hidden or removed, connected geometry is recalculated using the remaining visible supported children.
- When a child leaves the group, all internal connected presentation overrides are cleared synchronously.

---

## Internal Connected-Control Contract

Create an internal neutral presentation contract rather than coupling primitives to another primitive's public size enum:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal enum BootstrapConnectedControlSize
{
    Small,
    Default,
    Large
}

internal interface IBootstrapConnectedControl
{
    CornerRadius? ConnectedCornerRadius { get; set; }
    BootstrapConnectedControlSize? ConnectedSizeOverride { get; set; }
    int GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize size, int dpi);
}
```

Rules:

- Both properties are implementation-only and nullable; the measurement method is implementation-only.
- Public controls implement all three members **explicitly**. Backing fields/helpers remain private/internal. Reflection over public instance members must not expose `ConnectedCornerRadius`, `ConnectedSizeOverride`, or `GetConnectedSafeMinimumHeight`.
- Null means standalone behavior.
- `GetConnectedSafeMinimumHeight(...)` is pure with respect to public state: it may inspect theme/font/native preferred metrics but must not assign public size properties or mutate selection/value/text.
- `BootstrapButtonGroup` sets only `ConnectedCornerRadius`; it does not set a size override.
- `BootstrapInputGroup` applies connected size context, queries safe minimum height for every visible child, resolves the common row height, then applies final bounds.
- A connected size override changes effective visual metrics only while grouped. Public `ButtonSize`, `BorderRadius`, etc. remain unchanged.
- `BootstrapNumericBox.GetConnectedSafeMinimumHeight(...)` must include the native `NumericUpDown.PreferredHeight` floor so the final group row is tall enough before `LayoutEditor()` runs.
- During a final grouped layout pass, `BootstrapNumericBox` must not mutate its own outer `Height`; the group has already resolved a safe row height.
- `BootstrapSelect` connected support is Single-mode only in V1. Its measurement is therefore the collapsed one-row surface only.
- `BootstrapSplitButton` maps the outer connected radius into its two internal button regions while preserving their internal square seam.
- `BootstrapFormattedTextBox` inherits the connected behavior from `BootstrapTextBox`; it does not implement a second copy.
- Helpers used only to compute effective connected metrics remain internal.

---

## Canonical Order and Visual Stacking Contract

`BootstrapInputGroup` must separate **canonical connected order** from any temporary **visual z-order** used to keep active borders visible.

- Normal `Controls.Add`, `Controls.AddRange`, `Controls.Remove`, `Controls.RemoveAt`, `Controls.Clear`, reparenting, and `Controls.SetChildIndex` are the caller/designer composition API.
- Override `CreateControlsInstance()` and return a private/internal `Control.ControlCollection` subclass if required to make admission and order updates atomic.
- Admission validation occurs before committing a child to the collection. Rejected children must remain with their previous parent and must not receive connected overrides/subscriptions.
- The custom collection synchronizes a canonical sequence for caller-driven add/remove/reorder operations.
- `SetChildIndex` must update canonical sequence and trigger corner/layout recomputation.
- If internal active-border stacking changes child indices, it must run under an internal guard so the custom collection does not interpret that change as caller-driven canonical reordering.
- Layout, stretch allocation, first/middle/last geometry, and RTL mirroring always use canonical order filtered by `Visible`; they never infer semantic order from temporary z-order.
- Tests must prove `A,B,C → SetChildIndex(C,0) → C,A,B`, then move focus/hover/press among children and verify canonical order remains `C,A,B`.

---

## File Structure and Responsibilities

### New product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupSize.cs`
  - Public Small/Default/Large enum.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupText.cs`
  - Non-focusable addon text/icon primitive.
  - Standalone and connected painting, preferred size, theme/font/DPI lifecycle.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroup.cs`
  - Public container, supported-child validation, canonical child ordering, override ownership, two-pass measurement/layout, visual stacking, lifecycle, theme/DPI response.
  - Private/internal custom `ControlCollection` implementation when needed for atomic add/reorder semantics.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupLayoutLogic.cs`
  - Pure fixed/stretch width allocation, compressed-width policy, and connected bounds calculation.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/IBootstrapConnectedControl.cs`
  - Internal composition presentation and safe-height measurement seam.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapConnectedControlSize.cs`
  - Internal neutral size override.

### Product files to rename/modify

- Rename `Controls/BootstrapConnectedButtonLayoutLogic.cs` → `Controls/BootstrapConnectedControlLayoutLogic.cs`.
- Modify `BootstrapButtonGroup.cs` and `BootstrapSplitButton.cs` to consume the generalized helper.
- Modify `BootstrapButton.cs` to implement the internal connected contract explicitly without changing public properties.
- Modify `BootstrapTextBox.cs` to use effective connected corners/size for shell radius, control height, padding, icon layout, and safe-height measurement.
- Modify `BootstrapNumericBox.cs` and `BootstrapNumericBoxRenderLogic.cs` to use effective connected corners/size and expose safe native minimum measurement while retaining one native `NumericUpDown`.
- Modify `BootstrapSelect.cs` and `BootstrapSelectRenderLogic.cs` so Single-mode connected size/radius can be honored without changing popup/search/selection semantics; reject Multiple mode while connected before mutation.
- Modify `Controls/Internal/BootstrapSelectSelectionLayout.cs` only as needed to replace default collapsed-height assumptions with the effective connected height for Single mode. Do not introduce grouped multi-row behavior in this plan.
- Do not modify `BootstrapFormattedTextBox` except if compile/tests reveal a derived-class integration defect.
- Do not modify `BootstrapComboBox` or `BootstrapDatePicker` in this plan's MVP.

### New/renamed tests

- Rename `BootstrapConnectedButtonLayoutLogicTests.cs` → `BootstrapConnectedControlLayoutLogicTests.cs`.
- Add `BootstrapInputGroupLayoutLogicTests.cs`.
- Add `BootstrapInputGroupTextTests.cs`.
- Add `BootstrapInputGroupTests.cs`.
- Add `BootstrapInputGroupInteractionTests.cs`.
- Extend `BootstrapButtonTests.cs`, `BootstrapButtonGroupTests.cs`, `BootstrapSplitButtonTests.cs`, `BootstrapTextBoxTests.cs`, `BootstrapNumericBoxTests.cs`, `BootstrapNumericBoxRenderLogicTests.cs`, `BootstrapSelectTests.cs`, and `BootstrapSelectVisualRegressionTests.cs` only for connected overrides/regressions.
- Add `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/InputGroupDemoFormTests.cs`.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs`.

### Demo/docs

- Add `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/InputGroupDemoForm.cs`.
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`.
- Modify `docs/COMPONENTS.md`, `docs/TESTING.md`, `docs/PUBLIC_API_BASELINE.md`, `docs/PACKAGE_README.md`, `README.md`, `CHANGELOG.md`.
- Add a short usage/behavior document `docs/BOOTSTRAP_INPUT_GROUP.md` and link it from `docs/README.md`.

---

## Task 1 — Generalize connected geometry without behavior change

**Files:**
- Create/rename: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedControlLayoutLogic.cs`
- Delete after migration: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedButtonLayoutLogic.cs`
- Rename: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapConnectedControlLayoutLogicTests.cs`
- Modify: `BootstrapButtonGroup.cs`
- Modify: `BootstrapSplitButton.cs`

- [ ] **Step 1: Write/rename the regression tests first.**
  - Resolve the helper by the new internal type name.
  - Preserve DPI seam cases at 96/120/144/168/192 DPI.
  - Preserve horizontal and vertical first/middle/last/single corner cases.
  - Add invalid orientation/index/count/radius/DPI tests if absent.

- [ ] **Step 2: Run the focused tests and confirm failure.**
  - `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapConnectedControlLayoutLogicTests"`
  - Expected: fail because the new helper type does not exist.

- [ ] **Step 3: Rename/generalize the helper.**
  - Keep `ResolveSeamOverlap(BootstrapThemeMetrics, int dpi)`.
  - Keep orientation-aware `ResolveCornerRadius(...)`.
  - Do not add public surface.

- [ ] **Step 4: Migrate ButtonGroup and SplitButton.**
  - Replace all old helper references.
  - Preserve exact ButtonGroup layout/selection semantics and SplitButton internal seam behavior.

- [ ] **Step 5: Run focused regression tests for helper, ButtonGroup, SplitButton.**
  - Both target frameworks must pass.

- [ ] **Step 6: Commit.**
  - `git commit -m "refactor: generalize connected control geometry"`

---

## Task 2 — Introduce internal connected presentation and measurement overrides

**Files:**
- Create `Controls/Internal/IBootstrapConnectedControl.cs`
- Create `Controls/Internal/BootstrapConnectedControlSize.cs`
- Modify `BootstrapButton.cs`
- Modify `BootstrapSplitButton.cs`
- Modify `BootstrapTextBox.cs`
- Modify `BootstrapNumericBox.cs`
- Modify `BootstrapNumericBoxRenderLogic.cs`
- Modify `BootstrapSelect.cs`
- Modify `BootstrapSelectRenderLogic.cs`
- Modify `Controls/Internal/BootstrapSelectSelectionLayout.cs` only if Single-mode connected collapsed metrics require it
- Tests listed above

- [ ] **Step 1: Add failing reflection/behavior tests for the internal contract.**
  - Verify the interface/type is not exported.
  - For every public supported primitive, verify `GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)` does **not** expose `ConnectedCornerRadius` or `ConnectedSizeOverride`.
  - Verify `GetMethods(...)` does **not** expose `GetConnectedSafeMinimumHeight`.
  - Verify controls implement `IBootstrapConnectedControl` through explicit interface members.
  - Verify standalone controls have null connected overrides through internal-test access/reflection.
  - Verify setting an override changes effective corner geometry/preferred height but leaves public `BorderRadius`/`ButtonSize` unchanged.
  - Verify clearing overrides restores standalone metrics exactly.
  - Verify safe-minimum measurement does not mutate public size/value/selection properties.

- [ ] **Step 2: Run focused tests and confirm failure.**

- [ ] **Step 3: Implement `BootstrapButton`.**
  - Replace the button-specific internal group-radius field/property with private backing fields for the neutral connected contract.
  - Implement `IBootstrapConnectedControl` explicitly.
  - Effective button size maps internal Small/Default/Large to existing `BootstrapButtonSize` only for rendering/preferred-size calculations.
  - Safe minimum height is the DPI-scaled effective theme/button height needed to render without clipping.
  - Never assign the public `ButtonSize`.

- [ ] **Step 4: Update ButtonGroup.**
  - Access connected members through `IBootstrapConnectedControl`.
  - Set only connected corners.
  - Leave connected size null.
  - Removal clears only the overrides ButtonGroup owns.
  - Existing public behavior/tests must stay green.

- [ ] **Step 5: Implement `BootstrapTextBox`.**
  - Implement the internal interface explicitly.
  - Connected radius takes precedence over public standalone radius while non-null.
  - Connected size chooses Small/Default/Large height, radius basis, and horizontal padding.
  - Safe minimum height accounts for the native inner TextBox/font metrics and shell border/padding.
  - Preserve native inner TextBox, placeholder, clear button, icon slots, IME, clipboard, key forwarding, and public single tab stop.

- [ ] **Step 6: Implement `BootstrapNumericBox` with a non-fighting measurement/layout path.**
  - Implement the internal interface explicitly.
  - Extend pure metrics resolution to accept effective size/radius inputs rather than hard-coding default metrics.
  - `GetConnectedSafeMinimumHeight(size, dpi)` returns `max(themeTargetHeight, nativeEditorPreferredHeight + required shell insets)` without assigning `Height`.
  - Refactor `LayoutEditor()` so the standalone path may still enforce its existing safe floor, but when a connected size override is active it trusts the parent-resolved outer height and **must not write to outer `Height`**.
  - Preserve native editor semantics and do not change `Value`, range, spin, keyboard, wheel, or read-only policy.
  - Add regression tests proving repeated group layout does not oscillate or recursively resize when the native preferred height exceeds the selected Small target.

- [ ] **Step 7: Implement `BootstrapSelect` for Single-mode grouping only.**
  - Implement the internal interface explicitly.
  - Connected corners feed the current focus-aware border metric path.
  - Connected size replaces hard-coded default collapsed-height assumptions used by Single-mode selection layout with passed/effective control-height metrics.
  - Safe minimum height describes only the collapsed Single-mode surface.
  - Adding a `BootstrapSelect` whose `SelectionMode == Multiple` to an InputGroup is rejected before collection mutation.
  - While a connected size override is active, assigning `SelectionMode = Multiple` throws before changing `_selectionMode` or selection state. Clearing/reparenting the control restores normal standalone mode changes.
  - Do not change popup creation, overlay ownership, remote/local search, selection identity, or Tab continuation logic.
  - Keep existing standalone Multiple-mode tests green.

- [ ] **Step 8: Implement `BootstrapSplitButton`.**
  - Implement the internal interface explicitly.
  - Outer connected radius determines only the split control's outer-left/outer-right corners.
  - Internal primary/menu seam remains square.
  - Connected size flows to both internal region buttons without assigning public `ButtonSize`.
  - Safe minimum height is the maximum safe height required by the two internal regions.

- [ ] **Step 9: Run all affected control tests on both TFMs.**

- [ ] **Step 10: Commit.**
  - `git commit -m "refactor: add connected control presentation overrides"`

---

## Task 3 — Implement `BootstrapInputGroupText`

**Files:**
- Create `BootstrapInputGroupText.cs`
- Create `BootstrapInputGroupTextTests.cs`

- [ ] **Step 1: Write failing contract tests.**
  - Defaults, attributes, null normalization, invalid radius, non-focusable behavior.
  - Exact declared public surface.
  - Explicit internal-interface implementation is not exposed publicly.
  - Preferred width with text only, icon only, icon + text, empty content.
  - Small/Default/Large connected override height/padding/radius and safe minimum height.
  - Light/Dark and disabled palette.
  - DPI 96/120/144/168/192.

- [ ] **Step 2: Run tests and confirm compile/type failure.**

- [ ] **Step 3: Implement addon rendering.**
  - Owner-painted `Control`, transparent-capable/double-buffered.
  - `SurfaceSecondary` background, normal border, theme Body font.
  - `TextRenderer` for text; shared `IIconRenderer` for icon.
  - Temporary GDI objects disposed per paint; theme-owned font lifecycle matches existing controls.
  - `AccessibleRole = AccessibleRole.StaticText`, `TabStop = false`.

- [ ] **Step 4: Implement connected override/measurement support explicitly.**
  - Standalone uses public `BorderRadius`.
  - Group override is internal and non-serialized.
  - Implement `IBootstrapConnectedControl` explicitly so connected members do not expand public API.
  - Safe minimum height is the effective group target needed for text/icon vertical alignment without clipping.

- [ ] **Step 5: Run tests on both TFMs.**

- [ ] **Step 6: Commit.**
  - `git commit -m "feat: add input group text addon"`

---

## Task 4 — Implement pure Input Group layout logic

**Files:**
- Create `BootstrapInputGroupLayoutLogic.cs`
- Create `BootstrapInputGroupLayoutLogicTests.cs`

Model each visible child as an internal immutable layout input containing preferred width, `bool Stretch`, and soft minimum width. Return ordered rectangles plus preferred group width/height. The pure helper receives a final common row height already resolved by the container; it never measures Controls itself.

### Width-allocation policy

For `N` visible children and seam overlap `S`, compute `allocationBudget = max(0, clientWidth + S * (N - 1))`; child widths are allocated against that budget because adjacent rectangles overlap by `S` pixels.

1. **Natural/surplus mode:** start fixed children at preferred width and stretch children at soft minimum width. If budget remains, distribute all surplus equally across stretch children; if there are no stretch children, leave trailing visual space rather than mutating fixed preferred widths.
2. **Fixed-compression mode:** if the budget is below `sum(fixedPreferred) + sum(stretchMinimum)` but at least `sum(allMinimum)`, keep stretch children at minimum and shrink only fixed children from preferred toward their minimum, proportionally to each fixed child's shrink capacity. Resolve integer remainders deterministically from canonical logical start to end.
3. **Emergency compressed mode:** if the budget is below `sum(allMinimum)`, soft minimums are allowed to compress. Allocate the available budget proportionally to each child's soft minimum width using deterministic largest-remainder distribution, preserving canonical order for ties. Widths may reach zero but never become negative.
4. The emitted rectangles must remain within the client span after seam overlap. No rectangle may extend before logical start or after logical end.

- [ ] **Step 1: Write failing pure tests for:**
  - single fixed child;
  - addon + one stretch input;
  - prefix + input + suffix;
  - two stretch inputs sharing remaining width;
  - input + two fixed buttons;
  - mixed addon/input/button/split button;
  - seam subtraction/overlap across N children;
  - natural surplus allocation;
  - fixed compression between preferred and minimum widths;
  - emergency compression one pixel below/equal/above total soft minimum;
  - several stretch children with different soft minima;
  - deterministic integer remainder distribution;
  - fixed-only constrained width;
  - zero/one-pixel client width without negative/out-of-client rectangles;
  - empty/zero client size;
  - visible-child list already filtered by caller;
  - RTL mirroring if `RightToLeft.Yes` is supported by the container.

- [ ] **Step 2: Run and confirm failure.**

- [ ] **Step 3: Implement deterministic allocation exactly as specified above.**
  - Use checked/guarded arithmetic where seam/budget calculations could underflow.
  - Pure helper has no Control/theme/static application dependency.

- [ ] **Step 4: Run pure tests on both TFMs.**

- [ ] **Step 5: Commit.**
  - `git commit -m "feat: add input group layout logic"`

---

## Task 5 — Implement `BootstrapInputGroup` composition, canonical order, measurement, and lifecycle

**Files:**
- Create `BootstrapInputGroupSize.cs`
- Create `BootstrapInputGroup.cs`
- Create `BootstrapInputGroupTests.cs`

- [ ] **Step 1: Write failing public-contract/default tests.**
  - Enum exactly Small/Default/Large.
  - `InputGroupSize = Default`.
  - `TabStop = false`.
  - no public validation/wrap/orientation/fill-weight APIs.
  - exact new declared public surface is intentionally small.
  - Any required protected overrides such as `CreateControlsInstance()` are explicitly included in the expected protected API review rather than accidentally discovered at Task 9.

- [ ] **Step 2: Write failing child admission and atomicity tests.**
  - All supported types add successfully.
  - Derived `BootstrapFormattedTextBox` is accepted through TextBox support.
  - Single-mode `BootstrapSelect` is accepted.
  - Multiple-mode `BootstrapSelect` is rejected before collection mutation.
  - Native `TextBox`, native `Button`, `BootstrapComboBox`, `BootstrapDatePicker`, arbitrary `Panel`, and `BootstrapButtonGroup` are rejected in v1.
  - Rejection leaves no stale canonical-order entry, override, or event subscription.
  - Rejected child retains its previous parent and previous index when the attempted reparent/add operation fails.

- [ ] **Step 3: Implement canonical collection/order ownership.**
  - Prefer a private/internal `Control.ControlCollection` subclass returned by `CreateControlsInstance()` so validation happens before committing unsupported children.
  - Maintain an internal canonical sequence synchronized with caller/designer Add/Remove/Clear/reparent operations.
  - Override/handle `SetChildIndex` in the custom collection so caller-driven reorder changes canonical order immediately.
  - Add an internal stacking guard for any visual z-order change performed by InputGroup itself; guarded index changes must not rewrite canonical sequence.
  - Subscribe/unsubscribe only to layout/state-relevant child events (`VisibleChanged`, size/preferred-size-related signals, focus and mouse state as needed).
  - Do not add a second public ownership collection.

- [ ] **Step 4: Apply connected overrides.**
  - Map group size to internal connected size.
  - Resolve group radius from current theme size metric.
  - Use generalized first/middle/last connected radius for visible children in canonical order.
  - Hidden supported child keeps no active connected presentation if it is excluded from visual sequence.
  - Removed child clears both connected properties before detaching handlers.

- [ ] **Step 5: Implement two-pass row-height measurement.**
  - Resolve the DPI-scaled theme target height from `InputGroupSize`.
  - Query `GetConnectedSafeMinimumHeight(groupSize, dpi)` for every visible supported child without assigning final bounds.
  - Resolve `rowHeight = max(themeTargetHeight, allVisibleSafeMinimumHeights)`; empty groups use the theme target height.
  - The resolved row height is the only outer height assigned to visible children during the final layout pass.
  - Native-backed child layout code must not increase its own outer Height while connected.
  - Add a recursion/reentrancy guard so preferred-size notifications caused by applying overrides do not create a layout loop.

- [ ] **Step 6: Implement width layout using the pure helper.**
  - Fixed children: InputGroupText, Button, SplitButton.
  - Stretch children: TextBox/FormattedTextBox, NumericBox, Single-mode Select.
  - Build preferred/minimum inputs in canonical visible order.
  - Use the Task 4 natural/compressed allocation policy.
  - Assign every visible child the resolved common row height.
  - `GetPreferredSize` returns natural fixed/preferred width minus seams and resolved safe row height; a host-assigned wider Width gives stretch children remaining space.
  - A host-assigned narrower Width uses compressed mode without negative/out-of-client bounds.

- [ ] **Step 7: Implement runtime theme/DPI response.**
  - Theme size/radius changes and `InputGroupSize` changes reapply overrides, remeasure safe minimums, and relayout.
  - Parent DPI changes do the same.
  - No child public state mutation.

- [ ] **Step 8: Add canonical reorder tests.**
  - Add `A`, `B`, `C`; verify canonical/layout order `A,B,C`.
  - Call `Controls.SetChildIndex(C, 0)`; verify canonical/layout/corner order `C,A,B`.
  - Hide and re-show the middle child; verify visible corner assignment without losing canonical position.
  - Reparent a child out and back; verify override cleanup/reapplication and deterministic position.

- [ ] **Step 9: Run InputGroup + affected primitive tests on both TFMs.**

- [ ] **Step 10: Commit.**
  - `git commit -m "feat: add bootstrap input group container"`

---

## Task 6 — Harden focus, hover/press seam stacking, keyboard traversal, visibility, reorder, and RTL

**Files:**
- Create `BootstrapInputGroupInteractionTests.cs`
- Modify `BootstrapInputGroup.cs`
- Extend visual tests only where needed

- [ ] **Step 1: Add STA regression tests before behavior code.**
  - TextBox → Button → next external control with Tab.
  - Reverse with Shift+Tab.
  - Two stretch inputs preserve normal tab sequence.
  - SplitButton's two internal focusable regions remain reachable according to existing SplitButton contract.
  - Select popup Tab closes/continues according to existing Select behavior rather than being trapped by InputGroup.
  - Alt does not change group layout/focus state.
  - Disabled/non-tab-stop child is skipped naturally by WinForms.
  - Hiding first/middle/last recomputes corners immediately.
  - Removing focused child clears overrides and leaves remaining group usable.
  - Caller `SetChildIndex` changes canonical order; subsequent focus/hover/press stacking does not change it again.

- [ ] **Step 2: Add focused/validation border visual regression.**
  - Place focused TextBox/NumericBox/Select adjacent to addon/button.
  - Render at 96/120/144/168/192 DPI.
  - Assert focus-colored top/bottom/outer edge and seam pixels remain inside client bounds and are not erased by neighboring normal surfaces.
  - Repeat for invalid/valid validation colors to ensure child validation still determines palette.

- [ ] **Step 3: Add hover/pressed seam visual regression.**
  - Place normal and outline `BootstrapButton` adjacent to TextBox/addon and exercise Normal → Hover → Pressed → Hover → Normal.
  - Exercise both primary and dropdown regions of `BootstrapSplitButton` when adjacent to an input.
  - Verify the hovered/pressed border is not clipped by a neighboring normal child at 96/120/144/168/192 DPI.
  - Verify no stale active seam remains after mouse leave/up, disable, hide, or removal.

- [ ] **Step 4: Implement deliberate visual stacking only if the failing visuals prove it necessary.**
  - Keep canonical logical order independent from z-order.
  - Use one isolated guarded method for temporary visual stacking.
  - State priority is `Focused > Pressed > Hovered > Normal`; validation affects the child's color/palette but does not silently reorder canonical layout.
  - If two children have the same state priority, use canonical order as the deterministic tie-break.
  - Do not mutate `TabIndex`.
  - Restore deterministic normal stacking when active state leaves the group.
  - Add tests proving layout order/corners remain unchanged as focus/mouse state moves.

- [ ] **Step 5: Add RTL behavior.**
  - If `RightToLeft.Yes`, mirror visual placement and outer-corner assignment while preserving canonical Controls/Tab order.
  - Add pure + STA tests. If current repo policy decides RTL is deferred, document that explicitly and remove RTL code/tests rather than shipping partial mirroring.

- [ ] **Step 6: Run interaction/visual regression suites on both TFMs.**

- [ ] **Step 7: Commit.**
  - `git commit -m "test: harden input group focus and seams"`

---

## Task 7 — Add integrated demo coverage

**Files:**
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/InputGroupDemoForm.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/InputGroupDemoFormTests.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs`

Demo sections must include at least:

1. `[@] [Username]`
2. `[Username] [@example.com]`
3. `[$] [BootstrapNumericBox] [.00]`
4. `[Name] [First] [Last]`
5. `[BootstrapTextBox] [Search button]`
6. `[BootstrapTextBox] [Button] [Button]`
7. `[BootstrapInputGroupText] [BootstrapSelect Single]`
8. `[Card] [BootstrapFormattedTextBox]`
9. `[BootstrapTextBox] [BootstrapSplitButton]`
10. Small / Default / Large comparison
11. child valid / invalid / disabled state
12. hide/show middle addon to exercise visible-child corner recomputation
13. runtime reorder using `Controls.SetChildIndex` to demonstrate canonical-order recomputation
14. a narrow-width host example showing deterministic compression without negative/out-of-client bounds

- [ ] **Step 1: Write demo contract tests first.**
  - New navigation entry exists.
  - Selecting it embeds `InputGroupDemoForm`.
  - Demo contains expected InputGroup count and representative child types.
  - Select examples are Single mode only.
  - All examples remain inside client bounds after normal and narrow layout.

- [ ] **Step 2: Run tests and confirm failure.**

- [ ] **Step 3: Implement demo using existing shared page/layout patterns.**
  - No demo-only feature flags in product code.
  - Include concise manual keyboard/DPI/hover/press/reorder instructions in the form.

- [ ] **Step 4: Run demo tests on both TFMs.**

- [ ] **Step 5: Commit.**
  - `git commit -m "demo: add bootstrap input group scenarios"`

---

## Task 8 — Document the finalized component contract

**Files:**
- Create `docs/BOOTSTRAP_INPUT_GROUP.md`
- Modify `docs/README.md`
- Modify `docs/COMPONENTS.md`
- Modify `docs/TESTING.md`
- Modify `README.md`
- Modify `docs/PACKAGE_README.md`
- Modify `CHANGELOG.md`

- [ ] **Step 1: Document public API and supported child matrix.**
  - Explain composition via `Controls` and caller/designer reorder via `Controls.SetChildIndex`.
  - Explain InputGroupSize.
  - Explain two-pass safe-minimum-height measurement and common row height.
  - Explain stretch vs fixed children and the deterministic narrow-width compression policy.
  - Explain child-owned validation/focus.
  - Explain unsupported/deferred child types.
  - State clearly that grouped `BootstrapSelect` supports Single mode only in V1; standalone Multiple mode remains unchanged.
  - Explain that connected overrides never mutate public child state and are implemented internally/explicitly.
  - Include code samples for prefix/suffix, currency, two inputs, button, Single Select, SplitButton, and reorder.

- [ ] **Step 2: Document verification matrix.**
  - Light/Dark.
  - 100/125/150/175/200% DPI.
  - Small/Default/Large including a native-height floor case.
  - Tab/Shift+Tab/Enter/Space.
  - Select popup behavior and grouped Multiple-mode rejection.
  - hidden child / removal / reparent / SetChildIndex reorder.
  - validation/disabled.
  - button/split-button hover/pressed seams.
  - constrained-width compression.
  - designer construction.

- [ ] **Step 3: Update release-facing lists and changelog.**
  - Do not claim Multiple Select, ComboBox/DatePicker/file/checkbox/radio support inside InputGroup.

- [ ] **Step 4: Commit.**
  - `git commit -m "docs: document bootstrap input group"`

---

## Task 9 — Review and approve the public API baseline

**Files:**
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify `docs/PUBLIC_API_BASELINE.md`

Expected intentional exported type additions are limited to:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroup
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupSize
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupText
```

No connected-control helper/interface/size override/layout type may be exported. Any protected overrides declared by the two new public controls are part of those types' reviewed API fingerprint and must be intentional.

- [ ] **Step 1: Add a dedicated Input Group API contract assertion.**
  - Verify enum values.
  - Verify declared public properties/methods/events for both new controls.
  - Verify any required protected overrides such as `CreateControlsInstance()` are exactly the intended ones.
  - Assert internal helper names are absent from `GetExportedTypes()`.
  - Assert public supported primitives do not expose `ConnectedCornerRadius`, `ConnectedSizeOverride`, or `GetConnectedSafeMinimumHeight` as public/protected members.

- [ ] **Step 2: Run release baseline test before changing the approved fingerprint.**
  - Expected: `ExportedApiMatchesApprovedV1Baseline` fails and prints the actual reconstructed API/fingerprint.

- [ ] **Step 3: Review the printed API manually against this plan.**
  - Reject accidental public/protected helper members or implicit interface implementations.
  - Confirm existing exported signatures did not change except intentional behavior-internal implementation details that do not alter signatures.
  - Confirm `AssemblyVersion` stays `1.0.0.0`.

- [ ] **Step 4: Only after review, update the approved fingerprint and `docs/PUBLIC_API_BASELINE.md`.**

- [ ] **Step 5: Re-run release tests on both TFMs.**

- [ ] **Step 6: Commit.**
  - `git commit -m "chore: approve input group public api"`

---

## Task 10 — Full verification and self-review

- [ ] **Step 1: Run formatting/placeholder scan.**
  - Search modified/new files for `TODO`, `FIXME`, `NotImplementedException`, prototype namespaces, temporary aliases, debug drawing, and hard-coded Bootstrap semantic colors that should use theme tokens.

- [ ] **Step 2: Run the repository build gate.**
  - `powershell -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release`
  - This restores/builds product, tests, and demo for both `net48` and `net8.0-windows`.

- [ ] **Step 3: Run the full test gate.**
  - `powershell -ExecutionPolicy Bypass -File .\test.ps1 -Configuration Release -SkipBuild`
  - Both target frameworks must pass.

- [ ] **Step 4: Run focused Input Group tests once more without filters hidden by previous output.**
  - Connected geometry and explicit connected interface visibility
  - Safe-minimum-height measurement / NumericBox non-oscillation
  - InputGroupText
  - InputGroup layout including compression thresholds
  - InputGroup control / atomic child admission / SetChildIndex
  - InputGroup interaction / focus / hover / press stacking
  - affected Button/TextBox/NumericBox/Select/SplitButton
  - grouped Select Multiple-mode rejection plus standalone Multiple-mode regression
  - demo
  - release baseline

- [ ] **Step 5: Manual Windows verification.**
  - Launch demo for both target frameworks where practical.
  - Exercise all demo combinations with mouse and keyboard.
  - Tab forward/backward through grouped children and neighboring controls.
  - Activate Button/SplitButton with Enter/Space.
  - Hover/press buttons next to inputs and inspect seam continuity.
  - Open/close Single Select; verify Tab continuation and Escape.
  - Verify grouped Multiple Select is rejected clearly while standalone Multiple Select still works.
  - Switch Light/Dark repeatedly.
  - Resize host narrow/wide, including below total soft minimum width.
  - Hide/show a first/middle/last child.
  - Reorder children using `Controls.SetChildIndex` and verify connected corners/layout follow the new caller order.
  - Repeat at Windows 100/125/150/175/200% scaling.
  - Inspect 1px/2px seams, rounded outer corners, focused/hovered/pressed/valid/invalid borders, text/icon vertical alignment, and no stale pixels after resize/theme changes.
  - Exercise a Small group containing NumericBox and verify no native editor clipping or layout oscillation.
  - Open in Visual Studio Designer and verify parameterless construction/property serialization/reorder behavior.

- [ ] **Step 6: Self-review architecture.**
  - Confirm no duplicated connected-layout implementation.
  - Confirm all connected interface members on public controls are explicit and absent from exported API.
  - Confirm group never mutates public child size/radius/validation state.
  - Confirm row height is measured before bounds assignment and native children do not fight the parent height while connected.
  - Confirm caller `SetChildIndex` updates canonical order and internal visual stacking cannot rewrite it.
  - Confirm narrow-width allocation is deterministic and emits no negative/out-of-client rectangles.
  - Confirm grouped Select is Single-only and standalone Multiple behavior remains unchanged.
  - Confirm removal/hide/disposal clears internal overrides/subscriptions.
  - Confirm temporary fonts/pens/brushes/paths are disposed.
  - Confirm no target-specific public API divergence.
  - Confirm no unsupported control is claimed in docs/demo.
  - Confirm exported type additions are exactly the three intended Input Group types.

- [ ] **Step 7: Final commit if verification produced only cleanup/doc changes.**
  - `git commit -m "test: complete input group verification"`

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- `BootstrapInputGroup` and `BootstrapInputGroupText` are usable from normal WinForms code and Designer construction.
- Prefix, suffix, multiple addons, multiple stretch inputs, multiple buttons, Single Select, FormattedTextBox, NumericBox, and SplitButton scenarios render as one connected row.
- Small/Default/Large group sizing uses existing theme metrics and does not mutate child public size properties.
- The group resolves one common row height through safe-minimum measurement before assigning final child bounds; native-backed controls do not resize against the parent during grouped layout.
- A Small group containing `BootstrapNumericBox` never clips the native editor and does not oscillate/recursively resize.
- Only visible first/last children in canonical caller order keep outer radii; inner seams remain square and continuous at supported DPI scales.
- `Controls.SetChildIndex` immediately changes canonical connected order and geometry, while internal focus/hover/press stacking does not alter that order.
- Focused/hovered/pressed/valid/invalid child borders remain visible and do not create stale/doubled seam artifacts.
- Tab/Shift+Tab and each child control's native keyboard semantics remain intact; the group itself never becomes an extra tab stop.
- Child validation remains independent; there is no group validation state.
- Removing/hiding/reparenting children cannot leave stale connected corner/size overrides or event subscriptions.
- Unsupported child admission is atomic from the caller perspective; a rejected child does not lose its previous parent/order.
- Constrained-width layout follows the documented natural/fixed-compression/emergency-compression policy and never produces negative or out-of-client rectangles.
- Grouped `BootstrapSelect` supports Single mode only; Multiple mode is rejected before mutation, while standalone Multiple-mode behavior remains regression-free.
- Theme switching and DPI changes recalculate size, safe height, and geometry without reconstructing child controls or losing values/selections.
- Existing ButtonGroup and SplitButton connected rendering remains regression-free after helper generalization.
- Existing TextBox/FormattedTextBox/NumericBox/Select/Button/SplitButton standalone behavior is unchanged when not grouped.
- All connected presentation/measurement members on public controls are explicit internal-interface implementations and do not leak into public/protected API.
- Unsupported child types fail clearly; ComboBox/DatePicker/file/checkbox/radio/arbitrary-control support is not implied.
- Product, demo, and tests pass for `net48` and `net8.0-windows`.
- Integrated demo and manual DPI/keyboard/hover/press/reorder/compression matrix are present.
- Public API review approves only the intended Input Group exported types plus intentional inherited/protected override declarations on those new types; all connected infrastructure remains internal.
- Documentation and changelog accurately describe the implemented contract with no placeholders.

## Deferred Follow-ups

Plan separately if needed after MVP:

- grouped `BootstrapSelect` Multiple-mode one-row chip/overflow presentation;
- native `BootstrapComboBox` connected shell/Region integration;
- `BootstrapDatePicker` connected native HWND shell;
- direct `BootstrapDropdown` button-dropdown composition;
- checkbox/radio addon primitives;
- Bootstrap-like file input;
- arbitrary WinForms child adapter contract;
- vertical/wrapping input groups;
- public stretch weights;
- group-level custom radius;
- richer addon hosted content beyond text/icon;
- accessibility grouping metadata beyond the child-native baseline.
