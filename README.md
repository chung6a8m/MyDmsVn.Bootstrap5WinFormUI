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

## Integrated demo

The demo project is a single navigable showcase using `BootstrapSidebar` as the application navigation shell. Its root pages are Theme, Buttons / Groups / Toolbar, Inputs, Cards, Collapse / Accordion, Loading / Spinner, Progress, Sidebar, and DataGrid. Light/Dark switching and Reduced motion remain available while navigating.

Earlier Rendering / DPI, Icons, and Animation diagnostics remain available below the Theme navigation item.

See [Phase 14 — Integrated Demo Application](docs/PHASE14_INTEGRATED_DEMO.md) for the navigation contract and manual verification matrix.

## Hardening status

Phase 15 adds foundation-wide hardening gates for the 100–200% logical DPI matrix, runtime theme-switch stress, rapid state reversal, static-event lifetime cleanup, optional icon dependency boundaries, prototype API aliases, and XML documentation completeness. The core library now treats compiler warnings as errors on both target frameworks.

See [Phase 15 — Hardening and API review](docs/PHASE15_HARDENING_AND_API_REVIEW.md) for the audit findings, automated verification, and the real-Windows/manual checks carried forward to release preparation.

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

## Status

Phases 0–15 of the foundation development plan are implemented through the integrated demo and hardening/API-review gate. Phase 16 — Release preparation — is the next planned phase.

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
