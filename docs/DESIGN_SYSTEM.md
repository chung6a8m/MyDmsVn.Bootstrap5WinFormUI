# Design System

## 1. Intent

The design system borrows Bootstrap 5's semantic color vocabulary and clean component treatment while adapting density, focus behavior, typography, and interaction to native Windows desktop applications.

Controls consume semantic tokens. Hex values are defaults owned by the theme, not values copied throughout rendering code.

## 2. Default semantic palette

Bootstrap-inspired semantic defaults:

| Token | Default |
| --- | --- |
| Primary | `#0D6EFD` |
| Secondary | `#6C757D` |
| Success | `#198754` |
| Danger | `#DC3545` |
| Warning | `#FFC107` |
| Info | `#0DCAF0` |
| Light | `#F8F9FA` |
| Dark | `#212529` |

These values define semantic variants. Application surfaces use separate tokens so the UI does not become a collection of saturated Bootstrap colors.

## 3. Light theme surface tokens

Initial defaults:

| Token | Purpose |
| --- | --- |
| Body | Application/form background |
| Surface | Card/input/control surface |
| SurfaceSecondary | Secondary/hovered surface |
| Border | Neutral border |
| Text | Primary text |
| MutedText | Secondary text |
| Disabled | Disabled foreground/background basis |
| Focus | Focus ring/border |
| Hover | Interactive hover overlay/token |
| Active | Pressed/selected overlay/token |

Exact non-semantic values should be centralized in the theme implementation and may be tuned during the demo/hardening phases.

## 4. Dark theme

Dark mode is not created by simply swapping white and black. It needs independent Surface, SurfaceSecondary, Border, Text, MutedText, Hover, Active, and Disabled tokens while retaining recognizable semantic Primary/Success/Danger/etc. variants with sufficient contrast.

Controls must not contain code such as `if dark then Color.Black else Color.White` except for deliberate contrast calculations. Prefer theme tokens or a shared contrast helper.

## 5. Typography

Default desktop typeface: Segoe UI.

Typography should be represented as tokens rather than new `Font` allocations inside every paint pass.

Recommended initial roles:

- Body
- BodySmall
- Label
- HeadingSmall
- HeadingMedium
- Monospace only when a component genuinely needs it

Font ownership must be explicit. Do not dispose shared theme fonts from individual controls.

## 6. Density and control sizing

The framework targets productive desktop/business applications, so the default density should remain compact without becoming cramped.

Recommended starting metrics at 100% DPI:

| Token | Initial value |
| --- | ---: |
| ControlHeightSmall | 28 px |
| ControlHeight | 32 px |
| ControlHeightLarge | 38 px |
| RadiusSmall | 4 px |
| Radius | 6 px |
| RadiusLarge | 8 px |
| BorderWidth | 1 px |
| FocusBorderWidth | 2 px |

These are design tokens, not permanent hard-coded API guarantees. Tune them visually in the demo before stable release.

## 7. Spacing scale

Use a small spacing scale rather than arbitrary values:

| Token | Initial value |
| --- | ---: |
| SpacingXS | 4 px |
| SpacingSM | 8 px |
| SpacingMD | 12 px |
| SpacingLG | 16 px |
| SpacingXL | 24 px |

Padding may reuse the same scale or expose semantic padding tokens if repeated patterns justify them.

All pixel metrics must be scaled through DPI helpers before rendering/layout where WinForms does not already scale them.

## 8. Interactive states

Every interactive custom control must deliberately define:

1. Normal
2. Hover
3. Pressed/Active
4. Focused
5. Disabled
6. Selected/Expanded when relevant
7. Loading when relevant

State precedence must be deterministic. For example, Disabled wins over Hover and Pressed; Focus may coexist with Selected; Loading suppresses click interaction but still communicates focus/disabled semantics appropriately.

## 9. Focus

Focus must be visible without relying only on color changes too subtle for keyboard users.

Use a focus border/ring based on the Focus token and scaled focus width. Do not remove focus indication because a mouse hover style looks cleaner.

## 10. Icons

Icons should visually align with the text baseline and control size. The framework should normalize icon size and color independent of source.

Internal structural icons such as chevrons should prefer simple vector paths owned by the framework. Business/action icons may come from SVG, MDL2, FontAwesome, or application-provided sources through the icon abstraction.

## 11. Shadows

Use shadow sparingly. Desktop business UI benefits more from clear borders and surface hierarchy than heavy elevation.

Card shadow must be optional. Avoid expensive blur generation on every paint. A lightweight precomputed/cached approach is acceptable if lifecycle and DPI are handled correctly.

## 12. Motion

Motion should clarify state change rather than decorate the UI.

Recommended starting durations:

- Hover/color transitions: usually immediate or very short
- Collapse/expand: approximately 160–220 ms
- Progress value transition: approximately 200–300 ms
- Spinner loop: approximately 750 ms

Reduced motion should shorten or disable nonessential motion while always applying the correct final state.

## 13. Validation states

Inputs should support at least neutral, valid/success, warning when needed, and error/danger states. Validation must not be communicated by color alone; supporting text/icon patterns can be added at component/application level.

## 14. Designer defaults

A control dropped from the Toolbox should have a sensible visible size and should render safely with the default theme without requiring code in `Program.Main` or `Form.Load`.
