# MyDmsVn.Bootstrap5WinFormUI

A Bootstrap-inspired native Windows Forms UI framework for business desktop applications.

The project translates the visual language and component ideas of Bootstrap 5 into reusable WinForms controls. It is **not** a Bootstrap CSS/JavaScript port and does not require a browser or WebView.

## Project goals

- Native WinForms controls with a consistent Bootstrap 5-inspired visual language.
- Light and Dark themes driven by shared design tokens.
- Shared infrastructure for animation, icon rendering, DPI scaling, painting, and resource management.
- Designer-friendly controls with stable, predictable public APIs.
- Support for both .NET Framework 4.8 and .NET 8 on Windows.
- Good keyboard, focus, accessibility, DPI, and resource-management behavior.

## Target frameworks

The library multi-targets:

```xml
<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
<UseWindowsForms>true</UseWindowsForms>
```

`net8.0-windows` is the WinForms-specific TFM for the requested .NET 8 target.

## Namespace

All product code uses the root namespace:

```text
MyDmsVn.Bootstrap5WinFormUI
```

Primary child namespaces include:

```text
MyDmsVn.Bootstrap5WinFormUI.Theme
MyDmsVn.Bootstrap5WinFormUI.Animation
MyDmsVn.Bootstrap5WinFormUI.Icons
MyDmsVn.Bootstrap5WinFormUI.Rendering
MyDmsVn.Bootstrap5WinFormUI.Controls
MyDmsVn.Bootstrap5WinFormUI.Compatibility
```

## Implemented foundation components

- Theme system and Bootstrap-inspired palette
- Shared rendering and DPI helpers
- Shared finite/loop animation primitives
- Source-neutral icon abstraction
- `BootstrapSpinner`
- `BootstrapButton`
- `BootstrapButtonGroup`
- `BootstrapButtonToolbar`
- `BootstrapTextBox`
- `BootstrapNumericBox`
- `BootstrapCard`
- `BootstrapCollapse`
- `BootstrapAccordion`
- `BootstrapAccordionHeader`
- `BootstrapProgressBar`
- `BootstrapSidebar`
- `BootstrapDataGridView`
- `BootstrapPagination`
- `BootstrapBadge`
- `BootstrapAlert`
- `BootstrapTooltip`
- `BootstrapTabControl`

## Integrated demo

The demo project is a single navigable showcase using `BootstrapSidebar` as the application navigation shell. Its root pages are Theme, Buttons / Groups / Toolbar, Inputs, Advanced Inputs, Cards, Feedback, Collapse / Accordion, Loading / Spinner, Progress, Sidebar, DataGrid, Pagination, and Navigation / Tabs. Light/Dark switching and Reduced motion remain available while navigating.

The Advanced Inputs page is the shared native-backed input showcase. Stage 5 adds `BootstrapNumericBox` examples for integer/default values, decimal formatting/increments, thousands separators, signed ranges, validation states, read-only behavior, disabled behavior, and live `ValueChanged` feedback. Later ComboBox and DatePicker stages extend the same page rather than creating competing top-level demos.

The Feedback page hosts the component-expansion feedback controls. `BootstrapBadge` covers semantic, pill/custom/disabled, and long-text states; `BootstrapAlert` adds all semantic variants, optional icons, native keyboard-accessible dismissal, multiline/disabled/custom-radius states, and restore cycles; `BootstrapTooltip` adds default Dark, semantic and custom-color owner-drawn popups, explicit multiline/long captions, one Tooltip associated with multiple controls, and live native timing/state forwarding. The page remains the shared runtime Light/Dark and real-Windows 100–200% DPI verification surface.

The Pagination page demonstrates bounded numeric windows, ellipses, navigation visibility, size variants, boundary/zero-item states, and application-owned DataGrid paging. `BootstrapPagination` itself does not own or slice a data source.

The Navigation / Tabs page demonstrates `BootstrapTabControl` using native `TabPage` composition and selection with Bootstrap-inspired Tabs, Pills, and Underline header styles. It includes uniform Fill sizing, all semantic variants, `ImageList`/`ImageKey`/`ImageIndex`, native tooltip text, disabled pages, long labels, and live `SelectedIndexChanged` feedback.

Earlier Rendering / DPI, Icons, and Animation diagnostics remain available below the Theme navigation item.

See [Phase 14 — Integrated Demo Application](docs/PHASE14_INTEGRATED_DEMO.md) for the original navigation contract and manual verification matrix, and [Component contracts](docs/COMPONENTS.md) for current component behavior.

## Release candidate

Phase 16 prepares package `MyDmsVn.Bootstrap5WinFormUI` version `1.0.0-rc.1`. The v1 assembly compatibility version is `1.0.0.0`, and the proposed public/protected API is protected by a deterministic fingerprint test.

Create and validate the candidate locally with:

```powershell
pwsh ./release.ps1 -Configuration Release -Version 1.0.0-rc.1
```

The script produces `.nupkg`, `.snupkg`, SHA-256 checksums, and a release manifest under `artifacts/release`. CI runs the same package validation after both-target builds/tests and uploads the verified candidate as a workflow artifact. It does **not** automatically publish to NuGet.org.

See [Release process](docs/RELEASING.md), [v1 public API baseline](docs/PUBLIC_API_BASELINE.md), [supported build environment](docs/BUILD_ENVIRONMENT.md), and [Phase 16 release preparation](docs/PHASE16_RELEASE_PREPARATION.md).

## Hardening status

Phase 15 adds foundation-wide hardening gates for the 100–200% logical DPI matrix, runtime theme-switch stress, rapid state reversal, static-event lifetime cleanup, optional icon dependency boundaries, prototype API aliases, and XML documentation completeness. The core library treats compiler warnings as errors on both target frameworks.

Pagination extends that hardened surface without adding a new timer, theme subscription, rendering stack, data-source abstraction, or package dependency. It composes the existing `BootstrapButtonGroup` and `BootstrapButton` controls.

Badge extends the same hardened foundations as a primitive visual control: it reuses `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `RoundedPath`, and theme typography; owns one deterministic theme subscription/theme-created font lifecycle; and adds no timer, animation scheduler, icon model, geometry library, or external package.

Alert reuses those same rendering/theme primitives plus the source-neutral icon infrastructure. It owns one private native WinForms dismiss button, one deterministic theme subscription/theme-created font lifecycle, and no timeout, timer, overlay, floating host, queue manager, or Toast behavior.

Tooltip is a thin `Component + IExtenderProvider` wrapper over one owned native WinForms `ToolTip`. Native association, popup placement and delay semantics remain native; the framework owns only owner-drawn Bootstrap-inspired presentation. It resolves the current theme at popup/draw time, DPI-scales padding/border/radius through the shared rendering foundation, adds no timer or static theme subscription, and keeps the native `ToolTip` private.

Tabs remain native-backed: `BootstrapTabControl` derives from WinForms `TabControl`, preserves `TabPage`, `TabPages`, selection/events, focus/keyboard, native images/tooltips, and overflow behavior, and custom-paints only the native header rectangles. Header metrics reuse Theme/DPI/Rendering helpers; the control owns one deterministic theme subscription/theme-created font lifecycle and introduces no timer, animation engine, page wrapper, custom window, or external package.

NumericBox is likewise native-backed: `BootstrapNumericBox` owns one borderless WinForms `NumericUpDown` and forwards value/range/increment/formatting/read-only semantics directly. The wrapper owns the single public tab stop, themed shell, validation/focus presentation, DPI layout, and theme/font lifecycle without replacing native parsing, spin, wheel, or boundary behavior.

See [Phase 15 — Hardening and API review](docs/PHASE15_HARDENING_AND_API_REVIEW.md) for the audit findings and the real-Windows/manual checks carried into release validation.

## Documentation

Start with [docs/README.md](docs/README.md).

The primary sources of truth are:

- [Product requirements](docs/PRD.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development plan](docs/DEVELOPMENT_PLAN.md)
- [Design system](docs/DESIGN_SYSTEM.md)
- [Component contracts](docs/COMPONENTS.md)
- [Compatibility rules](docs/COMPATIBILITY.md)
- [Testing strategy](docs/TESTING.md)
- [Development/contribution guide](docs/CONTRIBUTING.md)
- [Architecture decisions](docs/DECISIONS.md)
- [Release process](docs/RELEASING.md)
- [Changelog](CHANGELOG.md)

## Status

Phases 0–16 of the foundation development plan are implemented through release preparation. `BootstrapPagination`, Stage 1 `BootstrapBadge`, Stage 2 `BootstrapAlert`, Stage 3 `BootstrapTooltip`, Stage 4 `BootstrapTabControl`, and Stage 5 `BootstrapNumericBox` are now documented compatible control additions on top of that foundation. The current package line remains `1.0.0-rc.1`; promotion to stable `1.0.0` remains gated by the manual release matrix recorded in `docs/RELEASING.md`.

The files under `idea-drafs/` remain historical design conversations and implementation sketches. They are useful context, but they are **not authoritative specifications** and code from those files must not be copied blindly.

## Guiding principle

Build from shared foundations upward:

```text
Compatibility + Theme + Rendering + Icons
                  |
             Animation
                  |
          Primitive controls
                  |
          Composite controls
                  |
        Demo + tests + docs
```

Do not implement controls in an arbitrary order or duplicate infrastructure that already exists.
