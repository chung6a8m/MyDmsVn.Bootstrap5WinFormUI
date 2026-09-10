# Active Roadmap

This file is the single ordered queue for unfinished planned work. Work from top to bottom unless an explicit current user instruction changes the order.

Do not preload every plan into an agent context. Read `AGENTS.md`, `AI_CONTEXT.md`, this roadmap, and then only the plan currently being implemented.

## Active queue

1. [Integrated Demo Typography Profiles](./plans/20260910-001-integrated-demo-typography-profiles.md)
   - Add runtime `Default`, `Base 14px`, and `Base 16px` typography profiles to the Integrated Demo while preserving core framework defaults and current theme/reduced-motion semantics.
2. [Bootstrap ToolStrip Family](./plans/20260907-001-bootstrap-toolstrip-family.md)
   - Add native-backed `BootstrapToolStrip`, `BootstrapMenuStrip`, `BootstrapContextMenuStrip`, and `BootstrapStatusStrip` with a shared Bootstrap renderer and no parallel menu/layout engine.
3. [BootstrapRange / TrackBar](./plans/20260907-002-bootstrap-range-trackbar.md)
   - Add `BootstrapRange : TrackBar`, gated by verified native `NM_CUSTOMDRAW` support, while preserving native range, input, accessibility, and lifecycle behavior.
4. [BootstrapModal](./plans/20260907-004-bootstrap-modal.md)
   - Add a native `Form`-based Bootstrap modal that keeps `ShowDialog` semantics authoritative and layers theme, backdrop, sizing, placement, and reduced-motion transitions on top.
5. [BootstrapListGroup](./plans/20260907-005-bootstrap-list-group.md)
   - Add a lightweight composition control for short static/actionable lists without duplicating `BootstrapListView` or large-data responsibilities.
6. [BootstrapBreadcrumb](./plans/20260907-006-bootstrap-breadcrumb.md)
   - Add an ordered hierarchy/navigation trail built from native links/labels with deterministic wrapping, RTL layout, and native keyboard/accessibility behavior.
7. [BootstrapPlaceholder / Skeleton](./plans/20260907-007-bootstrap-placeholder-skeleton.md)
   - Add the owner-painted `BootstrapPlaceholder` primitive with Bootstrap-compatible sizing and Glow/Wave animation; Skeleton remains normal WinForms composition rather than a second framework.

## Execution rules

- The sequence above is intentional. Do not reorder it based on filename, perceived difficulty, or archived roadmaps.
- Work on one plan at a time and read that plan completely before implementation.
- Use the plan's existing task checkboxes/status markers as the detailed progress record.
- Load architecture, design-system, component, compatibility, API, and testing documents only when the current task requires them.
- After a plan is completely implemented and verified on both targets, remove it from this active queue and archive the completed plan instead of accumulating history here.

For historical context, see [Archive](./archive/).