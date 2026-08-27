# Development and Contribution Guide

## 1. Development environment

Recommended environment:

- Windows 10 or Windows 11
- Visual Studio 2022 with .NET desktop development workload
- A .NET SDK capable of building the .NET 8 target and SDK-style .NET Framework target
- .NET Framework 4.8 targeting pack

## 2. Before starting a change

Read:

- `docs/PRD.md`
- `docs/ARCHITECTURE.md`
- `docs/COMPONENTS.md` for the affected component
- `docs/COMPATIBILITY.md`
- `docs/TESTING.md`

Then confirm where the change sits in `docs/DEVELOPMENT_PLAN.md`.

If the change introduces a new architectural dependency or changes public component relationships, update `docs/DECISIONS.md` before or with the implementation.

## 3. Change design

For each nontrivial component change, identify:

- Which shared foundation it consumes
- Which public state/properties/events it adds or changes
- Which states must be rendered
- Which keyboard/focus behavior applies
- Which DPI/theme/lifecycle scenarios need tests
- Whether the behavior already exists in another primitive and should be composed instead

## 4. Implementation style

- Keep each file focused on one responsibility.
- Prefer composition over deep inheritance.
- Prefer pure helper functions for geometry/state calculations that can be unit tested.
- Keep target-specific branches out of control code when a compatibility helper can isolate them.
- Use semantic tokens rather than literal theme colors.
- Do not allocate disposable GDI objects without clear ownership.

## 5. External dependencies

Adding a package requires answering:

1. Why is the dependency needed?
2. Does it support both `net48` and `net8.0-windows`?
3. Can it be optional rather than core?
4. Does it create a public API dependency?
5. Is the license suitable for redistribution?

FontAwesome.Sharp must remain optional. Generic SVG rendering may be an adapter if the renderer would otherwise impose an unnecessary core dependency.

## 6. Public API review

Before introducing a public member, compare it with existing naming in `docs/COMPONENTS.md`.

Avoid aliases and one-off naming. A property concept shared by components should use the same name unless WinForms itself reserves/conflicts with that name.

When a new public type is required, prefer small enums/value objects over magic strings.

## 7. Tests first for logic

For pure logic and state algorithms, add a failing test before implementation where practical.

For custom painting, extract calculations that deserve deterministic tests and use the Demo application for visual quality verification rather than brittle screenshot-only tests.

## 8. Build and verification

Before considering a change complete:

```text
Build net48
Build net8.0-windows
Run automated tests
Run affected demo page
Switch Light <-> Dark
Exercise keyboard/focus path
Check relevant DPI states
Check create/dispose lifecycle for animated controls
```

## 9. Documentation

Update documentation when:

- Public API changes
- A component's behavior changes
- A dependency or target-framework rule changes
- An architecture decision is made
- A new component is added to scope

Examples in documentation must use the real namespace `MyDmsVn.Bootstrap5WinFormUI`, never the historical placeholder `YourApp`.

## 10. Historical drafts

`idea-drafs/` must remain readable as design history, but new implementation work should not modify those files to make them appear authoritative. Move resolved decisions into the appropriate `docs/` file instead.
