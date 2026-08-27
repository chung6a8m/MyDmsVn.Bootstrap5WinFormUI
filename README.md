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

The library is intended to multi-target:

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

Suggested child namespaces include:

```text
MyDmsVn.Bootstrap5WinFormUI.Theme
MyDmsVn.Bootstrap5WinFormUI.Animation
MyDmsVn.Bootstrap5WinFormUI.Icons
MyDmsVn.Bootstrap5WinFormUI.Rendering
MyDmsVn.Bootstrap5WinFormUI.Controls
MyDmsVn.Bootstrap5WinFormUI.Compatibility
```

## Planned core components

- Theme system and Bootstrap-inspired palette
- Shared animation primitives
- Icon abstraction with SVG, Segoe MDL2 Assets, and optional FontAwesome.Sharp integration
- `BootstrapButton`
- `BootstrapSpinner`
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

The repository is currently in the architecture and foundation stage. The files under `idea-drafs/` are historical design conversations and implementation sketches. They are useful context, but they are **not authoritative specifications** and code from those files must not be copied blindly.

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
