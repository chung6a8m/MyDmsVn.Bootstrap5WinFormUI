# Phase 13 — BootstrapDataGridView

Phase 13 adds `BootstrapDataGridView`, a themed subclass of the native WinForms `DataGridView`. The control deliberately keeps the platform grid as the behavioral engine: applications continue to own data sources, columns, editing, sorting, selection, virtual mode, and other standard `DataGridView` APIs.

## Public contract

```text
BootstrapDataGridView : DataGridView

EmptyStateText : string = "No data to display."
Loading        : bool   = false
LoadingText    : string = "Loading..."
```

`EmptyStateText` and `LoadingText` normalize `null` assignments to an empty string. `Loading` is a presentation state only; it does not replace the caller's `DataSource`, modify columns, or overwrite the caller-owned `Enabled` value.

## Theme presentation

The grid listens to `BootstrapThemeManager.ThemeChanged` and maps the active Bootstrap theme to native `DataGridView` styles:

- grid/background surface → `Surface`
- foreground text → `Text`
- grid lines → `Border`
- alternating rows → `SurfaceSecondary`
- selected cells/rows → `Primary` with contrast-safe text
- column and row headers → `SurfaceSecondary` / `Text`

`EnableHeadersVisualStyles` is disabled so system header rendering cannot override framework colors. Runtime Light/Dark changes update the live control without recreating the grid or rebinding data.

The control adopts the theme body font until an application explicitly assigns `Font`. Theme-owned font resources are disposed by the control; caller-assigned fonts remain caller-owned. Equivalent typography tokens are detected so a runtime theme switch does not replace and dispose an otherwise identical live `Font` instance.

## Empty state

When the native grid contains no data rows, `BootstrapDataGridView` paints `EmptyStateText` once in the client area below/alongside visible headers. The native new-row placeholder, when present, does not count as data.

Empty-state rendering is intentionally implemented at the grid level after the normal `DataGridView` paint pass. It does not install `CellPainting` handlers and therefore does not add per-cell allocations to the hot rendering path.

## Loading overlay

The optional loading state composes the existing framework `BootstrapSpinner` with `LoadingText` inside a lightweight overlay. Setting `Loading = true`:

- shows the overlay above the grid presentation;
- starts the composed Spinner;
- keeps the existing data source and columns intact;
- does not change the grid's caller-owned enabled/disabled state.

Setting `Loading = false` hides the overlay and stops the Spinner. Spinner animation, reduced-motion behavior, theme updates, and lifecycle remain owned by the existing Spinner infrastructure; DataGridView does not create another timer or animation engine.

## DPI and lifecycle

Overlay spacing and empty-state insets use `DpiScaler` and theme spacing tokens. Layout is recomputed after parent-DPI changes. Theme subscriptions and owned font resources are released during disposal; child controls release their own resources through normal WinForms disposal.

## DataGridView compatibility boundary

The implementation does not shadow or replace native binding/column APIs. In particular, callers can continue to configure:

- `DataSource` / `BindingSource` / `BindingContext`;
- `AutoGenerateColumns` and explicit `DataGridViewColumn` instances;
- sorting and column order/resize behavior;
- selection and editing policies;
- virtual mode and other standard `DataGridView` options.

Phase 13 owns presentation, empty/loading states, and framework lifecycle only.

## Automated verification

The Phase 13 test suite covers:

- the public type, inheritance, constructor, and added property contract;
- default values and themed header/body/alternate/selection/grid-line colors;
- runtime Light/Dark theme switching;
- caller-owned binding and explicit columns;
- Spinner-backed loading without mutating `DataSource` or `Enabled`;
- `null` normalization for overlay text;
- the demo's real bound DataTable, explicit columns, scenario commands, and MainForm entry point.

Tests execute under the repository's Windows CI for both `net48` and `net8.0-windows`.

## Demo and manual verification

Launch the demo and choose **DataGrid**. The page starts with a real `DataTable` binding and five explicit columns: ID, Customer, Status, Total, and Updated.

Use the command bar to verify:

1. **Load sample** — normal bound rows, sorting, selection, editing policy, resize/reorder behavior.
2. **Show empty** — themed empty-state rendering.
3. **Load 10,000 rows** — large-row-count scrolling/sorting/resize/reorder responsiveness without custom per-cell painting.
4. **Toggle loading** — Spinner-backed overlay on top of the existing bound grid.

Switch Light/Dark from the main demo while the DataGrid window is open, then repeat the scenarios at the supported Windows DPI settings. Large-row smoothness remains a visual/manual performance gate because subjective UI responsiveness is not meaningfully proven by a timing assertion in CI.
