# BootstrapBreadcrumb Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap-inspired `BootstrapBreadcrumb` navigation control that represents an ordered hierarchy, renders ancestor items as native WinForms links, renders the last item as the current location, supports configurable dividers, wrapping and RTL layout, and preserves deterministic theme/DPI/accessibility/lifecycle behavior without inventing a custom focus or navigation engine.

**Architecture:** `BootstrapBreadcrumb : Panel` owns a small public item collection and dynamically composes native `LinkLabel` controls for ancestor items plus native `Label` controls for the current item and dividers. Native `LinkLabel` remains authoritative for Tab focus, keyboard activation, mouse hit-testing and link accessibility; Breadcrumb only maps `LinkClicked` to `ItemClicked`. A pure internal layout helper positions whole breadcrumb segments, keeps each divider attached to the following item, wraps only at segment boundaries, and mirrors geometry for `RightToLeft.Yes`. The control subscribes once to the shared theme because native Label/LinkLabel children are not Bootstrap primitives and therefore need their font/colors updated by the owner.

**Tech Stack:** C#, native Windows Forms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, theme typography/metrics, `DpiScaler`, NUnit 4, integrated demo application. No external dependency and no custom painting engine.

**Spec:** User request plus Bootstrap 5.3 Breadcrumb behavior (`https://getbootstrap.com/docs/5.3/components/breadcrumb/`) and WAI-ARIA Breadcrumb Pattern (`https://www.w3.org/WAI/ARIA/apg/patterns/breadcrumb/`). This plan's **Breadcrumb contract** is the WinForms adaptation baseline. Project-wide constraints come from `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, `docs/COMPONENTS.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; all new public Breadcrumb types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared code path wherever practical.
- Keep Breadcrumb a lightweight navigation composition. Do not add routing, URI opening, history management, page hosting, back/forward stacks, application state, async loading, menus, popup infrastructure, timers, animation, or data binding.
- Bootstrap Breadcrumb is a hierarchy trail, not a selectable toolbar. The last item is always the current location and is never a link; every preceding item is an actionable link while the Breadcrumb itself is enabled.
- Activating an ancestor raises `ItemClicked`; the framework does **not** automatically remove descendants, replace the last item, navigate a Form, or mutate the item collection. Application code owns the resulting navigation and collection update.
- Reuse native `LinkLabel` semantics for actionable ancestors. Do not paint link hit areas manually, install global keyboard hooks, synthesize an arrow-key roving-focus model, or replace native UI Automation objects.
- Do not set partial `LinkArea` ranges. Each ancestor LinkLabel represents one complete breadcrumb item so its accessible link name is the full item text.
- The current item and divider controls are non-focusable. Only actionable ancestor links participate in Tab/Shift+Tab traversal.
- Do not add special Left/Right/Home/End keyboard navigation. WAI-ARIA Breadcrumb defines no additional widget keyboard interaction; normal native link Tab behavior is sufficient.
- Default divider text is `/`, matching Bootstrap. Empty divider text is valid and removes the visible glyph while preserving inter-item spacing.
- `RightToLeftDivider = null` means reuse `Divider`; callers may provide an explicit directional divider such as `<` when RTL needs a flipped symbol.
- `WrapContents = true` wraps only between logical breadcrumb segments. A divider must never be stranded at the end of a row without the item it introduces.
- The item collection is authoritative. Do not mirror it into a second public navigation model.
- Public item models are caller-owned semantic data. Breadcrumb owns and disposes only native child controls it creates; it never disposes `BootstrapBreadcrumbItem` instances or caller-owned `Tag` objects.
- All framework-owned spacing comes from current theme metrics and scales through `DpiScaler`; do not hard-code repeated device-pixel gaps.
- Link color is `BootstrapThemeManager.CurrentTheme.Colors.Primary`. Divider/current-item color is `MutedText`. Disabled link/current/divider presentation uses `Colors.Disabled`.
- Do not add a Breadcrumb-specific `Variant`, arbitrary color properties, background, border, or radius in V1. Bootstrap 5's Breadcrumb baseline is intentionally minimal.
- Use the current theme Body typography while the control still owns its font. If the caller explicitly assigns `Font`, stop replacing that font on later theme changes and never dispose the caller-owned font.
- Theme subscription must be paired with deterministic unsubscribe in `Dispose(bool)` and remain safe across handle recreation.
- Designer construction must be parameterless and must not require application bootstrap or initialized external services.
- Breadcrumb introduces no per-item GDI resource ownership, no custom `Graphics` painting, no `Region`, and no external icon package.
- Tests that create WinForms handles or exercise link interaction must run STA. Tests that mutate global theme state must also be non-parallel.
- Every focused raw `dotnet test` command for UI tests must include `--blame-hang --blame-hang-timeout 5m`; the full suite should run through `./test.ps1`.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5.3 defines Breadcrumb as an ordered/unordered hierarchy list. Parent items are links, the final item represents the current page, dividers appear between items, divider text can be customized or removed, and divider/current colors use secondary text semantics. Bootstrap does not require a Breadcrumb background, border, or radius.

The WinForms adaptation is:

```text
Items: Home -> Library -> Data

LTR:
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
2. `Items.Count == 1` renders one current-location `Label`; no divider or link exists.
3. For `Items.Count > 1`, indices `0..Count-2` render as `LinkLabel`; index `Count-1` renders as the current-location `Label`.
4. Dividers exist only between items; never leading or trailing.
5. Link activation raises exactly one `ItemClicked` with the item and its logical collection index.
6. Link activation does not alter `Items` before or after the event.
7. A disposed/stale generated LinkLabel must not raise `ItemClicked` after a rebuild.
8. The final item's `Tag` is preserved like every other item even though it is not actionable.
9. Changing an item's `Text` refreshes presentation and preferred size without changing order or `Tag`.
10. Changing only `Tag` is semantic-only and does not rebuild generated controls.
11. `Enabled=false` on Breadcrumb disables effective child interaction through native parent/child semantics and switches generated child colors to `Colors.Disabled`; it does not mutate item data.
12. `RightToLeft.Yes` mirrors visual flow while preserving collection order and event indices.
13. Wrapping may move the current item to a later row, but divider + following item remain one logical segment.
14. Breadcrumb imposes no Bootstrap web `margin-bottom`; host WinForms layout owns external spacing.

Useful references:

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

    public string Text { get; set; }      // default "", null assignment => ""
    public object? Tag { get; set; }      // default null

    internal event EventHandler? TextChangedForOwner;
}
```

Exact internal behavior:

```csharp
public string Text
{
    get => _text;
    set
    {
        var normalized = value ?? string.Empty;
        if (_text == normalized)
        {
            return;
        }

        _text = normalized;
        TextChangedForOwner?.Invoke(this, EventArgs.Empty);
    }
}
```

- `Text` is the complete visual/accessibility label.
- `Tag` is caller-owned routing/application metadata and has no rendering semantics.
- `Tag` changes do not raise `TextChangedForOwner`.
- Item models are never disposed by Breadcrumb.

### Public collection

```csharp
public sealed class BootstrapBreadcrumbItemCollection
    : System.Collections.ObjectModel.Collection<BootstrapBreadcrumbItem>
{
    internal BootstrapBreadcrumbItemCollection(Action ownerChanged);
}
```

Exact collection rules:

- `ownerChanged == null` throws `ArgumentNullException` from the internal constructor.
- `InsertItem`/`SetItem` reject `null` with `ArgumentNullException`.
- The same object reference cannot appear twice in one collection; adding/replacing with a duplicate reference throws `ArgumentException` before changing the collection.
- Distinct item instances may have identical text/tag values.
- On Insert/Set/Remove/Clear, attach/detach `TextChangedForOwner` exactly once per contained instance.
- After an effective structural mutation, invoke the owner callback exactly once.
- On an item `TextChangedForOwner`, invoke the owner callback exactly once.
- A failed mutation leaves the original collection/subscriptions unchanged.

### Public event args

```csharp
public sealed class BootstrapBreadcrumbItemClickedEventArgs : EventArgs
{
    internal BootstrapBreadcrumbItemClickedEventArgs(
        BootstrapBreadcrumbItem item,
        int index);

    public BootstrapBreadcrumbItem Item { get; }
    public int Index { get; }
}
```

Constructor rules: `item == null` throws `ArgumentNullException`; `index < 0` throws `ArgumentOutOfRangeException`.

### Public control

```csharp
[DefaultEvent(nameof(ItemClicked))]
public class BootstrapBreadcrumb : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapBreadcrumbItemCollection Items { get; }

    public string Divider { get; set; }                  // default "/"; null => ""
    public string? RightToLeftDivider { get; set; }      // default null => Divider
    public bool WrapContents { get; set; }               // default true

    public event EventHandler<BootstrapBreadcrumbItemClickedEventArgs>? ItemClicked;
}
```

Default state:

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

Public API rules:

- Do not add `CurrentIndex`, `SelectedIndex`, `ActiveItem`, `NavigateTo`, `NavigateBack`, `Uri`, `Route`, or automatic descendant trimming in V1.
- Do not add `Variant`, `BorderRadius`, background/divider/link/current color properties, or icon properties in V1.
- Do not expose generated `LinkLabel`/`Label` controls as public Breadcrumb concepts.
- Inherited `Controls` remains normal WinForms API, but generated children are framework-owned implementation details; docs/demo use `Items` only.
- Public members receive XML documentation and appropriate WinForms designer attributes.

---

## Generated Child Contract

### Ancestor LinkLabel

Each ancestor item is one native `LinkLabel`:

```text
AutoSize          = true
TabStop           = true
UseMnemonic       = false
LinkBehavior      = AlwaysUnderline
LinkVisited       = false
LinkColor         = theme.Colors.Primary
VisitedLinkColor  = theme.Colors.Primary
ActiveLinkColor   = theme.Colors.Primary
DisabledLinkColor = theme.Colors.Disabled
BackColor         = Transparent
Margin            = 0
Padding           = 0
Text              = item.Text
AccessibleName    = item.Text
```

Do not set partial `LinkArea`; the entire text remains the link.

### Current item Label

The final item is one native `Label`:

```text
AutoSize              = true
TabStop               = false
UseMnemonic           = false
BackColor             = Transparent
ForeColor             = enabled ? theme.Colors.MutedText : theme.Colors.Disabled
Margin                 = 0
Padding                = 0
Text                   = item.Text
AccessibleName         = item.Text
AccessibleDescription  = "Current page."
```

### Divider Label

Every divider is one native `Label`:

```text
AutoSize              = true
TabStop               = false
UseMnemonic           = false
BackColor             = Transparent
ForeColor             = enabled ? theme.Colors.MutedText : theme.Colors.Disabled
AccessibleRole        = None
AccessibleName        = ""
AccessibleDescription = ""
Margin                 = 0
Padding                = 0
```

- LTR divider text: `Divider`.
- RTL divider text: `RightToLeftDivider ?? Divider`.
- `Divider = ""` means the divider Label has empty text/zero glyph width, but layout still preserves two divider gaps between adjacent items.
- Manual accessibility verification must confirm divider controls do not present as actionable navigation items. Do not create a replacement accessibility tree solely to hide separators in V1.

### Activation mapping

Each ancestor LinkLabel receives exactly one `LinkClicked` handler. Build the handler with the item instance, not a stale numeric index. At activation:

```csharp
var index = Items.IndexOf(item);
if (index < 0 || index >= Items.Count - 1)
{
    return;
}

ItemClicked?.Invoke(
    this,
    new BootstrapBreadcrumbItemClickedEventArgs(item, index));
```

This rule prevents a stale child from acting after collection mutation and ensures a formerly-current/reordered item is evaluated against current collection state.

---

## Theme, Font, DPI, Wrapping, and RTL Contract

### Font ownership

- Constructor creates a font from `BootstrapThemeManager.CurrentTheme.Typography.Body` and marks it framework-owned.
- Child controls reference Breadcrumb's current `Font`; they do not own separate fonts.
- Theme change replaces/disposes the previous framework-owned font and updates current children.
- Override `OnFontChanged`; when the change did not originate from framework theme application, mark font ownership caller-controlled.
- A caller-assigned font is never replaced/disposed by later theme changes or Breadcrumb disposal.

### DPI metrics

Resolve one internal layout metrics value from current theme and current DPI:

```text
DividerGap = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi)
RowGap     = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)
```

No other framework-owned fixed spacing is added. Text sizes come from normal child `GetPreferredSize`/font measurement.

### Logical segments

Treat each item after the first as one indivisible segment:

```text
LTR: [gap][divider][gap][item]
RTL: [item][gap][divider][gap]
```

The first item segment contains only the item.

`WrapContents=false` always measures/arranges one row.

`WrapContents=true`:

- use a positive available/proposed content width as the wrap boundary;
- if width is non-positive, treat measurement as unbounded;
- move a whole segment to the next row when it does not fit and the current row already contains at least one segment;
- never split divider from the following item;
- a segment wider than the row is placed alone; clip normally rather than generating negative rectangles;
- vertically center each child within the maximum row height;
- use `RowGap` only between rows.

### RTL

- `RightToLeft.No`: logical item 0 starts at the left and subsequent segments advance right.
- `RightToLeft.Yes`: logical item 0 starts at the right and subsequent segments advance left.
- Never reverse `Items` to implement RTL.
- `ItemClicked.Index` always means logical collection index.
- `RightToLeftDivider ?? Divider` is the only divider-flipping mechanism; do not attempt to infer mirroring for arbitrary Unicode text.

---

## Internal Layout Contract

Create `BootstrapBreadcrumbLayoutLogic.cs` with these exact internal types/signatures:

```csharp
internal readonly struct BootstrapBreadcrumbLayoutMetrics
{
    public BootstrapBreadcrumbLayoutMetrics(int dividerGap, int rowGap);
    public int DividerGap { get; }
    public int RowGap { get; }
}

internal readonly struct BootstrapBreadcrumbSegmentSize
{
    public BootstrapBreadcrumbSegmentSize(
        Size itemSize,
        Size dividerSize,
        bool hasDivider);

    public Size ItemSize { get; }
    public Size DividerSize { get; }
    public bool HasDivider { get; }
}

internal readonly struct BootstrapBreadcrumbSegmentLayout
{
    public BootstrapBreadcrumbSegmentLayout(
        Rectangle itemBounds,
        Rectangle dividerBounds);

    public Rectangle ItemBounds { get; }
    public Rectangle DividerBounds { get; }
}

internal static class BootstrapBreadcrumbLayoutLogic
{
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
}
```

Validation/geometry rules:

- `segments == null` throws `ArgumentNullException`.
- `DividerGap < 0` or `RowGap < 0` throws `ArgumentOutOfRangeException` from the metrics constructor.
- Any negative item/divider width or height passed to `BootstrapBreadcrumbSegmentSize` throws `ArgumentOutOfRangeException`.
- `Measure`: `maximumWidth <= 0` means unbounded width.
- `Arrange`: normalize `contentBounds.Width`/`Height` below zero to zero before packing.
- Empty segments return `Size.Empty` and an empty layout list.
- For a non-first segment, effective width is `itemWidth + dividerWidth + (2 * DividerGap)` even when divider width is zero.
- `Measure` and `Arrange` share the same row-packing routine/decision logic so preferred size cannot disagree with placement.
- LTR/RTL have identical measured size; arrangement mirrors x-coordinates within the same content bounds.
- The helper must not depend on a live `Control`, `Graphics`, theme singleton, handles, screen state, or DPI query.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItem.cs` — public item model + internal text-change event.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemCollection.cs` — public owner-backed collection + mutation wiring.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemClickedEventArgs.cs` — public event data.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbLayoutLogic.cs` — internal pure segment layout.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs` — public composite control, native children, events, theme/font/DPI/layout/lifecycle.

**Create test files**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbItemCollectionTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbLayoutLogicTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbContractTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BreadcrumbDemoFormTests.cs`

**Create demo file**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BreadcrumbDemoForm.cs`

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

### Task 1: Freeze the public item, collection, and event model

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemCollection.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbItemClickedEventArgs.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbItemCollectionTests.cs`

**Interfaces:**
- Produces exactly the item/collection/event-args APIs above.
- Collection owner callback is exactly `Action ownerChanged` through the internal constructor.
- Item visual notification is exactly internal event `TextChangedForOwner`.

- [ ] **Step 1: Write failing item tests** for parameterless/string constructors, default empty `Text`, null `Tag`, null-text normalization, no duplicate text-change event for equal values, exactly one event for an effective text change, and zero text-change events when only `Tag` changes.

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

- [ ] **Step 2: Write failing collection tests** for Add/Insert/Set/Remove/Clear exactly-once owner notification, null rejection, duplicate-reference rejection, failed-mutation rollback, text-change forwarding, callback detachment after removal/replacement/clear, and no disposal of item/Tag objects.
- [ ] **Step 3: Write failing event-args tests** for item/index preservation plus null/negative validation through internal construction.
- [ ] **Step 4: Run item/collection tests on `net8.0-windows`; verify RED because types do not exist.**
- [ ] **Step 5: Implement the minimal three public types and exact internal wiring above.** Validate before changing subscriptions/collection state.
- [ ] **Step 6: Run item/collection tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 7: Commit** `feat: add breadcrumb item model`.

### Task 2: Implement deterministic wrap and RTL layout logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumbLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbLayoutLogicTests.cs`

**Interfaces:**
- Produces exactly the internal layout types/signatures above.
- Consumes only primitive values, `Size`, `Rectangle`, and `IReadOnlyList<T>`.

- [ ] **Step 1: Write failing validation/empty tests** for null segments, negative metrics/sizes, empty measurement, empty arrangement, and unbounded measurement when `maximumWidth <= 0`.
- [ ] **Step 2: Write failing one-row tests.** For item sizes `40x20`, `50x20`, `30x20`, divider size `8x20`, `DividerGap=8`, `RowGap=4`, verify width `40 + (8+16+50) + (8+16+30) = 188`.
- [ ] **Step 3: Write failing empty-divider tests** proving zero divider width still leaves `16` pixels between adjacent items at `DividerGap=8`.
- [ ] **Step 4: Write failing wrap tests** where the first two segments fit but the third does not; third divider/item must share row 2.
- [ ] **Step 5: Write failing oversized-segment tests** proving an item wider than the available row is placed alone without an infinite loop or negative geometry.
- [ ] **Step 6: Write failing RTL parity tests** proving measured size matches LTR and x-coordinate placement mirrors inside the same rectangle while result order stays logical-item order.
- [ ] **Step 7: Run and verify RED:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapBreadcrumbLayoutLogicTests
```

- [ ] **Step 8: Implement the minimal pure row-packing algorithm, sharing pack decisions between `Measure` and `Arrange`.** Do not use `Math.Clamp`.
- [ ] **Step 9: Run layout tests on both targets; verify GREEN.**
- [ ] **Step 10: Commit** `feat: add breadcrumb layout logic`.

### Task 3: Add the BootstrapBreadcrumb public control and generated-child composition

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbContractTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs`

**Interfaces:**
- Public surface is exactly **Breadcrumb Contract**.
- Consumes `BootstrapBreadcrumbItemCollection` and `BootstrapBreadcrumbLayoutLogic`.
- Owns/disposes every generated native child.

- [ ] **Step 1: Write failing default/metadata tests** for all default state values, `[DefaultEvent(nameof(ItemClicked))]`, `Items` content serialization, public type/member shape, and absence of prohibited routing/style APIs.
- [ ] **Step 2: Write failing composition tests** for 0/1/2/3 items. Three items must create two LinkLabels, one current Label, and two divider Labels in semantic order.

```csharp
[Test]
public void ThreeItemsCreateTwoLinksAndOneCurrentItem()
{
    using var breadcrumb = new BootstrapBreadcrumb();
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home"));
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Library"));
    breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));

    var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(links.Select(x => x.Text), Is.EqualTo(new[] { "Home", "Library" }));
        Assert.That(links.All(x => x.TabStop), Is.True);
        Assert.That(breadcrumb.Controls.OfType<Label>().Any(x => x.Text == "Data" && !x.TabStop), Is.True);
        Assert.That(breadcrumb.Controls.OfType<Label>().Count(x => x.Text == "/"), Is.EqualTo(2));
    }));
}
```

- [ ] **Step 3: Write failing generated-child property tests** for exact LinkLabel/current/divider configuration from **Generated Child Contract**, including full-link area/no partial `LinkArea` and accessible names.
- [ ] **Step 4: Write failing mutation tests** proving structural changes and item Text changes refresh generated children/preferred size, while changing only `Tag` preserves the same generated child instances.
- [ ] **Step 5: Write failing rebuild-disposal tests** capturing old children, structurally mutating `Items`, and asserting old framework-owned children are disposed.
- [ ] **Step 6: Run focused tests with hang protection; verify RED:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -c Release -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter "FullyQualifiedName~BootstrapBreadcrumb"
```

- [ ] **Step 7: Implement constructor, property validation/normalization, `Items` owner callback, generated-child rebuild, and exact child configuration.** Wire ancestor `LinkClicked` once per generated LinkLabel.
- [ ] **Step 8: Override `GetPreferredSize` and `OnLayout` to measure generated children and call `BootstrapBreadcrumbLayoutLogic`.** Apply caller-owned outer `Padding` around the content rectangle.
- [ ] **Step 9: Run focused tests on both targets; verify GREEN.**
- [ ] **Step 10: Commit** `feat: add BootstrapBreadcrumb composition`.

### Task 4: Preserve activation, accessibility, theme/font, DPI, wrapping, RTL, and lifecycle

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBreadcrumb.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBreadcrumbTests.cs`

**Interfaces:**
- Native `LinkLabel.LinkClicked` is the only activation source mapped to `ItemClicked`.
- Breadcrumb has exactly one theme subscription and at most one framework-owned font.
- No timer, animation, custom focus engine, message filter, or custom accessibility tree.

- [ ] **Step 1: Add deterministic LinkClicked test helper inside the test file** using reflection to invoke protected `LinkLabel.OnLinkClicked` rather than `SendKeys` or sleeps:

```csharp
private static void Activate(LinkLabel link)
{
    var method = typeof(LinkLabel).GetMethod(
        "OnLinkClicked",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    method.Invoke(
        link,
        new object[] { new LinkLabelLinkClickedEventArgs(link.Links[0]) });
}
```

Use this only in tests; add no product test hook.

- [ ] **Step 2: Add failing ItemClicked tests** for first/middle links: exactly one event, correct item/index, unchanged Items/final item, and no current/divider activation path.
- [ ] **Step 3: Add a reentrancy test** whose handler clears/replaces `Items`; event args remain stable and the resulting child tree matches caller mutation with no duplicate event.
- [ ] **Step 4: Add stale-link tests**: capture a LinkLabel, rebuild Items, verify the old control is disposed; invoking its former native path must not produce a valid ancestor event.
- [ ] **Step 5: Add hosted STA focus/tab tests** proving only ancestor links are tabbable and parent `Enabled=false` prevents effective activation without mutating item data.
- [ ] **Step 6: Add accessibility tests** for full ancestor names, current `"Current page."` description, container Grouping/Name/Description, divider `AccessibleRole.None`, and empty divider accessible name/description.
- [ ] **Step 7: Add divider/RTL tests** for `Divider=">"`, `Divider=""`, `RightToLeftDivider="<"`, mirrored geometry, and logical event indices under RTL.
- [ ] **Step 8: Add wrap tests** that constrain the content width and assert each non-first divider and its following item have equal Y/row coordinates in LTR and RTL.
- [ ] **Step 9: Add `[NonParallelizable]` theme tests**: Light -> Dark updates link/current/divider colors without changing item order/Tag references or multiplying handlers.
- [ ] **Step 10: Add font ownership tests**: framework theme font is replaceable/disposable by the control; caller-assigned Font remains assigned and undisposed through theme changes and final control disposal.
- [ ] **Step 11: Add DPI metric tests** for 96/120/144/168/192 using the exact `SpacingSM`/`SpacingXS` mappings, plus a handle-backed smoke test that larger DPI increases gaps without changing event/index semantics.
- [ ] **Step 12: Add lifecycle stress test** with at least 100 structural/text/divider/RTL/wrap/theme-affecting changes, then dispose and assert current generated children are disposed and later theme notifications cause no callback.
- [ ] **Step 13: Run focused Breadcrumb UI tests with `--blame-hang` on both targets; verify RED before missing hardening and GREEN after implementation.**
- [ ] **Step 14: Implement theme/font/DPI/RTL/accessibility/lifecycle behavior exactly as specified.** Subscribe once, unsubscribe in `Dispose(bool)`, dispose only framework-owned fonts, and limit layout/invalidation to Breadcrumb.
- [ ] **Step 15: Commit** `test: harden BootstrapBreadcrumb behavior`.

### Task 5: Add an integrated Breadcrumb demo

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BreadcrumbDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BreadcrumbDemoFormTests.cs`

**Interfaces:**
- Demo uses only public Breadcrumb API.
- Any trail mutation after `ItemClicked` is demo/application code, never hidden control behavior.

- [ ] **Step 1: Write a failing demo smoke test** that constructs `BreadcrumbDemoForm`, finds multiple Breadcrumb examples, and verifies `MainForm` integrated navigation contains Breadcrumb.
- [ ] **Step 2: Implement demo scenarios:** one current item; `Home / Library / Data`; deep/long hierarchy; `>` divider; empty divider; constrained wrapping; RTL with `<`; disabled Breadcrumb; live item Text mutation; and an output label showing clicked index/text/tag.
- [ ] **Step 3: Add one interactive scenario whose Form-level `ItemClicked` handler trims/replaces `Items` to simulate navigation.** Comment the demo code to make caller ownership explicit.
- [ ] **Step 4: Add Breadcrumb to `MainForm` near other navigation controls with a concise description of hierarchy links/current item/divider/wrap/RTL/native keyboard behavior.**
- [ ] **Step 5: Build the demo for `net8.0-windows`; verify zero compile errors.**
- [ ] **Step 6: Run `BreadcrumbDemoFormTests` with hang protection; verify GREEN.**
- [ ] **Step 7: Commit** `demo: add BootstrapBreadcrumb scenarios`.

### Task 6: Finalize docs and deliberately approve the API addition

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1: Update `docs/COMPONENTS.md`** with exact Breadcrumb public types/defaults, last-item-current rule, native LinkLabel composition, divider/wrap/RTL, `ItemClicked` ownership boundary, accessibility semantics, and excluded scope.
- [ ] **Step 2: Update `docs/ARCHITECTURE.md`** with `Breadcrumb -> native LinkLabel/Label + Theme/DPI + pure layout helper`; explicitly exclude routing/history/page hosting.
- [ ] **Step 3: Update `docs/TESTING.md`** with pure layout/item tests, STA activation/accessibility/RTL/wrap/theme/font/lifecycle tests, manual real-DPI checks, and existing bounded test policy.
- [ ] **Step 4: Update `README.md` and `docs/PACKAGE_README.md`** without claiming built-in routing/navigation history.
- [ ] **Step 5: Add Breadcrumb under `## [Unreleased]` in `CHANGELOG.md`; do not rewrite historical releases.**
- [ ] **Step 6: Run API baseline before changing its hash:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -c Release -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL and print deterministic actual exported fingerprint.

- [ ] **Step 7: Review exported surface line-by-line.** Intentional new public types only:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumb : System.Windows.Forms.Panel
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItem
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItemCollection
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBreadcrumbItemClickedEventArgs
```

Internal layout types/events/callbacks must be absent from exported output.

- [ ] **Step 8: Copy reviewed actual fingerprint into `ApprovedV1Fingerprint` and `docs/PUBLIC_API_BASELINE.md`, recording Breadcrumb as an intentional compatible addition.** Keep `AssemblyVersion` unchanged unless a separate release task changes it.
- [ ] **Step 9: Rerun API baseline on `net8.0-windows` and `net48`; verify GREEN.**
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

- [ ] **Step 3: Run the full bounded suite:**

```powershell
./test.ps1
```

Expected: both targets pass with no hang timeout, modal dialog, or blame dump.

- [ ] **Step 4: Search Breadcrumb product files for prohibited infrastructure.** Confirm no `Timer`, `Task.Delay`, `Thread.Sleep`, `MessageBox.Show`, `ShowDialog`, `Application.AddMessageFilter`, global hook, top-level window, routing/data-source dependency, custom link hit-test engine, or external icon package.
- [ ] **Step 5: Run demo/manual checks:** 0/1/2/many items; mouse; Tab/Shift+Tab; native Enter activation; visible focus; current item non-focusability; custom/empty divider; long text; wrapping; repeated resize; LTR/RTL; disabled parent; live Text changes; Light/Dark; caller font; 100/125/150/175/200% Windows scaling.
- [ ] **Step 6: Accessibility smoke check** with Narrator or Windows accessibility inspection: ancestor links expose full names; current item is understandable as current/non-link; dividers are not actionable; focus order contains only ancestors.
- [ ] **Step 7: Verify ownership boundary:** activating an ancestor without an application handler leaves the trail unchanged; the demo handler that simulates navigation changes `Items` itself.
- [ ] **Step 8: If verification requires code changes, rerun Steps 1-7 and commit** `fix: harden BootstrapBreadcrumb verification`. **If no fixes are required, do not create an empty commit.**

---

## Definition of Done

- Public API contains exactly the planned Breadcrumb control/item/collection/event-args types with XML docs/designer metadata.
- Last item is always current non-link; preceding items are native actionable links.
- `ItemClicked` raises exactly once with current logical item/index and never mutates navigation state itself.
- Empty, one-item, custom-divider, empty-divider, wrapped, RTL, disabled, long-text cases are deterministic.
- Divider + following item never split across rows.
- Native LinkLabel remains keyboard/mouse/accessibility authority; no custom focus/hit-test/navigation engine exists.
- Link/current/divider colors and Body typography follow runtime theme changes while respecting caller Font ownership.
- Spacing scales across supported DPI values and LTR/RTL layout remains stable.
- Rebuilds dispose old generated controls; item models remain caller-owned; callbacks/theme/font lifetimes are deterministic.
- No timer, animation, popup, routing/history, page hosting, data binding, custom GDI painting, or external dependency is added.
- Demo clearly separates Breadcrumb presentation from caller-owned navigation behavior.
- Both targets build; focused/full tests pass with bounded WinForms hang detection.
- Component/architecture/testing/package/README/changelog/API-baseline docs are updated.
- New exported fingerprint is intentionally reviewed and approved.

## Self-Review

- **Spec coverage:** Bootstrap hierarchy links, final current item, customizable/removable divider, muted current/divider appearance, and accessibility intent map to concrete WinForms behavior. WAI's absence of special keyboard interaction is preserved by native LinkLabel behavior.
- **Repository fit:** Native-first behavior is preserved; theme/DPI infrastructure is reused; only row-packing geometry becomes a pure internal helper; no duplicate global infrastructure is introduced.
- **Placeholder scan:** No `TBD`, TODO, alternative implementation branch, unspecified helper, or guessed future fingerprint remains. Internal names/signatures used by tasks are defined exactly above.
- **Type consistency:** `Items` contains `BootstrapBreadcrumbItem`; `ItemClicked` always uses `BootstrapBreadcrumbItemClickedEventArgs`; `Index` is logical collection index; `Divider` has non-null effective text; `RightToLeftDivider` is nullable fallback; layout types remain internal.
- **Scope check:** Routing, history, URI launching, page hosting, icons, per-item variants, arbitrary colors, animation, overflow menus/collapse, drag/drop, data binding, and custom accessibility tree are intentionally excluded so Breadcrumb remains one independently testable component.