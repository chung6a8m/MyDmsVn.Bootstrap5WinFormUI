# BootstrapDropdown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 7 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired `BootstrapDropdown` command menu that composes an existing `BootstrapButton` target with native WinForms `ToolStripDropDown` behavior, preserves native focus/keyboard/outside-click dismissal and working-area placement, supports text/icons/checked/disabled/separator command items, and integrates with the framework theme, icon, DPI, lifecycle, demo, documentation, and frozen-public-API processes.

**Architecture:** `BootstrapDropdown : Component` owns exactly one native `ToolStripDropDownMenu` (a menu-specialized `ToolStripDropDown`) and one framework `BootstrapDropdownRenderer`; the caller owns the `BootstrapButton` target and public `BootstrapDropdownItem` models. The public item collection is the source of truth; each opening rebuilds a short-lived native `ToolStripMenuItem`/`ToolStripSeparator` snapshot so mutable model values are applied coherently without inventing a second command model or live collection synchronization engine. Native WinForms remains authoritative for popup activation, focus, Up/Down/Home/End/Enter/Escape navigation, outside-click dismissal, message-loop behavior, auto-close, and screen working-area placement; framework code owns only target wiring, item-to-command mapping, theme/DPI presentation, icon bitmap generation, deterministic resource cleanup, and public lifecycle events.

**Tech Stack:** C#, native Windows Forms `ToolStripDropDownMenu` / `ToolStripMenuItem` / `ToolStripSeparator`, existing `BootstrapButton`, Theme / Rendering / Icons / Compatibility infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `ColorUtil`, `IconDescriptor`, `IIconRenderer`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 7 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public Dropdown types remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Preserve roadmap order: Stage 6 (`BootstrapComboBox`) must be complete and green before Stage 7 implementation begins. If `NavigationDemoForm.cs` is absent when this plan is executed, that is evidence an earlier roadmap stage is incomplete; complete the earlier stage instead of creating a competing navigation demo from Stage 7.
- `BootstrapDropdown` is a command popup, not a data selector. Do not reuse `BootstrapComboBox`, ComboBox data items, binding APIs, or selection semantics.
- `BootstrapDropdown` must not create a transparent/custom top-level `Form`, install a global mouse/keyboard hook, replace the WinForms message loop, or implement a second popup/focus engine.
- Use one owned native `ToolStripDropDownMenu`. `ToolStripDropDownMenu` derives from `ToolStripDropDown` and is selected only because it supplies the native check/image-margin behavior appropriate for command menus.
- Keep native `AutoClose = true`. Outside-click, focus-loss, item activation, Escape, and native close reasons remain WinForms behavior.
- `Target` is caller-owned. Dropdown may attach `Click` and `Disposed` handlers, but replacement/disposal must detach them and Dropdown must never dispose the target.
- Public `BootstrapDropdownItem` instances are caller-owned command models. Dropdown does not dispose them and does not replace them with a second public/native model.
- Native `ToolStripItem` instances created from the public models are owned by Dropdown. Clear/rebuild/dispose paths must dispose them deterministically after detaching framework handlers.
- Native menu rows are a snapshot of `Items` at `Show()` time. Structural/property changes while the menu is open are not a live-binding contract; they are applied coherently on the next opening. This keeps command-state semantics explicit and avoids a second property-change infrastructure that the roadmap does not request.
- `BootstrapDropdownItem.Checked` is presentation state, not automatic toggle state. Activation never flips `Checked`; callers may update it in the item's `Click` handler and the next opening reflects the new value.
- Enabled command activation raises the model `Click` exactly once and native `AutoClose` closes the menu. Disabled rows and separators do not activate.
- Target click toggling only operates while the target is enabled and `Loading == false`. `BootstrapButton` already suppresses its own click while loading; Dropdown still checks the state defensively before opening.
- `Show()` without an assigned `Target` throws `InvalidOperationException`. Construction with `Target == null` remains designer-safe; only an explicit show request requires an anchor.
- `Show()` is a no-op for a disposed/disabled/loading target, for an already-open popup, or for an empty `Items` collection. These no-op paths do not raise `Opened`.
- `Close()` is idempotent. Calling it while closed does not raise `Closed`.
- `Opened` and `Closed` are raised only by forwarding the owned native popup's actual open/closed lifecycle, so one native transition produces one framework event.
- `Variant` defaults to `BootstrapVariant.Primary`, is validated with the shared enum contract, and controls the renderer's semantic selection/check accent. Undefined enum values throw `ArgumentOutOfRangeException`.
- `MinimumWidth` defaults to `0`, is expressed as a logical 96-DPI value, and rejects negative values. `0` means no extra framework minimum; native content measurement remains authoritative. Non-zero values scale through `DpiScaler` before being applied to the popup.
- Do not silently force the popup width to equal the target width. Applications that need that policy can set `MinimumWidth` explicitly; native content may make the popup wider.
- The popup remains rectangular OS/native ToolStrip chrome. Do not add `BorderRadius`, a top-level window region, layered-window transparency, or custom shadow/window-placement code in Stage 7.
- Reuse `BootstrapVariantColorResolver`, `ColorUtil.Blend`, theme `Surface`/`Border`/`Text`/`MutedText`/`Disabled` tokens, and existing spacing/border metrics. Do not add a second dropdown color table with fixed RGB values.
- Menu hover/keyboard-hot background uses a subtle blend of the semantic `Variant` onto the theme surface; normal rows use theme surface/text; disabled rows use muted/disabled tokens; checked indication uses the semantic variant. The renderer does not replace native selection/focus state, only paints it.
- Icons remain `IconDescriptor` values rendered by the target button's current `IconRenderer`, preserving application-provided icon adapters without adding another public renderer property. Native menu snapshots own any generated `Bitmap` objects and dispose them deterministically.
- If no item contains an icon, `ShowImageMargin` is false. If no item is checked, `ShowCheckMargin` is false. If either is present, native menu margins stay aligned across all rows.
- Generated icon bitmaps use the current theme text/muted color, current target renderer, and current target DPI. Runtime theme changes refresh owned icon bitmaps and invalidate an open popup without recreating public item models.
- No persistent `Pen`, `Brush`, `GraphicsPath`, or `Bitmap` may accumulate in the renderer. Scoped paint objects use `using`; generated menu images are tracked and released on refresh/rebuild/dispose.
- Do not add a per-dropdown `Timer`, animation scheduler, async worker, or transition. Popup animation behavior, if any, remains OS/WinForms-owned.
- Designer construction must work without application bootstrap, DI, service locators, or initialized global state beyond existing safe theme defaults.
- `Items` must use `DesignerSerializationVisibility.Content`; the parameterless item constructor creates a normal command so a WinForms collection editor can construct an item. A second constructor taking `BootstrapDropdownItemKind` is the minimal explicit path for creating separators while keeping `Kind` immutable.
- All public/protected members receive XML documentation. `TreatWarningsAsErrors` and `CS1591` remain green.
- The stage adds declared public API after the frozen v1 baseline. `Phase16PublicApiBaselineTests` must intentionally fail first, the exported surface must be reviewed against this plan, and only then may the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` be updated.
- Submenus, arbitrary hosted controls, split-button semantics, multi-select, radio groups, keyboard shortcut text/accelerators beyond native text handling, dynamic/live collection synchronization while open, custom popup placement policies, nested dropdown ownership, and custom popup animation are outside Stage 7.
- Every Stage 7 checkpoint must preserve both target frameworks. Final completion requires both builds, both test targets, demo/manual checks, docs, and API-baseline review.

---

## Platform Behavior Resolved During Planning

Stage 7 deliberately delegates command-menu mechanics to WinForms rather than reimplementing them.

Relevant native references:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.show?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.autoclose?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.closed?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdownmenu?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdownmenu.showimagemargin?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdownmenu.showcheckmargin?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstriprenderer?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripmenuitem?view=netframework-4.8.1>

Native behavior that is part of the Stage 7 contract:

- `ToolStripDropDownMenu` is a `ToolStripDropDown` specialization for command menus and supplies native image/check margin layout.
- `ToolStripDropDown.AutoClose` defaults to `true`; keep it true so activation/focus loss/outside-click behavior remains native.
- `Show(Control, Point)` anchors the popup relative to a control. Stage 7 calls it with the point immediately below the target and does not pre-clamp screen coordinates; WinForms owns working-area adjustment.
- `Opened`/`Closed` come from the native popup lifecycle. Stage 7 forwards them instead of manufacturing event timing around `Show()`/`Close()` calls.
- `ToolStripMenuItem.Enabled`, `Checked`, native item focus, selection, and `PerformClick` behavior remain native. Stage 7 sets snapshot values and handles command dispatch only.
- Up/Down/Home/End/Enter/Escape remain native ToolStrip key handling. Product code must not override or intercept those key messages.
- `ToolStripRenderer` is the supported theme hook for menu background, item background, text, image margin, check indication, separator, and border rendering; Stage 7 uses it rather than painting a separate popup Form.

When a behavior varies between .NET Framework 4.8 and .NET 8 WinForms, characterize the native controls on both targets and preserve their behavior. Do not copy runtime-internal keyboard/focus/placement algorithms into project code or tests.

---

## Stage 7 Public Contract

The roadmap's contract is retained. Planning resolves only the constructors/defaults needed to make the immutable `Kind` usable and designer-safe.

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapDropdownItemKind
{
    Item,
    Separator
}

public sealed class BootstrapDropdownItem
{
    public BootstrapDropdownItem();
    public BootstrapDropdownItem(BootstrapDropdownItemKind kind);

    public BootstrapDropdownItemKind Kind { get; }
    public string Text { get; set; }
    public IconDescriptor? Icon { get; set; }
    public bool Enabled { get; set; }
    public bool Checked { get; set; }
    public object? Tag { get; set; }
    public event EventHandler? Click;
}

public sealed class BootstrapDropdownItemCollection : Collection<BootstrapDropdownItem>
{
}

[DefaultEvent(nameof(Opened))]
public class BootstrapDropdown : Component
{
    public BootstrapDropdown();

    public BootstrapButton? Target { get; set; }
    public BootstrapDropdownItemCollection Items { get; }
    public BootstrapVariant Variant { get; set; }
    public int MinimumWidth { get; set; }
    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public void Show();
    public void Close();
}
```

### Defaults and validation

| Member | Default / rule |
| --- | --- |
| `BootstrapDropdownItem()` | `Kind = Item` |
| `BootstrapDropdownItem(BootstrapDropdownItemKind kind)` | accepts defined values only; undefined value throws `ArgumentOutOfRangeException` |
| `Text` | `string.Empty`; assigning `null` normalizes to `string.Empty` |
| `Icon` | `null` |
| `Enabled` | `true` |
| `Checked` | `false` |
| `Tag` | `null` |
| `Target` | `null` |
| `Items` | one stable empty collection instance |
| `Variant` | `Primary`; undefined value throws `ArgumentOutOfRangeException` |
| `MinimumWidth` | `0`; negative value throws `ArgumentOutOfRangeException` |

### Public behavior

- `Items` preserves insertion order.
- The collection rejects `null` on insert/replace with `ArgumentNullException`; this prevents a deferred null failure only when opening the popup.
- Separators ignore `Text`, `Icon`, `Enabled`, `Checked`, `Tag`, and `Click` for native activation purposes. Those properties remain ordinary model storage; they simply do not turn a separator into a command.
- Item model property changes do not raise framework property-change events. The model intentionally stays small; current values are snapshotted when `Show()` rebuilds native rows.
- `Target` replacement closes an open popup before detaching the old target. It then detaches old `Click`/`Disposed` handlers, assigns the new target, and attaches the same handlers to the new target.
- When the current target is disposed, Dropdown closes, detaches, and clears its internal reference. It never calls `Dispose()` on the target.
- Clicking the assigned target while the popup is closed calls the same `Show()` path used by callers. Clicking it while the popup is open calls the same `Close()` path.
- `Show()` throws only for the missing-anchor programmer error (`Target == null`). Disabled/loading/disposed targets and empty item lists are benign no-ops.
- `Show()` rebuilds native rows before opening, applies current `Variant`, target font/icon renderer/DPI, current theme, image/check margins, and scaled `MinimumWidth`, then calls the native popup anchored at `new Point(0, Target.Height)`.
- `Close()` calls native `Close()` only when the owned popup is visible.
- An enabled native command row maps back to its public model and calls one internal `RaiseClick()` method. `RaiseClick()` is `internal`, not public API, and exists so every activation route has one implementation.
- Dropdown does not manually raise `Closed` after command activation. Native `AutoClose` owns close timing; the native `Closed` event is the only source of the public `Closed` event.
- `Checked` does not change as a side effect of activation. This avoids inventing checkbox/radio policy absent from the roadmap.
- Public event sender for `Opened`, `Closed`, and item `Click` is the public framework object (`BootstrapDropdown` or `BootstrapDropdownItem`), never the private native ToolStrip object.

### Explicitly unsupported/new scope not added here

Do not add any of the following during Stage 7:

- `DropDownDirection`, `Placement`, `Alignment`, or monitor/screen public settings.
- `BorderRadius`, shadow, opacity, transparency, or popup animation settings.
- `AutoClose` public passthrough; Stage 7 intentionally keeps native `AutoClose = true`.
- `Show(Control)` or coordinate overloads that bypass the `Target` contract.
- Public access to the owned `ToolStripDropDownMenu`, renderer, or native items.
- Public `IconRenderer` on Dropdown; menu icons use the assigned target button's renderer.
- Submenu collections or child item trees.
- `CheckOnClick`, radio/check groups, or automatic `Checked` mutation.
- Shortcut-key registration/global hotkeys.
- Arbitrary `ToolStripItem`, `ToolStripControlHost`, textbox, combo, or custom-control hosting.
- Live `INotifyPropertyChanged`/`INotifyCollectionChanged` synchronization.
- Async item providers or lazy loading.
- A second target type such as plain `Button`, `Control`, `ToolStripButton`, or `BootstrapButtonGroup`.

---

## Internal Native Composition Contract

### Owned popup

`BootstrapDropdown` owns one `ToolStripDropDownMenu` for its entire lifetime:

```csharp
private readonly ToolStripDropDownMenu _dropDown;
private readonly BootstrapDropdownRenderer _renderer;
private readonly BootstrapDropdownItemCollection _items;
private readonly List<Image> _ownedImages = new List<Image>();
private BootstrapButton? _target;
private BootstrapVariant _variant = BootstrapVariant.Primary;
private int _minimumWidth;
private bool _themeSubscribed;
private bool _disposed;
```

Constructor initialization must establish these invariant native settings once:

```csharp
_dropDown = new ToolStripDropDownMenu
{
    AutoClose = true,
    AutoSize = true,
    ShowImageMargin = false,
    ShowCheckMargin = false,
    RenderMode = ToolStripRenderMode.ManagerRenderMode,
    Renderer = _renderer
};

_dropDown.Opened += OnNativeOpened;
_dropDown.Closed += OnNativeClosed;
_dropDown.ItemClicked += OnNativeItemClicked;
BootstrapThemeManager.ThemeChanged += OnThemeChanged;
_themeSubscribed = true;
```

Use the actual API combination that compiles on both targets. If assigning `Renderer` makes `RenderMode` update automatically on a target, do not fight the native property; the invariant is simply that `_renderer` is the active renderer.

### Snapshot rebuild

Every effective `Show()` performs this sequence before the native popup opens:

1. Clear and dispose previous owned native rows/images.
2. Inspect models to decide whether any row needs image/check margins.
3. Resolve current target DPI, target `Font`, target `IconRenderer`, current theme, and scaled metrics.
4. Create `ToolStripSeparator` for `Separator` models.
5. Create `ToolStripMenuItem` for `Item` models; copy `Text`, `Enabled`, `Checked`; set `CheckOnClick = false`; store the model in native `Tag` only for internal dispatch.
6. When `Icon != null`, create one current-DPI bitmap through the target's `IconRenderer`, assign it to native `Image`, set `ImageScaling = None`, and add that bitmap to `_ownedImages`.
7. Apply DPI-scaled row padding and separator margin without replacing native auto-size/text measurement.
8. Apply `ShowImageMargin`, `ShowCheckMargin`, popup `Font`, theme colors, renderer state, and scaled `MinimumWidth`.
9. Call `_dropDown.Show(target, new Point(0, target.Height))` and allow WinForms to constrain placement.

No model event subscriptions are required. Rebuild-from-model-on-open is the synchronization mechanism.

### Generated icon ownership

Use one helper that makes ownership unambiguous:

```csharp
private Image? CreateMenuImage(BootstrapDropdownItem model, int dpi, Color color)
{
    if (model.Icon is null || _target is null)
    {
        return null;
    }

    var logicalSize = BootstrapThemeManager.CurrentTheme.Metrics.SpacingLG;
    var size = Math.Max(1, DpiScaler.Scale(logicalSize, dpi));
    var bitmap = new Bitmap(size, size);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.Clear(Color.Transparent);
        if (!_target.IconRenderer.TryRender(
                graphics,
                model.Icon,
                new Rectangle(0, 0, size, size),
                color))
        {
            bitmap.Dispose();
            return null;
        }
    }

    _ownedImages.Add(bitmap);
    return bitmap;
}
```

Before disposing an owned image, first set the native item's `Image = null` so an open/invalidating ToolStrip never paints a disposed image reference.

### Clearing native rows

Use one path for rebuild and final disposal:

```csharp
private void ClearNativeItems()
{
    foreach (ToolStripItem nativeItem in _dropDown.Items)
    {
        nativeItem.Image = null;
    }

    foreach (var image in _ownedImages)
    {
        image.Dispose();
    }
    _ownedImages.Clear();

    while (_dropDown.Items.Count > 0)
    {
        var nativeItem = _dropDown.Items[0];
        _dropDown.Items.RemoveAt(0);
        nativeItem.Dispose();
    }
}
```

Do not depend on collection removal implicitly disposing a `ToolStripItem`; explicit ownership is clearer and is required by the roadmap lifecycle gate.

---

## Internal Renderer Contract

`BootstrapDropdownRenderer` is `internal sealed`; it is not part of the public API baseline.

```csharp
internal sealed class BootstrapDropdownRenderer : ToolStripRenderer
{
    public BootstrapVariant Variant { get; set; } = BootstrapVariant.Primary;

    internal static BootstrapDropdownPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled,
        bool selected);

    internal static BootstrapDropdownMetrics ResolveMetrics(
        BootstrapThemeMetrics metrics,
        int dpi);
}
```

Use small internal immutable structs in the same file rather than creating new public types:

```csharp
internal readonly struct BootstrapDropdownPalette
{
    public BootstrapDropdownPalette(
        Color background,
        Color foreground,
        Color border,
        Color accent)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        Accent = accent;
    }

    public Color Background { get; }
    public Color Foreground { get; }
    public Color Border { get; }
    public Color Accent { get; }
}

internal readonly struct BootstrapDropdownMetrics
{
    public BootstrapDropdownMetrics(
        int itemHorizontalPadding,
        int itemVerticalPadding,
        int imageSize,
        int separatorInset,
        float borderWidth)
    {
        ItemHorizontalPadding = itemHorizontalPadding;
        ItemVerticalPadding = itemVerticalPadding;
        ImageSize = imageSize;
        SeparatorInset = separatorInset;
        BorderWidth = borderWidth;
    }

    public int ItemHorizontalPadding { get; }
    public int ItemVerticalPadding { get; }
    public int ImageSize { get; }
    public int SeparatorInset { get; }
    public float BorderWidth { get; }
}
```

### Palette rules

```text
variantColor = BootstrapVariantColorResolver.Resolve(colors, variant)
background:
  selected => ColorUtil.Blend(variantColor, colors.Surface, 0.12f)
  otherwise => colors.Surface
foreground:
  enabled => colors.Text
  otherwise => colors.MutedText
border => colors.Border
accent:
  enabled => variantColor
  otherwise => colors.Disabled
```

- `colors == null` throws `ArgumentNullException`.
- Undefined `variant` throws through `BootstrapVariantColorResolver`.
- Selection uses the native `ToolStripItem.Selected` state; renderer does not create another hover/focus state field.
- Disabled rows remain readable and do not receive an active semantic foreground.
- Checked state uses `Accent` for the check glyph, but does not force a second background state. If the checked item is also selected, normal selected background applies.

### Metrics rules

At 96 DPI, use existing tokens:

```text
ItemHorizontalPadding = Metrics.SpacingSM
ItemVerticalPadding   = Metrics.SpacingXS
ImageSize             = Metrics.SpacingLG
SeparatorInset        = Metrics.SpacingSM
BorderWidth           = Metrics.BorderWidth
```

All values scale through `DpiScaler` using current target DPI. `dpi <= 0` throws `ArgumentOutOfRangeException`; `metrics == null` throws `ArgumentNullException`.

### Paint overrides

Override only supported ToolStrip rendering hooks:

- `OnRenderToolStripBackground` — fill current theme `Surface`.
- `OnRenderToolStripBorder` — draw one current-theme border using scaled `BorderWidth`.
- `OnRenderImageMargin` — fill with `Surface`; do not introduce a permanently different gutter color.
- `OnRenderMenuItemBackground` — fill using `ResolvePalette(..., e.Item.Enabled, e.Item.Selected).Background`.
- `OnRenderItemText` — replace `e.TextColor` with the resolved foreground, then call `base.OnRenderItemText(e)` so native text rectangle/mnemonic behavior stays intact.
- `OnRenderItemCheck` — draw a compact check mark using `Accent` inside the native check rectangle; use scoped `Pen`; do not use an external icon package for this structural glyph.
- `OnRenderSeparator` — draw one horizontal line using theme `Border`, inset by scaled `SeparatorInset`.
- `OnRenderItemImage` may call base because generated images are already rendered through `IIconRenderer` in the correct theme color and DPI.

Do not override input/focus/key processing. Do not create persistent GDI brushes/pens in renderer fields.

---

## Target / Popup Lifecycle State Machine

The implementation should be understandable as a small state machine rather than scattered booleans.

```text
No target
  Target = button --------------------> Ready / closed

Ready / closed
  target Click or Show() + can-open --> Native open
  Target replaced --------------------> Ready / closed (new target)
  Target disposed --------------------> No target
  Dispose ----------------------------> Disposed

Native open
  target Click / Close() -------------> Native closed
  command activation -----------------> native AutoClose -> Native closed
  outside click/focus loss/Escape ----> native AutoClose -> Native closed
  Target replaced/disposed -----------> Close first, then detach
  Dispose ----------------------------> Close + release owned resources

Disposed
  no further native subscriptions/resources
```

`CanOpen` means:

```csharp
private bool CanOpen(BootstrapButton target)
{
    return !target.IsDisposed && target.Enabled && !target.Loading && _items.Count > 0;
}
```

Do not add a public `IsOpen`; internal code may use `_dropDown.Visible` as native truth.

---

## File Map

### Create

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs` — public two-value command/separator enum with XML docs.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs` — public mutable command model, immutable `Kind`, defaults/validation, and internal single-path `RaiseClick()`.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemCollection.cs` — public collection with null rejection and XML docs; no live-binding/event infrastructure.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs` — public component, target ownership/wiring, native popup snapshot rebuild, open/close events, theme/DPI/icon refresh, disposal.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs` — internal ToolStrip renderer plus pure palette/metric helpers.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs` — pure + STA contract, ownership, rendering-helper, native-characterization, activation, lifecycle, DPI/theme tests.

### Modify after Stage 4 exists

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — add Dropdown scenarios beside Tabs; no new top-level demo window.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs` — assert Dropdown scenarios and component lifetime.
- `docs/COMPONENTS.md` — public API, ownership, snapshot semantics, native behavior, unsupported scope.
- `docs/TESTING.md` — Stage 7 automated/manual/theme/DPI/interaction/resource matrices.
- `docs/ARCHITECTURE.md` — document native command-popup composition and renderer boundary.
- `README.md` — add Dropdown to supported component summary/example list.
- `docs/PACKAGE_README.md` — package-facing Dropdown overview.
- `CHANGELOG.md` — record Stage 7 component addition.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — update approved fingerprint only after deliberate review.
- `docs/PUBLIC_API_BASELINE.md` — record reviewed Stage 7 declared additions and new fingerprint.

### Inspect only unless an earlier-stage defect requires correction

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButton.cs` — use existing `Click`, `Loading`, `IconRenderer`, theme/font/DPI behavior; Stage 7 should not change Button API.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapVariantColorResolver.cs` — reuse semantic resolution.
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/ColorUtil.cs` — reuse blending.
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/DpiScaler.cs` — reuse logical scaling.
- `src/MyDmsVn.Bootstrap5WinFormUI/Icons/IIconRenderer.cs` and `IconDescriptor.cs` — reuse source-neutral icon pipeline.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — Stage 4 should already have created exactly one Navigation entry; do not add a second route for Dropdown.

---

## Roadmap-to-Task Traceability

| Roadmap item | Detailed tasks in this plan |
| --- | --- |
| 7.1 ownership/collection | Tasks 1, 2, 4, 7 |
| 7.2 native-menu interaction | Tasks 1, 4, 5, 7 |
| 7.3 ToolStripDropDown composition + renderer | Tasks 3, 4, 5, 6 |
| 7.4 theme/DPI/working area | Tasks 3, 6, 7, 8 |
| 7.5 Navigation demo | Task 8 |
| 7.6 both targets/docs/API baseline | Tasks 9, 10 |

---

### Task 1: Characterize native ToolStripDropDownMenu semantics before adding framework behavior

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: native `ToolStripDropDownMenu`, `ToolStripMenuItem`, `ToolStripSeparator`, `Form`, `BootstrapButton`.
- Produces: executable assertions proving the native behaviors Stage 7 intends to delegate rather than reimplement.

- [ ] **Step 1: Create the STA fixture and native characterization helpers.**

Start the file with:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDropdownTests
{
    private static Form CreateHost(Control target)
    {
        var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            Size = new Size(500, 300)
        };
        target.Location = new Point(24, 24);
        form.Controls.Add(target);
        form.Show();
        Application.DoEvents();
        return form;
    }
}
```

No `Thread.Sleep`/`Task.Delay` is permitted. `Application.DoEvents()` is used only to process the synchronous WinForms show/close messages needed by a real popup.

- [ ] **Step 2: Characterize native ownership-neutral open/close events and AutoClose default.**

Add a test that creates a plain `ToolStripDropDownMenu`, asserts `AutoClose == true`, adds one native item, hosts a plain `Button`, calls `Show(button, new Point(0, button.Height))`, and verifies `Opened` fires once and `Visible` becomes true. Then call `Close()` and verify `Closed` fires once and `Visible` becomes false.

The test must dispose the menu and host form with `using`.

- [ ] **Step 3: Characterize native item state without framework mutation.**

Add a native test with one enabled checked `ToolStripMenuItem`, one disabled item, and one `ToolStripSeparator`. Assert `Checked`, `Enabled`, item order, separator type, and `CheckOnClick = false` remain as configured. Call `PerformClick()` on the checked item while `CheckOnClick == false` and assert its `Checked` value does not toggle automatically.

This establishes the exact policy Stage 7 will use for model `Checked`.

- [ ] **Step 4: Characterize native close idempotence.**

Open the native popup, call `Close()` twice, process messages, and assert only one `Closed` event was observed. This is the native basis for framework `Close()` idempotence.

- [ ] **Step 5: Run only the native characterization tests on the primary test target.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: the native-only tests pass before any `BootstrapDropdown` type exists.

- [ ] **Step 6: Commit the characterization checkpoint.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "test: characterize native dropdown menu semantics"
```

---

### Task 2: Implement the public item kind, command model, and collection contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemCollection.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: `IconDescriptor`, `Collection<T>`.
- Produces: exact public item API shown in this plan and `internal void RaiseClick()` for the later native dispatch path.

- [ ] **Step 1: Add failing enum/default/constructor tests.**

Assert:

```csharp
var defaultItem = new BootstrapDropdownItem();
var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);

Assert.Multiple((Action)(() =>
{
    Assert.That(defaultItem.Kind, Is.EqualTo(BootstrapDropdownItemKind.Item));
    Assert.That(defaultItem.Text, Is.EqualTo(string.Empty));
    Assert.That(defaultItem.Icon, Is.Null);
    Assert.That(defaultItem.Enabled, Is.True);
    Assert.That(defaultItem.Checked, Is.False);
    Assert.That(defaultItem.Tag, Is.Null);
    Assert.That(separator.Kind, Is.EqualTo(BootstrapDropdownItemKind.Separator));
}));

Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
    _ = new BootstrapDropdownItem((BootstrapDropdownItemKind)999)));
```

Also assign `Text = null!` and assert normalization to `string.Empty`.

- [ ] **Step 2: Add failing collection tests.**

Create a collection, add Item/Separator/Item, remove the middle entry, replace index 0, clear the collection, and assert order/count after each operation. Add explicit assertions that `Add(null!)` and index replacement with `null!` throw `ArgumentNullException`.

- [ ] **Step 3: Add a failing single-path Click test.**

Subscribe once to `BootstrapDropdownItem.Click`, call the planned internal `RaiseClick()` twice, and assert count is exactly two and `sender` is the item. This method is internal production behavior, not a public convenience method.

- [ ] **Step 4: Run tests and verify they fail because the public Stage 7 model types do not yet exist.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: compile/test failure references missing `BootstrapDropdownItemKind`, `BootstrapDropdownItem`, and `BootstrapDropdownItemCollection`.

- [ ] **Step 5: Implement `BootstrapDropdownItemKind`.**

Use exactly two values and XML-document both. Do not add aliases such as `Command`, `Divider`, or `Header`.

- [ ] **Step 6: Implement `BootstrapDropdownItem`.**

Use a normalized backing field for `Text`, default `Enabled = true`, immutable validated `Kind`, and one internal raiser:

```csharp
public BootstrapDropdownItem()
    : this(BootstrapDropdownItemKind.Item)
{
}

public BootstrapDropdownItem(BootstrapDropdownItemKind kind)
{
    if (!Enum.IsDefined(typeof(BootstrapDropdownItemKind), kind))
    {
        throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported dropdown item kind.");
    }

    Kind = kind;
    _text = string.Empty;
    Enabled = true;
}

internal void RaiseClick()
{
    Click?.Invoke(this, EventArgs.Empty);
}
```

Add `[DefaultValue]`, `[Category]`, and XML docs where useful. Do not add property-changed events.

- [ ] **Step 7: Implement null-safe collection overrides.**

Override only `InsertItem` and `SetItem`; call `base` after validating `item != null`. `RemoveItem`/`ClearItems` stay native `Collection<T>` behavior because there is no owner callback/live binding.

- [ ] **Step 8: Run model/collection tests until green and commit.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemCollection.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: add dropdown command item model"
```

---

### Task 3: Freeze deterministic Dropdown renderer palette and DPI metrics

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeColors`, `BootstrapThemeMetrics`, `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`.
- Produces: `internal BootstrapDropdownRenderer`, `BootstrapDropdownPalette`, `BootstrapDropdownMetrics`, `ResolvePalette`, and `ResolveMetrics` used by the popup implementation.

- [ ] **Step 1: Add failing palette tests for all semantic variants in Light and Dark.**

For each defined `BootstrapVariant`, resolve current `variantColor` through `BootstrapVariantColorResolver` and assert:

```csharp
var palette = BootstrapDropdownRenderer.ResolvePalette(
    colors,
    variant,
    enabled: true,
    selected: false);

Assert.That(palette.Background, Is.EqualTo(colors.Surface));
Assert.That(palette.Foreground, Is.EqualTo(colors.Text));
Assert.That(palette.Border, Is.EqualTo(colors.Border));
Assert.That(palette.Accent, Is.EqualTo(variantColor));
```

For `selected: true`, assert background equals `ColorUtil.Blend(variantColor, colors.Surface, 0.12f)` and foreground remains theme text. For `enabled: false`, assert foreground is `MutedText` and accent is `Disabled`.

- [ ] **Step 2: Add failing validation tests.**

Assert `ResolvePalette(null!, ...)` throws `ArgumentNullException` and undefined `BootstrapVariant` throws `ArgumentOutOfRangeException`.

- [ ] **Step 3: Add failing DPI metric tests for 96/120/144/168/192.**

For each DPI, compare every metric with `DpiScaler.Scale` from `BootstrapThemeMetrics.Default`:

```csharp
var actual = BootstrapDropdownRenderer.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);
Assert.That(actual.ItemHorizontalPadding,
    Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingSM, dpi)));
Assert.That(actual.ItemVerticalPadding,
    Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingXS, dpi)));
Assert.That(actual.ImageSize,
    Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingLG, dpi)));
```

Also assert separator inset and positive scaled border width. `null` metrics and non-positive DPI must throw.

- [ ] **Step 4: Run the tests and verify the renderer contract is red.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: compile/test failure references missing renderer/helper types.

- [ ] **Step 5: Implement pure helpers and the renderer shell.**

Create the two internal readonly structs and the exact static helper signatures from the Internal Renderer Contract. Derive `BootstrapDropdownRenderer` from `ToolStripRenderer`, default `Variant` to Primary, validate setter values, and keep it resource-free at rest.

- [ ] **Step 6: Implement supported paint overrides.**

Use current theme inside each paint call, scoped `SolidBrush`/`Pen`, native rectangles supplied by ToolStrip, and base text/image behavior. Do not cache a theme instance because runtime switching must affect an already-open menu.

- [ ] **Step 7: Run tests until green and commit.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: define BootstrapDropdown renderer"
```

---

### Task 4: Implement BootstrapDropdown target ownership, public contract, and native open/close lifecycle

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: `BootstrapButton`, `BootstrapDropdownItemCollection`, `BootstrapDropdownRenderer`, `BootstrapThemeManager`, native `ToolStripDropDownMenu`.
- Produces: exact public `BootstrapDropdown` contract plus private target/native lifecycle helpers.

- [ ] **Step 1: Add failing default/public-contract tests.**

Assert:

```csharp
using var dropdown = new BootstrapDropdown();
Assert.Multiple((Action)(() =>
{
    Assert.That(dropdown.Target, Is.Null);
    Assert.That(dropdown.Items, Is.Not.Null);
    Assert.That(dropdown.Items, Is.SameAs(dropdown.Items));
    Assert.That(dropdown.Items, Is.Empty);
    Assert.That(dropdown.Variant, Is.EqualTo(BootstrapVariant.Primary));
    Assert.That(dropdown.MinimumWidth, Is.EqualTo(0));
}));
```

Assert negative `MinimumWidth` and undefined `Variant` throw. Assert `Show()` with `Target == null` throws `InvalidOperationException`; `Close()` on a new component does not throw.

- [ ] **Step 2: Add failing target ownership tests.**

Use two `BootstrapButton` instances. Set the first target, replace with the second, dispose the Dropdown, and assert neither button is disposed. Dispose a current target and assert Dropdown can later accept a different target without invoking stale handlers.

The test should hold a `WeakReference` only if needed to verify event retention; do not make GC timing the primary correctness assertion.

- [ ] **Step 3: Add failing disabled/loading/empty no-op tests.**

Host the target in a real form. For each state (`Enabled=false`, `Loading=true`, empty `Items`), subscribe to `Opened`, invoke target `PerformClick()` and/or `Show()`, process messages, and assert no `Opened` event.

Restore the state before moving to the next case so failures are isolated.

- [ ] **Step 4: Implement constructor, defaults, attributes, and validation.**

Add `[DefaultEvent(nameof(Opened))]`, `[Category]`, `[DefaultValue]`, `[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]` on `Items`, XML docs, one stable collection instance, one owned native menu, one renderer, and one theme subscription.

- [ ] **Step 5: Implement `Target` attach/detach and disposal handling.**

Use private helpers:

```csharp
private void AttachTarget(BootstrapButton target)
{
    target.Click += OnTargetClick;
    target.Disposed += OnTargetDisposed;
}

private void DetachTarget(BootstrapButton target)
{
    target.Click -= OnTargetClick;
    target.Disposed -= OnTargetDisposed;
}
```

Replacement closes first if open, then detaches old/attaches new. `OnTargetDisposed` closes and clears the reference without disposing the sender.

- [ ] **Step 6: Implement `Show()`/`Close()` and native event forwarding.**

At this task, `Show()` may initially build text-only native rows through a small private `RebuildNativeItems()`; icons/full renderer refresh land in Tasks 5-6. Use `_dropDown.Visible` as open truth and call:

```csharp
_dropDown.Show(target, new Point(0, target.Height));
```

Forward native lifecycle only:

```csharp
private void OnNativeOpened(object? sender, EventArgs e)
{
    Opened?.Invoke(this, EventArgs.Empty);
}

private void OnNativeClosed(object? sender, ToolStripDropDownClosedEventArgs e)
{
    Closed?.Invoke(this, EventArgs.Empty);
}
```

Do not raise these events directly from `Show()`/`Close()`.

- [ ] **Step 7: Run target/open-close tests until green.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

- [ ] **Step 8: Commit the native component/lifecycle checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: add BootstrapDropdown native lifecycle"
```

---

### Task 5: Build native command snapshots and dispatch item activation exactly once

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: public model collection and native `ToolStripMenuItem`/`ToolStripSeparator`.
- Produces: deterministic snapshot rebuild, `internal ActivateItem(...)` command path, and native `ItemClicked` mapping.

- [ ] **Step 1: Add failing snapshot tests through internal behavior.**

Because tests already have `InternalsVisibleTo`, keep useful logic internal rather than exposing native controls publicly. Add an internal pure/native-independent helper if needed:

```csharp
internal static bool CanActivate(BootstrapDropdownItem item)
{
    return item.Kind == BootstrapDropdownItemKind.Item && item.Enabled;
}
```

Assert Item+Enabled is true; disabled item and separator are false.

- [ ] **Step 2: Add failing activation tests.**

Subscribe to a model's `Click`, call the internal `ActivateItem(model)`, and assert one event. Call it for disabled/separator models and assert zero events. Assert `Checked` never mutates through activation.

`ActivateItem` is justified as the single production command-dispatch path used by the native `ItemClicked` handler; it is not a test hook with a separate behavior.

- [ ] **Step 3: Add a failing rebuild/reopen scenario using public behavior.**

Create Item A + separator + Item B, show/close, then remove A, change B text/checked/enabled, add Item C, show/close again. Assert both openings succeed with one `Opened`/`Closed` pair each and no exception/disposed-image access. This proves mutation applies at the next opening without live binding.

- [ ] **Step 4: Implement native row creation.**

For command rows:

```csharp
var menuItem = new ToolStripMenuItem(model.Text)
{
    Enabled = model.Enabled,
    Checked = model.Checked,
    CheckOnClick = false,
    Tag = model,
    AutoSize = true
};
```

For separators, create `new ToolStripSeparator()` and do not attach command handlers.

Set `ShowCheckMargin` from `Items.Any(item => item.Kind == Item && item.Checked)` and `ShowImageMargin` from any command with `Icon != null`.

- [ ] **Step 5: Route native `ItemClicked` to one activation method.**

```csharp
private void OnNativeItemClicked(object? sender, ToolStripItemClickedEventArgs e)
{
    if (e.ClickedItem?.Tag is BootstrapDropdownItem model)
    {
        ActivateItem(model);
    }
}

internal void ActivateItem(BootstrapDropdownItem item)
{
    if (item is null)
    {
        throw new ArgumentNullException(nameof(item));
    }

    if (CanActivate(item))
    {
        item.RaiseClick();
    }
}
```

Do not call `Close()` from this handler; native `AutoClose=true` owns closure and avoids duplicate close paths/events.

- [ ] **Step 6: Implement explicit native-item cleanup before every rebuild.**

Use the `ClearNativeItems()` ownership pattern from this plan. At this checkpoint there may be no generated images yet, but the cleanup path must already dispose removed native rows.

- [ ] **Step 7: Run the control tests three times to catch handler multiplication.**

```powershell
1..3 | ForEach-Object {
    dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
}
```

Expected: all runs pass with no duplicate item event counts and no disposed native item access.

- [ ] **Step 8: Commit the command-snapshot checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: map dropdown commands to native menu items"
```

---

### Task 6: Complete renderer integration, icon generation, runtime theme refresh, and DPI sizing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: target `IconRenderer`, `IconDescriptor`, current theme/metrics, target `DeviceDpi`.
- Produces: theme-aware native popup presentation with deterministic generated-image lifetime.

- [ ] **Step 1: Add failing icon/margin tests with a recording renderer.**

Add a private test renderer:

```csharp
private sealed class RecordingIconRenderer : IIconRenderer
{
    public int RenderCount { get; private set; }
    public Rectangle LastBounds { get; private set; }
    public Color LastColor { get; private set; }

    public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
    {
        RenderCount++;
        LastBounds = bounds;
        LastColor = color;
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bounds);
        return true;
    }
}
```

Assign it to the target button, add an item with `IconDescriptor.Framework(FrameworkIconGlyph.Plus)`, open/close the dropdown, and assert rendering occurred with positive square bounds. Add a no-icon opening and assert no render attempt.

- [ ] **Step 2: Add failing minimum-width validation/scaling tests.**

Keep pure scaling in an internal helper:

```csharp
internal static int ResolveMinimumWidth(int logicalMinimumWidth, int dpi)
```

Assert `0` returns `0`, 160 at 96 DPI returns 160, 160 at 144 DPI equals `DpiScaler.Scale(160, 144)`, negative logical width and non-positive DPI throw.

- [ ] **Step 3: Add failing theme-refresh tests.**

Open a dropdown with an icon, switch Light -> Dark -> Light using the repository's existing theme-manager test pattern, process messages, and assert no exception. The recording renderer should be called again when owned images are refreshed for an open native popup. After disposal, further theme changes must not invoke Dropdown work or throw.

- [ ] **Step 4: Implement current-DPI popup preparation.**

Before each open:

```csharp
var dpi = _target.DeviceDpi > 0 ? _target.DeviceDpi : DpiScaler.DefaultDpi;
var theme = BootstrapThemeManager.CurrentTheme;
var metrics = BootstrapDropdownRenderer.ResolveMetrics(theme.Metrics, dpi);
_renderer.Variant = _variant;
_dropDown.Font = _target.Font;
_dropDown.BackColor = theme.Colors.Surface;
_dropDown.ForeColor = theme.Colors.Text;
_dropDown.MinimumSize = new Size(ResolveMinimumWidth(_minimumWidth, dpi), 0);
```

Apply scaled row padding to command items and scaled separator margins. Keep native text measurement/AutoSize authoritative.

- [ ] **Step 5: Implement generated menu icons with explicit ownership.**

Use `CreateMenuImage` from this plan. Use `theme.Colors.Text` for enabled command icons and `theme.Colors.MutedText` for disabled command icons. Set `ImageScaling = ToolStripItemImageScaling.None`.

If the target renderer returns `false`, leave the image null; do not throw merely because an optional icon source is unsupported.

- [ ] **Step 6: Implement theme-change refresh without recreating public models.**

`OnThemeChanged` should:

1. return immediately if `_disposed`;
2. update renderer variant/current popup colors;
3. when the popup is visible and a valid target exists, replace owned icon bitmaps from current model/native rows using current theme color and DPI;
4. invalidate the popup.

Do not call `Show()` recursively from `ThemeChanged` and do not move the popup.

- [ ] **Step 7: Add renderer smoke tests without pixel snapshots.**

Paint representative background/item/separator/check cases to a bitmap using the renderer where practical and assert no exception/GDI object retention. Do not assert exact anti-aliased pixels; pure palette/metric tests already own deterministic visual policy.

- [ ] **Step 8: Run renderer/theme/DPI tests until green and commit.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
```

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: theme BootstrapDropdown menu presentation"
```

---

### Task 7: Prove native interaction, target replacement, working-area behavior, and disposal safety

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Modify only if a defect is found: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`

**Interfaces:**
- Consumes: completed Stage 7 component.
- Produces: STA/lifecycle confidence and explicit boundaries between stable automation and OS-owned manual keyboard/placement verification.

- [ ] **Step 1: Add Opened/Closed event-count tests through both public activation paths.**

Host a target with one item. Call `dropdown.Show()`, process messages, `dropdown.Close()`, process messages, then open with `target.PerformClick()` and close with `target.PerformClick()`. Assert two `Opened` and two `Closed` events total; sender is always Dropdown.

- [ ] **Step 2: Add target replacement while open.**

Open from Target A, set `Target = Target B`, process messages, and assert one close transition. Verify `PerformClick()` on A no longer opens anything and `PerformClick()` on B does. Dispose A afterward and verify Dropdown remains usable.

- [ ] **Step 3: Add target disposal while open.**

Open from a target, dispose the target, process messages, assert one close transition, then assign a new target and reopen successfully. The test verifies caller ownership and stale-handler cleanup.

- [ ] **Step 4: Add repeated rebuild/dispose stress without sleeps.**

Loop 50 times in one STA test:

```csharp
for (var i = 0; i < 50; i++)
{
    dropdown.Items.Clear();
    dropdown.Items.Add(new BootstrapDropdownItem
    {
        Text = "Action " + i,
        Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
    });
    dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
    dropdown.Items.Add(new BootstrapDropdownItem { Text = "Disabled", Enabled = false });
    dropdown.Show();
    Application.DoEvents();
    dropdown.Close();
    Application.DoEvents();
}
```

Assert event counts equal 50/50 and the recording icon renderer count does not indicate duplicate per-row handlers. Dispose Dropdown, then switch theme once to ensure no retained subscription accesses disposed resources.

- [ ] **Step 5: Add a working-area smoke test, not a copied placement algorithm.**

Place a host form close to the current screen's working-area lower/right edge, open the menu, process messages, and assert the native popup's public `Bounds` only if it can be observed without exposing private production state. If observing private native bounds would require a product test hook/reflection, keep this as a manual assertion instead; do not add public/internal native-popup access solely for the test.

The automated requirement is that `Show()` succeeds at the edge without custom coordinate exceptions. Exact clamping belongs to WinForms.

- [ ] **Step 6: Document native keyboard characterization in the test file/manual matrix.**

Automated tests must prove Stage 7 does not install key handlers or mutate item state. Full Up/Down/Home/End/Enter/Escape behavior is verified in the Navigation demo on a real desktop because synthetic `SendKeys` is not reliable in headless Windows test runners.

Do not add global hooks, `SendKeys`-dependent release gates, or arbitrary sleeps merely to claim keyboard automation.

- [ ] **Step 7: Run the Stage 7 suite three times.**

```powershell
1..3 | ForEach-Object {
    dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapDropdownTests
}
```

Expected: all runs pass; no duplicate events, ObjectDisposedException, disposed-image painting, or stale-target activation.

- [ ] **Step 8: Commit lifecycle hardening.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
git commit -m "test: harden BootstrapDropdown lifecycle"
```

---

### Task 8: Extend the shared Navigation demo with Dropdown scenarios

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`
- Inspect only unless needed: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:**
- Consumes: completed `BootstrapDropdown`, Stage 4 Navigation demo infrastructure.
- Produces: manual verification surface for roadmap 7.4/7.5 without a second top-level window.

- [ ] **Step 1: Verify Stage 4's shared Navigation files exist before editing.**

If either Navigation file is absent, stop Stage 7 execution and complete Stage 4 first. Do not create a Dropdown-only route as a workaround.

- [ ] **Step 2: Add failing demo tests.**

Require the Navigation page to contain at least these Dropdown targets/scenarios with stable accessible names or test-visible labels:

1. `Basic dropdown` — three enabled text actions.
2. `Icons` — framework `Plus`, `Check`, and `Close` icon descriptors.
3. `States` — checked item, disabled item, separator, normal item.
4. `Long menu` — one long caption to verify native auto-size/minimum width.
5. `Stress` — a target that can be opened/closed repeatedly while theme switching.

Also assert the integrated MainForm still has exactly one Navigation entry, not a separate Dropdown page.

- [ ] **Step 3: Run demo tests and verify the new assertions fail.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~NavigationDemoFormTests
```

- [ ] **Step 4: Add the Dropdown section to `NavigationDemoForm`.**

Keep each `BootstrapDropdown` as a form-owned component field so it remains alive as long as its target. Dispose them from the form's normal component/form disposal path; do not create a new Dropdown instance inside every button click.

Use examples such as:

```csharp
var action = new BootstrapDropdownItem
{
    Text = "Create item",
    Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
};

var checkedItem = new BootstrapDropdownItem
{
    Text = "Pinned",
    Checked = true
};

var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
```

Attach visible status text to item `Click`, Dropdown `Opened`, and `Closed` so manual event-count behavior is observable.

- [ ] **Step 5: Add explicit manual scenarios to the demo page copy.**

The page must tell the tester to:

- open by mouse and keyboard target activation;
- navigate rows with Up/Down/Home/End;
- activate with Enter;
- close with Escape and outside click;
- confirm disabled/separator rows do not activate;
- confirm checked row stays checked until application code changes it;
- switch Light/Dark while the popup is open;
- open near bottom/right screen edges;
- repeat on 100/125/150/175/200% DPI and a secondary monitor when available;
- repeatedly open/close to inspect focus restoration and stale artifacts.

- [ ] **Step 6: Run Navigation + integrated demo tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~NavigationDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests"
```

Expected: PASS and still exactly one Navigation route.

- [ ] **Step 7: Run the demo manually in both frameworks/configurations available to the repository.**

At minimum launch the integrated demo in Release under the normal development runtime and execute the manual matrix above. Record any OS/runtime-specific difference in `docs/TESTING.md`; do not compensate with custom focus/placement logic unless it violates the roadmap contract.

- [ ] **Step 8: Commit demo coverage.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs
git commit -m "demo: showcase BootstrapDropdown"
```

---

### Task 9: Update component, architecture, testing, package, README, and changelog documentation

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: final implemented behavior and manual observations.
- Produces: documentation matching the actual Stage 7 contract; no aspirational submenu/custom-popup claims.

- [ ] **Step 1: Update `docs/COMPONENTS.md`.**

Document:

- public contract and defaults;
- `BootstrapDropdownItemKind.Item` / `Separator`;
- immutable `Kind`, mutable text/icon/enabled/checked/tag;
- snapshot-at-`Show()` semantics;
- target and item ownership;
- native AutoClose/focus/keyboard/placement delegation;
- `Checked` does not auto-toggle;
- `Variant` and `MinimumWidth` behavior;
- target `IconRenderer` reuse;
- unsupported submenus/split buttons/custom hosts/live synchronization/custom popup Form.

- [ ] **Step 2: Update `docs/ARCHITECTURE.md`.**

Add the command-popup native-first pattern:

```text
BootstrapDropdown (public component/model)
    -> owned ToolStripDropDownMenu + ToolStripItems (native behavior)
    -> BootstrapDropdownRenderer (framework presentation)
    -> Theme / Rendering / Icons / DPI
    -> caller-owned BootstrapButton target
```

State explicitly that future popup controls must not automatically reuse Dropdown unless they are command menus; ComboBox and DatePicker remain separate native semantic controls as the roadmap requires.

- [ ] **Step 3: Update `docs/TESTING.md`.**

Add Stage 7 sections for:

- pure palette/metric/validation tests;
- STA open/close/ownership/target replacement/disposal tests;
- snapshot rebuild/add/remove/clear tests;
- icon renderer/theme refresh and image disposal;
- disabled/loading target behavior;
- checked/separator/disabled activation policy;
- native keyboard manual matrix;
- working-area/multi-monitor manual matrix;
- 96/120/144/168/192 logical DPI tests plus 100/125/150/175/200% real-Windows checks;
- repeated open/close/theme-switch resource stress.

- [ ] **Step 4: Update README/package README/CHANGELOG.**

Keep wording consistent: "Bootstrap-inspired command dropdown backed by native ToolStripDropDown behavior." Do not claim HTML Bootstrap parity, arbitrary content, nested submenus, or custom rounded popup chrome.

- [ ] **Step 5: Review docs against public source before committing.**

Search for each public member name and ensure defaults match code. Search for `submenu`, `split`, `AutoClose`, `BorderRadius`, and `animation` so unsupported behavior is not accidentally described as supported.

- [ ] **Step 6: Commit documentation.**

```powershell
git add docs/COMPONENTS.md docs/TESTING.md docs/ARCHITECTURE.md README.md docs/PACKAGE_README.md CHANGELOG.md
git commit -m "docs: document BootstrapDropdown"
```

---

### Task 10: Review the frozen public API, run both targets, and close Stage 7

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Verify: all Stage 7 product/test/demo/doc files.

**Interfaces:**
- Consumes: complete implementation.
- Produces: deliberately approved Stage 7 API fingerprint and green release-quality verification.

- [ ] **Step 1: Build both product targets before changing the baseline.**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: both PASS with zero warnings/errors under the repository's warnings-as-errors policy.

- [ ] **Step 2: Run focused Stage 7 tests on both targets.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDropdownTests|FullyQualifiedName~NavigationDemoFormTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDropdownTests|FullyQualifiedName~NavigationDemoFormTests"
```

Expected: PASS on both TFMs.

- [ ] **Step 3: Run the API baseline test before updating it and inspect the intentional failure.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests.Export
```

Expected: `ExportedApiMatchesApprovedV1Baseline` fails and prints the actual fingerprint plus reconstructed exported API because Stage 7 adds public types.

Review the output. The Stage 7 declared additions must match this plan:

```text
BootstrapDropdownItemKind enum with Item and Separator
BootstrapDropdownItem class with only the approved constructors/properties/Click event
BootstrapDropdownItemCollection class derived from Collection<BootstrapDropdownItem>
BootstrapDropdown class derived from Component with Target, Items, Variant, MinimumWidth, Opened, Closed, Show(), Close()
```

The internal renderer/helper structs/methods must not appear. No accidental public native popup/renderer/image collection/test hook may appear.

- [ ] **Step 4: Update the approved fingerprint only after the surface review passes.**

Copy the actual reviewed fingerprint printed by the failing test into `ApprovedV1Fingerprint`. Update `docs/PUBLIC_API_BASELINE.md` with the Stage 7 additions, date, rationale, and new fingerprint.

Do not change the baseline to make an unexplained extra member green. Remove the accidental API instead.

- [ ] **Step 5: Re-run the API baseline test.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

Expected: both API fingerprint and assembly-version tests PASS.

- [ ] **Step 6: Run the complete test project for both TFMs.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: all tests PASS. Fix regressions before proceeding.

- [ ] **Step 7: Run final manual Dropdown matrix.**

Verify on the integrated Navigation page:

```text
Themes: Light at creation, Dark at creation, Light -> Dark, Dark -> Light
Targets: enabled, disabled, loading, target replacement, target disposal
Rows: text-only, icon, checked, disabled, separator, long text, empty collection no-op
Input: mouse, target Enter/Space, Up/Down/Home/End, item Enter, Escape, outside click
Lifecycle: repeated open/close, item Click mutating Checked for next open, component disposal
Placement: normal, near bottom edge, near right edge, secondary monitor when available
DPI: 100%, 125%, 150%, 175%, 200%
Resources: no stale popup, no duplicate event, no disposed-image/GDI exception
```

- [ ] **Step 8: Run final repository hygiene checks.**

```powershell
git diff --check
git status --short
```

Confirm no `bin/`, `obj/`, packages, screenshots, or IDE-local files are staged.

- [ ] **Step 9: Commit the reviewed API baseline and final Stage 7 closure.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md
git commit -m "chore: approve BootstrapDropdown public API"
```

If final verification required implementation fixes after Task 9, commit those focused fixes before this baseline commit rather than hiding unrelated changes in the API-baseline commit.

---

## Acceptance Checklist

Stage 7 is complete only when every item below is true:

- [ ] `BootstrapDropdownItemKind` has exactly `Item` and `Separator`.
- [ ] `BootstrapDropdownItem` has immutable validated `Kind`, null-normalized text, optional icon/tag, enabled/checked state, and one `Click` event.
- [ ] Parameterless item construction produces a normal command; kind constructor can create a separator.
- [ ] `BootstrapDropdownItemCollection` preserves order and rejects null entries without adding live-binding infrastructure.
- [ ] `BootstrapDropdown` owns one native `ToolStripDropDownMenu` and one internal renderer.
- [ ] Target is caller-owned and is never disposed by Dropdown.
- [ ] Target replacement/disposal detaches handlers and closes any open popup safely.
- [ ] Target click toggles the popup only when enabled and not loading.
- [ ] `Show()` uses current item snapshot; empty/disabled/loading/disposed paths are no-ops and missing Target is an explicit error.
- [ ] `Opened`/`Closed` forward real native transitions exactly once.
- [ ] Enabled item activation raises the model `Click` once and native AutoClose handles closure.
- [ ] Disabled rows and separators never activate.
- [ ] `Checked` never auto-toggles in framework code.
- [ ] Native Up/Down/Home/End/Enter/Escape behavior is not reimplemented or intercepted.
- [ ] Native outside-click/focus-loss dismissal and working-area placement remain authoritative.
- [ ] `Variant` uses shared semantic color resolution; no hard-coded semantic palette table is introduced.
- [ ] `MinimumWidth` is validated, logical-DPI-scaled, and does not force target-width policy.
- [ ] Native image/check margins appear only when needed.
- [ ] Menu icons render through the assigned target's `IIconRenderer` and current DPI/theme.
- [ ] Generated bitmap/native-item resources are disposed on refresh/rebuild/disposal.
- [ ] Renderer keeps no persistent GDI paint resources.
- [ ] Runtime theme switching invalidates/refreshes an open popup and disposed Dropdown no longer receives theme callbacks.
- [ ] Navigation demo contains basic/icon/state/long/stress Dropdown scenarios without adding a second top-level Navigation route.
- [ ] Real-desktop manual checks cover keyboard, outside-click, screen edges, multi-monitor when available, Light/Dark, and 100-200% DPI.
- [ ] `docs/COMPONENTS.md`, `docs/TESTING.md`, `docs/ARCHITECTURE.md`, `README.md`, `docs/PACKAGE_README.md`, and `CHANGELOG.md` match actual behavior.
- [ ] Public API baseline was observed failing first, reviewed, then intentionally updated.
- [ ] Product builds pass for `net48` and `net8.0-windows`.
- [ ] Full test project passes for `net48` and `net8.0-windows`.
- [ ] `git diff --check` is clean and repository hygiene is preserved.

---

## Out of Scope / Follow-up Candidates

These are intentionally excluded from Stage 7 and require separate roadmap/API decisions if requested later:

- Nested submenus.
- Split-button/dropdown-button composite semantics.
- Radio/check groups and automatic check policy.
- Arbitrary hosted controls/menu editors.
- Custom shortcut registration or global accelerators.
- Live item collection/property synchronization while the menu is open.
- Custom placement/direction/alignment API.
- Rounded/layered popup window, custom shadow, opacity, or animation.
- Async/lazy command providers.
- Multi-select command menus.
- A generic target type broader than `BootstrapButton`.
- Public access to native ToolStrip objects or custom renderer injection.

Keeping these out preserves the roadmap's native-first command-menu scope and leaves Stage 7 independently shippable.
