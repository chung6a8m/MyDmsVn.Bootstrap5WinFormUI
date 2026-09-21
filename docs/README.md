# Documentation

Current development documentation for `MyDmsVn.Bootstrap5WinFormUI`.

## Start here

The previous active roadmap is complete. There are currently no active plan files to preload.

For normal implementation work:

1. Read root `AI_CONTEXT.md` and `AGENTS.md`.
2. Read only the current task's relevant source files.
3. Load the supporting documents below only when the task needs them.
4. Read [ROADMAP.md](ROADMAP.md) only for planning/roadmap work or when a task explicitly refers to planned work.

Historical plans, phase notes, roadmaps, and superseded design material are under [archive/](archive/) and should not be preloaded into coding-agent context.

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

The root [CHANGELOG.md](../CHANGELOG.md) remains the release-facing summary.

## Historical notes

`docs/archive/` and `idea-drafs/` are design/development history. They may explain why a decision was made, but current user instructions and current architecture/product documents take precedence.
