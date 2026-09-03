# MyDmsVn.Bootstrap5WinFormUI

A Bootstrap-inspired native Windows Forms UI framework for business desktop applications.

The project translates the visual language and component ideas of Bootstrap 5 into reusable WinForms controls. It is **not** a Bootstrap CSS/JavaScript port and does not require a browser or WebView.

## Project goals

- Native WinForms controls with a consistent Bootstrap 5-inspired visual language.
- Light and Dark themes driven by shared design tokens.
- Shared infrastructure for animation, icon rendering, DPI scaling, painting, and resource management.
- Designer-friendly controls with stable, predictable public APIs.
- Support for both .NET Framework 4.8 and .NET 8 on Windows.
- Good keyboard, focus, accessibility, DPI, and resource-management behavior.

## Target frameworks

The library multi-targets:

```xml
<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
<UseWindowsForms>true</UseWindowsForms>
```

`net8.0-windows` is the WinForms-specific TFM for the requested .NET 8 target.

## Namespace

All product code uses the root namespace:

```text
MyDmsVn.Bootstrap5WinFormUI
```

Primary child namespaces include:

```text
MyDmsVn.Bootstrap5WinFormUI.Theme
MyDmsVn.Bootstrap5WinFormUI.Animation
MyDmsVn.Bootstrap5WinFormUI.Icons
MyDmsVn.Bootstrap5WinFormUI.Rendering
MyDmsVn.Bootstrap5WinFormUI.Controls
MyDmsVn.Bootstrap5WinFormUI.Compatibility
```

## Implemented foundation components

- Theme system and Bootstrap-inspired palette
- Shared rendering and DPI helpers
- Shared finite/loop animation primitives
- Source-neutral icon abstraction
- `BootstrapSpinner`
- `BootstrapButton`
- `BootstrapButtonGroup`
- `BootstrapButtonToolbar`
- `BootstrapTextBox`
- `BootstrapCheckBox`
- `BootstrapRadioButton`
- `BootstrapSwitch`
- `BootstrapNumericBox`
- `BootstrapComboBox`
- `BootstrapSelect`
- `BootstrapLookupBox` and `BootstrapLookupColumn`
- `BootstrapInputGroup`
- `BootstrapInputGroupText`
- `BootstrapDatePicker`
- `BootstrapCalendar`
- `BootstrapCalendarPicker`
- `BootstrapDropdown`
- `BootstrapSplitButton`
- `BootstrapCard`
- `BootstrapCollapse`
- `BootstrapAccordion`
- `BootstrapAccordionHeader`
- `BootstrapProgressBar`
- `BootstrapSidebar`
- `BootstrapDataGridView`
- `BootstrapPagination`
- `BootstrapBadge`
- `BootstrapAlert`
- `BootstrapTooltip`
- `BootstrapPopover`
- `BootstrapTabControl`
- `BootstrapToast`
- `BootstrapToastContainer`
- `BootstrapToastOptions`
- `BootstrapToastHistoryItem`
- `BootstrapToastService`

## Integrated demo

The demo project is a single navigable showcase using `BootstrapSidebar` as the application navigation shell. Its root pages are Theme, Buttons / Groups / Toolbar, Inputs, Checks / Radios / Switches, Advanced Inputs, Select, Input Groups, Cards, Feedback, Collapse / Accordion, Loading / Spinner, Progress, Sidebar, DataGrid, Pagination, and Navigation / Tabs. Light/Dark switching and Reduced motion remain available while navigating.

The Advanced Inputs page is the shared native-backed input showcase. Stage 5 adds `BootstrapNumericBox` examples for integer/default values, decimal formatting/increments, thousands separators, signed ranges, validation states, read-only behavior, disabled behavior, and live `ValueChanged` feedback. Stage 6 extends the same page with `BootstrapComboBox` examples for native unbound and bound items, `DisplayMember` / `ValueMember`, editable `DropDown`, selection-only `DropDownList`, native autocomplete, long text/ellipsis, optional leading icons, validation, disabled state, explicit radius, and live native selection feedback. Stage 9 adds `BootstrapDatePicker` Long/Short/Time and custom date/date-time formats, optional unchecked checkbox, constrained range, valid/invalid, disabled, explicit-radius, and live `ValueChanged` scenarios. ComboBox popup chrome and DatePicker calendar/localized rendering remain WinForms/OS-owned.

The Select page demonstrates the dedicated Select2-style `BootstrapSelect`: local single search, multiple chips, grouping, custom values, validation, asynchronous transport-agnostic providers, delayed responses, infinite paging, later-page failure/retry, first-page retry, rapid-typing stale-result protection, keyboard/accessibility behavior, Light/Dark switching, and the real-Windows 100–200% DPI verification matrix.

The Feedback page hosts the component-expansion feedback controls. `BootstrapBadge` covers semantic, pill/custom/disabled, and long-text states; `BootstrapAlert` adds all semantic variants, optional icons, native keyboard-accessible dismissal, multiline/disabled/custom-radius states, and restore cycles; `BootstrapTooltip` adds default Dark, semantic and custom-color owner-drawn popups, explicit multiline/long captions, one Tooltip associated with multiple controls, and live native timing/state forwarding. Stage 8 adds an application-placed `BootstrapToastContainer` with manual and auto-hide Toasts, icon/multiline content, FIFO/max-visible queueing, placement, dismissal, and stress actions. The same page now demonstrates the higher-level `BootstrapToastService`, per-monitor global Toasts, semantic in-memory history, unread state, the reusable notification center, `TopMost`, and all four placements. It remains the shared runtime Light/Dark, Reduced motion, and real-Windows 100–200% DPI verification surface.

The Pagination page demonstrates bounded numeric windows, ellipses, navigation visibility, size variants, boundary/zero-item states, and application-owned DataGrid paging. `BootstrapPagination` itself does not own or slice a data source.

The Navigation / Tabs page demonstrates native-backed Tabs plus Dropdown basic/icon/state/long/stress, recursive submenu, hosted-control, mixed-composition, and `BootstrapSplitButton` scenarios without adding a second navigation route. It records the real-desktop mouse/keyboard, accessibility, custom-font, outside-click, screen-edge, multi-monitor, lifetime, and 100–200% DPI verification matrix.

Earlier Rendering / DPI, Icons, and Animation diagnostics remain available below the Theme navigation item.

See [Phase 14 — Integrated Demo Application](docs/PHASE14_INTEGRATED_DEMO.md) for the original navigation contract, [BootstrapSelect guide](docs/BOOTSTRAP_SELECT.md), [BootstrapLookup guide](docs/BOOTSTRAP_LOOKUP_BOX.md), and [Component contracts](docs/COMPONENTS.md).

See [BootstrapInputGroup guide](docs/BOOTSTRAP_INPUT_GROUP.md) for connected addon/input/button composition, supported child types, sizing, compression, reorder, RTL, and verification.

## Native ComboBox usage

`BootstrapComboBox` derives directly from WinForms `ComboBox`, so native data binding and selection APIs remain canonical:

```csharp
var customerCombo = new BootstrapComboBox
{
    DropDownStyle = ComboBoxStyle.DropDownList,
    DisplayMember = nameof(CustomerOption.Name),
    ValueMember = nameof(CustomerOption.Id),
    DataSource = customers,
    ValidationState = BootstrapValidationState.None
};

customerCombo.SelectedIndexChanged += (_, _) =>
{
    var selectedId = customerCombo.SelectedValue;
};
```

No framework item wrapper is required. `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, autocomplete, keyboard behavior, and native selection/drop-down events are inherited from `ComboBox`.

## Select2-style BootstrapSelect usage

`BootstrapSelect` is a separate managed selection control for Select2-style scenarios. It uses `BootstrapSelectItem.Value` as logical identity and supports local single/multiple search, groups, custom values, and asynchronous paged providers without adding network dependencies to the UI library.

```csharp
var customerSelect = new BootstrapSelect
{
    Placeholder = "Choose a customer...",
    SelectionMode = BootstrapSelectMode.Multiple,
    SearchEnabled = true,
    MaximumSelectionRows = 3
};

customerSelect.Items.Add(new BootstrapSelectItem(1, "Contoso") { Group = "Preferred" });
customerSelect.Items.Add(new BootstrapSelectItem(2, "Fabrikam") { Group = "Preferred" });
customerSelect.Items.Add(new BootstrapSelectItem(3, "Northwind"));
```

Set `DataProvider` to an `IBootstrapSelectDataProvider` for remote/service-backed search. The control owns debounce, cancellation, stale-generation rejection, paging, retry, deduplication, and selection snapshots; the provider only returns `BootstrapSelectPage` instances. See [docs/BOOTSTRAP_SELECT.md](docs/BOOTSTRAP_SELECT.md) for local/async examples, custom-value semantics, keyboard/accessibility behavior, ownership, theme/DPI/RTL rules, and the manual validation matrix.

## Check, radio, and switch usage

The checkable controls directly inherit native WinForms controls, so checked state, events, keyboard/mnemonic behavior, `AutoCheck`, three-state cycling, and same-parent radio grouping remain native:

```csharp
var remember = new BootstrapCheckBox
{
    Text = "Remember this device",
    Variant = BootstrapVariant.Primary,
    ValidationState = BootstrapValidationState.None
};

var standardPlan = new BootstrapRadioButton { Text = "Standard", Checked = true };
var priorityPlan = new BootstrapRadioButton { Text = "Priority" };

var notifications = new BootstrapSwitch
{
    Text = "Notifications",
    Checked = true,
    Variant = BootstrapVariant.Success
};
```

`Valid` and `Invalid` color both the indicator/track and label even while unchecked. Programmatic `CheckState.Indeterminate` is painted regardless of `ThreeState`; `ThreeState` controls only native user cycling. `Appearance.Button` and effective image modes deliberately use native painting and native preferred sizing. RadioButton exclusivity remains the native same-parent behavior; with `AutoCheck = false`, state is entirely caller-managed.

## Native DatePicker usage

`BootstrapDatePicker` is a lightweight shell around exactly one native WinForms `DateTimePicker`. Native value/range/format/checkbox/calendar behavior stays canonical:

```csharp
var dueDate = new BootstrapDatePicker
{
    MinDate = new DateTime(2026, 1, 1),
    MaxDate = new DateTime(2026, 12, 31),
    Value = new DateTime(2026, 8, 28),
    Format = DateTimePickerFormat.Custom,
    CustomFormat = "yyyy-MM-dd",
    ShowCheckBox = true,
    Checked = true,
    ValidationState = BootstrapValidationState.None
};

dueDate.ValueChanged += (_, _) =>
{
    var value = dueDate.Value;
};
```

`Value`, `MinDate`, `MaxDate`, `Format`, `CustomFormat`, `ShowCheckBox`, and `Checked` forward directly to the owned native picker. The framework adds only the themed shell, validation/focus border, theme-owned font lifecycle, and DPI-aware layout. The native localized text, calendar popup, keyboard navigation, range normalization, and exception behavior are intentionally preserved. Stage 9 does not expose `ShowUpDown`, a nullable-value abstraction, a replacement calendar, or a custom parser/culture model.

## Custom Calendar usage

`BootstrapCalendar` and `BootstrapCalendarPicker` are the separate, owner-drawn date-only calendar surface. They support single, inclusive-range, and multiple-date selection in the safe WinForms `DateTimePicker` date domain; they do not change `BootstrapDatePicker` or its native popup behavior.

```csharp
var availability = new BootstrapCalendarPicker
{
    SelectionMode = BootstrapCalendarSelectionMode.Range,
    MinDate = new DateTime(2026, 1, 1),
    MaxDate = new DateTime(2026, 12, 31),
    PlaceholderText = "Choose a date range"
};

availability.SetRange(new DateTime(2026, 8, 10), new DateTime(2026, 8, 14));
```

The custom calendar uses the current culture’s first day of week and its own owner-drawn theme/DPI presentation. The picker uses a native ToolStrip host for placement and dismissal, while the hosted calendar receives normal calendar keyboard focus.

## Native command Dropdown usage

`BootstrapDropdown` composes a caller-owned `BootstrapButton` target with one native `ToolStripDropDownMenu`. The public item collection is the source of truth; native rows are rebuilt from the current model values at each effective `Show()`.

```csharp
var actionsButton = new BootstrapButton
{
    Text = "Actions",
    Variant = BootstrapVariant.Primary,
    AutoSize = true
};

var actions = new BootstrapDropdown
{
    Target = actionsButton,
    Variant = BootstrapVariant.Primary,
    MinimumWidth = 180
};

var create = new BootstrapDropdownItem
{
    Text = "Create",
    Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
};
create.Click += (_, _) => CreateRecord();

actions.Items.Add(create);
actions.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
actions.Items.Add(new BootstrapDropdownItem { Text = "Unavailable", Enabled = false });

var more = new BootstrapDropdownItem { Text = "More" };
var export = new BootstrapDropdownItem { Text = "Export" };
export.Click += (_, _) => ExportRecord();
more.DropDownItems.Add(export);
actions.Items.Add(more);

actions.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
{
    // A fresh control is required for each snapshot; Dropdown owns it after return.
    HostedControlFactory = () => new CheckBox { Text = "Include archived", AutoSize = true }
});
```

The target and public item models remain caller-owned. Native WinForms owns menu/submenu focus, keyboard navigation, AutoClose/outside-click dismissal, and working-area placement. Returned hosted controls become framework-owned snapshot controls. Trees reject duplicate/shared/cyclic item instances and invalid kind/factory/child combinations before opening.

`BootstrapSplitButton` provides an independent primary action plus a chevron menu using the same item model:

```csharp
var save = new BootstrapSplitButton
{
    Text = "Save",
    AccessibleName = "Save document",
    Variant = BootstrapVariant.Primary,
    MinimumWidth = 200
};
save.Click += (_, _) => SaveDocument();
save.Items.Add(more);
```

The popup anchors below the full split bounds. Loading/disabled state closes and suppresses the menu. `Font` and `AccessibleName` remain inherited: a caller font persists across theme changes, and both internal focus regions resolve accessible names dynamically. Inherited `Controls` can enumerate those regions, but they are framework-owned and unsupported for caller mutation or disposal.

## Managed Tooltip and interactive Popover usage

Native Tooltip positioning remains the default. Opt into deterministic placement only where explicit collision behavior is needed:

```csharp
var tooltip = new BootstrapTooltip
{
    Positioning = BootstrapTooltipPositioning.Managed,
    Placement = BootstrapOverlayPlacement.Top,
    CollisionBehavior = BootstrapOverlayCollisionBehavior.FlipAndShift
};
tooltip.SetToolTip(saveButton, "Save changes");
```

Interactive content belongs in a Popover. The application owns both target and initially unparented content; Popover reparents but never disposes either:

```csharp
var popover = new BootstrapPopover
{
    Target = optionsButton,
    Content = optionsPanel,
    Placement = BootstrapOverlayPlacement.Auto
};
```

## Toast notification usage

`BootstrapToastContainer` remains the application-placed WinForms path. Configure a `BootstrapToast`, then transfer ownership when showing it:

```csharp
var toastContainer = new BootstrapToastContainer
{
    Placement = BootstrapToastPlacement.TopRight,
    MaximumVisibleToasts = 3,
    ToastSpacing = 8
};

var toast = new BootstrapToast
{
    Title = "Saved",
    Text = "Changes were saved successfully.",
    Variant = BootstrapVariant.Success,
    AutoHide = true,
    AutoHideDelay = 5000
};

toastContainer.ShowToast(toast); // ownership transfers here
```

Before `ShowToast`, the caller owns the Toast and may configure or dispose it. After a successful `ShowToast`, the container owns the Toast until dismissal/disposal; callers must not dispose, reparent, remove, or manually toggle `Visible`. Use `Dismiss()` or `DismissAll()` to request dismissal. `Dismissed` is raised once when logical dismissal is accepted, before exit animation and container disposal complete. The auto-hide countdown begins only after enter animation completes. Reduced motion makes enter/exit/reflow transitions synchronous but keeps `AutoHideDelay` unchanged. `TopLeft`, `TopRight`, `BottomLeft`, and `BottomRight` are supported; notifications beyond `MaximumVisibleToasts` wait in FIFO order and are promoted as visible Toasts finish dismissal.

For application-level notifications, use the higher-level service composition:

```csharp
BootstrapToastService.Default.Show(
    new BootstrapToastOptions
    {
        Title = "Saved",
        Text = "The order was saved successfully.",
        Variant = BootstrapVariant.Success
    },
    this);

BootstrapToastService.Default.ShowNotificationCenter(this);
```

The service is WinForms UI-thread-affine; create, configure, call, and dispose a manual instance on its creating UI thread. Framework display/application callbacks are marshalled internally to that thread. `relativeTo` selects the target monitor, whose working area and DPI are used explicitly. Each screen has a non-activating transient host whose clickable area is defined by `Region`, never `TransparencyKey`; a removed-screen host retires and dismisses instead of being rebound over another screen. Height-constrained hosts retain strict FIFO order.

History is bounded, semantic, in-memory state—not a snapshot of live controls—and has no OS notification or persistence integration. Notification-center refresh is completed before `HistoryChanged` is raised. If an application handler throws, the framework mutation and center refresh have already committed. User close or Alt+F4 hides the reusable center; disposing the service really disposes it and all transient hosts.

## Release candidate

Phase 16 prepares package `MyDmsVn.Bootstrap5WinFormUI` version `1.0.0-rc.1`. The v1 assembly compatibility version is `1.0.0.0`, and the proposed public/protected API is protected by a deterministic fingerprint test.

Create and validate the candidate locally with:

```powershell
pwsh ./release.ps1 -Configuration Release -Version 1.0.0-rc.1
```

The script produces `.nupkg`, `.snupkg`, SHA-256 checksums, and a release manifest under `artifacts/release`. CI runs the same package validation after both-target builds/tests and uploads the verified candidate as a workflow artifact. It does **not** automatically publish to NuGet.org.

See [Release process](docs/RELEASING.md), [v1 public API baseline](docs/PUBLIC_API_BASELINE.md), [supported build environment](docs/BUILD_ENVIRONMENT.md), and [Phase 16 release preparation](docs/PHASE16_RELEASE_PREPARATION.md).

## Hardening status

Phase 15 adds foundation-wide hardening gates for the 100–200% logical DPI matrix, runtime theme-switch stress, rapid state reversal, static-event lifetime cleanup, optional icon dependency boundaries, prototype API aliases, and XML documentation completeness. The core library treats compiler warnings as errors on both target frameworks.

Pagination extends that hardened surface without adding a new timer, theme subscription, rendering stack, data-source abstraction, or package dependency. It composes the existing `BootstrapButtonGroup` and `BootstrapButton` controls.

Badge extends the same hardened foundations as a primitive visual control: it reuses `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `RoundedPath`, and theme typography; owns one deterministic theme subscription/theme-created font lifecycle; and adds no timer, animation scheduler, icon model, geometry library, or external package.

Alert reuses those same rendering/theme primitives plus the source-neutral icon infrastructure. It owns one private native WinForms dismiss button, one deterministic theme subscription/theme-created font lifecycle, and no timeout, timer, overlay, floating host, queue manager, or Toast behavior.

Tooltip remains a thin wrapper over one native WinForms `ToolTip`. Native association, timing, popup notification, and owner drawing remain authoritative. Native placement is backward-compatible by default; managed placement computes bounds with the shared pure overlay engine, obtains the current native Tooltip HWND during owner drawing, and applies those exact bounds immediately after the current paint without cancelling or reissuing the popup. It adds no timer, interactive Tooltip content, public Show/Hide API, or static theme subscription.

Popover is the separate interactive surface. It uses an internal `ToolStripDropDown` host, caller-owned content, and the same placement engine, then preserves the engine rectangle on the native HWND after WinForms layout. `None` may intentionally overflow the working area; `Flip` changes only the placement side and does not add a cross-axis shift. Transient anchor/theme subscriptions exist only while open and are removed on close/disposal.

Tabs remain native-backed: `BootstrapTabControl` derives from WinForms `TabControl`, preserves `TabPage`, `TabPages`, selection/events, focus/keyboard, native images/tooltips, and overflow behavior, and custom-paints only the native header rectangles. Header metrics reuse Theme/DPI/Rendering helpers; the control owns one deterministic theme subscription/theme-created font lifecycle and introduces no timer, animation engine, page wrapper, custom window, or external package.

NumericBox is likewise native-backed: `BootstrapNumericBox` owns one borderless WinForms `NumericUpDown` and forwards value/range/increment/formatting/read-only semantics directly. The wrapper owns the single public tab stop, themed shell, validation/focus presentation, DPI layout, and theme/font lifecycle without replacing native parsing, spin, wheel, or boundary behavior.

ComboBox stays even closer to native WinForms: `BootstrapComboBox` derives directly from `ComboBox`, uses fixed-height owner draw only for framework-controlled item/closed-selection presentation, adds validation/focus shell rendering and an optional control-level `LeadingIcon`, and deliberately leaves binding, selection, autocomplete, edit child, arrow button, popup lifecycle, keyboard behavior, and native events authoritative. It adds no custom popup, parallel item model, timer, reflection into private WinForms internals, or external dependency.

BootstrapSelect is the separate managed selector for richer selection workflows. It reuses the shared overlay placement/collision engine, theme/DPI/rendering infrastructure, and source-neutral renderer contract while keeping provider transport outside the UI library. Local and provider result modes do not merge; selection identity is value-based; async requests are debounced/cancellable/generation-safe; paging and retry stay internal control state; selected snapshots survive page/query replacement; and custom values are opt-in. Caller-supplied providers, matchers, renderers, items, and tags remain caller-owned.

DatePicker follows the NumericBox wrapper pattern around exactly one native `DateTimePicker`. Native date state, range rules, localized display, checkbox semantics, keyboard navigation, and calendar popup remain authoritative; the wrapper owns one tab stop, shared validation/focus shell rendering, theme-created font lifecycle, and DPI-scaled layout. It introduces no `MonthCalendar`, custom popup `Form`, nullable-date model, `ShowUpDown` proxy, parsing engine, culture property, timer, animation scheduler, Win32 hook, or external dependency. `BootstrapCalendar` and `BootstrapCalendarPicker` are a distinct custom, owner-drawn date-only family with its own selection modes; they do not alter native DatePicker behavior.

Dropdown uses a native-first command-menu pattern: each opening validates and recursively snapshots caller-owned item models into native menu/submenu rows and optional `ToolStripControlHost` content. Native ToolStrip owns focus, keyboard, dismissal, and screen placement; the framework owns rendering, generated images, hosted snapshot controls, theme refresh, and cleanup. `BootstrapSplitButton` composes that infrastructure with two connected `BootstrapButton` regions and adds no custom popup form, global hook, timer, animation scheduler, live collection synchronization, or external dependency.

Toast reuses the existing feedback palette/layout rules shared with Alert, source-neutral icon rendering, `BootstrapAnimation` finite transitions, DPI helpers, and theme typography. `BootstrapToastContainer` owns FIFO queueing, max-visible enforcement, placement/reflow, and deterministic Toast disposal; each Toast owns only its semantic auto-hide countdown after enter completion. Reduced motion completes transitions immediately without changing configured delays. `BootstrapToastService` composes that existing path with internal per-screen non-activating hosts and one reusable notification center; it introduces no public window/host/DPI/dispatcher seam, second feedback palette, public timer/scheduler seam, OS notification integration, persistence layer, or external dependency.

See [Phase 15 — Hardening and API review](docs/PHASE15_HARDENING_AND_API_REVIEW.md) for the audit findings and the real-Windows/manual checks carried into release validation.

## Documentation

Start with [docs/README.md](docs/README.md).

The primary sources of truth are:

- [Product requirements](docs/PRD.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Development plan](docs/DEVELOPMENT_PLAN.md)
- [Design system](docs/DESIGN_SYSTEM.md)
- [Component contracts](docs/COMPONENTS.md)
- [BootstrapSelect guide](docs/BOOTSTRAP_SELECT.md)
- [Compatibility rules](docs/COMPATIBILITY.md)
- [Testing strategy](docs/TESTING.md)
- [Development/contribution guide](docs/CONTRIBUTING.md)
- [Architecture decisions](docs/DECISIONS.md)
- [Release process](docs/RELEASING.md)
- [Changelog](CHANGELOG.md)

## Status

Phases 0–16 of the foundation development plan are implemented through release preparation. `BootstrapPagination`, Stage 1 `BootstrapBadge`, Stage 2 `BootstrapAlert`, Stage 3 `BootstrapTooltip`, Stage 4 `BootstrapTabControl`, Stage 5 `BootstrapNumericBox`, Stage 6 `BootstrapComboBox`, Stage 7 `BootstrapDropdown`, Stage 8 `BootstrapToast` / `BootstrapToastContainer`, Stage 9 `BootstrapDatePicker`, the dedicated Select2-style `BootstrapSelect`, and the separate `BootstrapCalendar` / `BootstrapCalendarPicker` family are documented compatible control additions on top of that foundation. The current package line remains `1.0.0-rc.1`; promotion to stable `1.0.0` remains gated by the manual release matrix recorded in `docs/RELEASING.md`.

The files under `idea-drafs/` remain historical design conversations and implementation sketches. They are useful context, but they are **not authoritative specifications** and code from those files must not be copied blindly.

## Guiding principle

Build from shared foundations upward:

```text
Compatibility + Theme + Rendering + Icons
                  |
             Animation
                  |
          Primitive controls
                  |
          Composite controls
                  |
        Demo + tests + docs
```

Do not implement controls in an arbitrary order or duplicate infrastructure that already exists.
