# MyDmsVn.Bootstrap5WinFormUI

A Bootstrap-inspired native Windows Forms UI framework for business desktop applications. The project translates Bootstrap 5 visual language and component ideas into reusable WinForms controls; it is not a CSS/JavaScript port and does not require a browser or WebView.

## Current development

The ordered queue of unfinished work is [docs/ROADMAP.md](./docs/ROADMAP.md). Work follows that order unless an explicit user instruction changes it.

For historical context, see [Archive](./docs/archive/).

## Platform contract

- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- UI technology: native Windows Forms
- Target frameworks: `net48;net8.0-windows`
- Theme model: shared Light/Dark design tokens and typography
- Compatibility: one shared code path where practical; `net48` remains a first-class target

## Design principles

- Keep native WinForms behavior authoritative whenever a native control already provides the required interaction model.
- Let the framework own Bootstrap-inspired presentation, theme integration, DPI-aware geometry, and reusable composition.
- Reuse shared Theme, Rendering, Animation, Icons, Compatibility, and test infrastructure instead of creating component-local copies.
- Treat keyboard, focus, accessibility, designer safety, DPI behavior, resource lifetime, and unattended test determinism as product requirements.
- Keep public APIs small and predictable; do not expose implementation details only for tests.

## Component coverage

The repository already contains the shared foundation plus a broad control set, including buttons/groups/toolbars, text and numeric inputs, checks/radios/switches, combo/select/lookup/input-group/date/calendar controls, cards/collapse/accordion/progress/sidebar/tree/data-grid/pagination, badge/alert/tooltip/popover/tabs/dropdown/split-button/toast, and supporting infrastructure.

See [docs/COMPONENTS.md](./docs/COMPONENTS.md) for component contracts and [docs/PACKAGE_README.md](./docs/PACKAGE_README.md) for detailed usage-oriented documentation.

## Build and test

Use Visual Studio 2022 or a supported Windows .NET SDK/toolchain.

```powershell
dotnet build
./test.ps1
```

`./test.ps1` is the preferred full-suite entry point because WinForms tests require bounded unattended execution. For focused raw `dotnet test` runs that can create UI handles, follow [docs/WINFORMS_TEST_EXECUTION.md](./docs/WINFORMS_TEST_EXECUTION.md).

## Documentation

Start with:

1. [AI_CONTEXT.md](./AI_CONTEXT.md) — compact project context for AI agents.
2. [docs/ROADMAP.md](./docs/ROADMAP.md) — active ordered work only.
3. [docs/README.md](./docs/README.md) — documentation map by concern.
4. [AGENTS.md](./AGENTS.md) — repository rules for coding agents.

Detailed architecture, design-system, compatibility, testing, API, and release documents live under `docs/`. Historical roadmaps are deliberately separated under `docs/archive/` so they are not treated as current implementation instructions.