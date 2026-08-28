# Changelog

All notable release-facing changes to this project are documented here.

## [Unreleased]

### Added

- `BootstrapPagination`, a data-source-agnostic composite page-navigation control built from the existing `BootstrapButtonGroup` and `BootstrapButton` controls.
- Bounded numeric page windows with ellipses, First/Previous/Next/Last navigation, Small/Default/Large button sizing, semantic variants, and connected outer-radius forwarding.
- Integrated Pagination demo including caller-owned `BootstrapDataGridView` page slicing through `PageChanged`.
- `BootstrapBadge`, a compact auto-sized, non-focusable text indicator with all semantic variants, custom color override, pill/default radius modes, disabled presentation, runtime theme switching, and DPI-scaled layout.
- Integrated Feedback demo page covering Badge semantic variants, pill/custom/disabled/long-text scenarios, runtime Light/Dark switching, and the real-Windows 100–200% DPI verification path.
- `BootstrapAlert`, an inline `UserControl` feedback surface with all semantic variants, optional source-neutral icons, one native keyboard-accessible dismiss button, deterministic `Dismiss()` / `Dismissed` behavior, multiline/disabled/custom-radius presentation, runtime Light/Dark switching, and DPI-scaled layout.
- Expanded the shared Feedback demo with Alert icon/dismissal/multiline/disabled/radius scenarios plus repeated restore cycles without reconstructing alerts.
- `BootstrapTooltip`, a designer-safe `Component + IExtenderProvider` over one owned native WinForms `ToolTip`, with Bootstrap semantic/custom owner-drawn presentation, DPI-scaled padding/border/radius, current-theme `BodySmall` typography, direct native timing/state forwarding, and deterministic ownership/disposal.
- Expanded the shared Feedback demo with default Dark, semantic Info, custom-color, explicit multiline/long-caption, multiple-control association, and live Tooltip timing/state scenarios.
- `BootstrapTabControl`, a native-backed WinForms `TabControl` with Bootstrap-inspired Tabs, Pills, and Underline header styles, semantic selected accents, uniform Fill sizing, DPI-scaled header metrics, runtime Light/Dark switching, and deterministic theme/font lifecycle.
- Integrated Navigation / Tabs demo covering all three header styles, all eight semantic variants, native `TabPage` composition and `SelectedIndexChanged`, Fill sizing, `ImageList`/`ImageKey`/`ImageIndex`, tooltip text, disabled pages, and long labels.
- `BootstrapNumericBox`, a native-backed numeric input that owns one borderless WinForms `NumericUpDown`, directly forwards value/range/increment/formatting/read-only semantics, and adds Bootstrap-themed validation/focus/radius/DPI presentation with a single wrapper `ValueChanged` path.
- Integrated Advanced Inputs demo page with integer/default, decimal, thousands-separator, signed-range, valid/invalid, read-only, disabled, and live NumericBox `ValueChanged` scenarios; the page is intentionally reusable by later ComboBox and DatePicker stages.
- `BootstrapComboBox`, a direct native WinForms `ComboBox` subclass that preserves native items, binding, selection, editable text, autocomplete, keyboard/drop-down behavior, and events while adding fixed-height owner-draw presentation, validation/focus shell rendering, runtime theme/DPI handling, configurable radius, and an optional source-neutral leading icon.
- Expanded the shared Advanced Inputs demo with unbound and bound ComboBox data, `DisplayMember` / `ValueMember`, editable `DropDown`, selection-only `DropDownList`, native `SuggestAppend` autocomplete, long-text ellipsis, icon/no-icon, validation, disabled, explicit-radius, and native selection-feedback scenarios without adding a second navigation page.
- `BootstrapDropdown`, a Bootstrap-inspired command popup that composes a caller-owned `BootstrapButton` with one owned native `ToolStripDropDownMenu`, snapshots text/icon/checked/disabled/separator models at each opening, and keeps native focus, keyboard navigation, AutoClose/outside-click dismissal, and working-area placement authoritative.
- Dropdown theme/DPI presentation through the shared semantic variant, rendering, icon, and DPI infrastructure, including logical `MinimumWidth`, target `IconRenderer` reuse, runtime open-popup icon refresh, and deterministic native-item/bitmap cleanup without custom popup windows, global hooks, timers, or animation infrastructure.
- Expanded the shared Navigation / Tabs demo with basic, icon, checked/disabled/separator, long-menu, and stress/theme Dropdown scenarios plus a real-desktop keyboard, outside-click, working-area, multi-monitor, and 100–200% DPI verification matrix; no second Navigation route was added.
- `BootstrapToast`, `BootstrapToastContainer`, and four-corner `BootstrapToastPlacement`, providing application-placed transient feedback with explicit ownership transfer, FIFO/max-visible queueing, shared Alert/Toast palette rules, deterministic enter/exit and reflow animation, post-enter auto-hide timing, reduced-motion support, and container-owned dismissal/disposal; the shared Feedback demo includes manual/auto-hide, icon/multiline, burst queue, placement, rapid-dismiss, disabled, Dismiss All, and 100-toast stress scenarios.

### Changed

- Reviewed and intentionally updated the proposed v1 public API fingerprint to include the compatible exported `BootstrapPagination`, `BootstrapBadge`, `BootstrapAlert`, `BootstrapTooltip`, `BootstrapTabStyle`, `BootstrapTabControl`, `BootstrapNumericBox`, `BootstrapComboBox`, `BootstrapDropdown`, `BootstrapToastPlacement`, `BootstrapToast`, and `BootstrapToastContainer` surfaces; all Toast palette/layout/timer/ownership/animation seams remain internal and `AssemblyVersion` remains `1.0.0.0`.

## [1.0.0-rc.1] - 2026-08-28

### Added

- Native WinForms Bootstrap-inspired theme/design-token system with Light/Dark runtime switching and reduced-motion preference.
- Shared DPI/rendering, icon, and finite/loop animation foundations.
- `BootstrapSpinner`, `BootstrapButton`, `BootstrapButtonGroup`, `BootstrapButtonToolbar`, `BootstrapTextBox`, `BootstrapCard`, `BootstrapCollapse`, `BootstrapAccordion`, `BootstrapProgressBar`, `BootstrapSidebar`, and `BootstrapDataGridView`.
- Integrated demo application covering the complete foundation surface.
- Cross-target automated tests for `net48` and `net8.0-windows`, including hardening gates for DPI logic, theme subscriptions, rapid state reversal, optional dependency boundaries, prototype aliases, and XML documentation.
- Release-candidate packaging, symbols, manifest/checksum generation, and v1 public API fingerprint protection.

### Compatibility

- Package target frameworks: `net48` and `net8.0-windows`.
- Proposed v1 public API baseline is frozen from this RC; see `docs/PUBLIC_API_BASELINE.md`.

### Known release checks

Real Windows DPI visual validation, Visual Studio Designer validation, process-level GDI/USER soak testing, and application-specific large DataGrid profiling remain manual stable-release gates. See `docs/RELEASING.md`.
