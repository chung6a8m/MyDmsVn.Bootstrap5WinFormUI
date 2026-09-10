# AGENTS.md

Instructions for AI coding agents and automated contributors working in this repository.

## 1. Context discipline

Keep the working context small. At the start of a task, read only:

1. `README.md`
2. `AI_CONTEXT.md`
3. `docs/ROADMAP.md`
4. The single active plan named by the user or selected from the top of `docs/ROADMAP.md`

Then load supporting documents only when the current task needs them:

- `docs/ARCHITECTURE.md` for dependency or ownership questions.
- `docs/DESIGN_SYSTEM.md` for theme, typography, metrics, or rendering decisions.
- The relevant section of `docs/COMPONENTS.md` for an existing component contract.
- `docs/COMPATIBILITY.md` for cross-target/runtime questions.
- `docs/TESTING.md` and `docs/WINFORMS_TEST_EXECUTION.md` when adding or running tests.
- `docs/PUBLIC_API_BASELINE.md` when changing public/protected API.

Do **not** bulk-read `docs/plans/`, `docs/archive/`, completed phase documents, or `idea-drafs/`. Historical material is context-on-demand only and is never authoritative for active work.

## 2. Active roadmap discipline

`docs/ROADMAP.md` is the source of truth for unfinished planned work. Follow its order unless the user explicitly directs otherwise.

- Work on one active plan at a time.
- Read that plan completely before implementation.
- Track progress in the plan's existing task checkboxes/status markers when applicable.
- Do not infer priority from archived roadmaps or filenames.
- When a plan is fully completed, verified, and documented, remove it from the active queue and archive it rather than growing the active roadmap indefinitely.

For historical context, see `docs/archive/`.

## 3. Fixed project constraints

Do not change these without explicit approval:

- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- Project TFMs: `net48;net8.0-windows`
- UI technology: native Windows Forms
- Bootstrap inspiration: visual language and component behavior, not a CSS/JS port
- Core package must not require FontAwesome.Sharp
- Shared infrastructure must be reused instead of duplicated

## 4. Architecture and API rules

- Controls may depend on Theme, Rendering, Icons, Animation, and Compatibility; foundation layers must not depend on concrete controls.
- Prefer native WinForms behavior as the source of truth when a native control already supplies value, focus, keyboard, accessibility, layout, or lifecycle semantics.
- Composite controls should compose existing primitives rather than copy their engines.
- Prefer one shared cross-target code path. Use compatibility helpers or conditional compilation only when an actual framework difference requires it.
- Do not use APIs unavailable on `net48` directly when a compatibility path is required.
- Keep public APIs small and coherent. Do not add aliases or expose internals merely to simplify tests.
- Public API changes require documentation and compatibility review.

## 5. Theme, rendering, and lifecycle

- Use semantic theme tokens instead of embedding repeated colors, spacing, radius, typography, or control-height constants.
- Use DPI helpers for logical-to-device geometry.
- Dispose framework-owned GDI resources and unsubscribe event handlers deterministically.
- Stop animation/scheduling when controls cannot render or are disposed.
- Do not create component-local timers or animation engines when shared infrastructure covers the behavior.
- Designer construction must not depend on application startup state.

## 6. Testing rules

Use `./test.ps1` for the full suite.

For focused raw `dotnet test` runs that can exercise WinForms UI, include bounded hang protection such as `--blame-hang --blame-hang-timeout 5m` unless a different bound is explicitly justified.

GUI-heavy tests must follow `docs/WINFORMS_TEST_EXECUTION.md`:

- use STA where required;
- never leave `MessageBox.Show`, exception dialogs, unbounded `ShowDialog()`, or other modal UI waiting for a human;
- use existing fail-fast test infrastructure, including `DataGridViewTestGuard` where required;
- keep message pumping finite and deterministic;
- fix root causes instead of extending timeouts or weakening assertions.

Every completed implementation must build for both target frameworks and run the relevant automated/manual verification matrix.

## 7. Repository hygiene and definition of done

Respect `.editorconfig` and `.gitattributes`; do not commit generated binaries, `bin/`, `obj/`, package caches, or local IDE state.

A task is complete only when its agreed scope is implemented, both targets build, relevant tests pass, lifecycle/resource handling is correct, documentation reflects public behavior, and no duplicate infrastructure was introduced.