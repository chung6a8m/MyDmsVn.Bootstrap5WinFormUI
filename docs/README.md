# Documentation

Current development documentation for `MyDmsVn.Bootstrap5WinFormUI`.

## Start here

For implementation work, keep context small:

1. [Active Roadmap](ROADMAP.md) — ordered unfinished work only.
2. The single active plan referenced by the roadmap.
3. Supporting documents below only when the current task needs them.

Historical roadmaps are under [archive/](archive/) and should not be preloaded into normal coding-agent context.

## Product and architecture

- [PRD.md](PRD.md) — scope, requirements, acceptance criteria, and non-goals.
- [ARCHITECTURE.md](ARCHITECTURE.md) — dependency direction, ownership, project structure, and lifecycle model.
- [DECISIONS.md](DECISIONS.md) — architectural decisions that should not be rediscovered.
- [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) — theme, typography, colors, spacing, radii, sizing, and states.
- [COMPONENTS.md](COMPONENTS.md) — component responsibilities and public contracts.

## Engineering constraints

- [COMPATIBILITY.md](COMPATIBILITY.md) — `net48` / `net8.0-windows` rules.
- [TESTING.md](TESTING.md) — automated, UI, DPI, theme, lifecycle, and resource testing strategy.
- [WINFORMS_TEST_EXECUTION.md](WINFORMS_TEST_EXECUTION.md) — unattended GUI-test execution and hang prevention.
- [PUBLIC_API_BASELINE.md](PUBLIC_API_BASELINE.md) — stable public/protected API compatibility baseline.
- [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md) — supported Windows, Visual Studio, SDK, and scripts.
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribution workflow and quality gates.

## Usage and release documentation

- [PACKAGE_README.md](PACKAGE_README.md) — detailed package/user-facing documentation.
- [RELEASING.md](RELEASING.md) — release candidate, package validation, CI artifacts, and stable promotion.
- Component-specific guides such as [BOOTSTRAP_SELECT.md](BOOTSTRAP_SELECT.md), [BOOTSTRAP_LOOKUP_BOX.md](BOOTSTRAP_LOOKUP_BOX.md), and [BOOTSTRAP_INPUT_GROUP.md](BOOTSTRAP_INPUT_GROUP.md) should be read only when working on those areas.

Completed phase documents and older plans remain useful as implementation history, but they are not part of the default reading set. The root [CHANGELOG.md](../CHANGELOG.md) remains the release-facing summary.

## Historical notes

`idea-drafs/` and `archive/` contain design/development history. They may explain why a decision was made, but current user instructions, the active plan, and current architecture/product documents take precedence.