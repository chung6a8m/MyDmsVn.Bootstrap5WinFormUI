# BootstrapBreadcrumb Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap-inspired `BootstrapBreadcrumb` navigation control that represents an ordered hierarchy, renders every ancestor as a native WinForms link, renders the last item as the current location, supports configurable dividers, wrapping and RTL layout, and preserves deterministic theme/DPI/accessibility/lifecycle behavior without inventing a custom focus or navigation engine.

**Architecture:** `BootstrapBreadcrumb : Panel` owns a small public item collection and dynamically composes native `LinkLabel` controls for ancestor items plus native `Label` controls for the current item and dividers. Native `LinkLabel` remains authoritative for Tab focus, keyboard link activation, mouse hit-testing and link accessibility; the Breadcrumb only maps an effective link activation to `ItemClicked`. A pure internal layout helper positions whole breadcrumb segments, keeps each divider attached to the following item, wraps only at segment boundaries, and mirrors geometry for `RightToLeft.Yes`. The control subscribes once to the shared theme because native Label/LinkLabel children are not Bootstrap primitives and therefore need their font/colors updated by the owner.

**Tech Stack:** C#, native Windows Forms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, theme typography/metrics, `DpiScaler`, NUnit 4, integrated demo application. No external dependency and no custom painting engine.

**Spec:** User request plus Bootstrap 5.3 Breadcrumb behavior (`https://getbootstrap.com/docs/5.3/components/breadcrumb/`) and the WAI-ARIA Breadcrumb Pattern (`https://www.w3.org/WAI/ARIA/apg/patterns/breadcrumb/`). This plan's **Breadcrumb contract** is the WinForms adaptation baseline. Repository-wide constraints come from `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, `docs/COMPONENTS.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; all new public Breadcrumb types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared code path wherever practical.
- Keep Breadcrumb a lightweight navigation composition. Do not add routing, URI opening, history management, page hosting, back/forward stacks, application state, async loading, menus, popup infrastructure, timers, animation, or data binding.
- Bootstrap Breadcrumb is a hierarchy trail, not a selectable toolbar. The last item is always the current location and is never a link; every preceding item is an actionable link while the Breadcrumb itself is enabled.
- Activating an ancestor raises `ItemClicked`; the framework does **not** automatically remove descendants, replace the last item, navigate a Form, or mutate the item collection. Application code owns the resulting navigation and collection update.
- Reuse native `LinkLabel` semantics for actionable ancestors. Do not paint link hit areas manually, install global keyboard hooks, synthesize an arrow-key roving-focus model, or replace native UI Automation objects.
- Do not use partial `LinkArea` ranges. Each ancestor LinkLabel represents one complete breadcrumb item so its accessible link name remains the full item text.
- The current item and divider controls are non-focusable. Only actionable ancestor links participate in Tab/Shift+Tab traversal.
- Do not add special Left/Right/Home/End keyboard navigation. The WAI-ARIA Breadcrumb Pattern defines no additional widget keyboard interaction; normal link Tab navigation is sufficient.
- Default divider text is `/`, matching Bootstrap. Empty divider text is valid and removes the visible divider while preserving inter-item spacing.
- `RightToLeftDivider = null` means reuse `Divider`; callers may provide an explicit directional divider such as `<` when `RightToLeft.Yes` needs a flipped symbol.
- `WrapContents = true` wraps only between logical breadcrumb segments. A divider must never be stranded at the end of a row without the item it introduces.
- The item collection is authoritative. Do not mirror it into a second public navigation model.
- Public item models are caller-owned semantic data. The Breadcrumb owns and disposes only the native child controls it creates; it never disposes `BootstrapBreadcrumbItem` instances or caller-owned `Tag` objects.
- All framework-owned spacing comes from current theme metrics and scales through `DpiScaler`; do not hard-code repeated device-pixel gaps.
- Link color comes from the theme's `Primary` semantic color. Divider/current-item color comes from `MutedText`, corresponding to Bootstrap's secondary-color treatment. Disabled presentation uses theme disabled/muted tokens. Do not add a Breadcrumb-specific semantic `Variant` in V1.
- Use the current theme Body typography while the control still owns its font. If the caller explicitly assigns `Font`, stop replacing that font on later theme changes and never dispose the caller-owned font.
- Theme subscription must be paired with deterministic unsubscribe in `Dispose(bool)` and remain safe across handle recreation.
- Designer construction must be parameterless and must not require application bootstrap or initialized external services.
- The control introduces no per-item GDI resource ownership, no custom `Graphics` painting, no `Region`, and no external icon package.
- Tests that create WinForms handles or exercise link interaction must be STA. Tests that mutate global theme state must be non-parallel.
- Every focused raw `dotnet test` command for UI tests must include `--blame-hang --blame-hang-timeout 5m`; the full suite should run through `./test.ps1`.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5.3 defines Breadcrumb as an ordered/unordered list of hierarchy items. Parent items are links, the final item represents the current page, dividers appear between items, divider text can be customized or removed, and the final item carries current-page semantics. Bootstrap's default visual model has no required breadcrumb background, border, or radius.

The WinForms adaptation is therefore:

```text
Items: Home -> Library -> Data

LTR visual:
Home  /  Library  /  Data
^^^^     ^^^^^^^      ^^^^
link     link          current/non-link

RTL visual coordinates:
Data  /  Library  /  Home
^^^^     ^^^^^^^      ^^^^
current  link          root link
```

Rules:

1. `Items.Count == 0` renders an empty, non-focusable control.
2. `Items.Count == 1` renders one current-location `Label`; no divider or link is created.
3. For `Items.Count > 1`, indices `0..Count-2` render as `LinkLabel`; index `Count-1` renders as the current-location `Label`.
4. Dividers exist only between items. There is never a leading or trailing divider.
5. Link activation raises exactly one `ItemClicked` with the item and its current collection index.
6. `ItemClicked` is raised only for an item that is still an ancestor at activation time. A disposed/stale child must never raise after a rebuild.
7. Clicking/activating a link does not alter `Items` before or after the event.
8. The last item's `Tag` is preserved like any other item, even though it is not actionable.
9. Changing an item's `Text` updates presentation and preferred size without changing item order or `Tag`.
10. Changing `Tag` is semantic-only and does not force a visual rebuild.
11. `Enabled=false` on the Breadcrumb disables effective link interaction through normal WinForms parent/child semantics and switches presentation to disabled theme colors; it does not mutate item state.
12. `RightToLeft.Yes` mirrors the visual flow while preserving collection order and event indices.
13. When wrapping is enabled, the current item may move to a later row, but a divider and the following item always remain in the same logical segment.
14. No component-specific margin-bottom is imposed. WinForms host layout owns vertical spacing around the Breadcrumb; this avoids importing Bootstrap's document-flow margin into a reusable desktop control.

Useful implementation references:

- Bootstrap 5.3 Breadcrumb: `https://getbootstrap.com/docs/5.3/components/breadcrumb/`
- WAI-ARIA Breadcrumb Pattern: `https://www.w3.org/WAI/ARIA/apg/patterns/breadcrumb/`
- WinForms `LinkLabel`: `https://learn.microsoft.com/dotnet/api/system.windows.forms.linklabel`
- WinForms `Control.RightToLeft`: `https://learn.microsoft.com/dotnet/api/system.windows.forms.control.righttoleft`
- WinForms accessibility properties: `https://learn.microsoft.com/dotnet/desktop/winforms/advanced/properties-on-windows-forms-controls-that-support-accessibility-guidelines`

---

## Breadcrumb Contract

### Public item model

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public sealed class BootstrapBreadcrumbItem
{
    public BootstrapBreadcrumbItem();
    public BootstrapBreadcrumbItem(string text);

    public string Text { get; set; }      // default "", null assignments normalize to ""
    public object? Tag { get; set; }      // default null
}
```

Item rules:

- `Text` is the complete visual/accessibility label for the item.
- `Tag` is caller-owned routing/application metadata and has no rendering semantics.
- `Text` changes notify any owning Breadcrumb collection so child text/layout refreshes.
- `Tag` changes do not rebuild child controls.
- The collection rejects `null` items.
- The same item instance should not be inserted into the same Breadcrumb collection more than once; reject duplicate references so item-change subscriptions and event indices remain unambiguous. Distinct items may have identical `Text` and/or `Tag` values.

### Public collection

```csharp
public sealed class BootstrapBreadcrumbItemCollection
    : System.Collections.ObjectModel.Collection<BootstrapBreadcrumbItem>
{
    // Owner-created collection; no public standalone constructor is required.
    // Inherited Add/Insert/Remove/RemoveAt/Clear/indexer are the mutation API.
}
```

The collection overrides protected mutation hooks internally to:

- validate non-null/no duplicate-reference items;
- subscribe/unsubscribe the item's internal text-change notification;
- notify the owning Breadcrumb exactly once after each effective mutation;
- dispose no public item model.

### Public event args

```csharp
public sealed class BootstrapBreadcrumbItemClickedEventArgs : EventArgs
{
    public BootstrapBreadcrumbItem Item { get; }
    public int Index { get; }
}
```

The event-args constructor may remain internal; callers receive stable item/index information from the event and do not construct framework event args themselves.

### Public control

```csharp
[DefaultEvent(nameof(ItemClicked))]
public class BootstrapBreadcrumb : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapBreadcrumbItemCollection Items { get; }

    public string Divider { get; set; }                  // default "/", null => ""
    public string? RightToLeftDivider { get; set; }      // default null => Divider
    public bool WrapContents { get; set; }               // default true

    public event EventHandler<BootstrapBreadcrumbItemClickedEventArgs>? ItemClicked;
}
```

### Default control state

```text
AutoSize              = true
AutoSizeMode          = GrowAndShrink
BackColor             = Transparent
TabStop               = false
AccessibleRole        = Grouping
AccessibleName        = "Breadcrumb"
AccessibleDescription = "Breadcrumb navigation."
Divider               = "/"
RightToLeftDivider    = null
WrapContents          = true
Items.Count           = 0
```

### Public API rules

- Do not add `CurrentIndex`, `SelectedIndex`, `ActiveItem`, `NavigateTo`, `NavigateBack`, `Uri`, `Route`, or automatic descendant trimming in V1.
- Do not add `Variant`, `BorderRadius`, `BackgroundColor`, `DividerColor`, `LinkColor`, or `CurrentColor` in V1. Theme tokens define the Bootstrap baseline; caller-level arbitrary style APIs can be considered later if a real application requirement appears.
- Do not expose child `LinkLabel`/`Label` controls or a public control collection dedicated to generated children.
- Inherited `Controls` remains WinForms API, but generated Breadcrumb children are framework-owned implementation details. Documentation/demo code should use `Items`, never add navigation children through `Controls`.
- Public members receive XML documentation and appropriate `Category`, `Description`, `DefaultValue`, `Browsable`, and designer serialization attributes.

---

## Visual, Interaction, Accessibility, and Layout Contract

### Link presentation

Each ancestor item is one native `LinkLabel` configured as follows:

```text
AutoSize          = true
TabStop           = true
UseMnemonic       = false
LinkBehavior      = AlwaysUnderline
LinkVisited       = false
LinkColor         = theme.Colors.Primary
VisitedLinkColor  = theme.Colors.Primary
ActiveLinkColor   = theme.Colors.Primary
DisabledLinkColor = theme.Colors.Disabled (or MutedText if that is the established readable disabled token)
BackColor         = Transparent
Margin/Padding    = zero; Breadcrumb owns spacing in layout
Text              = item.Text
AccessibleName    = item.Text
```

Do not set a partial `LinkArea`; the whole item is one link.

### Current item

The final item is one native `Label`:

```text
AutoSize              = true
TabStop               = false
UseMnemonic           = false
BackColor             = Transparent
ForeColor             = theme.Colors.MutedText when enabled
AccessibleName        = item.Text
AccessibleDescription = "Current page."
```

The current item is deliberately not a LinkLabel. This provides the desktop analogue of Bootstrap's final `.active` item without inventing an `aria-current` layer.

### Dividers

- Use native non-focusable `Label` controls.
- LTR text is `Divider`.
- RTL text is `RightToLeftDivider ?? Divider`.
- `Divider = ""` creates no visible glyph but layout still preserves the normal inter-item gap.
- Divider foreground is `theme.Colors.MutedText` while enabled and a disabled/muted token while the parent is disabled.
- Divider controls must not become Tab stops or activation targets.
- Give divider accessibility the least intrusive native semantics available (`AccessibleRole.None`, empty accessible description/name where effective) and verify manually that screen readers do not turn separator glyphs into misleading navigation items. Do not create a custom accessibility tree solely to hide dividers in V1.

### Font ownership

- Initial font comes from `BootstrapThemeManager.CurrentTheme.Typography.Body`.
- The Breadcrumb owns only the `Font` instances it creates from theme tokens.
- While framework-owned, a theme change replaces the old theme font and updates all current generated children.
- If the caller assigns `Font`, mark font ownership as caller-controlled; later theme changes update colors/metrics but do not replace or dispose that font.
- Child controls reference the Breadcrumb's current font; they do not independently create/dispose fonts.

### DPI and spacing

At 96 logical DPI:

```text
Inter-item divider gap = theme.Metrics.SpacingSM on each side of a visible divider
Empty-divider gap      = 2 * theme.Metrics.SpacingSM between adjacent items
Row gap                = theme.Metrics.SpacingXS
```

Scale these metrics with `DpiScaler.Scale` at the current control DPI. Do not manually scale text; WinForms/font lifecycle remains authoritative for text measurement.

### Wrapping

Treat each item after the first as an indivisible logical segment containing:

```text
LTR segment: [left gap][divider][right gap][item]
RTL segment: [item][left gap][divider][right gap]
```

The first item is a segment with only the item.

`WrapContents=false` always lays out one row and reports the full preferred width.

`WrapContents=true`:

- use the available/proposed content width when it is positive;
- move a whole segment to the next row if it does not fit and the current row already contains a segment;
- never split divider from its following item;
- if one segment is wider than available width, place it alone without negative geometry and let normal clipping/host layout handle the overflow;
- align controls vertically within each row using the row's maximum child height;
- preserve logical collection order in event mapping regardless of visual RTL coordinates.

### RTL

- `RightToLeft.No`: item 0 starts at the left edge and logical segments advance right.
- `RightToLeft.Yes`: item 0 starts at the right edge and logical segments advance left.
- Do not reverse the `Items` collection to implement RTL.
- Event `Index` always means the collection index, not visual left-to-right position.
- Use `RightToLeftDivider ?? Divider` for the divider string; do not infer that arbitrary Unicode divider text can be mirrored correctly.

---

## Internal Layout Contract

Create a pure helper instead of burying row-packing arithmetic in `BootstrapBreadcrumb.OnLayout`.

### `BootstrapBreadcrumbLayoutLogic`

Suggested internal types:

```csharp
internal readonly struct BootstrapBreadcrumbLayoutMetrics
{
    public BootstrapBreadcrumbLayoutMetrics(int dividerGap, int rowGap)
    {
        DividerGap = dividerGap;
        RowGap = rowGap;
    }

    public int DividerGap { get; }
    public int RowGap { get; }
}

internal readonly struct BootstrapBreadcrumbSegmentSize
{
    public BootstrapBreadcrumbSegmentSize(Size itemSize, Size dividerSize, bool hasDivider)
    {
        ItemSize = itemSize;
        DividerSize = dividerSize;
        HasDivider = hasDivider;
    }

    public Size ItemSize { get; }
    public Size DividerSize { get; }
    public bool HasDivider { get; }
}

internal readonly struct BootstrapBreadcrumbSegmentLayout
{
    public BootstrapBreadcrumbSegmentLayout(Rectangle itemBounds, Rectangle dividerBounds)
    {
        ItemBounds = itemBounds;
        DividerBounds = dividerBounds;
    }

    public Rectangle ItemBounds { get; }
    public Rectangle DividerBounds { get; }
}
```

Core API:

```csharp
internal static Size Measure(
    IReadOnlyList<BootstrapBreadcrumbSegmentSize> segments,
    int maximumWidth,
    BootstrapBreadcrumbLayoutMetrics metrics,
    bool wrapContents);

internal static IReadOnlyList<BootstrapBreadcrumbSegmentLayout> Arrange(
    IReadOnlyList<BootstrapBreadcrumbSegmentSize> segments,
    Rectangle contentBounds,
    BootstrapBreadcrumbLayoutMetrics metrics,
    bool wrapContents,
    bool rightToLeft);
```

Rules:

- Negative sizes/gaps are rejected or normalized according to the repository's existing helper conventions; do not permit negative rectangles.
- Empty segments return `Size.Empty` / an empty layout result.
- `maximumWidth <= 0` means unbounded measurement.
- A non-first segment's effective width is `itemWidth + dividerWidth + (2 * dividerGap)` even when divider text is empty/zero-width.
- `Measure` and `Arrange` use the same row-packing rules so preferred size cannot disagree with actual placement.
- LTR and RTL produce mirrored horizontal positions for the same segment widths/content bounds; measured size is direction-independent.
- No live `Control`, theme singleton, `Graphics`, handle, screen, or DPI query is allowed inside the pure layout helper.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItem.cs` — public caller-owned item model and internal text-change notification.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemCollection.cs` — public owner-backed collection, validation, item-change wiring, mutation notifications.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemClickedEventArgs.cs` — public event data with stable item/index.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbLayoutLogic.cs` — pure internal segment measurement/wrap/RTL geometry.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs` — public composite control, native child composition, event mapping, theme/font/DPI/layout/lifecycle.

**Create test files**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbItemCollectionTests.cs` — item defaults, text notification, collection mutation/validation semantics.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbLayoutLogicTests.cs` — pure one-row/wrap/RTL/empty-divider geometry.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbContractTests.cs` — public shape/defaults/designer metadata.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs` — STA composition, activation, accessibility, lifecycle, theme/font/DPI behavior.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BreadcrumbDemoFormTests.cs` — integrated demo smoke test.

**Create demo file**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BreadcrumbDemoForm.cs` — focused Breadcrumb scenarios.

**Modify integration/docs**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — add Breadcrumb to integrated demo navigation.
- `docs/COMPONENTS.md` — document finalized Breadcrumb contract and ownership rules.
- `docs/ARCHITECTURE.md` — add Breadcrumb as native Label/LinkLabel composition under navigation controls.
- `docs/TESTING.md` — add Breadcrumb pure/UI/manual coverage.
- `README.md` — include Breadcrumb in supported navigation controls.
- `docs/PACKAGE_README.md` — include package-facing Breadcrumb summary.
- `CHANGELOG.md` — add compatible Breadcrumb API addition under `Unreleased` without rewriting release history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — deliberately approve the new exported API fingerprint after review.
- `docs/PUBLIC_API_BASELINE.md` — record the new approved fingerprint and reason.

---

### Task 1: Freeze the public item and collection model

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemClickedEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbItemCollectionTests.cs`

**Interfaces:**
- Produces exactly the item, collection, and event-args public APIs in **Breadcrumb Contract**.
- `BootstrapBreadcrumbItemCollection` is owner-backed; its constructor remains internal.
- The collection notifies its owner through one internal callback/delegate; it must not depend on native child controls.

- [ ] **Step 1: Write failing item-model tests** for the parameterless constructor, `BootstrapBreadcrumbItem(string)`, default empty `Text`, default null `Tag`, null-text normalization, text-change notification only when the effective text changes, and no visual-change notification from `Tag` updates.

Use concrete expectations such as:

```csharp
[Test]
public void TextNormalizesNullAndNotifiesOnlyOnEffectiveChange()
{
    var item = new BootstrapBreadcrumbItem("Home");
    var changes = 0;
    item.TextChangedForOwner += (_, _) => changes++;

    item.Text = "Home";
    item.Text = null!;

    Assert.Multiple((Action)(() =>
    {
        Assert.That(item.Text, Is.EqualTo(string.Empty));
        Assert.That(changes, Is.EqualTo(1));
    }));
}
```

The exact internal event/callback name may differ, but it must remain internal and tests may use the assembly's existing internal visibility rather than adding a public test hook.

- [ ] **Step 2: Write failing collection tests** proving Add/Insert/Replace/Remove/Clear each produce exactly one owner mutation notification, `null` throws `ArgumentNullException`, duplicate object references are rejected, removing an item detaches its text-change callback, and collection operations never dispose item models or `Tag` objects.
- [ ] **Step 3: Run pure item/collection tests for `net8.0-windows`; verify RED because the types do not exist.**
- [ ] **Step 4: Implement the minimal item/collection/event-args types.** Keep owner notification internal, detach callbacks before replacement/removal, and attach only after validation succeeds so failed mutations leave the original collection unchanged.
- [ ] **Step 5: Run the item/collection tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 6: Commit** `feat: add breadcrumb item model`.

### Task 2: Implement deterministic wrap and RTL layout logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbLayoutLogicTests.cs`

**Interfaces:**
- Produces the internal `BootstrapBreadcrumbLayoutMetrics`, `BootstrapBreadcrumbSegmentSize`, `BootstrapBreadcrumbSegmentLayout`, `Measure(...)`, and `Arrange(...)` contract described above.
- Consumes only primitive values/`Size`/`Rectangle`; no live WinForms controls.

- [ ] **Step 1: Write failing one-row tests** for zero, one, two, and three items. For three items with sizes `(40x20, 50x20, 30x20)`, divider size `8x20`, `dividerGap=8`, `rowGap=4`, verify the expected full width is `40 + (8+16+50) + (8+16+30) = 188` pixels before outer Padding.
- [ ] **Step 2: Write failing empty-divider tests** proving a zero-width divider still leaves `2 * dividerGap` between adjacent logical items and never creates negative bounds.
- [ ] **Step 3: Write failing wrap tests** using a content width that fits the first two segments but not the third. Assert the third segment starts a second row and that its divider and item share that row.
- [ ] **Step 4: Write failing oversized-segment tests** proving one segment wider than the available width is placed alone, starts at the row origin/edge, and does not cause an infinite loop or negative x-coordinate.
- [ ] **Step 5: Write failing RTL parity tests** proving measured size is identical to LTR and each segment's horizontal placement mirrors within the same content rectangle while the segment order remains collection order.
- [ ] **Step 6: Run** `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapBreadcrumbLayoutLogicTests` **and verify RED because the helper does not exist.**
- [ ] **Step 7: Implement the minimal pure row-packing algorithm.** Keep measurement and arrangement on shared calculations so they cannot drift. Use normal comparisons instead of APIs unavailable on `net48` such as `Math.Clamp`.
- [ ] **Step 8: Run layout tests for both target frameworks; verify GREEN.**
- [ ] **Step 9: Commit** `feat: add breadcrumb layout logic`.

### Task 3: Add the BootstrapBreadcrumb public control and composition contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbContractTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs`

**Interfaces:**
- Public surface is exactly the **Breadcrumb Contract** above; do not add convenience routing/style APIs during implementation.
- Consumes `BootstrapBreadcrumbItemCollection` and `BootstrapBreadcrumbLayoutLogic`.
- Owns all generated native `LinkLabel`/`Label` children and disposes them on rebuild/disposal.

- [ ] **Step 1: Write failing public-contract/default tests** that construct `BootstrapBreadcrumb` and verify `Items.Count=0`, `Divider="/"`, `RightToLeftDivider=null`, `WrapContents=true`, `AutoSize=true`, `AutoSizeMode=GrowAndShrink`, `BackColor=Transparent`, `TabStop=false`, `AccessibleRole.Grouping`, `AccessibleName="Breadcrumb"`, and the default accessible description.
- [ ] **Step 2: Add reflection/metadata tests** proving `Items` is read-only at the property level, uses `DesignerSerializationVisibility.Content`, public item/event types expose only the planned members, and there is no public `CurrentIndex`, routing API, `Variant`, custom color, radius, or child-control exposure.
- [ ] **Step 3: Write failing composition tests**:

```csharp
[Test]
public void ThreeItemsCreateTwoLinksOneCurrentItemAndTwoDividers()
{
    using var breadcrumb = new BootstrapBreadcrumb();
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home"));
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Library"));
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));

    var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();
    var labels = breadcrumb.Controls.OfType<Label>().ToArray();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(links.Select(x => x.Text), Is.EqualTo(new[] { "Home", "Library" }));
        Assert.That(links.All(x => x.TabStop), Is.True);
        Assert.That(labels.Any(x => x.Text == "Data" && !x.TabStop), Is.True);
        Assert.That(labels.Count(x => x.Text == "/"), Is.EqualTo(2));
    }));
}
```

If implementation uses an internal Label subclass, keep the assertions semantic rather than depending on an exact private type name.

- [ ] **Step 4: Add zero/one/two-item tests** proving empty renders no generated children, one item is current/non-link with no divider, and two items yield one link + one divider + one current label.
- [ ] **Step 5: Add mutation tests** proving Add/Insert/Replace/Remove/Clear and `item.Text` updates refresh generated children and preferred size, while changing only `Tag` leaves the generated child instance set unchanged.
- [ ] **Step 6: Add rebuild disposal tests** that capture old generated controls, mutate the collection structurally, and assert removed framework-owned children have `IsDisposed == true` and cannot later raise `ItemClicked`.
- [ ] **Step 7: Run focused Breadcrumb tests with hang protection; verify RED:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -c Release -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter "FullyQualifiedName~BootstrapBreadcrumb"
```

- [ ] **Step 8: Implement the minimal composite control.** Rebuild generated children from `Items`; wire only ancestor LinkLabels; set zero `Margin`/`Padding`; use `UseMnemonic=false`; do not set partial `LinkArea`; use native labels rather than custom painting.
- [ ] **Step 9: Override `GetPreferredSize` and `OnLayout` to feed actual generated-child preferred sizes into `BootstrapBreadcrumbLayoutLogic`.** Outer `Padding` remains caller-owned and is applied around the computed content rectangle.
- [ ] **Step 10: Run focused Breadcrumb tests for both targets; verify GREEN.**
- [ ] **Step 11: Commit** `feat: add BootstrapBreadcrumb composition`.

### Task 4: Preserve native link activation, accessibility, theme, font, DPI, and RTL behavior

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs`

**Interfaces:**
- Ancestor `LinkLabel.LinkClicked` is the one native activation source mapped to `ItemClicked`.
- The Breadcrumb has one theme subscription and at most one framework-owned theme font at a time.
- No timer, animation, custom focus engine, accessibility replacement tree, or top-level message filter is introduced.

- [ ] **Step 1: Add failing ItemClicked tests** that activate the first and middle ancestor links through their native `LinkClicked` path and assert one event with the correct item reference/index, unchanged `Items`, unchanged final/current item, and no event from the current label.
- [ ] **Step 2: Add a reentrancy test** where the `ItemClicked` handler replaces/clears `Items`. Assert the event args retain the item/index captured at activation and the post-handler child tree matches the caller's collection mutation without duplicate events.
- [ ] **Step 3: Add failing focus/tab tests** on a hosted STA Form proving only ancestor links have `TabStop=true`; current/divider controls are skipped; disabling the Breadcrumb prevents effective child activation without changing `Items` or `Tag`.
- [ ] **Step 4: Add accessibility tests** proving ancestor links expose their full text as accessible names, the current label exposes full text plus `"Current page."` description, and the container exposes Breadcrumb grouping/name/description semantics. Explicitly assert no partial `LinkArea` configuration is introduced.
- [ ] **Step 5: Add failing divider/RTL tests** proving `Divider=">"` appears only between items, `Divider=""` removes the visible glyph but preserves gap geometry, `RightToLeftDivider="<"` is used under `RightToLeft.Yes`, visual item flow mirrors, and `ItemClicked.Index` remains the logical collection index.
- [ ] **Step 6: Add failing wrap tests** that constrain the control width, call layout, and assert divider/following-item pairs remain on the same row. Cover both LTR and RTL.
- [ ] **Step 7: Add failing theme tests** under `[NonParallelizable]`: switch Light -> Dark while a Breadcrumb exists and verify link/current/divider colors update in place, generated item order does not change, `Tag` references remain identical, and no duplicate `ItemClicked` handlers appear after repeated theme switches.
- [ ] **Step 8: Add font-ownership tests** proving a framework-created Body font is replaced/disposed when theme typography changes, but a caller-assigned `Font` remains assigned and is not disposed by later theme changes or Breadcrumb disposal.
- [ ] **Step 9: Add DPI/layout tests** for 96/120/144/168/192 logical DPI using the pure metrics boundary where possible, then a handle-backed smoke test confirming larger DPI increases divider/row gaps without changing item/event order.
- [ ] **Step 10: Add lifecycle stress coverage** with at least 100 structural/text/divider/RTL/wrap/theme-affecting state changes, then dispose the Breadcrumb and assert current generated controls are disposed and no later global theme event reaches the disposed control.
- [ ] **Step 11: Run the focused UI tests with `--blame-hang` on both target frameworks; verify RED before implementation and GREEN after implementation.**
- [ ] **Step 12: Implement theme/font/DPI/RTL/accessibility hardening.** Subscribe once in the constructor, unsubscribe in `Dispose(bool)`, replace only owned fonts, update current children in place when a full rebuild is unnecessary, and use `PerformLayout()`/`Invalidate()` only on the Breadcrumb rather than its entire Form.
- [ ] **Step 13: Commit** `test: harden BootstrapBreadcrumb behavior`.

### Task 5: Add an integrated Breadcrumb demo

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BreadcrumbDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BreadcrumbDemoFormTests.cs`

**Interfaces:**
- Demo consumes only the public Breadcrumb API.
- Demo navigation after `ItemClicked` is application-owned; the control itself never edits the trail automatically.

- [ ] **Step 1: Write a failing demo smoke test** that constructs `BreadcrumbDemoForm`, verifies it contains multiple `BootstrapBreadcrumb` examples, and confirms integrated `MainForm` navigation includes a Breadcrumb entry.
- [ ] **Step 2: Add demo scenarios** for:
  - one current item: `Home`;
  - standard hierarchy: `Home / Library / Data`;
  - deeper hierarchy with long labels;
  - custom divider `>`;
  - divider removed with empty string;
  - constrained width with wrapping;
  - RTL with `RightToLeftDivider="<"`;
  - disabled Breadcrumb;
  - live item text mutation;
  - an `ItemClicked` status/output area showing clicked index/text/tag.
- [ ] **Step 3: In one interactive scenario, handle `ItemClicked` in the Form by trimming/replacing the caller-owned `Items` collection to simulate navigation.** Make it visually obvious that this behavior lives in demo/application code, not `BootstrapBreadcrumb`.
- [ ] **Step 4: Add Breadcrumb to `MainForm` near other navigation controls (Tabs/Pagination/TreeView as appropriate to the current demo ordering) with a description covering hierarchy links, current location, configurable divider, wrap, RTL and native keyboard behavior.**
- [ ] **Step 5: Build the demo for `net8.0-windows`; verify zero compile errors.**
- [ ] **Step 6: Run `BreadcrumbDemoFormTests` with hang protection; verify GREEN.**
- [ ] **Step 7: Commit** `demo: add BootstrapBreadcrumb scenarios`.

### Task 6: Finalize documentation and deliberately approve the API addition

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1: Update `docs/COMPONENTS.md`.** Add a finalized `BootstrapBreadcrumb` section with the exact public types/defaults, last-item-current rule, native LinkLabel composition, divider/wrap/RTL behavior, `ItemClicked` ownership boundary, accessibility semantics, and excluded scope from this plan.
- [ ] **Step 2: Update `docs/ARCHITECTURE.md`.** Add Breadcrumb under composite/navigation controls with the dependency model `Breadcrumb -> native LinkLabel/Label + Theme/DPI/Layout helper`. Explicitly state that it owns hierarchy presentation only and not routing/history/page hosting.
- [ ] **Step 3: Update `docs/TESTING.md`** with pure layout tests, item/collection tests, STA native-link activation/accessibility/RTL/wrap/theme/font/lifecycle coverage, and manual real-DPI verification. Do not weaken the existing unattended WinForms execution policy.
- [ ] **Step 4: Update `README.md` and `docs/PACKAGE_README.md`** to advertise `BootstrapBreadcrumb` without claiming built-in routing or automatic navigation-stack management.
- [ ] **Step 5: Add Breadcrumb under `## [Unreleased]` in `CHANGELOG.md`.** Do not rewrite historical release sections.
- [ ] **Step 6: Run the approved API baseline test before changing its fingerprint:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -c Release -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL and print the deterministic actual exported fingerprint.

- [ ] **Step 7: Review the reconstructed exported surface line-by-line.** Intentional new public types are only:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumb : System.Windows.Forms.Panel
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItem
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItemCollection
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItemClickedEventArgs
```

`BootstrapBreadcrumbLayoutLogic` and all layout structs/helpers remain internal and must be absent. No public native-child type, routing interface, timer, or style helper should appear.

- [ ] **Step 8: Copy the reviewed deterministic actual fingerprint into `ApprovedV1Fingerprint` and record the same value/reason in `docs/PUBLIC_API_BASELINE.md`.** Keep `AssemblyVersion` unchanged unless a separate release task explicitly changes it.
- [ ] **Step 9: Rerun API-baseline tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 10: Commit** `docs: finalize BootstrapBreadcrumb contract`.

### Task 7: Complete dual-target verification and manual UI gate

**Files:**
- No new files expected; fix only Breadcrumb-related defects uncovered by verification.

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

- [ ] **Step 3: Run the full bounded test suite:**

```powershell
./test.ps1
```

Expected: both target frameworks pass with no hang timeout, modal dialog, or blame dump.

- [ ] **Step 4: Search the Breadcrumb implementation for prohibited infrastructure.** Confirm there is no `Timer`, `Task.Delay`, `Thread.Sleep`, `MessageBox.Show`, `ShowDialog`, `Application.AddMessageFilter`, global hook, custom top-level window, routing/data-source dependency, custom link hit-test engine, or external icon package.
- [ ] **Step 5: Run the demo and manually verify** zero/one/two/many items; mouse activation; Tab/Shift+Tab; native Enter/link activation; visible focus; current item non-focusability; custom/empty divider; long text; wrapping; repeated resize; LTR/RTL; disabled parent; live Text changes; Light/Dark switching; caller-assigned font; and 100/125/150/175/200% real Windows scaling.
- [ ] **Step 6: Perform an accessibility smoke check** with Windows accessibility inspection tooling or Narrator: ancestor links should expose full names, current page should be understandable as current/non-link, divider glyphs should not masquerade as clickable items, and focus order should contain only actionable ancestors.
- [ ] **Step 7: Verify application ownership explicitly:** clicking an ancestor without a demo handler leaves the trail unchanged; the demo handler that simulates navigation changes `Items` itself. No hidden automatic trimming/history behavior may exist.
- [ ] **Step 8: If verification required code changes, rerun Steps 1-7 and commit fixes as** `fix: harden BootstrapBreadcrumb verification`. **If no fixes were required, do not create an empty commit.**

---

## Definition of Done

`BootstrapBreadcrumb` is complete only when all of the following are true:

- The public API contains exactly the planned Breadcrumb control/item/collection/event-args types with XML documentation and designer metadata.
- The last item is always the current non-link item; all preceding items are native actionable links.
- `ItemClicked` raises exactly once with the correct logical item/index and never mutates navigation state itself.
- Empty, one-item, custom-divider, empty-divider, wrapped, RTL, disabled and long-text cases behave deterministically.
- Divider and following item never split across rows.
- Native LinkLabel remains the keyboard/mouse/accessibility authority; there is no custom focus/hit-test/navigation engine.
- Link/current/divider colors and Body typography follow runtime Light/Dark theme changes while preserving caller-owned fonts.
- Framework spacing scales across supported DPI values and both LTR/RTL layouts remain stable.
- Rebuilds dispose old generated controls, item models remain caller-owned, item callbacks detach correctly, and disposal releases the theme subscription/theme-owned font.
- The control introduces no timer, animation, popup, routing/history, page-hosting, data-binding, custom GDI painting, or external dependency.
- Demo scenarios clearly separate Breadcrumb presentation from caller-owned navigation behavior.
- Both target frameworks build; focused and full tests pass with bounded WinForms hang detection.
- Component, architecture, testing, package, README, changelog, and public API baseline documentation are updated.
- The new exported API fingerprint is deliberately reviewed and approved.

## Self-Review

- **Spec coverage:** Bootstrap's hierarchy links, final current item, configurable/removable divider, muted current/divider appearance, and accessible navigation intent all map to concrete WinForms behavior. WAI's lack of special widget keyboard interaction is preserved by native LinkLabel Tab behavior rather than inventing arrow navigation.
- **Repository fit:** The plan follows the project's native-first philosophy: semantic item state is small and framework-owned, while link focus/activation/accessibility is delegated to native controls. Theme/DPI/layout logic is reused or isolated as pure logic, and no duplicate infrastructure is introduced.
- **Placeholder scan:** No `TBD`, TODO-style implementation hole, vague error-handling instruction, or guessed future API fingerprint remains. The fingerprint is intentionally obtained from the repository's deterministic baseline test after implementation and then reviewed.
- **Type consistency:** `Items` always contains `BootstrapBreadcrumbItem`; `ItemClicked` always uses `BootstrapBreadcrumbItemClickedEventArgs`; `Index` is always the logical collection index; `Divider` is non-null effective text; `RightToLeftDivider` is nullable fallback text; layout types remain internal.
- **Scope check:** Routing, navigation history, URI launching, page hosting, icons, per-item variants, arbitrary color APIs, animated transitions, overflow menus/collapse, drag/drop, data binding, and a custom accessibility tree are intentionally excluded from V1 so Breadcrumb remains one independently testable navigation component.