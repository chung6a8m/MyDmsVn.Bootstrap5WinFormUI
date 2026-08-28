# Changelog

All notable release-facing changes to this project are documented here.

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
