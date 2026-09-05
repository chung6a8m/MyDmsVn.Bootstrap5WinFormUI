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

## BootstrapFormattedTextBox

Responsibility: format native text input as it is edited while exposing a stable canonical string separately from its presentation.

`BootstrapFormattedTextBox` derives from `BootstrapTextBox`. `Text` is always the final formatted display value; `RawValue` is the canonical unformatted value. Date, Time, Numeral, and CreditCard canonical numeric content accepts only ASCII digits `0-9`; Unicode decimal digits are ignored rather than transliterated. `RawValueChanged` follows `TextChanged` when one edit changes both values, and `CreditCardTypeChanged` reports effective card-family changes without implying Luhn or business validation.

The formatting engine is pure C# and independent of WinForms. `FormatMode` selects identity, General, Numeral, Date, Time, CreditCard, or Custom behavior. General supports blocks, delimiters, prefixes, character casing, and numeric-only filtering. Numeral keeps invariant `.` in `RawValue` while allowing configurable display grouping, decimal mark, scale, sign, and prefix rules. Date and Time perform structural partial-input formatting; CreditCard performs digit grouping and IIN-based type detection. Date, Time, and CreditCard delimiters plus the Numeral delimiter and decimal mark must not contain ASCII digits; invalid assignments throw `ArgumentException` before mutation or change notification. Mutable mode options otherwise reformat the current value immediately. Applications can implement `IInputFormatter` and assign `Formatter` in Custom mode.

Edits are normalized as one stable raw/display pair. Middle insertion, selection replacement, raw or already-formatted paste, and cut restore selection through raw-position mapping. In General mode, insertion inside the displayed prefix maps to raw position `0`, deleting prefix decoration restores it without changing the logical value, and a selection crossing from the prefix into raw content replaces only the mapped raw range. Backspace/Delete beside a formatting separator removes the adjacent raw character and recomputes presentation. Ctrl+Z/Ctrl+Y use bounded formatted-input history; Ctrl+A/C/X/V, Tab/Shift+Tab, Alt keys, focus, clipboard, and IME remain on the native editor path. Programmatic `Text`/`RawValue` assignment and `Reformat()` clear that edit history.

The inherited TextBox shell remains authoritative for placeholder, icons, clear button, read-only/password state, validation border, theme, DPI, focus, and disposal. Formatting only canonicalizes text; `ValidationState` is still caller-owned and does not assert semantic validity. Use `BootstrapNumericBox` when the application needs a typed `decimal`, range, increment, spin buttons, or mouse-wheel behavior. Formatted Numeral mode is string-based and preserves partial input.

The v1 contract deliberately excludes Phone mode, typed `DecimalValue`/`DateTime` properties, a regex-mask DSL, business validation, and a second native editor abstraction.

Manual verification: choose **Advanced Inputs** and exercise every formatted mode, live RawValue/card-type feedback, middle edits, formatted/raw paste, separator deletion, undo/redo, clear, Tab/Shift+Tab, Alt shortcuts, Vietnamese IME composition, Light/Dark switching, and real Windows 100/125/150/175/200% scaling.

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

## BootstrapSelect

Responsibility: provide a dedicated Select2-style managed selector for searchable single/multiple selection, chips, grouping, custom values, and asynchronous paged lookup without changing the native-backed `BootstrapComboBox` contract.

The finalized public concepts are:

```text
BootstrapSelect : UserControl
BootstrapSelectMode: Single | Multiple
BootstrapSelectChangeReason
BootstrapSelectItem / BootstrapSelectItemCollection
BootstrapSelectQuery / BootstrapSelectPage
IBootstrapSelectDataProvider
IBootstrapSelectMatcher / BootstrapSelectTextMatcher
IBootstrapSelectRenderer / BootstrapSelectRenderer
BootstrapSelectRenderState and render contexts

BootstrapSelect.SelectionMode
BootstrapSelect.Items
BootstrapSelect.DataProvider
BootstrapSelect.SelectedItem / SelectedItems
BootstrapSelect.SelectedValue / SelectedValues
BootstrapSelect.ValueComparer
BootstrapSelect.Placeholder
BootstrapSelect.AllowClear
BootstrapSelect.AllowCustomValues
BootstrapSelect.CustomValueFactory
BootstrapSelect.CloseOnSelect
BootstrapSelect.SearchEnabled
BootstrapSelect.MinimumSearchLength
BootstrapSelect.SearchDebounce
BootstrapSelect.PageSize
BootstrapSelect.DropDownWidth
BootstrapSelect.MaxDropDownHeight
BootstrapSelect.ResultRowHeight
BootstrapSelect.MaximumSelectionRows
BootstrapSelect.Matcher
BootstrapSelect.Renderer
BootstrapSelect.ValidationState
BootstrapSelect.BorderRadius
BootstrapSelect.Select(...) / SelectValue(...)
BootstrapSelect.Deselect(...) / DeselectValue(...)
BootstrapSelect.ClearSelection()
BootstrapSelect.Selecting / Selected
BootstrapSelect.Deselecting / Deselected
BootstrapSelect.SelectionChanged
BootstrapSelect.DropDownOpened / DropDownClosed
BootstrapSelect.SearchStarted / SearchCompleted / SearchFailed
```

Behavior:

- `BootstrapSelect` is a separate `UserControl`; it does not subclass, wrap, or replace `BootstrapComboBox`. Use ComboBox when native WinForms binding/editing/autocomplete/popup semantics are required; use Select when the application needs managed search, multiple chips, groups, custom values, or remote paging.
- `BootstrapSelectItem.Value` is non-null immutable logical identity. `Text` is presentation and may change; `Disabled`, `Group`, `Icon`, and `Tag` are optional metadata. Selection reconciliation uses `ValueComparer`, never object-reference identity.
- Local and asynchronous result modes are mutually exclusive. With `DataProvider = null`, `Items` plus `Matcher` drive results. While `DataProvider` is set, local items are retained but ignored and are never merged into provider pages.
- The built-in matcher performs case-insensitive text matching. A caller-owned `IBootstrapSelectMatcher` may replace local matching/ranking; remote matching/ranking belongs entirely to the provider.
- Single mode exposes one selected snapshot; Multiple mode exposes unique selected snapshots as chips and supports multi-row wrapping bounded by `MaximumSelectionRows`. Selected state is independent of the currently loaded/filter-visible result page.
- `AllowCustomValues` is opt-in. When non-empty search text has no exact text match, a Create row may invoke the synchronous caller-owned `CustomValueFactory`; returning `null` rejects creation and a returned item follows the normal cancellable selection pipeline.
- `IBootstrapSelectDataProvider` is transport-agnostic. The core library owns no `HttpClient`, URL, headers, database, cache, retry library, or service client. Providers receive one-based `BootstrapSelectQuery` page requests and return `BootstrapSelectPage` with `Items` and `HasMore`.
- Async query mechanics are control-owned: debounce, cancellation, monotonically increasing generation/race protection, initial loading, serialized load-more, result deduplication, loading/error rows, first-page retry, later-page retry, and stale-response rejection. Provider cancellation is cooperative; correctness does not rely on the provider honoring it.
- A first-page failure replaces the result body with an actionable error/retry row. A later-page failure retains already loaded rows and appends an actionable retry row for the same failed page. `OperationCanceledException` is normal control flow and does not raise `SearchFailed`.
- Newly loaded items with an already-selected logical value may refresh the selected snapshot metadata without raising a false selection change. A provider changing an item's disabled state does not silently remove an existing selection.
- Group headers are non-selectable and preserve provider/local encounter order. Filtering removes empty local groups; page append logic avoids duplicate adjacent group headers across page boundaries.
- `IBootstrapSelectRenderer` is presentation-only. It receives item/state/theme/DPI/bounds contexts for result rows, group headers, single selection, and chips. Layout, hit testing, scrolling, selection, keyboard behavior, provider calls, and popup lifetime remain control-owned.
- `ResultRowHeight` is a positive `int` expressed in logical pixels at 96 DPI. Its default is `32`, and it defines one uniform height for selectable, loading, empty, retry, create, and other popup result rows. Variable-height measurement and hosted/HTML-like row templates are outside the contract.
- Custom result metadata belongs in caller-owned `BootstrapSelectItem.Tag`. Local and provider-created items use the same `IBootstrapSelectRenderer.DrawResult` path; `DrawSelection` independently controls the closed single-selection presentation.
- Changing `ResultRowHeight` while open refreshes and repositions the existing popup. DPI transitions refresh presentation before placement, with one effective DPI used throughout that refresh. Wheel steps are row-based, while the exact final clamp may be non-row-aligned.
- The popup reuses the existing internal overlay host/surface/anchor tracking and `BootstrapOverlayPlacementEngine`. Bottom-start is preferred; flip/shift keeps the popup inside the current monitor working area. No second Popper-like engine, custom top-level `Form`, or global mouse/keyboard hook is introduced.
- Closed keyboard paths include Alt+Down/F4/Enter/Space open and printable-input search. Open popup uses Up/Down, Home/End, PageUp/PageDown, Enter, Escape, and normal Tab traversal. Search editing remains a real WinForms text editor so caret, clipboard, selection, IME, and Vietnamese input remain native responsibilities.
- The outer control exposes ComboBox-style accessibility semantics, focusability, expanded/collapsed state, selected text in Single mode, and a stable selected-count summary in Multiple mode. Retry, custom-value creation, clear, and chip removal have keyboard equivalents.
- Validation/focus/theme rendering reuses existing theme and rendering tokens. `BorderRadius = -1` uses the theme radius; logical dimensions scale through `DpiScaler`. Major horizontal affordances mirror for `RightToLeft.Yes`.
- `DataProvider`, custom matcher/renderer, items, item `Tag` values, and caller-assigned resources remain caller-owned. The control disposes only its debounce/cancellation/search state, popup/overlay content, owned subscriptions, and framework-created rendering resources.
- Hiding, disabling, disposal, or handle recreation closes an active popup and cancels obsolete query work without clearing logical selection. Reopen recreates current result/search state from the retained control configuration.
- Designer construction is parameterless and requires no application bootstrap. Async work and popup hosting begin only through runtime interaction.

Automated validation covers both `net48` and `net8.0-windows`, local matching/grouping/custom values, value identity and snapshot refresh, async cancellation/races/paging/retry, keyboard/focus/accessibility, popup lifecycle/placement, theme, DPI geometry/rendering, RTL, disposal, and integrated-demo contracts.

Manual verification: choose **Select** in the integrated demo. Exercise local single selection, multiple chips and wrapping, groups, custom values, disabled rows, async paging over the 300+ row deterministic provider, `fail-first` page-1 retry, `retry` later-page retry, and rapid `race` typing. Verify mouse plus keyboard-only operation, Vietnamese IME composition, Light/Dark switching, monitor-edge and cross-monitor popup placement, repeated open/close/handle recreation, and real Windows 100/125/150/175/200% scaling. These environment-dependent checks remain manual release evidence and are not implied by automated CI.

See `docs/BOOTSTRAP_SELECT.md` for complete usage examples and extension guidance.

## BootstrapCheckBox, BootstrapRadioButton, and BootstrapSwitch

Responsibility: provide Bootstrap 5.3-inspired normal form-check presentation while preserving native WinForms checked state, events, input, accessibility state, and RadioButton grouping.

Public concepts are deliberately limited to:

```text
BootstrapCheckBox : CheckBox
BootstrapRadioButton : RadioButton
BootstrapSwitch : CheckBox

Variant
ValidationState
```

Behavior:

- All three controls directly inherit their native WinForms counterpart. `Checked`, `CheckState`, `ThreeState`, `AutoCheck`, state-change events, Space/mnemonic behavior, alignment, padding, text, accessibility metadata, and AutoSize remain inherited and authoritative.
- Each class adds only `Variant` and `ValidationState`. `Variant` defaults to Primary; enabled Valid/Invalid overrides it with Success/Danger. Validation colors the unchecked border/track and label as well as checked/indeterminate active presentation. Disabled theme tokens have highest precedence; focus remains a separate Focus-token ring.
- Painting follows the actual native `CheckState`. A programmatically assigned Indeterminate value renders as mixed even when `ThreeState = false`; `ThreeState` controls only the states native user input cycles through. Switch uses a centered thumb/cue for Indeterminate.
- Default RadioButton same-parent exclusivity and reparenting are native. `AutoCheck = false` is a caller-managed mode in which multiple same-parent radios may remain checked; framework code never traverses siblings or restores exclusivity.
- CheckBox and RadioButton use a 16-logical-pixel indicator, Switch a 32-by-16 track, and all use theme spacing/border/focus/radius metrics through `DpiScaler`. CheckBox is small-rounded, RadioButton circular, and Switch pill-shaped; V1 exposes no size or radius property.
- `CheckAlign` and RTL resolve the indicator slot with the characterized native rule exactly once. Switch then mirrors logical thumb start/end inside that resolved track, avoiding double mirroring. `TextAlign`, `UseMnemonic`, keyboard cues, AutoEllipsis, and Padding remain part of text layout.
- `Appearance.Button` or an effective `Image`/`ImageList` presentation uses native base painting and native preferred-size calculation. Returning to normal text-only appearance restores framework presentation without changing state, Variant, ValidationState, or caller-owned image settings.
- Runtime Light/Dark changes update framework-owned normal presentation and only framework-created Body fonts. Caller fonts remain caller-owned. Each control owns no child, timer, animation, checked-state mirror, radio registry, global hook, or bitmap cache; disposal removes its theme subscription and font resource.
- V1 intentionally excludes animation, custom colors, public sizes/radii, custom grouping, and InputGroup integration.

Manual verification: choose **Checks / Radios / Switches**. Exercise unchecked/checked/indeterminate states (including programmatic mixed state with `ThreeState=false`), all variants, Valid/Invalid while unchecked, disabled and `AutoCheck=false`, native/default and caller-managed RadioButton groups, Tab/Shift+Tab/Space/arrows/mnemonics, RTL left/right alignments, Appearance/image fallback and return, live Light/Dark changes, repeated state/fallback changes, and real Windows 100/125/150/175/200% scaling.

## BootstrapInputGroup

Responsibility: compose supported addon, input, button, and split-button primitives into one horizontal connected row without replacing their native interaction or public state.

Public concepts are `BootstrapInputGroupSize` (`Small`, `Default`, `Large`), `BootstrapInputGroup.InputGroupSize`, and the addon properties `BootstrapInputGroupText.Text`, `Icon`, `IconRenderer`, `TextAlign`, and `BorderRadius`. Composition and reorder use the inherited `Controls` collection and `Controls.SetChildIndex(...)`; there is no second public collection.

Supported direct children are `BootstrapInputGroupText`, `BootstrapTextBox` (including `BootstrapFormattedTextBox`), `BootstrapNumericBox`, Single-mode `BootstrapSelect`, `BootstrapButton`, and `BootstrapSplitButton`. Multiple-mode Select, ComboBox, DatePicker, ButtonGroup, native/arbitrary controls, file input, checkbox, and radio addons are deferred and rejected before reparenting.

The group applies internal explicit connected corner/size overrides and never mutates child `BorderRadius`, `ButtonSize`, validation, value, text, selection, enabled, read-only, or Tab state. Visible children are measured first for a safe common row height; the container synchronizes its client height and assigns final bounds only after the maximum native/theme floor is known. Fixed children keep preferred width, stretch inputs share surplus, and constrained layouts use deterministic fixed then emergency compression without negative/out-of-client rectangles.

The container and addon are non-focusable. Interactive children keep normal Tab/Shift+Tab, editing, Enter/Space, popup, validation, and accessibility behavior. Visible children determine outer corners; hide/remove/reparent clears overrides. Caller reorder is canonical, including native `-1`/`SendToBack()` normalization. Temporary focused/pressed/hovered z-order keeps active seam borders visible without rewriting canonical layout order. RTL mirrors visual placement/corners without changing Controls or Tab order. See `docs/BOOTSTRAP_INPUT_GROUP.md`.

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

## BootstrapLookupBox and BootstrapLookupColumn

Responsibility: provide a local, single-selection lookup that separates committed value, pending editor text, and transient result highlight, plus a native DataGridView column that stores raw lookup identity.

`BootstrapLookupBox` accepts `BindingSource`, `BindingList<T>`, `IList`, `IListSource`, and arrays. `Columns` defines text-backed popup columns and `SearchMembers` defines member priority. The default Vietnamese-insensitive multi-token search ranks by worst token quality, total quality, DisplayMember match count, member priority, then source order without filtering or moving caller currency.

Exact text commits only one distinct logical value. Ambiguous labels with distinct values validate instead of silently choosing; duplicate rows with the same value choose the first source row. Empty text clears, while unmatched input follows RestorePreviousSelection, KeepFocusWithValidationError, or CommitAndAdd. Lookup validation is transient and does not erase application validation.

The popup composes shared overlay infrastructure and `BootstrapDataGridView`. Result navigation never takes editor focus or commits. Tab resolves before native traversal; Escape equals `CancelPendingEdit()`; application deactivation closes presentation only. Refresh and Add New are explicit application workflows.

`BootstrapLookupColumn` binds `DataPropertyName` to the row's raw value, maps `ValueMember` to `DisplayMember`, and exposes contextual SelectionCommitted/Refresh/AddNew/CreateItemFromText events. Its internal reusable editor detaches old datasource/events/debounce/popup state before applying cloned configuration for another cell. Native DataGridView navigation, validation, BindingSource currency, and new-row creation remain authoritative. See `docs/BOOTSTRAP_LOOKUP_BOX.md`.

V1 excludes remote paging, multiple selection, source filtering, and public cell/editor implementation types.

Manual verification: exercise Vietnamese search, ambiguity, keyboard/mouse commit, Tab blocking, Escape, Alt+Tab, Refresh/Add New, Light/Dark and 100–200% DPI. In the DataGrid demo verify raw ProductId binding, hidden/read-only traversal, native new row, and repeated cross-column editor reuse.

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

Responsibility: provide Bootstrap-inspired text-only tooltip presentation while preserving native association, delay, drawing, and lifecycle behavior. Native placement remains the default; managed placement is opt-in.

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
BootstrapTooltip.Positioning
BootstrapTooltip.Placement
BootstrapTooltip.CollisionBehavior
BootstrapTooltip.Offset
BootstrapTooltip.BoundaryPadding
BootstrapTooltip.CanExtend(object)
BootstrapTooltip.SetToolTip(Control, string)
BootstrapTooltip.GetToolTip(Control)
```

Behavior:

- `BootstrapTooltip` owns exactly one native WinForms `ToolTip`; it does not subclass `ToolTip`, create a custom popup `Form`, expose public Show/Hide, accept focusable content, or implement a second scheduler.
- `Positioning = Native` is the backward-compatible default. `Managed` retains native association, timing, popup notification, owner drawing, and lifetime. Popup measures and records the shared placement-engine result; Draw obtains the current native Tooltip HWND from its graphics DC and queues the exact rectangle for immediately after the current paint. This avoids paint reentrancy and does not cancel or reissue the native popup.
- Managed positioning supports Auto and explicit Top/Bottom/Left/Right alignment families, None/Flip/Shift/FlipAndShift, logical Offset and BoundaryPadding, RTL Start/End, negative desktop coordinates, and deterministic overflow handling. `None` may overflow the working area, while `Flip` changes only to the exact opposite side without an implicit cross-axis shift.
- The wrapper is an extender provider through `[ProvideProperty("ToolTip", typeof(Control))]`. `CanExtend` accepts WinForms `Control` instances, while `SetToolTip`/`GetToolTip` delegate association storage to the native ToolTip as the single source of truth. Empty captions remove the native association and explicit newline characters are preserved.
- The parameterless constructor is designer-safe. `BootstrapTooltip(IContainer)` adds only the wrapper to the supplied container; the inner native ToolTip remains privately owned and is disposed exactly once by the wrapper.
- The native ToolTip is configured internally with `OwnerDraw = true` and `IsBalloon = false`. Those implementation details, the native ToolTip itself, Popup/Draw events, `Show`/`Hide`, animation/fading/title/icon APIs, and other unplanned native surface are not re-exposed.
- `Variant` defaults to `Dark`; undefined enum values are rejected. `CustomColor = Color.Empty` uses the semantic variant while any non-empty custom color overrides the background. Foreground is selected through `ColorUtil.GetContrastingTextColor`, and the border uses the current theme `Border` token.
- `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical radii. `ContentPadding` defaults to logical `SpacingSM` horizontal / `SpacingXS` vertical (8/4/8/4 with default metrics). Negative padding edges and radius values below `-1` are rejected before state mutation.
- `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` forward directly to the native ToolTip rather than maintaining mirror state.
- Popup measurement and Draw rendering resolve `BootstrapThemeManager.CurrentTheme` at event time instead of subscribing to `ThemeChanged`. Text uses the current `Typography.BodySmall`; temporary fonts and all GDI resources are scoped to the event.
- `BootstrapTooltipRenderLogic`, `BootstrapTooltipPalette`, and `BootstrapTooltipRenderMetrics` remain internal pure helpers. They reuse `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `RoundedPath`, and `CornerRadius`; popup sizing and text-bounds calculations clamp malformed/tiny geometry without negative sizes.
- Content padding, border width, and radius scale from the associated control's `DeviceDpi`, falling back to 96 DPI when necessary. The framework does not impose automatic word wrapping: owner-drawn measurement/rendering uses `TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding`, while explicit newlines remain supported.
- Mouse leave/down, target hiding or disposal, switching to Native positioning, and component disposal invalidate the pending managed request before native Hide/teardown. A queued bounds correction validates its request generation and never starts a second popup. Disposal detaches the owned native Popup/Draw handlers, disposes the native ToolTip idempotently, and adds no static theme subscription or other process-lifetime root.

Manual verification: stay on **Feedback** and hover the default Dark, same-instance second target, semantic Info, custom-color, multiline, and long-caption examples. Change Initial/Reshow/Auto-pop delays and Active/Show always live. Switch Light/Dark while the page stays open and repeat at 100/125/150/175/200% real Windows scaling. Confirm native popup positioning/timing remains intact, explicit newlines render, long captions are not framework-wrapped, padding/border/radius scale cleanly, and the same Tooltip can serve multiple controls.

## BootstrapPopover

Responsibility: host arbitrary focusable caller content in a non-modal Bootstrap-inspired native popup without changing Tooltip semantics.

Public concepts:

```text
BootstrapPopover.Target / Content
BootstrapPopover.Trigger
BootstrapPopover.Placement / CollisionBehavior
BootstrapPopover.Offset / BoundaryPadding
BootstrapPopover.ContentPadding / BorderRadius
BootstrapPopover.CloseOnEscape / CloseOnClickOutside
BootstrapPopover.IsOpen
BootstrapPopover.Opened / Closed
BootstrapPopover.Show() / Hide() / Toggle()
```

Behavior:

- Target and Content are caller-owned and never disposed by Popover. Content must be live and unparented when assigned; it is parented to the private surface while assigned, detached when replaced/disposed, and cannot be replaced while open.
- Click trigger toggles from Target activation; Manual has no target Click subscription. External Target/Content disposal first detaches and clears the exact disposed object, then closes, so a replacement assigned reentrantly from `Closed` remains attached and subscribed.
- Placement/collision semantics are identical to managed Tooltip through the pure internal engine. Logical spacing is scaled at target DPI before screen-pixel computation against `Screen.FromRectangle(anchor).WorkingArea`; `None` may overflow and `Flip` does not add cross-axis shifting.
- The host is one internal `ToolStripDropDown` plus `ToolStripControlHost` and themed rounded surface. It applies the engine's exact position and size directly to the current native HWND after WinForms show/layout and during movement. No custom Form, global hook, timer, animation engine, or public native host is introduced.
- Native AutoClose owns outside-click dismissal. The accepted v1 host remains `ToolStripDropDown`: its narrow keyboard policy cancels only the observed Alt/menu-mode keyboard close and does not broadly cancel keyboard or application-click close reasons.
- Escape restores focus to a live Target; outside-click does not steal focus back. Pressing Alt alone leaves the Popover open. Focus selection checks the Content root first, then follows native WinForms tab order through nested containers while skipping invisible, disabled, non-tab-stop, or non-selectable controls.
- The Popover is non-modal and does not trap focus. Tab from the last eligible descendant and Shift+Tab from the first eligible descendant close without Escape-style target restoration, then continue after or before Target in the surrounding form tab order; Target is only the fallback when no eligible peer exists.
- An open-only tracker follows Target/ancestor/form movement, scrolling, parent/visibility/disposal, theme, and current DPI signals. Close/disposal removes every transient/static subscription.

Manual verification: use the Feedback edge sandbox and interactive content. Verify keyboard/content interaction, Escape/outside-click focus, placement/collision changes, form movement, ancestor scrolling, Light/Dark, 100–200% DPI, mixed-DPI and negative-coordinate monitors, and repeated open/close without losing caller content state.

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

## BootstrapDropdown and BootstrapSplitButton

Responsibility: provide native WinForms command menus with recursive submenus and snapshot-hosted controls, plus a connected two-region split command that reuses the same Dropdown infrastructure.

```text
BootstrapDropdownItemKind: Item | Separator | HostedControl
BootstrapDropdownItem.DropDownItems
BootstrapDropdownItem.HostedControlFactory

BootstrapSplitButton : Control
BootstrapSplitButton.Text / Variant / Outline / ButtonSize
BootstrapSplitButton.Icon / IconPosition / IconRenderer / BorderRadius
BootstrapSplitButton.Loading / LoadingText
BootstrapSplitButton.Items / MinimumWidth
BootstrapSplitButton.Opened / Closed
BootstrapSplitButton.ShowDropDown() / CloseDropDown()
```

Dropdown behavior:

- Every effective opening validates the entire model by reference identity and builds a recursive native snapshot. An item instance may appear only once, which rejects cycles and shared-node graphs. Normal items may have children but no hosted factory; separators have neither children nor a factory; hosted items require a factory and cannot have children.
- A normal item with children is a submenu parent and never dispatches its own `Click`. Only enabled normal leaves activate. Native `ToolStripDropDownMenu` / `ToolStripMenuItem` retain Right/Left/Up/Down/Home/End/Enter/Escape, focus, AutoClose, outside-click dismissal, and working-area placement behavior.
- Each submenu level receives the framework renderer, current theme/font, and independently computed image/check margins. Icons and checked state are snapshot presentation; theme changes refresh every open level without mutating the caller-owned model.
- `HostedControlFactory` runs for each effective snapshot and must return a fresh, non-null, non-disposed `Control`. Once returned, that control is owned by the framework's native snapshot and is disposed when the snapshot is rebuilt or when the owning `BootstrapDropdown` is disposed. Closing alone does not dispose the current snapshot; reopening rebuilds it. Callers retain ownership only of the factory and model item.
- Snapshot creation is transactional: invalid trees or invalid factory results fail opening and dispose partial native rows/controls/images. Public items remain the source of truth; there is no live collection-binding engine.
- Classic `Target` behavior and `Show()` remain supported. The internal anchored path used by composite controls is not public and does not assign `Target`. `MinimumWidth`, theme, font, and icon renderer continue to use the active presentation button while a popup is visible.

Split-button behavior:

- The primary region raises the outer inherited `Click` and never opens the menu. The chevron region toggles the owned Dropdown; `ShowDropDown()` / `CloseDropDown()` use the same real native lifecycle, and outer `Opened` / `Closed` events use the split control as sender.
- The popup request anchors below the full split bounds, while primary appearance supplies font/icon rendering. While open the chevron is selected. Empty, disabled, loading, disposed, or already-open states are safe no-ops; disabling the split or setting `Loading = true` closes first and prevents further opening.
- `Items` is the owned Dropdown's stable model collection. `Variant` and `MinimumWidth` stay synchronized, including a live minimum-width update while open. The two focusable child regions use shared connected-button seam/corner logic and preserve native Enter/Space activation and Tab/Shift+Tab traversal.
- `Font` is inherited rather than shadowed. Until a caller assigns a font, both regions follow the current theme font; a caller-assigned font remains authoritative across later theme changes and also supplies popup presentation. Region accessibility names resolve dynamically from the outer `AccessibleName`, falling back to `Text`; the chevron appends “menu”.
- No strongly typed child accessors are exported. Inherited WinForms `Controls` can still enumerate the two implementation regions, but they are framework-owned and callers must not mutate, remove, reparent, or dispose them. Parent disposal closes/disposes the owned Dropdown explicitly and lets base `Control` dispose child regions exactly once.

Example:

```csharp
var split = new BootstrapSplitButton { Text = "Save", AccessibleName = "Save document" };
var saveAs = new BootstrapDropdownItem { Text = "Save as" };
var local = new BootstrapDropdownItem { Text = "Local file" };
local.Click += (_, _) => SaveLocal();
saveAs.DropDownItems.Add(local);
split.Items.Add(saveAs);
split.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
{
    HostedControlFactory = () => new CheckBox { Text = "Create backup", AutoSize = true }
});
split.Click += (_, _) => Save();
```

Manual verification: use the **Navigation / Tabs** nested/hosted/mixed and split scenarios. Cover mouse routing, region focus and keyboard paths, submenu navigation, hosted-control focus/edit/dismiss/reopen, disabled/checked/loading states, Light/Dark while nested menus are visible, caller-font persistence, dynamic accessibility names, 100–200% DPI seams/arrows/margins, monitor edges, and repeated rebuild/disposal while open.

## BootstrapToast and BootstrapToastContainer

Responsibility: provide transient Bootstrap-inspired application feedback while keeping notification placement explicit in the application's WinForms control tree and making ownership/lifetime deterministic.

Stage 8 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapToastPlacement: TopLeft | TopRight | BottomLeft | BottomRight

BootstrapToast : UserControl
BootstrapToast.Title
BootstrapToast.Text
BootstrapToast.Variant
BootstrapToast.Icon
BootstrapToast.IconRenderer
BootstrapToast.Dismissible
BootstrapToast.AutoHide
BootstrapToast.AutoHideDelay
BootstrapToast.AnimationDuration
BootstrapToast.Dismissed
BootstrapToast.Dismiss()

BootstrapToastContainer : Panel
BootstrapToastContainer.Placement
BootstrapToastContainer.ToastSpacing
BootstrapToastContainer.MaximumVisibleToasts
BootstrapToastContainer.ShowToast(BootstrapToast)
BootstrapToastContainer.DismissAll()
```

Behavior:

- `BootstrapToast` is a caller-configured feedback surface. `Title` and inherited `Text` default to empty strings; `Variant = Primary`; `Icon = null`; `IconRenderer` is non-null; `Dismissible = true`; `AutoHide = true`; `AutoHideDelay = 5000`; `AnimationDuration = 200`; `TabStop = false`; `AccessibleRole = Alert`. `AutoHideDelay` and `AnimationDuration` must be greater than zero and undefined variants are rejected before mutation.
- Toast and Alert share the existing feedback palette/layout rules rather than maintaining competing semantic-color formulas. Toast adds title/body composition and transition/lifetime state, but does not introduce a second feedback color system.
- `Dismissible = true` exposes one private native close button. The Toast itself stays outside the tab sequence; the native close affordance remains keyboard/accessibility capable. Programmatic `Dismiss()` is the canonical dismissal request even when the Toast is not user-dismissible.
- `BootstrapToastContainer` is placed by the application like any other WinForms `Panel`; Stage 8 does not create a global/static notification service, overlay manager, hidden application singleton, or framework-owned top-level `Form`.
- `Placement` supports all four corners. `ToastSpacing` is a non-negative logical value and `MaximumVisibleToasts` is at least one. Layout uses a pure stack calculation with DPI-scaled spacing/padding and no arbitrary absolute-screen positioning.
- `ShowToast(toast)` is the ownership boundary. Before a successful call, the caller owns the Toast and may configure or dispose it. After a successful call, ownership transfers to the container, including queued Toasts. The caller must not dispose, reparent, remove from `Controls`, or manually toggle `Visible`; the container deterministically disposes an owned Toast after dismissal completes or when the container itself is disposed.
- The container owns one FIFO queue and never displays more than `MaximumVisibleToasts`. Queued Toasts remain hidden and do not start enter animation or auto-hide time. When a visible Toast finishes dismissal, the next queued Toast is promoted in insertion order unless a `DismissAll()` snapshot is being drained.
- `Dismissed` is raised exactly once when logical dismissal is accepted. For container-owned Toasts, that event happens before exit animation and disposal complete. Repeated dismissal requests, stale auto-hide ticks, and direct hidden-state transitions do not produce duplicate events.
- Enter and exit use the shared finite `BootstrapAnimation` infrastructure. Each Toast has at most one active transition; the container uses at most one survivor-reflow animation. Dismissal during enter reverses from the current visual position without jumping. Completed exit removes/disposes the Toast and promotes queued work; stale animation callbacks are ignored through generation/cancellation guards.
- `AnimationDuration` is a logical millisecond duration for Toast enter/exit/reflow transitions. Runtime reduced-motion mode completes those transitions synchronously instead of scheduling frame work; it does not change `AutoHideDelay` or cause auto-hide to fire immediately.
- Auto-hide countdown starts only after a Toast's enter transition has completed and only while the owning host is active/visible. Changing `AutoHide` or `AutoHideDelay` while a currently visible Toast is eligible restarts/cancels the countdown according to the new value. Queued Toasts never consume lifetime. Stale ticks from a replaced/cancelled timer generation are ignored.
- Hiding the container pauses transition scheduling through the shared animation owner lifecycle and suspends Toast lifetime work. Showing the host resumes from retained state rather than charging hidden wall-clock time. Disposal cancels timers/animations, detaches ownership callbacks, and disposes all active/queued Toasts exactly once.
- Runtime Light/Dark switches update Toast presentation in place without resetting queue position, enter/exit progress, or an active auto-hide lifetime. Caller-assigned fonts/renderers/descriptors remain caller-owned; framework-created theme fonts and private children follow normal owned-resource cleanup.

Package-facing usage:

```csharp
var toast = new BootstrapToast
{
    Title = "Saved",
    Text = "Changes were saved successfully.",
    Variant = BootstrapVariant.Success,
    AutoHide = true,
    AutoHideDelay = 5000
};

toastContainer.ShowToast(toast); // ownership transfers here
```

After the final line, use the Toast/container API to request dismissal rather than manually destroying or reparenting the instance.

Manual verification: choose **Feedback** in the integrated demo. Exercise manual and auto-hide Toasts, title/body/icon/multiline/disabled states, Burst 8 with `MaximumVisibleToasts = 3`, `DismissAll()`, all four placements, rapid show-then-dismiss, and Stress 100. Repeat with Light/Dark and Reduced motion, resize while Toasts are active, hide/show the host, switch theme during auto-hide, and repeat at 100/125/150/175/200% real Windows scaling. For resource soak, repeat stress/dismiss cycles and inspect process USER/GDI handles for unbounded growth.

### BootstrapToastService and notification history

Responsibility: compose the existing Toast/container primitive into application-level, per-screen transient notifications plus bounded semantic history and a reusable notification center.

The only additional public concepts are `BootstrapToastOptions`, immutable `BootstrapToastHistoryItem`, and `BootstrapToastService`. Host windows, notification-center controls, history storage, monitor/DPI resolution, layout/render logic, dispatcher behavior, topology retirement, and test seams remain internal.

- The service and `Default` are UI-thread-affine. Framework display/application callbacks marshal internally to the creating UI thread; callers must not call the public API from worker threads.
- `Show(..., relativeTo)` snapshots caller options, resolves the target monitor, applies that monitor's explicit DPI and working area, then transfers the created Toast to one canonical per-screen host. The host is non-activating and uses `Region`, not `TransparencyKey`.
- `Placement`, `ToastSpacing`, `MaximumVisibleToasts`, `ScreenMargin`, and `TopMost` update existing and future hosts. Height constraints preserve strict FIFO; a removed-screen host retires/dismisses rather than moving over a live screen.
- `IncludeInHistory = false` affects history only. Otherwise, history stores semantic values independently of transient controls, is newest-first when read, bounded by `HistoryCapacity`, and exists only in memory. There is no persistence or OS notification integration.
- `Show` is atomic with respect to history: the immutable record is committed only after the host accepts the transient Toast, so a failed transfer cannot trim or mutate existing entries. Successful mutations refresh the internal center before raising `HistoryChanged`; an exception from an application handler occurs after the mutation is committed.
- Activating an unread center row marks it read. Mark-all and clear are effective-mutation operations. User `Close()`, the close button, Escape, and Alt+F4 hide the center for reuse. Application exit and application/system close reasons are allowed to close it normally; service disposal closes and disposes it permanently.

```csharp
BootstrapToastService.Default.Show(
    new BootstrapToastOptions
    {
        Title = "Saved",
        Text = "The order was saved successfully.",
        Variant = BootstrapVariant.Success
    },
    this);

BootstrapToastService.Default.ShowNotificationCenter(this);
```

Manual verification: use the **Feedback** global-service section for Light/Dark, reduced motion, all real Windows 100/125/150/175/200% scales, every placement, TopMost off/on, single/burst/long content, auto-hide on/off, close dismissal, center row/mark-all/clear, Alt+F4 hide/reopen, capacity reduction, history-disabled Toasts, focus behavior, rapid stress, moving between monitors, and secondary-monitor removal/reconfiguration. History must survive host retirement and transient Toast appearance must not steal keyboard focus.

## BootstrapDatePicker

Responsibility: provide a Bootstrap-themed date/time input while preserving native WinForms date state, range, formatting, checkbox, keyboard, localized display, and calendar-popup semantics.

Stage 9 of the component-expansion roadmap finalizes the public concepts as:

```text
BootstrapDatePicker : UserControl

BootstrapDatePicker.Value
BootstrapDatePicker.MinDate
BootstrapDatePicker.MaxDate
BootstrapDatePicker.Format
BootstrapDatePicker.CustomFormat
BootstrapDatePicker.ShowCheckBox
BootstrapDatePicker.Checked
BootstrapDatePicker.ValidationState
BootstrapDatePicker.BorderRadius
BootstrapDatePicker.ValueChanged
```

Behavior:

- `BootstrapDatePicker` owns exactly one private native WinForms `DateTimePicker`. The native picker is the single source of truth for `Value`, `MinDate`, `MaxDate`, `Format`, `CustomFormat`, `ShowCheckBox`, `Checked`, localized text, range normalization/exceptions, keyboard navigation, and the calendar popup; the framework does not mirror date state.
- The wrapper owns the single public tab stop and keeps the private native picker out of the tab sequence. Entering the wrapper or clicking its shell redirects focus to the native picker; native `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown` are forwarded through the wrapper exactly once.
- `ValueChanged` is raised only from the native picker's effective `ValueChanged` path and reports the `BootstrapDatePicker` wrapper as sender. Assigning the same value remains a native no-op, and range-driven value adjustments preserve native event behavior.
- `Format` directly uses `DateTimePickerFormat.Long`, `Short`, `Time`, or `Custom`; `CustomFormat` is passed through unchanged. Stage 9 adds no custom parser, formatter, culture property, nullable-date model, or parallel text representation.
- `ShowCheckBox` and `Checked` retain native optional-date checkbox presentation/state. Stage 9 intentionally fixes the owned picker to `ShowUpDown = false` and does not expose `ShowUpDown` publicly.
- Border priority matches the established input model: disabled presentation wins first; otherwise Valid uses success, Invalid uses danger, neutral focus uses the focus token, and unfocused neutral uses the normal border token. Palette resolution reuses `BootstrapTextBoxRenderLogic` instead of creating a competing validation formula.
- `BorderRadius = -1` uses the current theme radius; non-negative values specify an explicit logical radius and values below `-1` are rejected before mutation. Radius applies only to the framework-owned outer shell and does not promise rounded OS-owned calendar/dropdown chrome.
- Shell padding uses current `SpacingXS`; border/focus widths and radius scale through `DpiScaler`. An internal pure layout helper centers the native preferred height inside the shell and clamps narrow/tiny client rectangles without negative geometry.
- The wrapper paints only its themed surface/border. The native picker paints its own text, checkbox/dropdown affordance, focus/edit internals, and calendar popup. No `MonthCalendar`, custom `Form`, popup host, global hook, P/Invoke, private WinForms reflection, timer, animation engine, or package dependency is introduced.
- Runtime Light/Dark switches update the shell/native palette and theme-owned `Body` font while preserving native date/range/format/checkbox state. Parent-DPI changes relayout the owned native picker. Caller-assigned fonts remain caller-owned.
- Designer construction is parameterless and requires no application bootstrap. Disposal detaches native/theme handlers and disposes only framework-created font resources; the owned native picker is disposed through normal WinForms child ownership.

Manual verification: choose **Advanced Inputs**. Compare Long/Short/Time, custom `yyyy-MM-dd`, custom `yyyy-MM-dd HH:mm`, optional unchecked checkbox, constrained range, Valid/Invalid, disabled, explicit radius, and live `ValueChanged`. Exercise Tab/Shift+Tab, native calendar open/close and arrow/navigation keys, locale-sensitive display, Light/Dark switching, repeated resize, and 100/125/150/175/200% real Windows scaling. The calendar popup and localized native rendering remain WinForms/OS-owned and may differ by Windows/runtime/culture.

## BootstrapCalendar and BootstrapCalendarPicker

Responsibility: provide the separate framework-owned calendar surface for date-only single, inclusive-range, and multiple-date workflows. These controls do not replace or change the native `BootstrapDatePicker` contract.

The approved public concepts are:

```text
BootstrapCalendarSelectionMode: Single | Range | Multiple

BootstrapCalendar : Control
  SelectionMode, DisplayMonth, MinDate, MaxDate, SelectedDate,
  RangeStart, RangeEnd, SelectedDates, BorderRadius,
  SelectionChanged, DisplayMonthChanged,
  SetRange(...), SetSelectedDates(...), ClearSelection(),
  ShowPreviousMonth(), ShowNextMonth(), GetPreferredSize(...)

BootstrapCalendarPicker : Control
  SelectionMode, MinDate, MaxDate, SelectedDate, RangeStart, RangeEnd,
  SelectedDates, DateFormat, PlaceholderText, ValidationState, BorderRadius,
  SelectionChanged, Opened, Closed,
  SetRange(...), SetSelectedDates(...), ClearSelection(),
  ShowDropDown(), CloseDropDown()
```

Behavior:

- `Single` accepts zero or one date, `Range` accepts an incomplete start or a complete inclusive start/end pair, and `Multiple` keeps a sorted immutable date snapshot. Changing modes clears the prior logical selection. Mode-specific setters reject calls in another mode; `SetRange` also rejects an end without a start and orders a supplied reversed pair.
- `MinDate`, `MaxDate`, and every selected value are normalized to `.Date` and use the safe inclusive WinForms `DateTimePicker` domain (`DateTimePicker.MinimumDateTime.Date` through `DateTimePicker.MaximumDateTime.Date`). Invalid bounds and out-of-domain/out-of-bounds selections are rejected. Tightening bounds removes invalid logical selection without inventing a replacement date.
- `BootstrapCalendar` starts with the clamped `DateTime.Today` month and private focus date. `DisplayMonth` is always the first day of an in-bounds month; it changes deterministically through assignment, header navigation, or keyboard movement. `BootstrapCalendarPicker` remembers the last displayed month while open; otherwise it opens on the first available selected date (single, range start, or first multiple value), then on the clamped current month.
- The calendar projects exactly six weeks by seven days (42 cells), beginning according to `CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek`. It owner-draws the shell, month header, weekday labels, adjacent-month cells, disabled/today/hot/focus/selection/range-preview states, and uses shared theme, rendering, and DPI metrics. Its preferred size is the metric formula `2 * outer padding + 7 * preferred cell width + 6 * cell gap` by `2 * outer padding + header + weekday row + 6 * day-row height + 7 * cell gap`.
- Keyboard focus is private calendar state, not public selection state. Arrow keys move it by day or week; PageUp/PageDown move it by month; Home/End move to the culture-defined week boundary; Enter/Space activate it. Movement clamps to bounds and moves the displayed month when necessary. Mouse activation follows the same mode rules; range hover supplies preview only while a start is awaiting completion.
- `SelectionChanged` fires only for an effective logical-selection change. A same-date single activation can still complete picker selection but does not manufacture a selection change. `DisplayMonthChanged` fires only for an effective month change.
- `BootstrapCalendarPicker` paints a themed collapsed summary and opens a fresh `BootstrapCalendar` in the existing native ToolStrip dropdown. It formats summaries through `DateFormat` and the current culture when that culture's active calendar represents the date. For a safe-domain boundary outside that calendar's range, it formats with a non-mutating clone using a representable optional Gregorian calendar when possible, then another representable optional calendar, and invariant formatting only as a final fallback. `DateFormat` validation uses the same rule. It uses `PlaceholderText` with no selection and the established validation/focus/radius input shell. It transfers mode, bounds, selection, remembered month, DPI, and focus to the hosted calendar; `Opened` and `Closed` reflect the native dropdown lifecycle. Single selection and completed ranges close the picker; multiple selection remains open for toggling.
- Calendar month titles and day/navigation accessibility names use that same representable-date formatter. Week order, weekday names, and integer day labels remain sourced directly from `CurrentCulture`, preserving its regional layout behavior.
- The hosted calendar, ToolStrip menu, selection model, activation/completion signal, focus state, layout types, and synchronization wiring are implementation details. There is no public popup overload, native host accessor, active-calendar reference, timer, parser, culture override, or second native DatePicker surface.

Manual verification: use the calendar demo scenarios for empty/default, single, range preview/completion, multiple toggle, constrained safe-domain boundaries, leap February, Monday/Sunday culture week starts, keyboard-only navigation, repeated open/close, Light/Dark, disabled/validation states, and 100/125/150/175/200% real Windows scaling. Confirm that `BootstrapDatePicker` remains the separate native `DateTimePicker` wrapper with OS-owned calendar behavior.

## BootstrapTreeView

Responsibility: apply Bootstrap-themed presentation to native WinForms `TreeView` while leaving the native tree, selection, expansion, checking, label editing, keyboard/focus, hit testing, drag/drop, scrolling, and accessibility contracts authoritative.

The V1 public addition is deliberately limited to:

```text
BootstrapTreeView : TreeView

BootstrapTreeView.Variant
```

Behavior:

- `BootstrapTreeView` directly inherits `System.Windows.Forms.TreeView`; `Nodes`, `SelectedNode`, `CheckBoxes`, `ImageList`, `StateImageList`, node image/state-image indices and keys, `Indent`, `ItemHeight`, `FullRowSelect`, `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `LabelEdit`, `HotTracking`, `Scrollable`, `AllowDrop`, the native event set, and accessibility object remain inherited rather than mirrored or re-declared.
- `Variant` is the only new V1 public property. It defaults to `Primary` and selects the semantic color used for visible selected-node presentation. No `BorderRadius`, custom tree/node model, selection wrapper, or renderer interface is introduced.
- The constructor configures `DrawMode = TreeViewDrawMode.OwnerDrawAll`; this is the only supported drawing mode because the framework owns the complete themed node presentation. `DrawMode` remains an inherited mutable WinForms property and is not shadowed or artificially made immutable, so callers can assign another value, but operating away from `OwnerDrawAll` is unsupported.
- Inherited `DrawNode` remains observable and is raised exactly once for each framework node draw. It is an observation/custom-side-effect hook, not a renderer-replacement contract: setting `DrawTreeNodeEventArgs.DrawDefault = true` does not transfer painting back to the native control, and framework rendering remains authoritative after the event.
- Neutral enabled nodes may use caller-assigned `TreeNode.ForeColor`, `TreeNode.BackColor`, and `TreeNode.NodeFont`; selected/hot/disabled framework states take presentation precedence where required. Caller fonts remain caller-owned.
- The framework establishes a theme/DPI-derived default `ItemHeight` and continues to update that default while it still owns the value. Once the caller changes inherited `ItemHeight`, ownership transfers to the caller and later theme/DPI changes preserve that value. A large per-node `NodeFont` does not automatically increase row height; callers using larger node fonts must increase inherited `ItemHeight` when clipping would otherwise occur.
- `Indent` remains entirely caller/native-owned. Framework layout anchors its vector expander, connector lines, state-image/checkbox slot, node image, text, selection background, and focus cue to native row/label geometry instead of inventing a second hierarchy measurement model.
- `ImageList`, node image keys/indices, selected-image keys/indices, `StateImageList`, and native state indices remain the source of truth. Framework image rendering never disposes caller-owned lists or images. When `CheckBoxes = true` and no custom `StateImageList` is assigned, the framework paints a checkbox in the native state-image slot while native `TreeNode.Checked` and Before/AfterCheck events remain authoritative.
- State-image display geometry follows the native TreeView state-image slot and hit-test behavior. It is intentionally **not** inferred merely from `StateImageList.ImageSize`, because native TreeView may normalize the displayed slot independently of the source image dimensions. Framework bounds and tests therefore use native hit-test evidence for parity.
- Full-row selection presentation follows native conditions: framework full-row geometry applies when `FullRowSelect && !ShowLines`; with lines visible, native TreeView does not expose the same full-row selection behavior. Framework code does not impose a second full-row mouse-selection engine.
- Any full-row mouse correction is evidence-gated. Current native-baseline coverage does not establish a gap requiring a custom selection layer, so clicks, `NodeMouseClick`, selection, expander, checkbox/state-image routing, and native hit testing remain on the native path. A future correction must first be justified by a reproducible native baseline and remain narrowly scoped to that proven gap.
- Hover invalidation is presentation-only. `HotTracking` remains the inherited native property and no parallel hover-selection or activation policy is introduced.
- Up/Down, Left/Right, Home/End, PageUp/PageDown, expand/collapse keys and checkbox Space behavior stay on the native key/message path. `BeforeSelect`/`AfterSelect`, expand/collapse, and check event sequences are not duplicated by framework code.
- `LabelEdit`, `TreeNode.BeginEdit()`, the native edit HWND, commit/cancel lifecycle, and whatever F2 behavior the active WinForms/native runtime actually provides remain native. The framework adds no F2 shortcut or second editor.
- `ItemDrag` and `AllowDrop`/`DragEnter`/`DragOver`/`DragDrop` remain inherited native events. V1 contains no drag/drop reorder policy or hidden tree mutation.
- Accessibility uses the inherited native `TreeView` accessibility object/hierarchy. No replacement provider, duplicate accessible node model, or framework accessibility tree is created.
- Explicit handle recreation caused by inherited properties such as `CheckBoxes`, `Scrollable`, `ImageIndex`, or `SelectedImageIndex` follows native WinForms side effects. The framework reapplies only presentation/theme state and does not cache/restore native expansion, selection, check, edit, scroll, or image state behind WinForms.
- Theme and DPI changes invalidate/recompute framework-owned presentation, native state-image slot cache, theme-created font, and framework-owned default row height without disposing caller-owned fonts/images or replacing the native handle contract. Disposal removes the theme subscription and releases only framework-created resources.
- V1 deliberately excludes custom data binding, async/lazy loading, a tri-state data model, filtering/search, per-node disabled state, a rounded outer shell, a virtualized tree, replacement accessibility, and drag/drop reorder policy. These require separate future contracts rather than hidden additions to the native-backed control.

API review confirms that the feature exports no helper family: `BootstrapTreeViewRenderLogic`, `BootstrapTreeNodeVisualState`, `BootstrapTreeNodePalette`, `Controls.Internal.BootstrapTreeViewLayout`, its layout structs, hit-test/state-image helpers, and test seams remain internal/private. The public/protected reflection fingerprint is documented in `docs/PUBLIC_API_BASELINE.md`.

Manual verification: choose **TreeView** in the integrated demo. Exercise mouse and keyboard selection/expand/collapse/checking, Tab/Shift+Tab exit and return, `DrawNode` event counting, `BeginEdit`/native label commit-cancel, native image/state-image hit-test diagnostics, checkboxes, `FullRowSelect` with `ShowLines` both off and on, HotTracking, ItemDrag and AllowDrop observation, RTL, horizontal/vertical scrolling, Light/Dark switching, caller-owned `ItemHeight` plus large `NodeFont`, explicit handle recreation (`CheckBoxes`, `Scrollable`, `ImageIndex`, `SelectedImageIndex`), disposal/recreation, and real Windows 100/125/150/175/200% scaling.

## BootstrapListView

Responsibility: apply Bootstrap-themed, DPI-aware presentation to native WinForms `ListView` while leaving items, columns, groups, selection, checks, images, view modes, virtualization, label editing, keyboard, drag/drop, scrolling, events, and accessibility native-owned.

The V1 public addition is deliberately limited to:

```text
BootstrapListView : ListView

BootstrapListView.Variant
BootstrapListView.Striped
BootstrapListView.HoverHighlight
```

Behavior:

- `BootstrapListView` directly inherits `System.Windows.Forms.ListView`. `Items`, `Columns`, `Groups`, image lists, selection/check collections, `View`, `VirtualMode`, sorting, activation, editing, drag/drop, events, and accessibility remain inherited rather than mirrored.
- `Variant` defaults to `Primary`; `Striped` defaults to `false`; `HoverHighlight` defaults to `true`. These properties invalidate presentation only and never mutate native data or interaction state.
- The framework keeps `OwnerDraw = true` and enables protected `DoubleBuffered` internally. No public buffering or rendering-mode API is added.
- All five normal views—`Details`, `List`, `SmallIcon`, `LargeIcon`, and `Tile`—are supported. Stripes apply only to odd neutral rows in `Details` and `List`.
- To avoid the Win32 owner-draw hover repaint defect, `Details` row/cell painting is entirely `DrawSubItem`-centric. `OnDrawItem` performs no framework painting in `Details`; column zero establishes the row background before cells are rendered.
- Hover uses native `HitTest`, stores the current item index and native item rectangle, and invalidates only the previous/current item—including in virtual mode—without changing selection, focus, checked state, `HotTracking`, `HoverSelection`, or activation. Independently, inherited `HotTracking` retains a distinct underlined presentation when WinForms reports an item as hot.
- Active selection uses the semantic `Variant`; inactive selection follows `HideSelection`; and `FullRowSelect = false` limits selected treatment to the first label/cell region. `ListViewItem.Selected` (or `SelectedIndices` in virtual mode) is authoritative, so transient native custom-draw flags cannot visually select unselected grouped rows.
- A focused item retains its native label/item focus cue even when it is not selected. The normal Bootstrap path draws directly to the final paint surface so GDI text retains native fidelity. When inherited `DrawItem`, `DrawSubItem`, or `DrawColumnHeader` subscribers are present, Bootstrap and subscriber output are composed in a paint-region-local buffer with absolute event coordinates and subscriber painting last. Keeping `DrawDefault = false` preserves subscriber pixels; setting it to `true` discards the local buffer and delegates that element to native drawing without pre-painting the target surface.
- Caller item/subitem colors and fonts remain meaningful. A differing effective public color overrides a neutral stripe. WinForms exposes no reliable public flag for an explicit assignment equal to the inherited list color, so that exact same-color assignment is indistinguishable from inheritance and may still receive stripes.
- Caller-owned `SmallImageList`, `LargeImageList`, `StateImageList`, images, fonts, items, columns, and groups are read for presentation but never cloned, resized, replaced, or disposed by the framework. State images use bounded native probing to stay inside the native state-image hit region in every view; checkbox fallback is omitted only in Tile. Corresponding Small/Large item images follow the native numeric image position when views change, even when their keys differ. Details retains native `IndentCount` geometry without clipping later subitem cells, themes the filler after the final visible header, and keeps header images resolved through `SmallImageList` by index or key. `LabelWrap` controls LargeIcon multiline labels. Tile receives no fabricated checkbox, always shows the primary label, and projects ordinal subitems `1..N` from the column-header count regardless of header name, width, or display order, with at most 20 projected values. Text reading direction follows `RightToLeft` without reversing the caller/native physical `TextAlign`; structural mirroring requires both `RightToLeft = Yes` and `RightToLeftLayout = true`.
- Group membership and view restrictions remain native. V1 does not custom-paint group headers because WinForms exposes no managed group-header draw event, and it does not fake groups in `View.List`.
- Virtual mode requires the native ordering: attach `RetrieveVirtualItem` before assigning a positive `VirtualListSize`. Normal `Items`, `SelectedItems`, and `CheckedItems` are not supported virtual-mode access paths; use retrieval events and `SelectedIndices`/`CheckedIndices`.
- Current WinForms runtimes reject enabling `VirtualMode` while `View.Tile` is active with the native `NotSupportedException`. The framework preserves that runtime behavior and does not hide the exception or force a different view. Applications that need virtual mode should select a supported view first.
- Theme/font subscriptions and framework-owned fonts are released deterministically. Caller-assigned fonts survive theme changes and disposal. DPI changes scale only framework gaps/glyphs, never native bounds, columns, `TileSize`, or image-list dimensions.
- V1 excludes a rounded outer border, custom item/data model, loading or empty overlay, sort state/glyphs, custom group-header rendering, and replacement accessibility.

Normal-mode example:

```csharp
var list = new BootstrapListView
{
    Dock = DockStyle.Fill,
    View = View.Details,
    FullRowSelect = true,
    GridLines = true,
    Striped = true,
    Variant = BootstrapVariant.Primary
};

list.Columns.Add("Code", 120);
list.Columns.Add("Name", 240);
list.Items.Add(new ListViewItem(new[] { "P-001", "Northwind Widget" }));
```

Virtual-mode example with the required ordering:

```csharp
var list = new BootstrapListView
{
    Dock = DockStyle.Fill,
    View = View.Details,
    VirtualMode = true
};

list.Columns.Add("Name", 240);
list.RetrieveVirtualItem += (_, e) =>
    e.Item = new ListViewItem($"Item {e.ItemIndex}");
list.VirtualListSize = 100000;
```

Manual verification: choose **ListView** in the integrated demo. Exercise all five normal views, Details `FullRowSelect`/`GridLines`/striping, repeated cross-column hover, checks and state images, Ctrl/Shift multi-selection, label editing, groups plus the native List restriction, the 100,000-row virtual example, native keyboard/context-menu/activation behavior, HideSelection, disabled state, runtime Variant and Light/Dark changes, RTL, rapid view changes, disposal, and real Windows 100/125/150/175/200% scaling. In the Details regression scenario, subitem text/images must never disappear during repeated pointer movement.

## Deferred components

Dialog/Modal, Skeleton, and others are not part of the initial foundation contract.

Before adding one, document which existing foundation pieces it reuses.
