# Testing Strategy

## 1. Testing goals

Tests protect more than appearance. The framework must verify logic, state transitions, target-framework compatibility, control lifecycle, keyboard/focus behavior, theme switching, DPI scaling, and resource ownership.

## 2. Test layers

### 2.1 Pure unit tests

Prefer ordinary unit tests for logic that does not require a WinForms handle:

- Color transforms and contrast selection
- Theme token selection
- Clamp/compatibility helpers
- DPI scaling calculations
- Easing functions
- Progress percentage/range calculations
- Radius/geometry calculations
- Alert palette, DPI metrics, and layout calculations without WinForms handles
- Tooltip palette, DPI metrics, popup-size, and text-bounds calculations without WinForms handles
- Tab header palette, DPI metrics, uniform sizing, and layout calculations without WinForms handles
- Icon descriptor/source selection
- Selection-state algorithms
- Pagination numeric-window and ellipsis calculations

These tests should run for all appropriate target frameworks.

Phase 2 specifically covers the pure rendering foundation with automated tests for:

- 96-DPI baseline and 125/150/175/200% scaling calculations
- `Size`, `Padding`, and `Rectangle` scaling
- Per-corner radius validation and normalization
- Rounded-path geometry bounds
- sRGB luminance, contrast ratio, foreground selection, and blending
- Shared horizontal content alignment and spacing behavior

Phase 3 covers the source-neutral icon foundation with automated tests for:

- Descriptor factory/source metadata
- Invalid external-source metadata rejection
- Ordered provider dispatch and unsupported-source fallback
- SVG adapter delegation of markup, bounds, and color
- Framework vector glyph rendering without an external package

Phase 4 covers shared animation logic with deterministic clock/frame-scheduler test doubles rather than wall-clock sleeps. Automated coverage includes:

- Easing boundaries, clamping, representative curve values, and monotonic normalized output
- Finite initial/intermediate/final progress and completion exactly once per run
- Finite stop/resume, restart, repeated start/stop, and restart from completed state
- Loop cycle progress, exact-boundary wrap, multi-cycle modulo, stop/resume, and restart
- Custom easing output normalization
- Reduced-motion behavior without unnecessary frame scheduling
- Optional owner hide/show pause/resume with hidden wall-clock time excluded
- Optional owner disposal and already-disposed-owner behavior
- Event-handler reentrancy for restart/disposal
- Idempotent disposal and post-disposal operation guards

Phase 6 covers pure Button rendering/state logic with automated tests for:

- Small/default/large height selection from theme metrics
- Filled and outline semantic palette resolution
- Selected outline state becoming a filled active presentation
- Disabled colors resolving through disabled/muted theme tokens
- Uniform radius validation and internal per-corner ButtonGroup override behavior

Phase 7 covers ButtonGroup/Toolbar layout and state policy with automated tests for:

- `None`, `Single`, and `Multiple` selection behavior
- Horizontal and vertical first/middle/last per-corner assignment
- Removal restoring a Button's standalone corner behavior
- Equal-width sizing from the widest preferred Button
- Toolbar left/center/right positioning
- Horizontal `SpaceBetween` anchoring of the first and last groups
- Vertical orientation and DPI-scalable configured group spacing
- Toolbar isolation from ButtonGroup selection policy

Phase 8 covers TextBox/Card presentation logic with automated tests for:

- Neutral/focused/valid/invalid/disabled TextBox border-token resolution
- Border-radius sentinel validation for both controls
- TextBox icon slots reserving native-editor width
- Card double-buffered custom-painting styles
- Theme-default Card padding and caller-owned custom padding behavior

BootstrapPagination pure tests cover:

- Small ranges with no ellipsis
- Beginning, middle, and ending windows for large ranges
- Different `MaxVisiblePages` values
- Page `1`, current page, and last page always being retained when the range is truncated
- No duplicate numeric page entries
- Rejection of invalid total-page/current-page/max-visible inputs by the internal helper

BootstrapBadge Stage 1 pure tests cover:

- All semantic variants resolving through `BootstrapVariantColorResolver`
- Custom color overriding semantic color while retaining contrast-based foreground selection
- Disabled presentation using muted foreground and a softened surface
- Empty, short, and long text preferred-size calculations
- `SpacingSM`/`SpacingXS` padding at logical DPI 96/120/144/168/192
- Pill half-height radius plus theme-default and explicit logical-radius scaling

BootstrapAlert Stage 2 pure tests cover:

- All eight semantic variants under Light/Dark using one theme-derived surface/border/foreground formula and the 4.5:1 contrast fallback
- Disabled palette independence from the semantic variant
- 96/120/144/168/192 DPI scaling for padding, spacing, icon slot, close slot, border/focus widths, and theme/explicit radii
- Validation of undefined variants, invalid DPI, and invalid radius values before state mutation
- Exact no-icon/no-dismiss, icon-only, dismiss-only, and icon-plus-dismiss layouts
- Narrow, empty, and malformed client rectangles clamping to non-negative contained geometry without throwing

BootstrapTooltip Stage 3 pure tests cover:

- All semantic variants in Light/Dark through `BootstrapVariantColorResolver`, with border from the current theme and foreground selected through `ColorUtil.GetContrastingTextColor`
- `CustomColor` overriding only the semantic background while preserving contrast selection and validation of undefined variants
- Logical padding, border width, theme radius, and explicit radius scaling at DPI 96/120/144/168/192 through `DpiScaler`
- Rejection of null theme inputs, negative padding edges, invalid radius values, and invalid DPI before returning usable render metrics
- Popup-size calculation that adds scaled padding/border without framework auto-wrap policy and saturates pathological integer overflow
- Text-bounds calculation for asymmetric padding plus tiny/malformed rectangles without negative geometry

BootstrapTabControl Stage 4 pure tests cover:

- 96-DPI header baseline of 32px height, 12px horizontal padding, 8px content spacing, 54px minimum width, 1px border, 2px focus/underline thickness, and 6px theme radius
- Exact metric scaling at DPI 120/144/168/192 plus theme/default and explicit logical-radius behavior
- Uniform fixed-width calculation from the widest content when `Fill = false` and available client width when `Fill = true`, including minimum-width fallback and invalid count validation
- Selected/inactive/hover/disabled palette resolution for Tabs/Pills/Underline in Light/Dark using the existing semantic resolver and contrast helper
- Style-specific corner geometry: top-only Tabs, fully rounded Pills, and no rounded path for Underline
- Text/image/underline/focus geometry containment for normal, narrow, empty, and malformed rectangles without negative sizes
- Rejection of undefined `BootstrapTabStyle` / `BootstrapVariant` values before usable output is returned

BootstrapNumericBox Stage 5 pure tests cover:

- Border palette priority for disabled, validation, focus, and neutral states using the established TextBox validation tokens
- Read-only and disabled surface/foreground selection
- Validation-state and radius-sentinel rejection before mutation
- DPI scaling of horizontal padding, border/focus widths, and theme/explicit radius at 96/120/144/168/192 logical DPI
- Native-editor bounds containment for normal, narrow, empty, and malformed client sizes without negative rectangles

BootstrapComboBox Stage 6 pure tests cover:

- Disabled/valid/invalid/focused/neutral border palette priority using the established input validation tokens
- Enabled and disabled surface/foreground selection from the current theme
- Validation-state and radius-sentinel rejection before mutation
- DPI scaling at logical 96/120/144/168/192 for item height, horizontal padding, icon slot/spacing, border/focus widths, and theme/explicit radius
- Closed-item icon/text layout containment for normal, narrow, empty, and malformed rectangles without negative geometry
- Long-text bounds suitable for single-line ellipsis while retaining native `GetItemText` as the source of display text

BootstrapDropdown Stage 7 pure tests cover:

- Every semantic `BootstrapVariant` under Light/Dark through `BootstrapVariantColorResolver`, with normal surface/text/border tokens, selected background blending, and disabled muted/accent tokens
- Undefined variants and null theme-color inputs rejected before usable palette output
- Logical 96/120/144/168/192 DPI scaling for horizontal/vertical item padding, icon size, separator inset, and border width through `DpiScaler`
- Null metric inputs and non-positive DPI rejection
- `BootstrapDropdownItemKind` accepting only `Item`, `Separator`, and `HostedControl`; consumers with exhaustive enum switches must handle the additive member
- Null text normalization, immutable kind, stable child collections, default enabled/unchecked state, item sender identity for leaf `Click`, and null-safe collection insertion/replacement
- Reference-identity tree validation: duplicate/shared/cyclic nodes, normal-item factories, separator children/factories, and hosted items missing factories or containing children are rejected before native snapshot creation
- Recursive snapshot structure, leaf-only activation, per-level image/check margins, connected split seam/corner resolution, full-width split anchor request, and narrow/tiny bounds containment

BootstrapDatePicker Stage 9 pure tests cover:

- Native `DateTimePicker` default/range/format/custom-format/checkbox characterization without framework assumptions about locale-specific rendered strings
- Disabled/valid/invalid/focused/neutral border palette priority through the established TextBox validation model
- Enabled/disabled surface and foreground selection from the current theme
- Validation-state/radius rejection plus null-theme and invalid-DPI guards before usable output
- DPI scaling at logical 96/120/144/168/192 for `SpacingXS`, border/focus widths, and theme/explicit radius
- Native-picker bounds containment for normal, narrow, tiny, empty, and malformed clients without negative rectangles

### 2.2 WinForms control tests

Tests that instantiate or interact with controls must run on Windows and use an STA-capable execution strategy.

Examples:

- Default property values
- Theme change reaction
- Keyboard activation
- Focusability/TabStop
- Loading disables interaction
- Collapse state transitions
- Accordion single-open behavior
- Animation stop/dispose behavior
- DataGridView style application
- Pagination boundary, focus, accessibility, event, and disposal behavior

Do not rely on arbitrary sleeps to wait for animation. Prefer controllable clocks/timing abstractions where practical or test state/progress deterministically.

Phase 4 owner lifecycle tests instantiate real WinForms `Control` owners on STA threads while keeping animation time and frame delivery manually controlled.

Phase 6 Button control tests run in STA and verify the finalized default contract, loading suppression of `PerformClick()`, and preferred-size stability when loading is toggled even when `LoadingText` differs from normal `Text`.

Phase 7 Group/Toolbar tests run in STA and exercise real `BootstrapButton`, `BootstrapButtonGroup`, and `BootstrapButtonToolbar` controls. Tests call `PerformClick()` for activation semantics and `PerformLayout()` for deterministic connected-corner/equal-size/alignment assertions rather than duplicating the production algorithms in test helpers.

Phase 8 TextBox/Card tests run in STA and verify:

- `BootstrapTextBox` contains exactly one native borderless `TextBox` editor and keeps that editor out of the tab sequence while the composed control owns the public tab stop.
- `Text`, `ReadOnly`, and `UseSystemPasswordChar` forward to the native editor.
- Placeholder visibility follows the actual text value without becoming editor content.
- The clear affordance uses the normal native `TextChanged` path and disappears when no editable text remains.
- Runtime theme changes update Card region colors and preserve explicit custom Card padding.
- Header/Body/Footer are stable child containers suitable for Designer serialization.

BootstrapPagination STA tests verify:

- Default values, `AccessibleRole.Grouping`, and the default accessibility description
- Exactly one owned horizontal `BootstrapButtonGroup` using `SelectionMode.None`
- First/Previous/Next/Last boundary enablement and current-page focusability
- Disabled/non-tabbable ellipses and semantic accessible names for navigation/current/other page buttons
- Mouse/native `PerformClick()` navigation with exactly one `PageChanged` event for an effective page change and no event for no-op activation
- Range-change clamping semantics for `TotalItems` and `PageSize`
- `ButtonSize`, `Variant`, and `BorderRadius` presentation changes without changing page state
- Disposal of dynamically removed buttons and normal disposal of the owned group
- Repeated state/visibility/style changes without duplicate child controls or event multiplication
- Preferred-size containment of the owned connected group

BootstrapBadge Stage 1 STA tests verify:

- Designer-safe defaults: empty text, `AutoSize = true`, `TabStop = false`, Primary variant, empty custom color, non-pill shape, theme-radius sentinel, and `AccessibleRole.StaticText`
- Null text normalization and preferred width tracking content length
- Rejection of `BorderRadius < -1` and undefined variants
- `CustomColor` accepting `Color.Empty`/fully opaque values and rejecting transparent or semi-transparent colors
- Double-buffered custom-paint styles and non-selectable behavior
- Runtime Light/Dark changes leave the active theme font usable
- Theme subscriptions detach on disposal, framework-created fonts are released, and caller-owned fonts remain caller-owned

BootstrapAlert Stage 2 STA tests verify:

- Designer-safe defaults/metadata, inherited `Text` normalization, `DefaultProperty(Text)`, `DefaultEvent(Dismissed)`, `TabStop = false`, and `AccessibleRole.Alert`
- Exactly one private native dismiss `Button`, correct accessible metadata, and no nested `Panel`, `Label`, or `BootstrapButton`
- Variant/radius/renderer validation before mutation plus content-icon independence from unrelated state
- Double-buffered owner painting, deterministic close-button layout, content-icon renderer dispatch, and framework close-glyph dispatch through the configured renderer
- Light/Dark/variant/disabled/icon/dismissible/multiline/radius paint smoke without exceptions
- Programmatic and native-button dismissal event counts, direct-visibility non-events, re-show/re-dismiss behavior, disabled programmatic dismissal, and non-disposal
- Dismissibility-driven native focusability without duplicate children or event handlers
- Runtime theme palette/font updates in place, caller-owned font preservation, theme-subscription cleanup, framework-owned font disposal, and repeated lifecycle stress

BootstrapTooltip Stage 3 STA tests verify:

- Designer-safe defaults for Dark variant, empty custom color, theme-radius sentinel, 8/4/8/4 logical padding, and native timing/state defaults
- `[ProvideProperty("ToolTip", typeof(Control))]`, `IExtenderProvider`, and `CanExtend` behavior limited to WinForms controls
- Parameterless and `IContainer` construction, including wrapper-only external-container ownership and exactly one privately owned native ToolTip
- Multiple control associations, explicit newline preservation, empty-caption removal, and null-argument guards without a parallel caption dictionary
- Undefined variant, invalid radius, and negative-padding rejection before state mutation
- Direct two-way forwarding of `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` to the native ToolTip
- Native owner-draw/non-balloon configuration while keeping the native Tooltip and unplanned popup APIs out of the public surface
- Runtime theme switching without adding a `BootstrapThemeManager.ThemeChanged` subscription; popup/draw resolve the current theme on demand
- Idempotent deterministic disposal of the owned native ToolTip and absence of public native-object leakage

BootstrapTabControl Stage 4 STA tests verify:

- Designer-safe/native defaults, `DefaultEvent(SelectedIndexChanged)`, exact four-member framework property surface, `OwnerDrawFixed`, and fixed sizing
- Native `TabPage` collection identity/order, `SelectedTab`/`SelectedIndex`, and exactly one inherited `SelectedIndexChanged` notification for an effective change
- Framework properties changing presentation without replacing pages or mutating native selection
- `HotTrack`, `ShowToolTips`, `ImageList`, and inherited native padding remaining caller-owned
- Fill/non-Fill uniform `ItemSize`, dynamic widening after native `TabPage.Text` changes, and preservation of native page order
- Owner-draw smoke under Light/Dark for Tabs/Pills/Underline with images, tooltip text, long labels, selected/inactive/disabled states, and no mutation of native page metadata
- Runtime theme typography changes updating the same control in place, caller-owned font preservation, framework-owned font disposal, static-theme subscription cleanup, and 100-cycle lifecycle stress
- Integrated `NavigationDemoForm` coverage for all three styles, Fill, images/tooltips, disabled/long-label pages, all eight variants, and visible native selection-event feedback

BootstrapNumericBox Stage 5 STA tests verify:

- Designer-safe native defaults, `DefaultProperty(Value)`, `DefaultEvent(ValueChanged)`, `AccessibleRole.SpinButton`, and the exact planned public member surface
- Exactly one owned borderless native `NumericUpDown`, with the wrapper as the single tab stop
- Direct forwarding of value/range/increment/decimal/thousands/read-only properties and preservation of native range normalization/exceptions
- Exactly one wrapper `ValueChanged` notification for each effective native value change
- Tab entry, Shift+Tab exit, shell click focus redirection, and wrapper keyboard-event forwarding without duplicate event paths
- Native Up/Down spin boundaries, read-only spin semantics, and mouse-wheel behavior without framework reimplementation
- Runtime Light/Dark palette/font updates, caller-owned font preservation, deterministic theme subscription cleanup, and repeated lifecycle stress
- Draw/layout smoke for validation, disabled/read-only, explicit radius, tiny bounds, and supported logical DPI calculations

BootstrapComboBox Stage 6 STA tests verify:

- Designer-safe/native defaults, `OwnerDrawFixed`, the exact four framework properties, runtime-only `IconRenderer`, and no duplicate public binding/selection/autocomplete surface
- Unbound item/selection behavior and event-count parity with a plain native `ComboBox`, including no-op selection assignments
- Bound-object `DataSource` identity, `DisplayMember`, `ValueMember`, `SelectedValue`, selection changes, rebinding/clearing, and native `GetItemText` formatting semantics
- Framework-only `ValidationState`, `BorderRadius`, `LeadingIcon`, and `IconRenderer` changes do not raise native selection/commit events or mutate selection/binding state
- Owner-draw text uses native display resolution; `LeadingIcon` is limited to the closed `ComboBoxEdit` presentation and does not create a per-item item model
- Handle recreation preserves data source, selected value/index, DropDown style, and native autocomplete configuration without framework caches
- Hosted `DropDown` / `DropDownClosed`, `DropDownList`, editable `DropDown`, Up/Down/Enter/Escape, Tab traversal, disabled/re-enabled interaction, and native autocomplete restrictions remain WinForms behavior
- Runtime Light/Dark updates palette/theme-owned font/item metrics while preserving items/data/selection/autocomplete; caller-owned fonts remain caller-owned
- Disposal before/after handle creation/theme changes releases the static theme subscription and framework-owned font, and repeated lifecycle stress returns handler count to baseline
- Advanced Inputs demo coverage includes native unbound/bound lists, editable autocomplete, selection-only mode, long text, leading-icon/no-icon, valid/invalid, disabled, radius, and OS-owned popup/edit/arrow note

BootstrapDropdown Stage 7 STA tests verify:

- Designer-safe defaults: null target, one stable empty `Items` collection, Primary variant, zero minimum width, and no popup transition from `Close()` on a new component
- Explicit error for `Show()` without `Target`, validation of negative `MinimumWidth` and undefined variants, and no-op opening for empty items, disabled targets, loading targets, or disposed targets
- Exactly one native `ToolStripDropDownMenu`, native AutoClose behavior, and one `Opened`/`Closed` event per real native transition
- Target click toggle semantics, replacement/disposal detachment, and caller ownership of every target button
- Snapshot rebuild on every opening, including add/remove/clear/reorder plus mutated text/enabled/checked state without a live collection-synchronization engine
- Enabled item activation dispatching one model `Click`, with disabled items and separators blocked and `Checked` never auto-toggled by framework code
- Target `IconRenderer` usage, DPI-scaled `MinimumWidth`, open-popup theme refresh, regenerated bitmap disposal, and deterministic component/theme/target cleanup
- Repeated open/close/theme-switch cycles without duplicate event delivery, stale image use, or disposed-object failures
- Integrated Navigation demo coverage for basic/icon/state/long/stress scenarios while retaining exactly one shared Navigation route

Advanced Dropdown and `BootstrapSplitButton` STA tests additionally verify:

- Recursive native submenu snapshots, disabled parents/leaves, nested leaf sender/event count, renderer/theme/font propagation, per-level margins, and recursive icon refresh/disposal
- Hosted factories creating fresh controls per snapshot, rejection of null/disposed results, partial-build cleanup, native `ToolStripControlHost` ownership, repeated rebuild/disposal, and mixed nested/hosted composition
- Split primary-versus-chevron routing, public show/close parity, outer lifecycle sender, full-width anchor request, chevron selected state, live `MinimumWidth`, and empty/disabled/loading suppression
- Two native-focusable button regions with Tab/Shift+Tab and Enter/Space semantics, inherited custom-font persistence, dynamic primary/menu accessibility names, and no strongly typed child accessors
- Parent disposal closes and disposes the owned Dropdown while base `Control` remains the single child-control disposal owner

### 2.3 Demo/manual visual tests

A demo application is required because not all rendering quality is productively asserted with pixels.

Every component page should expose relevant states side by side.

Manual checks include:

- Visual alignment
- Font clipping
- Border/radius quality
- Focus visibility
- Hover/pressed feedback
- Light/Dark contrast
- Designer behavior
- Rapid resize

For Phase 2, start the demo and choose **Rendering / DPI**. The preview draws shared rendering primitives at virtual 96/120/144/168/192 DPI so radius normalization, scaled strokes, contrast, and content layout can be compared side by side. Switch Light/Dark while the window is open to verify theme-dependent rendering. The virtual preview is a repeatable diagnostic aid; final DPI verification still requires real Windows scaling.

For Phase 3, choose **Icons**. Verify that Segoe MDL2 and framework vector glyphs use the current theme color, remain centered while resizing, and continue to render after Light/Dark switches. If the Windows font is unavailable, the demo must report the MDL2 source as unavailable instead of failing. SVG adapters are implementation-specific and should add their own visual verification while retaining the common `IIconRenderer` contract.

For Phase 4, choose **Animation**. Verify finite Start/Stop/Restart, loop Start/Stop/Restart, normalized progress labels, and smooth movement. Start both animations, choose **Hide previews**, leave them hidden briefly, then choose **Show previews**; progress must resume from the retained logical position rather than jump by hidden wall-clock time. Toggle **Reduced motion** and explicitly Start/Restart: the finite animation must immediately reach its final state and the loop must remain at zero without continuous movement. Switch Light/Dark while the diagnostic window is open to confirm the demo continues to render with current theme tokens.

For Phase 6, choose **Button**. Compare all filled and outline variants, Small/Default/Large sizing, left/right framework icons, selected/disabled/custom-radius examples, and hover/pressed/focus feedback. Tab through buttons and activate with Enter/Space. Trigger the async **Save** example and verify the same button cannot be reactivated while loading and that its measured size is unchanged before/after loading. Switch Light/Dark and Reduced motion while the loading example is active; the button and composed spinner must follow the current theme without introducing a Button-owned timer.

For Phase 7, choose **Groups / Toolbar**. Verify a horizontal Single-selection group, vertical Multiple-selection group, EqualWidth connected buttons, explicit outer group radius, a fixed-width horizontal `SpaceBetween` toolbar, and a vertical toolbar. Tab through grouped Buttons and activate them with Enter/Space; focus remains on the child Buttons. Inspect inner seams for square connected corners and a single continuous border, then switch Light/Dark and run the same page at each supported Windows DPI setting. Toolbar actions whose group uses `SelectionMode.None` must not change `Selected` state.

For Phase 8, choose **TextBox / Card**. Verify placeholder behavior, leading/trailing icons, clear button, valid/invalid borders, native read-only/password/disabled behavior, Tab focus, selection/copy/paste, and live Light/Dark switching. Compare default, Header/Body/Footer, shadow, and borderless/custom-radius cards. Resize repeatedly and run the page at 100/125/150/175/200% Windows scaling; editor text must remain vertically usable, icon slots must stay aligned, Card corners must not clip, and the lightweight shadow must not leave stale pixels.

For Pagination, choose **Pagination** in the integrated demo. Verify the small-range, middle-window, boundary, zero-item, Small/Default/Large, and directional-navigation visibility scenarios. Use mouse and keyboard to activate First/Previous/numeric/Next/Last controls, confirm the active page stays focusable/selected, and confirm ellipses are skipped by the tab sequence. Exercise the DataGrid example and verify the application owns the source table and ten-row slicing in response to `PageChanged`. Switch Light/Dark, resize repeatedly, and repeat under the supported real-Windows DPI matrix.

For BootstrapBadge Stage 1, choose **Feedback** in the integrated demo. Compare all eight semantic variants, default/pill geometry, custom color, disabled state, explicit square radius, and long-text AutoSize behavior. Switch Light/Dark while the page remains open, resize repeatedly, and repeat at 100/125/150/175/200% real Windows display scaling. Verify text remains unclipped, the compact padding scales consistently, the pill stays half-height rounded, and long labels expand without becoming focusable.

For BootstrapAlert Stage 2, stay on **Feedback** and compare all eight semantic variants plus icon, dismissible, multiline, disabled, and explicit-radius alerts. Use mouse, Tab/Shift+Tab, Enter, and Space on the native close affordance; verify focus is visible, each effective dismissal raises one event, direct visibility changes do not, and **Restore dismissed alerts** reuses the same instances across repeated cycles. Switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling to inspect text/icon/close alignment and rounded borders.

For BootstrapTooltip Stage 3, stay on **Feedback** and hover the default Dark target, the second target using the same Tooltip instance, the semantic Info target, the custom-color target, the explicit multiline target, and the long single-line target. Change Initial/Reshow/Auto-pop delays and Active/Show always live and confirm native timing behavior changes without manual popup positioning. Switch Light/Dark while the page remains open, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling. Inspect padding, border, radius, text contrast/alignment, explicit newlines, and native screen-edge placement; the framework must not impose automatic word wrapping or a custom popup window.

For BootstrapTabControl Stage 4, choose **Navigation / Tabs**. Compare Tabs/Pills/Underline, Fill off/on, all semantic variants, image-by-key/index, native tooltip text, disabled pages, and long labels. Exercise mouse selection plus Tab, arrow-key, and Ctrl+Tab native keyboard paths; verify focus remains visible and the status label reports inherited `SelectedIndexChanged`. Resize repeatedly, switch Light/Dark live, and repeat under 100/125/150/175/200% real Windows display scaling. Add enough tabs to force native overflow and verify the framework has not replaced native overflow controls, hit-testing, selection, or page hosting.

For BootstrapNumericBox Stage 5, choose **Advanced Inputs**. Verify integer/default, decimal `0.25` increment with two decimal places, thousands formatting, signed `-100..100` range with step `10`, valid/invalid borders, read-only versus disabled behavior, and live `ValueChanged` feedback. Type culture-sensitive values, use Up/Down and native spin buttons, exercise mouse wheel, Tab/Shift+Tab, switch Light/Dark, resize repeatedly, and repeat at 100/125/150/175/200% real Windows scaling.

For BootstrapComboBox Stage 6, stay on **Advanced Inputs**. Verify unbound items, a bound object list using `DisplayMember`/`ValueMember`, editable `DropDown`, selection-only `DropDownList`, native `SuggestAppend` with `ListItems`, long text/ellipsis, leading-icon and no-icon comparison, Valid/Invalid, disabled, and explicit radius examples. Exercise the native arrow and popup plus Up/Down/Enter/Escape, free typing, Tab/Shift+Tab, selected-value changes, and runtime Light/Dark switching without losing binding or selection. Repeat at 100/125/150/175/200% real Windows scaling. Native editable child, arrow button, and popup chrome may remain OS-themed/square; the framework must not replace them merely for visual uniformity.

For BootstrapDropdown and `BootstrapSplitButton`, use the **Navigation / Tabs** basic/icon/state/long/stress/nested/hosted/mixed/split scenarios. Verify primary versus chevron mouse routing, Tab/Shift+Tab between regions, Enter/Space on each region, native Up/Down/Home/End/Right/Left/Enter/Escape submenu navigation, and outside-click dismissal. Focus/edit/toggle hosted controls, navigate back to menu rows, dismiss, reopen, and confirm the documented fresh-snapshot policy. Cover disabled leaves/submenus/hosts, checked leaves, split loading, Light/Dark changes while root/submenus are visible, default and caller-owned fonts, and primary/menu accessibility names before/after outer `Text` and `AccessibleName` changes. Repeat at 100/125/150/175/200% real Windows scaling and at bottom/right/secondary-monitor edges. Stress repeated open/close/rebuild, hosted disposal, and form disposal while nested content is open; check focus restoration, stale windows, duplicate events, GDI growth, and disposed-object failures. Inherited split `Controls` are observable but must not be mutated or disposed by application code.

BootstrapDatePicker Stage 9 STA tests verify:

- Designer-safe/native defaults, `DefaultProperty(Value)`, `DefaultEvent(ValueChanged)`, `AccessibleRole.DropList`, and the exact planned public framework member surface
- Exactly one owned native `DateTimePicker`, wrapper-owned single tab stop, native child `TabStop = false`, and intentionally internal `ShowUpDown = false`
- Direct forwarding/parity for value, range, `DateTimePickerFormat`, custom format, optional checkbox state, native exceptions, range-driven changes, and effective `ValueChanged` event counts
- Wrapper focus redirection plus exactly-once native KeyDown/KeyPress/KeyUp/PreviewKeyDown forwarding without a second keyboard/date model
- Native DropDown/CloseUp calendar transitions remaining available on the owned picker, culture-sensitive native text parity, and no custom calendar/popup/parser surface
- Runtime Light/Dark shell/font updates while preserving native state; caller-owned font preservation; theme subscription/framework-font cleanup; repeated lifecycle stress
- Draw/layout smoke for neutral/valid/invalid/disabled/explicit-radius states and pure layout parity without painting over native content
- Advanced Inputs demo coverage for Long/Short/Time, custom date/date-time, optional unchecked checkbox, constrained range, validation, disabled, explicit radius, live `ValueChanged`, and native-calendar ownership guidance

Manual BootstrapDatePicker verification remains required on real Windows for calendar popup behavior, locale-sensitive text/formatting, keyboard/focus traversal, and physical DPI rendering because those surfaces are owned by WinForms/Windows rather than the framework shell.

## 3. DPI matrix

Release/manual checks must cover:

```text
100%
125%
150%
175%
200%
```

Verify:

- Text is not clipped.
- Icons remain sharp/aligned.
- Border widths/radii scale acceptably.
- Control preferred sizes remain usable.
- Nested layouts do not drift.
- Accordion/Collapse measured heights remain correct.
- DataGridView headers/rows remain aligned.
- Pagination connected seams, ellipses, and current-page focus visuals remain aligned.
- Badge compact padding, preferred size, and pill/theme/explicit radii remain aligned with text.
- Alert text/icon/close slots, border/focus widths, and theme/explicit radii remain aligned and unclipped.
- Tooltip content padding, border width, theme/explicit radius, and text alignment scale without clipping while native popup positioning remains intact.
- Tab header height/padding/image spacing/minimum width/border/focus/underline/radius scale while native page geometry, selection, and overflow remain intact.
- NumericBox shell padding, border/focus widths, radius, and native editor bounds remain aligned without clipping the native text/spin affordance.
- ComboBox fixed item height, text/icon slot, border/focus width, and shell radius scale without corrupting native selection, arrow hit-testing, edit child, or popup geometry.
- Dropdown item padding, icon/check slots, separator inset, border, and logical `MinimumWidth` scale while native ToolStrip focus, keyboard navigation, AutoClose, and working-area placement remain intact.

The Phase 2 Rendering/DPI demo covers the geometry calculations at all five scale factors. Run the demo under each corresponding Windows display-scaling setting as components are added and during hardening; do not treat the virtual matrix as proof of OS-level DPI behavior.

For Phase 8 specifically, verify the native TextBox caret/text baseline remains centered and usable after DPI changes, leading/trailing/clear slots remain inside the rounded input, and Card theme-default padding scales without overwriting application-set custom padding.

Pagination does not own a DPI-specific renderer; its real-Windows DPI check verifies that composed ButtonGroup/Button preferred sizes, connected seams, accessible navigation, and wrapping-free AutoSize behavior remain correct together.

Badge pure tests cover logical scaling through 96/120/144/168/192 DPI. The Feedback page remains the manual OS-level DPI gate because `DeviceDpi`, text rendering, and physical display scaling are WinForms/Windows behaviors rather than pure arithmetic alone.

Alert pure tests cover the same 96/120/144/168/192 logical matrix for padding, icon/close reservations, border/focus widths, and radius scaling. The shared Feedback page remains the Alert OS-level gate for `DeviceDpi`, wrapped text, native close-button focus/activation, and physical rounded-border rendering.

Tooltip pure tests cover 96/120/144/168/192 logical scaling for content padding, border width, and theme/explicit radius. The shared Feedback page remains the Tooltip OS-level gate because associated-control `DeviceDpi`, native ToolTip placement, monitor-edge behavior, owner-drawn text rendering, and physical rounded-border output require a real Windows popup.

TabControl pure tests cover 96/120/144/168/192 logical header sizing, padding, spacing, stroke, underline, minimum width, and radius. The Navigation / Tabs page remains the OS-level gate for `DeviceDpi`, native `GetTabRect` geometry, text/image rendering, focus cues, keyboard navigation, and overflow controls under physical display scaling.

NumericBox pure tests cover 96/120/144/168/192 logical shell metrics and native-editor bounds. The Advanced Inputs page remains the OS-level gate for `DeviceDpi`, native `NumericUpDown` text/spin layout, culture-sensitive editing, wheel behavior, focus cues, and physical rounded-border rendering.

ComboBox pure tests cover 96/120/144/168/192 logical item/icon/padding/border/focus/radius metrics. The Advanced Inputs page remains the ComboBox OS-level gate for actual native edit/arrow/popup geometry, `DeviceDpi`, owner-drawn text/icon alignment, keyboard behavior, and physical shell-border rendering.

Dropdown pure tests cover 96/120/144/168/192 logical item padding, icon size, separator inset, border width, and target-relative minimum-width scaling. The Navigation page remains the OS-level gate for actual `ToolStripDropDownMenu` `DeviceDpi`, item/text/check geometry, monitor-edge placement, focus restoration, and native keyboard/AutoClose behavior.

DatePicker pure tests cover 96/120/144/168/192 logical shell padding, border/focus widths, radius, and native-picker containment. The Advanced Inputs page remains the OS-level gate for actual `DeviceDpi`, localized native text, checkbox/dropdown affordance, native calendar popup geometry/navigation, focus cues, and physical shell-border rendering; the framework does not replace or normalize the popup.

## 4. Theme matrix

Every in-scope control must be checked under:

```text
Light at creation
Dark at creation
Light -> Dark at runtime
Dark -> Light at runtime
Control created after a runtime switch
Multiple controls subscribed simultaneously
Disposed control after a theme switch
```

A disposed control must not be kept alive by the theme manager.

Pagination itself does not subscribe to the theme manager. Theme-matrix verification confirms its composed ButtonGroup/Button children continue to react through their existing lifecycle without introducing a duplicate Pagination subscription.

Badge owns one direct theme subscription because it custom-paints semantic presentation and owns a theme-created font. Tests verify runtime theme changes keep that font usable and disposal removes the subscription without disposing caller-owned fonts.

Alert likewise owns one direct theme subscription because it custom-paints its semantic surface and owns a `Typography.Body` font. Theme-matrix tests verify the same Alert instance updates palette/font/layout in place and disposal removes that subscription without disposing caller-owned fonts or icon infrastructure.

Tooltip deliberately owns no direct theme subscription. Popup and Draw handlers resolve `BootstrapThemeManager.CurrentTheme` at event time, so the next popup/draw after a runtime switch must use the new palette/metrics/`BodySmall` typography while disposed Tooltip components add no static-event lifetime root.

TabControl owns one direct theme subscription because it custom-paints native header rectangles and uses theme `Body` typography. Theme-matrix tests verify the same native-backed instance preserves `TabPages`/selection while updating header palette/font/size, and disposal releases only the framework-owned subscription/font while caller fonts remain usable.

NumericBox owns one direct theme subscription because its wrapper custom-paints the shell and owns a theme-created Body font. Tests verify the same native-backed instance preserves numeric state while changing palette/font/layout, and disposal returns static theme-handler count to baseline without disposing caller-owned fonts.

ComboBox owns one direct theme subscription because it owner-draws item presentation, paints the shell border, and owns a theme-created Body font. Tests verify runtime Light/Dark changes preserve `Items`/`DataSource`, selected item/value/index, DropDown mode, and autocomplete configuration; disposal returns the static theme-handler count to baseline without disposing caller-owned fonts or renderers.

Dropdown owns one direct theme subscription because an already-open native popup must refresh semantic renderer state and generated icon bitmaps after a runtime theme switch. Theme-matrix tests verify the same public item models remain authoritative, generated images are replaced/disposed rather than retained, and component disposal removes the subscription without disposing the caller-owned target, item models, descriptors, or target `IconRenderer`.

DatePicker owns one direct theme subscription because its wrapper paints the shell and owns a theme-created `Body` font. Theme-matrix tests verify the same native-backed instance preserves value/range/format/custom-format/checkbox state while palette/font/layout update, and disposal returns the static theme-handler count to baseline without disposing caller-owned fonts.

## 5. Interaction matrix

Interactive controls should be exercised in:

- Normal
- Hover
- Pressed
- Focused
- Disabled
- Selected/Expanded when applicable
- Loading when applicable

Keyboard paths must be tested separately from mouse paths.

For Phase 7, Group and Toolbar themselves are intentionally non-focusable; interaction remains on each `BootstrapButton`. Selection policy is validated through the same Button activation path used by mouse and keyboard input, while Toolbar remains layout-only.

For Phase 8, `BootstrapTextBox` owns one public tab stop and forwards focus to its native editor. Test Tab entry, Shift+Tab exit, pointer clicks on the border/placeholder, keyboard text selection, clipboard commands, clear-button behavior, read-only copy behavior, and password masking. `BootstrapCard` is a non-focusable container; focus order belongs to controls placed in Header/Body/Footer.

For Pagination, the container and owned ButtonGroup are intentionally non-focusable; enabled child Buttons own Tab/Enter/Space behavior. Boundary navigation and ellipses are disabled, while the selected current-page button remains enabled/focusable and activation is a no-op. Setting `Enabled = false` on the Pagination container must prevent child activation without mutating `CurrentPage`.

Badge is intentionally outside the interactive focus/keyboard matrix: it is a non-focusable `StaticText`-style indicator with no click, toggle, or business-count semantics. Its enabled/disabled presentation is still covered.

Alert itself is also outside the tab sequence. With `Dismissible = true`, exactly one native child Button becomes focusable and owns normal WinForms Tab/Shift+Tab, Enter, Space, accessibility, and disabled interaction behavior. Programmatic `Dismiss()` remains valid while disabled; direct `Visible` changes must not synthesize `Dismissed`.

Tooltip does not replace or capture the associated control's keyboard/focus behavior. Interaction verification focuses on native hover popup behavior, `Active`/`ShowAlways` state, delay forwarding, association changes, multiple target controls, and preservation of native popup placement rather than introducing a framework-owned focusable popup surface.

TabControl must retain native interaction ownership. Test normal/hover/selected/disabled/focused header presentation while mouse selection, Tab entry, arrow navigation, Ctrl+Tab page cycling, `SelectedIndexChanged`, page identity, and native overflow/hit-testing remain WinForms behavior. Framework property changes must not synthesize selection events or replace pages.

NumericBox owns one public tab stop while its private native editor has `TabStop = false`. Test wrapper Tab entry and Shift+Tab exit, shell clicks redirecting focus to the editor, wrapper KeyDown/KeyPress/KeyUp/PreviewKeyDown forwarding exactly once, and preservation of native spin buttons, Up/Down, wheel, boundaries, and read-only spin behavior.

ComboBox remains a native `ComboBox`, so it retains one native focus/input path rather than a wrapper redirect. Test `DropDownList` and editable `DropDown`, native arrow/popup open-close, Up/Down/Enter/Escape, free typing, autocomplete, Tab/Shift+Tab traversal, disabled/re-enabled behavior, bound/unbound selection, and native selection/dropdown events. Framework presentation changes must never synthesize those events or create a second selection state.

Dropdown interaction begins on its caller-owned `BootstrapButton` target or one of the split button's two native-focusable regions and transfers to native `ToolStripDropDownMenu` behavior while open. Test primary/chevron Enter/Space routing, Tab/Shift+Tab region traversal, recursive native menu keys, hosted-control focus return, Escape/outside dismissal, disabled rows, and loading suppression. `Checked` is display state rather than a toggle policy. Advanced composition still introduces no second focus, keyboard, placement, or animation engine.

## 6. Animation matrix

For finite and loop animation, test:

- Start
- Stop
- Restart
- Dispose
- Hide/show
- Reduced motion
- Rapid repeated toggles
- Reverse direction during an active transition when the consuming control supports reversal
- Final value/state after completion

Shared Phase 4 primitives additionally verify that progress is elapsed-time based rather than tick-count based, stop/resume excludes paused time, completion is emitted exactly once, loop progress wraps predictably, and event callbacks can safely stop/restart/dispose the animation.

Animated controls must not continue producing useful work after disposal. New control-specific timers are prohibited unless an explicit documented exception is approved.

Pagination is explicitly outside the animation matrix: page-state and navigation changes are immediate and it must not introduce a timer or animation owner.

Badge is also outside the animation matrix and owns no timer or animation primitive.

Alert is outside the animation matrix as well: Stage 2 dismissal is immediate and the component owns no timer, timeout, animation primitive, auto-hide scheduler, or Toast queue behavior.

Tooltip is outside the framework animation matrix: native WinForms ToolTip owns its timing behavior, while Stage 3 adds no custom timer, animation scheduler, fading abstraction, or popup loop.

TabControl is outside the animation matrix: Stage 4 header state changes are immediate and no timer, animation owner, transition engine, or reduced-motion-specific behavior is introduced.

NumericBox is outside the animation matrix: Stage 5 focus, validation, value, and spin changes are immediate and no timer or animation primitive is introduced.

ComboBox is outside the animation matrix: Stage 6 selection/dropdown/autocomplete behavior remains native and presentation changes are immediate. It introduces no timer, animation owner, popup scheduler, or reduced-motion-specific path.

Dropdown is outside the animation matrix: Stage 7 delegates popup opening/closing and native timing to `ToolStripDropDownMenu`; it adds no framework timer, animation owner, custom fading/slide transition, reduced-motion branch, or popup scheduler.

## 7. Resource/lifecycle checks

Use targeted stress/manual tests for repeated creation and disposal of animated/custom-painted controls.

Watch for growth in:

- GDI handles
- USER handles
- Active timers
- Event subscriptions retaining disposed controls
- Cached bitmaps/fonts/paths

Exact zero-allocation rendering is not required. Unbounded growth is unacceptable.

For Phase 4, the animation object owns its internal frame scheduler and subscriptions to the optional lifecycle owner; disposal must release both. The supplied owner control is never owned by the animation object.

For Phase 6, Button owns only its theme subscription, theme-created font, and composed Spinner control. Spinner remains the sole owner of loading animation scheduling. Repeated Button creation/disposal must not leave either Button or Spinner subscribed/running.

For Phase 7, ButtonGroup owns only theme notification plus child Button event subscriptions needed for selection/layout; removal/disposal must detach those handlers and clear internal grouped-corner overrides. Toolbar subscribes only to child Group size/visibility changes for layout and never to Button click/selection events.

For Phase 8, TextBox and Card own only theme subscriptions plus TextBox's theme-created font. They must unsubscribe/dispose deterministically. Card shadow painting must not retain bitmaps, paths, brushes, or pens between frames; all temporary GDI objects remain scoped to painting.

For Pagination, rebuilding the paging structure must dispose every removed dynamic Button after detaching its click handler. Repeated current-page/max-visible/navigation-visibility changes must not duplicate controls or handlers. Disposing Pagination releases the owned ButtonGroup and its current Buttons through normal WinForms ownership. Pagination owns no timer, custom GDI cache, or direct theme subscription.

For Badge, every paint-time path/brush is scoped. The control owns one theme subscription and one framework-created theme font while theme typography remains authoritative. Reassigning `Font` transfers font choice to the caller without transferring disposal ownership; Badge disposal releases only its own font/subscription and owns no timer or retained GDI path/bitmap cache.

For Alert, paint-time paths/brushes/pens are scoped; it owns one theme subscription, one framework-created Body font while theme typography is authoritative, and exactly one native dismiss Button. Caller-assigned fonts, icon descriptors, and icon renderers remain caller-owned. Disposal releases only Alert-owned resources/handlers and normal child-control ownership; there is no retained bitmap/path cache, timer, animation object, overlay host, or queue manager.

For Tooltip, the wrapper owns exactly one native ToolTip and its Popup/Draw event handlers. An externally supplied `IContainer` owns only the wrapper component. Repeated `Dispose()` must release the native ToolTip once, and popup/draw resources (`Font`, `GraphicsPath`, brushes, pens) remain event-scoped. Tooltip owns no static theme subscription, retained bitmap/path cache, framework timer, custom window, overlay, or queue manager.

For TabControl, all paint-time paths/brushes/pens are scoped and native page/image objects remain application/WinForms-owned. The control owns one static theme subscription, one optional framework-created theme font, and per-page presentation event handlers for pages currently in the collection. Removal/disposal must detach those handlers; caller-owned fonts and ImageList content remain caller-owned. Repeated create/theme-switch/dispose cycles must return the static theme-handler count to baseline and no timer/cache/window may be retained.

For NumericBox, all paint-time paths/brushes/pens are scoped. The wrapper owns exactly one native `NumericUpDown`, one static theme subscription, and one optional framework-created Body font while theme typography is authoritative. Disposal detaches native editor events and the theme handler, releases only the framework-owned font, and retains no timer, parser, bitmap/path cache, popup, or alternate value state.

For ComboBox, all item/shell paint-time paths/brushes/pens remain scoped. The native `ComboBox` owns its items, data binding, editable child, arrow and popup windows; the framework owns one static theme subscription and one optional framework-created Body font. Disposal removes the theme handler and releases only framework-owned resources. Handle recreation must not add subscriptions or mirror/copy native data. No retained per-item bitmap/path cache, timer, custom popup host, or alternate selection state is permitted.

For Dropdown, the component owns one native `ToolStripDropDownMenu`, one internal renderer, its target/native/theme subscriptions, short-lived native item snapshots, and only the icon bitmaps generated for those snapshots. Rebuild/theme refresh/disposal must dispose previous generated images and native rows before replacing them. The caller retains ownership of the target button, public item models, icon descriptors, and target `IconRenderer`. Repeated target replacement, open/close, Light/Dark switching, item mutation, and component disposal must not retain stale handlers/images or multiply events. Renderer paint brushes/pens remain scoped and no timer, custom window, or second command cache is retained.

## 8. DataGridView tests

Use realistic scenarios:

- Empty grid
- Small bound list
- Large row count
- Alternating rows
- Selection changes
- Column resize/reorder
- Runtime theme switch
- Loading overlay

Avoid tests that replace normal DataGridView behavior with framework-specific assumptions.

Pagination integration tests/demos must keep this same boundary: Pagination does not receive a `DataSource` or DataGrid reference. Application/demo code listens to `PageChanged`, slices or queries the appropriate page, and binds that result to the grid.

## 9. Designer checks

For Designer-oriented controls verify in Visual Studio:

- Toolbox/instantiation works.
- Parameterless construction does not throw.
- Common properties serialize and reopen correctly.
- Theme defaults render without application startup code.
- Opening a form containing the control does not run animation indefinitely in the Designer.

For Phase 7, place Group and Toolbar in the Designer, add Buttons/Groups through their normal WinForms `Controls` collections, serialize `Orientation`, `SelectionMode`, `EqualWidth`, `BorderRadius`, `GroupSpacing`, and `Alignment`, then reopen the form and confirm connected layout is restored without application bootstrap code.

For Phase 8, place TextBox and Card in the Designer without theme bootstrap code. Serialize placeholder/validation/icons/clear/read-only/password/radius settings as applicable. Add controls into `Header`, `Body`, and `Footer`, toggle Header/Footer visibility, set custom Card padding/border/shadow/radius, save and reopen the form, and confirm the region contents and public property values are preserved.

For Pagination, place the control in the Designer without theme bootstrap code. Verify `TotalItems`, `PageSize`, `CurrentPage`, `MaxVisiblePages`, `ShowFirstLast`, `ShowPreviousNext`, `ButtonSize`, `Variant`, and `BorderRadius` serialize/reopen correctly; `TotalPages` remains read-only/non-serialized; no runtime timer or application data dependency is created by design-time instantiation.

For Badge, place the control in the Designer without theme bootstrap code. Verify `Text`, `Variant`, `CustomColor`, `Pill`, and `BorderRadius` serialize/reopen as normal WinForms properties, AutoSize remains usable, and construction does not require DI, application startup, or a running timer.

For Alert, place the control in the Designer without theme bootstrap code. Verify `Text`, `Variant`, `Icon`, `Dismissible`, and `BorderRadius` serialize/reopen as normal WinForms properties, `IconRenderer` stays runtime-only/non-serialized, the private close Button is not exposed as a public composition surface, and construction requires no DI, application startup, timer, or popup host.

For Tooltip, place `BootstrapTooltip` in the Designer component tray without theme bootstrap code. Verify the extender `ToolTip` value plus `Variant`, `CustomColor`, `BorderRadius`, `ContentPadding`, `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` serialize/reopen correctly. Confirm only the wrapper is container-owned, the private native ToolTip is not separately serialized/exposed, and opening the form requires no application startup, custom timer, or popup host.

For TabControl, place `BootstrapTabControl` in the Designer without theme bootstrap code, add ordinary native `TabPage` children through the existing TabPages collection editor, and verify `TabStyle`, `Variant`, `Fill`, and `BorderRadius` serialize/reopen normally. Confirm native page controls/content, ImageList associations, tooltip text, and selection are preserved and no framework page wrapper or custom host appears in the Designer.

For NumericBox, place `BootstrapNumericBox` in the Designer without theme bootstrap code. Verify `Minimum = -100`, `Maximum = 1000`, `Increment = 0.25`, `DecimalPlaces = 2`, `ThousandsSeparator = true`, `ReadOnly = true`, `ValidationState = Valid`, and `BorderRadius = 6` serialize/reopen as wrapper properties. Confirm the private native `NumericUpDown` is not separately serialized/exposed and construction requires no DI, application startup, timer, popup, or parser service.

For ComboBox, place `BootstrapComboBox` in the Designer without theme bootstrap code. Verify inherited native `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `DropDownStyle`, and autocomplete properties remain normal native designer surfaces; `ValidationState`, `BorderRadius`, and `LeadingIcon` serialize/reopen normally; `IconRenderer` stays runtime-only/non-serialized. Confirm no framework item wrapper, popup component, private child replacement, DI/bootstrap requirement, timer, or extra selector appears.

For Dropdown, place `BootstrapDropdown` in the Designer component tray without theme bootstrap code. Verify `Target`, `Items`, `Variant`, and `MinimumWidth` serialize/reopen through the component model, including `Item` and `Separator` rows plus mutable text/icon/enabled/checked/tag values. Confirm the owned native `ToolStripDropDownMenu`, internal renderer, and generated image snapshot are not exposed or serialized as public child components, and construction introduces no DI requirement, timer, custom popup `Form`, or live binding service.

## 10. Build commands

Once the solution exists, the project should support explicit target verification similar to:

```powershell
dotnet build -c Release -f net48
dotnet build -c Release -f net8.0-windows
dotnet test -c Release
```

Exact solution/project paths are established in Phase 0 of `DEVELOPMENT_PLAN.md`.

The repository CI runs `build.ps1` followed by `test.ps1 -SkipBuild` on Windows, covering the complete `net48` and `net8.0-windows` matrix used by implemented phases.

Pagination, Badge, Alert, Tooltip, TabControl, NumericBox, ComboBox, and Dropdown participate in the Phase 16 public/protected API fingerprint gate. Each addition must first fail that gate, the reconstructed API must be reviewed, and only the intentionally changed fingerprint may then be approved. `AssemblyVersion` remains `1.0.0.0`.

## 11. Definition of done for a component

A component is complete only when:

- Both target builds succeed.
- Core logic has automated coverage where practical.
- Relevant control/STA behavior is tested.
- Demo coverage exists.
- Light/Dark behavior works.
- Keyboard/focus behavior is checked if interactive.
- DPI behavior is checked.
- Animation/lifecycle behavior is checked if animated.
- No obvious GDI/timer/event leak remains.
- Public API is documented.

## 12. BootstrapToast Stage 8 verification

Stage 8 adds targeted pure, STA, animation/lifetime, integration, and manual checks for `BootstrapToast` and `BootstrapToastContainer` while retaining the shared foundation test strategy.

Pure tests cover:

- Shared Alert/Toast feedback palette behavior under all semantic variants and Light/Dark themes without a second feedback color system.
- Toast title/body/icon/close layout metrics and preferred-size calculations across logical DPI 96/120/144/168/192, including narrow/malformed geometry containment.
- Four-corner stack layout (`TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`) with DPI-scaled spacing and stable insertion order.
- Validation of `ToastSpacing`, `MaximumVisibleToasts`, undefined placement/variant values, `AutoHideDelay`, and `AnimationDuration` before usable state is returned.

STA/control tests cover:

- Designer-safe Toast defaults/metadata, one private native dismiss button, source-neutral icon dispatch, caller-owned font/renderer preservation, and deterministic theme/font cleanup.
- Container defaults, all four placements, max-visible values, FIFO ownership/queue promotion, and `DismissAll()` snapshot semantics.
- The ownership boundary: a failed `ShowToast` leaves ownership with the caller; a successful call transfers ownership to the container for both visible and queued Toasts; dismissal/container disposal disposes owned Toasts exactly once.
- Exactly-once `Dismissed` delivery for manual close, programmatic dismissal, auto-hide, rapid repeated dismissal, queued dismissal, and `DismissAll()`. For container-owned Toasts, logical `Dismissed` occurs before exit completion/removal/disposal.
- Queue promotion only after the exiting Toast completes, while queued Toasts remain hidden and consume neither enter animation nor auto-hide lifetime.
- Runtime Light/Dark changes repaint the same Toast instances without changing queue position, ownership, or restarting active lifetime/transition state.
- Demo action coverage for manual, auto-hide, icon/multiline, disabled, Burst 8, Dismiss All, placement cycling, rapid show/dismiss, and Stress 100 scenarios.

Deterministic animation/lifetime tests use the existing controllable animation clock/frame scheduler plus an internal auto-hide timer seam; they do not sleep on wall-clock time. Coverage includes:

- Enter start/midpoint/completion and `EaseOut` progression from the placement-specific offset.
- Dismissal during enter reversing from the current visual position without a jump.
- Exit completion as the only point that removes/disposes the Toast and promotes queued work.
- At most one transition animation per Toast and one survivor-reflow animation per container, including rapid second-dismiss cancellation/stale callback suppression.
- Reduced motion completing enter/exit/reflow synchronously with no unnecessary frame scheduling.
- Auto-hide countdown starting only after enter completion, not at `ShowToast`, and never while queued.
- `AutoHide` / `AutoHideDelay` changes cancelling or restarting the currently eligible countdown.
- Stale timer ticks ignored after cancellation/restart/dismissal/disposal through generation guarding.
- Hidden-host pause/resume without charging hidden wall-clock time and disposal with no remaining useful scheduler/timer work.

Manual Feedback-page verification must cover both Light and Dark themes, normal and Reduced motion, manual and auto-hide Toasts, title/body/icon/multiline content, dismissible and disabled presentation, every placement, `MaximumVisibleToasts` values 1/2/5, FIFO queue promotion, dismiss-during-enter, `DismissAll()` while exits are active, host hide/show, resize/reflow, theme switching during auto-hide, and container disposal while active. Repeat the page at real Windows 100/125/150/175/200% scaling. Run repeated Stress 100 / Dismiss All cycles and observe process USER/GDI handles; unbounded growth is a failure.

Stage 8 participates in the Phase 16 public/protected API fingerprint gate. The gate must first fail against the prior approved hash, the reconstructed export must be reviewed for only `BootstrapToastPlacement`, `BootstrapToast`, and `BootstrapToastContainer`, and only then may the fingerprint be updated. Palette/layout/timer/ownership/animation test seams remain internal/private and `AssemblyVersion` remains `1.0.0.0`.

Both `net48` and `net8.0-windows` must pass the focused Toast/Feedback/demo suite, shared Animation/Alert regressions, and the complete test suite before Stage 8 is considered complete.

## 13. Overlay placement and Popover verification

Pure tests cover all explicit placements, deterministic Auto selection, exact-opposite Flip, cross-axis Shift, FlipAndShift ordering, RTL Start/End, padded boundaries, oversized popups, negative coordinates, saturation, and invalid values on both TFMs.

STA tests cover Tooltip native-default compatibility and managed properties; surface/host ownership; Popover Target/Content/trigger/focus/open-close behavior; anchor/form/scroll tracking; transient theme subscriptions; external disposal; and repeated cleanup. Independent `GetWindowRect` checks verify that overlay `ShowAt`/`MoveTo`, Popover, and managed Tooltip HWNDs match the placement-engine rectangle at working-area edges, including intentional `None` overflow and `Flip` without cross-axis shift. Tooltip remains text-only with one native `ToolTip`; Popover content remains caller-owned.

Tooltip race tests invalidate a native Popup request through mouse leave/down, visibility loss, target disposal, Native-mode switching, and component teardown before draining the message queue. They assert that no second show/draw occurs, no managed request survives, and the native tooltip window does not reappear. Popover tests cover focusable root content, native nested tab order with ineligible controls skipped, and `Closed` reentrancy that replaces a disposing Target or Content without losing the replacement or transferring caller ownership.

Manual Feedback verification covers native-versus-managed Tooltip timing and edge placement, interactive Popover keyboard/content behavior, Escape/outside-click focus, movement without Opened/Closed churn, Light/Dark reflow, real Windows 96/120/144/168/192 DPI, mixed-DPI and negative-coordinate monitors, and at least 500 combined popup cycles while observing USER/GDI handles and event subscriptions.

