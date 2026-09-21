# Phase 14 — Integrated Demo Application

Phase 14 turns the accumulated component demos into one navigable WinForms showcase that exercises the framework as an application shell would.

## Application shell

`MainForm` is the integrated shell. It uses the framework's own `BootstrapSidebar` for navigation, a persistent header for global demo settings, and one content host for the selected page.

The header keeps these controls available while navigating:

- Light/Dark theme selector
- Reduced-motion toggle
- Sidebar expand/collapse command
- Current page title and description

Changing theme or reduced motion updates `BootstrapThemeManager.CurrentTheme`, so the shell and the currently embedded component demo follow the same runtime theme lifecycle used by normal applications.

## Required pages

The root navigation contains the nine Phase 14 pages in this order:

1. Theme
2. Buttons / Groups / Toolbar
3. Inputs
4. Cards
5. Collapse / Accordion
6. Loading / Spinner
7. Progress
8. Sidebar
9. DataGrid

Pages that combine related existing demos use a small tab host so the original component scenarios remain intact while staying inside the integrated window.

## Foundation diagnostics

Earlier manual verification paths remain reachable as nested entries under **Theme**:

- Rendering / DPI
- Icons
- Animation

This preserves the Phase 2–4 diagnostics without adding extra root pages to the Phase 14 navigation contract.

## Page hosting and lifecycle

Component demo forms are embedded with `TopLevel = false`, borderless, and docked into the content host. Navigating to another page disposes the previous embedded page before creating the next one.

This approach intentionally reuses the existing demo forms instead of duplicating component setup, interaction states, or framework infrastructure. Animated controls therefore continue to use their normal visibility/disposal lifecycle and shared animation primitives.

## Interaction coverage

The integrated pages retain the component scenarios established by previous phases, including:

- Button hover, pressed, focus, disabled, selected, icon, and loading states
- ButtonGroup selection modes and Toolbar layouts
- TextBox placeholder, validation, icons, clear, read-only, password, disabled, and focus states
- Card border, radius, shadow, and Header/Body/Footer composition
- Collapse variable/fixed height, rapid toggles, and Accordion keyboard interaction
- Spinner variants/sizes and reduced-motion behavior
- Progress determinate, striped, animated, indeterminate, and `AnimateTo(...)` scenarios
- Sidebar selection, badges, disabled items, nested navigation, expansion, and keyboard behavior
- DataGrid binding, empty state, loading overlay, selection/sorting, and 10,000-row manual performance scenario

## Automated verification

`IntegratedDemoApplicationTests` verifies that:

- `MainForm` uses `BootstrapSidebar` and exposes the exact nine required root pages.
- Theme is selected initially.
- Selecting DataGrid embeds `DataGridDemoForm` inside the main window rather than opening another top-level application window.
- The global Light/Dark and Reduced motion controls remain available while navigating every root page.

Existing component demo tests continue to validate the detailed scenarios on their respective forms.

## Manual verification

Run the demo for both target frameworks where practical and verify:

1. Navigate through all nine root pages and the three Theme diagnostics.
2. Switch Light → Dark → Light on several pages without recreating the main window.
3. Toggle Reduced motion while Spinner, Progress, Collapse/Accordion, Sidebar, or loading scenarios are active.
4. Collapse and expand the main navigation and confirm page content remains usable.
5. Tab through interactive controls and use Enter/Space where supported; focus indicators must remain visible.
6. Exercise disabled, selected, validation, loading, empty, and other page-specific states.
7. Switch pages repeatedly and confirm no extra top-level demo windows remain open.
8. Repeat visual checks at Windows DPI 100%, 125%, 150%, 175%, and 200% as part of the normal release matrix.

The integrated demo is a manual visual verification surface, not a replacement for the automated unit/STA tests or the dedicated diagnostic pages.
