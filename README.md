# MyDmsVn.Bootstrap5WinFormUI

A Bootstrap-inspired native Windows Forms UI framework for business desktop applications. The project translates Bootstrap 5 visual language and component ideas into reusable WinForms controls; it is not a CSS/JavaScript port and does not require a browser or WebView.

## Current development

The previously active roadmap has been completed. [docs/ROADMAP.md](./docs/ROADMAP.md) is intentionally kept as a small gate file and currently contains no active work.

Do not infer new implementation work from archived plans. New planned work should be added explicitly to the active roadmap when needed. Historical plans, roadmaps, phase notes, and design history live under [docs/archive/](./docs/archive/).

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

The repository contains the shared foundation plus a broad control set, including buttons/groups/toolbars, the native-backed ToolStrip/MenuStrip/ContextMenuStrip/StatusStrip family, text, numeric, and range inputs, checks/radios/switches, combo/select/lookup/input-group/date/calendar controls, cards/collapse/accordion/progress/placeholder/sidebar/tree/list/data-grid/pagination/breadcrumb, modal dialogs, badge/alert/tooltip/popover/tabs/dropdown/split-button/toast, and supporting infrastructure.

See [docs/COMPONENTS.md](./docs/COMPONENTS.md) for component contracts and [docs/PACKAGE_README.md](./docs/PACKAGE_README.md) for detailed usage-oriented documentation.

## Build and test

Use Visual Studio 2022 or a supported Windows .NET SDK/toolchain.

```powershell
dotnet build
./test.ps1
```

`./test.ps1` is the preferred full-suite entry point because WinForms tests require bounded unattended execution. For focused raw `dotnet test` runs that can create UI handles, follow [docs/WINFORMS_TEST_EXECUTION.md](./docs/WINFORMS_TEST_EXECUTION.md).

## Documentation

For normal coding work, keep context intentionally small:

1. [AI_CONTEXT.md](./AI_CONTEXT.md) — compact project context for AI agents.
2. [AGENTS.md](./AGENTS.md) — repository execution rules.
3. [docs/README.md](./docs/README.md) — current documentation map by concern.
4. Load only the task-relevant architecture/component/testing documents.
5. Read [docs/ROADMAP.md](./docs/ROADMAP.md) only for roadmap/planning work or when a task explicitly refers to active planned work.

Historical material under `docs/archive/` and `idea-drafs/` is context-on-demand only and is not authoritative for current work.
