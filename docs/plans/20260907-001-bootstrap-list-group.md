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
- keep `BootstrapListGroup` clearly distinct from a future/native `BootstrapListView`.

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

## Architectural decisions

### 1. Use real child controls, not an item data model

`BootstrapListGroup` should derive from `Panel` and arrange `BootstrapListGroupItem` child controls. `BootstrapListGroupItem` should also be a WinForms control/container so it can host arbitrary content.

Follow the established `BootstrapAccordion` pattern:

- `Controls` remains the WinForms ownership source of truth;
- maintain only an internal ordered cache of direct `BootstrapListGroupItem` children for layout/event coordination;
- expose `Items` as a read-only snapshot;
- provide `AddItem`, `RemoveItem`, and `ClearItems` convenience methods;
- synchronize the cache strictly from `OnControlAdded` / `OnControlRemoved`;
- never keep a separate list of item data that can drift out of sync with `Controls`.

### 2. `Active` is explicit presentation state, not automatic selection

A Bootstrap List Group is not a WinForms selection/data control. Therefore V1 must not introduce:

- `SelectedIndex`;
- `SelectedItem`;
- single/multiple selection modes;
- automatic activation when an item is clicked.

Each item owns an explicit `Active` property. Applications decide when to change it. Multiple items may technically be active because the control does not impose a selection policy.

This avoids semantic overlap with `BootstrapListView` and keeps navigation-state ownership with the application.

### 3. `Actionable` controls focus/click interaction

An item can be purely presentational or actionable.

When `Actionable == false`:

- item is not a tab stop;
- no hover/pressed action styling;
- it may still contain independently interactive child controls.

When `Actionable == true && Enabled == true`:

- the item itself is focusable;
- mouse hover/pressed/focus states are rendered;
- mouse click activates the item's normal `Click` event;
- Enter and Space activate it from keyboard;
- the parent raises a convenience `ItemClick` event carrying the item.

When `Enabled == false`:

- the item uses disabled styling;
- the item itself cannot be activated;
- it is excluded from group keyboard navigation.

Do not suppress normal child-control interaction merely because an item is not actionable.

### 4. Rich content uses normal WinForms composition

`BootstrapListGroupItem.Text` provides the simple one-line case, but an item must also be able to host arbitrary child controls.

Examples:

- `Label` for heading/description;
- `BootstrapBadge` for counts/status;
- icon control/renderer already supported by the project;
- compact panels/layout controls for richer settings rows.

Do **not** add ListGroup-specific properties such as `BadgeText`, `BadgeVariant`, `SecondaryText`, or a bespoke content object in V1. Composition is the extension mechanism.

### 5. Parent owns connected group geometry

The group is responsible for deciding each item's connected-edge role:

```text
Vertical:
[first ] top corners rounded
[middle] square connected edges
[last  ] bottom corners rounded

Horizontal:
[first ][middle][last]
left corners         right corners
```

The item renders its own surface using geometry/state supplied by the group, but group layout determines:

- first/middle/last visible position;
- outer corner mask;
- border/seam overlap;
- item bounds;
- flush behavior.

Avoid double-thick borders between adjacent items. Reuse the same connected-border principles already used by grouped controls rather than adding spacing to hide seams.

### 6. Theme and DPI are infrastructure, not local constants

Use:

- `BootstrapThemeManager.CurrentTheme`;
- existing `BootstrapThemeColors` / semantic color resolution;
- `BootstrapThemeMetrics`;
- `DpiScaler`;
- `BootstrapVariant`;
- existing rounded-path/rendering helpers where applicable.

Do not hard-code Bootstrap hex colors or fixed pixel metrics in the control.

### 7. Contextual variant is optional

The existing `BootstrapVariant` has semantic values but no `Default` value. To preserve the neutral List Group appearance without duplicating the shared enum, use an optional variant:

```csharp
public BootstrapVariant? Variant { get; set; }
```

Semantics:

- `null` = normal neutral List Group item;
- non-null = contextual List Group item using the shared semantic variant.

Verify WinForms designer serialization/PropertyGrid behavior for nullable enums in tests/demo. Do not introduce a duplicate `BootstrapListGroupVariant` enum unless a concrete designer compatibility problem is demonstrated.

### 8. Flush and horizontal behavior

Bootstrap web documents horizontal List Groups separately from flush List Groups and does not currently combine them.

For V1:

- `Orientation = Vertical` by default;
- `Orientation = Horizontal` is supported;
- `Flush` removes the outer group border/radius in vertical layout and keeps separators between items;
- when horizontal, `Flush` has no visual effect and must not mutate the property or throw during designer editing;
- document and test this rule explicitly.

Do not add Bootstrap responsive breakpoint variants such as `sm`, `md`, `lg`, `xl`, or `xxl` to the WinForms API.

## Proposed public API

The exact XML documentation can be refined during implementation, but the V1 public surface should remain close to the following.

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
    public BootstrapVariant? Variant { get; set; }     // default null
    public int BorderRadius { get; set; }              // default -1 = inherited/theme
    public bool UseThemeFont { get; set; }             // follow existing control convention

    // Text, Enabled, Padding, Controls, Font, AccessibleName, etc.
    // remain normal WinForms properties.
}
```

### `BootstrapListGroupItemEventArgs`

```csharp
public sealed class BootstrapListGroupItemEventArgs : EventArgs
{
    public BootstrapListGroupItemEventArgs(BootstrapListGroupItem item);
    public BootstrapListGroupItem Item { get; }
}
```

During implementation, prefer internal state/geometry types over expanding public API for rendering-only concerns.

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

## Task 1 — Establish failing contract tests and public types

**Files:**

- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItemEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify/create during implementation: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify/create during implementation: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`

### Steps

1. Write tests for the intended defaults before implementation:
   - group derives from `Panel`;
   - `Orientation == Orientation.Vertical`;
   - `Flush == false`;
   - `BorderRadius == -1`;
   - `AutoSize`/`AutoSizeMode` behavior is explicitly asserted rather than accidental;
   - group is not a tab stop;
   - accessible role is a list/grouping role appropriate to WinForms;
   - item `Active == false`;
   - item `Actionable == false`;
   - item `Variant == null`;
   - item uses theme font according to the project convention;
   - presentational item is not a tab stop.
2. Add validation tests:
   - invalid `Orientation` enum value throws `InvalidEnumArgumentException` or the project-standard equivalent;
   - `BorderRadius < -1` throws `ArgumentOutOfRangeException`;
   - invalid nullable `BootstrapVariant` value is rejected consistently with existing controls.
3. Add event-args null guard and `Item` identity tests.
4. Run the targeted tests and confirm they fail for missing types/API.
5. Add the minimum public types/properties necessary to make contract tests compile and pass; no rendering yet.

### Verification

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj --filter "FullyQualifiedName~BootstrapListGroup"
```

### Acceptance criteria

- Public defaults are intentional and test-covered.
- Public validation behavior is deterministic.
- The project compiles for its supported targets.
- No ListView-like selection/data API is introduced.

---

## Task 2 — Implement WinForms child ownership and item collection synchronization

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`

### Steps

1. Add an internal ordered `List<BootstrapListGroupItem>` cache similar to `BootstrapAccordion`.
2. Expose:

```csharp
[Browsable(false)]
[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
public IReadOnlyList<BootstrapListGroupItem> Items => ...;
```

3. Synchronize items only from `OnControlAdded` / `OnControlRemoved`.
4. Subscribe/unsubscribe only the events the group actually needs for layout and forwarded item activation.
5. Implement:
   - `AddItem(string text)`;
   - `AddItem(BootstrapListGroupItem item)`;
   - `RemoveItem(BootstrapListGroupItem item)`;
   - `ClearItems()`.
6. Preserve standard WinForms semantics:
   - adding through `Controls.Add(item)` updates `Items`;
   - removing through `Controls.Remove(item)` updates `Items`;
   - helper methods route through `Controls` rather than directly mutating the cache;
   - `RemoveItem` does not dispose the item;
   - `ClearItems` removes but does not silently dispose items;
   - reparenting is reflected correctly.
7. Decide direct non-item child behavior consistently with existing composite controls. Prefer ignoring non-item direct children for item layout rather than throwing in the designer, unless repo precedent clearly requires stricter validation.
8. Add tests for add/remove/reparent/order/visibility and duplicate-subscription prevention.

### Acceptance criteria

- `Controls` is the ownership source of truth.
- `Items` cannot become stale through normal WinForms add/remove/reparent flows.
- No collection operation causes double event subscription.
- Design-time child serialization remains standard WinForms child-control serialization.

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
- border/background/foreground color selection from theme + optional `BootstrapVariant`;
- effective border radius;
- flush edge rules.

Do not put WinForms event routing or mutable child-control ownership into this helper.

### Required state precedence

Define and test an explicit precedence so combined states cannot accidentally produce unreadable colors. Recommended ordering:

```text
disabled
  > active
    > pressed (actionable)
      > hover (actionable)
        > contextual variant
          > neutral/default
```

Focus indication is an overlay/outline concern and must not destroy the state foreground/background contrast.

### Tests

Cover at minimum:

- neutral state;
- every supported `BootstrapVariant`;
- active + variant;
- disabled + active;
- actionable hover/pressed;
- first/middle/last/single-item corners in both orientations;
- vertical flush geometry;
- horizontal flush documented no-op behavior;
- 96/120/144/192 DPI scaling;
- radius `-1` vs explicit radius;
- zero/very small bounds do not create invalid drawing geometry.

### Acceptance criteria

- Geometry/state calculations are deterministic unit tests.
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

1. Configure owner painting with the project-standard control styles:
   - `UserPaint`;
   - `AllPaintingInWmPaint`;
   - `OptimizedDoubleBuffer`;
   - `ResizeRedraw`;
   - transparent-background support where needed.
2. Implement theme font behavior consistent with controls such as `BootstrapBadge`:
   - theme typography by default;
   - user-assigned font can opt out according to existing `UseThemeFont` convention;
   - theme changes update only when theme font is enabled.
3. Implement `GetPreferredSize`:
   - simple text-only item measures `Text` plus theme/DPI padding;
   - hosted child controls contribute to preferred size;
   - explicit `Size`/non-AutoSize scenarios remain respected;
   - no negative/overflow dimensions.
4. Render:
   - background;
   - connected border edges;
   - rounded outer corners supplied by the group;
   - text for the simple-item case;
   - focus cue for actionable keyboard focus.
5. Rich content rule:
   - if custom child controls exist, keep `Text` rendering predictable and non-overlapping;
   - prefer reserving the normal content rectangle and allowing child controls to be positioned/docked within it;
   - do not automatically relocate arbitrary child controls unless documented.
6. Make parent-supplied connected-edge geometry internal rather than public.
7. Repaint/re-layout on:
   - `TextChanged`;
   - `FontChanged`;
   - `PaddingChanged`;
   - `EnabledChanged`;
   - `Active`/`Actionable`/`Variant` changes;
   - DPI changes;
   - theme changes.
8. Ensure theme subscription is released in `Dispose`.

### Acceptance criteria

- Text-only items look correct without extra child controls.
- Items can host `BootstrapBadge` and ordinary WinForms controls.
- Connected edges are not double-painted.
- Theme changes and DPI changes do not require recreating the item.
- Disposal does not leak theme subscriptions.

---

## Task 5 — Implement group layout, connected edges, orientation, and flush

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`

### Vertical layout

- Default orientation.
- Visible items are laid out top-to-bottom in `Controls` display order.
- Items stretch to the group's usable client width when the group has an explicit width.
- Auto-size computes height from visible item preferred heights minus shared seam overlap.
- Hidden items do not consume space and do not affect first/middle/last corner assignment.

### Horizontal layout

- Visible items are laid out left-to-right.
- Preserve each item's preferred width by default; do not add equal-width behavior unless a demonstrated use case requires it.
- Shared vertical seams overlap by exactly one scaled border thickness.
- Auto-size computes width from item preferred widths minus seam overlap and height from the tallest visible item.

### Flush

Vertical `Flush == true`:

- no rounded outer container/item corners;
- no outer left/right group border where Bootstrap flush semantics require edge-to-edge presentation;
- separators remain between adjacent items;
- content padding remains unchanged unless theme semantics explicitly require otherwise.

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
- all child bounds remain non-negative.

### Acceptance criteria

- Connected borders remain visually one logical border thick.
- Layout is stable across repeated calls.
- Visible item order and corner roles remain correct after add/remove/hide/reorder.
- Horizontal/vertical switch is safe at runtime and design time.

---

## Task 6 — Implement actionable mouse/keyboard behavior and accessibility

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`

### Mouse behavior

For actionable enabled items:

- track hover on mouse enter/leave;
- track pressed state only for a valid primary-button press/release cycle;
- raise normal `Click` once;
- parent forwards one `ItemClick` event;
- losing capture/focus must clear transient pressed state.

Do not convert child-control clicks into item clicks automatically. A `BootstrapBadge`, link, button, checkbox, etc. hosted inside an item must keep its own normal event semantics.

### Keyboard activation

For actionable enabled items:

- Enter activates the item;
- Space activates the item once using normal button-like semantics;
- disabled/non-actionable items do not activate.

### Group navigation

Implement desktop-friendly spatial navigation only among direct actionable, enabled, visible items:

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
- `Active`, disabled, and focused states are reflected as far as the WinForms accessibility API allows without building a custom UIA provider in V1.
- `AccessibleName` defaults sensibly from `Text` when the caller has not supplied one.

### Tests

Use deterministic direct tests where possible. For tests requiring real focus/message pumping, use the repository's WinForms test infrastructure and STA/message-loop helpers rather than ad-hoc threads.

Cover:

- actionable item tab-stop changes;
- disabled item cannot activate;
- Enter/Space exactly once;
- parent `ItemClick` forwards correct item exactly once;
- clicking an interactive child does not spuriously activate parent item;
- arrow/Home/End focus movement;
- hidden/disabled/non-actionable items are skipped;
- Tab is not trapped;
- `Active` never changes automatically through keyboard/mouse activation.

### Acceptance criteria

- Keyboard-only navigation is usable.
- Tab can enter/leave the group normally.
- Interaction does not introduce modal dialogs or uncontrolled GUI waits in tests.
- Active state remains application-owned.

---

## Task 7 — Theme, semantic variants, visual-state and contrast hardening

**Files:**

- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListGroupRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupRenderLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`

### Steps

1. Map `Variant == null` to neutral list-group colors derived from the current theme.
2. Map all shared semantic variants:
   - Primary;
   - Secondary;
   - Success;
   - Danger;
   - Warning;
   - Info;
   - Light;
   - Dark.
3. Verify readable foreground/background choices in light and dark themes.
4. Ensure `Active` is visibly dominant over contextual variant while retaining semantic readability.
5. Ensure disabled state is visibly disabled and cannot look like hover/pressed.
6. Ensure hover/pressed only appears for actionable items.
7. Ensure focus indication remains visible in light/dark and active/contextual combinations.
8. Subscribe to `BootstrapThemeManager.ThemeChanged`; invalidate/re-layout as needed; unsubscribe on disposal.
9. Add regression tests that switching theme does not reset public state (`Active`, `Actionable`, `Variant`, `Enabled`).

### Acceptance criteria

- No ListGroup-specific hard-coded palette exists.
- All semantic variants react to theme changes.
- State precedence is deterministic and test-covered.
- Disposal does not leave static theme event subscriptions behind.

---

## Task 8 — Designer/API compatibility and supported-target validation

**Files:**

- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroup.cs`
- Modify as needed: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListGroupItem.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListGroupItemTests.cs`
- Modify if needed: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Compatibility/*`

### Steps

1. Verify public API uses types available on both `net48` and `net8.0-windows`.
2. Avoid newer BCL/WinForms APIs unless guarded by the repository's compatibility strategy.
3. Verify nullable `BootstrapVariant?` works with the WinForms PropertyGrid/designer serialization used by the project.
4. If nullable enum designer behavior is genuinely unusable:
   - document the failure in the implementation PR/commit;
   - choose the smallest compatible alternative;
   - do **not** silently create a duplicate semantic enum without tests justifying it.
5. Verify adding `BootstrapListGroupItem` child controls in the designer produces stable generated code.
6. Verify theme subscriptions do not execute design-time-only unsafe behavior.
7. Check `AutoSize`, `Dock`, `Anchor`, `Padding`, `Margin`, `Visible`, and reparenting behavior.
8. Build all supported target frameworks.

### Verification

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj --filter "FullyQualifiedName~BootstrapListGroup"
```

When the local environment supports .NET Framework test execution, also run the repository-standard `net48` validation path.

### Acceptance criteria

- `net48` and `net8.0-windows` source compatibility is preserved.
- Designer serialization is stable.
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
4. **Contextual variants**
   - Primary/Success/Danger/Warning/Info and remaining supported variants.
5. **Rich content**
   - heading + description;
   - embedded `BootstrapBadge` count/status;
   - demonstrate that badge/custom child content is composition, not ListGroup-specific data.
6. **Flush**
   - vertical edge-to-edge presentation.
7. **Horizontal**
   - three items;
   - Left/Right keyboard navigation;
   - document/show that `Flush` is a V1 no-op in horizontal mode.
8. **Theme switch**
   - verify the existing demo theme switch updates all List Group states correctly.

### Acceptance criteria

- Demo makes ListGroup vs ListView scope obvious.
- No demo-only API is added to the library.
- Keyboard interaction can be manually verified without mouse-only steps.

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
- `Active` is explicit state, not automatic selection;
- `Actionable` semantics;
- keyboard behavior;
- `Variant == null` neutral semantics;
- `Flush` behavior and horizontal limitation;
- rich-content composition example with `BootstrapBadge`;
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
- Examples compile against the final public API.
- Component choice guidance prevents misuse for large data sets.

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
- horizontal layout is stable;
- flush vertical presentation is edge-to-edge;
- all contextual variants remain readable;
- active/disabled/hover/pressed/focus states are visually distinct;
- Tab enters/leaves the group;
- arrow/Home/End navigation does not trap focus;
- Enter/Space activates only actionable enabled items;
- embedded child controls keep their own click/focus behavior;
- light/dark theme switching works live;
- resize/Dock/Anchor do not create clipping or layout drift.

### Automated test safety

GUI-heavy tests must use the repository's established WinForms test infrastructure. Unexpected UI exceptions/dialogs should fail tests deterministically rather than block `dotnet test` waiting for interaction.

### Final acceptance criteria

- All targeted tests pass.
- Full solution tests pass.
- Full solution build passes.
- No new analyzer/compiler warnings are introduced.
- No modal test hangs are introduced.
- Theme/DPI/disposal regressions are covered.
- Public API remains small and composition-focused.

---

## Definition of Done

`BootstrapListGroup` is complete when all of the following are true:

- [ ] `BootstrapListGroup` and `BootstrapListGroupItem` are public, documented controls.
- [ ] Normal WinForms child ownership is preserved.
- [ ] `Items` stays synchronized with direct child controls.
- [ ] Vertical connected layout is stable.
- [ ] Horizontal connected layout is stable.
- [ ] Vertical `Flush` is supported and horizontal `Flush` behavior is explicitly documented/tested.
- [ ] Neutral and contextual semantic variants use shared theme infrastructure.
- [ ] Active, disabled, hover, pressed, and focus states have deterministic precedence.
- [ ] Actionable items support mouse, Enter, Space, and spatial keyboard navigation.
- [ ] Tab navigation is not trapped.
- [ ] Item activation never automatically changes `Active`.
- [ ] Arbitrary rich content can be composed with normal child controls.
- [ ] `BootstrapBadge` can be embedded without any ListGroup-specific badge API.
- [ ] Theme switching updates the control live.
- [ ] DPI scaling works across supported scale factors.
- [ ] Theme/event subscriptions are disposed correctly.
- [ ] `net48` and `net8.0-windows` compatibility is preserved.
- [ ] Demo covers basic, active/disabled, actionable, contextual, rich-content, flush, horizontal, keyboard, and theme scenarios.
- [ ] `docs/COMPONENTS.md` documents usage and control-selection boundaries.
- [ ] Targeted tests, full tests, and full build pass.

## Implementation order

Implement in this sequence to keep failures localized:

```text
1. Public contract/defaults
2. Controls/Items ownership synchronization
3. Pure render/state logic
4. Item painting + preferred size + rich content
5. Group layout + connected borders + flush/orientation
6. Mouse/keyboard/accessibility
7. Theme/variant hardening
8. Designer + target compatibility
9. Demo
10. Documentation
11. Full regression verification
```

Do not begin by polishing the demo or adding extra APIs. The core risks are connected layout geometry, focus/keyboard behavior, theme/DPI state rendering, and keeping the ListGroup abstraction lightweight.