# Product Requirements Document — MyDmsVn.Bootstrap5WinFormUI

## 1. Product summary

`MyDmsVn.Bootstrap5WinFormUI` is a native Windows Forms UI framework inspired by Bootstrap 5. It provides a coherent theme system, design tokens, reusable rendering/animation/icon infrastructure, and a focused set of desktop controls suitable for business applications.

The goal is to make WinForms applications feel modern and visually consistent without introducing a browser, WebView, CSS runtime, or a large third-party UI suite.

## 2. Product principles

1. **Native WinForms first.** Controls must behave like normal WinForms controls and integrate naturally with forms, layout containers, data binding, focus, and the Visual Studio Designer.
2. **Bootstrap-inspired, not Bootstrap-ported.** Reuse the semantic palette, visual rhythm, variants, and component concepts where they fit desktop UI.
3. **Shared foundations before controls.** Theme, rendering, DPI, animation, icons, and compatibility infrastructure are implemented before dependent controls.
4. **Composition over duplication.** Composite controls reuse primitives. Accordion reuses Collapse; button loading reuses Spinner; loading overlays reuse shared loading primitives.
5. **Compatibility is a feature.** The same library design supports .NET Framework 4.8 and .NET 8 on Windows.
6. **Desktop quality matters.** Keyboard, focus, high DPI, accessibility metadata, Designer safety, GDI resource lifetime, and runtime theme switching are first-class requirements.

## 3. Target users

Primary users are C# developers building Windows desktop business applications that need a cleaner and more consistent UI than stock WinForms while retaining native controls and the existing WinForms ecosystem.

Typical applications include CRUD/admin tools, ERP/SME software, internal desktop utilities, data-entry applications, and long-lived .NET Framework applications being gradually modernized.

## 4. Platform and package requirements

- Root namespace: `MyDmsVn.Bootstrap5WinFormUI`
- Required runtime targets: .NET Framework 4.8 and .NET 8 on Windows
- Intended library TFMs: `net48;net8.0-windows`
- UI framework: Windows Forms
- Core drawing technology: `System.Drawing` / WinForms painting APIs
- No browser or WebView dependency
- No Bootstrap CSS or Bootstrap JavaScript runtime dependency
- FontAwesome.Sharp must remain optional

## 5. In-scope foundation capabilities

### 5.1 Theme system

The framework must provide a central theme model containing:

- Theme mode: Light and Dark
- Semantic colors: Primary, Secondary, Success, Danger, Warning, Info, Light, Dark
- Application colors: Body, Surface, SurfaceSecondary, Border, Text, MutedText, Disabled, Focus, Hover, Active
- Typography tokens
- Spacing tokens
- Border-radius tokens
- Control-size tokens
- Runtime theme change notifications
- Reduced-motion preference

Controls must consume semantic tokens rather than hard-coded colors or repeated magic numbers.

### 5.2 Rendering and DPI infrastructure

Provide reusable helpers for:

- Rounded rectangles and per-corner radii
- Color transforms and contrast decisions
- DPI scaling of sizes, padding, spacing, and stroke widths
- Double buffering
- Shared text/icon alignment calculations
- Safe resource ownership/disposal patterns

Controls must remain usable at 100%, 125%, 150%, 175%, and 200% scaling.

### 5.3 Animation infrastructure

Provide a shared one-shot animation primitive and a shared loop animation primitive.

Required behavior includes:

- Start, stop, cancel/restart
- Linear, ease-in, ease-out, and ease-in-out easing
- Progress notification
- Completion notification for one-shot animation
- Reduced-motion handling
- UI-thread-safe operation
- Stop when no longer renderable/visible where appropriate
- Deterministic disposal

Animated controls must not implement independent frame scheduling when shared infrastructure can provide it.

### 5.4 Icon infrastructure

Controls must consume an icon abstraction rather than knowing how an icon is sourced.

Required icon sources:

- External SVG through an SVG-capable provider/adapter
- Segoe MDL2 Assets glyphs
- Optional FontAwesome.Sharp integration
- Framework-owned vector paths for simple internal glyphs when appropriate

The core package must not require FontAwesome.Sharp.

## 6. In-scope controls

### 6.1 BootstrapButton

Must support semantic variants, outline style, small/default/large size, icon placement, hover, pressed, focus, disabled, selected state where needed by groups, configurable radius, and loading state.

Loading must disable interaction, preserve layout size, and reuse spinner/loading infrastructure.

### 6.2 BootstrapSpinner

Must support Border and Grow visual modes, semantic variants, custom color, small/default/large sizing, animation duration, and start/stop behavior using shared loop animation.

### 6.3 BootstrapButtonGroup

Must support horizontal/vertical layout, connected borders, first/middle/last corner handling, optional equal sizing, and None/Single/Multiple selection modes without duplicating button rendering.

### 6.4 BootstrapButtonToolbar

Must arrange multiple button groups with configurable spacing, orientation, alignment, and useful desktop layouts such as left/right `SpaceBetween`. It must not own selection logic.

### 6.5 BootstrapTextBox

Must provide modern border/focus treatment, placeholder behavior, disabled/read-only/password states, validation state, optional leading/trailing icons, and optional clear affordance while preserving normal text-box semantics.

### 6.6 BootstrapCard

Must provide a themed surface with radius, optional border/shadow, padding, and Header/Body/Footer composition without excessive nested controls or expensive painting.

### 6.7 BootstrapCollapse

Must be the reusable expand/collapse primitive. It supports `Expanded`, `Expand()`, `Collapse()`, `Toggle()`, animation duration, content measurement, explicit/automatic expanded height, resize handling, reduced motion, and rapid toggle safety.

### 6.8 BootstrapAccordion and BootstrapAccordionHeader

Accordion must compose Collapse rather than copy its animation logic.

It must support single-open and multiple-open modes, flush presentation, animated expansion, a fully clickable/focusable header, Enter/Space activation, hover/pressed/focus state, icon, and animated vector chevron.

### 6.9 BootstrapProgressBar

Must support minimum/maximum/value/percentage, semantic variants, custom color, text formatting, stripes, animated stripes, smooth `AnimateTo(...)`, and indeterminate mode. All animation must use shared animation primitives.

### 6.10 BootstrapSidebar

Must support expanded/collapsed widths, selected navigation item, icons, optional badges, Light/Dark theme, keyboard-friendly navigation, and reusable Collapse/Animation behavior for expansion and nested sections.

### 6.11 BootstrapDataGridView

Must remain compatible with normal `DataGridView` usage while adding theme-aware headers, rows, alternate rows, selection, borders, fonts, Dark mode, empty-state presentation, and an optional loading overlay that reuses Spinner.

## 7. Accessibility and interaction requirements

Where supported by WinForms, controls must provide meaningful:

- `AccessibleName`
- `AccessibleDescription`
- `AccessibleRole`
- Focus visibility
- Keyboard activation/navigation
- Disabled and logical-state reporting

Custom clickable surfaces must not be mouse-only.

## 8. Designer requirements

Controls intended for Visual Studio Designer usage must:

- Have a parameterless constructor
- Avoid requiring application startup state during construction
- Expose useful properties with appropriate `Category`, `Description`, and `DefaultValue` metadata when beneficial
- Avoid Designer crashes when theme services are not explicitly initialized
- Avoid destructive serialization surprises

## 9. Performance and resource requirements

The framework must:

- Avoid obvious GDI leaks
- Dispose owned pens, brushes, fonts, paths, regions, bitmaps, timers, and subscriptions
- Avoid generating a bitmap every animation frame unless unavoidable
- Avoid invalidating unrelated form regions
- Stop nonessential animation while hidden/disposed
- Avoid one timer per animated control when shared scheduling/infrastructure is practical
- Remain responsive during rapid expand/collapse or progress updates

## 10. Public API requirements

Public naming must be consistent across controls. Prefer canonical concepts such as:

- `Variant`
- `BorderRadius`
- `AnimationDuration`
- `CustomColor`
- `Loading`
- `Expanded`
- `Selected`

Do not introduce multiple aliases for the same concept. Before a stable release, prefer a clean API over compatibility with prototype names from `idea-drafs/`.

## 11. Non-goals

The initial product does not attempt to provide:

- Pixel-perfect Bootstrap CSS reproduction
- Bootstrap JavaScript behavior or DOM semantics
- CSS selectors or utility-class engine
- Browser layout
- Cross-platform UI
- A complete replacement for every WinForms control
- Every Bootstrap component in the first release

Components such as Alert, Badge, Toast, Modal/Dialog, Dropdown, Tabs, Pagination, Tooltip, Skeleton, DatePicker, ComboBox, and NumericBox are post-foundation candidates.

## 12. Quality and acceptance criteria

The foundation release is acceptable when:

- Both `net48` and `net8.0-windows` builds succeed.
- Light and Dark runtime theme switching works without recreating controls.
- Shared design tokens are used throughout in-scope controls.
- Shared animation primitives drive Collapse, Spinner, Progress, and loading behaviors.
- Core icon abstraction works with at least SVG plus one font/glyph source, and FontAwesome remains optional.
- In-scope controls have demo coverage and documented usage.
- Keyboard/focus behavior is implemented for interactive custom controls.
- Demo/manual validation covers DPI 100–200%.
- No obvious timer, event-subscription, or GDI resource leak remains in normal use.
- Public APIs have basic XML documentation.
- `idea-drafs/` prototype code is not treated as production implementation.

## 13. Success measure

A developer should be able to build a modern-looking WinForms CRUD screen using the framework's Theme, Button, TextBox, Card, Sidebar, DataGridView, loading, Collapse/Accordion, and Progress components without creating application-specific painting or animation infrastructure.
