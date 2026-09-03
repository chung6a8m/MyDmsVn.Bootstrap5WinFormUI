# Development Plan

This plan is intentionally top-down. A later phase must not begin merely because its control is easier or more interesting to implement.

## Global gates

Every phase must satisfy all of the following before the next phase begins:

- Both `net48` and `net8.0-windows` build successfully.
- Relevant automated tests pass.
- New public behavior is documented.
- No duplicate theme/rendering/animation/icon infrastructure was introduced.
- Any UI change has a corresponding demo/manual verification path.

## Phase 0 — Repository and solution skeleton

Create the SDK-style solution/project structure for the core library, tests, and demo application.

Deliverables:

- Core library targeting `net48;net8.0-windows`
- Windows Forms enabled for both targets
- Test project with a Windows/STA-capable test strategy
- Demo application
- Initial build scripts or CI workflow when appropriate

Do not implement feature controls in this phase.

## Phase 1 — Compatibility, theme, and design tokens

Implement the cross-target compatibility helpers and the complete theme model before custom controls.

Deliverables:

- Theme mode
- Semantic Light/Dark palettes
- Metrics and typography tokens
- Theme manager and change event
- Reduced-motion preference
- Compatibility clamp/API helpers needed by foundation code
- A minimal Theme demo

## Phase 2 — Rendering and DPI foundation

Implement reusable painting and scaling helpers.

Deliverables:

- DPI scaling helpers
- Rounded path/per-corner radius helpers
- Color/contrast helpers
- Common content-layout helpers
- Double-buffer/lifecycle helpers where justified
- Tests for pure geometry/color/scaling logic

## Phase 3 — Icon infrastructure

Implement source-neutral icon contracts.

Deliverables:

- Icon descriptor/provider/renderer contracts
- Segoe MDL2 source
- External SVG source through a compatible renderer or adapter
- Internal vector-path support for framework glyphs
- Optional FontAwesome.Sharp integration design without making it a core dependency
- Icon demo using at least two source types

## Phase 4 — Shared animation infrastructure

Implement finite and looping animation primitives.

Deliverables:

- `BootstrapAnimation`
- `BootstrapLoopAnimation`
- Easing functions
- Start/stop/restart/dispose behavior
- Reduced-motion behavior
- Hidden/disposed lifecycle behavior
- Tests for progress, completion, restart, cancellation, and disposal

Do not implement control-specific timers after this phase unless a documented exception is approved.

## Phase 5 — BootstrapSpinner

Spinner is the first animated visual primitive and validates the loop-animation design.

Deliverables:

- Border and Grow modes
- Semantic variants/custom color
- Small/default/large sizing
- Start/stop
- Theme/DPI support
- Spinner demo

## Phase 6 — BootstrapButton and loading

Implement Button rendering and interaction, then integrate loading using Spinner.

Deliverables:

- Variants and outline variants
- Small/default/large sizing
- Icon placement
- Hover, pressed, focus, disabled
- Radius/per-corner radius support needed by ButtonGroup
- `Selected` state contract needed by groups
- `Loading` and `LoadingText`
- Size preservation while loading
- Button demo including async-loading simulation

## Phase 7 — ButtonGroup and ButtonToolbar

Build composite button controls only after Button is stable.

Deliverables:

- Horizontal/vertical ButtonGroup
- Connected borders/radii
- None/Single/Multiple selection
- Optional equal sizing
- Toolbar group spacing/orientation/alignment
- `SpaceBetween` desktop layout
- Group/Toolbar demos

Toolbar must not implement button selection logic.

## Phase 8 — TextBox and Card

Implement core form/surface primitives.

Deliverables:

- TextBox placeholder, validation, focus, icons, disabled/read-only/password states
- Card border, radius, padding, optional efficient shadow, Header/Body/Footer composition
- Theme/DPI/Designer support
- Demos for both controls

## Phase 9 — BootstrapCollapse

Implement the reusable collapse primitive before Accordion or Sidebar submenu behavior.

Deliverables:

- `Expanded`, `Expand()`, `Collapse()`, `Toggle()`
- Auto/measured/fixed expanded-height behavior
- Resize/content measurement
- Shared animation usage
- Rapid toggle behavior
- Reduced motion
- Collapse demo with variable and fixed-height content

## Phase 10 — Accordion and AccordionHeader

Compose Accordion from Collapse and a dedicated focusable header.

Deliverables:

- Single-open and multiple-open modes
- Flush style
- Header icon and vector chevron
- Full-header mouse interaction
- Tab focus and Enter/Space activation
- Animated chevron tied to Collapse progress
- Accordion demo including nested/dynamic content scenarios

## Phase 11 — BootstrapProgressBar

Implement determinate and indeterminate progress using shared animation.

Deliverables:

- Min/Max/Value/Percentage
- Variants/custom color
- Text format
- Striped/animated stripes
- Smooth `AnimateTo(...)`
- Indeterminate mode
- Progress demo

## Phase 12 — BootstrapSidebar

Build navigation composition on stable Button/Icon/Collapse/Animation infrastructure.

Deliverables:

- Expanded/collapsed widths
- Navigation item model and selected state
- Icons and optional badges
- Expand/collapse animation
- Nested sections using Collapse
- Keyboard/focus behavior
- Sidebar demo

## Phase 13 — BootstrapDataGridView

Theme the standard DataGridView without breaking normal behavior.

Deliverables:

- Header/row/alternate/selected styling
- Light/Dark theme
- Empty state
- Optional Spinner-based loading overlay
- Large-row-count manual performance check
- DataGrid demo with real columns/data/binding scenarios

## Phase 14 — Integrated demo application

Create a navigable showcase exercising the framework as an application would.

Required pages:

- Theme
- Buttons/Groups/Toolbar
- Inputs
- Cards
- Collapse/Accordion
- Loading/Spinner
- Progress
- Sidebar
- DataGrid

Every page should make Light/Dark switching available and expose disabled/focus/interaction states relevant to that page.

## Phase 15 — Hardening and API review

Before declaring the foundation feature-complete:

- Run the full DPI 100–200% matrix.
- Run theme switch stress checks.
- Exercise rapid animation toggles.
- Inspect timer/event/GDI lifetime.
- Review naming consistency.
- Remove prototype aliases.
- Complete XML documentation.
- Document known limitations.
- Verify optional icon dependencies remain optional.

## Phase 16 — Release preparation

Only after hardening:

- Define package metadata/versioning.
- Decide the stable public API baseline.
- Add package/release documentation.
- Confirm supported Visual Studio/.NET SDK build environment.
- Produce a release candidate and run the complete verification matrix.

## Post-foundation checkable controls

`BootstrapCheckBox`, `BootstrapRadioButton`, and `BootstrapSwitch` are an approved post-foundation addition. They directly inherit native WinForms controls, reuse Theme/Rendering/DPI infrastructure through one internal pure render helper, add only `Variant` and `ValidationState`, preserve native state/events/keyboard/radio grouping, and use native fallback for Appearance/image modes. The integrated demo and automated pure/STA matrices cover both target frameworks; real 100–200% DPI and Designer checks remain release evidence.

## Deferred roadmap

After the foundation is stable, candidates include Alert, Badge, Toast, Tooltip, Modal/Dialog, Dropdown, Tabs, Pagination, Skeleton, ComboBox, NumericBox, DatePicker, and other Bootstrap-inspired components.

Each new component must identify which existing primitives it composes before implementation begins.
