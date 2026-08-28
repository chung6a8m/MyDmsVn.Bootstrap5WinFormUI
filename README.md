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

## Integrated demo

The demo project is a single navigable showcase using `BootstrapSidebar` as the application navigation shell. Its root pages are Theme, Buttons / Groups / Toolbar, Inputs, Cards, Feedback, Collapse / Accordion, Loading / Spinner, Progress, Sidebar, DataGrid, and Pagination. Light/Dark switching and Reduced motion remain available while navigating.

The Feedback page hosts the component-expansion feedback controls. `BootstrapBadge` covers semantic, pill/custom/disabled, and long-text states; `BootstrapAlert` adds all semantic variants, optional icons, native keyboard-accessible dismissal, multiline/disabled/custom-radius states, restore cycles, runtime Light/Dark switching, and the documented real-Windows 100–200% DPI check.

The Pagination page demonstrates bounded numeric windows, ellipses, navigation visibility, size variants, boundary/zero-item states, and application-owned DataGrid paging. `BootstrapPagination` itself does not own or slice a data source.

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

Phases 0–16 of the foundation development plan are implemented through release preparation. `BootstrapPagination`, Stage 1 `BootstrapBadge`, and Stage 2 `BootstrapAlert` are now documented compatible control additions on top of that foundation. The current package line remains `1.0.0-rc.1`; promotion to stable `1.0.0` remains gated by the manual release matrix recorded in `docs/RELEASING.md`.

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
