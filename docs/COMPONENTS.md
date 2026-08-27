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

Responsibility: compose `BootstrapButton` instances into a connected horizontal or vertical control and own selection policy.

Phase 7 finalizes the public concepts as:

```text
BootstrapButtonSelectionMode: None | Single | Multiple

BootstrapButtonGroup.Orientation
BootstrapButtonGroup.SelectionMode
BootstrapButtonGroup.EqualWidth
BootstrapButtonGroup.BorderRadius
BootstrapButtonGroup.SelectedButtons
BootstrapButtonGroup.SelectionChanged
```

Behavior:

- The group lays out visible `BootstrapButton` children horizontally by default or vertically when `Orientation = Vertical`.
- It does not duplicate Button painting. Connected geometry is applied through the Button's internal per-corner radius override, leaving only the first/last outer corners rounded and middle seams square.
- Adjacent buttons overlap by the current DPI-scaled border width so a connected seam is painted once rather than separated by a gap.
- `BorderRadius = -1` preserves each outer button's configured/theme radius. A non-negative value applies one explicit logical outer radius for the whole group without mutating `BootstrapButton.BorderRadius`.
- `SelectionMode = None` never changes child `Selected` state. `Single` selects the activated button and clears the others. `Multiple` toggles only the activated button.
- Selection is activation-driven and therefore inherits Button suppression rules: disabled/loading buttons do not activate the group selection policy.
- `SelectedButtons` returns a snapshot of currently selected children; `SelectionChanged` is raised when the group policy changes selected state.
- `EqualWidth = true` uses the widest preferred button width for every visible child. Vertical groups always use the widest preferred width so connected left/right edges stay aligned.
- The control is auto-sized by default, non-focusable itself, and leaves Tab/focus/Enter/Space behavior on the child Buttons.
- Runtime theme or DPI changes recompute seam overlap and outer radii; removing a Button clears its internal grouping-radius override so it returns to standalone rendering.

Manual verification: launch the demo and choose **Groups / Toolbar**. Exercise horizontal Single selection, vertical Multiple selection, EqualWidth, explicit group radius, keyboard activation on child buttons, Light/Dark switching, and the supported Windows DPI matrix. Connected seams should remain continuous without duplicated rounded inner corners.

## BootstrapButtonToolbar

Responsibility: arrange multiple `BootstrapButtonGroup` controls without participating in Button selection.

Phase 7 finalizes the public concepts as:

```text
BootstrapToolbarAlignment: Left | Center | Right | SpaceBetween

BootstrapButtonToolbar.Orientation
BootstrapButtonToolbar.GroupSpacing
BootstrapButtonToolbar.Alignment
```

Behavior:

- The toolbar lays out visible ButtonGroups horizontally by default or vertically when requested.
- `GroupSpacing` is a non-negative logical-pixel value and scales through `DpiScaler`; the default is 8.
- `Left`, `Center`, and `Right` position the combined groups on the toolbar main axis. In vertical orientation, Left/Right mean leading/trailing on that axis.
- `SpaceBetween` anchors the first and last groups to opposite main-axis edges when sufficient space exists, distributes the remaining space between groups, and falls back to configured spacing when space is constrained.
- Auto-sized toolbars use natural group sizes plus configured spacing. A fixed-size toolbar can therefore use Center/Right/SpaceBetween meaningfully for desktop command bars.
- Toolbar never subscribes to child Button activation and never changes `Selected`; selection remains entirely a ButtonGroup responsibility.

Manual verification: in **Groups / Toolbar**, compare the fixed-width `SpaceBetween` command bar and vertical toolbar, resize the demo, switch Light/Dark, and verify toolbar actions never toggle selection when their group uses `SelectionMode.None`.

## BootstrapTextBox

Responsibility: modern themed text input while preserving native WinForms editing semantics.

Phase 8 finalizes the public concepts as:

```text
BootstrapValidationState: None | Valid | Invalid

BootstrapTextBox.PlaceholderText
BootstrapTextBox.ValidationState
BootstrapTextBox.Icon
BootstrapTextBox.TrailingIcon
BootstrapTextBox.IconRenderer
BootstrapTextBox.ShowClearButton
BootstrapTextBox.ReadOnly
BootstrapTextBox.UseSystemPasswordChar
BootstrapTextBox.BorderRadius
BootstrapTextBox.Clear()
BootstrapTextBox.SelectAll()
```

Behavior:

- The control composes a real borderless WinForms `TextBox`; selection, clipboard, IME, caret, read-only, and password behavior stay with the native editor instead of being reimplemented.
- `BootstrapTextBox` owns the single public tab stop and forwards focus to the native editor. Focus state is therefore painted around the whole themed surface while the inner editor remains out of the tab sequence.
- `PlaceholderText` is shown only while the native editor is empty. It is a presentation overlay, never stored as the actual `Text` value.
- `ValidationState = Valid` uses the current theme success color; `Invalid` uses danger; neutral focus uses the theme focus token; disabled state uses the disabled token.
- `Icon` and `TrailingIcon` use the same source-neutral `IconDescriptor` / `IIconRenderer` infrastructure as Button. Icon slots reserve editor width and scale through `DpiScaler`.
- `ShowClearButton` presents a clear affordance only while non-read-only, enabled text exists. Clearing uses the native editor's normal `TextChanged` path.
- Read-only and disabled states use the secondary surface and appropriate text tokens without changing the caller-owned text value.
- `BorderRadius = -1` uses the current theme radius; non-negative values specify an explicit uniform logical radius.
- Runtime theme changes update typography, surface, text, placeholder, border, icon, and validation colors. DPI changes recompute border, icon, padding, and layout geometry.
- Designer construction requires no application bootstrap. Theme subscriptions and theme-owned font resources are released during disposal.

Manual verification: launch the demo and choose **TextBox / Card**. Exercise placeholder, leading/trailing icons, clear, valid/invalid, read-only, password, and disabled examples. Tab into inputs, type/select/copy text, verify focus/validation border priority, switch Light/Dark, and repeat at each supported Windows DPI setting.

## BootstrapCard

Responsibility: reusable themed surface/container with lightweight composition regions.

Phase 8 finalizes the public concepts as:

```text
BootstrapCard.Header
BootstrapCard.Body
BootstrapCard.Footer
BootstrapCard.BorderRadius
BootstrapCard.ShowBorder
BootstrapCard.ShowShadow
BootstrapCard.Padding
```

Behavior:

- `Header`, `Body`, and `Footer` are stable `Panel` instances exposed with `DesignerSerializationVisibility.Content`. Header/Footer are hidden by default; Body fills the available content area.
- The Card paints one rounded themed surface and optional border itself. Child regions reuse the current surface color rather than introducing nested decorative controls.
- `Padding` defaults to the current theme `SpacingMD` token and scales through `DpiScaler`. Once an application explicitly sets Padding, later theme changes do not overwrite that caller-owned value.
- `BorderRadius = -1` uses the current theme radius; non-negative values specify an explicit uniform logical radius.
- `ShowShadow` paints lightweight rounded shadow geometry directly during Card painting; it does not allocate or retain shadow bitmaps.
- Runtime theme changes update surface, border, foreground, and all three region colors. DPI changes recompute theme-default padding and geometry.
- The Card is double-buffered, non-focusable itself, parameterless/designer-safe, and releases its theme subscription during disposal.

Manual verification: in **TextBox / Card**, compare the default bordered card, Header/Body/Footer composition, shadow card, and borderless custom-radius card. Resize the window, switch Light/Dark, inspect corners/shadow clipping, add controls to each region in the Designer, save/reopen, and repeat at the supported Windows DPI settings.

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

Responsibility: retain native WinForms `DataGridView` behavior while applying the framework visual language and lightweight presentation states.

Phase 13 finalizes the public concepts as:

```text
BootstrapDataGridView : DataGridView

BootstrapDataGridView.EmptyStateText
BootstrapDataGridView.Loading
BootstrapDataGridView.LoadingText
```

Behavior:

- `BootstrapDataGridView` derives directly from `DataGridView`; binding, columns, sorting, editing, selection, virtual mode, and other native APIs remain caller-owned and are not wrapped or replaced.
- `EmptyStateText` defaults to `"No data to display."`; `Loading` defaults to `false`; `LoadingText` defaults to `"Loading..."`. Text properties normalize `null` to an empty string.
- Header, normal row, alternating row, selected row/cell, grid-line, foreground, and background styles are mapped from the current theme. `EnableHeadersVisualStyles` is disabled so native visual styles cannot override framework header colors.
- Runtime Light/Dark changes update the existing grid presentation in place without rebinding data. The control owns theme-derived font instances until a caller explicitly assigns `Font`, after which caller font ownership is preserved.
- When there are no real data rows, the control paints `EmptyStateText` once in the grid client area. The native new-row placeholder does not count as data.
- Empty-state rendering is grid-level; the implementation does not install per-cell painting solely for framework decoration and avoids per-cell allocations in the hot path.
- `Loading = true` shows a lightweight overlay containing the existing `BootstrapSpinner` plus `LoadingText`. It does not replace `DataSource`, change columns, or mutate the caller-owned `Enabled` value.
- Loading animation, theme changes, reduced-motion behavior, and timer ownership stay with the composed Spinner; DataGridView does not create a second animation engine.
- Empty-state insets and loading-overlay spacing scale through `DpiScaler`. Parent-DPI changes recompute presentation layout.
- Theme subscriptions and theme-owned font resources are released during disposal; child controls dispose through normal WinForms ownership.

Manual verification: launch the demo and choose **DataGrid**. Exercise the sample binding, empty state, `10,000`-row scenario, and loading overlay; scroll, sort, select, resize/reorder columns, switch Light/Dark while the window is open, and repeat across the supported Windows DPI matrix. Large-row smoothness remains a manual visual performance gate.

## Deferred components

Alert, Badge, Toast, Tooltip, Dialog/Modal, Dropdown, Tabs, Pagination, Skeleton, ComboBox, NumericBox, DatePicker, and others are not part of the initial foundation contract.

Before adding one, document which existing foundation pieces it reuses.