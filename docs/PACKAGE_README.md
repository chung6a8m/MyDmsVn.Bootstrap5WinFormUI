# MyDmsVn.Bootstrap5WinFormUI

Bootstrap 5-inspired native Windows Forms controls for business desktop applications.

## Supported targets

- .NET Framework 4.8 (`net48`)
- .NET 8 for Windows (`net8.0-windows`)

The library is native WinForms. It does not require a browser, WebView, Bootstrap CSS, or Bootstrap JavaScript.

## Included foundation controls

`BootstrapButton`, `BootstrapButtonGroup`, `BootstrapButtonToolbar`, `BootstrapTextBox`, `BootstrapCard`, `BootstrapCollapse`, `BootstrapAccordion`, `BootstrapSpinner`, `BootstrapProgressBar`, `BootstrapSidebar`, and `BootstrapDataGridView`, plus shared Theme, Rendering, DPI, Animation, and Icon infrastructure.

## Minimal example

```csharp
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

BootstrapThemeManager.CurrentTheme =
    BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

var saveButton = new BootstrapButton
{
    Text = "Save",
    Variant = BootstrapVariant.Primary,
    AutoSize = true
};
```

Runtime Light/Dark switching is handled through `BootstrapThemeManager`. Controls update through the shared theme lifecycle.

## Icons

The core package contains source-neutral icon contracts and built-in Segoe MDL2/framework-vector providers. FontAwesome.Sharp, generic SVG libraries, and SkiaSharp are not required core dependencies. Applications can supply adapters through the icon interfaces.

## Release candidate status

`1.0.0-rc.1` freezes the proposed v1 public API baseline for compatibility review. The package is a release candidate, not an automatic NuGet.org publication.

Project source and full documentation: https://github.com/chung6a8m/MyDmsVn.Bootstrap5WinFormUI
