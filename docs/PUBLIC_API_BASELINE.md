# v1 Public API Baseline

## Baseline decision

Phase 16 froze the Phase 15-reviewed exported API as the proposed v1 compatibility baseline beginning with `1.0.0-rc.1`.

`BootstrapPagination` and Stage 1 `BootstrapBadge` are intentional compatible additions made while the package remains on the release-candidate line. Each API addition was reviewed through the existing fingerprint gate before the approved baseline was updated.

The baseline covers every exported type plus each declared public, protected, and protected-internal constructor, field, property, event, and method in the core assembly. Including protected surface is intentional because subclasses can depend on it.

Approved SHA-256 API fingerprint:

```text
f910eeb01ff60c86b84dab01abae4cb36ad6c11073815d5df4da93fa34b6e785
```

The reviewed Pagination addition exports only `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPagination`. Its declared surface is the parameterless constructor, `TotalItems`, `PageSize`, `CurrentPage`, `TotalPages`, `MaxVisiblePages`, `ShowFirstLast`, `ShowPreviousNext`, `ButtonSize`, `Variant`, `BorderRadius`, `PageChanged`, and the `GetPreferredSize(Size)` override inherited as part of the control contract. Pagination layout helper types remain internal.

The reviewed Badge addition exports only `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapBadge`. Its declared surface is the parameterless constructor, `Variant`, `CustomColor`, `Pill`, `BorderRadius`, `GetPreferredSize(Size)`, and the protected lifecycle/painting overrides required by the custom `Control` implementation (`Dispose`, `OnAutoSizeChanged`, `OnDpiChangedAfterParent`, `OnEnabledChanged`, `OnFontChanged`, `OnPaint`, and `OnTextChanged`). `BootstrapBadgeRenderLogic` and `BootstrapBadgePalette` remain internal; no new public enum, timer, theme service, geometry type, or dependency was introduced.

`Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline` deterministically reconstructs the surface with reflection and fails when the fingerprint changes. Its failure output contains the reconstructed surface so reviewers can inspect the change before approving a new fingerprint.

## Version compatibility policy

- `1.0.0-rc.*`: release-candidate validation of the proposed v1 surface. Any API change requires explicit review, documentation, and an intentional baseline update.
- `1.0.0` and later `1.x`: breaking changes to the frozen public/protected surface are not allowed. Compatible additions use a minor version; compatible fixes use a patch version.
- A deliberate breaking change after stable v1 requires the next major version.

Assembly compatibility remains `AssemblyVersion` `1.0.0.0` for the v1 line. Package versions use Semantic Versioning independently so RC/minor/patch releases do not force assembly-binding churn.

## What counts as a baseline change

Changes include removing or renaming a type/member, changing visibility, signature, base type, enum values, constructors, or protected extensibility hooks. Additions also change the fingerprint and therefore require review even when they are source/binary compatible.

The fingerprint gate is a review trigger, not a substitute for engineering judgment. A changed fingerprint must never be accepted merely to make CI green.
