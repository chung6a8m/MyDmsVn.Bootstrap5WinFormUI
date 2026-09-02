# BootstrapInputGroup

`BootstrapInputGroup` composes supported framework controls into one horizontal, non-wrapping row. Add and reorder children through the inherited `Controls` collection; `Controls.SetChildIndex(...)` is the canonical caller/designer reorder API.

## Supported direct children

- `BootstrapInputGroupText`
- `BootstrapTextBox` and derived `BootstrapFormattedTextBox`
- `BootstrapNumericBox`
- `BootstrapSelect` in `Single` mode only
- `BootstrapButton`
- `BootstrapSplitButton`

Native `TextBox`/`Button`, `BootstrapButtonGroup`, `BootstrapComboBox`, `BootstrapDatePicker`, Multiple-mode `BootstrapSelect`, checkbox/radio/file inputs, and arbitrary controls are not supported in v1. Invalid admission throws before reparenting or mutating the rejected child.

## Sizing and layout

`InputGroupSize` selects Small, Default, or Large theme height/radius metrics. The group applies internal connected overrides; public child properties such as `ButtonSize`, `BorderRadius`, and `ValidationState` are never rewritten.

Layout is measured in two passes. First, every visible child reports a safe minimum height; the row uses the maximum of those values and the selected theme target. This prevents native-backed editors, especially `BootstrapNumericBox`, from clipping or fighting the assigned height. Second, fixed addons/buttons keep their preferred width while TextBox, FormattedTextBox, NumericBox, and Single Select share remaining width. Constrained widths compress fixed capacity first, then soft minimums proportionally; bounds never become negative or leave the client area.

Only visible first/last controls keep outer corners. Hiding, removing, or reparenting a child clears its internal connected overrides. `RightToLeft.Yes` mirrors visual placement and corners without changing `Controls` or Tab order.

## Examples

```csharp
var username = new BootstrapInputGroup();
username.Controls.Add(new BootstrapInputGroupText { Text = "@" });
username.Controls.Add(new BootstrapTextBox { PlaceholderText = "Username" });

var currency = new BootstrapInputGroup { InputGroupSize = BootstrapInputGroupSize.Small };
currency.Controls.Add(new BootstrapInputGroupText { Text = "$" });
currency.Controls.Add(new BootstrapNumericBox { DecimalPlaces = 2 });
currency.Controls.Add(new BootstrapInputGroupText { Text = ".00" });

var names = new BootstrapInputGroup();
names.Controls.Add(new BootstrapTextBox { PlaceholderText = "First" });
names.Controls.Add(new BootstrapFormattedTextBox { PlaceholderText = "Last" });

var search = new BootstrapInputGroup();
search.Controls.Add(new BootstrapTextBox());
search.Controls.Add(new BootstrapButton { Text = "Search" });

var status = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Single };
var statusGroup = new BootstrapInputGroup();
statusGroup.Controls.Add(new BootstrapInputGroupText { Text = "Status" });
statusGroup.Controls.Add(status);

var save = new BootstrapSplitButton { Text = "Save" };
save.Items.Add(new BootstrapDropdownItem { Text = "Save as" });
var saveGroup = new BootstrapInputGroup();
saveGroup.Controls.Add(new BootstrapTextBox());
saveGroup.Controls.Add(save);
saveGroup.Controls.SetChildIndex(save, 0);
```

The group is not a tab stop. Interactive children retain native/foundation Tab, Shift+Tab, Enter, Space, editing, popup, validation, and accessibility behavior.

## Verification

Use the integrated **Input Groups** page to verify Light/Dark, Small/Default/Large, Windows 100/125/150/175/200% DPI, forward/reverse Tab traversal, Button/SplitButton activation, Single Select popup dismissal/continuation, validation/disabled states, hover/pressed seams, hidden child cleanup, runtime reorder, RTL, narrow compression, NumericBox native-height floor, and Visual Studio Designer construction.
