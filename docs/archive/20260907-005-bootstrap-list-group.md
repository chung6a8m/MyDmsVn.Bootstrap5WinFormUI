# BootstrapListGroup Implementation Plan

> **Status:** Proposed
> **Date:** 2026-09-07
> **Scope:** Add a lightweight Bootstrap-inspired List Group composition control for WinForms without turning it into a second `ListView`.

## Goal

Implement `BootstrapListGroup` and `BootstrapListGroupItem` as lightweight composition controls for short, mostly static lists such as settings navigation, command lists, summaries, and grouped content.

The implementation must preserve the project's existing design philosophy:

- keep normal WinForms child-control ownership, focus, events, accessibility, disposal, and designer behavior;
- let the framework own Bootstrap-inspired presentation, theme integration, DPI scaling, connected borders, and interaction states;
- reuse existing shared theme/rendering infrastructure and `BootstrapVariant` rather than create a parallel color system;
- support rich item content through normal child controls instead of introducing a heavy item data model;
- keep `BootstrapListGroup` clearly distinct from `BootstrapListView`.

## Why this control exists

`BootstrapListGroup` fills the lightweight composition niche:

```text
BootstrapListGroup
├─ a few static/composed items
├─ settings/navigation entries
├─ contextual status rows
├─ command/action rows
└─ arbitrary child content inside each item
```

It must **not** become the solution for large data sets:

```text
5,000 products / columns / sorting / virtualization
    -> BootstrapListView (or BootstrapDataGridView)

hierarchical data
    -> BootstrapTreeView
```

This boundary is important because List Group, ListView, and TreeView are separate abstractions with different ownership and performance models.

## Bootstrap semantics to carry over

Use Bootstrap 5.3 List Group as the visual/behavioral reference:

- normal list-group items;
- active item state;
- disabled item state;
- actionable items;
- contextual variants;
- flush presentation;
- horizontal presentation;
- custom/rich content;
- composition with existing controls such as `BootstrapBadge`.

Reference: <https://getbootstrap.com/docs/5.3/components/list-group/>

Do not copy web-only mechanics such as CSS responsive breakpoint class names or DOM anchor/button distinctions into the public WinForms API.

## Repository implementation references

Use existing controls as implementation references instead of inventing parallel conventions:

- `BootstrapListView` for shared `BootstrapVariant` validation/resolution and for the boundary between a native data/list control and this lightweight composition control;
- `BootstrapBadge` for theme-font behavior, non-interactive `ControlStyles.Selectable` handling, accessibility defaults, DPI-aware sizing, and theme subscription disposal;
- `BootstrapAccordion` for composite child ownership/event-subscription patterns, while **not** copying an ordered cache when `Controls` child-index order must remain authoritative.

---

## Architectural decisions

### 1. Use real child controls; `Controls` owns both membership and order

`BootstrapListGroup` derives from `Panel` and arranges direct `BootstrapListGroupItem` child controls. `BootstrapListGroupItem` also derives from `Panel` so it can host arbitrary content.

`Controls` is the authoritative source for both:

- item ownership/membership;
- item display/navigation order.

Do **not** maintain an ordered `_items` list whose order is updated only by `OnControlAdded` / `OnControlRemoved`, because WinForms can reorder children with `Controls.SetChildIndex`, `BringToFront`, or `SendToBack` without an add/remove cycle.

Instead:

- keep only internal membership/subscription tracking if needed for event wiring and disposal;
- synchronize that tracking from `OnControlAdded` / `OnControlRemoved`;
- derive `Items`, layout order, first/middle/last roles, and keyboard-navigation order from a fresh snapshot of direct `BootstrapListGroupItem` children in the current `Controls` child-index order;
- ensure reordering is observed on the next normal layout/navigation operation without requiring remove/re-add;
- expose `Items` as a read-only snapshot, not a mutable secondary collection;
- provide `AddItem`, `RemoveItem`, and `ClearItems` convenience methods that route through `Controls`.

This keeps ownership and ordering from drifting apart.

### 2. `Active` is explicit presentation state, not automatic selection

A Bootstrap List Group is not a WinForms selection/data control. V1 must not introduce:

- `SelectedIndex`;
- `SelectedItem`;
- single/multiple selection modes;
- automatic activation when an item is clicked.

Each item owns an explicit `Active` property. Applications decide when to change it. Multiple items may technically be active because the control does not impose a selection policy.

This avoids semantic overlap with `BootstrapListView` and keeps navigation-state ownership with the application.

### 3. `Actionable` owns both `TabStop` and actual WinForms selectability

An item can be purely presentational or actionable.

`Panel` disables `ControlStyles.Selectable` by default, so merely setting `TabStop = true` is **not sufficient** to make an actionable `BootstrapListGroupItem` focusable.

When `Actionable == false`:

- set `TabStop = false`;
- disable `ControlStyles.Selectable` for the item itself;
- render no hover/pressed action styling;
- independently interactive child controls may still receive focus and input.

When `Actionable == true`:

- enable `ControlStyles.Selectable`;
- set `TabStop = true`;
- when `Enabled == true` and the normal WinForms parent/visibility conditions are satisfied, `CanSelect` must become true;
- mouse hover/pressed/focus states are rendered;
- mouse/keyboard activation raises the item's normal `Click` once;
- the parent raises one convenience `ItemClick` carrying the item.

When `Enabled == false`:

- disabled styling wins;
- the item cannot activate;
- `CanSelect` follows normal WinForms disabled semantics;
- group keyboard navigation skips it.

Changing `Actionable` at runtime must update both selectability and tab behavior. Do not suppress normal child-control interaction merely because the item itself is not actionable.

### 4. Rich content uses normal composition, with deterministic activation forwarding

`BootstrapListGroupItem.Text` provides the simple one-line case, but an item may host arbitrary child controls.

Examples:

- `Label` for heading/description;
- `BootstrapBadge` for counts/status;
- non-interactive icon/presentation controls already supported by the project;
- compact panels/layout controls for richer rows;
- independently interactive controls such as buttons, links, checkboxes, text boxes, or selectors.

Do **not** add ListGroup-specific properties such as `BadgeText`, `BadgeVariant`, `SecondaryText`, or a bespoke content object in V1.

For an actionable item, child input follows these rules:

1. **Decorative/non-interactive descendants** may forward mouse activation to the owning item so labels, badges, icons, or layout-surface backgrounds do not create dead zones inside an otherwise clickable row.
2. **Interactive descendants** keep their own input semantics and must not automatically activate the parent item.
3. **Unknown/custom child control types default to preserving their own semantics**, not forwarding, unless they satisfy a small explicit internal decorative rule.
4. The initial decorative rule should cover the project's known presentation-only controls (`Label`, `BootstrapBadge`, and other explicitly non-selectable presentation controls discovered during implementation) plus non-interactive layout-container background surfaces where safe.
5. If forwarding is implemented by event subscription, recursively track relevant child `ControlAdded` / `ControlRemoved` changes and unsubscribe deterministically on removal/disposal.
6. Forwarded activation must still produce exactly one item `Click` and exactly one parent `ItemClick`.

This rule keeps the documented rich-content examples clickable without stealing interaction from real child controls.

### 5. Parent owns connected geometry and the only public radius override

The group is responsible for each item's connected-edge role:

```text
Vertical:
[first ] top corners rounded
[middle] square connected edges
[last  ] bottom corners rounded

Horizontal:
[first ][middle][last]
left corners         right corners
```

The group owns the public `BorderRadius` override. `BootstrapListGroupItem` does **not** expose a separate public `BorderRadius` in V1.

Semantics:

- `BootstrapListGroup.BorderRadius == -1` uses the current theme radius;
- a non-negative group radius applies to the outer corners of the first/last visible connected items;
- middle connected corners remain square regardless of the group radius;
- vertical `Flush == true` visually suppresses outer rounding/borders as specified below without mutating `BorderRadius`;
- an item used standalone falls back to the current theme radius because it has no group-level override.

The item renders its own surface using internal geometry/state supplied by the group, but group layout determines:

- first/middle/last visible position;
- outer corner mask;
- effective radius;
- border/seam overlap;
- item bounds;
- flush behavior.

Avoid double-thick borders between adjacent items. Reuse connected-border principles already used by grouped controls rather than adding spacing to hide seams.

### 6. Theme and DPI are infrastructure, not local constants

Use:

- `BootstrapThemeManager.CurrentTheme`;
- existing `BootstrapThemeColors` / semantic color resolution;
- `BootstrapVariantColorResolver` or the current shared variant resolver used by existing controls;
- `BootstrapThemeMetrics`;
- `DpiScaler`;
- `BootstrapVariant`;
- existing rounded-path/rendering helpers where applicable.

Do not hard-code Bootstrap hex colors or fixed pixel metrics in the control.

### 7. Contextual variant is optional, but compatibility is a Task 1 gate

The existing `BootstrapVariant` has semantic values but no neutral/default member. Preferred V1 API:

```csharp
public BootstrapVariant? Variant { get; set; }
```

Semantics:

- `null` = normal neutral List Group item;
- non-null = contextual List Group item using the shared semantic variant.

However, nullable-enum PropertyGrid/designer serialization must be validated **before the public API is locked**. Task 1 contains an explicit compatibility spike.

If `BootstrapVariant?` is genuinely unusable in the supported WinForms designer path:

- record the concrete failing behavior/test/manual reproduction;
- choose the smallest designer-safe fallback in Task 1;
- update all subsequent contract tests and plan references before rendering/layout work begins;
- do not defer this API decision until Task 8;
- do not introduce a duplicate semantic enum without evidence that the preferred shared nullable enum cannot satisfy the supported designer contract.

### 8. Flush and horizontal behavior

Bootstrap web documents horizontal List Groups separately from flush List Groups and does not currently combine them.

For V1:

- `Orientation = Vertical` by default;
- `Orientation = Horizontal` is supported;
- `Flush` removes the outer group border/radius in vertical layout and keeps separators between items;
- when horizontal, `Flush` has no visual effect and must not mutate the property or throw during designer editing;
- document and test this rule explicitly.

Do not add Bootstrap responsive breakpoint variants such as `sm`, `md`, `lg`, `xl`, or `xxl` to the WinForms API.

---

## Proposed public API

The exact XML documentation can be refined during implementation, but after the Task 1 nullable-enum compatibility gate, the V1 public surface should remain close to the following.

### `BootstrapListGroup`

```csharp
[DefaultEvent(nameof(ItemClick))]
public class BootstrapListGroup : Panel
{
    public Orientation Orientation { get; set; }       // default Vertical
    public bool Flush { get; set; }                    // default false
    public int BorderRadius { get; set; }              // default -1 = theme

    [Browsable(false)]
    public IReadOnlyList<BootstrapListGroupItem> Items { get; }

    public event EventHandler<BootstrapListGroupItemEventArgs>? ItemClick;

    public BootstrapListGroupItem AddItem(string text);
    public void AddItem(BootstrapListGroupItem item);
    public bool RemoveItem(BootstrapListGroupItem item);
    public void ClearItems();
}
```

### `BootstrapListGroupItem`

```csharp
[DefaultEvent(nameof(Click))]
[DefaultProperty(nameof(Text))]
public class BootstrapListGroupItem : Panel
{
    public bool Active { get; set; }                   // default false
    public bool Actionable { get; set; }               // default false
    public BootstrapVariant? Variant { get; set; }     // preferred API, default null
    public bool UseThemeFont { get; set; }             // follow existing control convention

    // Text, Enabled, Padding, Controls, Font, AccessibleName, etc.
    // remain normal WinForms properties.
}
```

`BootstrapListGroupItem` intentionally has no public `BorderRadius` in V1. Connected/standalone radius is resolved internally according to Architectural Decision 5.

### `BootstrapListGroupItemEventArgs`

```csharp
public sealed class BootstrapListGroupItemEventArgs : EventArgs
{
    public BootstrapListGroupItemEventArgs(BootstrapListGroupItem item);
    public BootstrapListGroupItem Item { get; }
}
```

During implementation, prefer internal state/geometry/activation-classification types over expanding public API for rendering-only concerns.

## Non-goals for V1

Do not implement any of the following unless a later plan explicitly adds them:

- virtualization;
- thousands-of-items performance model;
- owner-data mode;
- data binding engine / `DataSource` / `DisplayMember` / `ValueMember`;
- columns;
- native ListView views (`Details`, `Tile`, `LargeIcon`, etc.);
- sorting/filtering/search;
- groups inside groups;
- hierarchy/tree expansion;
- checked items;
- image-list model;
- drag/drop reorder policy;
- automatic active-item selection policy;
- responsive Bootstrap breakpoint APIs;
- animation;
- swipe/touch gesture abstractions.

Those concerns belong to other controls or later focused plans.

---

## Task 1 — Lock the public contract, selectability semantics, and designer-safe variant API

**Files:**

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItemEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Create or extend as appropriate: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Compatibility/BootstrapListGroupDesignerCompatibilityTests.cs`
- Modify/create during implementation: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify/create during implementation: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`

### Steps

1. Write failing tests for intended defaults:
   - group derives from `Panel`;
   - `Orientation == Orientation.Vertical`;
   - `Flush == false`;
   - group `BorderRadius == -1`;
   - `AutoSize`/`AutoSizeMode` behavior is explicitly asserted rather than accidental;
   - group is not a tab stop;
   - accessible role is a list/grouping role appropriate to WinForms;
   - item `Active == false`;
   - item `Actionable == false`;
   - preferred API item `Variant == null`;
   - item uses theme font according to the project convention;
   - presentational item has `TabStop == false` and cannot be selected as the row itself.
2. Add validation tests:
   - invalid `Orientation` enum value throws `InvalidEnumArgumentException` or the project-standard equivalent;
   - group `BorderRadius < -1` throws `ArgumentOutOfRangeException`;
   - invalid nullable `BootstrapVariant` value is rejected consistently with existing controls.
3. Add `BootstrapListGroupItemEventArgs` null guard and identity tests.
4. Add a selectability contract test using the repository's STA/WinForms test helper with the item parented to a visible/selectable host:
   - `Actionable = false` => `TabStop == false` and item row is not selectable;
   - `Actionable = true` => `TabStop == true`, `ControlStyles.Selectable` is effectively enabled, and `CanSelect == true` when normal WinForms prerequisites are satisfied;
   - toggling back to false removes the row from item-level keyboard navigation.
5. Implement the minimum `Actionable` setter behavior required by the test: keep `TabStop` and `ControlStyles.Selectable` synchronized.
6. Before locking the `Variant` public property, run a compatibility spike for `BootstrapVariant?`:
   - verify `TypeDescriptor` exposes the property correctly;
   - verify the nullable enum converter can round-trip `null` and every valid enum value;
   - verify the repository's supported designer serialization path, if there is an automated designer-host convention;
   - otherwise perform and document a focused Visual Studio PropertyGrid/designer smoke check for `null -> enum -> null` and generated-code round-trip;
   - validate on the supported target/tooling paths that are practical in the repository.
7. If the nullable enum fails concretely, choose and test the smallest compatible fallback **inside this task**, update the public contract once, and use that resolved API in all later tasks.
8. Run targeted tests and confirm the contract is green before starting rendering/layout work.

### Verification

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj --filter "FullyQualifiedName~BootstrapListGroup"
```

### Acceptance criteria

- Public defaults are intentional and test-covered.
- Public validation behavior is deterministic.
- Actionable rows are genuinely selectable/focusable, not merely `TabStop = true`.
- The final contextual-variant API is designer-safe and fixed before Task 2.
- The project compiles for supported targets.
- No ListView-like selection/data API is introduced.

---

## Task 2 — Implement WinForms child ownership, authoritative ordering, and item snapshots

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`

### Steps

1. Do **not** add an ordered `_items` list as an ordering source.
2. If event wiring needs tracking, use a membership/subscription structure whose only responsibility is preventing duplicate subscriptions and enabling deterministic unsubscribe.
3. Expose:

```csharp
[Browsable(false)]
[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
public IReadOnlyList<BootstrapListGroupItem> Items => ...;
```

where the returned snapshot is rebuilt from direct `Controls` children using the same current WinForms child-index order used by layout/navigation.
4. Synchronize membership/event subscriptions from `OnControlAdded` / `OnControlRemoved` only; never infer ordering from subscription order.
5. Implement:
   - `AddItem(string text)`;
   - `AddItem(BootstrapListGroupItem item)`;
   - `RemoveItem(BootstrapListGroupItem item)`;
   - `ClearItems()`.
6. Preserve normal WinForms semantics:
   - adding through `Controls.Add(item)` is reflected in `Items`;
   - removing through `Controls.Remove(item)` is reflected in `Items`;
   - helper methods route through `Controls` rather than a parallel collection;
   - `RemoveItem` does not dispose the item;
   - `ClearItems` removes but does not silently dispose items;
   - reparenting is reflected correctly.
7. Decide direct non-item child behavior consistently with existing composite controls. Prefer ignoring non-item direct children for ListGroup item layout instead of throwing in the designer, unless repository precedent clearly requires stricter validation.
8. Add ordering tests that explicitly exercise:
   - initial add order;
   - `Controls.SetChildIndex`;
   - `BringToFront` / `SendToBack` where meaningful for the host;
   - remove/re-add;
   - reparenting.
9. Assert after each reorder that `Items` snapshot order matches the authoritative child-index order, without requiring remove/re-add.
10. Add duplicate-subscription tests.

### Acceptance criteria

- `Controls` is the ownership **and ordering** source of truth.
- `Items` cannot become stale through normal add/remove/reparent/reorder flows.
- Reorder operations that do not raise add/remove still affect subsequent layout/navigation order.
- No collection operation causes double event subscription.
- Design-time child serialization remains normal WinForms child-control serialization.

---

## Task 3 — Extract deterministic List Group rendering/state logic

**Files:**

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`

### Responsibilities

Keep geometry/color-state decisions testable without creating visible windows.

The internal helper should cover only pure calculations such as:

- logical padding -> DPI-scaled padding;
- preferred text/content size;
- first/middle/last visible-item corner mask;
- vertical/horizontal seam overlap;
- neutral/contextual/active/disabled/hover/pressed/focused state precedence;
- border/background/foreground color selection from theme + resolved contextual variant API;
- effective group/standalone radius;
- flush edge rules.

Do not put WinForms event routing, mutable child ownership, or click-forwarding subscriptions into this helper.

### Required state precedence

Define and test an explicit precedence so combined states cannot accidentally produce unreadable colors:

```text
disabled
  > active
    > pressed (actionable)
      > hover (actionable)
        > contextual variant
          > neutral/default
```

Focus indication is an overlay/outline concern and must not destroy foreground/background contrast.

### Radius rules to test

- group radius `-1` resolves from the current theme;
- explicit non-negative group radius is DPI-scaled/used according to repository metric conventions;
- middle connected items receive square connected corners;
- first/last/single visible items receive only the parent-supplied outer corner mask;
- standalone item uses theme radius;
- vertical flush suppresses outer radius visually without mutating the public group property.

### Tests

Cover at minimum:

- neutral state;
- every supported semantic variant from the Task 1-resolved API;
- active + variant;
- disabled + active;
- actionable hover/pressed;
- first/middle/last/single-item corners in both orientations;
- vertical flush geometry;
- horizontal flush documented no-op behavior;
- 96/120/144/192 DPI scaling;
- group radius `-1` vs explicit radius;
- standalone item theme radius;
- zero/very small bounds do not create invalid drawing geometry.

### Acceptance criteria

- Geometry/state calculations are deterministic unit tests.
- Radius precedence is unambiguous; there is no competing public item radius.
- No hard-coded Bootstrap hex colors are introduced.
- DPI scaling is derived from project infrastructure.

---

## Task 4 — Implement `BootstrapListGroupItem` rendering and rich-content layout

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`

### Steps

1. Configure owner painting with project-standard styles:
   - `UserPaint`;
   - `AllPaintingInWmPaint`;
   - `OptimizedDoubleBuffer`;
   - `ResizeRedraw`;
   - transparent-background support where needed.
2. Preserve the Task 1 `Actionable` selectability contract while configuring styles; do not accidentally disable `Selectable` unconditionally in the constructor.
3. Implement theme font behavior consistent with `BootstrapBadge`:
   - theme typography by default;
   - user-assigned font can opt out according to existing `UseThemeFont` convention;
   - theme changes update only when theme font is enabled.
4. Implement `GetPreferredSize`:
   - text-only item measures `Text` plus theme/DPI padding;
   - hosted child controls contribute to preferred size;
   - explicit `Size`/non-AutoSize scenarios remain respected;
   - no negative/overflow dimensions.
5. Render:
   - background;
   - connected border edges;
   - internal parent-supplied outer corner mask/effective radius;
   - text for the simple-item case;
   - focus cue for actionable keyboard focus.
6. Rich-content layout rule:
   - if custom child controls exist, keep `Text` rendering predictable and non-overlapping;
   - reserve the normal content rectangle and allow child controls to be positioned/docked within it;
   - do not automatically relocate arbitrary child controls unless explicitly documented.
7. Make parent-supplied connected-edge geometry/radius internal rather than public.
8. Repaint/re-layout on:
   - `TextChanged`;
   - `FontChanged`;
   - `PaddingChanged`;
   - `EnabledChanged`;
   - `Active`/`Actionable`/resolved contextual-variant changes;
   - DPI changes;
   - theme changes.
9. Ensure theme subscription is released in `Dispose`.

### Acceptance criteria

- Text-only items look correct without extra child controls.
- Items can host `BootstrapBadge`, `Label`, layout containers, and ordinary interactive WinForms controls.
- Connected edges are not double-painted.
- The item has no competing public radius override.
- Theme/DPI changes do not require recreating the item.
- Disposal does not leak theme subscriptions.

---

## Task 5 — Implement group layout, connected edges, orientation, flush, and reorder resilience

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`

### Authoritative item sequence

Every layout pass must derive a fresh visible-item sequence from direct `BootstrapListGroupItem` children in current `Controls` child-index order.

That same sequence determines:

- bounds;
- first/middle/last roles;
- seam overlap;
- preferred-size aggregation;
- spatial keyboard-navigation order in Task 6.

No stale ordered cache may participate.

### Vertical layout

- Default orientation.
- Visible items are laid out top-to-bottom in authoritative child-index order.
- Items stretch to the group's usable client width when the group has an explicit width.
- Auto-size computes height from visible item preferred heights minus shared seam overlap.
- Hidden items consume no space and do not affect first/middle/last corner assignment.

### Horizontal layout

- Visible items are laid out left-to-right in authoritative child-index order.
- Preserve each item's preferred width by default; do not add equal-width behavior without a demonstrated use case.
- Shared vertical seams overlap by exactly one scaled border thickness.
- Auto-size computes width from item preferred widths minus seam overlap and height from the tallest visible item.

### Flush

Vertical `Flush == true`:

- no rounded outer item corners;
- no outer left/right group border where Bootstrap flush semantics require edge-to-edge presentation;
- separators remain between adjacent items;
- content padding remains unchanged unless theme semantics explicitly require otherwise;
- public `BorderRadius` is preserved and becomes effective again when `Flush` returns to false.

Horizontal `Flush == true`:

- render the same as non-flush horizontal V1;
- do not throw;
- do not silently rewrite `Flush` to false.

### Layout safety

Use a `_performingLayout` guard similar to existing composite controls to avoid recursive layout storms.

Add tests for:

- zero items;
- one item;
- multiple items;
- hidden first/middle/last items;
- changing orientation at runtime;
- changing item preferred size at runtime;
- changing group padding;
- explicit size vs AutoSize;
- DPI changes;
- repeated `PerformLayout` does not drift bounds by seam overlap;
- all child bounds remain non-negative;
- `Controls.SetChildIndex` immediately affects the next layout's visual order and corner roles;
- `Items`, layout order, and first/middle/last geometry agree after reorder.

### Acceptance criteria

- Connected borders remain visually one logical border thick.
- Layout is stable across repeated calls.
- Visible item order/corner roles remain correct after add/remove/hide/reorder without remove/re-add workarounds.
- Horizontal/vertical switch is safe at runtime and design time.

---

## Task 6 — Implement actionable mouse/keyboard behavior, child activation forwarding, and accessibility

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Create if useful: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupInteraction.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`

### Mouse behavior on the item surface

For actionable enabled items:

- track hover on mouse enter/leave;
- track pressed state only for a valid primary-button press/release cycle;
- raise normal `Click` once;
- parent forwards one `ItemClick` event;
- losing capture/focus clears transient pressed state.

### Decorative-child activation forwarding

Implement a small internal classification/forwarding mechanism with conservative defaults:

- known non-interactive presentation controls such as `Label` and `BootstrapBadge` forward a valid primary mouse activation to the owning actionable item;
- safe non-interactive layout-container background surfaces may forward where this does not interfere with descendant controls;
- buttons, links, checkboxes/radios/switches, text-entry controls, selectors, list/grid/tree controls, and other clearly interactive controls never auto-forward;
- unknown/custom control types default to **no forwarding**;
- a nested interactive child must not forward merely because its parent layout container can forward background clicks;
- recursively subscribe/unsubscribe only where needed as the child tree changes;
- child forwarding uses the same item activation path as direct mouse/keyboard activation so `Click` and `ItemClick` cannot double-fire.

Do not use an approach that disables child controls or globally intercepts all descendant input.

### Keyboard activation

For actionable enabled items:

- Enter activates the item;
- Space activates the item once using normal button-like semantics;
- disabled/non-actionable items do not activate.

The implementation must rely on the Task 1 selectability contract; do not assume `TabStop` alone makes a `Panel` focusable.

### Group navigation

Implement desktop-friendly spatial navigation only among direct actionable, enabled, visible items, using the same fresh authoritative child-index order as layout:

- Vertical: Up / Down;
- Horizontal: Left / Right;
- Home / End: first / last actionable item.

Rules:

- do not steal arrow keys from a focused interactive child control;
- navigation begins only when the item itself owns focus;
- Tab remains normal WinForms tab navigation and can leave the group;
- group navigation changes focus only, never `Active`.

### Accessibility

- Group exposes a list/grouping role and meaningful accessible description.
- Presentational items expose a list-item/static role.
- Actionable items expose an appropriate actionable role/state.
- `Active`, disabled, and focused states are reflected as far as WinForms accessibility allows without building a custom UIA provider in V1.
- `AccessibleName` defaults sensibly from `Text` when the caller has not supplied one.

### Tests

Use deterministic direct tests where possible. For tests requiring real focus/message pumping, use the repository's WinForms STA/message-loop infrastructure rather than ad-hoc threads.

Cover:

- actionable item synchronizes `TabStop` and real selectability;
- `Actionable = true` item can actually receive focus on a valid visible host;
- toggling actionable state changes item-level keyboard eligibility;
- disabled item cannot activate;
- Enter/Space activate exactly once;
- direct item click produces exactly one item `Click` and one parent `ItemClick`;
- clicking a `Label` inside an actionable item activates the parent exactly once;
- clicking a `BootstrapBadge` inside an actionable item activates the parent exactly once;
- clicking a safe decorative layout surface activates the parent where the chosen rule permits it;
- clicking a Button/CheckBox/TextBox/LinkLabel or other interactive child does not spuriously activate the parent;
- unknown custom child type defaults to no forwarding;
- dynamically added/removed decorative descendants are subscribed/unsubscribed without duplicate events;
- arrow/Home/End focus movement follows current `Controls` order after reorder;
- hidden/disabled/non-actionable items are skipped;
- Tab is not trapped;
- `Active` never changes automatically through keyboard/mouse activation.

### Acceptance criteria

- Keyboard-only navigation is usable because actionable items are genuinely selectable.
- Labels/badges/decorative row content do not create click dead zones.
- Interactive descendants preserve their own semantics.
- Every activation path raises item `Click`/parent `ItemClick` at most once.
- Tab can enter/leave the group normally.
- Interaction tests do not introduce modal dialogs or uncontrolled GUI waits.
- Active state remains application-owned.

---

## Task 7 — Theme, semantic variants, visual-state and contrast hardening

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`

### Steps

1. Map the neutral/default state from the Task 1-resolved contextual-variant API to theme-derived neutral List Group colors.
2. Map all shared semantic variants:
   - Primary;
   - Secondary;
   - Success;
   - Danger;
   - Warning;
   - Info;
   - Light;
   - Dark.
3. Reuse the existing shared variant resolver where applicable rather than duplicating palette logic.
4. Verify readable foreground/background choices in light and dark themes.
5. Ensure `Active` is visibly dominant over contextual variant while retaining semantic readability.
6. Ensure disabled state is visibly disabled and cannot look like hover/pressed.
7. Ensure hover/pressed appears only for actionable items.
8. Ensure focus indication remains visible in light/dark and active/contextual combinations.
9. Subscribe to `BootstrapThemeManager.ThemeChanged`; invalidate/re-layout as needed; unsubscribe on disposal.
10. Add regression tests that switching theme does not reset public state (`Active`, `Actionable`, resolved contextual variant, `Enabled`, `Flush`, `Orientation`, `BorderRadius`).

### Acceptance criteria

- No ListGroup-specific hard-coded palette exists.
- All semantic variants react to theme changes.
- State precedence is deterministic and test-covered.
- Disposal does not leave static theme event subscriptions behind.

---

## Task 8 — Designer/API compatibility regression and supported-target validation

The nullable/contextual variant **decision is already complete in Task 1**. Task 8 validates the finished control; it is not allowed to discover and redesign the public variant API late in implementation.

**Files:**

- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify if needed: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Compatibility/*`

### Steps

1. Verify public API uses types available on both `net48` and `net8.0-windows`.
2. Avoid newer BCL/WinForms APIs unless guarded by the repository's compatibility strategy.
3. Re-run the Task 1 designer/PropertyGrid serialization regression for the **already resolved** contextual variant API.
4. Verify adding/reordering `BootstrapListGroupItem` child controls in the designer produces stable generated code.
5. Verify changing `Actionable` in designer/runtime produces stable `TabStop`/selectability behavior and no design-time exceptions.
6. Verify group `BorderRadius` is the sole public radius override and serializes correctly.
7. Verify theme subscriptions do not execute design-time-only unsafe behavior.
8. Check `AutoSize`, `Dock`, `Anchor`, `Padding`, `Margin`, `Visible`, `Controls.SetChildIndex`, and reparenting behavior.
9. Build all supported target frameworks.

### Verification

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj --filter "FullyQualifiedName~BootstrapListGroup"
```

When the local environment supports .NET Framework test execution, also run the repository-standard `net48` validation path.

### Acceptance criteria

- `net48` and `net8.0-windows` source compatibility is preserved.
- Designer serialization is stable.
- The Task 1 contextual-variant contract still round-trips correctly.
- Reorder and Actionable/selectability behaviors remain designer-safe.
- No target-specific public API leak is added.

---

## Task 9 — Add an integrated demo

**Files:**

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListGroupDemoForm.cs`
- Modify: the demo application's existing navigation/launcher registration file(s) discovered during implementation
- Add/modify demo tests under: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/` when current demo testing conventions warrant it

### Demo scenarios

Show all important behavior on one focused form:

1. **Basic**
   - three normal vertical text items.
2. **Active + disabled**
   - one explicitly active item;
   - one disabled item;
   - show that clicking another actionable item does not auto-change active state unless demo code chooses to do so.
3. **Actionable settings navigation**

```text
Profile
Security
Notifications
```

   - Tab into/out of group;
   - Up/Down navigation;
   - Enter/Space activation;
   - status label showing which item emitted `ItemClick`.
4. **Reorder**
   - provide a small demo action that changes child order with `Controls.SetChildIndex`;
   - show that layout and keyboard navigation immediately follow the new order.
5. **Contextual variants**
   - Primary/Success/Danger/Warning/Info and remaining supported variants.
6. **Rich content**
   - heading + description via `Label`;
   - embedded `BootstrapBadge` count/status;
   - clicking decorative label/badge area activates the row;
   - include one real interactive child control and show that activating it does not also activate the row.
7. **Flush**
   - vertical edge-to-edge presentation.
8. **Horizontal**
   - three items;
   - Left/Right keyboard navigation;
   - document/show that `Flush` is a V1 no-op in horizontal mode.
9. **Theme switch**
   - verify the existing demo theme switch updates all List Group states correctly.

### Acceptance criteria

- Demo makes ListGroup vs ListView scope obvious.
- No demo-only API is added to the library.
- Keyboard interaction can be manually verified without mouse-only steps.
- Reorder, decorative forwarding, and interactive-child isolation are visible behaviors rather than test-only details.

---

## Task 10 — Documentation and API examples

**Files:**

- Modify: `docs/COMPONENTS.md`
- Modify: `README.md` only if the existing component catalog is maintained there
- Modify: `CHANGELOG.md` at implementation/release time according to repo policy

### Documentation requirements

Document:

- intended use cases;
- when to choose `BootstrapListGroup` vs `BootstrapListView` vs `BootstrapTreeView` vs `BootstrapDataGridView`;
- public API and defaults;
- `Controls` child-index order is authoritative for `Items`, layout, and keyboard navigation;
- `Active` is explicit state, not automatic selection;
- `Actionable` means the item is truly selectable/focusable as well as a tab stop;
- keyboard behavior;
- the Task 1-resolved neutral/contextual-variant semantics;
- group-only `BorderRadius` semantics;
- `Flush` behavior and horizontal limitation;
- rich-content composition with `BootstrapBadge`/`Label`;
- decorative child activation forwarding vs interactive child isolation;
- theme/DPI support;
- accessibility expectations;
- large-list/virtualization non-goal.

Include a concise example:

```csharp
var group = new BootstrapListGroup
{
    Dock = DockStyle.Top
};

var profile = group.AddItem("Profile");
profile.Actionable = true;
profile.Active = true;

var security = group.AddItem("Security");
security.Actionable = true;

var notifications = group.AddItem("Notifications");
notifications.Actionable = true;

group.ItemClick += (_, e) =>
{
    // Application owns navigation and Active-state policy.
};
```

### Acceptance criteria

- Documentation does not imply virtualization/data binding.
- Examples compile against the final Task 1-resolved public API.
- Component choice guidance prevents misuse for large data sets.
- Documentation does not imply arbitrary interactive children bubble activation to their parent item.

---

## Task 11 — Regression, hardening, and final verification

**Files:**

- Modify tests only as required by discovered regressions.

### Targeted tests

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj --filter "FullyQualifiedName~BootstrapListGroup"
```

### Full test suite

```powershell
dotnet test MyDmsVn.Bootstrap5WinFormUI.sln
```

### Build

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln
```

### Manual demo checklist

At 100%, 125%, 150%, and 200% DPI where practical:

- vertical connected borders are one logical border thick;
- first/middle/last corner masks are correct;
- hidden items do not leave broken corners/seams;
- `Controls.SetChildIndex` changes visible/layout/navigation order consistently;
- horizontal layout is stable;
- flush vertical presentation is edge-to-edge;
- group `BorderRadius` returns to effect after leaving flush mode;
- all contextual variants remain readable;
- active/disabled/hover/pressed/focus states are visually distinct;
- actionable item row actually receives focus, not merely TabStop metadata;
- Tab enters/leaves the group;
- arrow/Home/End navigation does not trap focus and follows reordered item sequence;
- Enter/Space activates only actionable enabled items;
- label/badge/decorative row areas activate their actionable item once;
- embedded interactive child controls keep their own click/focus behavior and do not activate the parent;
- light/dark theme switching works live;
- resize/Dock/Anchor do not create clipping or layout drift.

### Automated test safety

GUI-heavy tests must use the repository's established WinForms test infrastructure. Unexpected UI exceptions/dialogs should fail tests deterministically rather than block `dotnet test` waiting for interaction.

In particular, focus/message-loop tests added for actionable item selectability must not rely on modal forms or manual interaction.

### Final acceptance criteria

- All targeted tests pass.
- Full solution tests pass.
- Full solution build passes.
- No new analyzer/compiler warnings are introduced.
- No modal test hangs are introduced.
- Theme/DPI/disposal regressions are covered.
- Reorder/selectability/decorative-forwarding regressions are covered.
- Public API remains small and composition-focused.

---

## Definition of Done

`BootstrapListGroup` is complete when all of the following are true:

- [ ] `BootstrapListGroup` and `BootstrapListGroupItem` are public, documented controls.
- [ ] Normal WinForms child ownership is preserved.
- [ ] `Controls` child-index order is the source of truth for `Items`, layout, geometry, and navigation.
- [ ] `Items` remains correct after add/remove/reparent and `Controls.SetChildIndex` reorder.
- [ ] Vertical connected layout is stable.
- [ ] Horizontal connected layout is stable.
- [ ] Vertical `Flush` is supported and horizontal `Flush` behavior is explicitly documented/tested.
- [ ] Group `BorderRadius` is the sole public radius override; no ambiguous item/group radius precedence exists.
- [ ] The contextual-variant API is designer-safe and decided in Task 1 before downstream implementation.
- [ ] Neutral and contextual semantic variants use shared theme infrastructure.
- [ ] Active, disabled, hover, pressed, and focus states have deterministic precedence.
- [ ] `Actionable` synchronizes both `TabStop` and actual WinForms selectability.
- [ ] Actionable items can truly receive focus and support mouse, Enter, Space, and spatial keyboard navigation.
- [ ] Tab navigation is not trapped.
- [ ] Item activation never automatically changes `Active`.
- [ ] Arbitrary rich content can be composed with normal child controls.
- [ ] `BootstrapBadge` and `Label` can be embedded without creating dead click zones on actionable rows.
- [ ] Interactive child controls retain their own semantics and do not spuriously activate the parent item.
- [ ] Forwarded/direct activation raises item `Click` and parent `ItemClick` exactly once.
- [ ] Theme switching updates the control live.
- [ ] DPI scaling works across supported scale factors.
- [ ] Theme/event/child-forwarding subscriptions are disposed correctly.
- [ ] `net48` and `net8.0-windows` compatibility is preserved.
- [ ] Demo covers basic, active/disabled, actionable/selectability, reorder, contextual, rich-content forwarding, interactive-child isolation, flush, horizontal, keyboard, and theme scenarios.
- [ ] `docs/COMPONENTS.md` documents usage and control-selection boundaries.
- [ ] Targeted tests, full tests, and full build pass.

## Implementation order

Implement in this sequence to keep failures localized and avoid late public-API changes:

```text
1. Public contract + real selectability + contextual-variant designer compatibility gate
2. Controls ownership + authoritative child-index ordering + Items snapshots
3. Pure render/state/radius logic
4. Item painting + preferred size + rich-content layout
5. Group layout + connected borders + reorder resilience + flush/orientation
6. Mouse/keyboard + decorative-child forwarding + accessibility
7. Theme/variant hardening
8. Designer + supported-target regression validation
9. Demo
10. Documentation
11. Full regression verification
```

Do not begin by polishing the demo or adding extra APIs. The core risks are:

- `Panel` selectability vs `TabStop`;
- keeping `Controls` ordering authoritative after non-add/remove reorder operations;
- making rich actionable rows clickable without stealing input from interactive descendants;
- connected layout geometry/radius ownership;
- fixing designer compatibility before the contextual-variant public API propagates through the implementation;
- theme/DPI state rendering and deterministic WinForms test execution.