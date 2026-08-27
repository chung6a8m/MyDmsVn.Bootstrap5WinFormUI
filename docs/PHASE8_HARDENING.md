# Phase 8 Review Hardening

This note records post-review hardening applied to the Phase 8 `BootstrapTextBox` and `BootstrapCard` implementations. It supplements the existing contracts in `COMPONENTS.md`; it does not introduce a separate component API.

## BootstrapTextBox keyboard event forwarding

`BootstrapTextBox` keeps one public tab stop and moves the actual keyboard focus into its composed native WinForms `TextBox`. Because the native editor is an implementation detail, applications must still be able to subscribe to the normal keyboard events on `BootstrapTextBox` itself.

The native editor therefore forwards these events through the composed control:

- `KeyDown`
- `KeyPress`
- `KeyUp`
- `PreviewKeyDown`

The same event-argument instance is forwarded. Application handlers on `BootstrapTextBox` can therefore continue to use normal WinForms semantics such as `KeyEventArgs.Handled`, `KeyEventArgs.SuppressKeyPress`, `KeyPressEventArgs.Handled`, and `PreviewKeyDownEventArgs.IsInputKey` without access to the private native editor.

Regression coverage raises the native editor keyboard path and verifies that each public event fires exactly once and that handled/input-key state flows back through the original event arguments.

## BootstrapCard decoration-safe content layout

`BootstrapCard.Padding` remains caller-owned. Explicit padding is never rewritten merely to protect the card decoration.

The Card now exposes a decoration-safe `DisplayRectangle` used by normal WinForms docking/layout. Each side uses the larger of:

- the caller/theme `Padding` for that side; or
- the minimum inset required to keep rectangular Header/Body/Footer regions inside the painted rounded surface.

The right and bottom sides also reserve the optional shadow offset. This means `Padding.Empty` or very small custom padding can no longer cause the opaque region panels to extend across the rounded edge, border, or drop-shadow area. Larger caller padding continues to determine the content layout unchanged.

Changing `ShowBorder`, `ShowShadow`, or `BorderRadius` triggers layout as well as repaint because those properties can change the decoration-safe display area. Theme and DPI changes continue to recompute layout through the existing lifecycle.

Regression coverage verifies a rounded, bordered, shadowed Card with `Padding.Empty`: the public `Padding` value remains empty while `DisplayRectangle` and the fill Body stay inside all four decorated edges.

## Verification

The regression tests are included in the existing Phase 8 STA suites:

- `BootstrapTextBoxTests.NativeEditorKeyboardEventsAreForwardedThroughPublicControl`
- `BootstrapCardTests.EmptyPaddingStillKeepsSectionsInsideRoundedSurfaceAndShadow`

Manual verification should additionally include:

- subscribing to `BootstrapTextBox.KeyDown` and handling Enter while typing in the native editor;
- using `SuppressKeyPress` from the public `BootstrapTextBox` handler;
- setting `BootstrapCard.Padding = Padding.Empty` with a visible border, non-zero radius, and shadow;
- switching Light/Dark themes and repeating the Card check at supported Windows DPI settings.
