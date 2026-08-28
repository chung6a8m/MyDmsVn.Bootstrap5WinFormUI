# MyDmsVn.Bootstrap5WinFormUI

Bootstrap 5-inspired native Windows Forms controls for business desktop applications.

## Supported targets

- .NET Framework 4.8 (`net48`)
- .NET 8 for Windows (`net8.0-windows`)

The library is native WinForms. It does not require a browser, WebView, Bootstrap CSS, or Bootstrap JavaScript.

## Included foundation controls

`BootstrapButton`, `BootstrapButtonGroup`, `BootstrapButtonToolbar`, `BootstrapTextBox`, `BootstrapCard`, `BootstrapCollapse`, `BootstrapAccordion`, `BootstrapSpinner`, `BootstrapProgressBar`, `BootstrapSidebar`, `BootstrapDataGridView`, `BootstrapPagination`, `BootstrapBadge`, and `BootstrapAlert`, plus shared Theme, Rendering, DPI, Animation, and Icon infrastructure.

`BootstrapPagination` is a data-source-agnostic composite control. Applications own data retrieval/slicing and react to `PageChanged`; the control owns only page state and navigation presentation.

`BootstrapBadge` is a compact, auto-sized, non-interactive text indicator. `Variant` selects an existing semantic color; `CustomColor` accepts `Color.Empty` or a fully opaque override; `Pill` selects half-height pill geometry; `BorderRadius = -1` uses the current theme radius.

`BootstrapAlert` is inline semantic feedback. It supports all `BootstrapVariant` values, an optional source-neutral `Icon`, a native keyboard-accessible close affordance through `Dismissible`, deterministic `Dismiss()` / `Dismissed` semantics, and `BorderRadius = -1` for the current theme radius. Alert has no timeout, timer, overlay, floating host, or Toast queue behavior.

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

Runtime Light/Dark switching is handled through `BootstrapThemeManager`. Pagination inherits that behavior from its composed `BootstrapButtonGroup` / `BootstrapButton` children, while Badge and Alert directly update their semantic presentation and theme-owned fonts through the existing theme lifecycle. None introduces a separate theme service.

## Icons

The core package contains source-neutral icon contracts and built-in Segoe MDL2/framework-vector providers. FontAwesome.Sharp, generic SVG libraries, and SkiaSharp are not required core dependencies. Applications can supply adapters through the icon interfaces.

## Release candidate status

`1.0.0-rc.1` uses the reviewed proposed v1 public API baseline. `BootstrapPagination`, Stage 1 `BootstrapBadge`, and Stage 2 `BootstrapAlert` were added deliberately on the RC line and the compatibility fingerprint was re-reviewed before approval. The assembly compatibility version remains `1.0.0.0`.

The package is a release candidate, not an automatic NuGet.org publication.

Project source and full documentation: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI
