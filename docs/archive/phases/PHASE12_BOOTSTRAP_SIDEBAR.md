# Phase 12 — BootstrapSidebar

Phase 12 adds the framework's application-navigation container. The implementation composes the existing Button, icon, Collapse, Theme, DPI, and shared finite-animation infrastructure; it does not introduce a Sidebar-specific timer, icon abstraction, or nested-height animation engine.

## Final public contract

```text
BootstrapSidebar.ExpandedWidth
BootstrapSidebar.CollapsedWidth
BootstrapSidebar.Expanded
BootstrapSidebar.SelectedItem
BootstrapSidebar.Items
BootstrapSidebar.AnimationDuration
BootstrapSidebar.IconRenderer
BootstrapSidebar.Expand()
BootstrapSidebar.Collapse()
BootstrapSidebar.Toggle()
BootstrapSidebar.ExpandedChanged
BootstrapSidebar.SelectedItemChanged

BootstrapSidebarItem.Text
BootstrapSidebarItem.Icon
BootstrapSidebarItem.BadgeText
BootstrapSidebarItem.Enabled
BootstrapSidebarItem.Expanded
BootstrapSidebarItem.Selected      // read-only to application code
BootstrapSidebarItem.Tag
BootstrapSidebarItem.Items
```

Defaults:

```text
ExpandedWidth = 260 logical px
CollapsedWidth = 72 logical px
Expanded = true
AnimationDuration = 200 ms
SelectedItem = null
```

`ExpandedWidth` must remain strictly greater than `CollapsedWidth`. `CollapsedWidth` must be positive. Invalid endpoint assignments throw `ArgumentOutOfRangeException`.

The Sidebar itself is non-focusable and exposes `AccessibleRole.Grouping`; focus remains on its navigation rows.

## Navigation item model

`BootstrapSidebarItem` is a lightweight observable navigation model rather than a Control. It owns navigation data and nested-item state while the Sidebar owns control creation, layout, selection policy, and lifecycle.

`Text` and `BadgeText` normalize null assignments to empty strings. `Icon` is the framework's existing source-neutral `IconDescriptor`. `Tag` remains application-owned data. `Items` is a nested `BindingList<BootstrapSidebarItem>` so root and child additions/removals or property changes are reflected by an existing Sidebar without recreating the application form.

`Selected` is readable publicly but can only be changed by the owning Sidebar. This prevents multiple model items from independently claiming selection while still making selected state observable to application code.

## Selection behavior

`BootstrapSidebar.SelectedItem` is the canonical selection owner.

A non-null selected item must belong to the Sidebar's current root/nested tree; assigning a foreign item throws `ArgumentException`. Selecting a new item clears the previous item's `Selected` state, sets the new item's state, refreshes the corresponding Button-backed row, and raises `SelectedItemChanged`.

Activating an enabled navigation row selects its item. Disabled rows cannot become selected through user activation. Removing the currently selected item from the tree clears `SelectedItem` deterministically.

## Button-backed navigation rows

Each navigation row is implemented by an internal `BootstrapSidebarItemButton` derived from `BootstrapButton`.

The derived row deliberately keeps Button responsibility for:

- hover/pressed surface behavior;
- selected presentation;
- focus rendering;
- native Button activation semantics;
- Tab focus;
- Enter/Space activation;
- theme subscription and Button resource ownership.

Sidebar-specific content is then rendered over that Button surface:

- source-neutral navigation icon;
- left-aligned navigation text while expanded;
- optional badge;
- framework-owned structural chevron for nested items.

This avoids copying BootstrapButton's interaction state machine while allowing the Sidebar-specific desktop layout that a normal centered command Button does not provide.

`IconRenderer` defaults to `BootstrapIconRenderer.CreateDefault()` and can be replaced with any compatible renderer. Sidebar never branches on Segoe MDL2 vs SVG vs an application-provided icon source.

## Expanded and collapsed presentation

Expanded mode shows normal item text, badges, and any logically expanded nested sections.

Collapsed mode keeps each root navigation row focusable and keeps the source-neutral icon visible. Item text and badges are hidden visually while `AccessibleName` and a tooltip retain the navigation label. Nested sections are visually collapsed while their item-model `Expanded` state is retained; expanding the Sidebar restores sections that were logically open before the width collapse.

When a collapsed parent item is activated, the Sidebar expands and opens that parent section so its child navigation becomes reachable.

## Width animation

Expanded/collapsed width transitions use `BootstrapAnimation` with `EaseInOut`; Sidebar creates no WinForms Timer.

The width properties are logical pixels and are scaled through `DpiScaler`. A transition starts from the Sidebar's current visual width rather than from a stale endpoint. Reversing during an active transition therefore continues from the width already reached. Remaining duration is proportional to the remaining distance between the configured expanded/collapsed widths.

Before a runtime handle exists, and on a Designer surface, width changes apply immediately instead of starting frame scheduling.

Reduced motion is inherited from the shared animation primitive. When Reduced motion is enabled, a requested width transition reaches its final state immediately. Runtime changes to the Reduced motion preference restart an active width transition through the same shared animation abstraction.

The animation receives the Sidebar as its lifecycle owner, so hidden/disposed behavior follows the shared animation lifecycle rather than a Sidebar-specific scheduler.

## Nested sections

Nested navigation sections are real `BootstrapCollapse` controls. Sidebar does not implement a second nested-height animation engine.

Each item that owns children receives one Collapse containing the child navigation host. The Collapse uses the Sidebar's `AnimationDuration`, keeps its own shared animation lifecycle, and receives a measured fixed expanded height derived from the nested host. Dynamic item collection changes rebuild the affected visual tree and remeasure the nested host.

When the Sidebar is collapsed, nested Collapse controls receive a collapsed request. When the Sidebar expands again, each Collapse follows the corresponding item model's retained `Expanded` state.

## Keyboard behavior

The Sidebar container itself remains outside the tab sequence. Each enabled navigation row is a normal focusable Button-backed control.

Keyboard behavior is:

```text
Tab / Shift+Tab  move through normal WinForms tab order
Enter / Space    activate the focused navigation row
Up / Down        move focus through visible enabled navigation items
Home / End       move focus to first / last visible enabled item
Right            expand Sidebar, open a closed nested section, or focus its first enabled child
Left             close an open nested section, otherwise collapse the Sidebar
```

Visible-item arrow navigation includes children only when the Sidebar is expanded and the containing item is logically expanded. Disabled items are skipped by arrow/Home/End navigation.

## Badge and accessibility behavior

Badge text is optional and uses the current theme's semantic surface/text contrast helpers. Badges disappear in collapsed mode to preserve the compact width.

Every Button-backed navigation row keeps `AccessibleName = BootstrapSidebarItem.Text`. Its accessible description reports navigation state, nested expanded/collapsed state when applicable, badge text when present, and selected state. The collapsed tooltip mirrors the navigation label for pointer users.

## Theme and DPI behavior

Sidebar background uses the current theme `Surface` color and normal foreground uses the theme text token. Child BootstrapButton and BootstrapCollapse controls continue to receive their own framework Theme notifications.

Runtime theme changes update Sidebar surface/foreground and rebuild its presentation without application calls to a manual refresh API.

Expanded/collapsed widths, row heights, spacing, nested indentation, icon extents, badge geometry, and structural glyph strokes are based on framework metrics and `DpiScaler`. DPI changes stop any stale width animation, apply the correct new target width, and rebuild the measured navigation layout.

## Lifecycle and resource ownership

Sidebar owns:

- its current width `BootstrapAnimation`;
- model collection subscriptions;
- the collapsed-mode ToolTip;
- Button/Collapse visual children it creates.

Disposal stops/releases the current width animation, detaches collection subscriptions, disposes the ToolTip, unsubscribes from Theme changes, and then lets the normal WinForms parent-child disposal chain release Button/Collapse children.

Rebuilding the navigation tree explicitly disposes removed visual controls instead of merely clearing the Controls collection, preventing old Collapse animation/theme subscriptions from surviving a dynamic model change.

## Automated coverage

Phase 12 tests cover:

- required Sidebar and SidebarItem public API presence;
- default expanded/collapsed width contract and accessibility role;
- ordered width endpoint validation;
- selection of nested items and rejection of foreign items;
- Button-backed row creation for root and nested items;
- real `BootstrapCollapse` composition for nested sections;
- enabled row activation and selected-state transfer;
- parent activation toggling nested section state;
- disabled activation suppression;
- collapsed presentation retaining a focusable row/accessibility name while hiding text/nested content;
- Sidebar demo scenarios for icons, badges, disabled items, nesting, initial selection, interactive commands, current-theme surface, and main-demo navigation.

Windows GitHub Actions runs the complete repository build/test matrix for both `net48` and `net8.0-windows`.

## Demo and manual verification

Launch the main demo and choose **Sidebar**.

Verify:

1. Expanded mode shows Home, Orders with a badge, an expanded Reports section with nested Sales/Inventory entries, and a disabled Administration entry.
2. The initial Home item has selected presentation; selecting another item moves the selected state rather than leaving multiple rows selected.
3. Activate Reports repeatedly with mouse, Enter, or Space; its children must use Collapse animation and retain valid layout during rapid toggles.
4. Choose **Toggle sidebar** repeatedly, including while a width transition is already active; the width must reverse from its current visual position without jumping through the opposite endpoint.
5. In collapsed mode, item labels/badges disappear, icons remain visible/focusable, tooltips/accessibility retain navigation names, and nested sections are not displayed.
6. Activate a nested parent while collapsed; the Sidebar expands and makes its child section available.
7. Use Tab to enter navigation rows. Use Up/Down and Home/End through visible enabled rows; disabled Administration must not become an arrow-navigation destination.
8. Use Left/Right to close/open the current nested section and collapse/expand the Sidebar according to context.
9. Choose **Select Sales** and confirm the Reports section is expanded and Sales becomes the selected item.
10. Switch Light/Dark from the main demo while the Sidebar window remains open; Sidebar, Button rows, badges, nested Collapse content, and workspace text must repaint without recreating the form.
11. Enable Reduced motion and toggle/open/close repeatedly; Sidebar width and Collapse state must settle immediately without continuous frame scheduling.
12. Disable Reduced motion and hide/show the Sidebar demo during an active transition; shared animation owner lifecycle must resume without counting hidden wall-clock time.
13. Repeat at Windows display scaling 100%, 125%, 150%, 175%, and 200%; compact width, expanded width, icon/badge alignment, row height, nested indentation, and focus rendering must remain usable.
14. Resize the demo repeatedly; nested content must not overlap or leave stale pixels.
15. Close the demo while width/nested animations are active and verify no Sidebar-owned timer/animation or theme/model subscription remains alive.
