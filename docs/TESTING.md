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

## 6. Animation matrix

For finite and loop animation, test:

- Start
- Stop
- Restart
- Dispose
- Hide/show
- Reduced motion
- Rapid repeated toggles
- Reverse direction during an active transition
- Final value/state after completion

Animated controls must not continue producing useful work after disposal.

## 7. Resource/lifecycle checks

Use targeted stress/manual tests for repeated creation and disposal of animated/custom-painted controls.

Watch for growth in:

- GDI handles
- USER handles
- Active timers
- Event subscriptions retaining disposed controls
- Cached bitmaps/fonts/paths

Exact zero-allocation rendering is not required. Unbounded growth is unacceptable.

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

## 10. Build commands

Once the solution exists, the project should support explicit target verification similar to:

```powershell
dotnet build -c Release -f net48
dotnet build -c Release -f net8.0-windows
dotnet test -c Release
```

Exact solution/project paths are established in Phase 0 of `DEVELOPMENT_PLAN.md`.

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
