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
- Icon descriptor/source selection
- Selection-state algorithms

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

The Phase 2 Rendering/DPI demo covers the geometry calculations at all five scale factors. Run the demo under each corresponding Windows display-scaling setting as components are added and during hardening; do not treat the virtual matrix as proof of OS-level DPI behavior.

For Phase 8 specifically, verify the native TextBox caret/text baseline remains centered and usable after DPI changes, leading/trailing/clear slots remain inside the rounded input, and Card theme-default padding scales without overwriting application-set custom padding.

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

## 9. Designer checks

For Designer-oriented controls verify in Visual Studio:

- Toolbox/instantiation works.
- Parameterless construction does not throw.
- Common properties serialize and reopen correctly.
- Theme defaults render without application startup code.
- Opening a form containing the control does not run animation indefinitely in the Designer.

For Phase 7, place Group and Toolbar in the Designer, add Buttons/Groups through their normal WinForms `Controls` collections, serialize `Orientation`, `SelectionMode`, `EqualWidth`, `BorderRadius`, `GroupSpacing`, and `Alignment`, then reopen the form and confirm connected layout is restored without application bootstrap code.

For Phase 8, place TextBox and Card in the Designer without theme bootstrap code. Serialize placeholder/validation/icons/clear/read-only/password/radius settings as applicable. Add controls into `Header`, `Body`, and `Footer`, toggle Header/Footer visibility, set custom Card padding/border/shadow/radius, save and reopen the form, and confirm the region contents and public property values are preserved.

## 10. Build commands

Once the solution exists, the project should support explicit target verification similar to:

```powershell
dotnet build -c Release -f net48
dotnet build -c Release -f net8.0-windows
dotnet test -c Release
```

Exact solution/project paths are established in Phase 0 of `DEVELOPMENT_PLAN.md`.

The repository CI runs `build.ps1` followed by `test.ps1 -SkipBuild` on Windows, covering the complete `net48` and `net8.0-windows` matrix used by implemented phases.

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
