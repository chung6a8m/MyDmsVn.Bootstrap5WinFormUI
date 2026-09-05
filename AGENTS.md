# AGENTS.md

Instructions for AI coding agents and automated contributors working in this repository.

## 1. Read before changing code

Before modifying product code, read these files completely:

1. `README.md`
2. `AI_CONTEXT.md`
3. `docs/PRD.md`
4. `docs/ARCHITECTURE.md`
5. `docs/DEVELOPMENT_PLAN.md`
6. `docs/COMPATIBILITY.md`
7. `docs/TESTING.md`
8. `docs/WINFORMS_TEST_EXECUTION.md`
9. The relevant component section in `docs/COMPONENTS.md`

The files under `idea-drafs/` are historical notes only. They contain useful reasoning and prototype code, but also obsolete names, incompatible APIs, and exploratory implementations. Never treat them as the current specification.

## 2. Fixed project constraints

Do not change these without explicit approval:

- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- Runtime targets: .NET Framework 4.8 and .NET 8 for Windows
- Project TFMs: `net48;net8.0-windows`
- UI technology: native Windows Forms
- Bootstrap inspiration level: visual language and component behavior, not a CSS/JS port
- Core package must not require FontAwesome.Sharp
- Shared infrastructure must be reused instead of duplicated

## 3. Dependency direction

Follow the dependency flow defined in `docs/ARCHITECTURE.md`.

In particular:

- Controls may depend on Theme, Rendering, Icons, Animation, and Compatibility.
- Composite controls may depend on primitive controls.
- Foundation layers must not depend on concrete controls.
- `BootstrapAccordion` must compose `BootstrapCollapse`; it must not implement a second collapse animation engine.
- `BootstrapSpinner` and animated/indeterminate progress must use shared loop animation infrastructure.
- Button loading must reuse spinner/loading infrastructure rather than implement its own timer/spinner engine.

## 4. Compatibility rules

Every product change must compile for both target frameworks.

Do not directly use runtime APIs unavailable on `net48`, including APIs such as `Math.Clamp`, unless the call is isolated behind a compatibility helper or conditional compilation.

Prefer one shared code path. Use `#if` only when an actual target-specific implementation is required.

Do not use `Thread.Sleep` or `Task.Delay` as a frame scheduler for UI animation.

## 5. Public API rules

- Prefer a small, coherent API over aliases and convenience duplicates.
- Use consistent names such as `Variant`, `BorderRadius`, `AnimationDuration`, `CustomColor`, `Loading`, and `Expanded`.
- Do not silently rename or remove an approved public member.
- If a public API needs to change, document the reason before changing it.
- Do not expose implementation details merely to make testing easier.
- Public members should have XML documentation before a component is considered complete.

## 6. Theme and rendering rules

- Do not hard-code semantic colors inside controls when an equivalent theme token exists.
- Do not hard-code repeated spacing/radius/control-height values when a theme metric exists.
- Custom-painted controls must use double buffering where appropriate.
- Dispose every owned GDI object (`Pen`, `Brush`, `Font`, `GraphicsPath`, `Bitmap`, `Region`, and similar resources).
- Avoid per-frame allocation in hot animation paths when practical.
- Do not invalidate an entire form when only one control needs repainting.

## 7. Lifecycle rules

- Stop animations when controls are hidden, disposed, or otherwise unable to render.
- Unsubscribe event handlers that can keep controls alive.
- Do not create one independent WinForms `Timer` per control when the shared animation primitive can provide the behavior.
- Designer construction must not require application startup code or initialized global state.

## 8. Development order

Follow `docs/DEVELOPMENT_PLAN.md`. Do not jump to a later component because it is easier or more visually interesting.

At the end of every implementation phase:

1. Build `net48`.
2. Build `net8.0-windows`.
3. Run the relevant automated tests.
4. Run the relevant demo/manual checks when UI behavior changed.
5. Fix failures before starting the next phase.

## 9. Testing expectations

Use the strategy in `docs/TESTING.md` and the unattended WinForms execution rules in `docs/WINFORMS_TEST_EXECUTION.md`.

At minimum, new behavior needs coverage for the pure logic that can be tested without a UI handle. Interactive controls also require an STA-based control test or a documented demo/manual verification path.

Always consider:

- Light and Dark themes
- Enabled/disabled
- Hover/pressed/focus
- Keyboard operation
- DPI scaling
- Rapid state changes
- Animation start/stop/restart/dispose
- Hidden/disposed controls
- GDI/event/timer resource lifetime

### Mandatory unattended WinForms test rules

These rules apply to Codex and every other automated coding agent:

- Use `./test.ps1` for the full suite. It includes bounded hang detection for both target frameworks.
- If running a focused raw `dotnet test`, include `--blame-hang --blame-hang-timeout 5m` (or another explicitly justified bounded timeout). Never start an unbounded GUI test run and wait indefinitely.
- Tests that create WinForms handles or exercise UI interaction must run on STA, normally with NUnit `[Apartment(ApartmentState.STA)]`.
- Never allow `MessageBox.Show`, an unexpected WinForms exception dialog, an unbounded `ShowDialog()`, or another modal UI to wait for human input during automated tests.
- Hosted `DataGridView` interaction tests must attach `DataGridViewTestGuard.FailOnDataError(...)` unless the specific test is intentionally characterizing `DataError`. Keep that opt-out local to the test.
- Do not add fail-fast behavior to production controls merely to make tests deterministic. Test-only guards belong in the test infrastructure.
- If a GUI test hangs, use the hang/blame diagnostics and fix the root cause. Do not solve it by clicking dialogs, skipping tests, weakening assertions, or extending the timeout without evidence.
- Keep `Application.DoEvents()` usage finite and at known synchronization points; do not run an unbounded application message loop inside a normal NUnit test.
- When new GUI-test infrastructure or execution rules are introduced, update `docs/WINFORMS_TEST_EXECUTION.md` in the same change.

## 10. Repository hygiene

- Respect `.editorconfig` and `.gitattributes`.
- Markdown files use UTF-8 and CRLF on checkout.
- Keep files focused by responsibility.
- Do not add an external dependency unless it has a clear architectural need and does not undermine `net48` compatibility.
- Do not commit generated binaries, `bin/`, `obj/`, packages, or local IDE state.

## 11. Definition of done for an agent task

A task is not complete merely because it renders correctly on one machine. It is complete only when its agreed scope is implemented, both targets build, relevant tests pass, lifecycle/resource handling is correct, documentation is updated when public behavior changed, and no duplicate infrastructure was introduced.
