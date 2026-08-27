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

Expected concepts:

```text
Type: Border | Grow
SpinnerSize: Small | Default | Large
Variant
CustomColor
AnimationDuration
Spinning
```

Rules:

- Uses `BootstrapLoopAnimation`.
- Stops/releases animation appropriately when hidden or disposed.
- Is not focusable by default.
- Supports accessible description when used independently.

## BootstrapButton

Responsibility: themed command surface with standard button semantics.

Expected concepts:

```text
Variant
Outline
ButtonSize
Icon
IconPosition
BorderRadius
Loading
LoadingText
Selected
```

Rules:

- Entire client area is clickable.
- Enter/Space behavior follows normal button expectations.
- Loading suppresses click interaction and preserves visual size.
- Loading reuses Spinner infrastructure.
- Per-corner radii are supported internally/publicly as needed by ButtonGroup without making grouping depend on painting hacks.

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
