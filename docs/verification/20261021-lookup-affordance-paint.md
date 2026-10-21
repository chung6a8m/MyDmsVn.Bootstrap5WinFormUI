# Lookup trailing accessory paint correction

Baseline: `aba102e33c48937fd92468791c287afda0a59e77`. This is a narrow rendering repair requested by the consuming application's BCF-T03 work; it does not reopen historical roadmap items.

The private lookup accessory overrides OnPaint to draw a chevron, but its constructor disables UserPaint, so the native paint/print path produces a blank square. BootstrapTextBox also omits its framework-owned accessory when applying the editor surface. The application cannot repair either through a public accessory API, and should not traverse or replace private children.

Enable UserPaint/AllPaintingInWmPaint/OptimizedDoubleBuffer for the existing control while keeping Selectable false and TabStop false. Forward the existing editor surface/foreground and invalidation to the framework accessory when installed and when the parent reapplies its palette. No new API, event, editor HWND, selection/cancellation rule, timer or resource subscription is introduced. Existing ownership/disposal remains unchanged.

`BootstrapLookupAffordanceRenderingTests` exercises Light/Dark with editable, read-only and disabled owners, actual child WM_PRINT rendering of the surface and chevron, theme invalidation, stable native editor handle and retained editable focus. It uses STA, serialization and the existing fail-fast DataGridView guard. All six rows fail on baseline (blank glyph/missing surface) before the repair.

Verification on interactive Windows, SDK8.0.425/VSTest17.11.1, actual native100 monitor:

| Scope | Target | Discovered/executed | Result |
|---|---|---|---|
| New rendering rows before fix | net48/x86 | 6 | 6 intended FAIL, zero skipped |
| Lookup and TextBox regression selector | net48/x86 | 178 | 178 PASS, zero skipped |
| Same selector | net8.0-windows/x64 | 178 | 178 PASS, zero skipped |

Commands use ordinary `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f <target> -p:PlatformTarget=<architecture> --filter 'FullyQualifiedName~BootstrapLookup|FullyQualifiedName~BootstrapTextBox' --blame-hang --blame-hang-timeout 2m --logger trx --results-directory <context> -- RunConfiguration.TargetPlatform=<architecture>`. Matching list-tests discovery has178 rows. Both product/test target graphs build through those commands. No full repository suite, native125/manual DPI, consumer integration or release acceptance is implied.

Raw TRX remain ignored in `TestResults/bcf-affordance-red`, `bcf-affordance-green-net48` and `bcf-affordance-green-net8`. Before the source commit, built product DLL SHA-256 values are net48 `1F40F33A2FC99DB4E1D1BD01BCEBA4F1611530EB9FE2319E26A4BDF76C47538F` and net8 `0C3EC18232687C5155876C53EC21AC4E0298EC00606D51674CA5D6632DA9E858`. A downstream pin must cite the upstream source commit and obtain its own affected integration evidence; this page does not advance any consumer pin.
