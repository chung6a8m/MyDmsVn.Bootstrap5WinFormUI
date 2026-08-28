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

The Phase 2 Rendering/DPI demo covers the geometry calculations at all five scale factors. Run the demo under each corresponding Windows display-scaling setting as components are added and during hardening; do not treat the virtual matrix as proof of OS-level DPI behavior.

For Phase 8 specifically, verify the native TextBox caret/text baseline remains centered and usable after DPI changes, leading/trailing/clear slots remain inside the rounded input, and Card theme-default padding scales without overwriting application-set custom padding.

Pagination does not own a DPI-specific renderer; its real-Windows DPI check verifies that composed ButtonGroup/Button preferred sizes, connected seams, accessible navigation, and wrapping-free AutoSize behavior remain correct together.

Badge pure tests cover logical scaling through 96/120/144/168/192 DPI. The Feedback page remains the manual OS-level DPI gate because `DeviceDpi`, text rendering, and physical display scaling are WinForms/Windows behaviors rather than pure arithmetic alone.

Alert pure tests cover the same 96/120/144/168/192 logical matrix for padding, icon/close reservations, border/focus widths, and radius scaling. The shared Feedback page remains the Alert OS-level gate for `DeviceDpi`, wrapped text, native close-button focus/activation, and physical rounded-border rendering.

Tooltip pure tests cover 96/120/144/168/192 logical scaling for content padding, border width, and theme/explicit radius. The shared Feedback page remains the Tooltip OS-level gate because associated-control `DeviceDpi`, native ToolTip placement, monitor-edge behavior, owner-drawn text rendering, and physical rounded-border output require a real Windows popup.

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

## 10. Build commands

Once the solution exists, the project should support explicit target verification similar to:

```powershell
dotnet build -c Release -f net48
dotnet build -c Release -f net8.0-windows
dotnet test -c Release
```

Exact solution/project paths are established in Phase 0 of `DEVELOPMENT_PLAN.md`.

The repository CI runs `build.ps1` followed by `test.ps1 -SkipBuild` on Windows, covering the complete `net48` and `net8.0-windows` matrix used by implemented phases.

Pagination, Badge, Alert, and Tooltip participate in the Phase 16 public/protected API fingerprint gate. Each addition must first fail that gate, the reconstructed API must be reviewed, and only the intentionally changed fingerprint may then be approved. `AssemblyVersion` remains `1.0.0.0`.

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
