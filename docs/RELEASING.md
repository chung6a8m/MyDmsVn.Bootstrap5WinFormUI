# Release Process

## 1. Version policy

The first release candidate is `1.0.0-rc.1`. The stable v1 assembly compatibility version is `1.0.0.0`; package versions follow Semantic Versioning.

Do not create a stable `1.0.0` package simply by removing the prerelease suffix. Stable promotion requires every automated gate plus the manual release matrix below.

## 2. Build and package locally

On a supported Windows development environment:

```powershell
pwsh ./release.ps1 -Configuration Release -Version 1.0.0-rc.1
```

Without skip switches, `release.ps1` runs the full build and test scripts first. It then packs the core project and validates:

- exactly one `.nupkg` and one `.snupkg`;
- PackageId and requested version;
- DLL and XML documentation for `net48`;
- DLL and XML documentation for `net8.0-windows*`;
- package README presence.

The output directory is `artifacts/release` by default and also contains `SHA256SUMS.txt` and `release-manifest.json` with source commit, SDK, target frameworks, and artifact names.

## 3. CI release-candidate artifact

Every CI run performs Release build, both target test runs, and package verification. GitHub Actions uploads the contents of `artifacts/release` as a workflow artifact named with the package version and source SHA.

CI does not publish to NuGet.org. The repository currently has no declared repository license, so public package publication must not be automated until the owner deliberately chooses and adds the redistribution license/package license metadata.

## 4. Manual release matrix

Before promoting an RC to stable, run the integrated demo on an interactive Windows desktop and record the result for:

- Windows display scaling: 100%, 125%, 150%, 175%, 200%.
- Light at creation, Dark at creation, Light -> Dark, Dark -> Light, controls created after a switch, and disposed controls after switching.
- Keyboard focus/activation and disabled/selected/loading/expanded states as applicable.
- Rapid Collapse/Sidebar/Progress animation reversals, reduced motion, hide/show, and disposal.
- Visual Studio Designer construction, serialization, save/reopen, and composite-control editing.
- Repeated create/dispose and interactive soak while observing GDI/USER handle growth.
- DataGrid empty/small/bound/10,000-row/loading scenarios, scrolling, sorting, resize/reorder, and application-representative data sources.
- Segoe MDL2/font fallback behavior on the Windows versions intended for deployment.

The virtual/logical DPI tests and CI lifecycle tests remain required but do not replace these real desktop checks.

## 5. Stable promotion

After the RC matrix is signed off:

1. Confirm the public API fingerprint still matches `docs/PUBLIC_API_BASELINE.md`.
2. Update package version/release notes from the validated RC to the stable version.
3. Re-run `release.ps1` for the exact stable version from the exact commit to be tagged.
4. Verify generated checksums and manifest.
5. Create the release tag from that commit and publish only through an explicitly approved distribution channel.

Any change after RC validation invalidates the previous release evidence and requires the affected verification to be repeated.
