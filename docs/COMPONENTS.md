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

## BootstrapNumericBox

Responsibility: provide Bootstrap-themed numeric input while preserving native WinForms numeric editing, formatting, range, spin, keyboard, and wheel semantics.

Stage 5 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapNumericBox : UserControl

BootstrapNumericBox.Value
BootstrapNumericBox.Minimum
BootstrapNumericBox.Maximum
BootstrapNumericBox.Increment
BootstrapNumericBox.DecimalPlaces
BootstrapNumericBox.ThousandsSeparator
BootstrapNumericBox.ReadOnly
BootstrapNumericBox.ValidationState
BootstrapNumericBox.BorderRadius
BootstrapNumericBox.ValueChanged
```

Behavior:

- `BootstrapNumericBox` composes exactly one real borderless native WinForms `NumericUpDown`. The native editor remains the single source of truth for value, range, increment, decimal formatting, thousands formatting, typed editing, spin buttons, Up/Down keys, and mouse-wheel behavior; the framework does not maintain a parallel numeric state model.
- `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, and `ReadOnly` forward directly to the native editor. Native range normalization and native `ArgumentOutOfRangeException` behavior are intentionally preserved rather than wrapped or replaced.
- The wrapper owns the single public tab stop and keeps the private native editor out of the tab sequence. Tab entry and shell clicks redirect focus to the native editor; Shift+Tab exit remains normal WinForms navigation.
- Native `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown` are forwarded through the wrapper exactly once so application handlers can observe the public control without introducing a second input path.
- `ValueChanged` is raised only from the native editor's effective `ValueChanged` path and reports the `BootstrapNumericBox` wrapper as sender. Assigning the same value is therefore a no-op according to native semantics.
- Border priority matches the established input validation model: disabled presentation wins first; otherwise `Valid` uses the success token, `Invalid` uses danger, neutral focus uses the focus token, and the unfocused neutral state uses the normal border token.
- `ReadOnly = true` prevents typed editing but intentionally retains native spin-button, Up/Down, and wheel behavior. `Enabled = false` remains the distinct non-interactive state and uses disabled presentation tokens.
- `BorderRadius = -1` uses the current theme radius; non-negative values specify an explicit logical uniform radius and values below `-1` are rejected before mutation.
- Runtime Light/Dark changes update the wrapper shell, native editor palette, and theme-owned `Body` font while preserving caller-owned numeric state. Parent-DPI changes recompute shell padding, border/focus widths, radius, and contained native-editor bounds through shared DPI/rendering helpers.
- Designer construction is parameterless and requires no application bootstrap. The private native editor is an implementation detail rather than a separately serialized/public composition surface. Disposal detaches native/theme handlers and releases only framework-created font resources.
- Stage 5 deliberately does **not** expose or implement `Hexadecimal`, `Accelerations`, custom numeric parsing/format-provider contracts, icons, prefix/suffix slots, or other adornments. Those capabilities remain deferred until a later explicit contract is planned; no aliases or hidden proxy APIs are added now.

Manual verification: choose **Advanced Inputs** in the integrated demo. Compare integer/default, decimal `0.25` increment with two decimal places, thousands separators, signed `-100..100` with step `10`, valid/invalid, read-only, disabled, and live `ValueChanged` examples. Type culture-sensitive values, use native spin buttons, Up/Down keys, mouse wheel, Tab/Shift+Tab, switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling.

## BootstrapComboBox

Responsibility: apply Bootstrap-themed presentation to the native WinForms `ComboBox` while keeping native data, selection, editing, autocomplete, dropdown, focus, keyboard, accessibility, and event semantics authoritative.

Stage 6 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapComboBox : ComboBox

BootstrapComboBox.ValidationState
BootstrapComboBox.BorderRadius
BootstrapComboBox.LeadingIcon
BootstrapComboBox.IconRenderer
```

Behavior:

- `BootstrapComboBox` derives directly from native WinForms `ComboBox`; it is not a wrapper around a second selector. Inherited `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, formatting, autocomplete, dropdown lifecycle, and selection events remain the single canonical model.
- The framework uses fixed-height `OwnerDrawFixed` presentation and native `GetItemText`/formatting semantics rather than raw `ToString()` assumptions. The optional control-level `LeadingIcon` is rendered only in the supported closed selected-item presentation; Stage 6 introduces no per-item icon/template/check/group model.
- `IconRenderer` uses the existing source-neutral `IIconRenderer` infrastructure and rejects `null`. It is runtime-only/non-serialized; no FontAwesome-specific dependency is introduced.
- Validation/focus border priority reuses the established TextBox input model: disabled presentation wins first; otherwise Valid uses success, Invalid uses danger, neutral focus uses the focus token, and unfocused neutral uses the border token.
- `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical radii and values below `-1` are rejected before mutation. Radius is best-effort for framework-controlled shell presentation only and does **not** promise rounded native arrow/edit/popup chrome.
- The framework owns theme/DPI palette, fixed item-height metrics, owner-drawn text/icon presentation, validation/focus shell border, and theme-created font lifecycle. Runtime Light/Dark switches update presentation in place without replacing or clearing native items, data source, selection, or autocomplete state.
- The native editable child, native arrow button, popup window/list chrome, hit-testing, dropdown placement, IME/text editing, Up/Down/Enter/Escape behavior, Tab traversal, and `DropDown`/`DropDownClosed`/selection event paths remain WinForms/OS-owned.
- `BootstrapDropdown` is not used internally. Stage 6 creates no custom `Form`, `ToolStripDropDown`, `ListBox`, popup host, child-window replacement, global hook, or private WinForms reflection path.
- The control subscribes once to the existing theme manager and owns only framework-created theme fonts. Handle recreation restores framework presentation settings without mirroring native data/selection state. Disposal removes the static theme subscription and never disposes caller-assigned fonts/renderers/descriptors.
- Designer construction is parameterless and requires no application bootstrap. `DropDown` and `DropDownList` remain normal native modes; `Simple` is best-effort/native and is not given a separate framework implementation.

Manual verification: choose **Advanced Inputs**. Compare unbound and bound (`DisplayMember`/`ValueMember`) lists, editable `DropDown`, selection-only `DropDownList`, native `SuggestAppend`/`ListItems` autocomplete, long values, leading-icon/no-icon examples, valid/invalid/disabled states, and explicit radius. Exercise mouse plus Up/Down/Enter/Escape/Tab/Shift+Tab, switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling. Confirm selected value/binding remain stable and accept that native edit/arrow/popup chrome may remain square/OS-themed.

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

## BootstrapPagination

Responsibility: provide Bootstrap-inspired page navigation while leaving data retrieval, slicing, binding, and virtualization entirely application-owned.

The finalized public concepts are:

```text
BootstrapPagination : Panel

BootstrapPagination.TotalItems
BootstrapPagination.PageSize
BootstrapPagination.CurrentPage
BootstrapPagination.TotalPages
BootstrapPagination.MaxVisiblePages
BootstrapPagination.ShowFirstLast
BootstrapPagination.ShowPreviousNext
BootstrapPagination.ButtonSize
BootstrapPagination.Variant
BootstrapPagination.BorderRadius
BootstrapPagination.PageChanged
```

Behavior:

- The control owns exactly one horizontal `BootstrapButtonGroup` with `SelectionMode = None` and dynamically composes existing `BootstrapButton` children. It does not duplicate Button or ButtonGroup painting.
- `TotalItems` defaults to `0`, `PageSize` to `20`, `CurrentPage` to `1`, and `TotalPages` is always at least `1`. The page count uses overflow-safe ceiling division.
- `TotalItems` rejects negative values; `PageSize` rejects values below `1`; `MaxVisiblePages` defaults to `5` and rejects values below `5`; direct `CurrentPage` assignments outside `1..TotalPages` throw.
- When reducing `TotalItems` or changing `PageSize` makes the current page invalid, `CurrentPage` clamps to the new last page and `PageChanged` is raised exactly once. Range changes that keep the current page valid do not raise `PageChanged`.
- Numeric-page layout is computed by an internal pure helper. Large ranges always include page `1`, the current page, and the last page, with disabled/non-focusable ellipses inserted only where pages are omitted.
- With `MaxVisiblePages = 5`, representative windows are `1 2 3 4 5`, `1 2 3 4 … 20`, `1 … 9 10 11 … 20`, and `1 … 17 18 19 20` for the documented boundary/middle scenarios.
- First/Previous and Next/Last are disabled at their respective boundaries. The active numeric page remains enabled, focusable, and uses `Selected = true`; activating the already-current page is a no-op.
- `ButtonSize` and `Variant` update existing child buttons in place. `BorderRadius = -1` preserves theme/button radii through the owned group; non-negative values are forwarded to `BootstrapButtonGroup.BorderRadius`.
- `ShowFirstLast` and `ShowPreviousNext` independently control the directional navigation sets. Paging-structure changes rebuild the button collection; removed dynamic controls are deterministically disposed.
- The Pagination container is non-focusable and exposes `AccessibleRole.Grouping` with default description `"Pagination navigation."`. Child buttons use semantic accessible names such as `First page`, `Previous page`, `Page N`, `Current page N`, `Next page`, and `Last page`.
- The control owns no timer, animation engine, theme subscription, data-source API, or DataGridView-specific coupling. Runtime theme/DPI behavior flows through the existing ButtonGroup/Button controls.
- Designer construction is parameterless and requires no application bootstrap. `TotalPages` is read-only and not designer-serialized.

Manual verification: launch the integrated demo and choose **Pagination**. Compare no-ellipsis, middle-window, boundary, zero-item, Small/Default/Large, and navigation-visibility scenarios. Exercise every navigation button with mouse and keyboard, switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% Windows scaling. In the DataGrid scenario, verify the application reacts to `PageChanged` and slices/binds ten rows per page while Pagination itself never owns the table or grid data source.

## BootstrapBadge

Responsibility: display a compact, auto-sized, non-interactive semantic text indicator without owning click/toggle behavior or notification-count business logic.

Stage 1 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapBadge : Control

BootstrapBadge.Variant
BootstrapBadge.CustomColor
BootstrapBadge.Pill
BootstrapBadge.BorderRadius
```

Behavior:

- `Text` remains the inherited WinForms content property; the default is empty, `AutoSize = true`, `TabStop = false`, and `AccessibleRole = StaticText`.
- `Variant` defaults to `Primary`. `CustomColor = Color.Empty` resolves through the existing `BootstrapVariantColorResolver`; a non-empty **fully opaque** custom color overrides the semantic variant. Transparent or semi-transparent custom colors are rejected so foreground contrast is computed against the same background that is actually painted.
- Foreground color is selected with the existing `ColorUtil.GetContrastingTextColor` helper instead of assuming fixed white/black text. Disabled presentation uses the current muted text token and a softened semantic/custom surface.
- Horizontal padding uses `SpacingSM` and vertical padding uses `SpacingXS`, both scaled through `DpiScaler`. Text uses the current theme `Label` typography token.
- `Pill = true` uses half the rendered physical height as its radius. Otherwise `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical radii and values below `-1` are rejected.
- `BootstrapBadgeRenderLogic` and its palette remain internal pure logic for semantic/custom colors, contrast, preferred size, DPI padding, and radius calculation.
- Painting is double-buffered and uses `RoundedPath` with scoped GDI resources. Badge introduces no timer, animation scheduler, icon model, geometry library, or external dependency.
- Runtime Light/Dark switches repaint semantic colors and retain a usable theme-owned font. Disposal detaches the theme subscription and disposes only framework-created fonts; a caller-assigned `Font` remains caller-owned.
- Designer construction is parameterless and requires no application bootstrap.

Manual verification: launch the integrated demo and choose **Feedback**. Compare all eight semantic variants, default and pill geometry, custom color, disabled, explicit square radius, and long-text AutoSize cases. Switch Light/Dark while the page is open, resize the host, and repeat at 100/125/150/175/200% Windows scaling to verify text clipping, padding, and rounded geometry.

## BootstrapAlert

Responsibility: display inline semantic feedback without owning toast timing, popup hosting, overlay placement, or queue behavior.

Stage 2 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapAlert : UserControl

BootstrapAlert.Variant
BootstrapAlert.Icon
BootstrapAlert.IconRenderer
BootstrapAlert.Dismissible
BootstrapAlert.BorderRadius
BootstrapAlert.Dismissed
BootstrapAlert.Dismiss()
```

Behavior:

- `Text` remains the inherited WinForms content property. Defaults are empty text, `Variant = Primary`, `Icon = null`, a non-null default `IconRenderer`, `Dismissible = false`, `BorderRadius = -1`, `TabStop = false`, and `AccessibleRole.Alert` with a default description.
- All eight `BootstrapVariant` values reuse `BootstrapVariantColorResolver`. Enabled alerts derive the surface, border, and foreground from the semantic color plus current theme `Surface`/`Border`/`Text` tokens, with a 4.5:1 foreground contrast fallback; disabled alerts use `SurfaceSecondary`, `Border`, `MutedText`, and `Disabled` focus tokens.
- Alert paints the rounded surface, border, optional content icon, and text itself. `Icon` stays source-neutral and uses `IIconRenderer`; no icon-source branching is introduced in the control.
- `Dismissible = true` exposes exactly one private native WinForms `Button` with accessible name `Dismiss alert` and description `Dismisses this alert.`. The close glyph is `IconDescriptor.Framework(FrameworkIconGlyph.Close)` rendered through the same configured `IconRenderer`; no nested `BootstrapButton` is created.
- The Alert container itself never enters the tab sequence. When dismissible, only the native close button is focusable and therefore retains normal WinForms Tab/Shift+Tab, Enter, Space, accessibility, and disabled behavior.
- `Dismiss()` hides a currently visible alert immediately and raises `Dismissed` once for that effective dismissal. Repeated calls while hidden are no-ops; re-showing with normal `Visible` permits a later dismissal event. Direct `Visible = false` changes never synthesize `Dismissed`, dismissal never disposes the control, and programmatic dismissal remains valid while the Alert is disabled.
- Logical 96-DPI metrics use 12px horizontal padding, 8px vertical padding/content spacing, a 16px icon slot, a 28px close slot, 1px border, 2px focus border, and the current theme radius. All metrics and explicit non-negative radii scale through `DpiScaler`; narrow/empty layouts clamp without negative rectangles.
- Text uses the current theme `Body` typography token and `TextRenderer` with word wrapping, end ellipsis, left alignment, vertical centering, and no mnemonic processing.
- Runtime Light/Dark changes recompute palette/layout and replace only the framework-owned theme font. Caller-assigned fonts and caller-supplied icon renderers/descriptors remain caller-owned. Disposal releases the Alert theme subscription and its framework-owned font; the child native Button is disposed through normal WinForms ownership.
- Alert introduces no timer, animation primitive, timeout/auto-hide state, overlay, z-order host, popup/window host, queue manager, Toast abstraction, or external package dependency.
- Designer construction is parameterless and requires no application bootstrap.

Manual verification: choose **Feedback** in the integrated demo. Compare all eight variants plus icon, dismissible, multiline, disabled, and explicit-radius examples. Dismiss with mouse and keyboard, restore the same instances repeatedly, switch Light/Dark while the page remains open, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling. Confirm text/icon/close alignment, focus visibility, rounded borders, and absence of stale rendering.

## BootstrapTooltip

Responsibility: provide Bootstrap-inspired tooltip presentation while preserving the native WinForms `ToolTip` association, popup-placement, delay, and lifecycle model.

Stage 3 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapTooltip : Component, IExtenderProvider

BootstrapTooltip.Variant
BootstrapTooltip.CustomColor
BootstrapTooltip.BorderRadius
BootstrapTooltip.ContentPadding
BootstrapTooltip.InitialDelay
BootstrapTooltip.ReshowDelay
BootstrapTooltip.AutoPopDelay
BootstrapTooltip.Active
BootstrapTooltip.ShowAlways
BootstrapTooltip.CanExtend(object)
BootstrapTooltip.SetToolTip(Control, string)
BootstrapTooltip.GetToolTip(Control)
```

Behavior:

- `BootstrapTooltip` owns exactly one native WinForms `ToolTip`; it does not subclass `ToolTip`, create a custom popup `Form`, introduce overlay/queue infrastructure, or implement a second scheduling model.
- The wrapper is an extender provider through `[ProvideProperty("ToolTip", typeof(Control))]`. `CanExtend` accepts WinForms `Control` instances, while `SetToolTip`/`GetToolTip` delegate association storage to the native ToolTip as the single source of truth. Empty captions remove the native association and explicit newline characters are preserved.
- The parameterless constructor is designer-safe. `BootstrapTooltip(IContainer)` adds only the wrapper to the supplied container; the inner native ToolTip remains privately owned and is disposed exactly once by the wrapper.
- The native ToolTip is configured internally with `OwnerDraw = true` and `IsBalloon = false`. Those implementation details, the native ToolTip itself, Popup/Draw events, `Show`/`Hide`, animation/fading/title/icon APIs, and other unplanned native surface are not re-exposed.
- `Variant` defaults to `Dark`; undefined enum values are rejected. `CustomColor = Color.Empty` uses the semantic variant while any non-empty custom color overrides the background. Foreground is selected through `ColorUtil.GetContrastingTextColor`, and the border uses the current theme `Border` token.
- `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical radii. `ContentPadding` defaults to logical `SpacingSM` horizontal / `SpacingXS` vertical (8/4/8/4 with default metrics). Negative padding edges and radius values below `-1` are rejected before state mutation.
- `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` forward directly to the native ToolTip rather than maintaining mirror state.
- Popup measurement and Draw rendering resolve `BootstrapThemeManager.CurrentTheme` at event time instead of subscribing to `ThemeChanged`. Text uses the current `Typography.BodySmall`; temporary fonts and all GDI resources are scoped to the event.
- `BootstrapTooltipRenderLogic`, `BootstrapTooltipPalette`, and `BootstrapTooltipRenderMetrics` remain internal pure helpers. They reuse `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `RoundedPath`, and `CornerRadius`; popup sizing and text-bounds calculations clamp malformed/tiny geometry without negative sizes.
- Content padding, border width, and radius scale from the associated control's `DeviceDpi`, falling back to 96 DPI when necessary. The framework does not impose automatic word wrapping: owner-drawn measurement/rendering uses `TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding`, while explicit newlines remain supported.
- Disposal detaches the owned native Popup/Draw handlers, disposes the native ToolTip idempotently, and adds no static theme subscription or other process-lifetime root.

Manual verification: stay on **Feedback** and hover the default Dark, same-instance second target, semantic Info, custom-color, multiline, and long-caption examples. Change Initial/Reshow/Auto-pop delays and Active/Show always live. Switch Light/Dark while the page stays open and repeat at 100/125/150/175/200% real Windows scaling. Confirm native popup positioning/timing remains intact, explicit newlines render, long captions are not framework-wrapped, padding/border/radius scale cleanly, and the same Tooltip can serve multiple controls.

## BootstrapTabControl

Responsibility: apply Bootstrap-inspired tab-header presentation while retaining native WinForms `TabControl` page composition, selection, focus, keyboard, image, tooltip, and overflow behavior.

Stage 4 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapTabStyle: Tabs | Pills | Underline

BootstrapTabControl : TabControl

BootstrapTabControl.TabStyle
BootstrapTabControl.Variant
BootstrapTabControl.Fill
BootstrapTabControl.BorderRadius
```

Behavior:

- `BootstrapTabControl` derives directly from native `TabControl`. `TabPage`, `TabPages`, `SelectedIndex`, `SelectedTab`, `SelectedIndexChanged`, arrow/Ctrl+Tab behavior, mouse hit-testing/selection, accessibility, `ImageList`, `ImageKey`/`ImageIndex`, `ToolTipText`, and native overflow remain WinForms-owned rather than being mirrored or wrapped.
- The control forces `DrawMode = OwnerDrawFixed` and `SizeMode = Fixed`, then paints only rectangles supplied by the native `DrawItem` event / `GetTabRect`. It never paints or recolors the selected page content area and introduces no custom page host/window.
- `TabStyle` defaults to `Tabs`; `Variant` defaults to `Primary`; `Fill` defaults to `false`; `BorderRadius = -1` uses the current theme radius. Undefined style/variant values and radius values below `-1` are rejected before mutation.
- `Tabs` uses rounded top corners and a semantic selected border/text accent; `Pills` uses a rounded semantic selected surface with contrast-selected foreground; `Underline` stays visually minimal and paints only a DPI-scaled selected accent at the header bottom.
- Enabled inactive headers remain theme-neutral and use the current Hover token while hovered. Disabled headers use `Surface`, `Border`, and `Disabled` tokens independent of semantic variant. Selected focus uses the shared theme focus token without replacing native focus/selection behavior.
- Header metrics reuse `BootstrapThemeMetrics` and `DpiScaler`: the 96-DPI baseline is 32px height, 12px horizontal padding, 8px image/text spacing, 54px minimum width, 1px border, 2px focus/underline thickness, and the theme 6px radius.
- With `Fill = false`, all fixed headers use one uniform width based on the widest measured native tab content plus horizontal padding and the minimum width. With `Fill = true`, all headers share available client width evenly but never shrink below the minimum; native TabControl overflow/navigation remains responsible when all minimum headers do not fit.
- Header text uses `TextRenderer` with single-line end ellipsis. Native images are read from the assigned `ImageList` through each page's `ImageKey`/`ImageIndex`; the control does not introduce a second icon model or copy image state.
- Runtime theme changes update theme-owned `Body` typography, metrics, palette, header size, and painting in place. Caller-assigned fonts remain caller-owned. Parent-DPI changes recompute `ItemSize`; page text/enabled changes and add/remove update sizing/presentation without replacing pages or selection.
- Hover tracking uses native `GetTabRect` bounds only. No surrogate button controls, synthetic selection state, animation/timer, P/Invoke, custom window, page wrapper, or external package dependency is introduced.
- Disposal releases the direct theme subscription, page presentation handlers, control event handlers, and framework-created font while leaving caller-owned fonts/images/pages to normal WinForms ownership semantics.
- Designer construction is parameterless and requires no application bootstrap.

Manual verification: choose **Navigation / Tabs** in the integrated demo. Exercise Tabs/Pills/Underline with mouse, Tab/arrow/Ctrl+Tab keyboard paths, Fill on/off, all eight variants, native ImageList/ImageKey/ImageIndex, tooltip text, disabled pages, long labels, and live `SelectedIndexChanged` status. Switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling. Confirm headers stay aligned/unclipped, focus remains visible, selection/page identity remains native, and native overflow controls remain usable when headers exceed available width.

## BootstrapDropdown

Responsibility: provide a Bootstrap-inspired command dropdown while delegating popup behavior to native WinForms `ToolStripDropDownMenu` semantics.

Stage 7 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapDropdownItemKind: Item | Separator

BootstrapDropdownItem
BootstrapDropdownItem.Kind
BootstrapDropdownItem.Text
BootstrapDropdownItem.Icon
BootstrapDropdownItem.Enabled
BootstrapDropdownItem.Checked
BootstrapDropdownItem.Tag
BootstrapDropdownItem.Click

BootstrapDropdownItemCollection : Collection<BootstrapDropdownItem>

BootstrapDropdown : Component
BootstrapDropdown.Target
BootstrapDropdown.Items
BootstrapDropdown.Variant
BootstrapDropdown.MinimumWidth
BootstrapDropdown.Opened
BootstrapDropdown.Closed
BootstrapDropdown.Show()
BootstrapDropdown.Close()
```

Behavior:

- `BootstrapDropdown` is a non-visual component that owns exactly one native `ToolStripDropDownMenu` and one internal `BootstrapDropdownRenderer`. The caller owns the `BootstrapButton` assigned to `Target` and every public `BootstrapDropdownItem` model.
- `BootstrapDropdownItemKind` has exactly `Item` and `Separator`. `Kind` is immutable after construction and invalid enum values are rejected. Normal item construction defaults to empty text, `Enabled = true`, `Checked = false`, `Icon = null`, and `Tag = null`; `Text` normalizes `null` to an empty string.
- `Items` is one stable `BootstrapDropdownItemCollection`. It preserves order and rejects null insertions/replacements; the collection has no live change-notification or popup-binding engine.
- Each successful `Show()` rebuilds a short-lived native item snapshot from the current public model. Changes made while the menu is closed are therefore reflected on the next opening; the framework does not keep a second synchronized command model while the popup is open.
- `Target` defaults to `null`; `Variant` defaults to `Primary`; `MinimumWidth` defaults to `0`. A negative `MinimumWidth` or undefined `Variant` is rejected. `Show()` without a target is an explicit error, while empty items, a disabled target, a loading target, or a disposed target produce no popup transition.
- Target activation toggles the native popup only while the target is enabled and not loading. Replacing or disposing the target closes any open popup and detaches the old handlers. Dropdown never disposes a caller-owned target.
- Enabled normal-item activation raises that model item's `Click` exactly once. Disabled items and separators do not activate. `Checked` is presentation state only and is copied into the native snapshot with `CheckOnClick = false`; the framework never auto-toggles the model. Application code may update `Checked` in `Click`, and the next `Show()` reflects the new state.
- Native `ToolStripDropDownMenu` remains authoritative for AutoClose, outside-click dismissal, focus/message-loop behavior, Up/Down/Home/End/Enter/Escape navigation, and working-area/screen placement. `Opened` and `Closed` forward real native transitions from the owned popup rather than synthetic component state changes.
- `Variant` controls semantic accent/check/selection presentation through the internal renderer. `MinimumWidth` is a logical-pixel minimum scaled for the target DPI before each opening; native measurement may still make the menu wider for content.
- Optional item icons remain source-neutral `IconDescriptor` values. Dropdown renders snapshot bitmaps through the current `Target.IconRenderer`; generated bitmaps are framework-owned and disposed on rebuild, theme refresh, and component disposal. A runtime theme change refreshes renderer/icon presentation for an already-open popup without replacing the public item model.
- Dropdown deliberately does not expose `BorderRadius`, custom popup chrome, arbitrary hosted controls, submenus, split-button behavior, nested command trees, live synchronization while open, custom placement hooks, a popup `Form`, a second focus/keyboard engine, timer, or animation API. ComboBox and later DatePicker controls retain their own native semantic popup architectures rather than automatically reusing Dropdown.
- Designer construction is parameterless and requires no application bootstrap. Disposal detaches target/theme/native handlers, disposes generated images and the owned native popup, and never disposes caller-owned item models, icon descriptors/renderers, or target controls.

Manual verification: choose **Navigation / Tabs** in the integrated demo and exercise the Dropdown basic/icon/state/long/stress scenarios. Verify target mouse and Enter/Space activation, Up/Down/Home/End plus item Enter, Escape/outside-click dismissal, checked/disabled/separator policy, runtime item mutation between openings, target replacement/disposal, and repeated Light/Dark switches. Repeat near bottom/right working-area edges, on a secondary monitor when available, and at 100/125/150/175/200% real Windows scaling. Repeated open/close/theme-switch cycles must not leave stale images, duplicate events, or disposed-GDI exceptions.

## Deferred components

Toast, Dialog/Modal, Skeleton, DatePicker, and others are not part of the initial foundation contract.

Before adding one, document which existing foundation pieces it reuses.
