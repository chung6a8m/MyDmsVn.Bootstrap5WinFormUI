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

### Changed

- Reviewed and intentionally updated the proposed v1 public API fingerprint to include the compatible exported `BootstrapPagination`, `BootstrapBadge`, `BootstrapAlert`, and `BootstrapTooltip` surfaces; all component-specific render/layout helpers remain internal and `AssemblyVersion` remains `1.0.0.0`.

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
