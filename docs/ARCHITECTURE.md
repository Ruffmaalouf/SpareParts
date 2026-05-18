# SpareParts Architecture

## Solution Entry Points

- `SpareParts.sln`
  Full repository solution. Use this when you need to touch multiple layers.
- `solutions/SpareParts.Backend.sln`
  Domain, infrastructure, service host, and bounded API entry points.
- `solutions/SpareParts.Desktop.sln`
  Desktop shell, controls, helpers, interfaces, and view state projects.
- `solutions/SpareParts.Tests.sln`
  Test-first entry point with architecture, integration, and desktop management tests.

## Layer Map

### Core

- `SpareParts.Domain`
  Business entities, value objects, DTOs, and shared domain rules.
- `SpareParts.Application`
  Reusable application workflows, pricing/calculation services, orchestration models, and use-case logic that should stay UI-free.
- `SpareParts.Infrastructure.Interfaces`
  Persistence-facing and workflow-facing contracts owned by the domain/infrastructure boundary.

### Backend

- `SpareParts.Infrastructure`
  Dapper/SQL implementations, orchestration services, accounting strategies, and repository-level work.
- `SpareParts.Api`
  Shared service host, DI composition, common controllers, and capability-based API bootstrapping.
- `SpareParts.Sales.Api`
- `SpareParts.Purchases.Api`
- `SpareParts.Inventory.Api`
- `SpareParts.Identity.Api`
- `SpareParts.Catalog.Api`
  Capability-specific API entry points that compose the shared `SpareParts.Api` host.

### Desktop

- `SpareParts.Desktop.Abstractions`
  UI-agnostic desktop contracts for dialogs, workspace launchers, and presentation-safe request models.
- `SpareParts.Desktop.Interfaces`
  API client contracts and desktop-specific integration models.
- `SpareParts.Desktop.Helpers`
  HTTP clients, theme services, commands, notifications, and cross-cutting helpers.
- `SpareParts.Desktop.Controls`
  Reusable desktop controls, dialogs, pickers, search surfaces, and tabs.
- `SpareParts.Desktop.ViewModels`
  Desktop state, workflow coordinators, management flows, purchasing flows, and report builder logic.
- `SpareParts.Desktop.Wpf`
  Application shell, windows, converters, factories, and WPF composition.

## Dependency Rules

- `Domain` must not reference `Infrastructure`, `Api`, or any desktop assembly.
- `Application` may depend on `Domain`, but not on `Infrastructure`, `Api`, or desktop assemblies.
- `Infrastructure.Interfaces` may depend on `Domain`, but not on `Infrastructure`, `Api`, or desktop assemblies.
- `Infrastructure` may depend on `Domain` and `Infrastructure.Interfaces`, but not on desktop assemblies.
- `Api` projects may depend on backend projects only, never on desktop projects.
- `Desktop.Interfaces` may depend on `Domain`, but not on `Helpers`, `ViewModels`, or `Wpf`.
- `Desktop.Abstractions` must stay UI-framework-free and must not depend on `Helpers`, `ViewModels`, `Controls`, or `Wpf`.
- `Desktop.Helpers` may depend on `Desktop.Interfaces` and `Domain`, but not on `ViewModels` or `Wpf`.
- `Desktop.ViewModels` may depend on `Domain`, `Application`, `Desktop.Abstractions`, `Desktop.Interfaces`, `Desktop.Helpers`, and `Desktop.Controls`, but not on `Desktop.Wpf`.
- `Desktop.Wpf` is the outer presentation shell and may depend on the desktop projects beneath it.

These rules are enforced by the architecture and desktop guardrail tests.

## Shared Build Policy

- Common compiler defaults now live in `Directory.Build.props`.
- Package versions now live in `Directory.Packages.props`.
- Project files should reference packages without inline versions unless there is a deliberate exception.

## Folder Strategy

- Keep one public type per file.
- Group files by `feature` first, then by `role` when needed.
- Windows and user controls stay in WPF/control projects.
- Domain and contract projects should stay framework-light.
