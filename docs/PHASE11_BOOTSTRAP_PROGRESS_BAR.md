# Phase 11 — BootstrapProgressBar

Phase 11 introduces a native Bootstrap-inspired progress control for determinate and indeterminate work. The implementation reuses the framework Theme, Rendering, DPI, `BootstrapAnimation`, and `BootstrapLoopAnimation` foundations; it does not introduce a Progress-owned timer or a second animation scheduler.

## Final public contract

```text
BootstrapProgressBar.Minimum
BootstrapProgressBar.Maximum
BootstrapProgressBar.Value
BootstrapProgressBar.Percentage
BootstrapProgressBar.Variant
BootstrapProgressBar.CustomColor
BootstrapProgressBar.BorderRadius
BootstrapProgressBar.ShowText
BootstrapProgressBar.TextFormat
BootstrapProgressBar.Striped
BootstrapProgressBar.Animated
BootstrapProgressBar.AnimationDuration
BootstrapProgressBar.Indeterminate
BootstrapProgressBar.AnimateTo(int value)
```

Defaults:

```text
Minimum = 0
Maximum = 100
Value = 0
Variant = Primary
CustomColor = Color.Empty
BorderRadius = -1
ShowText = false
TextFormat = "{0}%"
Striped = false
Animated = false
AnimationDuration = 600 ms
Indeterminate = false
```

The control is non-focusable by default and exposes `AccessibleRole.ProgressBar`. Its accessibility value reports the current determinate percentage (for example, `50%`) and reports `Indeterminate` while indeterminate mode is active. Value/state changes notify accessibility clients through the WinForms value-change event.

## Range and percentage behavior

`Minimum` must remain strictly less than `Maximum`. Assigning an invalid endpoint throws `ArgumentOutOfRangeException`.

Changing a valid endpoint keeps the control state valid by moving the existing `Value` into the new range when necessary. Direct assignment to `Value`, and targets supplied to `AnimateTo`, must already be inside the current range and are rejected otherwise.

`Percentage` is the current value normalized to the configured range and rounded to the nearest whole percent. Range and interpolation arithmetic are performed without 32-bit subtraction overflow, so the complete `int` range remains valid for both direct percentage calculation and `AnimateTo` transitions.

## Color, track, radius, and DPI

The track uses the current theme `SurfaceSecondary` color. The fill uses the current semantic `Variant` through the shared variant-color resolver unless `CustomColor` is non-empty, in which case the custom color wins.

`BorderRadius = -1` uses the current theme radius. Any non-negative value is treated as a logical-pixel uniform radius and is scaled through `DpiScaler`. Values below `-1` are rejected.

Painting uses the shared `RoundedPath` / `CornerRadius` geometry and double-buffered WinForms custom painting. Temporary GDI objects are scoped to the paint operation.

## Text format

`ShowText = true` displays determinate progress text. `TextFormat` is a normal composite-format string with these arguments:

```text
{0} = Percentage (0..100)
{1} = Value
{2} = Minimum
{3} = Maximum
```

For example:

```text
{1} / {3} ({0}%)
```

`TextFormat` cannot be null. Invalid composite format strings are rejected when assigned. Text over the filled region uses a contrasting semantic foreground while the remaining region uses the normal theme text color.

Indeterminate mode does not render percentage text because the visual segment does not represent a measurable completion fraction.

## Stripes and animated stripes

`Striped = true` adds diagonal stripes inside the current fill. Static stripes require no animation.

`Striped = true` plus `Animated = true` uses `BootstrapLoopAnimation` for stripe motion. The control creates no WinForms timer of its own. Stripe painting reuses one four-point polygon buffer per paint operation instead of allocating a new point array for every stripe/frame.

With Reduced motion enabled, shared loop animation does not continuously schedule frames, leaving a stable striped presentation.

## AnimateTo

`AnimateTo(int value)` smoothly transitions the displayed/logical value from the current value to the requested target using `BootstrapAnimation` with `EaseInOut` easing and the configured `AnimationDuration`.

A new `AnimateTo` request replaces the current finite transition and starts from the value already reached visually, so rapid progress updates do not reset through an unrelated value.

When Reduced motion is enabled, the shared finite animation publishes its final state immediately. Before a runtime control handle exists, and while `Indeterminate = true`, `AnimateTo` updates the logical value immediately instead of scheduling a finite visual transition.

Changing the range cancels a pending value transition so the invariant between range and value remains deterministic.

## Indeterminate mode

`Indeterminate = true` replaces the determinate fill with a moving segment driven by `BootstrapLoopAnimation`. The segment uses the same semantic/custom fill color and rounded track clipping as determinate progress.

With Reduced motion enabled, indeterminate mode remains visible at a stable representative frame rather than disappearing or continuously moving.

Switching into indeterminate mode cancels a pending finite `AnimateTo` transition. The stored `Value` remains valid logical state and becomes visible again when determinate mode is restored.

## Lifecycle and theme behavior

Both finite and loop animations receive the progress control as their lifecycle owner. Shared animation infrastructure therefore handles hide/show pause-resume and owner disposal semantics.

The control subscribes to `BootstrapThemeManager.ThemeChanged` and unsubscribes during disposal. Runtime theme changes repaint semantic colors immediately. A Reduced motion change recreates/restarts the relevant shared animation so the new preference applies to active progress without a separate timing engine.

Destroying the WinForms handle releases active Progress-owned animation objects. Designer construction does not require application bootstrap and does not start continuous runtime animation.

## Automated coverage

Phase 11 tests cover:

- required public API presence;
- defaults, accessibility role, determinate accessibility value, and indeterminate accessibility state;
- custom Min/Max/Value percentage calculation;
- full signed `int` range without overflow in percentage and interpolation logic;
- endpoint validation and value normalization after valid range changes;
- direct Value and `AnimateTo` target validation;
- radius, variant, duration, and text-format validation;
- Reduced motion completion for `AnimateTo`;
- logical `AnimateTo` behavior in indeterminate mode;
- theme track painting and CustomColor precedence;
- demo coverage for every semantic variant, custom color, text formatting, square radius, static stripes, animated stripes, indeterminate mode, and interactive `AnimateTo` commands;
- demo content/background theme separation;
- main-demo navigation to the Progress page.

## Demo and manual verification

Launch the demo and choose **Progress**.

Verify the following under both Light and Dark themes:

1. All semantic variants render against the themed track.
2. The custom-color example overrides its semantic variant.
3. The formatted example shows value/maximum/percentage and remains legible across the filled/unfilled boundary.
4. Static stripes remain still.
5. Animated stripes move smoothly when Reduced motion is disabled.
6. The indeterminate segment moves continuously when Reduced motion is disabled.
7. Enable Reduced motion: animated stripes become stable, indeterminate remains visibly stable, and `25%`, `75%`, `Complete`, and `Reset` transitions reach their targets immediately.
8. Disable Reduced motion and repeatedly activate `25%`, `75%`, `Complete`, and `Reset` during active transitions; progress should continue from the currently reached value without invalid jumps.
9. Resize the form repeatedly and confirm track/fill corners and text do not leave stale pixels.
10. Repeat at Windows display scaling 100%, 125%, 150%, 175%, and 200%; radii and stripes should scale while text remains usable.
11. Hide/show the demo while animated progress is active and confirm the shared owner lifecycle pauses/resumes without counting hidden wall-clock time.
12. Close the demo while animations are active and confirm no Progress-owned animation/timer work survives disposal.
