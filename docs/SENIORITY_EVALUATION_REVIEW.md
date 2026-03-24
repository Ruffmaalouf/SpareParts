# SpareParts Code Review for Seniority Evaluation

## Scope
This review focuses on the current architecture and implementation in the API, infrastructure, and WPF client layers with emphasis on:
- Implementation quality and production readiness.
- SOLID principles.
- Design-pattern usage and extension strategy.

## Executive Summary
**Current level:** Mid-level implementation with selected senior-level decisions.

**What is strong:**
- Clean project separation by layer (`Domain`, `Infrastructure`, `Api`, `Desktop`).
- Use of dependency injection in the API composition root.
- Strategy pattern is already present for accounting postings.

**What blocks a senior rating today:**
- Transaction scripts mixing business rules, persistence, and side effects in large services.
- Data access concentrated in a single God-class style context.
- Testability constraints (static singletons, concrete type coupling, broad exception handling).
- Correctness risks in reference linking and invoice number generation under concurrency.

---

## SOLID Evaluation

### 1) Single Responsibility Principle (SRP)
**Findings**
- `SalesService` and `PurchaseService` each handle validation, pricing, stock mutation, persistence, accounting, and response composition in one method.
- `ManagementViewModel` is a large orchestration + form state + API coordination class.
- `SparePartsDataContext` includes many unrelated aggregates and SQL operations.

**Impact**
- Hard to unit-test in isolation.
- High change surface and regression risk.
- Slower onboarding and code review cycles.

**Recommendation**
Split by use-case and concern:
- `ISaleCalculator`, `IStockReservationService`, `IInvoiceNumberGenerator`, `IJournalPoster`.
- Feature-specific repositories (e.g., `ISalesRepository`, `IPurchasesRepository`) instead of one mega data context.
- WPF: split `ManagementViewModel` into feature VMs (`CustomersManagementVm`, `SuppliersManagementVm`, etc.) with a coordinator shell VM.

### 2) Open/Closed Principle (OCP)
**Findings**
- Account IDs are hardcoded in composition root when creating accounting strategies.
- Payment status logic and invoice numbering are embedded directly in services.

**Impact**
- New accounting schemes or numbering rules require modifying existing code instead of extending behavior.

**Recommendation**
- Move posting account configuration to typed options (database or configuration) and inject through interfaces.
- Introduce policy objects (`IPaymentStatusPolicy`, `IInvoiceNumberPolicy`) to allow extension without modification.

### 3) Liskov Substitution Principle (LSP)
**Findings**
- No obvious LSP violations in inheritance usage.
- The strategy interface for accounting is a positive sign.

**Recommendation**
- Keep strategy contracts stable and document invariants (balanced journal lines, non-negative amounts, etc.).

### 4) Interface Segregation Principle (ISP)
**Findings**
- API client abstraction is broad and used as a single surface for many features.

**Recommendation**
- Split into smaller interfaces (`IUsersApi`, `ICatalogApi`, `ISalesApi`, etc.) so consumers depend only on what they use.

### 5) Dependency Inversion Principle (DIP)
**Findings**
- Good: constructor DI is used in API and services.
- Gaps: services instantiate concrete dependencies internally (`new SparePartsDataContext`, `new InventoryService`) and view models depend on static singleton API.

**Recommendation**
- Inject abstractions for unit-of-work, repositories, and domain services.
- Replace static singleton API client usage with injected interfaces in the view-model composition root.

---

## Design Pattern Assessment

## Patterns already used effectively
1. **Strategy Pattern** for accounting journal-line creation.
2. **Composition Root + DI** in API startup.
3. **MVVM** baseline separation in desktop layer.

## Patterns to add next
1. **Repository + Unit of Work**
   - Replace the large context with aggregate-oriented repositories.
   - Keep transaction boundary at application-service level.

2. **Template Method or Pipeline for document flows (Sale/Purchase)**
   - Both flows share validation, amount calculations, persistence, inventory movement, and posting.
   - Extract reusable pipeline with hook points for domain differences.

3. **Domain Service for pricing/calculation**
   - Consolidate line calculations and totals so math logic has one source of truth.

4. **Specification pattern (optional)**
   - Useful once filtering/search complexity grows in parts/customers/suppliers endpoints.

---

## Critical Correctness & Reliability Risks

1. **Reference ID correctness for accounting entries**
   - `CreateJournalEntryForSale` / `CreateJournalEntryForPurchase` use `invoice.Id` / `purchase.Id` although inserted IDs are captured separately.
   - If entity IDs are not hydrated post-insert, entries may be linked with incorrect `ReferenceId`.

2. **Invoice/Purchase number collision risk**
   - Number generation is based on second precision timestamp.
   - Concurrent requests in the same second can collide.

3. **Silent exception swallowing in client image/ping operations**
   - Generic catches return null/false without logging or error context.
   - Debugging production failures becomes difficult.

4. **Monolithic data context growth risk**
   - One class currently handles many bounded contexts and SQL blocks.
   - Increases merge conflicts and accidental coupling over time.

---

## Proposed “Senior-grade” Target Architecture

### Application layer
- Command handlers / use-case services per operation (`CreateSale`, `CreatePurchase`, etc.).
- Each handler orchestrates collaborators and is unit-test focused.

### Domain layer
- Entities + value objects + domain services for pricing and payment status.
- Enforce invariants at domain boundary (no negative quantities, balanced journals).

### Infrastructure layer
- Repositories by aggregate root.
- Outbox/event logging (optional phase 2) for cross-process consistency.

### Presentation layer (WPF)
- Feature-specific VMs with dedicated API interfaces.
- Central error handling + user feedback policy.

---

## Suggested Refactor Roadmap (Incremental)

### Phase 1 (high ROI, low disruption)
1. Fix reference-ID linkage in accounting entries.
2. Introduce robust invoice number generator (DB sequence, snowflake, or GUID-based external number).
3. Add structured logging and remove silent catches.

### Phase 2 (architecture hardening)
1. Extract calculators (`InvoiceTotalsCalculator`) and payment policy.
2. Inject `IInventoryService` and repository interfaces.
3. Split `SparePartsDataContext` into focused repositories.

### Phase 3 (maintainability + team scaling)
1. Break `ManagementViewModel` into feature modules.
2. Segment API client interfaces by bounded context.
3. Add comprehensive unit tests for pricing, posting, and stock adjustment flows.

---

## Seniority Rubric (Quick Score)
- **Architecture boundaries:** 3/5
- **SOLID adherence:** 2.5/5
- **Design patterns:** 3/5 (good start, inconsistent depth)
- **Correctness under load/concurrency:** 2/5
- **Testability:** 2/5
- **Operational readiness:** 2.5/5

**Overall:** ~2.5–3.0 / 5 (solid mid-level baseline with clear path to senior-grade quality).
