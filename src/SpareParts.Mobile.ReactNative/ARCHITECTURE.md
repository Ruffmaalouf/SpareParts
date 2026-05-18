# Mobile App Architecture

The app should stay organized around small, replaceable layers instead of large screens that mix API calls, form rules, and UI.

## Layers

- `core/`: low-level infrastructure such as API client, formatting, session, app configuration.
- `services/`: API-facing resource services. Services own HTTP behavior and reusable resource operations.
- `admin/`: admin metadata and resource mapping utilities. This is where CRUD fields, id rules, payload mapping, and search helpers live.
- `components/`: reusable UI building blocks. Components should not know screen state or API details.
- `screens/`: orchestration only. A screen wires services, state, and components together.
- `theme/`: visual design tokens and styles.

## Design Rules

- Prefer composition for React UI. Build screens from focused components.
- Use classes only where object behavior is useful. `CrudResourceService` extends `ResourceService` because CRUD resources share real behavior.
- Keep API payload mapping outside UI components.
- Keep repeated admin behavior config-driven.
- Keep screens thin. A screen should not own field schemas, payload casting, row id detection, and component definitions.
- Add a new reusable abstraction only after a pattern appears in at least two places.

## Current Pattern

Management uses:

- `admin/crud-config.js` for resource schemas.
- `admin/resource-utils.js` for row and payload mapping.
- `services/resource-service.js` for OOP resource behavior.
- `components/admin/crud-workspace.js` for reusable CRUD panels.
- `screens/management-screen.js` as the coordinator.

Use this structure for future admin screens before adding more functionality.
