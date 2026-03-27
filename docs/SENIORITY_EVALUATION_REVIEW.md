# SpareParts Seniority Assessment (Code-Based)

## Assessment Date
- **March 27, 2026 (UTC)**

## What was evaluated
This review is based on direct code inspection of representative backend, API composition, desktop client, and test files:
- `src/SpareParts.Api/Program.cs`
- `src/SpareParts.Infrastructure/Services/CreateSaleHandler.cs`
- `src/SpareParts.Infrastructure/Data/Repositories/Sales/SalesRepository.cs`
- `src/SpareParts.Desktop.Helpers/ApiClient.cs`
- `src/SpareParts.Desktop.ViewModels/ManagementViewModel.cs`
- `tests/SpareParts.ArchitectureTests/ArchitectureTests.cs`
- `tests/SpareParts.ArchitectureTests/CriticalPathTests.cs`

---

## Executive Verdict
**Current level: strong mid-level with targeted senior-level patterns.**

The codebase demonstrates senior behaviors in backend orchestration and architectural intent (layering, DI composition, critical-path testing), while desktop composition and test breadth still look more mid-level.

---

## Evidence by engineering dimension

### 1) Architecture & boundaries
**Strengths**
- Solution-level separation across API, Domain, Infrastructure, Desktop Helpers, Desktop ViewModels, and interfaces projects.
- `Program.cs` acts as a clear composition root with explicit dependency registrations for core services and handlers.

**Gaps**
- Repository responsibilities are spread across many concrete classes with uneven organization and discoverability.
- Several UI-facing projects still expose broad classes with many responsibilities.

**Assessment**: good architectural direction with practical separation, but module-level ownership boundaries can be sharpened.

### 2) Use-case orchestration & correctness
**Strengths**
- `CreateSaleHandler` follows a coherent transaction-oriented flow: validation, lookups, totals, invoice generation, persistence, stock movement, accounting, and commit.
- Explicit conflict handling paths (e.g., duplicate invoice attempts / stock conditions) indicate real production awareness.

**Gaps**
- Some domain markers and identifiers remain string-based and could drift over time without stronger typing.

**Assessment**: senior-leaning implementation on a critical backend path.

### 3) Dependency inversion & extensibility
**Strengths**
- Backend orchestration depends on interfaces and policies (`IInventoryService`, `IInvoiceTotalsCalculator`, `IPaymentStatusPolicy`, accounting strategies), enabling substitution and testability.
- Composition is environment-aware with guarded startup configuration.

**Gaps**
- Desktop client includes shared static/singleton access patterns in API plumbing, which can reduce isolation and independent testability.

**Assessment**: strong backend DIP, mixed desktop DI maturity.

### 4) Client architecture & maintainability
**Strengths**
- `ManagementViewModel` includes feature-oriented decomposition patterns and command surfaces that map to business operations.

**Gaps**
- The same view model remains very broad in state and command orchestration, increasing change risk.
- Coordination logic and UI state management density suggest a need for further extraction into smaller units/coordinators.

**Assessment**: mid-level maintainability profile on desktop layer.

### 5) Testing maturity
**Strengths**
- Architecture tests enforce important structural expectations.
- Critical-path tests cover inventory/accounting behavior and transactional expectations.

**Gaps**
- Failure-path, concurrency, and rollback coverage appears narrower than a full senior-level quality bar.

**Assessment**: solid foundation, but not yet deep enough for consistently senior confidence.

---

## Seniority scorecard (1–5)
- **Architecture boundaries:** 3.8
- **Use-case orchestration:** 4.1
- **SOLID / dependency inversion:** 3.8
- **Desktop maintainability:** 3.1
- **Testing depth:** 3.5
- **Operational robustness:** 3.7

**Overall weighted assessment: 3.7 / 5**

Interpretation:
- This is **above mid-level** with **clear senior traits on backend workflows**.
- To classify as **consistently senior across the stack**, prioritize desktop decomposition, clearer repository module ownership, and deeper failure-path automation.

---

## High-ROI actions to reach a clear senior bar
1. **Further decompose desktop orchestration**
   - Split `ManagementViewModel` responsibilities into narrower coordinators and feature-specific command handlers.
2. **Strengthen repository/module discoverability**
   - Align repository organization strictly by aggregate/business capability and enforce naming consistency.
3. **Reduce stringly-typed domain markers**
   - Introduce constants/enums/value objects where domain event/reference types are repeated.
4. **Expand critical-path test depth**
   - Add rollback/concurrency and API contract parity tests (error envelope mapping end-to-end).
5. **Harden client-side error taxonomy**
   - Prefer typed, structured failures over generic message-driven handling in desktop API layers.

---

## Final evaluation statement
If this code is being used for a seniority check today, the best-fit rating is:

> **Strong Mid-Level Engineer (Senior-leaning): senior-caliber backend decisions are present, but full-stack consistency is limited by desktop architecture breadth and test-depth gaps.**
