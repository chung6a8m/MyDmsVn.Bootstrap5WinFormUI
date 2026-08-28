# Architecture Decisions

This file records decisions derived from the historical discussions so they do not have to be rediscovered during implementation.

## ADR-001 — Native WinForms, Bootstrap-inspired

**Decision:** Implement native WinForms controls inspired by Bootstrap 5's visual language and component concepts.

**Reason:** The framework is intended for long-lived desktop business applications and must preserve WinForms integration, Designer usage, data binding, and deployment simplicity.

**Consequence:** No browser/WebView, Bootstrap CSS runtime, Bootstrap JS runtime, DOM, or CSS utility engine is required.

## ADR-002 — Multi-target .NET Framework 4.8 and .NET 8 Windows

**Decision:** Use `net48;net8.0-windows` as the intended TFMs.

**Reason:** The product must support existing .NET Framework applications and modern .NET WinForms applications from one codebase.

**Consequence:** Shared code cannot assume modern runtime APIs. Compatibility helpers/conditional implementations are required when APIs differ.

## ADR-003 — Root namespace is fixed

**Decision:** Use `MyDmsVn.Bootstrap5WinFormUI` as the root namespace.

**Consequence:** Historical `YourApp.*` and `BootstrapWinForms.*` namespaces in prototype notes are examples only and must not enter production code.

## ADR-004 — Theme is centralized and event-driven

**Decision:** Controls consume shared semantic theme tokens and react to a central theme-change notification.

**Reason:** Manual application calls such as `button.RefreshTheme()` do not scale and make theme switching error-prone.

**Consequence:** Controls need deterministic theme-subscription lifecycle and safe default theme behavior in the Designer.

## ADR-005 — Shared animation primitives

**Decision:** Provide finite and loop animation abstractions reused by animated controls.

**Reason:** The design discussions repeatedly converged on eliminating independent timers/animation engines for Spinner, Progress, Collapse, Accordion, and loading states.

**Consequence:** Control implementations consume animation abstractions. The internal scheduler may evolve later without changing control APIs.

## ADR-006 — Accordion composes Collapse

**Decision:** `BootstrapAccordion` and its items reuse `BootstrapCollapse` for expansion state, measurement, and animation.

**Reason:** Collapse is useful independently for filters, expandable cards, sidebar sections, and other future components.

**Consequence:** Accordion must not own a second height animation implementation.

## ADR-007 — Accordion header is a real interactive control

**Decision:** Use a dedicated focusable `BootstrapAccordionHeader` rather than a clickable `Label`.

**Reason:** The complete header must support mouse, Tab focus, Enter/Space, visible focus, accessibility metadata, and animated chevron state.

## ADR-008 — Icons are source-neutral

**Decision:** Controls depend on icon descriptors/providers rather than SVG, FontAwesome, or MDL2 directly.

**Reason:** Applications should choose icon sources without forcing one library on the framework.

**Consequence:** FontAwesome.Sharp is optional; generic SVG rendering may live behind an adapter; internal structural glyphs may be simple vector paths.

## ADR-009 — Button loading reuses Spinner

**Decision:** `BootstrapButton.Loading` uses shared spinner/loading infrastructure.

**Reason:** A button-specific spinner would duplicate animation and rendering logic.

**Consequence:** Loading preserves button size, disables command activation, and adopts variant-aware foreground color.

## ADR-010 — Designer compatibility is part of the product

**Decision:** Designer-safe parameterless construction and property serialization are requirements, not optional polish.

**Reason:** A WinForms framework that works only when controls are created in runtime code would unnecessarily limit its target users.

## ADR-011 — Historical drafts are non-authoritative

**Decision:** `idea-drafs/` remains design history. Current source of truth lives in root guidance plus `docs/`.

**Reason:** The drafts contain intermediate architectures, multiple naming variants, code using APIs unavailable on `net48`, and later corrections that supersede earlier snippets.

**Consequence:** Do not normalize prototype code into production merely to match the drafts.

## ADR-012 — v1 API baseline and Semantic Versioning

**Decision:** `1.0.0-rc.1` freezes the Phase 15-reviewed exported/protected API as the proposed v1 compatibility baseline. The v1 line uses `AssemblyVersion` `1.0.0.0`; package versions follow Semantic Versioning.

**Reason:** WinForms consumers may subclass controls and therefore depend on protected members as well as public members. A reflection fingerprint makes any surface change an explicit review event instead of accidental API drift.

**Consequence:** The fingerprint test must be deliberately updated for every API addition/change. After stable `1.0.0`, breaking changes require a new major version.

## ADR-013 — RC artifacts are produced before distribution is automated

**Decision:** CI builds and validates `.nupkg`/`.snupkg` release-candidate artifacts but does not publish them to NuGet.org.

**Reason:** Build reproducibility and package correctness can be automated independently from release-channel credentials and legal/license decisions. The repository currently declares no license.

**Consequence:** Public package publication requires an explicit owner decision on licensing and distribution. Release artifacts, checksums, and manifest remain available for validation without silently changing distribution policy.
