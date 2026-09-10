# AI_CONTEXT.md

Compact working context for AI assistants.

## Identity

- Repository: `chung6a8m/MyDmsVn.Bootstrap5WinFormUI`
- Product: Bootstrap-inspired native WinForms UI framework
- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- Target frameworks: `net48;net8.0-windows`
- UI technology: native Windows Forms

## Current source of truth

The active implementation queue is `docs/ROADMAP.md`. It contains only unfinished planned work and defines execution order unless the user explicitly overrides it.

Read only the current plan being implemented. Do not preload all active plans, old phase documents, `docs/archive/`, or `idea-drafs/`.

Historical roadmaps are archived under `docs/archive/` and are not current specifications.

## Product model

Bootstrap supplies visual language and component ideas. Native WinForms remains authoritative for desktop behavior wherever practical.

```text
Compatibility
Theme ---- Rendering ---- Icons
   \          |          /
    \         |         /
        Animation
            |
      Primitive controls
            |
      Composite controls
```

Foundation infrastructure is shared. Composite controls compose primitives instead of duplicating behavior engines.

## Stable decisions

- Use semantic theme/design-system tokens rather than component-local Bootstrap constants.
- Runtime theme changes flow through `BootstrapThemeManager`.
- Keep `net48` compatibility explicit and first-class.
- Prefer native value, focus, keyboard, accessibility, layout, popup, and lifecycle semantics when a suitable WinForms control exists.
- FontAwesome.Sharp integration is optional and must not become a core dependency.
- Animation and scheduling are shared; avoid ad-hoc control-local timers.
- Designer safety, DPI scaling, accessibility, keyboard behavior, and GDI/event lifecycle are product requirements.
- Public APIs should stay small, coherent, and native-friendly.

## Unattended WinForms test safety

- Use `./test.ps1` for the full suite.
- Follow `docs/WINFORMS_TEST_EXECUTION.md` for handle-based/UI tests.
- Focused UI-capable `dotnet test` runs require bounded hang detection.
- Tests must not leave exception dialogs, `MessageBox`, unbounded modal windows, or other human-interaction blockers open.
- Fail-fast guards belong in test infrastructure unless failure behavior is genuinely part of the production contract.

## Load supporting context only when needed

- Architecture/ownership: `docs/ARCHITECTURE.md`
- Theme/typography/metrics: `docs/DESIGN_SYSTEM.md`
- Existing component contract: relevant section of `docs/COMPONENTS.md`
- Cross-target concerns: `docs/COMPATIBILITY.md`
- Tests: `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`
- Public/protected API changes: `docs/PUBLIC_API_BASELINE.md`
- Product-scope ambiguity: `docs/PRD.md`, then `docs/DECISIONS.md`

## Precedence

When information conflicts, use this order:

1. Explicit current user instruction
2. The current active plan
3. `docs/DECISIONS.md`
4. `docs/PRD.md`
5. `docs/ARCHITECTURE.md`
6. Relevant current component/design/testing documentation
7. Archived/historical material

Read `AGENTS.md` for repository execution rules.