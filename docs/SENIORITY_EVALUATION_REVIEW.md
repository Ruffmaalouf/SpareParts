# SpareParts Seniority Assessment (Code-Based)

## Assessment Date
- **March 24, 2026 (UTC)**

## What was evaluated
This review is based on direct code inspection of representative backend, API composition, desktop client, and test files:
- `src/SpareParts.Api/Program.cs`
- `src/SpareParts.Infrastructure/Services/CreateSaleHandler.cs`
- `src/SpareParts.Infrastructure/Data/Repositories.cs`
- `src/SpareParts.Desktop.Helpers/ApiClient.cs`
- `src/SpareParts.Desktop.ViewModels/ManagementViewModel.cs`
- `tests/SpareParts.ArchitectureTests/ArchitectureTests.cs`
- `tests/SpareParts.ArchitectureTests/CriticalPathTests.cs`

---

## Executive Verdict
**Current level: strong mid-level with targeted senior-level patterns.**

The codebase demonstrates several senior behaviors (clear layering, dependency inversion in critical workflows, architecture tests, transaction-oriented orchestration), but still has maintainability bottlenecks that keep the overall implementation from being consistently senior across all layers.

---

## Evidence by engineering dimension

### 1) Architecture & boundaries
**Strengths**
- Solution is separated into `Domain`, `Infrastructure`, `Api`, and desktop projects.
- Composition root registers policy/strategy abstractions for accounting, totals, payment status, and use-case handlers, which is a strong separation of concerns signal.

**Gaps**
- Data access has many repository contracts and concrete repository implementations in a single file (`Repositories.cs`), which will become a merge/conflict and ownership hotspot as scope grows.

**Assessment**: good architectural direction, but repository packaging and module ownership can be made more senior-grade.

### 2) Use-case orchestration & correctness
**Strengths**
- `CreateSaleHandler` follows a coherent use-case flow:
  1. validate request,
  2. load and verify business inputs,
  3. calculate totals,
  4. generate unique invoice number,
  5. persist invoice/items,
  6. adjust inventory,
  7. create journal entries,
  8. commit transaction.
- Explicit conflict paths for stock and invoice-number collisions indicate practical production thinking.

**Gaps**
- Some business constants (`"Sale"`, `"Purchase"`) remain stringly-typed and repeated; these are minor but common drift points at scale.

**Assessment**: senior-leaning implementation on backend critical path.

### 3) Dependency inversion & extensibility
**Strengths**
- Backend handlers depend on interfaces (`IInventoryService`, `IInvoiceTotalsCalculator`, `IPaymentStatusPolicy`, strategy interfaces), enabling targeted testability and policy replacement.
- API startup wires dependencies in one place with environment-aware guards for required secrets/settings.

**Gaps**
- Desktop side still includes singleton/static style access (`ApiClient.Instance`) and broad API surface in one client class, which reduces composability and unit isolation.

**Assessment**: backend DIP is strong; desktop DIP is mixed.

### 4) Client architecture & maintainability
**Strengths**
- `ManagementViewModel` has been partially decomposed into feature view models (`CustomersFeature`, `SuppliersFeature`, etc.), indicating movement in the right direction.

**Gaps**
- The same view model still owns a very broad state/command surface and orchestration responsibilities.
- Property forwarding plus command/event coordination density is high, increasing change risk and regression probability.

**Assessment**: currently mid-level maintainability profile on desktop layer.

### 5) Testing maturity
**Strengths**
- Architecture tests assert key boundary expectations.
- Critical-path tests cover totals calculation, stock adjustment behavior, accounting balancing, and payment-policy behavior.

**Gaps**
- Current tests are useful but still a thin slice; broader workflow and failure-path coverage (DB transaction rollback, API error envelope parity, race conditions) would strengthen senior-level confidence.

**Assessment**: solid baseline; not yet comprehensive.

---

## Seniority scorecard (1–5)
- **Architecture boundaries:** 3.8
- **Use-case orchestration:** 4.0
- **SOLID / dependency inversion:** 3.8
- **Desktop maintainability:** 3.1
- **Testing depth:** 3.4
- **Operational robustness:** 3.6

**Overall weighted assessment: 3.6 / 5**

Interpretation:
- This is **above mid-level** with **clear senior traits in backend service design**.
- To confidently classify as **senior across the full stack**, the main upgrades should target desktop decomposition, repository modularization, and deeper automated coverage of failure and concurrency paths.

---

## High-ROI actions to reach a clear senior bar
1. **Modularize `Repositories.cs` by aggregate/feature**
   - Split interfaces + implementations into focused files/folders (inventory, sales, purchases, master data, accounting).
2. **Further split `ManagementViewModel`**
   - Move orchestration into coordinator services and keep feature VMs narrow and independently testable.
3. **Unify desktop error handling contracts**
   - Prefer typed domain/API exceptions over generic message-based throws.
4. **Expand critical-path tests**
   - Add transaction rollback tests, duplicate-number contention tests, and API-to-client error envelope contract tests.
5. **Reduce stringly-typed domain markers**
   - Introduce constants or small value objects for reference/movement types.

---

## Final evaluation statement
If this code is for a **seniority assessment**, the most accurate rating today is:

> **Strong Mid-Level Engineer (Senior-leaning), with senior-quality backend patterns but uneven maturity in desktop architecture and long-term maintainability controls.**
