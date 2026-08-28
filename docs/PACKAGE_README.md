# MyDmsVn.Bootstrap5WinFormUI

Bootstrap 5-inspired native Windows Forms controls for business desktop applications.

## Supported targets

- .NET Framework 4.8 (`net48`)
- .NET 8 for Windows (`net8.0-windows`)

The library is native WinForms. It does not require a browser, WebView, Bootstrap CSS, or Bootstrap JavaScript.

## Included foundation controls

`BootstrapButton`, `BootstrapButtonGroup`, `BootstrapButtonToolbar`, `BootstrapTextBox`, `BootstrapNumericBox`, `BootstrapComboBox`, `BootstrapCard`, `BootstrapCollapse`, `BootstrapAccordion`, `BootstrapSpinner`, `BootstrapProgressBar`, `BootstrapSidebar`, `BootstrapDataGridView`, `BootstrapPagination`, `BootstrapBadge`, `BootstrapAlert`, `BootstrapTooltip`, and `BootstrapTabControl`, plus shared Theme, Rendering, DPI, Animation, and Icon infrastructure.

`BootstrapNumericBox` is a native-backed numeric input. It owns one borderless WinForms `NumericUpDown` and forwards `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, and `ReadOnly` directly, while the framework owns the themed shell, validation/focus rendering, DPI layout, single public tab stop, and `BorderRadius`. Native range exceptions, spin buttons, Up/Down keys, mouse wheel, parsing, and formatting semantics remain native.

`BootstrapComboBox` derives directly from WinForms `ComboBox`. Native `Items`, `DataSource`, `DisplayMember`, `ValueMember`, selection, editable text, autocomplete, keyboard/drop-down behavior, and events remain authoritative. The framework owns fixed-height owner-draw presentation, validation/focus border rendering, theme/DPI integration, `BorderRadius`, and an optional control-level `LeadingIcon`. The editable child, arrow button, hit-testing, and popup remain WinForms/OS-owned; no framework item wrapper or custom popup is introduced.

`BootstrapPagination` is a data-source-agnostic composite control. Applications own data retrieval/slicing and react to `PageChanged`; the control owns only page state and navigation presentation.

`BootstrapBadge` is a compact, auto-sized, non-interactive text indicator. `Variant` selects an existing semantic color; `CustomColor` accepts `Color.Empty` or a fully opaque override; `Pill` selects half-height pill geometry; `BorderRadius = -1` uses the current theme radius.

`BootstrapAlert` is inline semantic feedback. It supports all `BootstrapVariant` values, an optional source-neutral `Icon`, a native keyboard-accessible close affordance through `Dismissible`, deterministic `Dismiss()` / `Dismissed` semantics, and `BorderRadius = -1` for the current theme radius. Alert has no timeout, timer, overlay, floating host, or Toast queue behavior.

`BootstrapTooltip` is a designer-safe `Component + IExtenderProvider` that delegates associations, native popup placement, and timing to one owned WinForms `ToolTip`. `Variant` defaults to `Dark`, `CustomColor` optionally overrides the semantic background, `BorderRadius = -1` uses the current theme radius, `ContentPadding` is DPI-scaled, and the native delay/state properties are forwarded directly. The owned native `ToolTip` remains private and no custom popup scheduler or theme subscription is introduced.

`BootstrapTabControl` derives directly from the native WinForms `TabControl`. Applications keep normal `TabPage` composition, `TabPages`, `SelectedIndex` / `SelectedTab`, `SelectedIndexChanged`, keyboard/focus behavior, `ImageList`, tab images/tooltips, and native overflow handling; the framework owner-draws only header rectangles. `TabStyle` supports `Tabs`, `Pills`, and `Underline`; `Variant` selects the active accent; `Fill` uses uniform fixed-width headers; `BorderRadius = -1` uses the current theme radius.

## Minimal example

```csharp
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

BootstrapThemeManager.CurrentTheme =
    BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

var saveButton = new BootstrapButton
{
    Text = "Save",
    Variant = BootstrapVariant.Primary,
    AutoSize = true
};

var amount = new BootstrapNumericBox
{
    Minimum = 0m,
    Maximum = 1000000m,
    Increment = 0.25m,
    DecimalPlaces = 2,
    ThousandsSeparator = true,
    Value = 1250.50m,
    ValidationState = BootstrapValidationState.Valid
};
amount.ValueChanged += (_, _) =>
{
    // React to the native numeric value change through the wrapper event.
};

var customerCombo = new BootstrapComboBox
{
    DropDownStyle = ComboBoxStyle.DropDownList,
    DisplayMember = nameof(CustomerOption.Name),
    ValueMember = nameof(CustomerOption.Id),
    DataSource = customers,
    LeadingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Check)
};
customerCombo.SelectedIndexChanged += (_, _) =>
{
    // SelectedValue / SelectedItem remain the inherited native ComboBox values.
};

var statusBadge = new BootstrapBadge
{
    Text = "Active",
    Variant = BootstrapVariant.Success,
    Pill = true
};

var savedAlert = new BootstrapAlert
{
    Text = "Changes saved.",
    Variant = BootstrapVariant.Success,
    Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
    Dismissible = true
};
savedAlert.Dismissed += (_, _) =>
{
    // The application can update surrounding UI after dismissal.
};

var tooltip = new BootstrapTooltip
{
    Variant = BootstrapVariant.Dark
};
tooltip.SetToolTip(saveButton, "Save the current changes.");

var tabs = new BootstrapTabControl
{
    TabStyle = BootstrapTabStyle.Pills,
    Variant = BootstrapVariant.Primary,
    Fill = true
};
tabs.TabPages.Add(new TabPage("General"));
tabs.TabPages.Add(new TabPage("Advanced"));
tabs.SelectedIndexChanged += (_, _) =>
{
    // Native TabControl selection event.
};

var pagination = new BootstrapPagination
{
    TotalItems = 250,
    PageSize = 20,
    CurrentPage = 1
};

pagination.PageChanged += (_, _) =>
{
    // The application loads/slices pagination.CurrentPage here.
};
```

Runtime Light/Dark switching is handled through `BootstrapThemeManager`. NumericBox, ComboBox, Badge, Alert, and TabControl directly update their semantic presentation and theme-owned fonts through the existing theme lifecycle; Pagination inherits theme behavior from its composed `BootstrapButtonGroup` / `BootstrapButton` children; Tooltip resolves the current theme only when its native Popup/Draw events execute. None introduces a separate theme service.

The integrated demo exposes NumericBox and ComboBox under **Advanced Inputs**. ComboBox scenarios include unbound and bound items, editable `DropDown`, selection-only `DropDownList`, native `SuggestAppend` autocomplete, long text, optional leading icons, validation, disabled state, explicit radius, and native selection feedback.

## Icons

The core package contains source-neutral icon contracts and built-in Segoe MDL2/framework-vector providers. FontAwesome.Sharp, generic SVG libraries, and SkiaSharp are not required core dependencies. Applications can supply adapters through the icon interfaces.

## Release candidate status

`1.0.0-rc.1` uses the reviewed proposed v1 public API baseline. `BootstrapPagination`, Stage 1 `BootstrapBadge`, Stage 2 `BootstrapAlert`, Stage 3 `BootstrapTooltip`, Stage 4 `BootstrapTabControl`, Stage 5 `BootstrapNumericBox`, and Stage 6 `BootstrapComboBox` were added deliberately on the RC line and the compatibility fingerprint is re-reviewed whenever an exported surface is added. The assembly compatibility version remains `1.0.0.0`.

The package is a release candidate, not an automatic NuGet.org publication.

Project source and full documentation: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI
