# Component Contracts

This document defines responsibilities and public API direction. Exact signatures are finalized during implementation/API review, but implementations should not invent competing names or architectures.

## Shared conventions

Use these names consistently where applicable:

```text
Variant
BorderRadius
AnimationDuration
CustomColor
Loading
Selected
Expanded
Icon
IconPosition
```

Do not create `AnimationTime`, `TransitionDuration`, and `Duration` aliases for the same concept.

## BootstrapSpinner

Responsibility: display ongoing work without owning business/task state.

Phase 5 finalizes the public concepts as:

```text
BootstrapSpinnerType: Border | Grow
BootstrapSpinnerSize: Small | Default | Large
BootstrapVariant: Primary | Secondary | Success | Danger | Warning | Info | Light | Dark

BootstrapSpinner.Type
BootstrapSpinner.SpinnerSize
BootstrapSpinner.Variant
BootstrapSpinner.CustomColor
BootstrapSpinner.AnimationDuration
BootstrapSpinner.Spinning
BootstrapSpinner.Start()
BootstrapSpinner.Stop()
```

Behavior:

- `Border` renders a rotating arc; `Grow` renders a pulsing filled circle.
- `CustomColor = Color.Empty` uses the current theme color selected by `Variant`; any non-empty custom color overrides the semantic variant.
- Small/default/large logical diameters reuse existing theme metrics (`SpacingLG`, `SpacingXL`, and `ControlHeight`) and are scaled through `DpiScaler`.
- The default animation cycle is 750 ms. `AnimationDuration` must be greater than zero.
- `Spinning` defaults to `true`; `Start()` and `Stop()` are the canonical run-control methods.
- Animation is driven exclusively by `BootstrapLoopAnimation`; Spinner does not own a WinForms timer or a second scheduling engine.
- Hiding the control pauses shared loop scheduling and showing it resumes from retained logical progress through the animation owner lifecycle. Disposal releases the animation and theme subscription.
- Runtime theme changes update semantic colors and sizing. They also recreate the loop animation so a changed reduced-motion preference takes effect immediately.
- With reduced motion enabled, an active spinner remains on a stable visible frame without continuously scheduling animation frames.
- The control is double-buffered, non-focusable by default, transparent-background capable, and exposes an accessibility role/description suitable for an activity indicator.
- Designer construction requires no application bootstrap; animation starts only after a runtime handle exists.

Manual verification: launch the demo and choose **Spinner**. Compare Border/Grow, all three sizes, all semantic variants, and the custom-color examples; use **Start all** / **Stop all**, switch Light/Dark, toggle Reduced motion, resize the window, and validate the page under the supported Windows DPI matrix.

## BootstrapButton

Responsibility: themed command surface with standard native WinForms button semantics.

Phase 6 finalizes the public concepts as:

```text
BootstrapButtonSize: Small | Default | Large
BootstrapIconPosition: Left | Right
BootstrapVariant: Primary | Secondary | Success | Danger | Warning | Info | Light | Dark

BootstrapButton.Variant
BootstrapButton.Outline
BootstrapButton.ButtonSize
BootstrapButton.Icon
BootstrapButton.IconPosition
BootstrapButton.IconRenderer
BootstrapButton.BorderRadius
BootstrapButton.Loading
BootstrapButton.LoadingText
BootstrapButton.Selected
```

Behavior:

- `BootstrapButton` derives from the native WinForms `Button`, preserving normal command, focus, Enter/Space, and `PerformClick()` semantics when not loading.
- The entire normal-state client area is clickable; hover, pressed, focus, disabled, and selected states are custom-painted from theme tokens.
- Filled and outline presentations support all semantic `BootstrapVariant` values. Semantic color resolution is shared with Spinner instead of duplicated in each control.
- Small/default/large heights and padding/radius choices reuse `BootstrapThemeMetrics` and scale through `DpiScaler`.
- `Icon` is a source-neutral `IconDescriptor`. `IconRenderer` defaults to the built-in Segoe MDL2/framework-vector renderer and can be replaced with a renderer that includes SVG or application-defined providers. The Button never branches on icon source kind.
- `BorderRadius = -1` uses the current theme radius for `ButtonSize`; non-negative values specify an explicit uniform logical radius.
- ButtonGroup support is prepared through an internal per-corner `CornerRadius` override. Grouping can therefore apply first/middle/last corner geometry without replacing Button painting or mutating the public uniform radius.
- `Selected` is a visual state only. A standalone Button does not toggle it automatically; ButtonGroup owns selection policy in Phase 7.
- `Loading` suppresses click activation without mutating the caller-owned `Enabled` value. It replaces normal content with a framework `BootstrapSpinner` plus `LoadingText` (or the normal `Text` when `LoadingText` is empty).
- Loading animation is owned by the composed `BootstrapSpinner`; Button does not create a timer or animation engine.
- Preferred size reserves both normal and loading presentations, so toggling `Loading` does not change the preferred/AutoSize footprint.
- Runtime theme changes update colors, metrics, typography, focus rendering, and the loading spinner through the existing theme lifecycle. Disposal releases the Button theme subscription and owned theme font; the child Spinner releases its own animation/theme resources.
- Designer construction requires no application bootstrap. The loading spinner is non-focusable and does not animate unless loading is active at runtime.

Manual verification: launch the demo and choose **Button**. Compare filled/outline variants, Small/Default/Large sizing, left/right icons, selected/disabled/custom-radius states, mouse hover/press, Tab focus, Enter/Space activation, and the async loading simulation. Confirm repeat activation is suppressed while loading, size remains unchanged, Light/Dark switches repaint live, Reduced motion produces a stable spinner frame, and the page remains usable across the supported Windows DPI matrix.

## BootstrapButtonGroup

Responsibility: group Buttons and own connected-border plus selection behavior.

Expected concepts:

```text
Orientation
SelectionMode: None | Single | Multiple
EqualWidth
BorderRadius
SelectedIndex/selection query API as appropriate
```

Rules:

- Contains `BootstrapButton` instances.
- Does not duplicate button rendering.
- Applies first/middle/last corner rules.

## BootstrapButtonToolbar

Responsibility: lay out multiple ButtonGroups.

Expected concepts:

```text
Orientation
GroupSpacing
Alignment: Left | Center | Right | SpaceBetween
AutoSize behavior
```

Rule: selection belongs to ButtonGroup, never Toolbar.

## BootstrapTextBox

Responsibility: modern themed text input while preserving standard WinForms text editing behavior.

Expected concepts:

```text
PlaceholderText
ValidationState
Icon / leading icon
TrailingIcon
ShowClearButton
ReadOnly
UseSystemPasswordChar or equivalent
BorderRadius
```

Implementation should prefer composition around a real `TextBox` when custom border/focus rendering is needed, rather than reimplementing text editing.

## BootstrapCard

Responsibility: reusable surface/container.

Expected concepts:

```text
Header
Body
Footer
BorderRadius
ShowBorder
ShowShadow
Padding
```

Header/Body/Footer may be exposed as child containers or a simple composition model. Preserve Designer usability.

## BootstrapCollapse

Responsibility: reusable animated vertical expand/collapse container.

Expected concepts:

```text
Expanded
Expand()
Collapse()
Toggle()
AnimationDuration
ExpandedHeightMode: Auto/Measured/Fixed (or equivalent coherent model)
ExpandedHeight
AnimationProgress (read-only when useful to composed controls)
IsAnimating
```

Rules:

- Owns collapse state and measurement.
- Uses shared finite animation.
- Handles reversal during an active animation.
- Exposes enough progress/state for AccordionHeader to animate chevron without reimplementing timing.

## BootstrapAccordionHeader

Responsibility: full-width interactive/focusable accordion header.

Expected concepts:

```text
Text
Icon
Expanded
AnimationProgress
Chevron visibility/style
```

Rules:

- Mouse click works on the whole header.
- Tab focus is supported.
- Enter and Space activate.
- Focus is visibly rendered.
- Chevron is vector-based and its rotation follows Collapse progress.

## BootstrapAccordion

Responsibility: manage a collection of accordion items and enforce single/multiple-open behavior.

Expected concepts:

```text
Items
AllowMultipleOpen
Flush
AnimationDuration
Add/Remove/Clear item API
ExpandAll/CollapseAll where compatible with mode
```

Rules:

- Items reuse `BootstrapCollapse` and `BootstrapAccordionHeader` behavior.
- Accordion does not contain its own timer or separate height-animation implementation.

## BootstrapProgressBar

Responsibility: render determinate or indeterminate progress.

Expected concepts:

```text
Minimum
Maximum
Value
Percentage
Variant
CustomColor
BorderRadius
ShowText
TextFormat
Striped
Animated
AnimationDuration
Indeterminate
AnimateTo(...)
```

Rules:

- `AnimateTo` uses finite animation.
- Animated stripes and indeterminate mode use loop animation.
- Invalid ranges are normalized/rejected consistently.

## BootstrapSidebar

Responsibility: themed application navigation container.

Expected concepts:

```text
ExpandedWidth
CollapsedWidth
Expanded/Collapsed state
SelectedItem
Items
Toggle/Expand/Collapse
```

Navigation items support text, icon, optional badge, hover, selected, disabled, focus, and optional nested content.

Rules:

- Reuse Collapse for nested section content where applicable.
- Reuse Animation for width/state transitions.
- Do not create a separate icon model.

## BootstrapDataGridView

Responsibility: retain DataGridView capabilities while applying framework visual language.

Expected capabilities:

- Header style
- Body/alternate-row style
- Selected row style
- Theme-aware grid/borders
- Empty state
- Optional loading overlay

Rules:

- Do not hide or replace standard data-binding APIs.
- Avoid per-cell allocations in hot paint paths.
- Loading overlay reuses Spinner.

## Deferred components

Alert, Badge, Toast, Tooltip, Dialog/Modal, Dropdown, Tabs, Pagination, Skeleton, ComboBox, NumericBox, DatePicker, and others are not part of the initial foundation contract.

Before adding one, document which existing foundation pieces it reuses.