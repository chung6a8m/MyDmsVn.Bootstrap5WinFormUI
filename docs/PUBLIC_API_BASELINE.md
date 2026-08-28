# v1 Public API Baseline

## Baseline decision

Phase 16 froze the Phase 15-reviewed exported API as the proposed v1 compatibility baseline beginning with `1.0.0-rc.1`.

The BootstrapPagination implementation is an intentional compatible addition made while the package remains on the release-candidate line. Its API was reviewed through the existing fingerprint gate before the approved baseline was updated.

The baseline covers every exported type plus each declared public, protected, and protected-internal constructor, field, property, event, and method in the core assembly. Including protected surface is intentional because subclasses can depend on it.

Approved SHA-256 API fingerprint:

```text
84e799be1ce97c12fcd7f7d03f46a744295332f9c639c53bfa11d08def33cb02
```

The reviewed Pagination addition exports only `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPagination`. Its declared surface is the parameterless constructor, `TotalItems`, `PageSize`, `CurrentPage`, `TotalPages`, `MaxVisiblePages`, `ShowFirstLast`, `ShowPreviousNext`, `ButtonSize`, `Variant`, `BorderRadius`, `PageChanged`, and the `GetPreferredSize(Size)` override inherited as part of the control contract. Pagination layout helper types remain internal.

`Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline` deterministically reconstructs the surface with reflection and fails when the fingerprint changes. Its failure output contains the reconstructed surface so reviewers can inspect the change before approving a new fingerprint.

## Version compatibility policy

- `1.0.0-rc.*`: release-candidate validation of the proposed v1 surface. Any API change requires explicit review, documentation, and an intentional baseline update.
- `1.0.0` and later `1.x`: breaking changes to the frozen public/protected surface are not allowed. Compatible additions use a minor version; compatible fixes use a patch version.
- A deliberate breaking change after stable v1 requires the next major version.

Assembly compatibility remains `AssemblyVersion` `1.0.0.0` for the v1 line. Package versions use Semantic Versioning independently so RC/minor/patch releases do not force assembly-binding churn.

## What counts as a baseline change

Changes include removing or renaming a type/member, changing visibility, signature, base type, enum values, constructors, or protected extensibility hooks. Additions also change the fingerprint and therefore require review even when they are source/binary compatible.

The fingerprint gate is a review trigger, not a substitute for engineering judgment. A changed fingerprint must never be accepted merely to make CI green.
