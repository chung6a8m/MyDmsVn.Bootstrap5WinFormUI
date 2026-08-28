# Bootstrap Component Expansion Implementation Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this roadmap stage-by-stage. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Badge, Alert, Tooltip, Tabs, NumericBox, ComboBox, Dropdown, Toast, and DatePicker in an order that delivers the most basic and independent controls first, preserves mature native WinForms semantics wherever practical, and prevents later controls from inventing duplicate infrastructure.

**Architecture:** Follow the repository's native-first composition model. Small visual primitives custom-paint only their Bootstrap presentation. Controls with mature WinForms semantics delegate selection, editing, numeric parsing, menu navigation, tooltip timing, and calendar behavior to native controls while the framework owns theme, geometry, icons, validation presentation, and lifecycle integration. All new controls consume the existing Theme, Rendering, Icons, Animation, Compatibility, `BootstrapVariant`, and `BootstrapVariantColorResolver` infrastructure.

**Tech Stack:** C#, native Windows Forms, NUnit 4, existing Theme / Rendering / Icons / Animation infrastructure, SDK-style multi-targeting (`net48;net8.0-windows`). No new external package is required.

**Spec:** User request dated 2026-08-28 plus `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. Bootstrap 5 visual/component behavior is inspiration only; native WinForms semantics and repository compatibility rules take precedence.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public controls remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared implementation wherever practical.
- Every public/protected API addition changes the frozen v1 API fingerprint and requires explicit review before updating `Phase16PublicApiBaselineTests` and `docs/PUBLIC_API_BASELINE.md`.
- Do not remove, rename, or change an existing public/protected member as part of this roadmap.
- Reuse `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `ColorUtil`, `BootstrapThemeManager`, `IconDescriptor` / `IIconRenderer`, and shared animation primitives where applicable.
- Do not create another theme manager, icon model, geometry library, focus engine, or frame-animation scheduler.
- Prefer mature native WinForms semantics over reimplementing editing, selection, keyboard navigation, menu navigation, numeric parsing, or calendar behavior solely for visual purity.
- Designer construction must remain safe without application bootstrap, DI, service locators, or initialized global state beyond the framework's existing safe defaults.
- Every interactive control needs a keyboard/focus path and an STA test or explicit native-behavior characterization.
- Every stage must add demo/manual coverage and update `docs/COMPONENTS.md`, `docs/TESTING.md`, `README.md`, `docs/PACKAGE_README.md`, `CHANGELOG.md`, and the public API baseline after deliberate review.
- Each stage is independently shippable. Both target builds and relevant tests must pass before the next stage begins.
- `BootstrapPagination` is not part of this roadmap because it already has `docs/plans/20260828-001-bootstrap-pagination-control.md`.

---

## Ordering Decision

| Order | Component | Classification | New-control dependencies | Reason |
| ---: | --- | --- | --- | --- |
| 1 | `BootstrapBadge` | Primitive visual | None | Smallest non-interactive semantic surface. |
| 2 | `BootstrapAlert` | Primitive feedback | Existing Icons | Independent inline feedback; no popup/overlay host. |
| 3 | `BootstrapTooltip` | Attached component | Native `ToolTip` | Independent, non-interactive, native timing/placement. |
| 4 | `BootstrapTabControl` | Native-backed navigation | Native `TabControl` | Independent page selection/navigation; no popup subsystem. |
| 5 | `BootstrapNumericBox` | Native-backed input | Native `NumericUpDown` | Focused, well-bounded input semantics. |
| 6 | `BootstrapComboBox` | Native-backed input | Native `ComboBox` | Broader binding/dropdown behavior than NumericBox, still independent. |
| 7 | `BootstrapDropdown` | Command popup | Existing Button/Icon + native `ToolStripDropDown` | Adds popup focus, dismissal, command-item lifecycle. |
| 8 | `BootstrapToast` | Transient feedback overlay | Alert visual language + Animation | Adds stacking, auto-hide, transitions, ownership. |
| 9 | `BootstrapDatePicker` | Native-backed composite input | Native `DateTimePicker` + established input-shell patterns | Hardest native control to theme safely; calendar remains OS-owned. |

### Dependency graph

```text
Existing foundation
Theme + Rendering + Icons + Animation + Compatibility
       |
       +--> Badge
       +--> Alert
       +--> Tooltip -> native ToolTip
       +--> TabControl -> native TabControl
       +--> NumericBox -> native NumericUpDown
       +--> ComboBox -> native ComboBox
       +--> Dropdown -> BootstrapButton + native ToolStripDropDown
       +--> Toast -> BootstrapAnimation + feedback palette rules
       +--> DatePicker -> native DateTimePicker
```

The graph is intentionally shallow. `BootstrapComboBox` must not be implemented by embedding `BootstrapDropdown`, and `BootstrapDatePicker` must not build a custom calendar from Dropdown. These are distinct semantic controls with mature native behavior.

---

## Shared Demo Strategy

Group demos by purpose instead of creating nine top-level windows:

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs` — Badge, Alert, Tooltip, Toast.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — Tabs and Dropdown.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — NumericBox, ComboBox, DatePicker.

Tests:

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

`MainForm.cs` adds one navigation entry when the first control of each demo group lands; later stages extend the same page.

---

# Stage 1 — BootstrapBadge

## Contract

`BootstrapBadge` is a compact, auto-sized, non-focusable text indicator. It owns no click/toggle semantics and no notification-count business logic.

```csharp
[DefaultProperty(nameof(Text))]
public class BootstrapBadge : Control
{
    public BootstrapVariant Variant { get; set; }      // Primary
    public Color CustomColor { get; set; }             // Color.Empty
    public bool Pill { get; set; }                      // false
    public int BorderRadius { get; set; }               // -1 = theme radius
}
```

Rules:

- `Text` is inherited content; `AutoSize=true`, `TabStop=false` by default.
- `CustomColor=Color.Empty` resolves through `BootstrapVariantColorResolver`; non-empty color overrides `Variant`.
- Foreground uses existing contrast calculation rather than a fixed white/black assumption.
- `Pill=true` uses half-height radius; otherwise `BorderRadius=-1` uses the theme radius.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadge.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadgeRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeTests.cs`
- Create/extend: Feedback demo/test files.

## Tasks

- [ ] **1.1 Write failing contract tests.** Assert defaults, designer-safe construction, text normalization, `AutoSize`, `TabStop`, and rejection of `BorderRadius < -1`.
- [ ] **1.2 Write failing pure render/layout tests.** Cover semantic/custom colors, contrast foreground, empty/short/long text, DPI-scaled padding, pill radius, and explicit radius.
- [ ] **1.3 Implement minimal custom painting.** Use current theme typography/metrics, `DpiScaler`, `RoundedPath`, `BootstrapVariantColorResolver`, `ColorUtil`, double buffering, and scoped GDI resources only.
- [ ] **1.4 Add theme/lifecycle tests.** Runtime Light/Dark repaint works; theme subscriptions and theme-created fonts are released on disposal; caller-owned font remains caller-owned.
- [ ] **1.5 Add demo cases.** All semantic variants, default/pill, custom color, disabled, long text, runtime theme switching, 100–200% DPI.
- [ ] **1.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapBadge`.

**Gate:** Stage 1 must be green before Alert starts.

---

# Stage 2 — BootstrapAlert

## Contract

`BootstrapAlert` is an inline feedback surface with text, optional icon, and optional dismiss affordance. Rich arbitrary child content is deferred so Alert does not become another Card.

```csharp
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapAlert : UserControl
{
    public BootstrapVariant Variant { get; set; }      // Primary
    public IconDescriptor? Icon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
    public bool Dismissible { get; set; }               // false
    public int BorderRadius { get; set; }               // -1
    public event EventHandler? Dismissed;
    public void Dismiss();
}
```

Rules:

- Derive subtle variant surface/border/foreground from existing theme/color helpers; do not add a separate hard-coded alert palette table.
- `Dismiss()` changes visible state and raises `Dismissed` exactly once for an effective visible-to-dismissed transition; it does not dispose the Alert.
- The close affordance uses the existing icon renderer/framework close glyph and is keyboard-focusable only when dismissible.
- Auto-hide and overlay behavior belong to Toast, not Alert.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`
- Modify: Feedback demo/test files.

## Tasks

- [ ] **2.1 Write failing contract tests.** Cover defaults, text normalization, radius validation, icon renderer null rejection, dismissible state, accessibility metadata.
- [ ] **2.2 Write failing pure presentation tests.** Cover surface/border/foreground derivation, disabled presentation, icon/text/close rectangles, multiline measurement, DPI scaling.
- [ ] **2.3 Implement rendering/layout.** Reuse Theme, Rendering, Icons; do not make Alert depend on Badge merely because both use semantic colors.
- [ ] **2.4 Implement dismissal tests/behavior.** Close-button activation and `Dismiss()` share one path; repeated dismissal is a no-op; re-show then dismiss produces one new event.
- [ ] **2.5 Add demo cases.** Icon/no-icon, dismissible/non-dismissible, multiline, all variants, disabled, keyboard close, Light/Dark, DPI.
- [ ] **2.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapAlert`.

---

# Stage 3 — BootstrapTooltip

## Architecture decision

Do **not** inherit from `System.Windows.Forms.ToolTip`. Define `BootstrapTooltip : Component, IExtenderProvider` that owns exactly one native `ToolTip` instance. This avoids relying on target-dependent inheritance/sealing details while preserving the WinForms tooltip timing, association, popup positioning, and owner-draw pipeline.

The wrapper implements the designer extender contract itself and forwards each associated control/caption to the owned native `ToolTip`. Explicit Top/Bottom/Left/Right placement, rich HTML, interactive content, and a custom top-level tooltip Form are out of scope.

## Contract

```csharp
[ProvideProperty("ToolTip", typeof(Control))]
public class BootstrapTooltip : Component, IExtenderProvider
{
    public BootstrapTooltip();
    public BootstrapTooltip(IContainer container);

    public BootstrapVariant Variant { get; set; }      // Dark
    public Color CustomColor { get; set; }             // Color.Empty
    public int BorderRadius { get; set; }               // -1
    public Padding ContentPadding { get; set; }

    public int InitialDelay { get; set; }
    public int ReshowDelay { get; set; }
    public int AutoPopDelay { get; set; }
    public bool Active { get; set; }
    public bool ShowAlways { get; set; }

    public bool CanExtend(object extendee);
    public void SetToolTip(Control control, string caption);
    public string GetToolTip(Control control);
}
```

Rules:

- The inner `ToolTip` is owned and disposed by `BootstrapTooltip`; associated controls are never owned.
- `CanExtend` accepts WinForms `Control` instances except the component itself/non-controls.
- Timing/active/show-always properties forward directly to the native instance; do not mirror separate behavioral state.
- The native tooltip uses `OwnerDraw=true`; its `Popup` determines measured size and `Draw` paints the Bootstrap surface/text.
- Theme switching changes subsequent owner-draw rendering without recreating user associations.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- Modify: Feedback demo/test files.

## Tasks

- [ ] **3.1 Write failing extender/forwarding tests.** Verify parameterless and `IContainer` construction, `CanExtend`, `SetToolTip`/`GetToolTip`, replacing/removing captions, multiple controls, native delay forwarding, `Active`, `ShowAlways`, and disposal.
- [ ] **3.2 Write failing pure measurement/palette tests.** Cover text + padding size, semantic/custom color, contrast foreground, radius, and DPI 96/120/144/168/192.
- [ ] **3.3 Implement the wrapper and one owned native ToolTip.** Enable `OwnerDraw`, forward public behavioral properties, implement the extender API, and never create a tooltip Form.
- [ ] **3.4 Implement owner-draw events.** `Popup` uses measured text/padding to size the native popup; `Draw` uses rounded theme/custom background, border, and text with scoped GDI resources.
- [ ] **3.5 Add lifecycle/theme tests and manual scenarios.** Multiple anchors, long text, disabled anchors where native behavior allows it, timing changes, repeated hover, Light/Dark, DPI, disposal without retained controls.
- [ ] **3.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapTooltip`.

---

# Stage 4 — BootstrapTabControl (Tabs)

## Architecture decision

Implement Tabs as `BootstrapTabControl : TabControl` using owner-drawn headers. Preserve native `TabPages`, `SelectedIndex`, `SelectedTab`, page hosting, keyboard navigation, and Designer semantics. Do not create a parallel page model around `BootstrapButtonGroup`.

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
    public BootstrapTabStyle TabStyle { get; set; }    // Tabs
    public BootstrapVariant Variant { get; set; }      // Primary
    public bool Fill { get; set; }                     // false
    public int BorderRadius { get; set; }               // -1
}
```

Rules:

- Native `TabPage` remains the page type; no `BootstrapTabPage` is added.
- Only headers are framework-painted; page contents remain caller-owned.
- `Fill=true` distributes header width across available header space.
- Selected headers use `Variant`; inactive/disabled headers resolve from theme tokens.
- Native focus and keyboard page selection remain authoritative.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabStyle.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControlRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs`
- Create/extend: Navigation demo/test files.

## Tasks

- [ ] **4.1 Write failing contract/native-behavior tests.** Verify `TabPages.Add/Remove`, normal selected-index events, defaults, Designer construction, and no duplicate page collection.
- [ ] **4.2 Write failing header-layout tests.** Tabs/Pills/Underline geometry, `Fill`, long labels, minimum width, DPI scaling, selected/disabled palette.
- [ ] **4.3 Implement owner-drawn headers only.** Do not custom-paint page bodies or intercept selection unless required for presentation correctness.
- [ ] **4.4 Add STA interaction regressions.** Mouse selection, Tab focus, arrows/Ctrl+Tab behavior supported by native control, one selection event per effective change, disabled-page behavior.
- [ ] **4.5 Add demo cases.** All styles, fill/non-fill, long labels, disabled page, nested focusable content, Light/Dark, DPI.
- [ ] **4.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapTabControl`.

---

# Stage 5 — BootstrapNumericBox

## Architecture decision

Use a `UserControl` shell around exactly one native borderless `NumericUpDown`. The native control owns decimal parsing, culture, min/max/value rules, incrementing, keyboard arrows, mouse wheel, and value events. The shell owns one public tab stop, focus/validation border, rounded surface, theme font, and DPI layout.

Do not parse numbers through `BootstrapTextBox` and do not create custom +/- buttons in this version.

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

- Inner `NumericUpDown` is the numeric state authority; wrapper properties forward directly.
- Outer control owns the public tab stop; inner editor remains outside the parent tab sequence, matching the established `BootstrapTextBox` pattern.
- Native min/max/value validation semantics remain authoritative.
- Hexadecimal mode and acceleration collections are deferred.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs`
- Create/extend: Advanced Inputs demo/test files.

## Tasks

- [ ] **5.1 Write failing delegation tests.** Value/Minimum/Maximum/Increment/DecimalPlaces/ThousandsSeparator/ReadOnly forwarding and exactly one `ValueChanged` for an effective native change.
- [ ] **5.2 Write failing shell-render tests.** Focus/validation priority, disabled/read-only colors, radius validation, DPI padding/border, theme font ownership.
- [ ] **5.3 Implement the shell around one native NumericUpDown.** No duplicate parser/range state.
- [ ] **5.4 Add STA interaction tests.** Tab entry/exit, Up/Down increments, editable numeric text where native behavior permits, mouse wheel, disabled/read-only, min/max boundaries.
- [ ] **5.5 Add demo cases.** Integer, decimal, thousands separator, multiple increments, min/max, validation, read-only/disabled, Light/Dark, DPI.
- [ ] **5.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapNumericBox`.

---

# Stage 6 — BootstrapComboBox

## Architecture decision

Subclass native `ComboBox` and preserve its selection/binding model. Use owner-drawn list items plus a themed shell as far as WinForms safely permits. Do not replace the dropdown with `BootstrapDropdown`; a command menu and a data-selection control have different semantics.

Multi-select, token/chip mode, remote async lookup, custom popup virtualization, and a fully custom rounded popup are deferred.

## Contract

```csharp
public class BootstrapComboBox : ComboBox
{
    public BootstrapValidationState ValidationState { get; set; } // None
    public int BorderRadius { get; set; }                          // -1
    public IconDescriptor? LeadingIcon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
}
```

Native members remain canonical: `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, `DropDownStyle`, `AutoCompleteMode`, `AutoCompleteSource`, and selection events.

Rules:

- Framework owns `DrawMode`; callers provide data/text/value through normal ComboBox APIs, not by replacing framework painting.
- Item paint handles normal/highlighted/selected/disabled-host states without retained per-item GDI resources.
- Binding/dropdown lifecycle remains native.
- `BorderRadius` applies where safely controllable; native popup chrome may remain square and must be documented as a limitation rather than replaced with unsupported hacks.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`
- Modify: Advanced Inputs demo/test files.

## Tasks

- [ ] **6.1 Characterize inherited semantics with failing/regression tests.** Items, object data binding, DisplayMember/ValueMember, selected state/events, DropDown/DropDownList, autocomplete properties, Designer construction.
- [ ] **6.2 Write failing render/layout tests.** Leading-icon slot, arrow reserve, focus/validation border, item text rectangle, long text, custom font, DPI.
- [ ] **6.3 Implement framework painting without replacing native selection/binding logic.** Document unavoidable native-chrome limits.
- [ ] **6.4 Add STA interaction tests.** Open/close dropdown, Up/Down/Enter/Escape, type-to-select where native mode supports it, data-bound selection, disabled state, runtime theme switch.
- [ ] **6.5 Add demo cases.** Unbound, bound object list, editable/DropDownList, autocomplete, long items, validation, disabled, Light/Dark, DPI.
- [ ] **6.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapComboBox`.

---

# Stage 7 — BootstrapDropdown

## Architecture decision

Dropdown is a command menu. Compose an existing `BootstrapButton` target with a native `ToolStripDropDown` so Windows menu focus, keyboard navigation, outside-click dismissal, message-loop behavior, and working-area placement remain native. Theme the menu through a framework `ToolStripRenderer`.

Do not create a transparent top-level Form and do not reuse ComboBox data items as command items.

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

- `Target` is caller-owned. Replacement/disposal detaches handlers; Dropdown never disposes the target.
- Native menu items created by Dropdown are owned/disposed by Dropdown.
- Enabled item activation raises its model `Click` and closes; disabled items/separators never activate.
- Target toggles only when enabled and not loading.
- Up/Down/Home/End/Enter/Escape behavior remains native.
- Submenus, arbitrary hosted controls, split-button semantics, and multi-select are deferred.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Modify: Navigation demo/test files.

## Tasks

- [ ] **7.1 Write failing ownership/collection tests.** Add/remove/clear, target replacement/disposal, component disposal, disabled/loading target, separator behavior.
- [ ] **7.2 Write failing native-menu interaction tests.** Open/close events once, target toggle, enabled item click once, disabled/separator no-op, Escape closure without item mutation.
- [ ] **7.3 Implement ToolStripDropDown composition and renderer.** Reuse current theme/icon infrastructure; removed native items and renderer-owned resources are disposed deterministically.
- [ ] **7.4 Add theme/DPI/working-area checks.** Runtime Light/Dark, long items, icons, check marks, separators, anchor near screen edges, multi-monitor/manual checks, 100–200% DPI.
- [ ] **7.5 Add Navigation demo cases.** Basic actions, icons, disabled/checked item, separator, keyboard operation, open/close stress.
- [ ] **7.6 Run both targets, docs, API baseline, then commit** `feat: add BootstrapDropdown`.

---

# Stage 8 — BootstrapToast

## Architecture decision

Use two units: `BootstrapToast` is one transient notification, and `BootstrapToastContainer` owns stacking/layout/lifetime. The container is a normal WinForms control placed by the application, avoiding an implicit global window/service locator.

Show/hide transitions use `BootstrapAnimation`. Auto-hide is not frame animation: each visible auto-hide toast may own one short-lived `System.Windows.Forms.Timer` whose interval equals `AutoHideDelay`; it starts only after the toast is fully shown, stops before invoking dismissal, and is disposed on dismissal/removal/disposal. Reduced motion skips transition frames but keeps auto-hide timing behavior.

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
    public bool Dismissible { get; set; }               // true
    public bool AutoHide { get; set; }                  // true
    public int AutoHideDelay { get; set; }              // 5000 ms
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

- `ShowToast` explicitly transfers ownership of that toast to the container; XML docs must say this. Removed/dismissed owned toasts are disposed after exit animation completes.
- One toast instance cannot be hosted by two containers.
- `AutoHideDelay <= 0` and invalid `AnimationDuration` values throw consistently.
- Auto-hide timer is semantic delay only; all visual transition frames come from `BootstrapAnimation`.
- Rapid show/dismiss paths cannot create overlapping animations, stale timers, or duplicate `Dismissed` events.
- No top-level Toast Form/global notification service in this version.

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

- [ ] **8.1 Write failing pure stacking tests.** Top/bottom order, left/right alignment, spacing, maximum visible count, resize, empty state, DPI.
- [ ] **8.2 Write failing ownership/contract tests.** Defaults, validation, one-container rule, transfer-of-ownership behavior, exactly-once dismissal, deterministic child disposal.
- [ ] **8.3 Write deterministic animation tests following existing animation test patterns.** Enter, exit, dismiss while entering, reduced motion, hidden container, disposal during transition, no scheduling after disposal.
- [ ] **8.4 Implement auto-hide timer lifecycle.** Create/start only for visible auto-hide state after enter completion; stop before dismissal; dispose and null it on dismissal/removal/disposal; stale ticks must not dismiss a subsequently reused/re-shown toast.
- [ ] **8.5 Implement container stacking/reflow.** Reuse finite animation for movement/opacity-like visual progress where WinForms rendering permits; no extra frame timer.
- [ ] **8.6 Add Feedback demo cases.** Manual and auto-hide, burst stacking, max-visible policy, dismiss button, rapid dismiss/show, reduced motion, Light/Dark, DPI.
- [ ] **8.7 Run resource stress.** Repeatedly create/show/dismiss hundreds of toasts and verify no unbounded timer/event/GDI/USER-handle growth.
- [ ] **8.8 Run both targets, docs, API baseline, then commit** `feat: add BootstrapToast`.

---

# Stage 9 — BootstrapDatePicker

## Architecture decision

Use a themed `UserControl` shell around exactly one native `DateTimePicker`. Native WinForms owns date parsing, locale-aware formatting, min/max validation, keyboard editing, checkbox state, and the OS calendar popup. The framework owns border/focus/validation surface, font, and DPI layout.

Do not implement a custom calendar in this roadmap. A custom calendar would introduce its own navigation, accessibility, localization, month/year navigation, and popup subsystem and requires a separate plan.

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

- Inner native picker is the date/format state authority; wrapper properties forward and do not mirror independent state.
- `CustomFormat` follows native semantics and matters when `Format=Custom`.
- `ShowCheckBox`/`Checked` provide native optional-value behavior; do not simultaneously add a second nullable `DateTime? Value` API.
- Min/max/value validation remains native.
- OS calendar popup chrome may remain native. Do not use unsupported Win32 painting hacks to force popup theming.
- `ShowUpDown`, range picker, multi-date selection, week numbers, custom calendar templates are deferred.

## Files

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`
- Modify: Advanced Inputs demo/test files.

## Tasks

- [ ] **9.1 Characterize native date behavior on both targets before styling.** Value/min/max, built-in/custom format, checkbox/checked, keyboard editing, dropdown open/close, locale-sensitive display.
- [ ] **9.2 Write failing wrapper contract tests.** Forwarded state/events, one public focus path, validation state, radius validation, disabled state, Designer construction, no duplicate `ValueChanged`.
- [ ] **9.3 Write failing shell-layout/palette tests.** Native picker inset, focus/validation border priority, disabled surface, DPI dimensions, font and text clipping.
- [ ] **9.4 Implement themed shell without intercepting calendar semantics.** Preserve native popup behavior and document OS-rendered portions.
- [ ] **9.5 Add STA interaction tests.** Tab focus, keyboard date editing, open/select/close calendar, Escape, checkbox toggle, min/max boundaries, runtime theme switch.
- [ ] **9.6 Add Advanced Inputs demo cases.** Short/long/custom format, min/max, checked/unchecked, validation, disabled, locale manual check, Light/Dark, DPI.
- [ ] **9.7 Run final cross-control regression.** TextBox/NumericBox/ComboBox/DatePicker tab sequence, Tooltip associations, adjacent Dropdown, Toast triggered from input changes, repeated theme switching/disposal.
- [ ] **9.8 Run both targets, docs, API baseline, then commit** `feat: add BootstrapDatePicker`.

---

# Cross-Stage Documentation and API Checklist

Run after **every** stage:

- [ ] Finalize that component's contract in `docs/COMPONENTS.md` and remove it from the deferred list.
- [ ] Update `docs/ARCHITECTURE.md` only when a real new dependency/composition edge has been introduced.
- [ ] Add automated/manual coverage notes to `docs/TESTING.md`.
- [ ] Add the supported component to `README.md` and `docs/PACKAGE_README.md`.
- [ ] Add an `Unreleased` changelog entry without rewriting `1.0.0-rc.1` history.
- [ ] Run `Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline` and inspect the reconstructed exported surface before accepting a new fingerprint.
- [ ] Update `docs/PUBLIC_API_BASELINE.md` with the approved fingerprint and the named compatible API addition.
- [ ] Run `dotnet build -c Release -f net48`.
- [ ] Run `dotnet build -c Release -f net8.0-windows`.
- [ ] Run `dotnet test -c Release` or the repository's `build.ps1` + `test.ps1` sequence.
- [ ] Run the relevant demo under Light/Dark and Windows display scaling at 100%, 125%, 150%, 175%, and 200%.
- [ ] Repeatedly create/dispose the new component and verify no obvious unbounded GDI handles, USER handles, timers, or event subscriptions.

---

# Explicitly Deferred Scope

Do not pull these into the nine stages without a separate approved plan:

- Rich/HTML Alert or Tooltip content.
- Interactive tooltip/popover content.
- Explicit Popper-like Tooltip placement/collision engine.
- Nested Dropdown submenus or arbitrary hosted controls.
- Split-button Dropdown.
- Multi-select/tokenized ComboBox.
- Async/remote ComboBox lookup.
- Fully custom calendar rendering.
- Date ranges or multi-date selection.
- Global/top-level Toast service that creates its own windows.
- Toast notification history/notification center.
- Broad migration to a new public shared size enum across existing controls.

---

# Final Completion Gate

This roadmap is complete only when:

1. Badge, Alert, Tooltip, Tabs, NumericBox, ComboBox, Dropdown, Toast, and DatePicker each have a finalized documented public contract.
2. Both target frameworks build and all automated tests pass.
3. Feedback, Navigation, and Advanced Inputs demo pages expose every new component and relevant interaction state.
4. Runtime Light/Dark switching works without recreating the application.
5. Interactive controls preserve keyboard/focus behavior and native semantic behavior where delegated.
6. Manual DPI verification passes at 100%, 125%, 150%, 175%, and 200%.
7. No component introduces duplicate Theme, Rendering, Icons, Compatibility, or Animation infrastructure.
8. Public API fingerprint additions were reviewed incrementally at each stage rather than accepted once at the end.
9. Resource/lifecycle stress checks show no obvious unbounded GDI, USER, event, or timer growth.
10. Deferred features remain deferred until a separate plan explicitly approves them.
