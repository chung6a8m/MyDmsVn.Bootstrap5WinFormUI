# BootstrapPlaceholder / Skeleton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap-inspired `BootstrapPlaceholder` primitive for loading placeholders, including Bootstrap-compatible ExtraSmall/Small/Default/Large intrinsic heights, semantic colors, optional square/rounded rendering, static/Glow/Wave presentation, reduced-motion behavior, and a documented Skeleton composition pattern built from multiple placeholders without introducing a second loading-state framework.

**Architecture:** `BootstrapPlaceholder : Control` is a lightweight, owner-painted, non-focusable primitive. It owns only presentation: preferred size, theme/font/DPI adaptation, rounded fill rendering, and animation progress; application code owns loading state, async work, content swapping, layout, and accessibility status. `Glow` and `Wave` reuse the existing `BootstrapLoopAnimation` owner lifecycle, while a pure internal render helper contains all size/color/opacity/wave/radius calculations so most behavior is deterministic without a live WinForms handle. “Skeleton” is a composition pattern made from several `BootstrapPlaceholder` instances in normal WinForms layout containers; V1 deliberately does not add a redundant `BootstrapSkeleton` wrapper.

**Tech Stack:** C#, native Windows Forms, `System.Drawing`, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariantColorResolver`, `DpiScaler`, `CornerRadius`/`RoundedPath`, `BootstrapLoopAnimation`, NUnit 4, integrated demo application. No external dependency and no new timer/scheduler infrastructure.

**Spec:** User request plus Bootstrap 5.3 Placeholder behavior (`https://getbootstrap.com/docs/5.3/components/placeholders/`) and Bootstrap 5.3.8 source (`https://github.com/twbs/bootstrap/blob/v5.3.8/scss/_placeholders.scss`). This plan's **Placeholder/Skeleton contract** is the WinForms adaptation baseline. Project-wide constraints come from `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/DESIGN_SYSTEM.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, `docs/COMPONENTS.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; all new public Placeholder types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared code path wherever practical.
- V1 exports exactly one new control primitive, `BootstrapPlaceholder`, plus `BootstrapPlaceholderSize` and `BootstrapPlaceholderAnimation`. Do **not** export `BootstrapSkeleton`, `SkeletonItem`, a skeleton collection, a skeleton template engine, or aliases for the same concepts.
- A skeleton is normal WinForms composition: several placeholders arranged with `TableLayoutPanel`, `FlowLayoutPanel`, `Panel`, `Dock`, `Anchor`, explicit `Size`, and visibility controlled by application code.
- Placeholder never starts, awaits, cancels, retries, polls, or binds to business work. Do not add `Loading`, `IsLoaded`, `Task`, `DataSource`, `Content`, `TargetControl`, async callbacks, cancellation tokens, completion events, or automatic skeleton/content swapping.
- Reuse Bootstrap terminology: sizes are ExtraSmall/Small/Default/Large; animations are None/Glow/Wave; default animation is None; default animation cycle is exactly 2 seconds.
- Match Bootstrap 5.3 source constants: normal/max placeholder opacity is `0.5`; minimum animation opacity is `0.2`; ExtraSmall/Small/Default/Large intrinsic heights are `0.6em`, `0.8em`, `1.0em`, and `1.2em` respectively.
- Width is not represented as a Bootstrap grid/percentage property. With `AutoSize=true`, the primitive has a deterministic 100-logical-pixel preferred width; applications that need 25%/50%/75%/100% skeleton lines use normal WinForms layout constraints or set `AutoSize=false` and own bounds.
- `PlaceholderSize` affects intrinsic/preferred height. Explicit bounds remain authoritative when `AutoSize=false`; the control must not fight caller-owned width/height on every paint or theme tick.
- `Variant` defaults to `BootstrapVariant.Secondary`; `CustomColor = Color.Empty` resolves through the existing semantic variant resolver. A non-empty custom color must be fully opaque; alpha is owned by the placeholder opacity/animation model.
- `Enabled=false` uses `theme.Colors.Disabled` as the base color before placeholder opacity is applied. It does not start/stop loading or alter application state.
- `BorderRadius = 0` is the Bootstrap-compatible square default. `BorderRadius = -1` uses `theme.Metrics.Radius`; non-negative values are explicit logical pixels; values below `-1` are rejected before mutation.
- Use current theme Body typography while the control still owns its font. If the caller explicitly assigns `Font`, stop replacing that font on later theme changes and never dispose the caller-owned font.
- All logical width/radius metrics scale through `DpiScaler`; the em-based height uses `Font.Height`, which is already device-scaled by WinForms.
- Animation is driven exclusively by `BootstrapLoopAnimation`. Do not add `System.Windows.Forms.Timer`, `System.Threading.Timer`, `Task.Delay`, a background thread, `Application.Idle`, or a second frame scheduler.
- `Animation=None` owns no live loop animation. `Glow` and `Wave` automatically animate only after a runtime handle exists; changing back to None releases loop resources.
- Runtime theme changes repaint semantic colors and, when animation is active, recreate the loop so a changed `ReducedMotion` preference takes effect immediately.
- With reduced motion enabled, Glow/Wave remain on a stable, visible static frame and continuously scheduled animation frames must stop. Do not silently change the public `Animation` property to None.
- Placeholder is decorative presentation, not a progress/status control. Set it non-selectable and non-focusable; do not invent keyboard, mouse activation, selection, drag/drop, tooltip, or context-menu behavior.
- Placeholder instances must not become repetitive screen-reader announcements. V1 does not create a custom accessibility tree or live region; documentation/demo must tell applications to expose loading status separately through meaningful native text/status UI.
- Bootstrap's web `cursor: wait` maps to the placeholder's default `Cursor = Cursors.WaitCursor`; callers may override normal inherited `Cursor` if desired.
- Rendering must be flicker-resistant (`UserPaint`, `AllPaintingInWmPaint`, `OptimizedDoubleBuffer`, `ResizeRedraw`, transparent-background support) and must dispose every per-paint brush/path resource.
- Reuse `RoundedPath.Create` and `CornerRadius`; do not duplicate rounded-rectangle path construction.
- Wave rendering must not allocate an off-screen bitmap per frame. Use one `LinearGradientBrush` with a fixed small `ColorBlend` sample set and the current normalized loop progress.
- Designer construction must be parameterless and must not require application bootstrap, an existing handle, or external services. Animation must not run in the WinForms designer.
- Theme subscription must be paired with deterministic unsubscribe in `Dispose(bool)` and remain safe across handle recreation.
- Tests that instantiate controls or create handles run STA and do not run in parallel when mutating global theme state.
- Every focused raw `dotnet test` command that can create WinForms handles must include `--blame-hang --blame-hang-timeout 5m`; the full suite should run through `./test.ps1`.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5.3.8 defines `.placeholder` as an inline block with minimum height `1em`, `background-color: currentcolor`, and opacity `0.5`. Size modifiers use `0.6em`, `0.8em`, and `1.2em`. `placeholder-glow` animates opacity over a 2-second ease-in-out loop down to `0.2`, while `placeholder-wave` moves a 130-degree gradient mask through the placeholder over a 2-second linear loop. Bootstrap leaves actual loading state and the swap from skeleton to real content to application code.

The WinForms adaptation is:

```text
Bootstrap web                     WinForms adaptation
-------------------------------   -----------------------------------------------
.placeholder                      BootstrapPlaceholder
.placeholder-xs                   PlaceholderSize = ExtraSmall
.placeholder-sm                   PlaceholderSize = Small
(default 1em)                     PlaceholderSize = Default
.placeholder-lg                   PlaceholderSize = Large
(no animation class)              Animation = None
.placeholder-glow                 Animation = Glow
.placeholder-wave                 Animation = Wave
currentColor                      Variant / CustomColor theme resolution
width utility / grid column       AutoSize=false + Size/Dock/Anchor/layout panel
several placeholder elements      several BootstrapPlaceholder controls
application swaps real content    application toggles skeleton/content visibility
```

Core rules:

1. A default placeholder is visible, static, square-cornered, Secondary, and rendered at 50% opacity.
2. `Animation=None` never creates/schedules a loop animation.
3. Glow starts at 50% opacity, reaches 20% at half-cycle, and returns to 50% at cycle end.
4. Wave keeps the 50% base opacity and moves one lower-opacity band across the control; reduced motion at progress zero is a stable uniform frame.
5. Animation never changes `Size`, `PlaceholderSize`, `Variant`, `CustomColor`, `BorderRadius`, `Enabled`, or application loading state.
6. Placeholder contains no user text and exposes no custom click/keyboard action.
7. `AutoSize=true` uses an intrinsic 100-logical-pixel width and em-derived height. `AutoSize=false` lets caller-owned `Size`/layout fully determine the painted rectangle.
8. A 40x40 placeholder with a very large explicit radius is normalized by `CornerRadius.NormalizeTo` and can be used as a circular avatar skeleton without a public `Shape` enum.
9. A button-shaped skeleton is just a normally sized placeholder with a radius; it is not a disabled `BootstrapButton` and cannot be activated.
10. A card/list skeleton is a layout composition, not a new public model.

Useful references:

- Bootstrap 5.3 Placeholders: `https://getbootstrap.com/docs/5.3/components/placeholders/`
- Bootstrap 5.3.8 placeholder SCSS: `https://github.com/twbs/bootstrap/blob/v5.3.8/scss/_placeholders.scss`
- Bootstrap 5.3.8 placeholder variables: `$placeholder-opacity-max: .5`, `$placeholder-opacity-min: .2`
- Existing animation infrastructure: `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapLoopAnimation.cs`
- Existing semantic color resolver: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapVariantColorResolver.cs`
- Existing rounded geometry: `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/CornerRadius.cs`, `RoundedPath.cs`

---

## Placeholder / Skeleton Contract

### Public enums

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapPlaceholderSize
{
    ExtraSmall,
    Small,
    Default,
    Large
}

public enum BootstrapPlaceholderAnimation
{
    None,
    Glow,
    Wave
}
```

Enum validation must reject undefined values before mutating control state.

### Public control

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Animation))]
public class BootstrapPlaceholder : Control
{
    public BootstrapPlaceholderSize PlaceholderSize { get; set; }
    public BootstrapPlaceholderAnimation Animation { get; set; }
    public BootstrapVariant Variant { get; set; }
    public Color CustomColor { get; set; }
    public int BorderRadius { get; set; }
    public TimeSpan AnimationDuration { get; set; }

    public override Size GetPreferredSize(Size proposedSize);
}
```

Default state:

```text
PlaceholderSize     = Default
Animation           = None
Variant             = Secondary
CustomColor         = Color.Empty
BorderRadius        = 0
AnimationDuration   = 00:00:02
AutoSize            = true
BackColor           = Transparent
TabStop             = false
AccessibleRole      = None
Cursor              = Cursors.WaitCursor
preferred width     = 100 logical px at 96 DPI
preferred height    = Font.Height * 1.0
```

Validation/notification rules:

- `PlaceholderSize` accepts only the four defined enum values. Same-value assignment is a no-op. Effective change recomputes preferred size and invalidates.
- `Animation` accepts only None/Glow/Wave. Same-value assignment is a no-op. Effective change reconciles loop ownership and invalidates.
- `Variant` accepts only existing `BootstrapVariant` values. Same-value assignment is a no-op and no new variant enum is introduced.
- `CustomColor` accepts `Color.Empty` or a fully opaque color (`A == 255`). Semi-transparent colors throw `ArgumentException` before mutation because placeholder opacity is applied separately.
- `BorderRadius` accepts `-1` or any non-negative logical-pixel value. Values below `-1` throw `ArgumentOutOfRangeException` before mutation.
- `AnimationDuration` must be greater than zero. Non-positive values throw `ArgumentOutOfRangeException` before mutation. Effective change recreates an active Glow/Wave loop without changing the selected animation kind.
- The control exports no new public events and no `Start()`, `Stop()`, `Restart()`, `Loading`, `Progress`, `WaveAngle`, `Opacity`, `Shape`, or percentage-width API in V1.

### Intrinsic sizing and explicit bounds

Use these exact size multipliers:

```text
ExtraSmall = 0.6
Small      = 0.8
Default    = 1.0
Large      = 1.2
```

`GetPreferredSize` resolves:

```text
baseWidth = DpiScaler.Scale(100, dpi)
width     = proposedSize.Width > 0
              ? min(baseWidth, proposedSize.Width)
              : baseWidth
height    = max(1, ceil(Font.Height * sizeMultiplier))
```

Rules:

- `Font.Height` is already device-scaled and must not be passed through `DpiScaler` again.
- When `AutoSize=true`, constructor, size change, font change, DPI change, and relevant theme-font change call one private `ApplyPreferredSize()` that assigns `Size = GetPreferredSize(Size.Empty)` only when the control owns AutoSize sizing.
- When `AutoSize=false`, those events call `Invalidate()`/layout notification as needed but do not overwrite caller-owned `Size`.
- `Padding` does not enlarge the placeholder fill in V1; the control is one painted primitive. Host layout owns spacing through `Margin`/container padding.
- Width percentages are achieved in a `TableLayoutPanel` percent column or through caller-owned explicit bounds, not through a `WidthPercent` property.

### Theme and font ownership

Follow the established theme-owned font pattern used by controls such as `BootstrapBadge`:

```text
initial font token = BootstrapThemeManager.CurrentTheme.Typography.Body
_useThemeFont      = true
```

- Constructor creates one Font from the current Body token and applies it under a private `_settingThemeFont` guard.
- `OnFontChanged` treats an unguarded caller assignment as ownership transfer: `_useThemeFont=false`, dispose any framework-owned previous font, then recompute preferred size.
- Theme change replaces/disposes only framework-owned fonts while `_useThemeFont=true`.
- Value-equal WinForms Font assignment must preserve correct ownership exactly as existing themed controls do; never dispose the Font instance still referenced by the control.
- Disposal releases only framework-created fonts.

### Base color and opacity

Base-color resolution is:

```text
if Enabled == false:
    baseColor = theme.Colors.Disabled
else if CustomColor != Color.Empty:
    baseColor = CustomColor
else:
    baseColor = BootstrapVariantColorResolver.Resolve(theme.Colors, Variant)
```

Then apply placeholder opacity to alpha; RGB is unchanged.

```text
OpacityMax = 0.5
OpacityMin = 0.2
```

Do not mutate theme/custom `Color` objects or add opacity into `CustomColor` semantics.

### Glow formula

Use a deterministic periodic function over normalized progress `p` in `[0,1]`:

```csharp
var wave = 0.5 + (0.5 * Math.Cos(2.0 * Math.PI * p));
var opacity = OpacityMin + ((OpacityMax - OpacityMin) * wave);
```

Therefore:

```text
p = 0.00 -> 0.50
p = 0.25 -> 0.35
p = 0.50 -> 0.20
p = 0.75 -> 0.35
p = 1.00 -> 0.50
```

The `BootstrapLoopAnimation` itself uses linear easing for Placeholder; the Glow formula supplies the smooth cycle shape and must not be double-eased.

### Wave formula

Wave is represented as a moving low-opacity trough sampled across a 130-degree linear gradient. Normalize gradient position `x` and animation progress `p` to `[0,1]` and use:

```csharp
const double startCenter = -0.25;
const double travel = 1.50;
const double halfWidth = 0.20;

var center = startCenter + (travel * p);
var distance = Math.Abs(x - center);
if (distance >= halfWidth)
{
    return OpacityMax;
}

var t = distance / halfWidth;
var smooth = t * t * (3.0 - (2.0 * t));
return OpacityMin + ((OpacityMax - OpacityMin) * smooth);
```

Rules:

- Progress zero puts the trough completely outside the visible gradient, producing a stable 50%-opacity frame. This is the reduced-motion frame.
- The trough reaches the center around mid-cycle and exits beyond the opposite side by cycle end.
- Build one `ColorBlend` per Wave paint from exactly 9 positions: `0`, `.125`, `.25`, `.375`, `.5`, `.625`, `.75`, `.875`, `1`.
- Each sample color uses the base RGB and alpha returned by `GetWaveOpacity(position, progress)`.
- Paint with one `LinearGradientBrush` at `130f` degrees. Do not cache a `Brush` across theme/DPI/size changes and do not create a bitmap mask.

### Radius

Resolve radius in device pixels:

```text
logicalRadius = BorderRadius == -1
    ? theme.Metrics.Radius
    : BorderRadius
radius = DpiScaler.Scale(logicalRadius, dpi)
```

Use `new CornerRadius(radius)` and `RoundedPath.Create`. Oversized values are normalized by the existing `CornerRadius` logic; no separate Circle/Pill shape is needed.

---

## Internal Render Logic Contract

Create `BootstrapPlaceholderRenderLogic.cs` with these exact constants/methods:

```csharp
internal static class BootstrapPlaceholderRenderLogic
{
    internal const int DefaultLogicalWidth = 100;
    internal const double OpacityMax = 0.5;
    internal const double OpacityMin = 0.2;
    internal const float WaveAngleDegrees = 130f;

    internal static Size GetPreferredSize(
        int fontHeight,
        BootstrapPlaceholderSize size,
        int dpi,
        Size proposedSize);

    internal static Color ResolveBaseColor(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor,
        bool enabled);

    internal static double GetGlowOpacity(double progress);

    internal static double GetWaveOpacity(
        double position,
        double progress);

    internal static Color ApplyOpacity(
        Color color,
        double opacity);

    internal static float GetRadius(
        BootstrapThemeMetrics metrics,
        int borderRadius,
        int dpi);
}
```

Validation rules:

- `fontHeight <= 0` throws `ArgumentOutOfRangeException`.
- Undefined `BootstrapPlaceholderSize` throws `ArgumentOutOfRangeException`.
- `dpi <= 0` throws `ArgumentOutOfRangeException`.
- `colors == null`/`metrics == null` throws `ArgumentNullException`.
- `customColor` must be Empty or fully opaque; semi-transparent input throws `ArgumentException` even though the public property already validates it.
- Undefined `BootstrapVariant` is rejected through the shared resolver.
- Glow/Wave inputs reject NaN/infinity; finite progress/position outside `[0,1]` are clamped so painting stays robust against floating-point scheduler edge values.
- `ApplyOpacity` rejects NaN/infinity and clamps finite opacity to `[0,1]`; alpha uses `Convert.ToByte(Math.Round(255.0 * opacity, MidpointRounding.AwayFromZero))`.
- `borderRadius < -1` throws `ArgumentOutOfRangeException`.
- Helper methods must not query a live `Control`, handle, screen, theme singleton, timer, or graphics context.

---

## Animation Lifecycle Contract

`BootstrapPlaceholder` owns at most one `BootstrapLoopAnimation`:

```csharp
private BootstrapLoopAnimation? _animationLoop;
```

Reconciliation rules:

1. `Animation=None` => dispose `_animationLoop`, set it null, paint using `progress=0` and `OpacityMax`.
2. Glow/Wave + no runtime handle or design mode => do not create/start a loop; paint stable progress zero.
3. Glow/Wave + runtime handle => lazily create `new BootstrapLoopAnimation(AnimationDuration, BootstrapEasing.Linear, this)`, subscribe `ProgressChanged`, and start it.
4. `ProgressChanged` only calls `Invalidate()` when control is not disposed; it never changes public state or bounds.
5. Changing Glow <-> Wave reuses the same duration contract but may dispose/recreate the loop for simple deterministic lifecycle; no progress-preservation guarantee is public.
6. Changing `AnimationDuration` while Glow/Wave is active recreates the loop and starts a new cycle at zero.
7. `OnHandleCreated` reconciles animation after base creation and preferred-size refresh.
8. `OnHandleDestroyed` stops active scheduling before calling base; handle recreation can start again from a stable fresh cycle.
9. Theme change recreates active animation so `BootstrapThemeManager.CurrentTheme.ReducedMotion` is reevaluated.
10. Owner visibility pause/resume and owner disposal remain responsibilities of `BootstrapLoopAnimation`; Placeholder must not duplicate those event subscriptions.
11. Control disposal unsubscribes `ProgressChanged`, disposes the loop, unsubscribes theme events, and releases framework-owned font resources exactly once.

No public `Animating`/`Start`/`Stop` API is needed: choosing None vs Glow/Wave is the Bootstrap-equivalent presentation switch. Application loading state remains separate.

---

## Painting Contract

`OnPaint` follows this order:

1. Return when client width/height is zero.
2. Resolve current theme, DPI, base color, radius, animation progress.
3. For None: use a solid brush with `ApplyOpacity(baseColor, OpacityMax)`.
4. For Glow: use a solid brush with `ApplyOpacity(baseColor, GetGlowOpacity(progress))`.
5. For Wave: create the 9-sample `ColorBlend`, create one 130-degree `LinearGradientBrush`, and assign `InterpolationColors`.
6. If radius is zero, fill `ClientRectangle` directly. If radius is positive, temporarily enable anti-aliasing, build one `RoundedPath`, fill it, then restore the previous smoothing mode.
7. Dispose every brush/path deterministically within the paint call.

Do not paint borders, text, icons, focus cues, selection, hover, press, error glyphs, shadows, or extra backgrounds in V1.

---

## Skeleton Composition Pattern

The documented composition pattern should look like normal WinForms code, for example:

```csharp
var skeleton = new TableLayoutPanel
{
    AutoSize = true,
    AutoSizeMode = AutoSizeMode.GrowAndShrink,
    ColumnCount = 2,
    RowCount = 4
};

var avatar = new BootstrapPlaceholder
{
    AutoSize = false,
    Size = new Size(40, 40),
    BorderRadius = 999,
    Animation = BootstrapPlaceholderAnimation.Wave
};

var title = new BootstrapPlaceholder
{
    AutoSize = false,
    Dock = DockStyle.Fill,
    Height = 18,
    Animation = BootstrapPlaceholderAnimation.Wave
};

var line1 = new BootstrapPlaceholder
{
    AutoSize = false,
    Dock = DockStyle.Fill,
    Height = 14,
    PlaceholderSize = BootstrapPlaceholderSize.Small,
    Animation = BootstrapPlaceholderAnimation.Wave
};
```

The final demo should use percent-sized `TableLayoutPanel` columns to show different line lengths. Do not copy this illustrative snippet blindly if the final demo layout needs different row/column counts; the invariant is that ordinary WinForms layout owns geometry and each placeholder remains independent.

Application-owned content swap example:

```csharp
private void SetLoaded(bool loaded)
{
    _skeletonPanel.Visible = !loaded;
    _contentPanel.Visible = loaded;
    _statusLabel.Text = loaded ? "Content loaded." : "Loading content…";
}
```

The status label is the meaningful accessibility/status surface; placeholders are decorative visuals.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderSize.cs` — public four-value size enum.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderAnimation.cs` — public None/Glow/Wave enum.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderRenderLogic.cs` — internal pure sizing/color/opacity/wave/radius calculations.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholder.cs` — public painted control, theme/font/DPI/animation lifecycle.

**Create test files**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderRenderLogicTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderContractTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/PlaceholderDemoFormTests.cs`

**Create demo file**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PlaceholderDemoForm.cs`

**Modify integration/docs**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- `docs/COMPONENTS.md`
- `docs/ARCHITECTURE.md`
- `docs/TESTING.md`
- `README.md`
- `docs/PACKAGE_README.md`
- `CHANGELOG.md`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- `docs/PUBLIC_API_BASELINE.md`

---

### Task 1: Freeze public Placeholder types, defaults, validation, and intrinsic sizing

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderSize.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderAnimation.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholder.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderTests.cs`

**Interfaces:**
- Produces exactly the two public enums and public control surface in **Placeholder / Skeleton Contract**.
- Control consumes current theme Body typography and `DpiScaler` for preferred width.
- Painting may be a minimal static fill at the end of this task; final formulas/lifecycle arrive in later tasks without changing public API.

- [ ] **Step 1: Write failing default-state and enum tests.** Assert enum numeric order exactly `ExtraSmall=0`, `Small=1`, `Default=2`, `Large=3` and `None=0`, `Glow=1`, `Wave=2`. Assert a new control has Default/None/Secondary/Empty/0/2s, `AutoSize=true`, transparent background, `TabStop=false`, `AccessibleRole.None`, and wait cursor.

```csharp
[Test]
public void DefaultsMatchPlaceholderContract()
{
    using var placeholder = new BootstrapPlaceholder();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(placeholder.PlaceholderSize, Is.EqualTo(BootstrapPlaceholderSize.Default));
        Assert.That(placeholder.Animation, Is.EqualTo(BootstrapPlaceholderAnimation.None));
        Assert.That(placeholder.Variant, Is.EqualTo(BootstrapVariant.Secondary));
        Assert.That(placeholder.CustomColor, Is.EqualTo(Color.Empty));
        Assert.That(placeholder.BorderRadius, Is.Zero);
        Assert.That(placeholder.AnimationDuration, Is.EqualTo(TimeSpan.FromSeconds(2)));
        Assert.That(placeholder.AutoSize, Is.True);
        Assert.That(placeholder.TabStop, Is.False);
        Assert.That(placeholder.AccessibleRole, Is.EqualTo(AccessibleRole.None));
        Assert.That(placeholder.Cursor, Is.EqualTo(Cursors.WaitCursor));
    }));
}
```

- [ ] **Step 2: Write failing validation tests** for undefined size/animation/variant, semi-transparent custom color, `BorderRadius=-2`, zero/negative animation duration, and rollback to original state after every failed assignment.
- [ ] **Step 3: Write failing AutoSize tests** for the four size multipliers using current Font.Height, 100-logical-pixel preferred width at 96/144/192 virtual DPI through the helper seam added in Task 2, and explicit `AutoSize=false` bounds remaining unchanged after `PlaceholderSize` changes.
- [ ] **Step 4: Run focused UI tests and verify RED because the public types do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapPlaceholderTests --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 5: Implement the two enums and minimal `BootstrapPlaceholder` public contract.** Use private validation helpers and same-value no-op guards. Apply current Body theme font using the established caller-font ownership pattern; do not add any extra public state to make tests easier.
- [ ] **Step 6: Implement `ApplyPreferredSize()` so it writes `Size` only when `AutoSize=true`.** Keep explicit bounds untouched when AutoSize is false.
- [ ] **Step 7: Run focused tests on `net8.0-windows`; expect all Task 1 tests not depending on Task 2 render math to pass.** If sizing tests are temporarily red only because the helper does not yet exist, move those exact assertions into Task 2 rather than weakening them.
- [ ] **Step 8: Run the same focused fixture on `net48`; verify GREEN for implemented assertions.**
- [ ] **Step 9: Commit** `feat: add BootstrapPlaceholder public contract`.

### Task 2: Implement pure Bootstrap size, color, opacity, wave, and radius logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholderRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderRenderLogicTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholder.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderTests.cs`

**Interfaces:**
- Produces exactly the internal constants/methods in **Internal Render Logic Contract**.
- Consumes only values, `Size`, theme token objects, `DpiScaler`, and existing `BootstrapVariantColorResolver`; no live Control/Graphics/animation dependency.

- [ ] **Step 1: Write failing sizing tests** for 100 logical px at 96/144/192 DPI, proposed-width capping, 0.6/0.8/1.0/1.2 Font.Height multipliers, minimum height 1, and invalid font height/size/DPI.

```csharp
[TestCase(BootstrapPlaceholderSize.ExtraSmall, 0.6)]
[TestCase(BootstrapPlaceholderSize.Small, 0.8)]
[TestCase(BootstrapPlaceholderSize.Default, 1.0)]
[TestCase(BootstrapPlaceholderSize.Large, 1.2)]
public void PreferredHeightMatchesBootstrapEmScale(
    BootstrapPlaceholderSize size,
    double multiplier)
{
    var actual = BootstrapPlaceholderRenderLogic.GetPreferredSize(
        fontHeight: 20,
        size,
        dpi: 96,
        proposedSize: Size.Empty);

    Assert.That(actual.Height, Is.EqualTo((int)Math.Ceiling(20.0 * multiplier)));
}
```

- [ ] **Step 2: Write failing base-color tests** for all semantic variants, opaque custom-color precedence, disabled token precedence, null palette rejection, and semi-transparent custom-color rejection.
- [ ] **Step 3: Write failing Glow tests** at progress 0/.25/.5/.75/1 with expected opacity .5/.35/.2/.35/.5, clamp tests for finite out-of-range values, and NaN/infinity rejection.
- [ ] **Step 4: Write failing Wave tests** proving progress 0 is 0.5 at visible positions, progress 0.5 reaches approximately 0.2 at x=0.5, points outside the trough return 0.5, symmetry around the center holds, finite bounds clamp, and non-finite inputs throw.
- [ ] **Step 5: Write failing alpha tests** for 0.5 => alpha 128, 0.2 => alpha 51, exact RGB preservation, finite clamp, and non-finite rejection.
- [ ] **Step 6: Write failing radius tests** for 0, explicit logical radii at multiple DPI values, `-1` resolving `theme.Metrics.Radius`, and invalid values/null metrics.
- [ ] **Step 7: Run pure helper tests; verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapPlaceholderRenderLogicTests
```

- [ ] **Step 8: Implement the minimal pure helper exactly as specified.** Do not use framework APIs unavailable to `net48`; keep formulas identical on both targets.
- [ ] **Step 9: Wire `BootstrapPlaceholder.GetPreferredSize` to the helper and rerun Placeholder + helper fixtures on both target frameworks; verify GREEN.**
- [ ] **Step 10: Commit** `feat: add placeholder render logic`.

### Task 3: Add static painting, rounded geometry, and live theme/font/DPI adaptation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholder.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderTests.cs`

**Interfaces:**
- Consumes `BootstrapPlaceholderRenderLogic`, `BootstrapThemeManager`, `DpiScaler`, `CornerRadius`, and `RoundedPath`.
- Produces no new public properties/events/methods.

- [ ] **Step 1: Add failing tests** proving caller-assigned Font survives subsequent Light/Dark theme changes and is not disposed by Placeholder, while an untouched Placeholder follows current theme Body typography.
- [ ] **Step 2: Add failing tests** proving theme-owned preferred size updates after a theme typography change, custom color/variant values survive theme change, and explicit bounds survive theme/DPI changes when `AutoSize=false`.
- [ ] **Step 3: Add a failing paint smoke test** that renders None/Glow-progress-zero-compatible static state into a small bitmap for square radius, theme radius, oversized radius, enabled/disabled, semantic and custom color without exception or leaked persistent GDI object ownership. Do not assert fragile full-image golden pixels.
- [ ] **Step 4: Run focused UI tests with hang protection and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapPlaceholderTests --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 5: Implement `OnPaint` for static/solid states** using the base-color/opacity helper and `RoundedPath`. Keep all brushes/paths local `using` resources and restore `SmoothingMode` after rounded painting.
- [ ] **Step 6: Implement theme subscription and Body-font ownership lifecycle** using the same value-equal Font ownership safeguard already used by themed controls. Add `OnFontChanged`, `OnAutoSizeChanged`, `OnEnabledChanged`, `OnDpiChangedAfterParent`, theme handler, and deterministic `Dispose(bool)` cleanup.
- [ ] **Step 7: Run focused tests on both targets; verify GREEN.**
- [ ] **Step 8: Commit** `feat: render themed placeholders`.

### Task 4: Add Glow/Wave animation through BootstrapLoopAnimation only

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPlaceholder.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderContractTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPlaceholderTests.cs`

**Interfaces:**
- Consumes `BootstrapLoopAnimation` + `BootstrapEasing.Linear` and the render helper's Glow/Wave formulas.
- Maintains exactly one private loop instance at most; no new public runtime-control API.

- [ ] **Step 1: Add failing behavior tests** that None is a stable static presentation, switching to Glow/Wave preserves every other public property, changing duration validates and preserves animation kind, and switching back to None remains usable across handle recreation.
- [ ] **Step 2: Add failing theme/reduced-motion tests** around public observable behavior: construct under ReducedMotion, create a handle, force multiple paints, and verify no exception/size churn/property mutation; repeat after toggling theme ReducedMotion at runtime. Rely on existing `BootstrapLoopAnimation` unit tests for scheduler internals instead of duplicating timer tests here.
- [ ] **Step 3: Add failing Wave paint smoke tests** for tiny (`1x1`, `2x2`), normal, very wide, very tall, square, rounded, Light/Dark, enabled/disabled, and custom-color controls. The goal is deterministic no-throw/resource lifecycle, not brittle exact anti-aliasing screenshots.
- [ ] **Step 4: Add a failing public-surface contract test.** Assert declared public properties are exactly:

```text
Animation
AnimationDuration
BorderRadius
CustomColor
PlaceholderSize
Variant
```

Assert the only declared public method is `GetPreferredSize`, declared public events are empty, and exported Placeholder-related types are exactly:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholder
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderAnimation
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderSize
```

Explicitly assert no exported type name contains `BootstrapSkeleton` or `PlaceholderRenderLogic`.

- [ ] **Step 5: Run focused tests and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapPlaceholder" --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 6: Implement loop reconciliation** exactly as defined in **Animation Lifecycle Contract**. Subscribe one `ProgressChanged` handler per loop and always detach before dispose/replacement.
- [ ] **Step 7: Implement Wave painting** with exactly nine `ColorBlend` samples and one `LinearGradientBrush` at `130f`. Glow uses one solid brush with helper opacity. Do not cache Brushes or create Bitmaps.
- [ ] **Step 8: Review declared protected surface.** Expected Placeholder-specific protected overrides are limited to lifecycle/render needs: `Dispose`, `OnAutoSizeChanged`, `OnDpiChangedAfterParent`, `OnEnabledChanged`, `OnFontChanged`, `OnHandleCreated`, `OnHandleDestroyed`, and `OnPaint`. If implementation introduces another protected override, either remove it or document/test why it is required before API baseline approval.
- [ ] **Step 9: Run all Placeholder fixtures on `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 10: Search product files and confirm no `Timer`, `Task.Delay`, `Thread`, `Application.Idle`, `MessageBox.Show`, or `ShowDialog` was introduced.**
- [ ] **Step 11: Commit** `feat: animate BootstrapPlaceholder`.

### Task 5: Add integrated Placeholder/Skeleton demo and application-owned content swap

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PlaceholderDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/PlaceholderDemoFormTests.cs`

**Interfaces:**
- Demo consumes only public Placeholder API and ordinary WinForms layout/visibility APIs.
- Demo must not add reusable skeleton infrastructure to product code.

- [ ] **Step 1: Write a failing demo construction test** that instantiates `PlaceholderDemoForm` on STA and verifies the form can create/dispose with no modal dialog, background worker, or required external service.
- [ ] **Step 2: Write failing structural demo tests** for examples covering all four sizes, None/Glow/Wave, at least Primary/Secondary/Success/Danger variants, custom color, square/theme/large rounded radius, AutoSize and explicit-size modes, and a composed skeleton panel with several independent placeholders.
- [ ] **Step 3: Add a failing integration test** proving `MainForm` registers a page named `Placeholder / Skeleton` and can construct that page through the existing demo navigation pattern.
- [ ] **Step 4: Run demo tests with hang protection; verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter PlaceholderDemoFormTests --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 5: Implement `PlaceholderDemoForm`.** Include these sections:
  - `Sizes` — XS/SM/Default/LG at natural AutoSize height.
  - `Colors` — semantic variants plus one opaque CustomColor.
  - `Animations` — static, Glow, Wave side-by-side; global demo Reduced motion toggle remains authoritative.
  - `Skeleton card` — circular avatar, title, 3 body lines with different percent widths, and a button-shaped placeholder composed in ordinary layout containers.
  - `Application-owned swap` — one button toggles skeleton/content panel visibility and updates a native visible status label (`Loading content…` / `Content loaded.`).
- [ ] **Step 6: Do not put `Thread.Sleep`, async fake work, timers, or auto-completion into the demo.** The swap button demonstrates ownership without making tests time-dependent.
- [ ] **Step 7: Register `Placeholder / Skeleton` in `MainForm.ConfigurePages()` near other loading/presentation components.** Description should state static/Glow/Wave placeholders, skeleton composition, theme/reduced-motion, and application-owned content swapping.
- [ ] **Step 8: Run demo tests on both target frameworks; verify GREEN.**
- [ ] **Step 9: Commit** `demo: showcase placeholder skeleton patterns`.

### Task 6: Document the contract and deliberately approve the public API addition

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Documentation must distinguish decorative Placeholder from application loading state and from Spinner/Progress.
- API baseline intentionally adds only the three exported Placeholder-related types.

- [ ] **Step 1: Add `BootstrapPlaceholder` to `docs/COMPONENTS.md`.** Document public properties/defaults, size multipliers, animation formulas/2s cycle, reduced-motion behavior, semantic/custom color, radius semantics, AutoSize vs explicit bounds, decorative accessibility rule, and Skeleton-as-composition rule.
- [ ] **Step 2: Update architecture/testing docs.** Architecture states Placeholder reuses Theme/DPI/Rendering/Animation infrastructure and introduces no scheduler. Testing documents pure render-math coverage, STA paint/lifecycle coverage, reduced-motion/manual gates, and explicit-size layout checks.
- [ ] **Step 3: Update README/package README/changelog** with concise usage and one skeleton composition example; do not advertise a nonexistent `BootstrapSkeleton` type.
- [ ] **Step 4: Add a release-contract test** in `Phase16PublicApiBaselineTests` that verifies the reviewed Placeholder public properties/method/events and keeps `BootstrapPlaceholderRenderLogic` internal.
- [ ] **Step 5: Run API baseline before changing the approved fingerprint.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests --blame-hang --blame-hang-timeout 5m
```

Expected: `ExportedApiMatchesApprovedV1Baseline` fails and prints a deterministic actual exported fingerprint/API surface; the new explicit Placeholder contract test should otherwise describe the intended surface.

- [ ] **Step 6: Review exported surface line-by-line.** Intentional new public types only:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholder : System.Windows.Forms.Control
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderAnimation
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPlaceholderSize
```

Confirm there is no exported `BootstrapSkeleton`, render helper, animation implementation detail, opacity constant, wave geometry type, layout collection, or status/loading abstraction.

- [ ] **Step 7: Review protected declared members** of `BootstrapPlaceholder`; keep only the lifecycle/render overrides justified in Task 4. Any extra visible protected member must be explicitly reviewed before accepting the fingerprint.
- [ ] **Step 8: Copy the reviewed actual fingerprint into `ApprovedV1Fingerprint` and `docs/PUBLIC_API_BASELINE.md`, recording Placeholder as an intentional compatible addition.** Keep `AssemblyVersion` unchanged unless a separate release task changes it.
- [ ] **Step 9: Rerun API baseline on `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 10: Commit** `docs: finalize BootstrapPlaceholder contract`.

### Task 7: Complete dual-target verification and manual UI/accessibility gate

**Files:**
- No new files expected; fix only Placeholder-related defects uncovered by verification.

- [ ] **Step 1: Build .NET Framework 4.8:**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net48
```

Expected: zero errors.

- [ ] **Step 2: Build .NET 8 Windows:**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net8.0-windows
```

Expected: zero errors.

- [ ] **Step 3: Run full bounded suite:**

```powershell
./test.ps1
```

Expected: both targets pass with no hang timeout, modal dialog, blame dump, or background animation left alive after disposal.

- [ ] **Step 4: Search Placeholder product files for prohibited infrastructure.** Confirm no `Timer`, `Task.Delay`, `Thread.Sleep`, `Thread`, `BackgroundWorker`, `Application.Idle`, `MessageBox.Show`, `ShowDialog`, new global event hub, off-screen per-frame bitmap, or external package.
- [ ] **Step 5: Run demo/manual visual checks:** XS/SM/Default/LG; static/Glow/Wave; all semantic variants; CustomColor; square/theme/oversized radius; circle avatar; short/long skeleton lines; AutoSize true; explicit-size AutoSize false; Dock/Anchor/TableLayout percent sizing; repeated resize; Light/Dark live switch; global Reduced motion toggle; Enabled/disabled parent; caller Font; 100/125/150/175/200% Windows scaling.
- [ ] **Step 6: Verify animation lifecycle manually/diagnostically:** None schedules nothing; Glow/Wave start only at runtime; hide/show pauses/resumes through shared owner lifecycle; handle recreation remains usable; changing duration/reduced-motion does not leak loops; disposing an animated page leaves no later callback into the disposed control.
- [ ] **Step 7: Accessibility smoke check** with Narrator or Windows accessibility inspection: placeholders do not enter Tab order and are not announced as actionable controls; the demo's native loading status text is understandable; loaded content becomes the meaningful accessible surface after swap.
- [ ] **Step 8: Verify application ownership boundary:** toggling demo skeleton/content changes only demo panel visibility/status; Placeholder itself never changes application loading state, starts async work, or emits completion events.
- [ ] **Step 9: Verify Bootstrap fidelity visually:** static opacity appears equivalent to 50% current/semantic color; Glow reaches a visibly dim midpoint and returns smoothly; Wave presents a single moving low-opacity band at approximately Bootstrap's 130-degree direction; reduced motion is stable rather than blank.
- [ ] **Step 10: If verification requires code changes, rerun Steps 1-9 and commit** `fix: harden BootstrapPlaceholder verification`. **If no fixes are required, do not create an empty commit.**

---

## Definition of Done

- Public API exports exactly `BootstrapPlaceholder`, `BootstrapPlaceholderSize`, and `BootstrapPlaceholderAnimation` for this feature.
- No public `BootstrapSkeleton` type exists; Skeleton is explicitly documented and demonstrated as composition of ordinary placeholders.
- Public Placeholder API is limited to `PlaceholderSize`, `Animation`, `Variant`, `CustomColor`, `BorderRadius`, `AnimationDuration`, and `GetPreferredSize`.
- Defaults are Default size, None animation, Secondary variant, Empty custom color, square radius, 2-second duration, AutoSize, transparent background, non-focusable, wait cursor.
- XS/SM/Default/LG intrinsic heights map exactly to 0.6/0.8/1.0/1.2 of `Font.Height`.
- AutoSize preferred width is 100 logical pixels scaled through `DpiScaler`; explicit bounds are authoritative when AutoSize is false.
- Static placeholder uses 0.5 opacity; Glow reaches 0.2 at half-cycle and returns to 0.5; Wave uses the specified moving trough and 130-degree nine-sample gradient.
- Semantic colors reuse `BootstrapVariantColorResolver`; CustomColor is opaque-only; disabled presentation uses theme Disabled color before placeholder opacity.
- `BorderRadius=0` is square; `-1` uses current theme radius; explicit radii are DPI-scaled and normalized through existing CornerRadius/RoundedPath infrastructure.
- Glow/Wave use only `BootstrapLoopAnimation`; no component-specific Timer/thread/frame scheduler exists.
- Runtime reduced-motion changes produce a stable visible frame without mutating the public animation selection or continuously scheduling frames.
- Theme, Body typography, caller Font ownership, DPI, Enabled state, handle recreation, and disposal are deterministic.
- Painting uses disposable local GDI resources and no per-frame off-screen bitmap.
- Placeholder adds no focus, keyboard, mouse activation, loading/task model, data binding, content host, completion event, or automatic swap behavior.
- Demo covers sizes, colors, animations, radius, skeleton card composition, reduced motion, and application-owned skeleton/content swapping with separate loading status text.
- Accessibility smoke check confirms placeholders are decorative/non-actionable and loading status is communicated outside the placeholder visuals.
- Both targets build; focused/full tests pass with bounded WinForms hang detection.
- Component/architecture/testing/package/README/changelog/API-baseline docs are updated and the exported fingerprint is deliberately reviewed.

## Self-Review

- **Bootstrap coverage:** The plan carries across Bootstrap 5.3's placeholder base presentation, exact `.5/.2` opacity values, XS/SM/LG em sizes, 2-second Glow, 2-second Wave, semantic color concept, and application-owned loading/content swap while adapting width/layout to native WinForms.
- **Repository fit:** Theme colors, Body typography, `DpiScaler`, `BootstrapVariantColorResolver`, `CornerRadius`/`RoundedPath`, and `BootstrapLoopAnimation` are reused; no competing global infrastructure is introduced.
- **API discipline:** One primitive plus two enums is sufficient. Skeleton templates, shape enums, percentage widths, loading/task state, public progress, Start/Stop aliases, opacity knobs, wave-angle knobs, and a second scheduler are intentionally excluded.
- **Animation determinism:** Glow/Wave math is pure and unit-testable; runtime loop only publishes normalized progress; reduced motion has a defined progress-zero visible frame.
- **Ownership discipline:** Application owns async/loading/content visibility; Placeholder owns only its visual state, theme subscription, optional loop, and framework-created font/GDI resources.
- **Accessibility discipline:** Decorative placeholders remain out of interaction/focus; the plan does not falsely treat each gray bar as meaningful status, and the demo provides separate status text.
- **Placeholder scan:** No `TBD`, `TODO`, “implement later”, unspecified alternate architecture, or guessed future fingerprint remains. Every public/internal type and formula used by later tasks is defined above.
- **Type consistency:** `BootstrapPlaceholder.Animation` always uses `BootstrapPlaceholderAnimation`; `PlaceholderSize` always uses `BootstrapPlaceholderSize`; semantic color uses existing `BootstrapVariant`; render helper stays internal; animation loop stays private.
- **Scope check:** The feature is one independently testable presentation primitive plus demo/docs/API review. Data loading, content hosting, skeleton templating, shimmer customization, virtualization, list/data-grid integration, and automatic replacement are deliberately outside V1.