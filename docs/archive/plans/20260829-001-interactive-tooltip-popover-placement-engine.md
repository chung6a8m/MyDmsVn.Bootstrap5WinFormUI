# Interactive Tooltip / Popover and Popper-like Placement Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an explicit Popper-like placement/collision engine for floating WinForms surfaces, make `BootstrapTooltip` opt into deterministic Top/Bottom/Left/Right placement without breaking its existing native default behavior, and add `BootstrapPopover` for arbitrary interactive/focusable content.

**Architecture:** Introduce one deterministic, control-agnostic overlay placement engine in `Rendering` that receives screen-pixel anchor/popup/boundary geometry and returns final bounds/placement after optional flip/shift collision handling. Existing `BootstrapTooltip` keeps its current native `ToolTip` association, timing, owner-draw, and placement behavior by default; an opt-in managed positioning mode cancels the native auto-popup at `Popup`, computes a safe location, and re-shows the same owned native `ToolTip` through the native `ToolTip.Show(..., Point, duration)` API. Interactive content is intentionally modeled as a separate `BootstrapPopover` because a tooltip should remain non-focusable; Popover uses one internal `ToolStripDropDown` host, one themed overlay surface, caller-owned content, native outside-click dismissal, and the shared placement engine.

**Tech Stack:** C#, Windows Forms, `System.ComponentModel`, `System.Drawing`, `System.Windows.Forms.ToolTip`, `ToolStripDropDown`, `ToolStripControlHost`, existing `BootstrapThemeManager`, `BootstrapThemeMetrics`, `BootstrapThemeTypography`, `DpiScaler`, `CornerRadius`, `RoundedPath`, NUnit 4, multi-target `net48;net8.0-windows`.

**Spec:** This plan extends `docs/plans/20260828-005-bootstrap-tooltip.md` and `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by superseding only the previous Stage 3 exclusions for explicit placement and interactive floating content. Existing Stage 3 native Tooltip semantics remain the compatibility baseline unless this plan explicitly says otherwise.

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; public component APIs stay under `MyDmsVn.Bootstrap5WinFormUI.Controls`; shared geometry stays under `MyDmsVn.Bootstrap5WinFormUI.Rendering`.
- The product and test projects must continue to compile for both `net48` and `net8.0-windows` from one shared implementation wherever practical.
- Do not remove, rename, or silently change semantics of any existing public/protected member.
- Treat every new public/protected member as a frozen-v1 API change. Update `Phase16PublicApiBaselineTests` and `docs/PUBLIC_API_BASELINE.md` only after the new API is finalized and reviewed.
- Reuse existing Theme, Rendering, DPI, color, and rounded-path infrastructure. Do not introduce a second DPI scaler, theme manager, palette system, or geometry utility for overlays.
- Do not add Popper/Floating UI/JavaScript/browser dependencies or another NuGet package. The project needs only the small deterministic subset of placement behavior described here.
- Do not replace `BootstrapTooltip` with a `Form`, `ToolStripDropDown`, or a second tooltip component. It must continue to own exactly one native `ToolTip` instance.
- Do not put arbitrary focusable `Control` content inside `BootstrapTooltip`. Interactive/focusable floating content belongs to `BootstrapPopover`.
- Do not create a custom top-level `Form` for Popover. Use a native `ToolStripDropDown` + `ToolStripControlHost` so native popup focus/dismissal/message-loop behavior remains authoritative.
- Do not add a global mouse hook, keyboard hook, Win32 CBT hook, or polling loop.
- Do not create one timer per target control. Tooltip timing remains native; Popover Click/Manual trigger semantics require no scheduling timer.
- All WinForms tests that instantiate controls/components/popup hosts run in STA and are non-parallel where they touch global theme state or top-level popup state.
- All GDI objects and `Region` instances created by overlay code are disposed/replaced in the same ownership scope that creates them.
- Logical public spacing values are 96-DPI pixels; convert them with `DpiScaler` at the target/anchor DPI before invoking the placement engine.
- Placement geometry is computed in physical screen pixels. Never mix logical/client coordinates and physical screen coordinates inside the engine.
- Use `Screen.FromRectangle(anchorScreenBounds).WorkingArea` as the v1 collision boundary. Custom clipping parents, custom boundary providers, and virtual anchors are deferred.
- Stage completion requires both target frameworks, pure geometry tests, STA lifecycle tests, demo coverage, documentation, and public API baseline to be green.

---

## Current State and Compatibility Contract

At planning time, `BootstrapTooltip` already:

- derives from `Component` and implements `IExtenderProvider`;
- owns exactly one native `ToolTip`;
- uses native `SetToolTip`/`GetToolTip` as the single caption source of truth;
- forwards `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` directly to that native instance;
- uses native `Popup` for measurement and native `Draw` for Bootstrap owner drawing;
- resolves current theme at popup/draw time;
- intentionally exposes no public `Show`/`Hide` method and no native `ToolTip` instance;
- currently delegates placement entirely to native WinForms.

This plan must preserve all of those behaviors in the default configuration.

The compatibility rule is therefore:

```text
BootstrapTooltip.Positioning == Native
    => Stage 3 behavior remains authoritative.

BootstrapTooltip.Positioning == Managed
    => native association/timing/owner drawing remain authoritative,
       but automatic native popup location is canceled and re-issued
       through explicit computed coordinates.
```

Existing tests such as `PublicSurfaceDoesNotLeakNativeTooltipOrUnplannedPopupApis` must remain conceptually valid: this plan does **not** add public Tooltip `Show`/`Hide`, expose the native instance, or add interactive Tooltip content.

---

## Accessibility / Semantic Decision

Interactive content is a Popover responsibility, not a Tooltip extension.

The WAI-ARIA Authoring Practices tooltip pattern states that a tooltip does not receive focus and that focusable popup content is better modeled as a non-modal dialog-like surface. Although this is a WinForms library rather than a browser library, the interaction principle is still useful: a tooltip is descriptive and non-interactive; a popover can host buttons, links, editors, and other focusable controls.

Therefore:

- `BootstrapTooltip` remains text-only and non-focusable.
- `BootstrapPopover` hosts arbitrary interactive `Control` content.
- Tooltip managed placement and Popover share geometry infrastructure, not content/focus semantics.

References:

- https://www.w3.org/WAI/ARIA/apg/patterns/tooltip/
- https://floating-ui.com/docs/computeposition
- https://floating-ui.com/docs/flip
- https://floating-ui.com/docs/shift
- https://popper.js.org/docs/v2/modifiers/flip/
- https://popper.js.org/docs/v2/modifiers/prevent-overflow/
- https://learn.microsoft.com/dotnet/api/system.windows.forms.popupeventargs
- https://learn.microsoft.com/dotnet/api/system.windows.forms.tooltip.show

---

## Scope

### In scope

1. Shared explicit placement model:
   - Auto
   - Top / TopStart / TopEnd
   - Bottom / BottomStart / BottomEnd
   - Left / LeftStart / LeftEnd
   - Right / RightStart / RightEnd
2. Shared collision modes:
   - None
   - Flip
   - Shift
   - FlipAndShift
3. Deterministic pure placement engine using caller-provided anchor bounds, floating size, boundary, logical offset converted to pixels, and RTL state.
4. `BootstrapTooltip` opt-in managed placement while retaining the same native `ToolTip` for timing, captions, drawing, and lifetime.
5. `BootstrapPopover` with arbitrary caller-owned `Control` content, Click or Manual trigger, explicit placement/collision, keyboard Escape dismissal, native outside-click dismissal, and safe target/content lifetime.
6. Automatic reposition for open Popovers when the target, ancestor scroll containers, or owning Form moves/resizes.
7. Light/Dark runtime repaint, per-monitor DPI recalculation, multi-monitor working-area collision handling, and resource/lifetime tests.

### Explicitly deferred

- HTML/Markdown/rich-text parsing.
- Focusable content inside `BootstrapTooltip`.
- Tooltip/Popover arrow/caret middleware.
- Popover hover trigger and hover grace polygons.
- Popover focus trigger.
- Nested Popovers.
- Modal Popovers or focus trapping.
- Resize/size middleware that shrinks content to the boundary.
- Custom boundary providers / clipping ancestor intersection.
- Virtual anchors not backed by a WinForms `Control`.
- Follow-cursor Tooltip mode.
- Animation/transition work for Tooltip or Popover.
- Retrofitting `BootstrapDropdown`, `BootstrapComboBox`, or other existing popups to use this engine in the same change.

---

## Public Contract to Add

### 1. Shared placement enum

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayPlacement.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapOverlayPlacement
{
    Auto,
    Top,
    TopStart,
    TopEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Left,
    LeftStart,
    LeftEnd,
    Right,
    RightStart,
    RightEnd
}
```

Semantics:

- `Top`, `Bottom`, `Left`, `Right` center the floating surface on the cross axis.
- For Top/Bottom, `Start` aligns to the target's logical leading horizontal edge and `End` to its logical trailing horizontal edge. `RightToLeft.Yes` swaps horizontal Start/End.
- For Left/Right, `Start` means top alignment and `End` means bottom alignment; RTL does not alter vertical Start/End.
- `Auto` asks the engine to choose the candidate with least overflow, then greatest visible area, using deterministic tie order `Bottom`, `Top`, `Right`, `Left`.

### 2. Shared collision enum

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayCollisionBehavior.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapOverlayCollisionBehavior
{
    None,
    Flip,
    Shift,
    FlipAndShift
}
```

Rules:

- `None`: keep the computed preferred placement even if it overflows.
- `Flip`: change side when the preferred side cannot fit on its main axis; preserve alignment where possible.
- `Shift`: keep the selected side and clamp only the cross-axis coordinate into the padded boundary.
- `FlipAndShift`: flip first, then shift. This is the default for managed Tooltip placement and Popover.
- For `Auto`, side selection is already an automatic best-fit operation; collision behavior only controls whether final cross-axis Shift is applied.

### 3. Tooltip positioning mode

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipPositioning.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapTooltipPositioning
{
    Native,
    Managed
}
```

Extend `BootstrapTooltip` with:

```csharp
public BootstrapTooltipPositioning Positioning { get; set; } // Native
public BootstrapOverlayPlacement Placement { get; set; }     // Top
public BootstrapOverlayCollisionBehavior CollisionBehavior { get; set; } // FlipAndShift
public int Offset { get; set; }                               // 6 logical px
public int BoundaryPadding { get; set; }                      // 8 logical px
```

Validation/defaults:

| Member | Rule |
| --- | --- |
| `Positioning` | default `Native`; reject undefined enum values |
| `Placement` | default `Top`; reject undefined enum values |
| `CollisionBehavior` | default `FlipAndShift`; reject undefined enum values |
| `Offset` | logical 96-DPI main-axis gap; default `6`; reject negative |
| `BoundaryPadding` | logical 96-DPI inset from current screen working area; default `8`; reject negative |

`Placement`, `CollisionBehavior`, `Offset`, and `BoundaryPadding` are inert while `Positioning == Native`; they do not mutate the native tooltip configuration or placement path.

### 4. Popover trigger enum

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopoverTrigger.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapPopoverTrigger
{
    Click,
    Manual
}
```

### 5. BootstrapPopover

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`:

```csharp
[DefaultEvent(nameof(Opened))]
public class BootstrapPopover : Component
{
    public BootstrapPopover();
    public BootstrapPopover(IContainer container);

    public Control? Target { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Content { get; set; }

    public BootstrapPopoverTrigger Trigger { get; set; } // Click
    public BootstrapOverlayPlacement Placement { get; set; } // Auto
    public BootstrapOverlayCollisionBehavior CollisionBehavior { get; set; } // FlipAndShift
    public int Offset { get; set; } // 8 logical px
    public int BoundaryPadding { get; set; } // 8 logical px
    public Padding ContentPadding { get; set; } // 12,8,12,8 logical px
    public int BorderRadius { get; set; } // -1, theme radius sentinel
    public bool CloseOnEscape { get; set; } // true
    public bool CloseOnClickOutside { get; set; } // true
    public bool IsOpen { get; }

    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public void Show();
    public void Hide();
    public void Toggle();
}
```

Popover ownership/lifecycle rules:

1. `Target` is caller-owned and is never disposed/reparented by Popover.
2. `Content` is caller-owned and is never disposed by Popover.
3. `Content` must be non-disposed and unparented at assignment time. Popover then parents it to its private themed overlay surface for the lifetime of the assignment, including while closed.
4. Replacing `Content` removes the previous content from the private surface before attaching the new one. The old control becomes unparented and remains undisposed.
5. Setting `Content` while open is rejected with `InvalidOperationException`; callers must `Hide()` before changing interactive content. This prevents ambiguous focus/close event ordering.
6. Disposing Popover first closes the popup, detaches target events, removes caller-owned content from the private surface, then disposes only framework-owned popup/surface/host objects.
7. If assigned `Content` is disposed externally, Popover closes and clears the reference without throwing.
8. If `Target` is disposed externally, Popover closes, detaches events, and sets `Target = null`.
9. `Show()` requires a live visible `Target` and non-null live `Content`; otherwise it throws `InvalidOperationException` for missing configuration and no-ops for an already-open Popover.
10. `Hide()` is idempotent and never disposes `Content`.
11. `Toggle()` maps closed → `Show()` and open → `Hide()`.
12. `Opened`/`Closed` fire exactly once per effective state transition, based on the actual native dropdown open/close state.

Interaction rules:

- `Trigger == Click`: subscribe once to `Target.Click`; target activation toggles the Popover.
- `Trigger == Manual`: target Click is not subscribed; callers use `Show`/`Hide`/`Toggle`.
- Opening keeps the target as the placement anchor. If the popup contains a focusable descendant, move focus to the first tabbable content descendant after the dropdown is visible.
- Escape closes when `CloseOnEscape == true` and returns focus to the live target.
- Native outside-click auto-close is enabled only when `CloseOnClickOutside == true`.
- Outside-click close must not force focus back to Target; native focus destination remains authoritative.
- Content clicks/typing do not close the Popover unless the application calls `Hide()` or native AutoClose determines the click is outside.

---

## Shared Placement Engine Contract

Create `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapOverlayPlacementEngine.cs`.

The engine must be pure and deterministic:

- no `Control` references;
- no `Screen` access;
- no theme access;
- no handles;
- no static mutable state;
- no GDI objects;
- no DPI scaling;
- input and output are screen-pixel geometry only.

Internal request/result types:

```csharp
internal readonly struct BootstrapOverlayPlacementRequest
{
    public BootstrapOverlayPlacementRequest(
        Rectangle anchorBounds,
        Size floatingSize,
        Rectangle boundaryBounds,
        BootstrapOverlayPlacement preferredPlacement,
        BootstrapOverlayCollisionBehavior collisionBehavior,
        int offset,
        int boundaryPadding,
        bool rightToLeft)
    {
        AnchorBounds = anchorBounds;
        FloatingSize = floatingSize;
        BoundaryBounds = boundaryBounds;
        PreferredPlacement = preferredPlacement;
        CollisionBehavior = collisionBehavior;
        Offset = offset;
        BoundaryPadding = boundaryPadding;
        RightToLeft = rightToLeft;
    }

    public Rectangle AnchorBounds { get; }
    public Size FloatingSize { get; }
    public Rectangle BoundaryBounds { get; }
    public BootstrapOverlayPlacement PreferredPlacement { get; }
    public BootstrapOverlayCollisionBehavior CollisionBehavior { get; }
    public int Offset { get; }
    public int BoundaryPadding { get; }
    public bool RightToLeft { get; }
}

internal readonly struct BootstrapOverlayOverflow
{
    public BootstrapOverlayOverflow(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    public int Total => Left + Top + Right + Bottom;
}

internal readonly struct BootstrapOverlayPlacementResult
{
    public BootstrapOverlayPlacementResult(
        Rectangle bounds,
        BootstrapOverlayPlacement placement,
        BootstrapOverlayOverflow overflow,
        bool flipped,
        bool shifted)
    {
        Bounds = bounds;
        Placement = placement;
        Overflow = overflow;
        Flipped = flipped;
        Shifted = shifted;
    }

    public Rectangle Bounds { get; }
    public BootstrapOverlayPlacement Placement { get; }
    public BootstrapOverlayOverflow Overflow { get; }
    public bool Flipped { get; }
    public bool Shifted { get; }
}

internal static class BootstrapOverlayPlacementEngine
{
    public static BootstrapOverlayPlacementResult Compute(
        BootstrapOverlayPlacementRequest request);
}
```

### Base placement formulas

For an anchor `(A.X, A.Y, A.Width, A.Height)`, floating size `(W, H)`, and offset `O`:

```text
Top center:
    X = A.Left + (A.Width - W) / 2
    Y = A.Top - O - H

Bottom center:
    X = A.Left + (A.Width - W) / 2
    Y = A.Bottom + O

Left center:
    X = A.Left - O - W
    Y = A.Top + (A.Height - H) / 2

Right center:
    X = A.Right + O
    Y = A.Top + (A.Height - H) / 2
```

Top/Bottom Start/End replace the centered X formula with logical leading/trailing alignment; Left/Right Start/End replace centered Y with top/bottom alignment.

Use saturating/`long` intermediate arithmetic so extreme rectangles cannot overflow `int` and wrap coordinates.

### Padded boundary

`BoundaryPadding` shrinks the supplied boundary before overflow detection:

```csharp
var effectiveBoundary = Rectangle.FromLTRB(
    SaturatingAdd(boundary.Left, padding),
    SaturatingAdd(boundary.Top, padding),
    SaturatingSubtract(boundary.Right, padding),
    SaturatingSubtract(boundary.Bottom, padding));
```

If padding is larger than half an axis, clamp the effective axis to zero size centered inside the original boundary instead of producing an inverted rectangle.

### Overflow calculation

For candidate `C` against padded boundary `B`:

```text
left   = max(0, B.Left   - C.Left)
top    = max(0, B.Top    - C.Top)
right  = max(0, C.Right  - B.Right)
bottom = max(0, C.Bottom - B.Bottom)
```

### Flip behavior

For an explicit preferred placement:

1. Compute preferred candidate.
2. If it has no main-axis overflow, keep it.
3. Otherwise try its exact opposite preserving alignment:
   - Top ↔ Bottom
   - TopStart ↔ BottomStart
   - TopEnd ↔ BottomEnd
   - Left ↔ Right
   - LeftStart ↔ RightStart
   - LeftEnd ↔ RightEnd
4. If the opposite has zero main-axis overflow, choose it.
5. If both overflow, compare their total overflow and choose the smaller one; ties keep the original preferred placement.
6. Do not silently rotate to a perpendicular side in v1 explicit Flip. Perpendicular best-fit selection belongs to `Auto`.

This narrow rule makes behavior deterministic and easy to explain.

### Shift behavior

Shift never changes the chosen side.

- Top/Bottom placements shift X only.
- Left/Right placements shift Y only.
- Clamp the coordinate so the popup fits within the padded boundary when possible.
- If the popup is larger than the boundary on that axis, align to the padded boundary start and report residual overflow; do not resize content.

### Auto behavior

`Auto` evaluates center-aligned candidates in deterministic order:

```text
Bottom
Top
Right
Left
```

Choose by:

1. lowest `Overflow.Total`;
2. then greatest intersection area between candidate and padded boundary;
3. then the order above.

The result placement is one of the four concrete center placements, never `Auto`.

If collision behavior is `Shift` or `FlipAndShift`, apply the same cross-axis shift after Auto chooses a side. `Flip` alone has no extra effect because Auto already selected the best side.

### Result invariants

- `Placement` is always concrete after `Compute`; it is never `Auto`.
- `Flipped` is true only when explicit Flip/FlipAndShift selected the exact opposite side.
- `Shifted` is true only when final cross-axis coordinates differ from the pre-shift candidate.
- `Overflow` describes the final returned bounds after all requested collision operations.

---

## Shared Popover Popup Host

Create internal UI infrastructure:

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayToolStripRenderer.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`

These types are internal and must not expand the frozen public API.

### BootstrapOverlaySurface

`BootstrapOverlaySurface : Panel` owns overlay chrome only.

Responsibilities:

- double buffered;
- exactly one optional child content control;
- theme surface/background/border painting;
- DPI-scaled content padding and border width;
- rounded path/Region derived from existing `RoundedPath`/`CornerRadius`;
- calculate preferred size from child content + scaled padding + border;
- no placement logic;
- no top-level window logic;
- no ownership of caller content.

Suggested internal surface:

```csharp
internal sealed class BootstrapOverlaySurface : Panel
{
    public BootstrapOverlaySurface();

    public Control? HostedContent { get; }

    public Padding LogicalContentPadding { get; set; }
    public int LogicalBorderRadius { get; set; }

    public void AttachContent(Control content);
    public Control? DetachContent();
    public void ApplyTheme(BootstrapTheme theme, int dpi);
}
```

`AttachContent` rejects disposed controls and controls whose `Parent` is not null unless already parented to this surface. `DetachContent` removes but never disposes the caller control.

### BootstrapOverlayDropDown

`BootstrapOverlayDropDown : ToolStripDropDown` owns exactly one `ToolStripControlHost`, which hosts exactly one framework-owned `BootstrapOverlaySurface`.

Responsibilities:

- `AutoSize = false`;
- `Padding = Padding.Empty`;
- no image/check margins;
- internal renderer suppresses native ToolStrip background/border so only BootstrapOverlaySurface paints chrome;
- `DropShadowEnabled = true` when native platform supports it;
- expose an internal `ShowAt(Rectangle bounds)` that sizes host/surface then calls native Show at the screen location;
- set/recreate top-level `Region` from surface rounded geometry so rounded corners do not reveal square native background;
- route Escape through `ProcessCmdKey` and invoke an internal close callback only when enabled;
- native `AutoClose` mirrors `CloseOnClickOutside`;
- never dispose caller content; Popover must detach caller content before disposing this host.

### BootstrapOverlayAnchorTracker

This internal helper exists only while a Popover is open.

It receives one Target and invokes an `Action` when placement must be recomputed.

Subscribe to:

- Target `LocationChanged`
- Target `SizeChanged`
- Target `VisibleChanged`
- Target `ParentChanged`
- Target `Disposed`
- each current ancestor `Control.LocationChanged`
- each current ancestor `Control.SizeChanged`
- each current ancestor `ScrollableControl.Scroll`
- containing Form `Move`
- containing Form `Resize`
- containing Form `FormClosed`

Rules:

- rebuild ancestor subscriptions on `ParentChanged`;
- if Target becomes disposed or not visible, request close instead of reposition;
- no global message filter or polling;
- `Dispose()` unsubscribes every handler and releases all references.

---

# Task 1 — Lock the New Public Contract and Preserve the Stage 3 Baseline

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Rendering/BootstrapOverlayPlacementEngineTests.cs`
- Later modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

**Interfaces:**
- Consumes: current Stage 3 `BootstrapTooltip` API and current global theme infrastructure.
- Produces: failing contract expectations for new enums, Tooltip positioning properties, Popover public API, and pure placement engine.

- [ ] **Step 1: Add Tooltip compatibility/default tests before production changes**

Extend `BootstrapTooltipTests` with:

```csharp
[Test]
public void ManagedPositioningDefaultsAreBackwardCompatible()
{
    using var tooltip = new BootstrapTooltip();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(tooltip.Positioning, Is.EqualTo(BootstrapTooltipPositioning.Native));
        Assert.That(tooltip.Placement, Is.EqualTo(BootstrapOverlayPlacement.Top));
        Assert.That(tooltip.CollisionBehavior, Is.EqualTo(BootstrapOverlayCollisionBehavior.FlipAndShift));
        Assert.That(tooltip.Offset, Is.EqualTo(6));
        Assert.That(tooltip.BoundaryPadding, Is.EqualTo(8));
    }));
}
```

Also add assertions that a fresh Tooltip still owns exactly one native `ToolTip`, still has `OwnerDraw == true`, still forwards native timing/state, and still exposes no public `Show`, `Hide`, or `Content` member.

- [ ] **Step 2: Add validation tests for all new Tooltip properties**

Test undefined enums and negative spacing before mutation:

```csharp
[Test]
public void ManagedPositioningPropertiesRejectInvalidValuesBeforeMutation()
{
    using var tooltip = new BootstrapTooltip
    {
        Positioning = BootstrapTooltipPositioning.Managed,
        Placement = BootstrapOverlayPlacement.BottomEnd,
        CollisionBehavior = BootstrapOverlayCollisionBehavior.Shift,
        Offset = 9,
        BoundaryPadding = 11
    };

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        tooltip.Positioning = (BootstrapTooltipPositioning)99);
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        tooltip.Placement = (BootstrapOverlayPlacement)99);
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        tooltip.CollisionBehavior = (BootstrapOverlayCollisionBehavior)99);
    Assert.Throws<ArgumentOutOfRangeException>(() => tooltip.Offset = -1);
    Assert.Throws<ArgumentOutOfRangeException>(() => tooltip.BoundaryPadding = -1);

    Assert.That(tooltip.Positioning, Is.EqualTo(BootstrapTooltipPositioning.Managed));
    Assert.That(tooltip.Placement, Is.EqualTo(BootstrapOverlayPlacement.BottomEnd));
    Assert.That(tooltip.CollisionBehavior, Is.EqualTo(BootstrapOverlayCollisionBehavior.Shift));
    Assert.That(tooltip.Offset, Is.EqualTo(9));
    Assert.That(tooltip.BoundaryPadding, Is.EqualTo(11));
}
```

- [ ] **Step 3: Add Popover constructor/default/ownership contract tests**

Create STA/non-parallel fixture and cover:

- parameterless construction;
- `IContainer` construction and null guard;
- defaults exactly matching the public contract above;
- `Show` fails without Target or Content;
- assigning a disposed Content fails;
- assigning already-parented Content fails;
- assigning content parents it to exactly one internal framework surface but does not dispose it;
- replacing Content while closed detaches old content without disposing it;
- changing Content while open throws;
- Target assignment subscribes according to Trigger and replacement detaches old Target;
- external Target disposal clears Target and closes;
- external Content disposal clears Content and closes;
- disposing Popover does not dispose Target or Content;
- repeated `Hide` and repeated `Dispose` are safe.

Use disposal counters:

```csharp
var targetDisposed = 0;
var contentDisposed = 0;
target.Disposed += (_, _) => targetDisposed++;
content.Disposed += (_, _) => contentDisposed++;

popover.Dispose();

Assert.That(targetDisposed, Is.Zero);
Assert.That(contentDisposed, Is.Zero);
```

- [ ] **Step 4: Add failing pure engine placement matrices**

Create initial tests for:

- all 12 explicit placements without collision;
- Auto selection;
- RTL Start/End inversion for Top/Bottom;
- no RTL change for Left/Right Start/End;
- offset application;
- boundary padding;
- Flip exact-opposite behavior;
- Shift cross-axis only;
- FlipAndShift ordering;
- oversized popup behavior;
- extreme coordinate saturation.

- [ ] **Step 5: Run focused tests and confirm RED**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayPlacementEngineTests|FullyQualifiedName~BootstrapPopoverTests|FullyQualifiedName~BootstrapTooltipTests"
```

Expected: compile failures for the new public types/members and engine. Existing Stage 3 tests must not be deleted or weakened to reach the red state.

- [ ] **Step 6: Commit the failing contract**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Rendering/BootstrapOverlayPlacementEngineTests.cs
git commit -m "test: define overlay positioning and popover contract"
```

---

# Task 2 — Implement the Pure Popper-like Placement / Collision Engine

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayPlacement.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayCollisionBehavior.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapOverlayPlacementEngine.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Rendering/BootstrapOverlayPlacementEngineTests.cs`

**Interfaces:**
- Consumes: `Rectangle`, `Size`, enum values, already-scaled pixel offset/padding.
- Produces: `BootstrapOverlayPlacementEngine.Compute(request)` and final screen-pixel `BootstrapOverlayPlacementResult` used by Tooltip and Popover.

- [ ] **Step 1: Add enums exactly as specified**

Implement only the enum values in the Public Contract. Do not add aliases such as `TopLeft`, `TopRight`, `BestFit`, or `PreventOverflow`.

- [ ] **Step 2: Add request/result/overflow immutable structs**

Implement the internal structs from the Shared Placement Engine Contract. Validate request inputs inside `Compute`, not constructors, so tests can construct invalid cases explicitly.

- [ ] **Step 3: Implement explicit base placement formulas**

Use a small switch that resolves side/alignment without referencing Controls.

Representative formula helper:

```csharp
private static Rectangle CalculateBaseBounds(
    Rectangle anchor,
    Size floating,
    BootstrapOverlayPlacement placement,
    int offset,
    bool rightToLeft)
{
    var width = Math.Max(0, floating.Width);
    var height = Math.Max(0, floating.Height);

    // Use long intermediates and saturate when converting back to int.
    // Resolve physical alignment from placement + rightToLeft.
}
```

Keep this helper internal/private; do not expose geometry internals publicly.

- [ ] **Step 4: Implement padded-boundary and overflow helpers**

Add deterministic helpers that clamp oversized padding and never generate inverted rectangles.

Test boundary examples with negative desktop coordinates, e.g. a monitor left of primary:

```csharp
var boundary = new Rectangle(-1920, 0, 1920, 1080);
var anchor = new Rectangle(-100, 500, 80, 30);
```

Expected: placement calculations preserve negative coordinates correctly.

- [ ] **Step 5: Implement Flip**

Implement exact-opposite mapping only.

```csharp
private static BootstrapOverlayPlacement GetOpposite(
    BootstrapOverlayPlacement placement)
{
    return placement switch
    {
        BootstrapOverlayPlacement.Top => BootstrapOverlayPlacement.Bottom,
        BootstrapOverlayPlacement.TopStart => BootstrapOverlayPlacement.BottomStart,
        BootstrapOverlayPlacement.TopEnd => BootstrapOverlayPlacement.BottomEnd,
        BootstrapOverlayPlacement.Bottom => BootstrapOverlayPlacement.Top,
        BootstrapOverlayPlacement.BottomStart => BootstrapOverlayPlacement.TopStart,
        BootstrapOverlayPlacement.BottomEnd => BootstrapOverlayPlacement.TopEnd,
        BootstrapOverlayPlacement.Left => BootstrapOverlayPlacement.Right,
        BootstrapOverlayPlacement.LeftStart => BootstrapOverlayPlacement.RightStart,
        BootstrapOverlayPlacement.LeftEnd => BootstrapOverlayPlacement.RightEnd,
        BootstrapOverlayPlacement.Right => BootstrapOverlayPlacement.Left,
        BootstrapOverlayPlacement.RightStart => BootstrapOverlayPlacement.LeftStart,
        BootstrapOverlayPlacement.RightEnd => BootstrapOverlayPlacement.LeftEnd,
        _ => throw new ArgumentOutOfRangeException(nameof(placement))
    };
}
```

For `net48`, if the repository language version rejects switch expressions in the current configuration, use the equivalent classic `switch`; do not add target-specific logic just for syntax.

- [ ] **Step 6: Implement Shift**

Top/Bottom clamp X only. Left/Right clamp Y only. Do not alter the main-axis gap produced by placement + offset.

Add tests proving a right-edge Top tooltip shifts left without changing its Y coordinate.

- [ ] **Step 7: Implement Auto scoring**

Calculate all four center candidates, overflow, and intersection area. Sort through explicit comparison code rather than LINQ allocation in this hot but small utility.

Tie example:

```csharp
[Test]
public void AutoUsesDeterministicTieOrder()
{
    var request = CreateRequest(
        anchor: new Rectangle(450, 450, 100, 100),
        floating: new Size(100, 100),
        boundary: new Rectangle(0, 0, 1000, 1000),
        placement: BootstrapOverlayPlacement.Auto,
        collision: BootstrapOverlayCollisionBehavior.None);

    var result = BootstrapOverlayPlacementEngine.Compute(request);

    Assert.That(result.Placement, Is.EqualTo(BootstrapOverlayPlacement.Bottom));
}
```

- [ ] **Step 8: Complete edge-case matrix**

Cover:

- zero-size anchor;
- zero-size floating surface;
- popup wider/taller than boundary;
- boundary padding equal to/greater than half the boundary axis;
- very large positive/negative desktop coordinates;
- invalid enum values;
- negative offset/padding rejected;
- final `Overflow` corresponds to final bounds, not pre-flip/pre-shift candidate.

- [ ] **Step 9: Run pure engine tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapOverlayPlacementEngineTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayPlacementEngineTests"
```

Expected: all placement tests pass identically on both targets.

- [ ] **Step 10: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayPlacement.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayCollisionBehavior.cs src/MyDmsVn.Bootstrap5WinFormUI/Rendering/BootstrapOverlayPlacementEngine.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Rendering/BootstrapOverlayPlacementEngineTests.cs
git commit -m "feat: add overlay placement engine"
```

---

# Task 3 — Add Opt-in Managed Placement to BootstrapTooltip

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipPositioning.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs` only if a reusable measured-size helper is necessary; do not move placement logic here.
- Modify/Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`

**Interfaces:**
- Consumes: native `ToolTip.Popup`, current caption association, current owner-draw sizing, `BootstrapOverlayPlacementEngine`.
- Produces: managed repositioning through the same native Tooltip with no new public Show/Hide API.

- [ ] **Step 1: Add the positioning enum and new properties**

Add backing fields with exact defaults:

```csharp
private BootstrapTooltipPositioning _positioning = BootstrapTooltipPositioning.Native;
private BootstrapOverlayPlacement _placement = BootstrapOverlayPlacement.Top;
private BootstrapOverlayCollisionBehavior _collisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift;
private int _offset = 6;
private int _boundaryPadding = 8;
private bool _managedShowInProgress;
private Control? _managedVisibleControl;
```

Add XML docs and `Category`, `Description`, `DefaultValue` metadata consistent with existing Tooltip properties.

- [ ] **Step 2: Keep native mode code path byte-for-byte equivalent in behavior**

Refactor `OnToolTipPopup` only enough to separate measurement from placement.

Pseudo-structure:

```csharp
private void OnToolTipPopup(object? sender, PopupEventArgs e)
{
    var associatedControl = e.AssociatedControl;
    if (associatedControl is null)
    {
        return;
    }

    var caption = _toolTip.GetToolTip(associatedControl) ?? string.Empty;
    var popupSize = MeasurePopupSize(associatedControl, caption);
    e.ToolTipSize = popupSize;

    if (_positioning == BootstrapTooltipPositioning.Native || _managedShowInProgress)
    {
        return;
    }

    e.Cancel = true;
    QueueManagedShow(associatedControl, caption, popupSize);
}
```

`Positioning == Native` must perform no `Screen` query and no explicit `ToolTip.Show` call.

- [ ] **Step 3: Implement managed placement from native Popup timing**

Use the native automatic Popup event as the timing trigger. Do **not** build another hover-delay scheduler.

`QueueManagedShow` must use `BeginInvoke`/posted UI work rather than recursively call `ToolTip.Show` from inside the canceled Popup callback:

```csharp
private void QueueManagedShow(Control control, string caption, Size popupSize)
{
    if (control.IsDisposed || !control.IsHandleCreated || string.IsNullOrEmpty(caption))
    {
        return;
    }

    control.BeginInvoke((Action)(() =>
    {
        if (_disposed || _positioning != BootstrapTooltipPositioning.Managed || control.IsDisposed)
        {
            return;
        }

        ShowManagedTooltip(control, caption, popupSize);
    }));
}
```

Before `BeginInvoke`, guard against disposal/handle teardown; after posting, re-check all state.

- [ ] **Step 4: Compute screen geometry and call native ToolTip.Show**

Inside `ShowManagedTooltip`:

1. `anchorBounds = control.RectangleToScreen(control.ClientRectangle)`.
2. `dpi = GetControlDpi(control)`.
3. scale `Offset` and `BoundaryPadding` using `DpiScaler`.
4. `boundary = Screen.FromRectangle(anchorBounds).WorkingArea`.
5. create placement request using `control.RightToLeft == RightToLeft.Yes`.
6. compute result.
7. convert `result.Bounds.Location` back with `control.PointToClient(...)`.
8. set `_managedShowInProgress = true`.
9. call `_toolTip.Show(caption, control, relativePoint, _toolTip.AutoPopDelay)`.
10. clear guard in `finally`.
11. remember `_managedVisibleControl = control` only after the native call succeeds.

Core call:

```csharp
_toolTip.Show(
    caption,
    control,
    control.PointToClient(result.Bounds.Location),
    _toolTip.AutoPopDelay);
```

Do not expose this Show method publicly.

- [ ] **Step 5: Hide the managed native Tooltip on target exit/disposal without a timer**

Managed placement needs native-like mouse-out behavior after programmatic Show.

Maintain a `HashSet<Control>` of controls that currently have non-empty Tooltip text only for managed event subscription/lifetime purposes; captions remain exclusively in `_toolTip`.

When `Positioning` becomes Managed:

- attach `MouseLeave`, `MouseDown`, `Disposed`, `VisibleChanged` to currently tracked tooltip targets;
- when it returns Native, detach those managed-only handlers and hide any managed visible tooltip.

Always update the tracked set in `SetToolTip`:

- non-empty caption → add target;
- empty caption → detach managed handlers/remove target and hide if it is the visible managed target.

Managed handlers:

```text
MouseLeave       => HideManagedTooltip(control)
MouseDown        => HideManagedTooltip(control)
Disposed         => remove/detach; clear visible reference
VisibleChanged   => hide when !Visible
```

`HideManagedTooltip` calls `_toolTip.Hide(control)` and clears `_managedVisibleControl` when appropriate.

This set must never become a second caption dictionary.

- [ ] **Step 6: Add tests for no-regression and managed subscription lifetime**

Using reflection only for internal invariants already permitted by Stage 3 tests, verify:

- Native mode leaves managed target handlers unsubscribed.
- Managed mode adds at most one handler per tracked target.
- toggling Native ↔ Managed repeatedly does not accumulate handlers.
- empty caption removes managed target subscription.
- disposing Tooltip removes all managed subscriptions and still disposes the single native ToolTip exactly once.
- native caption association remains the source of truth.

Do not attempt to unit-test actual OS popup coordinates through screenshots/reflection. Geometry correctness belongs to pure engine tests; actual point honoring is covered by manual demo verification.

- [ ] **Step 7: Add an STA integration probe for canceled Popup re-issue**

Use a small form and visible target, invoke the private managed-show path through a test helper/reflection only if required, and assert it does not create a second native ToolTip instance or throw on both runtimes. Keep the test deterministic; do not synthesize mouse movement/timing.

The acceptance behavior requiring real hover timing/position is a manual scenario in Task 8.

- [ ] **Step 8: Run Tooltip tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapTooltip"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapTooltip"
```

Expected: all old Stage 3 tests plus new managed-placement tests pass.

- [ ] **Step 9: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipPositioning.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs
git commit -m "feat: add managed BootstrapTooltip placement"
```

---

# Task 4 — Build the Shared Themed ToolStripDropDown Overlay Host

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayToolStripRenderer.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlaySurfaceTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs`

**Interfaces:**
- Consumes: one caller-owned child Control, current theme, target DPI, concrete screen bounds.
- Produces: one interactive native popup shell used by `BootstrapPopover` only.

- [ ] **Step 1: Write failing surface ownership/layout tests**

Cover:

- no content by default;
- attach exactly one content control;
- reject disposed/already-parented content;
- detach returns same control and does not dispose it;
- replacing requires explicit detach first;
- logical padding scales at 96/120/144/168/192 DPI;
- `BorderRadius == -1` uses theme metric;
- explicit radius 0/+ scales;
- preferred size = content size + scaled padding + 2× border;
- tiny/zero content sizes remain non-negative.

- [ ] **Step 2: Implement `BootstrapOverlaySurface`**

Use `SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true)`.

Paint using current resolved surface palette:

```text
Background = theme.Colors.Surface
Border     = theme.Colors.Border
Foreground/content palette remains caller-content responsibility
```

Do not mutate caller content Font/ForeColor/BackColor automatically; interactive content remains application-owned. The surface supplies chrome only.

- [ ] **Step 3: Implement rounded Region lifetime**

When size/theme/DPI/radius changes:

1. build rounded path with existing `RoundedPath.Create`;
2. create new `Region` from path;
3. swap `Region`;
4. dispose previous Region after replacement.

Do not retain `GraphicsPath` objects beyond the method scope.

- [ ] **Step 4: Write failing dropdown host tests**

Verify by reflection/internal visibility:

- exactly one ToolStripControlHost exists;
- exactly one BootstrapOverlaySurface is hosted;
- dropdown does not own caller content directly;
- `AutoClose` can be toggled;
- native renderer suppresses ToolStrip border/background;
- repeated show/close cycles reuse the same host/surface rather than allocate a new tree each time;
- disposing host after caller content is detached does not dispose caller content.

- [ ] **Step 5: Implement `BootstrapOverlayToolStripRenderer`**

Override only what is necessary:

```csharp
protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
{
    // Intentionally no native background; BootstrapOverlaySurface paints it.
}

protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
{
    // Intentionally no native border.
}
```

Do not duplicate Button/Dropdown renderer logic.

- [ ] **Step 6: Implement `BootstrapOverlayDropDown.ShowAt`**

`ShowAt(Rectangle screenBounds)` must:

- validate positive/non-negative size;
- size Surface, ControlHost, and ToolStripDropDown consistently;
- set/recreate top-level Region;
- call native `Show(screenBounds.Location)` only after size/region are applied.

Do not recompute placement here.

- [ ] **Step 7: Add Escape routing**

Override `ProcessCmdKey` so Escape invokes a supplied internal callback only when `CloseOnEscape` is active. Return `true` only when handled.

- [ ] **Step 8: Run host/surface tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapOverlaySurfaceTests|FullyQualifiedName~BootstrapOverlayDropDownTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlaySurfaceTests|FullyQualifiedName~BootstrapOverlayDropDownTests"
```

- [ ] **Step 9: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlaySurface.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayDropDown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayToolStripRenderer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlaySurfaceTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayDropDownTests.cs
git commit -m "feat: add interactive overlay popup host"
```

---

# Task 5 — Implement BootstrapPopover Content, Target, Trigger, and Placement

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopoverTrigger.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:**
- Consumes: `BootstrapOverlayDropDown`, `BootstrapOverlayPlacementEngine`, target screen bounds, caller content.
- Produces: public interactive `BootstrapPopover` component.

- [ ] **Step 1: Implement constructor and container ownership**

Pattern must match existing component conventions:

```csharp
public BootstrapPopover()
{
    _surface = new BootstrapOverlaySurface();
    _dropDown = new BootstrapOverlayDropDown(_surface);
    _dropDown.Opened += OnDropDownOpened;
    _dropDown.Closed += OnDropDownClosed;
}

public BootstrapPopover(IContainer container)
    : this()
{
    if (container is null)
    {
        throw new ArgumentNullException(nameof(container));
    }

    container.Add(this);
}
```

The caller container owns only `BootstrapPopover`; Popover owns its internal overlay infrastructure.

- [ ] **Step 2: Implement Target assignment lifecycle**

On Target change:

1. if open, hide first;
2. detach old Target Click/Disposed handlers;
3. assign new target unless disposed;
4. attach handlers according to Trigger;
5. never dispose/reparent Target.

`Trigger` changes while a Target is assigned must rewire handlers exactly once.

- [ ] **Step 3: Implement Content assignment lifecycle**

Setter behavior:

```text
if same reference       => no-op
if IsOpen               => throw InvalidOperationException
if new content disposed => throw ArgumentException/InvalidOperationException consistently
if new content parented => throw InvalidOperationException
Detach old content from surface without disposing
Detach old Disposed handler
Attach new content to surface
Attach new Disposed handler
```

When content is externally disposed, close if needed, detach, and set backing field null.

- [ ] **Step 4: Implement appearance/layout properties**

Validate and forward `ContentPadding`/`BorderRadius` to `_surface`; `Offset`/`BoundaryPadding` remain logical values used only during `Show`/reposition.

Default content padding:

```csharp
private static Padding CreateDefaultContentPadding()
{
    var metrics = BootstrapThemeMetrics.Default;
    return new Padding(
        metrics.SpacingMD,
        metrics.SpacingSM,
        metrics.SpacingMD,
        metrics.SpacingSM);
}
```

If the current metric names differ, use the existing values that resolve to 12 horizontal / 8 vertical logical pixels; do not add new theme metrics just for this plan.

- [ ] **Step 5: Implement `Show` geometry path**

Pseudo-structure:

```csharp
public void Show()
{
    ThrowIfDisposed();
    if (_dropDown.Visible)
    {
        return;
    }

    var target = RequireLiveTarget();
    var content = RequireLiveContent();

    ApplyCurrentThemeAndDpi(target);
    content.PerformLayout();
    var popupSize = _surface.GetPreferredSize(Size.Empty);
    var bounds = CalculatePopupBounds(target, popupSize);

    _dropDown.AutoClose = _closeOnClickOutside;
    _dropDown.CloseOnEscape = _closeOnEscape;
    _dropDown.ShowAt(bounds);
}
```

`CalculatePopupBounds` is an adapter only:

- RectangleToScreen
- GetTargetDpi
- scale offset/padding
- Screen.FromRectangle(...).WorkingArea
- engine Compute

No duplicate flip/shift arithmetic may live in `BootstrapPopover`.

- [ ] **Step 6: Implement Hide/Toggle/events**

`Hide()` closes the native dropdown only when visible. `Opened` and `Closed` are raised from the actual `_dropDown.Opened`/`Closed` events, not eagerly from the public methods.

- [ ] **Step 7: Implement Click/Manual trigger**

Target Click:

```csharp
private void OnTargetClick(object? sender, EventArgs e)
{
    if (_trigger != BootstrapPopoverTrigger.Click || !ReferenceEquals(sender, _target))
    {
        return;
    }

    Toggle();
}
```

Manual mode must not retain the Click handler.

- [ ] **Step 8: Implement focus entry and Escape restoration**

After native Opened:

- find the first visible/enabled/tab-stop descendant under `Content` in normal child/tab order;
- call `Select()`/`Focus()` only if one exists;
- otherwise keep target focus.

Escape closure path:

- close popup;
- after close, if Target is still visible/enabled/non-disposed and no outside-click focus transfer is in progress, return focus to Target.

Native AppClicked/ItemClicked outside-close reasons must not restore focus.

- [ ] **Step 9: Complete Popover behavior tests**

Cover:

- Click toggles exactly once per target activation;
- Manual ignores target click;
- Show/Hide/Toggle idempotence;
- concrete engine result is used for position adapter;
- right-to-left target passes RTL=true to engine adapter;
- `CloseOnClickOutside` updates native `AutoClose`;
- Escape closes once;
- first focusable content receives focus after open;
- non-focusable content leaves target focus intact;
- outside-close does not steal focus back;
- content controls remain usable after close/reopen;
- content survives Popover disposal.

- [ ] **Step 10: Run Popover tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapPopover"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapPopover"
```

- [ ] **Step 11: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopoverTrigger.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "feat: add interactive BootstrapPopover"
```

---

# Task 6 — Track Anchor Movement, Scroll, Theme, and DPI While Popover Is Open

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs`

**Interfaces:**
- Consumes: Target and its current parent/Form chain.
- Produces: reposition/close callbacks while Popover is open.

- [ ] **Step 1: Write failing tracker subscription tests**

Create nested controls:

```text
Form
  Panel (AutoScroll=true)
    Panel
      Button target
```

Assert tracker reacts to:

- target LocationChanged/SizeChanged;
- scroll panel Scroll;
- form Move/Resize;
- target ParentChanged rebuild;
- target Visible=false requests close;
- target disposal requests close;
- disposing tracker removes all reactions.

Use counters, not screen-position assertions.

- [ ] **Step 2: Implement ancestor subscription rebuild**

Keep a `List<Control>` / `List<ScrollableControl>` of subscribed ancestors. On rebuild:

1. unsubscribe previous chain;
2. walk `target.Parent` until null;
3. subscribe LocationChanged/SizeChanged;
4. additionally subscribe Scroll for `ScrollableControl`;
5. capture containing Form and subscribe Move/Resize/FormClosed.

No static/global subscriptions.

- [ ] **Step 3: Integrate tracker into Popover open/close lifecycle**

On actual `Opened`, create tracker. On actual `Closed`, dispose it.

Reposition callback:

```csharp
private void RepositionOpenPopover()
{
    var target = _target;
    if (target is null || target.IsDisposed || !target.Visible || !_dropDown.Visible)
    {
        Hide();
        return;
    }

    ApplyCurrentThemeAndDpi(target);
    var bounds = CalculatePopupBounds(target, _surface.GetPreferredSize(Size.Empty));
    _dropDown.MoveTo(bounds);
}
```

`MoveTo` updates Size/Region/location without closing/reopening so Opened/Closed do not fire on reposition.

- [ ] **Step 4: Handle runtime theme changes only while open**

Subscribe to `BootstrapThemeManager.ThemeChanged` when Popover opens and unsubscribe when it closes. On theme change:

- reapply surface theme;
- recompute preferred size if metrics/radius changed;
- recompute placement because size may change;
- invalidate surface.

This transient subscription avoids retaining closed/disposed Popovers through the static event.

- [ ] **Step 5: Handle target DPI changes through existing WinForms signals**

Do not add Win32 hooks. On target/form size/location/parent events and before each reposition, read current `Target.DeviceDpi`; always recompute scaled padding/offset/boundary padding from logical values.

Manual per-monitor movement remains required because `DeviceDpi`/DPI-change event behavior differs by runtime/OS.

- [ ] **Step 6: Add leak/lifetime tests**

Verify across 50 open/close cycles:

- one tracker at most while open;
- no static theme subscription remains after close;
- no ancestor event subscription remains after close/dispose;
- Target/Content can be garbage-collected after Popover disposal when no application references remain (use weak references only if stable across both target test runners; otherwise use handler-count/reflection invariants).

- [ ] **Step 7: Run focused tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapOverlayAnchorTracker|FullyQualifiedName~BootstrapPopover"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapOverlayAnchorTracker|FullyQualifiedName~BootstrapPopover"
```

- [ ] **Step 8: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapOverlayAnchorTracker.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPopover.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapOverlayAnchorTrackerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPopoverTests.cs
git commit -m "feat: keep BootstrapPopover anchored while open"
```

---

# Task 7 — Integrate Tooltip Placement and Popover into Feedback Demo

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

**Interfaces:**
- Consumes: current Feedback demo, managed Tooltip, new Popover.
- Produces: manual/automated examples for placement, collision, interactive content, theme, and lifecycle.

- [ ] **Step 1: Extend Tooltip section with managed-placement targets**

Add at least these buttons:

```text
Managed Top
Managed BottomEnd
Managed Auto near edge
Native baseline
```

Use separate `BootstrapTooltip` instances when configuration differs, all owned by `_components`.

Examples:

```csharp
_managedTopTooltip = new BootstrapTooltip(_components)
{
    Positioning = BootstrapTooltipPositioning.Managed,
    Placement = BootstrapOverlayPlacement.Top,
    CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
};
```

Keep the existing default/semantic/custom/multiline/long-text Stage 3 matrix intact.

- [ ] **Step 2: Add an edge/collision sandbox**

Create a fixed-size bordered Panel with targets near all four edges/corners. Associate managed Tooltip placements so manual testing can verify:

- Top flips to Bottom when no space above;
- Bottom flips to Top;
- wide popup shifts horizontally;
- Left/Right flip at side edges;
- Auto picks a visible side.

Do not fake geometry with hard-coded screen coordinates; place the sandbox controls near panel/form edges and move the demo window to monitor edges during manual verification.

- [ ] **Step 3: Add interactive Popover content**

Create a caller-owned panel/user control (not added to the Form Controls collection) containing at minimum:

- Label heading;
- TextBox;
- CheckBox;
- BootstrapButton or native Button action;
- Close button calling `_interactivePopover.Hide()`.

Assign it to one `BootstrapPopover` owned by `_components`.

Add a status Label outside the popup so the button/checkbox can prove interaction changed application state.

- [ ] **Step 4: Add placement controls for Popover**

Add commands that change:

- Placement: Auto / Top / Bottom / Left / Right;
- CollisionBehavior: None / Flip / Shift / FlipAndShift;
- CloseOnClickOutside;
- CloseOnEscape.

If a Popover is open, hide before changing Content only; placement/collision properties may trigger immediate reposition when open.

- [ ] **Step 5: Extend demo tests**

Assert:

- existing Stage 3 Tooltip matrix remains present;
- at least one Tooltip uses `Positioning.Managed`;
- managed examples cover explicit and Auto placement;
- exactly one interactive Popover exists in the demo component fields/container;
- Popover content contains focusable controls;
- interacting with content updates the external status label;
- form disposal disposes Popover/Tooltip components but not twice;
- caller-owned Popover content is detached and disposed only by the demo after Popover is disposed, or is disposed separately by the demo if the demo intentionally owns it.

Make demo ownership explicit: because `BootstrapPopover` does not own Content, `FeedbackDemoForm.Dispose` must dispose its dedicated content control after `_components.Dispose()` detaches it.

- [ ] **Step 6: Commit demo changes**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
git commit -m "demo: showcase tooltip placement and popover content"
```

---

# Task 8 — Documentation and Public API Baseline

**Files:**
- Modify: `README.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md` if overlay-specific manual guidance needs a permanent home
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

**Interfaces:**
- Consumes: finalized implementation/public types.
- Produces: documented frozen API and architecture boundary.

- [ ] **Step 1: Document shared overlay infrastructure in ARCHITECTURE**

Add a small dependency section:

```text
Controls (BootstrapTooltip / BootstrapPopover)
        |
        +--> Rendering/BootstrapOverlayPlacementEngine
        |
        +--> existing Theme + Rendering + DPI helpers

BootstrapPopover only
        |
        +--> internal ToolStripDropDown overlay host
```

State that the placement engine is a foundation rendering utility and must not depend on concrete controls.

- [ ] **Step 2: Update component contracts**

In `docs/COMPONENTS.md`, update BootstrapTooltip and add BootstrapPopover.

Tooltip documentation must clearly state:

- `Native` remains default/backward-compatible;
- `Managed` uses native Tooltip timing/association/drawing but explicit computed location;
- managed positioning supports placement/collision/offset/boundary padding;
- Tooltip remains text-only/non-focusable.

Popover documentation must state:

- arbitrary caller-owned unparented Control content;
- content is reparented into internal overlay surface but never disposed;
- Click/Manual trigger;
- placement/collision semantics;
- Escape/outside-click behavior;
- focus and anchor tracking;
- no custom Form/global hooks.

- [ ] **Step 3: Update README component list/examples**

Add short examples only after final API matches code:

```csharp
var tooltip = new BootstrapTooltip
{
    Positioning = BootstrapTooltipPositioning.Managed,
    Placement = BootstrapOverlayPlacement.Top,
    CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
};
tooltip.SetToolTip(saveButton, "Save changes");
```

And:

```csharp
var popover = new BootstrapPopover
{
    Target = optionsButton,
    Content = optionsPanel,
    Placement = BootstrapOverlayPlacement.Auto
};
```

- [ ] **Step 4: Update frozen public API baseline tests**

Add exact enum/type/property/event/method signatures. Continue asserting Tooltip does not expose native implementation details or arbitrary Content.

Add explicit baseline coverage that `BootstrapPopover.Content` is a `Control` reference and that overlay host/engine request/result types remain internal.

- [ ] **Step 5: Update `docs/PUBLIC_API_BASELINE.md`**

Record the new public enums and component members exactly as implemented, including defaults/ownership notes where the baseline document format supports them.

- [ ] **Step 6: Run API/docs-related tests**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

- [ ] **Step 7: Commit**

```powershell
git add README.md docs/ARCHITECTURE.md docs/COMPONENTS.md docs/TESTING.md docs/PUBLIC_API_BASELINE.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document overlay positioning and popover API"
```

---

# Task 9 — Full Verification, Manual Collision Matrix, and Resource Stress

**Files:**
- Modify only files required to fix defects found by verification.
- No new API unless a verified defect cannot be fixed inside the planned contract; any such API change requires re-review of Task 8 baseline first.

**Interfaces:**
- Consumes: complete feature.
- Produces: release-ready verified state.

- [ ] **Step 1: Run full Release tests on net48**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

Expected: PASS.

- [ ] **Step 2: Run full Release tests on net8.0-windows**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 3: Build demo on both targets**

Use the demo project's supported target commands from the repository. At minimum build the full solution/relevant demo configuration for both product TFMs and confirm no target-specific API was introduced accidentally.

- [ ] **Step 4: Manual Tooltip managed-placement matrix**

For each placement family Top/Bottom/Left/Right plus Auto:

1. hover a target in the center of the working area;
2. move demo window so target approaches corresponding monitor edge;
3. confirm Flip changes side only when needed;
4. confirm Shift keeps the chosen side but moves cross-axis position into view;
5. confirm FlipAndShift does both in order;
6. confirm None allows intentional overflow;
7. compare with Native positioning to verify the old default remains unchanged;
8. verify InitialDelay/ReshowDelay/AutoPopDelay still behave through the native Tooltip scheduler;
9. verify mouse leave/click hides managed Tooltips promptly;
10. verify repeated hover does not duplicate popups or leak target handlers.

- [ ] **Step 5: Manual Popover interaction matrix**

Verify:

- target Click opens/closes exactly once;
- Manual mode ignores target Click;
- TextBox typing works;
- checkbox/button activation works;
- Tab can move among focusable content controls;
- Escape closes and restores Target focus;
- outside click closes when enabled and focus goes to clicked control;
- outside click does not close when disabled;
- moving/resizing the Form repositions without close/open event churn;
- scrolling an ancestor keeps the Popover anchored;
- hiding/disposing Target closes safely;
- external Content disposal closes safely;
- changing Light/Dark while open repaints/repositions;
- repeated open/close preserves caller content state.

- [ ] **Step 6: DPI matrix**

Run at real Windows scale settings:

```text
100% = 96 DPI
125% = 120 DPI
150% = 144 DPI
175% = 168 DPI
200% = 192 DPI
```

Check:

- logical Offset and BoundaryPadding scale consistently;
- surface padding/radius/border scale consistently;
- placement remains anchored after DPI transitions;
- content does not clip due stale preferred size;
- moving the window between monitors with different DPI recalculates Popover position/size.

- [ ] **Step 7: Multi-monitor negative-coordinate matrix**

With a monitor positioned left/up of the primary monitor:

- place the demo near each outer working-area edge;
- verify negative screen coordinates do not wrap/teleport;
- verify Screen working area (not primary screen) is used;
- verify Auto/Flip/Shift remain deterministic.

- [ ] **Step 8: Resource stress**

Run at least 500 cycles:

```text
open Popover
interact
close Popover
show managed Tooltip on several targets
switch placement
switch Light/Dark periodically
```

Observe process USER/GDI handle counts and ensure they return near steady state after idle. Confirm:

- no unbounded Region/GraphicsPath/GDI growth;
- no growing static ThemeChanged subscription count;
- no duplicate Target/ancestor handlers;
- no post-disposal callbacks/exceptions;
- caller Content remains undisposed until the caller disposes it.

- [ ] **Step 9: Search for scope violations**

Confirm there is no new:

```text
Form subclass used as Tooltip/Popover popup
Popper/Floating UI package/dependency
second theme manager
second DPI scaler
global mouse/keyboard hook
HTML/rich-content parser
public native ToolTip exposure
public Tooltip Show/Hide
focusable Tooltip content
```

- [ ] **Step 10: Final repository status and diff review**

```powershell
git status --short
git diff --check
git log --oneline --decorate -10
```

Expected:

- clean working tree after final fixes/commit;
- no whitespace errors;
- commit history follows the task boundaries in this plan.

If verification fixes were required, commit them with a scoped message such as:

```powershell
git commit -am "fix: harden overlay popup lifecycle"
```

Do not use a blanket final commit if there are no changes.

---

## Acceptance Checklist

Implementation is complete only when all items below are true:

- [ ] `BootstrapOverlayPlacementEngine` is pure, deterministic, shared, and tested on both TFMs.
- [ ] All explicit placement values produce correct base geometry.
- [ ] Auto uses deterministic least-overflow/best-visible-area selection.
- [ ] Flip uses exact opposite placement; Shift preserves side; FlipAndShift applies in order.
- [ ] RTL Start/End behavior is explicitly tested.
- [ ] Negative desktop coordinates and oversized popups are handled without integer wrap.
- [ ] `BootstrapTooltip.Positioning` defaults to Native and old Stage 3 behavior remains intact.
- [ ] Managed Tooltip uses the same single native `ToolTip`, the same caption association, the same native timing properties, and the same owner-draw path.
- [ ] Managed Tooltip does not expose public Show/Hide or interactive content.
- [ ] `BootstrapPopover` can host arbitrary focusable caller content and never disposes that content.
- [ ] Popover Click and Manual trigger modes work without duplicate handlers.
- [ ] Escape/outside-click semantics are correct and focus restoration is reason-aware.
- [ ] Popover remains anchored while target/form/scroll ancestors move.
- [ ] Light/Dark changes while open repaint and reflow without leaking static subscriptions.
- [ ] 96/120/144/168/192 DPI tests/manual checks pass.
- [ ] Multi-monitor working-area collision handling passes.
- [ ] Existing Feedback demo cases remain intact and new placement/interactive scenarios are added.
- [ ] Public API baseline/documentation reflects the finalized contract.
- [ ] Full Release suite passes for `net48` and `net8.0-windows`.
- [ ] Resource stress shows no unbounded GDI/USER/event subscription growth.

---

## Rationale Summary

This plan intentionally avoids two tempting but high-risk designs:

1. **Making Tooltip itself interactive.** That would blur focus semantics, require a second popup host inside an already working native Tooltip abstraction, and conflict with the conventional accessibility model. Popover is the correct interactive surface.
2. **Replacing native Tooltip entirely to gain placement control.** The existing component already delegates association, delays, activity, and drawing to one stable native `ToolTip`. The managed mode instead uses the cancelable native `Popup` event as the timing trigger and re-issues the same tooltip at a computed point. Default Native mode remains untouched.

The resulting reusable boundary is narrow:

```text
pure placement/collision geometry
        ↑
        ├── BootstrapTooltip (native popup, opt-in explicit point)
        └── BootstrapPopover (interactive ToolStripDropDown popup)
```

That satisfies the requested Popper-like placement/collision behavior while preserving the repository's existing compatibility, ownership, and shared-infrastructure principles.