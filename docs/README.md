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
10. [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md) — supported Windows, Visual Studio, SDK, targeting-pack, and script baseline.
11. [PUBLIC_API_BASELINE.md](PUBLIC_API_BASELINE.md) — frozen v1 exported/protected API fingerprint and compatibility policy.
12. [RELEASING.md](RELEASING.md) — RC creation, package validation, CI artifacts, manual matrix, and stable-promotion process.
13. [PACKAGE_README.md](PACKAGE_README.md) — self-contained README embedded in the NuGet package.
14. [BOOTSTRAP_SELECT.md](BOOTSTRAP_SELECT.md) — Select2-style `BootstrapSelect` local/async usage, identity, grouping/custom values, matcher/renderer extension points, retry, keyboard/accessibility, ownership, and desktop verification matrix.
15. [BOOTSTRAP_INPUT_GROUP.md](BOOTSTRAP_INPUT_GROUP.md) — connected input composition, supported child matrix, sizing/measurement, compression, reorder, RTL, and verification.
15. [PHASE8_HARDENING.md](PHASE8_HARDENING.md) — post-review hardening notes for TextBox keyboard event forwarding and Card decoration-safe layout.
16. [PHASE9_BOOTSTRAP_COLLAPSE.md](PHASE9_BOOTSTRAP_COLLAPSE.md) — finalized Collapse contract, height measurement/reversal behavior, tests, and manual verification matrix.
17. [PHASE11_BOOTSTRAP_PROGRESS_BAR.md](PHASE11_BOOTSTRAP_PROGRESS_BAR.md) — finalized ProgressBar range, formatting, animation, indeterminate, lifecycle, tests, and manual verification contract.
18. [PHASE12_BOOTSTRAP_SIDEBAR.md](PHASE12_BOOTSTRAP_SIDEBAR.md) — finalized Sidebar navigation, nested Collapse, keyboard/focus, animation, accessibility, tests, and manual verification contract.
19. [PHASE13_BOOTSTRAP_DATA_GRID_VIEW.md](PHASE13_BOOTSTRAP_DATA_GRID_VIEW.md) — finalized DataGridView theming, native-behavior boundary, empty/loading states, tests, demo, and large-row manual performance gate.
20. [PHASE14_INTEGRATED_DEMO.md](PHASE14_INTEGRATED_DEMO.md) — integrated application shell, required navigation pages, retained foundation diagnostics, lifecycle, automated checks, and manual verification matrix.
21. [PHASE15_HARDENING_AND_API_REVIEW.md](PHASE15_HARDENING_AND_API_REVIEW.md) — foundation-wide DPI/theme/animation/lifetime/API hardening, compiler and XML-documentation gates, verification results, and known release limitations.
22. [PHASE16_RELEASE_PREPARATION.md](PHASE16_RELEASE_PREPARATION.md) — package/version metadata, v1 API freeze, release tooling, CI RC artifact, and stable-release manual gates.

Release-facing changes are summarized in the root [CHANGELOG.md](../CHANGELOG.md).

## About `idea-drafs/`

The two Markdown files under `idea-drafs/` are valuable historical discussions. They show how the project evolved from simple theming helpers to a real framework with shared theme, animation, icon, collapse, loading, and composite-control infrastructure.

They are deliberately **not** normalized into current specifications because they also contain exploratory code, obsolete namespaces, inconsistent API names, incompatible runtime calls, and intermediate designs that were later superseded.

Use them to understand reasoning, not as implementation instructions.
