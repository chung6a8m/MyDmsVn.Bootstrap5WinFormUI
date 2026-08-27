# AI_CONTEXT.md

Compact project context for AI assistants.

## Identity

- Repository: `chung6a8m/MyDmsVn.Bootstrap5WinFormUI`
- Product: Bootstrap-inspired native WinForms UI framework
- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- Required runtimes: .NET Framework 4.8 and .NET 8 on Windows
- Intended TFMs: `net48;net8.0-windows`

## Product intent

Create a small, maintainable WinForms design system that borrows Bootstrap 5's visual language, semantic variants, component concepts, and interaction expectations while remaining completely native WinForms.

This is not a CSS runtime, browser wrapper, WebView solution, or pixel-perfect Bootstrap port.

## Architecture summary

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

Foundation logic is shared. Composite controls compose primitives instead of copying their behavior.

## High-priority components

1. Theme, metrics, typography, DPI/rendering helpers
2. Icon abstraction
3. Animation primitives
4. Spinner
5. Button + loading
6. ButtonGroup + ButtonToolbar
7. TextBox + Card
8. Collapse
9. Accordion + AccordionHeader
10. ProgressBar
11. Sidebar
12. DataGridView

## Key design decisions

- Use semantic theme tokens; controls should not embed Bootstrap hex values directly.
- Runtime theme changes are event-driven, not manual `RefreshTheme()` calls throughout application code.
- SVG, Segoe MDL2 Assets, and FontAwesome are icon sources behind abstractions.
- FontAwesome.Sharp integration is optional and must not be a core dependency.
- Animation is UI-thread based and shared; no ad-hoc timers in each animated control.
- `BootstrapAccordion` is built on `BootstrapCollapse`.
- Button loading reuses spinner/loading infrastructure.
- WinForms Designer safety, keyboard behavior, accessibility metadata, DPI scaling, and GDI lifecycle are product requirements, not polish items.

## Compatibility warning

Prototype code in `idea-drafs/` contains constructs such as `Math.Clamp`, nullable syntax, newer property syntax, and placeholder namespaces such as `YourApp`. Do not copy it directly. The current namespace and compatibility rules are authoritative in `docs/`.

## Source-of-truth order

When documents disagree, use this precedence:

1. Explicit current user instruction
2. `docs/DECISIONS.md`
3. `docs/PRD.md`
4. `docs/ARCHITECTURE.md`
5. `docs/COMPONENTS.md`
6. `docs/DEVELOPMENT_PLAN.md`
7. Historical notes in `idea-drafs/`

Read `AGENTS.md` before implementation work.
