# Bootstrap Component Expansion Implementation Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this roadmap stage-by-stage. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Badge, Alert, Tooltip, Tabs, NumericBox, ComboBox, Dropdown, Toast, and DatePicker in an order that delivers the smallest and most independent controls first, preserves native WinForms behavior wherever practical, and prevents later controls from inventing duplicate theme/rendering/animation/icon infrastructure.

**Architecture:** The expansion follows the repository's existing native-first composition model. Visual primitives custom-paint only their Bootstrap presentation, while semantic WinForms controls such as `ToolTip`, `TabControl`, `NumericUpDown`, `ComboBox`, `DateTimePicker`, and `ToolStripDropDown` retain selection/editing/focus/calendar/menu behavior when that behavior is already mature and compatible across `net48` and `net8.0-windows`. New public controls continue to consume Theme, Rendering, Icons, Animation, `BootstrapVariantColorResolver`, and existing controls rather than introducing another foundation layer.

**Tech Stack:** C#, native Windows Forms, existing Theme / Rendering / Icons / Animation infrastructure, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external package is required by this roadmap.

**Spec:** User request dated 2026-08-28 plus `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. Bootstrap 5.3 component behavior is design inspiration only; native WinForms semantics and repository compatibility rules take precedence.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public controls remain discoverable under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Every public/protected API addition changes the frozen v1 API fingerprint and therefore requires explicit review before updating `Phase16PublicApiBaselineTests` and `docs/PUBLIC_API_BASELINE.md`.
- Do not remove, rename, or change an existing public/protected member as part of this roadmap.
- Reuse `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `ColorUtil`, `BootstrapThemeManager`, `IconDescriptor` / `IIconRenderer`, and shared animation primitives where applicable.
- Do not add a control-specific WinForms timer when `BootstrapAnimation` can represent the behavior. A one-shot semantic delay that is not frame animation, such as toast auto-hide, may use one lifecycle-owned delay mechanism, but it must not become another animation scheduler.
- Prefer mature native WinForms semantics over reimplementing editing, selection, keyboard navigation, menu navigation, numeric parsing, or calendar behavior solely for visual purity.
- Designer construction must remain safe without application bootstrap, service locators, or DI.
- Every interactive control must have a keyboard/focus path and an STA test or a documented reason why the native control already owns that path.
- Every stage must update its demo/manual verification path, `docs/COMPONENTS.md`, `docs/TESTING.md`, README/package-facing docs when the component becomes supported, `CHANGELOG.md`, and the API fingerprint after review.
- Each stage is independently shippable and must finish both-target build/test gates before the next stage starts.
- `BootstrapPagination` is intentionally outside this roadmap because it already has `docs/plans/20260828-001-bootstrap-pagination-control.md`.

---

## Ordering Decision

| Order | Component | Classification | New-control dependencies | Why here |
| ---: | --- | --- | --- | --- |
| 1 | `BootstrapBadge` | Primitive visual | None | Smallest non-interactive surface; validates compact semantic-color rendering. |
| 2 | `BootstrapAlert` | Primitive feedback | Existing Icons; optional close affordance | Still independent; establishes dismissible feedback behavior without overlay hosting. |
| 3 | `BootstrapTooltip` | Attached component | Native `ToolTip` | Non-interactive and independent when implemented as an owner-drawn native tooltip. |
| 4 | `BootstrapTabControl` | Native-backed navigation | Native `TabControl` | Independent selection/navigation component; no popup infrastructure required. |
| 5 | `BootstrapNumericBox` | Native-backed input | Native `NumericUpDown` | Independent input; delegates culture/range/keyboard semantics to WinForms. |
| 6 | `BootstrapComboBox` | Native-backed input | Native `ComboBox` | Independent but visually/behaviorally broader than NumericBox; preserves binding and selection APIs. |
| 7 | `BootstrapDropdown` | Interactive popup | Existing Button/Icon + native `ToolStripDropDown` | First command popup; keyboard/menu/focus lifecycle makes it more complex than native-backed inputs. |
| 8 | `BootstrapToast` | Transient overlay feedback | Alert visual language + Animation | Adds stacking, show/hide transition, auto-hide, host lifecycle, and reduced-motion behavior. |
| 9 | `BootstrapDatePicker` | Composite/native-backed input | Native `DateTimePicker` + input shell patterns | Most difficult native control to theme safely; deliberately last after input/focus/theme patterns are proven. |

### Dependency graph

```text
Existing foundation
Theme + Rendering + Icons + Animation + Compatibility
       |
       +--> Badge
       +--> Alert
       +--> Tooltip (native ToolTip owner-draw)
       +--> TabControl (native TabControl owner-draw)
       +--> NumericBox (native NumericUpDown inside themed shell)
       +--> ComboBox (native ComboBox + owner-drawn items)
       +--> Dropdown (BootstrapButton/Icon + native ToolStripDropDown)
       +--> Toast (Alert-like palette + BootstrapAnimation)
       +--> DatePicker (native DateTimePicker inside themed shell)
```

The graph is intentionally shallow. ComboBox must not be implemented by embedding `BootstrapDropdown`, and DatePicker must not be implemented by building a custom calendar from Dropdown. Those controls have mature native semantics that should be retained.

---

## Shared File / Demo Strategy

Create component files directly under the existing flat `Controls` folder to match the current repository rather than introducing a folder/namespace reorganization during feature work.

Shared demo pages should be grouped by purpose instead of adding nine top-level demo windows:

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs` — Badge, Alert, Tooltip, Toast.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — Tabs and Dropdown.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — NumericBox, ComboBox, DatePicker.

Corresponding smoke tests:

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

`MainForm.cs` should add a navigation entry when the first control in each group lands; later stages extend that existing page rather than adding duplicate navigation entries.

---

# Stage 1 — BootstrapBadge

## Contract

`BootstrapBadge` is a compact, non-focusable, auto-sized text indicator. It owns no click behavior and no business state.

Proposed public surface:

```csharp
[DefaultProperty(nameof(Text))]
public class BootstrapBadge : Control
{
    public BootstrapVariant Variant { get; set; }      // default Primary
    public Color CustomColor { get; set; }             // default Color.Empty
    public bool Pill { get; set; }                      // default false
    public int BorderRadius { get; set; }               // default -1
}
```

Rules:

- Inherited `Text` is the badge content; `AutoSize` defaults to `true` and `TabStop` defaults to `false`.
- `CustomColor = Color.Empty` resolves through `BootstrapVariantColorResolver`; a non-empty color overrides `Variant`.
- Foreground must be selected with the existing contrast helper rather than hard-coded white/black.
- `Pill = true` uses a radius equal to half the rendered height; otherwise `BorderRadius = -1` uses the current theme radius.
- Badge does not expose `Selected`, `Checked`, `ClickToToggle`, or notification-count semantics.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadge.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadgeRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeTests.cs`
- Create/extend: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Create/extend: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

## Tasks

- [ ] **1.1 Freeze badge defaults and validation with failing tests.** Verify default `Variant`, empty custom color, `Pill=false`, `BorderRadius=-1`, `AutoSize=true`, `TabStop=false`, designer-safe construction, and rejection of `BorderRadius < -1`.
- [ ] **1.2 Add pure layout/palette tests.** Cover DPI-scaled horizontal/vertical padding, pill radius, custom radius, empty/non-empty text, semantic variant resolution, custom-color override, and contrast foreground selection.
- [ ] **1.3 Implement the minimal double-buffered badge.** Reuse theme typography/metrics, `RoundedPath`, `DpiScaler`, `BootstrapVariantColorResolver`, and `ColorUtil`; scope every GDI resource to painting.
- [ ] **1.4 Add lifecycle/theme tests.** Runtime Light/Dark changes repaint without retaining disposed controls; caller-owned `Font` remains caller-owned following the existing control conventions.
- [ ] **1.5 Add Feedback demo scenarios.** Show all variants, pill/default shapes, custom color, short/long text, disabled state, Light/Dark, and the DPI matrix.
- [ ] **1.6 Run both targets and documentation/API gates.** Update component/testing/readme/package/changelog docs, deliberately review the fingerprint change, then commit `feat: add BootstrapBadge`.

**Stage gate:** no Alert work starts until Badge tests pass on both targets and the API addition is intentionally approved.

---

# Stage 2 — BootstrapAlert

## Contract

`BootstrapAlert` is an inline feedback surface with optional icon and optional dismiss affordance. The initial control displays one text message; arbitrary rich-content composition is deferred to avoid turning Alert into a second Card.

Proposed public surface:

```csharp
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapAlert : UserControl
{
    public BootstrapVariant Variant { get; set; }      // default Primary
    public IconDescriptor? Icon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
    public bool Dismissible { get; set; }               // default false
    public int BorderRadius { get; set; }               // default -1
    public event EventHandler? Dismissed;
    public void Dismiss();
}
```

Rules:

- Alert uses a subtle variant-tinted surface, readable foreground, and a related border/accent derived from current theme colors; do not add nine hard-coded palette tables.
- Dismissal sets `Visible=false` and raises `Dismissed` once per visible-to-dismissed transition; it does not dispose the control.
- A dismiss button is an owned child affordance using the existing icon renderer and framework close glyph. It is keyboard-focusable only when `Dismissible=true`.
- Alert itself is not focusable unless required to expose an accessibility role; focus belongs to the dismiss button.
- Do not add auto-hide or overlay behavior; those belong to Toast.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

## Tasks

- [ ] **2.1 Write failing contract tests.** Cover defaults, icon renderer null rejection, border-radius validation, `Dismissible`, default accessibility metadata, and text normalization.
- [ ] **2.2 Write failing pure presentation tests.** Cover semantic variant surface/border/foreground derivation, disabled colors, DPI-scaled content/icon/close slots, and no overlap for long text.
- [ ] **2.3 Implement inline rendering and layout.** Reuse existing theme, rendering and icon infrastructure; do not depend on Badge just because both use variants.
- [ ] **2.4 Implement deterministic dismissal tests and behavior.** `Dismiss()` when already hidden is a no-op; close-button activation and `Dismiss()` share the same path; no duplicate event is raised.
- [ ] **2.5 Add Feedback demo cases.** Variants, icon/no-icon, dismissible/non-dismissible, multiline text, disabled, runtime theme switch, keyboard close activation, and DPI checks.
- [ ] **2.6 Complete docs/API baseline and commit** `feat: add BootstrapAlert`.

---

# Stage 3 — BootstrapTooltip

## Architecture decision

Use the native `System.Windows.Forms.ToolTip` behavior rather than a custom top-level window. `BootstrapTooltip` should inherit from or wrap `ToolTip` and enable `OwnerDraw`; native timing, control association, screen positioning, accessibility integration, and hide/show lifecycle remain authoritative.

Explicit placement (Top/Bottom/Left/Right), HTML/rich content, interactive tooltip content, and Popper-like collision APIs are out of scope for the first version. Native placement is the safer cross-target baseline.

## Contract

```csharp
public class BootstrapTooltip : ToolTip
{
    public BootstrapVariant Variant { get; set; }      // default Dark
    public Color CustomColor { get; set; }             // default Color.Empty
    public int BorderRadius { get; set; }               // default -1
    public Padding ContentPadding { get; set; }
}
```

Inherited `SetToolTip`, `GetToolTip`, `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` remain the canonical API; do not duplicate them with Bootstrap-prefixed aliases.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- Modify: Feedback demo/test files.

## Tasks

- [ ] **3.1 Prove native extender behavior with failing/characterization tests.** Associate text with multiple controls, replace/remove text, dispose the component, and verify native delay properties remain usable on both target frameworks.
- [ ] **3.2 Add pure measurement/palette tests.** Text measurement plus padding must produce a positive popup size; semantic/custom color and contrast resolution must be deterministic at 96–192 DPI.
- [ ] **3.3 Implement owner-drawn popup rendering.** Use `Popup` to set size and `Draw` to paint rounded background/border/text. Do not create a secondary tooltip Form.
- [ ] **3.4 Add lifecycle/theme tests.** Theme changes affect the next draw; disposal releases theme subscriptions and does not retain associated controls.
- [ ] **3.5 Add manual scenarios.** Buttons, TextBox, disabled/long-text anchors, multiple delay values, Light/Dark, high DPI, and repeated hover transitions.
- [ ] **3.6 Complete docs/API baseline and commit** `feat: add BootstrapTooltip`.

---

# Stage 4 — BootstrapTabControl (Tabs)

## Architecture decision

Implement Tabs as `BootstrapTabControl : TabControl` with owner-drawn tab headers. Preserve the native `TabPages` collection, `SelectedIndex`, `SelectedTab`, keyboard selection, designer support, and page hosting. Do not build a parallel page collection around `BootstrapButtonGroup`.

## Contract

```csharp
public enum BootstrapTabStyle
{
    Tabs,
    Pills,
    Underline
}

[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapTabControl : TabControl
{
    public BootstrapTabStyle TabStyle { get; set; }    // default Tabs
    public BootstrapVariant Variant { get; set; }      // default Primary
    public bool Fill { get; set; }                     // default false
    public int BorderRadius { get; set; }               // default -1
}
```

Rules:

- Native `TabPage` remains the page type; no `BootstrapTabPage` is introduced in this stage.
- Selected state uses `Variant`; inactive tabs use theme surface/text; disabled tab pages render muted and cannot be activated through framework-added mouse logic.
- Native focus/keyboard behavior remains intact. If custom hit testing is required for disabled pages, it must not regress Ctrl+Tab/arrow behavior.
- `Fill=true` distributes available header width across visible tabs; no separate `Justified` alias is added until a distinct behavior is required.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabStyle.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControlRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs`
- Create/extend: Navigation demo/test files.

## Tasks

- [ ] **4.1 Freeze public defaults and native collection behavior.** Tests must prove normal `TabPages.Add/Remove`, selected-index events, designer-safe construction, and no duplicate page model.
- [ ] **4.2 Add pure header-layout tests.** Cover tab rectangles, `Fill`, DPI scaling, Tabs/Pills/Underline selected geometry, edge clipping, and minimum usable width.
- [ ] **4.3 Implement owner-drawn headers only.** Do not custom-paint page contents; `TabPage` remains caller-owned.
- [ ] **4.4 Add keyboard/focus/disabled-page regression tests.** Tab into the control, switch pages with keyboard, click headers, and verify selection changes exactly once.
- [ ] **4.5 Add Navigation demo scenarios.** Three styles, fill/non-fill, long labels, disabled page, runtime theme switch, DPI matrix, nested controls retaining focus.
- [ ] **4.6 Complete docs/API baseline and commit** `feat: add BootstrapTabControl`.

---

# Stage 5 — BootstrapNumericBox

## Architecture decision

Use a `UserControl` shell around one native borderless `NumericUpDown`. The native control remains responsible for decimal parsing, culture, min/max clamping, incrementing, keyboard arrows, mouse wheel, and value events. The Bootstrap shell owns one public tab stop, focus border, rounded surface, validation presentation, and layout.

Do not reimplement a numeric parser in `BootstrapTextBox` and do not create custom plus/minus buttons in the first version.

## Contract

```csharp
[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapNumericBox : UserControl
{
    public decimal Value { get; set; }
    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; }
    public decimal Increment { get; set; }
    public int DecimalPlaces { get; set; }
    public bool ThousandsSeparator { get; set; }
    public bool ReadOnly { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }
    public event EventHandler? ValueChanged;
}
```

Rules:

- Inner `NumericUpDown` is the numeric state authority; wrapper properties forward rather than mirror separate state.
- The shell owns `TabStop`; the native editor is removed from the outer tab sequence just like `BootstrapTextBox`.
- Minimum/Maximum/Value behavior follows native WinForms exceptions/clamping semantics; do not invent conflicting normalization rules.
- `ReadOnly` preserves selection/copy and native value display.
- Hexadecimal mode and custom acceleration collections are deferred unless a concrete application need appears.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs`
- Create/extend: Advanced Inputs demo/test files.

## Tasks

- [ ] **5.1 Write native-delegation contract tests.** Verify forwarding for Value/Minimum/Maximum/Increment/DecimalPlaces/ThousandsSeparator/ReadOnly and one `ValueChanged` event for an effective native value change.
- [ ] **5.2 Add focus/layout/palette tests.** Cover validation-state priority, disabled/read-only surface, DPI-scaled padding/border, and focus ownership.
- [ ] **5.3 Implement the themed shell around one native NumericUpDown.** Do not parse numeric text in the wrapper.
- [ ] **5.4 Add STA keyboard tests.** Arrow increment/decrement, Tab entry/exit, direct text edit where native control permits it, disabled/read-only behavior, and mouse-wheel behavior without framework interference.
- [ ] **5.5 Add Advanced Inputs demo scenarios.** Integer/decimal/currency-like formatting, min/max, increment sizes, invalid/valid presentation, read-only/disabled, Light/Dark, DPI.
- [ ] **5.6 Complete docs/API baseline and commit** `feat: add BootstrapNumericBox`.

---

# Stage 6 — BootstrapComboBox

## Architecture decision

Subclass native `ComboBox` and preserve its complete selection/binding model. Use owner-drawn list items and a flat themed surface as far as WinForms permits safely. Do not replace the dropdown with `BootstrapDropdown`; a command menu and a data-selection control have different semantics.

A fully custom rounded popup, multi-select combo, token/chip mode, search-as-you-type server lookup, and virtualized remote data are explicitly out of scope.

## Contract

```csharp
public class BootstrapComboBox : ComboBox
{
    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }               // default -1
    public IconDescriptor? LeadingIcon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
}
```

All normal native members remain canonical: `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, `DropDownStyle`, `AutoCompleteMode`, `AutoCompleteSource`, and selection events.

Rules:

- `DrawMode` is owned by the Bootstrap implementation; callers customize data through normal text/value APIs, not by replacing the framework renderer.
- Item painting must handle selected, highlighted, disabled-host, and theme states without per-item retained GDI resources.
- Native dropdown and binding lifecycle remain untouched.
- `BorderRadius` affects the control shell where feasible; platform-native dropdown chrome may remain square. Document this limitation rather than replacing native semantics with a custom popup.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`
- Modify: Advanced Inputs demo/test files.

## Tasks

- [ ] **6.1 Characterize inherited behavior before custom painting.** Tests cover Items, DataSource binding, DisplayMember/ValueMember, SelectedIndexChanged, DropDownList/DropDown modes, autocomplete properties, and designer-safe construction.
- [ ] **6.2 Add pure rendering/layout tests.** Leading-icon slot, dropdown-arrow reserve area, focus/validation border color, item text rectangle, DPI scaling, and custom font measurement.
- [ ] **6.3 Implement owner-drawn item and shell presentation without replacing native selection logic.** Any unavoidable native-chrome limitation must be documented in `docs/COMPONENTS.md` rather than hidden behind platform-specific hacks.
- [ ] **6.4 Add STA interaction tests.** Open dropdown, keyboard Up/Down/Enter/Escape, type-to-select where native mode supports it, disabled state, data-bound selection, runtime theme switch.
- [ ] **6.5 Add Advanced Inputs demo cases.** Unbound items, data-bound objects, editable and DropDownList modes, long items, disabled, validation states, Light/Dark, DPI.
- [ ] **6.6 Complete docs/API baseline and commit** `feat: add BootstrapComboBox`.

---

# Stage 7 — BootstrapDropdown

## Architecture decision

Dropdown is a command menu, not a ComboBox. Compose an existing `BootstrapButton` target with a native `ToolStripDropDown`/`ToolStripItem` menu so Windows keyboard navigation, menu focus, dismissal on outside click, nested message-loop behavior, and screen working-area placement remain native.

Do not create a transparent top-level Form, a second Button renderer, or reuse ComboBox items as command items.

## Contract

```csharp
public enum BootstrapDropdownItemKind
{
    Item,
    Separator
}

public sealed class BootstrapDropdownItem
{
    public BootstrapDropdownItemKind Kind { get; }
    public string Text { get; set; }
    public IconDescriptor? Icon { get; set; }
    public bool Enabled { get; set; }
    public bool Checked { get; set; }
    public object? Tag { get; set; }
    public event EventHandler? Click;
}

public sealed class BootstrapDropdownItemCollection : Collection<BootstrapDropdownItem>
{
}

[DefaultEvent(nameof(Opened))]
public class BootstrapDropdown : Component
{
    public BootstrapButton? Target { get; set; }
    public BootstrapDropdownItemCollection Items { get; }
    public BootstrapVariant Variant { get; set; }
    public int MinimumWidth { get; set; }
    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public void Show();
    public void Close();
}
```

Rules:

- `Target` is not owned or disposed by Dropdown. Replacing/disposal detaches handlers.
- Menu items are rebuilt/synchronized deterministically and removed native items are disposed.
- Clicking an enabled item raises its `Click` and closes the dropdown; separators and disabled items never activate.
- Target click toggles the dropdown only while `Target.Enabled` and not `Target.Loading`.
- Native menu keyboard semantics (Up/Down/Home/End/Enter/Escape) remain authoritative.
- Submenus, headers, forms, arbitrary hosted controls, split-button behavior, and multi-select menu semantics are deferred.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Modify: Navigation demo/test files.

## Tasks

- [ ] **7.1 Freeze ownership and collection semantics with failing tests.** Add/remove/clear items, target replacement, target disposal, component disposal, disabled/loading target, and separator behavior.
- [ ] **7.2 Write native menu interaction tests.** Opening/closing events fire once, target click toggles, enabled item click fires once, disabled/separator items do nothing, outside/Escape closure does not mutate item state.
- [ ] **7.3 Implement a custom `ToolStripRenderer` using current theme and icon infrastructure.** Keep menu keyboard/message-loop behavior native; theme changes refresh open and future menus without leaking old renderers/resources.
- [ ] **7.4 Add DPI/working-area manual checks.** Anchor near each screen edge, multi-monitor working areas, 100–200% scaling, long text, icons, checked items, separators.
- [ ] **7.5 Add Navigation demo cases.** Basic actions, icons, disabled item, checked item, separator, Light/Dark while closed/open, keyboard operation.
- [ ] **7.6 Complete docs/API baseline and commit** `feat: add BootstrapDropdown`.

---

# Stage 8 — BootstrapToast

## Architecture decision

Toast is a transient notification surface and should not be modeled as an Alert with `Visible` toggles only. Implement `BootstrapToast` as the visual notification and `BootstrapToastContainer` as the stacking/lifetime owner. The container is a normal WinForms control placed by the application (typically anchored top-right/bottom-right), avoiding an implicit global window or service locator.

Show/hide motion uses `BootstrapAnimation`; auto-hide timing is a semantic delay owned by each toast/container and must be canceled on hover when configured, hide, removal, or disposal. Reduced motion skips transition frames but preserves auto-hide semantics.

## Contract

```csharp
public enum BootstrapToastPlacement
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapToast : UserControl
{
    public string Title { get; set; }
    public BootstrapVariant Variant { get; set; }
    public IconDescriptor? Icon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
    public bool Dismissible { get; set; }
    public bool AutoHide { get; set; }                  // default true
    public int AutoHideDelay { get; set; }              // default 5000 ms
    public int AnimationDuration { get; set; }
    public event EventHandler? Dismissed;
    public void Dismiss();
}

public class BootstrapToastContainer : Panel
{
    public BootstrapToastPlacement Placement { get; set; }
    public int ToastSpacing { get; set; }
    public int MaximumVisibleToasts { get; set; }
    public void ShowToast(BootstrapToast toast);
    public void DismissAll();
}
```

Rules:

- Container owns only toasts passed to `ShowToast`; ownership transfer must be explicit in XML docs. Removed/dismissed owned toasts are disposed after the dismissal transition completes.
- The same toast instance cannot be hosted by two containers simultaneously.
- `AutoHideDelay <= 0` throws. `AnimationDuration <= 0` follows the shared animation validation rule.
- Rapid show/dismiss/re-show paths must not create overlapping animation schedulers or duplicate `Dismissed` events.
- Auto-hide does not use `Thread.Sleep`, `Task.Delay` as a frame scheduler, or a permanent polling timer.
- Toast does not create its own top-level Form in the first version; applications choose where the container is placed.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastPlacement.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: Feedback demo/test files.

## Tasks

- [ ] **8.1 Write pure stacking/layout tests.** Top/bottom ordering, left/right alignment, DPI spacing, max-visible overflow policy, resize, and empty container.
- [ ] **8.2 Freeze toast contract and ownership tests.** Defaults, delay validation, one-container ownership, `Dismissed` exactly once, container disposal, and deterministic child disposal.
- [ ] **8.3 Add deterministic animation tests using existing animation test patterns.** Show, dismiss, reversal while entering, reduced motion, hidden container, disposal during transition, and no work after disposal.
- [ ] **8.4 Implement auto-hide as a lifecycle-owned semantic delay.** Cancellation/restart rules are explicit: start after fully shown, cancel on dismiss/removal/disposal, and do not let stale callbacks dismiss a reused toast.
- [ ] **8.5 Implement container stacking and transitions.** Reflow existing toasts using the shared finite animation abstraction when motion is enabled; do not create one independent frame timer per toast.
- [ ] **8.6 Add Feedback demo scenarios.** Manual show, auto-hide, multiple stacking, maximum-visible behavior, dismiss button, hover/interaction, rapid bursts, Light/Dark, reduced motion, DPI.
- [ ] **8.7 Run resource stress checks.** Repeatedly create/show/dismiss hundreds of toasts and verify no unbounded timer/event/GDI growth.
- [ ] **8.8 Complete docs/API baseline and commit** `feat: add BootstrapToast`.

---

# Stage 9 — BootstrapDatePicker

## Architecture decision

Use a themed `UserControl` shell containing one native `DateTimePicker`. The native picker owns date parsing, locale-aware formatting, keyboard editing, min/max validation, optional checkbox state, and the OS calendar popup. The shell owns the Bootstrap border/focus/validation surface and forwards a deliberately small core API.

Do not implement a custom calendar grid in this roadmap. Bootstrap itself does not define a native DatePicker widget, and a custom calendar would introduce a separate navigation/accessibility/localization subsystem that should be planned independently if ever required.

## Contract

```csharp
[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapDatePicker : UserControl
{
    public DateTime Value { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public DateTimePickerFormat Format { get; set; }
    public string CustomFormat { get; set; }
    public bool ShowCheckBox { get; set; }
    public bool Checked { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }
    public event EventHandler? ValueChanged;
}
```

Rules:

- The inner native picker is the value/format authority; wrapper state must not diverge.
- `CustomFormat` matters only when `Format=Custom`, matching native semantics.
- `ShowCheckBox`/`Checked` retain native optional-value behavior; do not add a second nullable `DateTime? Value` API in the same release.
- `MinDate`/`MaxDate`/`Value` validation follows the native control's documented behavior.
- The OS calendar popup may retain platform-native chrome. The Bootstrap shell must not use unsupported Win32 painting hacks to force popup theming.
- `ShowUpDown`, time-only picker, range picker, date-range calendar, week numbers, multi-date selection, and custom calendar templates are deferred.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`
- Modify: Advanced Inputs demo/test files.

## Tasks

- [ ] **9.1 Characterize native date behavior on both targets before styling.** Value/min/max changes, custom format, checkbox/checked state, keyboard editing, dropdown open/close, and locale-sensitive display must remain native.
- [ ] **9.2 Freeze wrapper contract tests.** Forwarded values/events, focus ownership, validation state, border-radius validation, disabled state, designer-safe construction, and no duplicate `ValueChanged` event.
- [ ] **9.3 Add pure shell layout/palette tests.** Native picker inset, focus/validation border, disabled surface, DPI dimensions, and no clipping at 100–200%.
- [ ] **9.4 Implement the themed shell without intercepting calendar semantics.** Keep native calendar popup behavior intact and document unavoidable OS-rendered portions.
- [ ] **9.5 Add STA keyboard/dropdown tests.** Tab focus, arrow/date editing, open calendar, choose date, Escape close, checkbox toggle, min/max boundaries, Light/Dark switch while control is alive.
- [ ] **9.6 Add Advanced Inputs demo cases.** Short/long/custom formats, min/max, checked/unchecked, validation states, disabled, locale/manual checks, Light/Dark, DPI.
- [ ] **9.7 Run final cross-control regression.** TextBox/NumericBox/ComboBox/DatePicker tab order, Tooltip attachment, Dropdown nearby, Toast notifications from value changes, and no theme/lifecycle leaks.
- [ ] **9.8 Complete docs/API baseline and commit** `feat: add BootstrapDatePicker`.

---

# Cross-Stage Documentation and API Checklist

Run this after every stage, not only after Stage 9:

- [ ] Add/finalize the component contract in `docs/COMPONENTS.md` and remove that component from the deferred list.
- [ ] Add architecture dependency notes only when a real new dependency exists; do not redraw the architecture for cosmetic reasons.
- [ ] Add automated/manual coverage notes to `docs/TESTING.md`.
- [ ] Add supported-control mention to `README.md` and `docs/PACKAGE_README.md`.
- [ ] Add an `Unreleased` changelog entry; do not rewrite `1.0.0-rc.1` history.
- [ ] Run the API baseline test and inspect the reconstructed exported surface before changing the approved fingerprint.
- [ ] Update `docs/PUBLIC_API_BASELINE.md` with the newly approved fingerprint and a short reason naming the compatible API addition.
- [ ] Run `dotnet build -c Release -f net48`.
- [ ] Run `dotnet build -c Release -f net8.0-windows`.
- [ ] Run `dotnet test -c Release` or the repository's `build.ps1` / `test.ps1` sequence as defined by the release docs.
- [ ] Run the relevant demo page at Light and Dark themes and the 100/125/150/175/200% DPI matrix.
- [ ] Verify creation/disposal repeatedly and confirm no unbounded GDI handles, USER handles, timers, or event subscriptions.

---

# Explicitly Deferred Scope

The following ideas should not be pulled into these nine stages without a separate plan because they materially enlarge the public API or introduce a new subsystem:

- Rich/HTML Alert or Tooltip content.
- Interactive tooltips/popovers.
- Nested Dropdown submenus or arbitrary hosted controls.
- Split-button dropdowns.
- Multi-select/tokenized ComboBox.
- Async/remote ComboBox search.
- Fully custom calendar rendering.
- Date ranges or multi-date selection.
- Global/top-level toast notification service that creates its own windows.
- Persistence of toast history/notification center.
- Shared public `BootstrapControlSize` migration for existing TextBox/Button controls.

---

# Final Completion Gate

The roadmap is complete only when all nine stages have individually passed their stage gates and the final repository satisfies all of the following:

1. Badge, Alert, Tooltip, Tabs, NumericBox, ComboBox, Dropdown, Toast, and DatePicker all have documented public contracts.
2. Both target frameworks build and all automated tests pass.
3. Feedback, Navigation, and Advanced Inputs demo pages expose every new component and relevant interaction state.
4. Runtime Light/Dark switching works without recreating the application.
5. Interactive controls preserve keyboard/focus behavior and native semantic behavior where delegated.
6. DPI verification passes at 100%, 125%, 150%, 175%, and 200%.
7. No component introduced duplicate theme, icon, rendering, or animation infrastructure.
8. Public API fingerprint updates were reviewed incrementally rather than accepted once at the end.
9. Resource/lifecycle stress checks show no obvious unbounded GDI, USER, event, or timer growth.
10. Deferred features remain deferred unless a separate approved plan explicitly adds them.
