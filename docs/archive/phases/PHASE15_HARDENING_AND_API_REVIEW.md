# Phase 15 — Hardening and API review

Phase 15 hardens the implemented foundation before release preparation. It does not introduce a new visual component. The work converts the development-plan checklist into repeatable build/test gates, reviews the exported API, fixes defects exposed by the audit, and records validation that still requires a real Windows desktop environment.

## Scope completed

The Phase 15 review covers:

- DPI behavior across the 100%, 125%, 150%, 175%, and 200% logical scale matrix.
- Runtime theme switching and deterministic theme-event subscription cleanup.
- Rapid animation state reversal under Reduced motion.
- Animation, event, timer, GDI, font, and owned-resource lifetime review.
- Public naming consistency and removal/prevention of known prototype aliases.
- XML documentation completeness for the core assembly.
- Optional icon dependency boundaries.
- Compiler warning cleanup discovered during the hardening pass.
- Known limitations that remain relevant for Phase 16 release preparation.

## Automated hardening gates

`tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Hardening/Phase15HardeningTests.cs` adds cross-framework tests for the foundation-wide concerns below.

### DPI matrix

The shared `DpiScaler` is exercised at the exact Windows DPI values corresponding to the requested matrix:

| Scale | DPI |
| ---: | ---: |
| 100% | 96 |
| 125% | 120 |
| 150% | 144 |
| 175% | 168 |
| 200% | 192 |

The test verifies shared `Size`, `Padding`, and `Rectangle` geometry scaling at every matrix point. Component-specific DPI tests remain in their existing suites.

This is a deterministic logical-scaling gate. It is intentionally not reported as a substitute for changing the Windows display scale and visually inspecting the application on a physical/interactive desktop; that remains a manual verification item documented below.

### Theme switch stress and event lifetime

The hardening suite creates representative controls from the foundation and switches the global theme 50 times while alternating Light/Dark and Reduced motion states. The representative set includes Button, Spinner, TextBox, Card, Collapse, Accordion, ProgressBar, Sidebar, and DataGridView.

The test records the `BootstrapThemeManager.ThemeChanged` subscriber count before control creation, disposes every created control, and requires the subscriber count to return to the baseline. A subsequent theme switch must remain safe. This protects against the most important static-event retention failure mode in the current architecture.

### Rapid state reversal

Collapse and Sidebar are toggled 101 times under Reduced motion and must converge to the requested collapsed state without a pending Collapse animation. Existing component tests continue to cover normal animated progress and reversal behavior.

### Child lifecycle defense

The hardening pass turned the nullable `ControlEventArgs.Control` compiler findings in `BootstrapCollapse` into an explicit failing test before fixing production code. `OnControlAdded` and `OnControlRemoved` now attach/detach child layout handlers only when a child control is present.

This both removes the nullable dereference warnings and makes the lifecycle hooks defensive if they are invoked with an empty event payload.

### Optional dependency boundary

The core assembly reference list is inspected at runtime. The hardening test requires the assembly not to reference:

- `FontAwesome.Sharp`
- `Svg`
- `SkiaSharp`

The framework continues to expose source-neutral icon descriptors/renderers. Applications may provide adapters for external icon libraries without turning those libraries into core runtime dependencies.

### Prototype alias prevention

The exported Controls API is inspected with reflection. The hardening suite rejects known exploratory/prototype aliases including:

- `AnimationTime`
- `TransitionDuration`
- `IsLoading`
- `IsExpanded`
- `Busy`
- `BusyText`

It also requires the historical `Theme.AppTheme` type to remain absent. Canonical contracts use the names established by the current documentation, including `AnimationDuration`, `Loading`, `Expanded`, `Variant`, `CustomColor`, and `BorderRadius` where applicable.

## Compiler and XML documentation gates

The core project now enables:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<WarningsAsErrors>$(WarningsAsErrors);CS1591</WarningsAsErrors>
```

`GenerateDocumentationFile` remains enabled.

This changes API/documentation review from a one-time inspection into a repeatable build rule:

- New core compiler warnings fail the build.
- Missing XML documentation on public API (`CS1591`) fails the build.
- Both target frameworks must satisfy the same gate.

The Phase 15 CI build confirms that the existing exported core API is already covered by XML documentation under this policy.

## Findings fixed during the review

Phase 15 surfaced and corrected the following issues rather than suppressing the diagnostics:

1. `BootstrapCollapse.OnControlAdded` and `OnControlRemoved` dereferenced a nullable `ControlEventArgs.Control`. A regression test was added first; production code now checks the child before subscribing/unsubscribing.
2. `BootstrapSidebarItemButton.Text` and `BootstrapTextBox.Text` intentionally normalize a `null` assignment to `string.Empty`. On .NET 8 their overrides now state the inherited nullable input contract explicitly with `AllowNull` while preserving .NET Framework 4.8 compatibility.
3. `BootstrapSidebarItemButton.HasChildren` intentionally hides the WinForms `Control.HasChildren` member inside an internal rendering helper. The code now declares that hiding explicitly.
4. The Sidebar demo previously dereferenced a system-font property whose modern annotation is nullable. The demo title now uses the owning control's font family.

No prototype compatibility aliases were required or retained by these fixes.

## Lifetime and resource audit

The review confirmed the following ownership rules across the current foundation:

- Controls that subscribe to `BootstrapThemeManager.ThemeChanged` unsubscribe during `Dispose`.
- Collapse, Sidebar, ProgressBar, Spinner, Button loading, and other animated components consume the shared animation infrastructure rather than defining independent component timers.
- Animation instances detach progress/completion handlers and dispose their scheduler/timer ownership when stopped, replaced, or disposed.
- Sidebar owns and disposes its `ToolTip` and detaches recursive collection subscriptions.
- Collapse detaches child layout subscriptions and disposes its active animation.
- Theme-owned/control-owned fonts are disposed when replaced or when the owner is disposed.
- Painting code creates disposable GDI resources such as `Pen`, `Brush`, and `GraphicsPath` in scoped `using` statements.

The automated theme subscription baseline test provides a concrete regression gate for static-event leakage. Process-level USER/GDI handle counting is not reliable in headless CI and remains part of manual soak testing.

## Public API review result

The exported surface is consistent with the current product documentation and design decisions:

- Theme state is represented by `BootstrapTheme` and managed through `BootstrapThemeManager`.
- Control semantic color uses `Variant`; explicit overrides use `CustomColor` where supported.
- Animated components use `AnimationDuration` rather than competing duration aliases.
- Loading state uses `Loading`; collapse/navigation state uses `Expanded`.
- Source-neutral icons are represented by `IconDescriptor` and rendered through `IIconRenderer`/framework renderers.
- Composite controls compose existing primitives instead of exporting duplicate infrastructure concepts.

Phase 15 does not freeze this surface as a v1 compatibility baseline. Stable public API/versioning decisions belong to Phase 16.

## Verification result

The Phase 15 branch is required to pass the repository CI workflow, which builds and tests both supported TFMs on Windows.

After the lifecycle regression was fixed, the hardening suite and all existing tests passed with:

- `net48`: 224 passed, 0 failed, 0 skipped.
- `net8.0-windows`: 224 passed, 0 failed, 0 skipped.

The final branch build additionally requires the core library to compile with warnings treated as errors. CI on the final Phase 15 commit is the authoritative completion gate.

## Known limitations and manual release checks

The following items are deliberately documented rather than hidden behind CI claims:

1. **Real Windows display-scaling validation is manual.** The automated suite covers logical scaling at 96/120/144/168/192 DPI, but Phase 16 should still launch the integrated demo at Windows display scales 100%, 125%, 150%, 175%, and 200% and inspect clipping, text, borders, icons, focus cues, and nested layouts.
2. **Designer validation is environment-dependent.** Visual Studio WinForms Designer behavior should be checked on supported developer environments during release preparation, especially for inherited/composite controls and both target frameworks.
3. **OS font/icon availability varies.** Framework fallbacks keep the core functional, but exact glyph/font rendering may vary across Windows installations.
4. **External icon packages remain application-owned.** The core does not ship or reference FontAwesome.Sharp, a general SVG parser, or SkiaSharp. Applications that need those sources provide adapters/renderers.
5. **No centralized animation scheduler is claimed.** Components share the animation abstraction and lifecycle rules; active animation instances may own scheduler/timer resources through that shared infrastructure.
6. **Process-level GDI/USER-handle soak testing remains manual.** Automated tests verify deterministic disposal/subscription behavior, but long interactive soak sessions and Windows handle counters are release-validation tools rather than CI assertions.
7. **Large real-world DataGrid workloads require manual profiling.** Unit tests protect style/native-behavior contracts; production-scale row counts, custom cell painting, and application-specific data sources should be profiled in an interactive Windows process.

## Phase 15 completion criteria

Phase 15 is complete when all of the following are true on the final branch commit:

- both target frameworks build successfully;
- the core library has no compiler warnings under `TreatWarningsAsErrors`;
- all tests pass on both target frameworks;
- Phase 15 hardening tests remain in the normal suite;
- public XML documentation remains complete under `CS1591` enforcement;
- optional icon packages are not referenced by core;
- prototype aliases remain absent;
- known limitations/manual checks are documented here.

The next development-plan gate is **Phase 16 — Release preparation**.
