# Documentation

This directory contains the authoritative development documentation for `MyDmsVn.Bootstrap5WinFormUI`.

## Read in this order

1. [PRD.md](PRD.md) — product scope, requirements, acceptance criteria, and non-goals.
2. [ARCHITECTURE.md](ARCHITECTURE.md) — boundaries, dependency direction, project structure, and lifecycle model.
3. [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) — colors, typography, spacing, radii, sizing, states, and theme behavior.
4. [COMPONENTS.md](COMPONENTS.md) — component responsibilities and expected public contracts.
5. [COMPATIBILITY.md](COMPATIBILITY.md) — `net48`/`net8.0-windows` compatibility rules.
6. [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) — top-down implementation phases and gates.
7. [TESTING.md](TESTING.md) — automated, UI, DPI, theme, lifecycle, and resource testing strategy.
8. [CONTRIBUTING.md](CONTRIBUTING.md) — human development workflow and quality gates.
9. [DECISIONS.md](DECISIONS.md) — architecture decisions that should not be rediscovered in every task.
10. [PHASE8_HARDENING.md](PHASE8_HARDENING.md) — post-review hardening notes for TextBox keyboard event forwarding and Card decoration-safe layout.
11. [PHASE9_BOOTSTRAP_COLLAPSE.md](PHASE9_BOOTSTRAP_COLLAPSE.md) — finalized Collapse contract, height measurement/reversal behavior, tests, and manual verification matrix.
12. [PHASE11_BOOTSTRAP_PROGRESS_BAR.md](PHASE11_BOOTSTRAP_PROGRESS_BAR.md) — finalized ProgressBar range, formatting, animation, indeterminate, lifecycle, tests, and manual verification contract.
13. [PHASE12_BOOTSTRAP_SIDEBAR.md](PHASE12_BOOTSTRAP_SIDEBAR.md) — finalized Sidebar navigation, nested Collapse, keyboard/focus, animation, accessibility, tests, and manual verification contract.
14. [PHASE13_BOOTSTRAP_DATA_GRID_VIEW.md](PHASE13_BOOTSTRAP_DATA_GRID_VIEW.md) — finalized DataGridView theming, native-behavior boundary, empty/loading states, tests, demo, and large-row manual performance gate.

## About `idea-drafs/`

The two Markdown files under `idea-drafs/` are valuable historical discussions. They show how the project evolved from simple theming helpers to a real framework with shared theme, animation, icon, collapse, loading, and composite-control infrastructure.

They are deliberately **not** normalized into current specifications because they also contain exploratory code, obsolete namespaces, inconsistent API names, incompatible runtime calls, and intermediate designs that were later superseded.

Use them to understand reasoning, not as implementation instructions.