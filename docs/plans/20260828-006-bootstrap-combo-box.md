# BootstrapComboBox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 6 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired `BootstrapComboBox` that preserves native WinForms item storage, data binding, selection, editable/drop-down behavior, autocomplete, keyboard navigation, and dropdown lifecycle while the framework owns item presentation, validation/focus presentation, optional leading-icon rendering, theme/font/DPI integration, lifecycle handling, demo coverage, documentation, and public API review.

**Architecture:** `BootstrapComboBox : ComboBox` remains a real native WinForms `ComboBox`; there is no wrapper data model, no embedded `BootstrapDropdown`, and no replacement popup. The framework fixes the native control to an owner-draw mode for item/selected-value presentation, reuses existing Theme / Rendering / Icons infrastructure, and applies only a conservative shell overlay that does not replace the native edit child, drop-down button, popup window, accessibility tree, binding engine, or keyboard state machine. Native inherited members such as `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, `DropDownStyle`, `AutoCompleteMode`, `AutoCompleteSource`, and selection/dropdown events remain canonical.

**Tech Stack:** C#, native Windows Forms `ComboBox`, existing Theme / Rendering / Icons / Compatibility infrastructure, `BootstrapValidationState`, `BootstrapThemeManager`, `DpiScaler`, `RoundedPath`, `ColorUtil`, `IconDescriptor`, `IIconRenderer`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 6 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; the public control remains under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Stage 5 (`BootstrapNumericBox`) must be complete and green before Stage 6 implementation begins, preserving roadmap order. `BootstrapComboBox` itself must not depend on NumericBox implementation details.
- `BootstrapComboBox` must subclass native `ComboBox`. Do not replace it with a `UserControl` composition, `TextBox + Button + ListBox`, `BootstrapDropdown`, `ToolStripDropDown`, or a custom top-level popup.
- Native WinForms remains the sole authority for item storage, object data binding, `CurrencyManager`/`BindingContext` interaction, display/value-member resolution, selected state, editable text semantics, autocomplete, keyboard navigation, drop-down open/close behavior, and selection events.
- Do not mirror `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, `Text`, `DropDownStyle`, `AutoCompleteMode`, `AutoCompleteSource`, or `AutoCompleteCustomSource` into framework backing fields.
- The framework owns the item owner-draw path. Stage 6 uses one fixed-height owner-draw implementation and does not expose a custom item-template API.
- Callers continue to provide objects/text/value through normal `ComboBox` APIs. The framework obtains display text through the native formatting/display pipeline rather than casting items to project-specific types.
- `BootstrapValidationState` is reused. Do not add a ComboBox-specific validation enum.
- Validation/focus priority follows the established TextBox/NumericBox rule: disabled presentation wins; then valid/invalid semantic border; then focused border; then neutral border.
- `BorderRadius = -1` means current theme radius. Values below `-1` throw `ArgumentOutOfRangeException`.
- `LeadingIcon` is decorative presentation only. It does not become part of item identity, selection, `DisplayMember`, accessible value, or binding state.
- `IconRenderer` defaults to `BootstrapIconRenderer.CreateDefault()`, rejects `null`, is not designer-serialized, and must not introduce a FontAwesome dependency into the core package.
- Theme metrics and colors come from `BootstrapThemeManager.CurrentTheme`; do not hard-code repeated spacing, border width, focus width, control height, radius, or semantic colors when theme tokens exist.
- Reuse `DpiScaler`, `RoundedPath`, `ColorUtil`, `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`, and existing icon abstractions. Do not add another theme manager, DPI helper, geometry library, contrast helper, or icon model.
- The native drop-down button, native editable text child, and OS-owned popup chrome remain native. Do not use child-window replacement, reflection into WinForms internals, global hooks, window-region shaping, owner-created popup forms, or unsupported Win32 painting hacks to force pixel-perfect Bootstrap chrome.
- A conservative post-native-paint border overlay is permitted only inside the ComboBox client bounds; it must never suppress `base.WndProc`, intercept native input messages, or replace the drop-down button.
- `ComboBoxStyle.DropDown` and `ComboBoxStyle.DropDownList` are the primary supported presentation modes. `ComboBoxStyle.Simple` remains inherited native behavior but receives only best-effort framework colors/item drawing and is not a custom Bootstrap permanent-list implementation.
- Editable `DropDown` mode keeps the native edit child authoritative. Stage 6 must not move or resize that child through undocumented handles merely to make room for `LeadingIcon`; therefore the leading icon is guaranteed in owner-drawn closed selection/list presentation where WinForms supplies `DrawItem`, while editable text chrome remains native.
- The framework does not promise a rounded native popup window. `BorderRadius` applies to framework-controlled shell/item presentation only; the OS popup can remain square.
- Designer construction must work without application bootstrap, DI, service locators, or initialized global state beyond existing safe theme defaults.
- The control must unsubscribe from `BootstrapThemeManager.ThemeChanged` and dispose only fonts it created. A caller-assigned `Font` remains caller-owned.
- The component adds declared public API after the frozen v1 baseline. `Phase16PublicApiBaselineTests` must intentionally fail first, the reconstructed API must be reviewed, and only then may the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` be updated.
- Multi-select, token/chip mode, remote/async lookup, custom popup virtualization, per-item arbitrary icons/templates, grouped items, custom search/filter engines, fully custom rounded popup chrome, and replacement keyboard navigation are outside Stage 6.
- No animation scheduler, per-control timer, async worker, external package, or `BootstrapDropdown` dependency belongs in this stage.

---

## Platform Behavior Resolved During Planning

Stage 6 deliberately preserves documented WinForms behavior instead of reimplementing it.

Relevant native references:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox.drawmode?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox.drawitem?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox.dropdownstyle?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox.autocompletemode?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.combobox.autocompletesource?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listcontrol.displaymember?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.listcontrol.valuemember?view=netframework-4.8.1>

Native behavior that is part of the Stage 6 contract:

- A `ComboBox` can be unbound (`Items`) or bound (`DataSource`, `DisplayMember`, `ValueMember`). Stage 6 must support both without copying source objects into a parallel collection.
- `SelectedIndex`, `SelectedItem`, `SelectedValue`, `Text`, `SelectedIndexChanged`, `SelectedValueChanged`, `SelectionChangeCommitted`, `DropDown`, and `DropDownClosed` retain native semantics and ordering.
- `DropDownStyle.DropDown` keeps an editable text portion; `DropDownStyle.DropDownList` is selection-only; `Simple` remains native and is not redesigned.
- `AutoCompleteMode` and `AutoCompleteSource` remain a native pair. Native validation/exception behavior, including restrictions for `DropDownList`, is not duplicated by framework validation code.
- Owner drawing is enabled through native `DrawMode`; `DrawItem` remains the supported item-rendering hook. Fixed-height items are sufficient for Stage 6, so `OwnerDrawFixed` is the framework mode.
- Native WinForms can recreate the underlying handle when style-related properties change. Stage 6 must reapply framework-owned presentation after handle creation without resetting selection, binding, or autocomplete state.
- `GetItemText(object)` / the inherited formatting path is the correct source for the displayed string. Do not use `item.ToString()` when native `DisplayMember`/formatting can produce different text.
- The native ComboBox internally owns its edit/list/drop-down child windows. Tests may characterize their effects, but product code must not depend on undocumented internal child types or private fields.

When target/runtime behavior differs, tests should compare `BootstrapComboBox` with a plain native `ComboBox` configured equivalently instead of copying WinForms implementation algorithms into project test helpers.

---

## Stage 6 Public Contract

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public class BootstrapComboBox : ComboBox
{
    public BootstrapValidationState ValidationState { get; set; } // default None
    public int BorderRadius { get; set; }                          // default -1
    public IconDescriptor? LeadingIcon { get; set; }               // default null
    public IIconRenderer IconRenderer { get; set; }
}
```

Inherited native members remain the public behavioral API. Do not redeclare forwarding aliases for them.

### Public behavior

- `ValidationState = None` uses focus/neutral border tokens. `Valid` uses `theme.Colors.Success`; `Invalid` uses `theme.Colors.Danger`.
- Disabled state overrides validation/focus border presentation with the disabled token.
- `BorderRadius = -1` resolves to `theme.Metrics.Radius`; a non-negative value is interpreted as a logical 96-DPI value and scaled through `DpiScaler`.
- `LeadingIcon = null` reserves no icon slot.
- `LeadingIcon != null` draws one framework icon in the framework-controlled closed selected-value presentation when WinForms raises owner draw for the combo edit/selection area. It is not repeated for every drop-down row in Stage 6.
- In editable `ComboBoxStyle.DropDown`, the OS/native edit child may own the closed text area; Stage 6 does not manipulate that child solely to force the icon into editable text. The icon remains available in framework-controlled owner-draw contexts, and this limitation is documented in component docs/demo notes.
- `IconRenderer = null` throws `ArgumentNullException`.
- `Items`, binding members, selected-state members, autocomplete members, editable `Text`, dropdown sizing members, and native events behave exactly as the inherited control defines them.
- The constructor establishes framework presentation (`base.DrawMode = DrawMode.OwnerDrawFixed`, theme colors/font, flat/native-friendly shell settings) but does not add items, choose a selection, assign a `DataSource`, or change native autocomplete defaults.
- Framework code treats `DrawMode` as presentation-owned. Applications should not change it. The implementation does not add a second public `DrawMode` property solely to hide the inherited one.
- The inherited accessible role/value/name semantics remain native ComboBox semantics; `LeadingIcon` stays decorative and must not replace the native accessible value.

### Explicitly unsupported/new scope not added here

Do not add any of the following during Stage 6:

- `BootstrapComboBoxItem`, a framework item wrapper, or a second items collection.
- Custom item templates/delegates.
- Per-item `IconDescriptor` API.
- Checkbox/multi-select items.
- Tags/chips/tokenization.
- Async data providers, remote search, debounce, cancellation, or loading rows.
- Built-in filtering beyond native autocomplete.
- A custom drop-down `Form`, `ToolStripDropDown`, `ListBox`, or `BootstrapDropdown`.
- A custom arrow button or replacement arrow keyboard behavior.
- Manual `SelectedIndexChanged`/`SelectedValueChanged` raising.
- A framework `SelectedValue` cache.
- A framework `Text` cache.
- A custom `Format` callback beyond inherited native `Format`/`FormatInfo` behavior.
- Variable-height owner-draw items.
- Rounded OS popup-window corners.
- Animation on opening, focus, validation, or selection.

These can be evaluated later as separate public API additions if concrete requirements justify them.

---

## Internal Rendering and Layout Contract

Keep deterministic calculations in `BootstrapComboBoxRenderLogic.cs` so palette, DPI, item geometry, icon/text placement, and shell-border policy can be tested without opening a native drop-down.

### Metrics

```csharp
internal readonly struct BootstrapComboBoxMetrics
{
    public BootstrapComboBoxMetrics(
        int horizontalPadding,
        int verticalPadding,
        int iconSize,
        int iconGap,
        int itemHeight,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        IconSize = iconSize;
        IconGap = iconGap;
        ItemHeight = itemHeight;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }
    public int VerticalPadding { get; }
    public int IconSize { get; }
    public int IconGap { get; }
    public int ItemHeight { get; }
    public float BorderWidth { get; }
    public float FocusBorderWidth { get; }
    public float Radius { get; }
}
```

Pure helper:

```csharp
internal static BootstrapComboBoxMetrics ResolveMetrics(
    BootstrapThemeMetrics metrics,
    int fontHeight,
    int dpi,
    int borderRadius)
```

Rules:

```text
HorizontalPadding = Metrics.SpacingSM
VerticalPadding   = Metrics.SpacingXS
IconSize          = Metrics.SpacingLG
IconGap           = Metrics.SpacingXS
ItemHeight        = max(Metrics.ControlHeight, fontHeight + 2 * scaled VerticalPadding)
BorderWidth       = Metrics.BorderWidth
FocusBorderWidth  = Metrics.FocusBorderWidth
Radius            = BorderRadius >= 0 ? BorderRadius : Metrics.Radius
```

- `metrics == null` throws `ArgumentNullException`.
- `fontHeight <= 0` throws `ArgumentOutOfRangeException`.
- `dpi <= 0` throws `ArgumentOutOfRangeException`.
- `borderRadius < -1` throws `ArgumentOutOfRangeException`.
- Theme logical metrics scale through existing `DpiScaler`; `fontHeight` is already a device-pixel measurement and is not scaled a second time.

### Shell palette

```csharp
internal readonly struct BootstrapComboBoxPalette
{
    public BootstrapComboBoxPalette(
        Color background,
        Color foreground,
        Color border,
        Color selectedBackground,
        Color selectedForeground)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
        SelectedBackground = selectedBackground;
        SelectedForeground = selectedForeground;
    }

    public Color Background { get; }
    public Color Foreground { get; }
    public Color Border { get; }
    public Color SelectedBackground { get; }
    public Color SelectedForeground { get; }
}
```

Pure helper:

```csharp
internal static BootstrapComboBoxPalette ResolvePalette(
    BootstrapThemeColors colors,
    BootstrapValidationState validationState,
    bool containsFocus,
    bool enabled)
```

Rules:

```text
background: enabled ? colors.Surface : colors.SurfaceSecondary
foreground: enabled ? colors.Text : colors.MutedText
border:
  !enabled                         => colors.Disabled
  validationState == Valid         => colors.Success
  validationState == Invalid       => colors.Danger
  containsFocus                    => colors.Focus
  otherwise                        => colors.Border
selected background: enabled ? colors.Primary : colors.SurfaceSecondary
selected foreground: enabled
    ? ColorUtil.GetContrastingTextColor(colors.Primary, colors.Light, colors.Dark)
    : colors.MutedText
```

`ResolvePalette` must reuse `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)` for border priority rather than maintain a divergent validation-state implementation.

### Item/selected-value layout

```csharp
internal readonly struct BootstrapComboBoxItemLayout
{
    public BootstrapComboBoxItemLayout(Rectangle iconBounds, Rectangle textBounds)
    {
        IconBounds = iconBounds;
        TextBounds = textBounds;
    }

    public Rectangle IconBounds { get; }
    public Rectangle TextBounds { get; }
}

internal static BootstrapComboBoxItemLayout CalculateItemLayout(
    Rectangle bounds,
    BootstrapComboBoxMetrics metrics,
    bool showLeadingIcon,
    int trailingReserve)
```

Rules:

- `bounds.Width <= 0 || bounds.Height <= 0` returns empty rectangles.
- `trailingReserve < 0` throws `ArgumentOutOfRangeException`.
- Horizontal content starts at `bounds.Left + HorizontalPadding` and ends before `bounds.Right - HorizontalPadding - trailingReserve`.
- `showLeadingIcon=false` returns `Rectangle.Empty` for the icon and gives the text all available content width.
- `showLeadingIcon=true` allocates a square icon centered vertically, clipped to available height, followed by `IconGap` before text.
- A too-narrow row never returns negative widths; icon/text bounds collapse safely to empty/zero-width rectangles.
- Popup list rows call this helper with `showLeadingIcon=false` and `trailingReserve=0`.
- Closed owner-drawn combo selection calls it with `showLeadingIcon = LeadingIcon != null`; when the draw bounds include the native arrow area, the caller passes a measured/conservative trailing reserve. Do not hard-code a Win32 button width inside pure render logic.

### Text rendering

- Use `GetItemText(item)` to obtain display text so `DisplayMember`, formatting, and native conversion remain authoritative.
- Use `TextRenderer.DrawText` with single-line vertical centering, ellipsis, and no prefix processing unless native mnemonic behavior explicitly requires otherwise.
- Long values are ellipsized; they do not resize the control or popup item height.
- Empty/null display text draws no glyphs but still paints the row background correctly.
- Do not retain a `Brush`, `Pen`, `GraphicsPath`, or temporary bitmap between `DrawItem` calls.

### Conservative shell overlay

A native `ComboBox` deliberately disables `ControlStyles.UserPaint`; Stage 6 must not turn it into a fully owner-painted control. The implementation may override `WndProc` only to add a post-native-paint border overlay for `WM_PAINT` / `WM_NCPAINT` after calling `base.WndProc(ref m)`.

Overlay rules:

1. Native painting happens first and remains authoritative.
2. Resolve current theme, DPI, palette, and radius.
3. Draw only inside `ClientRectangle`; never obtain a non-client/window DC to repaint the OS arrow/popup.
4. Use `FocusBorderWidth` when `Focused || ContainsFocus`; otherwise `BorderWidth`.
5. Inset the border by half its stroke so it remains inside client bounds.
6. Use `RoundedPath.Create(...)`/existing geometry with best-effort radius; do not assign a rounded `Region` to the native window.
7. Dispose the scoped `Graphics`, `GraphicsPath`, `Pen`, and any brush used by the overlay.
8. Do not suppress, transform, or synthesize input messages.
9. If the OS draws square interior pixels near corners, accept/document that native limitation; do not escalate to child-window replacement.

The border overlay is presentation-only. It must not influence hit testing, drop-down button bounds, edit-child bounds, selection, or accessibility.

---

## Theme, Font, Handle, and Resource Ownership

### Construction

Suggested private state:

```csharp
private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

private BootstrapValidationState _validationState = BootstrapValidationState.None;
private int _borderRadius = -1;
private IconDescriptor? _leadingIcon;
private IIconRenderer _iconRenderer = DefaultIconRenderer;
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

Constructor responsibilities:

```csharp
base.DrawMode = DrawMode.OwnerDrawFixed;
FlatStyle = FlatStyle.Flat;
IntegralHeight = true;
BootstrapThemeManager.ThemeChanged += OnThemeChanged;
_themeSubscribed = true;
ApplyThemeFont();
ApplyTheme();
ApplyOwnerDrawMetrics();
```

Do not set `DataSource`, `Items`, `SelectedIndex`, `Text`, `DropDownStyle`, `AutoCompleteMode`, or `AutoCompleteSource` in the constructor.

### Owner-draw path

Override `OnDrawItem(DrawItemEventArgs e)` and keep all painting in one path:

```csharp
protected override void OnDrawItem(DrawItemEventArgs e)
{
    DrawBootstrapItem(e);
    base.OnDrawItem(e);
}
```

`DrawBootstrapItem` rules:

- Determine whether this is a selected/highlighted row through `DrawItemState.Selected`.
- Determine whether WinForms identifies the closed combo edit/selection area through `DrawItemState.ComboBoxEdit`.
- Resolve the item using `e.Index` only when it is inside the current native item range. When WinForms supplies a draw call without a valid item index, use current native `Text` for the display rather than indexing blindly.
- Use `GetItemText(item)` when an item exists.
- Draw the control-level `LeadingIcon` only for the closed framework-controlled selected-value area, not for every list row.
- Draw selected/highlighted rows using `SelectedBackground`/`SelectedForeground`; normal rows use `Background`/`Foreground`; disabled host uses disabled palette.
- Never call `SelectedIndex = ...`, mutate `Items`, or alter binding during painting.
- Do not invoke public selection/dropdown events manually.

Calling `base.OnDrawItem(e)` after framework painting preserves external event observers. External handlers may observe the event but Stage 6 does not support callers replacing framework painting through a custom draw mode.

### Handle recreation

Override `OnHandleCreated` and reapply only presentation state:

```csharp
protected override void OnHandleCreated(EventArgs e)
{
    base.OnHandleCreated(e);
    EnsureFrameworkDrawMode();
    ApplyTheme();
    ApplyOwnerDrawMetrics();
}
```

`EnsureFrameworkDrawMode()` sets `base.DrawMode = DrawMode.OwnerDrawFixed` only when necessary. It must be guarded against recursive handle recreation and must not touch behavioral state.

Handle recreation tests must prove that the following survive unchanged:

- `DataSource` reference.
- `DisplayMember` / `ValueMember`.
- current `SelectedIndex` / selected value where native WinForms preserves it.
- `DropDownStyle`.
- `AutoCompleteMode` / `AutoCompleteSource` / custom source.

### Theme/font lifecycle

- Subscribe once to `BootstrapThemeManager.ThemeChanged` in the constructor and record ownership with `_themeSubscribed`.
- Create the theme body font from `BootstrapThemeManager.CurrentTheme.Typography.Body` using the same ownership pattern as `BootstrapTextBox`.
- If the caller assigns `Font`, `OnFontChanged` marks `_useThemeFont = false`, disposes only the previous theme-owned font, recalculates `ItemHeight`, and repaints.
- Runtime theme switching recreates/disposes only theme-owned fonts, updates native `BackColor` / `ForeColor`, updates fixed `ItemHeight`, and invalidates both shell and item display.
- `OnEnabledChanged`, `OnGotFocus`, `OnLostFocus`, `OnSelectedIndexChanged`, `OnDropDownStyleChanged`, and DPI/size changes invalidate only as necessary; they do not duplicate native state transitions.
- `Dispose(bool)` unsubscribes the theme handler and disposes `_themeFont`. There are no retained per-item GDI resources.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs` — pure DPI metrics, palette, validation-border reuse, item/icon/text layout, and geometry guards.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs` — public native `ComboBox` subclass, public Stage 6 properties, owner-draw path, conservative shell overlay, theme/font/DPI/handle lifecycle.

**Create tests**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxRenderLogicTests.cs` — pure metrics/palette/layout/invalid-input tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs` — STA defaults, inherited semantics, unbound/bound data, display/value members, selection events, dropdown styles, autocomplete, owner draw, focus/validation, icon renderer, handle recreation, theme/font/DPI/disposal, paint smoke, and native-parity tests.

**Modify shared Advanced Inputs demo created by Stage 5**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — add ComboBox scenarios beside existing NumericBox scenarios. Do not create a second ComboBox-only top-level form.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs` — extend the shared demo integration tests with ComboBox discovery/scenario assertions.

If either Advanced Inputs file is missing at Stage 6 execution time, stop and complete/fix Stage 5 first; do not silently create a divergent demo structure in Stage 6.

**Normally no MainForm navigation change**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` should already contain the single `Advanced Inputs` navigation entry from Stage 5. Stage 6 only changes it if an integration defect is discovered; it must not add a duplicate page.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs` should already require `Advanced Inputs`; Stage 6 only extends assertions if needed for the shared page, not add another route.

**Modify docs/release baseline**

- `docs/COMPONENTS.md` — add finalized `BootstrapComboBox` contract, native authority, binding/autocomplete behavior, leading-icon semantics, and native-chrome limitations.
- `docs/TESTING.md` — add Stage 6 pure/STA/data-binding/keyboard/autocomplete/theme/DPI/designer/manual coverage.
- `docs/ARCHITECTURE.md` — add `ComboBox -> native ComboBox` to native-backed input composition and explicitly distinguish it from command-popup `BootstrapDropdown`.
- `README.md` — list ComboBox support and point to the integrated Advanced Inputs demo.
- `docs/PACKAGE_README.md` — add package-facing ComboBox capability/usage notes and native popup limitation.
- `CHANGELOG.md` — record the compatible ComboBox API addition under `Unreleased` without rewriting release history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — update the approved fingerprint only after deliberate API review.
- `docs/PUBLIC_API_BASELINE.md` — record the reviewed ComboBox declared additions and new fingerprint after the failing gate has been inspected.

---

### Task 1: Characterize native ComboBox semantics before adding framework behavior

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: native `System.Windows.Forms.ComboBox`, NUnit STA test infrastructure already used by existing control tests.
- Produces: regression fixtures that define which semantics Stage 6 must preserve and reusable test data types local to the test fixture.

- [ ] **Step 1: Add an STA test fixture and local bound-item type.**

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapComboBoxTests
{
    private sealed class Option
    {
        public Option(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}
```

- [ ] **Step 2: Add native characterization tests for unbound and bound state.**

Cover at minimum:

```csharp
[Test]
public void NativeComboBoxBindingResolvesDisplayAndSelectedValue()
{
    var options = new[]
    {
        new Option(10, "Alpha"),
        new Option(20, "Beta")
    };

    using var control = new ComboBox
    {
        DataSource = options,
        DisplayMember = nameof(Option.Name),
        ValueMember = nameof(Option.Id)
    };

    control.SelectedIndex = 1;

    Assert.Multiple((Action)(() =>
    {
        Assert.That(control.GetItemText(control.SelectedItem), Is.EqualTo("Beta"));
        Assert.That(control.SelectedValue, Is.EqualTo(20));
        Assert.That(control.SelectedItem, Is.SameAs(options[1]));
    }));
}
```

Also characterize `Items.Add(...)`, `SelectedIndex`, `SelectedItem`, and `Text` in an unbound control.

- [ ] **Step 3: Characterize event and style/autocomplete behavior using plain native ComboBox.**

Add tests for:

- programmatic `SelectedIndex` effective/no-op changes and native event counts;
- `DropDownStyle.DropDown` versus `DropDownList` editing semantics;
- native `AutoCompleteMode` / `AutoCompleteSource` round-trip;
- the native exception/constraint for unsupported autocomplete combinations rather than encoding an expected framework exception type by hand;
- native state across `CreateControl()` and one handle-recreating style change.

- [ ] **Step 4: Run the characterization tests on the primary test target.**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxTests
```

Expected: native-only characterization tests pass. These tests are the baseline for later parity assertions.

- [ ] **Step 5: Add the first failing `BootstrapComboBox` constructor/default test.**

```csharp
[Test]
public void DefaultsPreserveNativeBehaviorAndAddFrameworkPresentation()
{
    using var control = new BootstrapComboBox();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(control.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        Assert.That(control.BorderRadius, Is.EqualTo(-1));
        Assert.That(control.LeadingIcon, Is.Null);
        Assert.That(control.IconRenderer, Is.Not.Null);
        Assert.That(control.Items, Is.Empty);
        Assert.That(control.DataSource, Is.Null);
        Assert.That(control.SelectedIndex, Is.EqualTo(-1));
        Assert.That(control.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
        Assert.That(control.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.None));
        Assert.That(control.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.None));
        Assert.That(((ComboBox)control).DrawMode, Is.EqualTo(DrawMode.OwnerDrawFixed));
    }));
}
```

- [ ] **Step 6: Run and verify the new framework test fails because `BootstrapComboBox` does not exist.**

Expected: compile failure referencing the missing new control.

- [ ] **Step 7: Commit the characterization/test-first checkpoint.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "test: characterize native ComboBox semantics"
```

---

### Task 2: Freeze deterministic ComboBox render and layout rules

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxRenderLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `BootstrapThemeColors`, `BootstrapValidationState`, `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`, `DpiScaler`, `ColorUtil`.
- Produces: `BootstrapComboBoxMetrics`, `BootstrapComboBoxPalette`, `BootstrapComboBoxItemLayout`, `ResolveMetrics(...)`, `ResolvePalette(...)`, and `CalculateItemLayout(...)` exactly as specified above.

- [ ] **Step 1: Write failing metric/DPI/radius tests.**

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(168)]
[TestCase(192)]
public void ResolveMetricsScalesThemeTokensAndKeepsFontHeightPhysical(int dpi)
{
    var themeMetrics = BootstrapThemeMetrics.Default;
    var fontHeight = 15;

    var actual = BootstrapComboBoxRenderLogic.ResolveMetrics(
        themeMetrics,
        fontHeight,
        dpi,
        borderRadius: -1);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(actual.HorizontalPadding, Is.EqualTo(DpiScaler.Scale(themeMetrics.SpacingSM, dpi)));
        Assert.That(actual.VerticalPadding, Is.EqualTo(DpiScaler.Scale(themeMetrics.SpacingXS, dpi)));
        Assert.That(actual.IconSize, Is.EqualTo(DpiScaler.Scale(themeMetrics.SpacingLG, dpi)));
        Assert.That(actual.IconGap, Is.EqualTo(DpiScaler.Scale(themeMetrics.SpacingXS, dpi)));
        Assert.That(actual.ItemHeight, Is.GreaterThanOrEqualTo(fontHeight + (2 * actual.VerticalPadding)));
        Assert.That(actual.Radius, Is.EqualTo(DpiScaler.Scale((float)themeMetrics.Radius, dpi)));
    }));
}
```

Also assert explicit radius scaling and invalid `null` metrics / non-positive font height / non-positive DPI / radius below `-1`.

- [ ] **Step 2: Run the pure test file and verify it fails because render logic does not exist.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxRenderLogicTests
```

Expected: compile/test failure referencing the missing helper and structs.

- [ ] **Step 3: Add failing palette-priority tests.**

```csharp
[Test]
public void ResolvePaletteReusesInputValidationPriorityAndReadableSelectionText()
{
    var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

    var neutral = BootstrapComboBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.None, containsFocus: false, enabled: true);
    var focused = BootstrapComboBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.None, containsFocus: true, enabled: true);
    var valid = BootstrapComboBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Valid, containsFocus: true, enabled: true);
    var invalid = BootstrapComboBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: true);
    var disabled = BootstrapComboBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: false);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(neutral.Border, Is.EqualTo(colors.Border));
        Assert.That(focused.Border, Is.EqualTo(colors.Focus));
        Assert.That(valid.Border, Is.EqualTo(colors.Success));
        Assert.That(invalid.Border, Is.EqualTo(colors.Danger));
        Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
        Assert.That(disabled.Background, Is.EqualTo(colors.SurfaceSecondary));
        Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
        Assert.That(neutral.SelectedBackground, Is.EqualTo(colors.Primary));
        Assert.That(
            ColorUtil.GetContrastRatio(neutral.SelectedBackground, neutral.SelectedForeground),
            Is.GreaterThanOrEqualTo(Math.Max(
                ColorUtil.GetContrastRatio(colors.Primary, colors.Light),
                ColorUtil.GetContrastRatio(colors.Primary, colors.Dark)) - 0.001d));
    }));
}
```

Also assert undefined `BootstrapValidationState` values are rejected through the shared TextBox validation path.

- [ ] **Step 4: Add failing item-layout tests.**

Cover no icon, icon, trailing arrow reserve, very narrow bounds, empty bounds, and negative reserve:

```csharp
[Test]
public void CalculateItemLayoutReservesIconAndTrailingButtonWithoutNegativeTextWidth()
{
    var metrics = new BootstrapComboBoxMetrics(
        horizontalPadding: 8,
        verticalPadding: 4,
        iconSize: 16,
        iconGap: 4,
        itemHeight: 32,
        borderWidth: 1f,
        focusBorderWidth: 2f,
        radius: 6f);

    var layout = BootstrapComboBoxRenderLogic.CalculateItemLayout(
        new Rectangle(0, 0, 180, 32),
        metrics,
        showLeadingIcon: true,
        trailingReserve: 24);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(layout.IconBounds.Width, Is.EqualTo(16));
        Assert.That(layout.IconBounds.Height, Is.EqualTo(16));
        Assert.That(layout.TextBounds.Left, Is.GreaterThan(layout.IconBounds.Right));
        Assert.That(layout.TextBounds.Right, Is.LessThanOrEqualTo(180 - 8 - 24));
        Assert.That(layout.TextBounds.Width, Is.GreaterThanOrEqualTo(0));
    }));
}
```

- [ ] **Step 5: Implement minimal pure render logic.**

Implementation rules:

```csharp
var border = BootstrapTextBoxRenderLogic.ResolveBorderColor(
    colors,
    validationState,
    containsFocus,
    enabled);

var selectedBackground = enabled ? colors.Primary : colors.SurfaceSecondary;
var selectedForeground = enabled
    ? ColorUtil.GetContrastingTextColor(selectedBackground, colors.Light, colors.Dark)
    : colors.MutedText;
```

Keep all methods handle-free and side-effect-free.

- [ ] **Step 6: Run pure tests until green.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxRenderLogicTests
```

Expected: PASS.

- [ ] **Step 7: Commit the deterministic rendering checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBoxRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxRenderLogicTests.cs
git commit -m "test: define BootstrapComboBox render rules"
```

---

### Task 3: Implement the public ComboBox subclass without duplicating native state

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: Task 2 render logic, `BootstrapThemeManager`, `BootstrapIconRenderer.CreateDefault()`, `IconDescriptor`, `IIconRenderer`.
- Produces: public `BootstrapComboBox` contract and framework-owned presentation initialization.

- [ ] **Step 1: Expand failing public-contract tests.**

Add tests for:

```csharp
[Test]
public void BorderRadiusRejectsValuesBelowThemeSentinel()
{
    using var control = new BootstrapComboBox();
    Assert.Throws<ArgumentOutOfRangeException>(() => control.BorderRadius = -2);
}

[Test]
public void IconRendererRejectsNull()
{
    using var control = new BootstrapComboBox();
    Assert.Throws<ArgumentNullException>(() => control.IconRenderer = null!);
}
```

Also verify changing `ValidationState`, `BorderRadius`, and `LeadingIcon` does not change `SelectedIndex`, `Text`, `Items.Count`, or `DataSource`.

- [ ] **Step 2: Implement constructor and four public properties with XML documentation/designer metadata.**

Required property metadata pattern:

```csharp
[Category("Appearance")]
[Description("Selects neutral, valid, or invalid validation border presentation.")]
[DefaultValue(BootstrapValidationState.None)]
public BootstrapValidationState ValidationState { get; set; }

[Category("Appearance")]
[Description("Gets or sets the logical corner radius, or -1 to use the current theme radius.")]
[DefaultValue(-1)]
public int BorderRadius { get; set; }

[Category("Appearance")]
[Description("Specifies an optional decorative icon shown in framework-controlled selected-value presentation.")]
[DefaultValue(null)]
public IconDescriptor? LeadingIcon { get; set; }

[Browsable(false)]
[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
public IIconRenderer IconRenderer { get; set; }
```

Set `base.DrawMode = DrawMode.OwnerDrawFixed`; do not add forwarding aliases for inherited ComboBox members.

- [ ] **Step 3: Run contract/default tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxTests
```

Expected: contract tests pass; later owner-draw/theme tests can still be pending/failing until their steps are implemented.

- [ ] **Step 4: Add tests that inherited native state remains canonical.**

Use the same inputs as Task 1 and assert wrapper parity with plain `ComboBox` for:

- unbound `Items` and `SelectedIndex`;
- bound `DataSource`, `DisplayMember`, `ValueMember`, `SelectedItem`, `SelectedValue`;
- editable `Text` in `DropDown` mode;
- `DropDownList` selection-only behavior;
- `AutoCompleteMode`, `AutoCompleteSource`, `AutoCompleteCustomSource` round-trip.

- [ ] **Step 5: Run parity tests and fix only framework regressions.**

Do not add framework validation that changes native exceptions merely to make tests simpler.

- [ ] **Step 6: Commit the public-contract checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "feat: add BootstrapComboBox native contract"
```

---

### Task 4: Implement owner-drawn selected/list presentation and leading icon

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: `ResolveMetrics`, `ResolvePalette`, `CalculateItemLayout`, `GetItemText`, `IIconRenderer`.
- Produces: one owner-draw path that preserves native formatting/selection while rendering Bootstrap row colors and optional closed-selection icon.

- [ ] **Step 1: Add failing tests for native display-member text.**

Create bound `Option` data where `ToString()` deliberately differs from `Name`, then verify the framework display helper/path uses `GetItemText` and therefore renders/derives `Name`, not `ToString()`.

A test-only item can be:

```csharp
private sealed class Option
{
    // existing members...
    public override string ToString() => $"raw:{Id}";
}
```

- [ ] **Step 2: Add failing tests for draw-mode ownership and `ItemHeight` theme metrics.**

After construction and after `CreateControl()`, assert:

```csharp
Assert.That(((ComboBox)control).DrawMode, Is.EqualTo(DrawMode.OwnerDrawFixed));
Assert.That(control.ItemHeight, Is.GreaterThanOrEqualTo(control.Font.Height));
```

Change `Font` and verify item height is recomputed to remain usable.

- [ ] **Step 3: Implement `OnDrawItem` and a private draw helper.**

Required sequence:

```csharp
protected override void OnDrawItem(DrawItemEventArgs e)
{
    DrawBootstrapItem(e);
    base.OnDrawItem(e);
}
```

Inside `DrawBootstrapItem`:

1. guard invalid/empty bounds;
2. resolve theme/DPI/metrics/palette;
3. detect selected/highlighted and disabled state;
4. fill the row background;
5. resolve item/display text with native `GetItemText` or current `Text` when no item index exists;
6. calculate icon/text bounds;
7. draw `LeadingIcon` only for `DrawItemState.ComboBoxEdit` when supported by the owner-draw call;
8. draw text with `TextRenderer` and ellipsis;
9. draw focus rectangle only if native state requests it and it does not conflict with the framework shell focus border;
10. dispose all per-call GDI objects.

- [ ] **Step 4: Add icon-renderer spy tests.**

Use a test `IIconRenderer` implementation to capture calls and assert:

- `LeadingIcon = null` does not render an icon;
- a closed owner-draw selection context renders exactly one control-level icon;
- popup rows do not repeat the control-level icon;
- assigning a custom renderer is honored;
- assigning `null` remains rejected.

Do not expose a product-only `RenderForTest` method; invoke protected draw behavior through a test subclass or paint/DrawItem event path.

- [ ] **Step 5: Add long/empty/custom-font draw smoke tests.**

Use a bitmap/graphics test surface only where needed to prove no exception, bounds stay inside `e.Bounds`, and custom font metrics affect item height. Do not assert anti-aliased pixels exactly.

- [ ] **Step 6: Run control + render tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~BootstrapComboBoxTests|FullyQualifiedName~BootstrapComboBoxRenderLogicTests"
```

Expected: PASS.

- [ ] **Step 7: Commit the owner-draw checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "feat: render BootstrapComboBox items"
```

---

### Task 5: Add conservative focus/validation shell painting and handle-recreation safety

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: Task 2 palette/metrics, `RoundedPath`, native `WndProc`, `BootstrapThemeManager`.
- Produces: best-effort rounded border/focus/validation overlay that leaves all native interaction chrome intact.

- [ ] **Step 1: Add failing focus/validation palette integration tests.**

Use a test subclass exposing a protected/internal observation seam already appropriate for paint smoke, not a new public API. Verify the state matrix resolves to:

```text
disabled + invalid + focused => Disabled
valid + focused             => Success
invalid + focused           => Danger
none + focused              => Focus
none + unfocused            => Border
```

- [ ] **Step 2: Add a failing handle-recreation regression test.**

Configure a bound control with `DisplayMember`, `ValueMember`, selected item, `DropDownStyle`, and autocomplete; create the handle; trigger a supported style operation that recreates the handle; then assert framework draw mode is restored and native behavioral state remains equivalent to a plain ComboBox baseline.

- [ ] **Step 3: Implement post-native-paint shell overlay.**

Override `WndProc` with these hard rules:

```csharp
protected override void WndProc(ref Message m)
{
    base.WndProc(ref m);

    if (m.Msg == WmPaint || m.Msg == WmNcPaint)
    {
        DrawBootstrapShellOverlay();
    }
}
```

`DrawBootstrapShellOverlay()` must:

- return when handle/client size is unusable;
- create a scoped `Graphics` from the control handle only after native paint;
- draw a fill only if required to mask the native flat border without covering text/edit/dropdown-button content; otherwise draw the border only;
- use the current palette and scaled radius;
- stay entirely within `ClientRectangle`;
- never call `base.WndProc` a second time;
- never use `GetWindowDC`, `SetWindowRgn`, child edit handles, reflection, or popup handles.

- [ ] **Step 4: Implement guarded `OnHandleCreated` presentation restoration.**

Reapply only:

- `OwnerDrawFixed` when needed;
- current theme background/foreground;
- current theme-derived item height;
- invalidation.

Do not reset binding, selection, text, style, or autocomplete.

- [ ] **Step 5: Add paint smoke tests for 96/120/144/168/192 DPI calculations and small sizes.**

The pure helper owns exact geometry assertions. WinForms smoke tests only need to prove that creating/painting/resizing/enabling/disabling the control does not throw or recurse.

- [ ] **Step 6: Run control tests repeatedly to catch handle recreation recursion.**

```powershell
1..3 | ForEach-Object {
    dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxTests
}
```

Expected: all three runs PASS without stack overflow, disposed-handle access, or state loss.

- [ ] **Step 7: Commit the shell/lifecycle checkpoint.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "feat: theme BootstrapComboBox shell"
```

---

### Task 6: Prove binding and selection events remain native

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: completed `BootstrapComboBox` behavior from Tasks 3–5.
- Produces: regression coverage preventing framework presentation changes from altering data/selection semantics.

- [ ] **Step 1: Add unbound selection-event parity tests.**

Create a plain `ComboBox` and a `BootstrapComboBox` with identical items. Apply the same `SelectedIndex` transitions including no-op assignment and compare final state/event counts rather than inventing framework event rules.

- [ ] **Step 2: Add bound-object parity tests.**

Cover:

- `DataSource` object identity;
- `DisplayMember` text resolution;
- `ValueMember` and `SelectedValue`;
- changing `SelectedIndex`;
- changing the current item through `BindingSource`/`CurrencyManager` where the repository test infrastructure supports it;
- rebinding to a different list;
- clearing `DataSource` back to `null` without stale framework caches.

- [ ] **Step 3: Add format/display regression coverage.**

Where native ComboBox formatting is enabled, compare `GetItemText` output and selection with a plain ComboBox. Do not add a new framework formatting event.

- [ ] **Step 4: Add explicit tests proving presentation property changes do not raise selection events.**

Changing only `ValidationState`, `BorderRadius`, `LeadingIcon`, or `IconRenderer` must not raise `SelectedIndexChanged`, `SelectedValueChanged`, or `SelectionChangeCommitted`.

- [ ] **Step 5: Run parity tests for both target test assemblies/configurations supported by the repository.**

Use the repository commands documented in `docs/TESTING.md`; at minimum run the normal Release test project after target-specific builds in Task 10.

- [ ] **Step 6: Commit the semantic-regression checkpoint.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "test: preserve BootstrapComboBox binding semantics"
```

---

### Task 7: Add STA interaction coverage for dropdown, keyboard, and autocomplete

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: native ComboBox interaction behavior and Stage 6 presentation.
- Produces: STA regression coverage for interactive paths required by the roadmap.

- [ ] **Step 1: Add hosted-control helpers inside the test fixture.**

Use a real `Form` containing the ComboBox, create/show handles in STA, and ensure cleanup through `using`. Do not expose product internals for keyboard tests.

- [ ] **Step 2: Test `DropDown` / `DropDownClosed` lifecycle.**

Programmatically open through `DroppedDown = true` where the environment supports it, assert the native events/state, close it, and compare with a plain ComboBox if headless behavior differs. No framework popup should exist.

- [ ] **Step 3: Test keyboard selection in `DropDownList`.**

Exercise Up/Down and Enter/Escape using existing repository keyboard-message test patterns. Assert selection/focus results, not custom internal state.

- [ ] **Step 4: Test editable `DropDown` typing/type-to-select behavior.**

Verify framework owner drawing does not make the native edit child read-only and does not steal text/selection events.

- [ ] **Step 5: Test autocomplete round-trip and supported interaction.**

Configure:

```csharp
control.DropDownStyle = ComboBoxStyle.DropDown;
control.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
control.AutoCompleteSource = AutoCompleteSource.ListItems;
```

Assert values remain set after handle creation/theme change and compare resulting native selection/text behavior with a plain ComboBox rather than expecting a framework algorithm.

- [ ] **Step 6: Characterize `DropDownList` autocomplete restrictions.**

Apply the same invalid/supported property combinations to plain and Bootstrap controls and assert exception/state parity. Do not catch native exceptions in product code merely to translate them.

- [ ] **Step 7: Test disabled-state interaction.**

A disabled control must not open or alter selection through user input; presentation resolves disabled colors. Re-enabling restores interaction without reconstructing items/binding.

- [ ] **Step 8: Run the interaction subset and commit.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapComboBoxTests
```

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "test: cover BootstrapComboBox interactions"
```

---

### Task 8: Complete theme, font, DPI, designer, and disposal lifecycle

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs`

**Interfaces:**
- Consumes: existing `BootstrapThemeManager`, typography ownership pattern from `BootstrapTextBox`, Task 2 DPI metrics.
- Produces: leak-safe runtime theme/font/DPI behavior with designer-safe construction.

- [ ] **Step 1: Add failing runtime Light/Dark theme tests.**

Capture selection/data/autocomplete state, switch theme, then assert:

- `BackColor` / `ForeColor` and palette-derived rendering update;
- `ItemHeight` remains valid for the active font/DPI;
- `Items`/`DataSource`/selection/autocomplete are unchanged;
- no second theme subscription is created by handle recreation.

Restore the original global theme in `finally` so tests are isolated.

- [ ] **Step 2: Add caller-owned font tests.**

Assign a caller-owned `Font`, switch themes, dispose the control, and prove the caller font remains usable/not disposed. Also verify prior theme-owned font objects are disposed only by the control.

- [ ] **Step 3: Implement theme-font ownership identical in principle to `BootstrapTextBox`.**

Use `_settingThemeFont`, `_useThemeFont`, and `_themeFont` guards so internal theme font assignments do not mark themselves as caller overrides.

- [ ] **Step 4: Add DPI matrix tests around `ResolveMetrics` and control-height/item-height smoke.**

Exact scaling remains in pure tests. UI tests create/resize at representative logical sizes and verify no clipping-related negative geometry or exceptions.

- [ ] **Step 5: Add designer-safe construction test.**

Construct `BootstrapComboBox` without explicit application/theme bootstrap and inspect public defaults/designer metadata through `TypeDescriptor`. Do not require a service provider or container.

- [ ] **Step 6: Add disposal tests.**

Dispose before handle creation and after handle creation/theme switch/dropdown use. Subsequent global theme changes must not access the disposed control or throw.

- [ ] **Step 7: Run lifecycle tests and commit.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~BootstrapComboBoxTests|FullyQualifiedName~BootstrapComboBoxRenderLogicTests"
```

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapComboBoxTests.cs
git commit -m "test: harden BootstrapComboBox lifecycle"
```

---

### Task 9: Extend the shared Advanced Inputs demo

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`
- Inspect only unless needed: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Inspect only unless needed: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs`

**Interfaces:**
- Consumes: shared Advanced Inputs page introduced by Stage 5 and completed `BootstrapComboBox`.
- Produces: discoverable manual scenarios covering every Stage 6 roadmap demo requirement without a duplicate top-level page.

- [ ] **Step 1: Fail fast if Stage 5 demo files do not exist.**

Do not create a second page as a workaround. Confirm `AdvancedInputsDemoForm` and its tests are present because Stage 5 is a hard gate.

- [ ] **Step 2: Add demo-test assertions first.**

Require labeled scenarios for:

- unbound list;
- bound `Option` object list using `DisplayMember` / `ValueMember`;
- editable `DropDown`;
- selection-only `DropDownList`;
- autocomplete with `ListItems`;
- long item text/ellipsis;
- `LeadingIcon` and no-icon comparison;
- `ValidationState.Valid` / `Invalid`;
- disabled control;
- runtime Light/Dark switching inherited from demo shell;
- explanatory note that native popup/edit chrome can remain OS-owned.

- [ ] **Step 3: Run demo tests and verify the new assertions fail.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~AdvancedInputsDemoFormTests
```

Expected: FAIL because ComboBox scenarios are not yet present.

- [ ] **Step 4: Add the ComboBox section to `AdvancedInputsDemoForm`.**

Use real `BootstrapComboBox` instances and real data binding. Keep sample data local to the demo; do not add a reusable product item model.

Example bound setup:

```csharp
var customerCombo = new BootstrapComboBox
{
    DisplayMember = nameof(DemoOption.Name),
    ValueMember = nameof(DemoOption.Id),
    DataSource = new[]
    {
        new DemoOption(1, "Alpha Company"),
        new DemoOption(2, "Beta Company")
    },
    DropDownStyle = ComboBoxStyle.DropDownList
};
```

- [ ] **Step 5: Keep `MainForm` navigation singular.**

Assert there is still exactly one `Advanced Inputs` entry. Do not add `ComboBox` as a new route.

- [ ] **Step 6: Run Advanced Inputs + integrated demo tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~AdvancedInputsDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests"
```

Expected: PASS.

- [ ] **Step 7: Manual UI verification matrix.**

Run the demo on Windows and verify at 100%, 125%, 150%, 175%, and 200% display scaling where available:

```text
[ ] Unbound items select correctly
[ ] Bound DisplayMember text and SelectedValue are correct
[ ] DropDown mode remains editable
[ ] DropDownList blocks free typing
[ ] Up/Down/Enter/Escape retain native behavior
[ ] Autocomplete remains native and functional
[ ] Long values ellipsize without corrupting selection
[ ] Leading icon renders in supported owner-draw closed presentation
[ ] Valid/Invalid/Focused/Disabled borders are distinguishable
[ ] Light/Dark switch updates without resetting selected value
[ ] Native arrow button remains clickable and correctly hit-tested
[ ] Native popup opens/closes normally and may remain square/OS-themed
[ ] No duplicate Advanced Inputs navigation entry exists
```

- [ ] **Step 8: Commit demo coverage.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
git commit -m "demo: showcase BootstrapComboBox"
```

---

### Task 10: Update docs, review public API, run both targets, and close Stage 6

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify after intentional failure/review: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify after intentional failure/review: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Consumes: final Stage 6 implementation/tests/demo.
- Produces: reviewed package documentation and approved public API fingerprint with both target builds green.

- [ ] **Step 1: Update component/architecture docs with exact ownership boundaries.**

`docs/COMPONENTS.md` must state all of the following explicitly:

```text
BootstrapComboBox derives from native ComboBox.
Native Items/DataSource/DisplayMember/ValueMember/selection/autocomplete/events remain authoritative.
Framework owns fixed-height owner-draw item presentation, validation/focus border, theme/DPI, and optional LeadingIcon.
BootstrapDropdown is not used internally.
Editable native child, arrow button, and popup chrome remain OS/WinForms-owned.
BorderRadius is best-effort for framework-controlled shell presentation and does not promise rounded popup chrome.
```

`docs/ARCHITECTURE.md` must preserve the shallow dependency graph: ComboBox depends on foundation infrastructure + native `ComboBox`, not on NumericBox or Dropdown.

- [ ] **Step 2: Update test/readme/package/changelog documentation.**

`docs/TESTING.md` records pure, STA, binding, dropdown, keyboard, autocomplete, theme, DPI, lifecycle, designer, and manual scenarios. `README.md` / `docs/PACKAGE_README.md` show concise usage based on native binding rather than introducing a framework item type. `CHANGELOG.md` adds the Stage 6 feature under `Unreleased`.

- [ ] **Step 3: Run the public API baseline test before updating the fingerprint.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

Expected: FAIL because `BootstrapComboBox` and its four declared public properties change the frozen API surface.

- [ ] **Step 4: Review the reconstructed API before approving it.**

The review should confirm there are no accidental declared public/protected helpers, duplicate aliases, item wrappers, custom popup APIs, or test-only members. Expected intentional declared additions are limited to:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapComboBox : System.Windows.Forms.ComboBox
  BootstrapValidationState ValidationState { get; set; }
  int BorderRadius { get; set; }
  IconDescriptor? LeadingIcon { get; set; }
  IIconRenderer IconRenderer { get; set; }
```

Inherited native ComboBox members are not re-declared merely for documentation convenience.

- [ ] **Step 5: Update the approved API fingerprint and `docs/PUBLIC_API_BASELINE.md`.**

Only after Step 4 review. Record the Stage 6 addition and new fingerprint in the same format as the existing Phase 16 baseline.

- [ ] **Step 6: Run focused Stage 6 tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~BootstrapComboBox|FullyQualifiedName~AdvancedInputsDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: PASS.

- [ ] **Step 7: Build the product for both required targets.**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: both PASS with no new warnings attributable to Stage 6.

- [ ] **Step 8: Build the demo and run the complete Release test suite.**

```powershell
dotnet build .\demo\MyDmsVn.Bootstrap5WinFormUI.Demo\MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release
```

Expected: PASS.

- [ ] **Step 9: Perform final hygiene review.**

Check:

```text
[ ] No BootstrapDropdown dependency
[ ] No custom popup Form/ToolStripDropDown/ListBox
[ ] No duplicate Items/DataSource/selection/autocomplete backing state
[ ] No manually raised native selection/dropdown events
[ ] No external package
[ ] No reflection into WinForms private ComboBox internals
[ ] No child-window replacement or global hook
[ ] No leaked theme subscription or caller-owned Font disposal
[ ] No retained per-item Pen/Brush/GraphicsPath/Bitmap
[ ] No accidental public/protected test seam
[ ] No duplicate Advanced Inputs page
[ ] Both target builds pass
[ ] Full tests pass
[ ] Public API baseline intentionally reviewed and green
```

- [ ] **Step 10: Commit Stage 6 completion.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI tests/MyDmsVn.Bootstrap5WinFormUI.Tests demo/MyDmsVn.Bootstrap5WinFormUI.Demo docs README.md CHANGELOG.md
git commit -m "feat: add BootstrapComboBox"
```

---

## Stage 6 Acceptance Matrix

| Area | Acceptance criterion |
| --- | --- |
| Type | `BootstrapComboBox : ComboBox`; no wrapper/dropdown replacement |
| Native data | `Items`, `DataSource`, `DisplayMember`, `ValueMember` remain canonical |
| Selection | `SelectedIndex`, `SelectedItem`, `SelectedValue`, native events remain canonical |
| Modes | `DropDown` and `DropDownList` preserve native behavior; `Simple` is best-effort/native |
| Autocomplete | Native `AutoCompleteMode` / `AutoCompleteSource` semantics and restrictions preserved |
| Draw mode | Framework uses `OwnerDrawFixed`; no custom item template API |
| Display text | Uses native `GetItemText` / formatting path, not raw `ToString()` assumptions |
| Validation | Reuses `BootstrapValidationState` and TextBox border priority |
| Icon | Optional control-level `LeadingIcon`; no per-item icon model |
| Icon renderer | Existing source-neutral renderer; `null` rejected; no FontAwesome core dependency |
| Shell | Conservative post-native-paint border only; native edit/arrow/popup remain authoritative |
| Radius | Theme sentinel `-1`; invalid below `-1`; popup rounding not promised |
| Theme | Light/Dark runtime update without selection/binding loss |
| Font | Theme-owned fonts disposed; caller fonts remain caller-owned |
| DPI | Pure geometry tested at 96/120/144/168/192 DPI |
| Keyboard | Up/Down/Enter/Escape and editable typing remain native |
| Lifecycle | Handle recreation, disabled/enabled, disposal, theme unsubscribe tested |
| Designer | Safe parameterless construction with no bootstrap/DI requirement |
| Demo | Shared Advanced Inputs page extended; no duplicate navigation |
| Docs | Components/Testing/Architecture/README/package/changelog updated |
| API | Phase 16 baseline fails intentionally, reviewed, then updated |
| Targets | `net48` and `net8.0-windows` Release builds pass |
| Tests | Focused + complete Release suite pass |

---

## Out-of-Scope Guardrail

If implementation pressure appears to require any of the following, stop and treat it as a separate design/API proposal rather than expanding Stage 6:

- replacing the native editable child;
- replacing the drop-down button;
- constructing a custom popup/list host;
- importing `BootstrapDropdown` for data selection;
- adding a parallel item/binding/selection model;
- adding async lookup/filtering;
- adding variable-height/custom-template rows;
- introducing per-item icons/checks/groups;
- using undocumented private WinForms fields/types via reflection;
- forcing rounded popup chrome through window-region or global/native hooks.

The Stage 6 success criterion is a Bootstrap-presented **native ComboBox**, not a new combo-box widget implemented from scratch.

---

## Execution Order Summary

```text
Task 1  Native semantics characterization + first failing contract
   ↓
Task 2  Pure render/palette/layout rules
   ↓
Task 3  Public native ComboBox subclass
   ↓
Task 4  Owner-drawn item/selected presentation + LeadingIcon
   ↓
Task 5  Conservative shell border + handle safety
   ↓
Task 6  Binding/selection parity regression
   ↓
Task 7  STA dropdown/keyboard/autocomplete interaction
   ↓
Task 8  Theme/font/DPI/designer/disposal hardening
   ↓
Task 9  Shared Advanced Inputs demo
   ↓
Task 10 Docs + API baseline + both targets + full suite
```

**Gate:** Stage 6 is complete only when `BootstrapComboBox` remains behaviorally native for data/selection/dropdown/autocomplete, framework presentation is deterministic and leak-safe, the shared demo/manual matrix is green, both required target builds pass, the full test suite passes, and the public API baseline has been deliberately reviewed and updated. Only then may Stage 7 — `BootstrapDropdown` begin.