# WinForms Test Execution Policy

This document defines the mandatory execution policy for automated WinForms tests in this repository, especially when tests are run unattended by CI or coding agents such as Codex.

The goal is deterministic failure. A broken GUI test must report a test/build failure with diagnostics; it must never wait indefinitely for a human to dismiss a Windows dialog.

## 1. Scope

This policy applies to tests that instantiate or interact with WinForms controls, create native handles, show host forms, pump the Windows message queue, edit a `DataGridView`, or otherwise execute code through WinForms event/message dispatch.

Pure logic tests that do not create WinForms handles remain covered by the general strategy in `docs/TESTING.md`.

## 2. STA is mandatory for handle-based UI tests

Tests that require WinForms handles or UI interaction must use an STA-capable NUnit execution path, normally:

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ExampleTests
{
}
```

Do not build another custom STA runner when NUnit's apartment support is sufficient.

Use `[NonParallelizable]` when a fixture owns process-global UI state, visible host forms, static theme state, or other resources that cannot be safely shared across concurrently executing tests.

## 3. Unhandled WinForms exceptions must escape to NUnit

The test assembly configures the application-wide WinForms exception mode:

```csharp
Application.SetUnhandledExceptionMode(
    UnhandledExceptionMode.ThrowException,
    threadScope: false);
```

through `Infrastructure/WinFormsTestEnvironment.cs`.

Using `threadScope: false` is required because NUnit may execute handle-based tests on STA worker threads other than the thread that runs the assembly-level setup fixture. Threads left in `Automatic` mode then inherit the application-wide `ThrowException` policy.

This is intentional. Unexpected exceptions dispatched by the WinForms message loop must propagate to the test runner instead of being converted into the default Windows Forms exception dialog.

Do not remove, weaken, or override this policy in an individual test merely to make a failing test pass.

## 4. `DataGridView.DataError` must fail fast

`DataGridView` can convert binding/edit/commit failures into `DataError` notifications and, depending on handling, may display UI instead of producing a deterministic automated-test failure.

Hosted `DataGridView` interaction tests must attach:

```csharp
DataGridViewTestGuard.FailOnDataError(grid);
```

The helper is located at:

```text
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/DataGridViewTestGuard.cs
```

If the `DataError` contains an exception, the guard rethrows the original exception directly with its stack trace preserved so a later event handler cannot suppress the failure. If no exception is supplied, it throws a diagnostic `InvalidOperationException` containing row, column, and context information.

### Exception: tests that intentionally characterize `DataError`

A test whose explicit purpose is to observe or count `DataError` events may opt out of the guard for that test only and attach its own event probe. The opt-out must be obvious at the call site.

Do not disable the guard for a whole fixture because one test needs to inspect `DataError`.

## 5. Automated tests must not require interactive dialogs

Automated tests must not leave any of the following waiting for human input:

- `MessageBox.Show(...)`
- an unexpected WinForms exception dialog
- an unbounded `Form.ShowDialog()`
- a file/color/font/printer dialog
- any other modal window that is not deterministically controlled and closed by the test harness

If product behavior needs a user-facing dialog, separate the decision/business behavior from the concrete dialog interaction so the automated test can substitute a deterministic implementation. Keep the actual dialog path for focused integration/manual verification when necessary.

Do not solve a hanging test by teaching Codex or CI to click the dialog.

## 6. Message pumping must remain bounded

`Application.DoEvents()` may be used in focused WinForms interaction tests when a native message must be processed before the next assertion.

Rules:

- Call it only at known synchronization points.
- Do not create an infinite `Application.Run()` loop in a normal NUnit test.
- Do not use arbitrary sleeps as UI synchronization.
- Dispose/close hosted forms and owned controls deterministically.
- Prefer testable state transitions over timing-based assertions.

## 7. Every test run must have a hang safety net

The repository test entry point is:

```powershell
./test.ps1
```

It runs both `net48` and `net8.0-windows` with VSTest hang detection enabled. The default per-test-host timeout is five minutes:

```powershell
./test.ps1 -HangTimeoutMinutes 5
```

For a focused direct `dotnet test` command, preserve the same protection:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -c Release -f net8.0-windows `
  --blame-hang --blame-hang-timeout 5m `
  --filter "FullyQualifiedName~SomeFixture"
```

CI also has a job-level timeout. The outer timeout is a final safety net for failures outside the testhost; it is not a replacement for `--blame-hang`.

## 8. What Codex/agents must do when a GUI test hangs

When an unattended test appears stuck:

1. Do not wait for or interact with a modal dialog manually.
2. Let the bounded test command terminate and preserve its diagnostics.
3. Identify the last executing fixture/test and inspect any blame output.
4. Check for WinForms exception routing, `DataGridView.DataError`, modal APIs, focus/message-loop reentrancy, leaked forms, and unbounded waits.
5. Reproduce with a focused test using the same hang options.
6. Fix the root cause or test harness; do not skip, ignore, or weaken the assertion merely to get a green run.
7. Run both target frameworks again before considering the change complete.

## 9. Keep fail-fast behavior in test infrastructure

Do not change production control semantics solely to make tests fail fast.

For example, `BootstrapDataGridView` must retain normal caller-owned WinForms behavior. Test-only exception routing and `DataError` enforcement belong under the test project's `Infrastructure` folder.

A production behavior change is appropriate only when it is part of the actual public/control contract, not as a workaround for the automated test environment.

## 10. Checklist for new GUI tests

Before committing a new handle-based interaction test, verify:

- The fixture/test runs in STA.
- Shared/global UI state is isolated or marked non-parallel where necessary.
- Any hosted `DataGridView` uses the fail-fast guard unless `DataError` is the behavior under test.
- No code path can wait for an uncontrolled modal dialog.
- Message pumping is finite and deterministic.
- Forms, controls, timers, subscriptions, and other owned resources are disposed.
- The command used to validate the change has hang detection enabled.
- Both `net48` and `net8.0-windows` pass.

See also `docs/TESTING.md` for the broader testing strategy.
