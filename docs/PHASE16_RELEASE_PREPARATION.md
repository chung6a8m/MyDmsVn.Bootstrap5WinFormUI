# Phase 16 — Release preparation

Phase 16 converts the hardened foundation into a reproducible `1.0.0-rc.1` release candidate. It introduces no new visual component and does not claim that environment-dependent manual checks can be replaced by CI.

## Scope completed

- Defined NuGet/package identity and Semantic Versioning metadata.
- Set the first release candidate to `1.0.0-rc.1` and the v1 assembly compatibility version to `1.0.0.0`.
- Froze the Phase 15-reviewed exported/protected API with a deterministic reflection fingerprint gate.
- Added a self-contained package README and release/changelog documentation.
- Documented the supported Visual Studio/.NET SDK/Windows build baseline.
- Added `release.ps1` to build/test by default, pack, inspect package contents, and create checksums/manifest.
- Extended CI to build, test, verify the RC package, and upload release artifacts.
- Kept package publication separate from package production; no NuGet.org push is performed.

## Package identity

```text
PackageId:       MyDmsVn.Bootstrap5WinFormUI
RC version:      1.0.0-rc.1
AssemblyVersion: 1.0.0.0
TFMs:            net48;net8.0-windows
```

The core package remains free of mandatory FontAwesome.Sharp, generic SVG, and SkiaSharp dependencies. Symbols are emitted as `.snupkg`; public XML documentation remains generated for each target.

The repository does not currently declare a license. Phase 16 therefore deliberately produces distributable build artifacts without automating public NuGet.org publication. License selection is an owner decision and must be explicit before public package publication.

## Stable public API baseline

The API surface approved from Phase 15 is frozen by `Phase16PublicApiBaselineTests` with fingerprint:

```text
74c6146fcb47e546244cc99c54597c72cf2969a3fed82c43aab42d3f97ec0465
```

The test includes exported types and their declared public/protected/protected-internal constructors, fields, properties, events, and methods. This protects both normal consumers and subclass extensibility points.

See `PUBLIC_API_BASELINE.md` for compatibility/versioning rules.

## Reproducible RC command

```powershell
pwsh ./release.ps1 -Configuration Release -Version 1.0.0-rc.1
```

The release script runs the standard build and tests unless explicitly told they were already run. Package validation then confirms PackageId/version, both target-framework assemblies, XML docs, package README, NuGet symbols package, SHA-256 checksums, source commit, and SDK version.

## Automated verification matrix

CI is the authoritative automated gate for the candidate commit:

1. Restore/build core, tests, and demo for `net48`.
2. Restore/build core, tests, and demo for `net8.0-windows`.
3. Run the complete automated suite on `net48`.
4. Run the complete automated suite on `net8.0-windows`.
5. Pack `1.0.0-rc.1`.
6. Inspect package identity/content and require `.nupkg` + `.snupkg`.
7. Emit checksum and release manifest.
8. Upload the verified files as the CI release-candidate artifact.

Phase 15's warnings-as-errors, CS1591, DPI/theme/lifecycle, optional dependency, and prototype-alias gates remain part of this same test/build path.

## Manual verification still required before stable v1

Phase 16 does not report the following as automated successes:

- Real Windows 100–200% display-scaling visual inspection.
- Visual Studio WinForms Designer save/reopen verification.
- Long interactive GDI/USER-handle soak testing.
- Large application-specific DataGrid profiling.
- Font/glyph comparison across deployment Windows versions.

These checks are defined in `RELEASING.md`. A successful CI RC means the candidate is reproducibly built and automation-clean; stable promotion still requires the manual matrix to be recorded on the exact candidate commit.
