# BootstrapInputGroup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap 5.3-inspired `BootstrapInputGroup` composition control that connects text/addon surfaces, native-backed framework inputs, buttons, and split buttons into one coherent horizontal input surface with shared size, seam, corner, focus, theme, DPI, keyboard, validation, demo, documentation, and reviewed public API behavior.

**Architecture:** Treat Input Group as a composition container, not as prefix/suffix properties on `BootstrapTextBox`. Generalize the existing internal connected-button geometry into reusable connected-control infrastructure, then let each supported primitive expose an internal composition-only presentation override for per-corner geometry and Small/Default/Large sizing without mutating its caller-owned public properties. `BootstrapInputGroup` owns child order, connected layout, size overrides, seam overlap, visible-first/last corner assignment, and focused-child visual stacking; child controls remain responsible for native editing/selection/command behavior and their own validation state.

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
- `BootstrapInputGroup` itself is non-focusable (`TabStop = false`). Addon text is non-focusable. Interactive child controls preserve their normal Tab/Shift+Tab, Enter/Space, editing, popup, and accessibility behavior.
- Validation remains child-owned. Do not add `BootstrapInputGroup.ValidationState`.
- V1 is horizontal and non-wrapping. Do not add public `Orientation`, `Wrap`, `FillWeight`, responsive-breakpoint, or row-layout APIs.
- V1 supports multiple addons, multiple stretch inputs, multiple buttons, and mixed supported children.
- Stretch inputs share remaining width equally after fixed-width children and seam overlap are accounted for. No public weighting API in v1.
- Visible children define first/middle/last connected geometry. Invisible children must not leave stale rounded corners or gaps.
- `InputGroupSize` is group-owned and uses existing theme metrics: Small → `ControlHeightSmall`/`RadiusSmall`, Default → `ControlHeight`/`Radius`, Large → `ControlHeightLarge`/`RadiusLarge`.
- Group sizing must not mutate `BootstrapButton.ButtonSize` or equivalent caller-owned child size properties.
- Runtime theme and DPI changes re-resolve target height, radius, padding, seam overlap, addon preferred widths, and child overrides.
- Focused child borders must remain visibly continuous at seams. Do not use an order-dependent `BringToFront()` implementation that also changes logical layout order. If z-order is used to emulate Bootstrap focus stacking, logical child order must be stored independently and tested.
- Unsupported arbitrary WinForms controls are outside v1. Fail fast with a clear exception during addition rather than silently producing a visually broken group.
- `BootstrapComboBox`, `BootstrapDatePicker`, standalone `BootstrapDropdown`, checkbox/radio addons, file input, and arbitrary controls are deferred unless an implementation task below explicitly promotes them after tests prove the existing contract can support connected corners safely.
- `BootstrapSelect` is supported for its normal collapsed surface. Group composition must not change selection/search/popup ownership; multiple-selection height remains governed by the current control contract and must be documented/tested rather than silently rewritten.
- Every pure geometry/sizing helper gets ordinary unit tests. Focus, Tab, popup, child add/remove, and theme/DPI behavior get STA tests.
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
- Visible-child ordering, not raw Controls z-order, defines the connected sequence.
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
- Adding an unsupported direct child throws `NotSupportedException` with the child type in the message and leaves the group in a consistent state.
- V1 direct supported children:
  - `BootstrapInputGroupText`
  - `BootstrapTextBox` (therefore `BootstrapFormattedTextBox`)
  - `BootstrapNumericBox`
  - `BootstrapSelect`
  - `BootstrapButton`
  - `BootstrapSplitButton`
- `BootstrapButtonGroup` is not a direct child contract in v1; multiple buttons are added directly.
- The group may contain one child. That child receives all four outer corners and the selected group size.
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
}
```

Rules:

- Both properties are implementation-only and nullable.
- Null means standalone behavior.
- `BootstrapButtonGroup` sets only `ConnectedCornerRadius`; it does not set a size override.
- `BootstrapInputGroup` sets both properties for every supported child.
- A connected size override changes effective visual metrics only while grouped. Public `ButtonSize`, `BorderRadius`, etc. remain unchanged.
- `BootstrapSplitButton` maps the outer connected radius into its two internal button regions while preserving their internal square seam.
- `BootstrapFormattedTextBox` inherits the connected behavior from `BootstrapTextBox`; it does not implement a second copy.
- Helpers used only to compute effective connected metrics remain internal.

---

## File Structure and Responsibilities

### New product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupSize.cs`
  - Public Small/Default/Large enum.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupText.cs`
  - Non-focusable addon text/icon primitive.
  - Standalone and connected painting, preferred size, theme/font/DPI lifecycle.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroup.cs`
  - Public container, supported-child validation, logical child ordering, override ownership, focus stacking, layout lifecycle, theme/DPI response.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapInputGroupLayoutLogic.cs`
  - Pure fixed/fill width allocation and connected bounds calculation.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/IBootstrapConnectedControl.cs`
  - Internal composition presentation seam.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapConnectedControlSize.cs`
  - Internal neutral size override.

### Product files to rename/modify

- Rename `Controls/BootstrapConnectedButtonLayoutLogic.cs` → `Controls/BootstrapConnectedControlLayoutLogic.cs`.
- Modify `BootstrapButtonGroup.cs` and `BootstrapSplitButton.cs` to consume the generalized helper.
- Modify `BootstrapButton.cs` to implement the internal connected contract without changing public properties.
- Modify `BootstrapTextBox.cs` to use effective connected corners/size for shell radius, control height, padding, and icon layout.
- Modify `BootstrapNumericBox.cs` and `BootstrapNumericBoxRenderLogic.cs` to use effective connected corners/size while retaining one native `NumericUpDown`.
- Modify `BootstrapSelect.cs`, `BootstrapSelectRenderLogic.cs`, and only the necessary selection-layout metrics so connected size/radius can be honored without changing popup/search/selection semantics.
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

## Task 2 — Introduce internal connected presentation overrides

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
- Modify `Controls/Internal/BootstrapSelectSelectionLayout.cs`
- Tests listed above

- [ ] **Step 1: Add failing reflection/behavior tests for the internal contract.**
  - Verify the contract is not exported.
  - Verify standalone controls have null connected overrides.
  - Verify setting an override changes effective corner geometry/preferred height but leaves public `BorderRadius`/`ButtonSize` unchanged.
  - Verify clearing overrides restores standalone metrics exactly.

- [ ] **Step 2: Run focused tests and confirm failure.**

- [ ] **Step 3: Implement `BootstrapButton`.**
  - Replace the button-specific internal group-radius field/property with the neutral connected corner property.
  - Add nullable connected size override.
  - Effective button size maps internal Small/Default/Large to existing `BootstrapButtonSize` only for rendering/preferred-size calculations.
  - Never assign the public `ButtonSize`.

- [ ] **Step 4: Update ButtonGroup.**
  - Set only connected corners.
  - Leave connected size null.
  - Removal clears only the overrides ButtonGroup owns.
  - Existing public behavior/tests must stay green.

- [ ] **Step 5: Implement `BootstrapTextBox`.**
  - Connected radius takes precedence over public standalone radius while non-null.
  - Connected size chooses Small/Default/Large height, radius basis, and horizontal padding.
  - Preserve native inner TextBox, placeholder, clear button, icon slots, IME, clipboard, key forwarding, and public single tab stop.

- [ ] **Step 6: Implement `BootstrapNumericBox`.**
  - Extend pure metrics resolution to accept effective size/radius inputs rather than hard-coding default metrics.
  - Preserve native editor semantics and do not change `Value`, range, spin, keyboard, wheel, or read-only policy.
  - Guard against the existing native preferred-height floor; Small must never clip the native editor. If the native editor requires more pixels on a runtime/font, effective control height is the max of theme target and safe native minimum and that behavior is characterized in tests.

- [ ] **Step 7: Implement `BootstrapSelect`.**
  - Connected corners feed the current focus-aware border metric path.
  - Connected size replaces the hard-coded default collapsed-height assumptions used by selection layout with a passed/effective control-height metric.
  - Do not change popup creation, overlay ownership, remote/local search, selection identity, or Tab continuation logic.
  - Test both Single and Multiple modes at connected sizes; document that group height remains one connected row and that multi-chip overflow follows existing Select overflow semantics.

- [ ] **Step 8: Implement `BootstrapSplitButton`.**
  - Outer connected radius determines only the split control's outer-left/outer-right corners.
  - Internal primary/menu seam remains square.
  - Connected size flows to both internal region buttons without assigning public `ButtonSize`.

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
  - Preferred width with text only, icon only, icon + text, empty content.
  - Small/Default/Large connected override height/padding/radius.
  - Light/Dark and disabled palette.
  - DPI 96/120/144/168/192.

- [ ] **Step 2: Run tests and confirm compile/type failure.**

- [ ] **Step 3: Implement addon rendering.**
  - Owner-painted `Control`, transparent-capable/double-buffered.
  - `SurfaceSecondary` background, normal border, theme Body font.
  - `TextRenderer` for text; shared `IIconRenderer` for icon.
  - Temporary GDI objects disposed per paint; theme-owned font lifecycle matches existing controls.
  - `AccessibleRole = AccessibleRole.StaticText`, `TabStop = false`.

- [ ] **Step 4: Implement connected override support.**
  - Standalone uses public `BorderRadius`.
  - Group override is internal and non-serialized.

- [ ] **Step 5: Run tests on both TFMs.**

- [ ] **Step 6: Commit.**
  - `git commit -m "feat: add input group text addon"`

---

## Task 4 — Implement pure Input Group layout logic

**Files:**
- Create `BootstrapInputGroupLayoutLogic.cs`
- Create `BootstrapInputGroupLayoutLogicTests.cs`

Model each visible child as internal immutable layout input containing preferred width, `bool Stretch`, and minimum width (internal constant/derived value, not public API). Return ordered rectangles plus preferred group width/height.

- [ ] **Step 1: Write failing pure tests for:**
  - single fixed child;
  - addon + one stretch input;
  - prefix + input + suffix;
  - two stretch inputs sharing remaining width;
  - input + two fixed buttons;
  - mixed addon/input/button/split button;
  - seam subtraction across N children;
  - constrained width without negative rectangles;
  - empty/zero client size;
  - visible-child list already filtered by caller;
  - RTL mirroring if `RightToLeft.Yes` is supported by the container.

- [ ] **Step 2: Run and confirm failure.**

- [ ] **Step 3: Implement deterministic allocation.**
  - Compute fixed total.
  - Subtract seam overlap exactly once between adjacent children.
  - Divide remaining width equally between stretch children.
  - Distribute integer remainder deterministically from logical start to end.
  - Never emit negative width/height.
  - Pure helper has no Control/theme/static application dependency.

- [ ] **Step 4: Run pure tests on both TFMs.**

- [ ] **Step 5: Commit.**
  - `git commit -m "feat: add input group layout logic"`

---

## Task 5 — Implement `BootstrapInputGroup` composition and lifecycle

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

- [ ] **Step 2: Write failing child admission tests.**
  - All supported types add successfully.
  - Derived `BootstrapFormattedTextBox` is accepted through TextBox support.
  - Native `TextBox`, native `Button`, `BootstrapComboBox`, `BootstrapDatePicker`, arbitrary `Panel`, and `BootstrapButtonGroup` are rejected in v1.
  - Rejection leaves no stale logical-order entry or override.

- [ ] **Step 3: Implement logical child ownership.**
  - Maintain an internal logical sequence independent from z-order.
  - Subscribe/unsubscribe only to layout-relevant child events (`VisibleChanged`, size/preferred-size-related signals, focus enter/leave as needed).
  - Do not add a second public ownership collection.

- [ ] **Step 4: Apply connected overrides.**
  - Map group size to internal connected size.
  - Resolve group radius from current theme size metric.
  - Use generalized first/middle/last connected radius for visible children.
  - Hidden supported child keeps no active connected presentation if it is excluded from visual sequence.
  - Removed child clears both connected properties before detaching handlers.

- [ ] **Step 5: Implement layout.**
  - Fixed children: InputGroupText, Button, SplitButton.
  - Stretch children: TextBox/FormattedTextBox, NumericBox, Select.
  - Group target height comes from selected theme control-height metric, DPI-scaled.
  - Set all visible child heights to effective target/safe minimum and pass widths to pure helper.
  - `GetPreferredSize` returns natural fixed/preferred width minus seams and group target height; a host-assigned wider Width gives stretch children remaining space.

- [ ] **Step 6: Implement runtime theme/DPI response.**
  - Theme size/radius changes and `InputGroupSize` changes reapply overrides + layout.
  - Parent DPI changes do the same.
  - No child public state mutation.

- [ ] **Step 7: Run InputGroup + affected primitive tests on both TFMs.**

- [ ] **Step 8: Commit.**
  - `git commit -m "feat: add bootstrap input group container"`

---

## Task 6 — Harden focus, seam stacking, keyboard traversal, visibility, and RTL

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

- [ ] **Step 2: Add focused-border visual regression.**
  - Place focused TextBox/NumericBox/Select adjacent to addon/button.
  - Render at 96/120/144/168/192 DPI.
  - Assert focus-colored top/bottom/outer edge and seam pixels remain inside client bounds and are not erased by neighboring normal surfaces.
  - Repeat for invalid/valid validation colors to ensure child validation still wins over neutral focus.

- [ ] **Step 3: Implement deliberate focus stacking only if the failing visual proves it necessary.**
  - Keep a separate logical child list for layout/corner semantics.
  - If z-order must change, use one isolated method and never derive layout order from `Controls.GetChildIndex`.
  - Do not mutate `TabIndex`.
  - Restore deterministic non-focused stacking when focus leaves the group.
  - Add tests proving layout order/corners do not change as focus moves.

- [ ] **Step 4: Add RTL behavior.**
  - If `RightToLeft.Yes`, mirror visual placement and outer-corner assignment while preserving logical Controls/Tab order.
  - Add pure + STA tests. If current repo policy decides RTL is deferred, document that explicitly and remove RTL code/tests rather than shipping partial mirroring.

- [ ] **Step 5: Run interaction/visual regression suites on both TFMs.**

- [ ] **Step 6: Commit.**
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
7. `[BootstrapInputGroupText] [BootstrapSelect]`
8. `[Card] [BootstrapFormattedTextBox]`
9. `[BootstrapTextBox] [BootstrapSplitButton]`
10. Small / Default / Large comparison
11. child valid / invalid / disabled state
12. hide/show middle addon to exercise visible-child corner recomputation

- [ ] **Step 1: Write demo contract tests first.**
  - New navigation entry exists.
  - Selecting it embeds `InputGroupDemoForm`.
  - Demo contains expected InputGroup count and representative child types.
  - All examples remain inside client bounds after layout.

- [ ] **Step 2: Run tests and confirm failure.**

- [ ] **Step 3: Implement demo using existing shared page/layout patterns.**
  - No demo-only feature flags in product code.
  - Include concise manual keyboard/DPI instructions in the form.

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
  - Explain composition via `Controls`.
  - Explain InputGroupSize.
  - Explain stretch vs fixed children.
  - Explain child-owned validation/focus.
  - Explain unsupported/deferred child types.
  - Explain that connected overrides never mutate public child state.
  - Include code samples for prefix/suffix, currency, two inputs, button, Select, SplitButton.

- [ ] **Step 2: Document verification matrix.**
  - Light/Dark.
  - 100/125/150/175/200% DPI.
  - Small/Default/Large.
  - Tab/Shift+Tab/Enter/Space.
  - Select popup behavior.
  - hidden child / removal.
  - validation/disabled.
  - designer construction.

- [ ] **Step 3: Update release-facing lists and changelog.**
  - Do not claim deferred ComboBox/DatePicker/file/checkbox/radio support.

- [ ] **Step 4: Commit.**
  - `git commit -m "docs: document bootstrap input group"`

---

## Task 9 — Review and approve the public API baseline

**Files:**
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify `docs/PUBLIC_API_BASELINE.md`

Expected intentional exported additions are limited to:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroup
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupSize
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapInputGroupText
```

No connected-control helper/interface/size override/layout type may be exported.

- [ ] **Step 1: Add a dedicated Input Group API contract assertion.**
  - Verify enum values.
  - Verify declared public properties/methods/events for both new controls.
  - Assert internal helper names are absent from `GetExportedTypes()`.

- [ ] **Step 2: Run release baseline test before changing the approved fingerprint.**
  - Expected: `ExportedApiMatchesApprovedV1Baseline` fails and prints the actual reconstructed API/fingerprint.

- [ ] **Step 3: Review the printed API manually against this plan.**
  - Reject accidental public/protected helper members.
  - Confirm existing exported signatures did not change.
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
  - Connected geometry
  - InputGroupText
  - InputGroup layout
  - InputGroup control
  - InputGroup interaction
  - affected Button/TextBox/NumericBox/Select/SplitButton
  - demo
  - release baseline

- [ ] **Step 5: Manual Windows verification.**
  - Launch demo for both target frameworks where practical.
  - Exercise all demo combinations with mouse and keyboard.
  - Tab forward/backward through grouped children and neighboring controls.
  - Activate Button/SplitButton with Enter/Space.
  - Open/close Select; verify Tab continuation and Escape.
  - Switch Light/Dark repeatedly.
  - Resize host narrow/wide.
  - Hide/show a first/middle/last child.
  - Repeat at Windows 100/125/150/175/200% scaling.
  - Inspect 1px/2px seams, rounded outer corners, focused/valid/invalid borders, text/icon vertical alignment, and no stale pixels after resize/theme changes.
  - Open in Visual Studio Designer and verify parameterless construction/property serialization.

- [ ] **Step 6: Self-review architecture.**
  - Confirm no duplicated connected-layout implementation.
  - Confirm group never mutates public child size/radius/validation state.
  - Confirm removal/hide/disposal clears internal overrides/subscriptions.
  - Confirm temporary fonts/pens/brushes/paths are disposed.
  - Confirm no target-specific public API divergence.
  - Confirm no unsupported control is claimed in docs/demo.
  - Confirm public surface is exactly the three intended exported types.

- [ ] **Step 7: Final commit if verification produced only cleanup/doc changes.**
  - `git commit -m "test: complete input group verification"`

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- `BootstrapInputGroup` and `BootstrapInputGroupText` are usable from normal WinForms code and Designer construction.
- Prefix, suffix, multiple addons, multiple stretch inputs, multiple buttons, Select, FormattedTextBox, NumericBox, and SplitButton scenarios render as one connected row.
- Small/Default/Large group sizing uses existing theme metrics and does not mutate child public size properties.
- Only visible first/last children keep outer radii; inner seams remain square and continuous at supported DPI scales.
- Focused/valid/invalid child borders remain visible and do not create stale/doubled seam artifacts.
- Tab/Shift+Tab and each child control's native keyboard semantics remain intact; the group itself never becomes an extra tab stop.
- Child validation remains independent; there is no group validation state.
- Removing/hiding/reparenting children cannot leave stale connected corner/size overrides.
- Theme switching and DPI changes recalculate geometry without reconstructing child controls or losing values/selections.
- Existing ButtonGroup and SplitButton connected rendering remains regression-free after helper generalization.
- Existing TextBox/FormattedTextBox/NumericBox/Select/Button/SplitButton standalone behavior is unchanged when not grouped.
- Unsupported child types fail clearly; ComboBox/DatePicker/file/checkbox/radio/arbitrary-control support is not implied.
- Product, demo, and tests pass for `net48` and `net8.0-windows`.
- Integrated demo and manual DPI/keyboard matrix are present.
- Public API review approves only `BootstrapInputGroup`, `BootstrapInputGroupSize`, and `BootstrapInputGroupText`; all infrastructure remains internal.
- Documentation and changelog accurately describe the implemented contract with no placeholders.

## Deferred Follow-ups

Plan separately if needed after MVP:

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
