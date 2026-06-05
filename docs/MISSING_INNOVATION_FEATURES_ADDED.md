# Missing Innovation Features Added

Branch: `add-missing-innovation-features`

---

## Features That Already Existed

The following features were confirmed to already exist before this branch:

- **Part Requests** with status workflow (Open → Contacted → Reserved → Fulfilled → Cancelled)
- **WhatsApp integration** (inbox, send, campaigns, message templates)
- **Owner Cockpit Dashboard** (daily P&L, profit heatmap, unpaid transactions, inventory snapshot)
- **Dashboard Quick Cards** (total parts, available, reserved, low stock, active requests, quote ready, source gaps, margin watch)
- **Smart Search** across parts, customers, orders
- **Barcode scanning + visual part recognition**
- **Inventory** with stock levels, dead stock detection, part reservations
- **Loyalty program**, warranty claims, shipments tracking
- **Business Assistant** (AI queries), Growth Intelligence
- **Part compatibility / substitutes**
- **Used car workflows** (purchase, teardown, repair prep board, wholesale)
- **Excel bulk import**
- **Accounting** with GL, journal, trial balance, SOA

---

## Missing Features Added

### 1. Sale Profit Per Line

**What it adds:** When viewing a sales invoice detail, each line item now shows:
- Cost price of the part
- Line profit (revenue − cost)
- Profit margin percentage

A summary row shows total revenue, total profit, and overall margin for the invoice.

**Why selected:** Auto parts shops in Lebanon need to track profitability per transaction. Previously the app showed sale amounts but no margin visibility per invoice. This is high-value, zero-risk (read-only enhancement).

**Files changed:**
- `src/SpareParts.Domain/Sales/SalesInvoiceLineDto.cs` — Added `CostPrice`, `LineRevenue`, `LineCost`, `LineProfit`, `ProfitMarginPercent` computed properties
- `src/SpareParts.Infrastructure/Data/Repositories/Sales/SalesRepository.cs` — Updated `itemsSql` to include `ISNULL(p.CostPrice, 0) AS CostPrice` from the Parts table
- `src/SpareParts.Web.React/wwwroot/js/views/invoices-view.js` — Added `selectedInvoice` state, `loadInvoiceDetail()` function, "View Profit" button on each row, and an expandable invoice detail panel with per-line profit display

**How to test:**
1. Open the web app → Invoices
2. Click "View Profit" on any invoice
3. A panel expands showing each line with quantity × price, and profit (if cost price is set on the part)
4. Parts with cost price = 0 show no profit line
5. The total row shows overall margin

---

### 2. Quote-to-Invoice Workflow

**What it adds:** A full quote management screen where staff can:
- Create a quote with customer name, phone, warehouse, expiry date, and line items
- Assign parts from the catalog to quote lines
- Mark quotes as Sent, Accepted, or Declined
- Convert an Accepted (or Draft) quote directly to a sales invoice with one click
- View quote detail with all lines and total

**Why selected:** Lebanese auto parts shops routinely send customers price estimates before finalizing a sale, especially over WhatsApp. There was no formal quote entity — only invoices. This fills a critical daily workflow gap without replacing anything.

**Files changed:**
- `src/SpareParts.Api/Infrastructure/QuotesMigration.cs` — New migration creating `Quotes` and `QuoteItems` tables
- `src/SpareParts.Domain/Sales/Quote.cs` — Domain entities (`Quote`, `QuoteItem`)
- `src/SpareParts.Domain/Sales/QuoteDto.cs` — DTOs: `QuoteLookupDto`, `QuoteDetailsDto`, `QuoteLineDto`, `CreateQuoteRequest`, `CreateQuoteLineRequest`, `UpdateQuoteStatusRequest`, `ConvertQuoteToInvoiceResponse`
- `src/SpareParts.Infrastructure/Services/QuotesService.cs` — New service with CRUD, status updates, convert-to-invoice
- `src/SpareParts.Api/Controllers/QuotesController.cs` — New API controller: GET, POST, PUT status, POST convert, DELETE
- `src/SpareParts.Api/Hosting/SparePartsApiComposition.cs` — Registered `QuotesMigration` in migration list, `QuotesController` in controller map, `QuotesService` in DI
- `src/SpareParts.Web.React/wwwroot/js/views/quotes-view.js` — New React view with create form, line builder, status workflow, convert-to-invoice
- `src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js` — Registered `QuotesView` under key `quotes`
- `database/schema.sql` — Added `Quotes` and `QuoteItems` DDL (idempotent)

**API endpoints added:**
```
GET    /api/quotes              ?status=Draft&search=...
GET    /api/quotes/{id}
POST   /api/quotes
PUT    /api/quotes/{id}/status
POST   /api/quotes/{id}/convert
DELETE /api/quotes/{id}         (Admin/Manager only)
```

**How to test:**
1. Apply the migration: run `QuotesMigration.EnsureApplied(factory)` or execute the DDL from `database/schema.sql`
2. Open the web app → navigate to "Quotes / Estimates"
3. Fill in customer name, warehouse, expiry date
4. Add parts from the dropdown with quantity and price
5. Click "Create Quote" → quote appears in the table with status "Draft"
6. Click "View" to open detail panel, then "Mark Sent" → status changes to "Sent"
7. Click "Mark Accepted" then "Convert to Invoice" → a sales invoice is created
8. Navigate to Invoices to see the new invoice

---

### 3. Customer Aging Report

**What it adds:** A dedicated aging screen showing all customers with outstanding balances, bucketed by:
- Current (today)
- 1–30 days overdue
- 31–60 days overdue
- 61–90 days overdue
- 90+ days overdue

Includes a totals row, count of customers overdue 30+ and 90+ days, and search by customer name or phone.

**Why selected:** Receivables management is critical for Lebanese auto parts shops where credit sales are common and collections can be challenging. Previously the app had Statement of Account per customer but no aggregate aging view to prioritize follow-up.

**Files changed:**
- `src/SpareParts.Domain/BusinessPartners/CustomerAgingDto.cs` — New DTO
- `src/SpareParts.Infrastructure/Services/CustomersService.cs` — Added `GetAging()` method with SQL aging query
- `src/SpareParts.Api/Controllers/CustomersController.cs` — Added `GET /api/customers/aging`
- `src/SpareParts.Web.React/wwwroot/js/views/customer-aging-view.js` — New React view with aging table and totals
- `src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js` — Registered `CustomerAgingView` under key `customer-aging`

**API endpoint added:**
```
GET /api/customers/aging
```

**How to test:**
1. Ensure there are sales invoices with unpaid balances (PaidAmount < TotalAmount)
2. Open the web app → navigate to "Customer Aging"
3. The table shows customers with overdue amounts in 30/60/90 day buckets
4. Overdue amounts show in red
5. Search filters by customer name or phone
6. The header cards show total customers with balances, overdue 30+, overdue 90+, and total outstanding

---

## Database Changes

### New Tables (Quotes feature)

```sql
dbo.Quotes        — Quote header (number, date, expiry, customer, warehouse, status, notes)
dbo.QuoteItems    — Quote line items (quote, part, description, qty, price, discount)
```

**Apply via:**
1. Run the SQL in `database/schema.sql` (idempotent — uses `IF OBJECT_ID IS NULL`)
2. Or call `QuotesMigration.EnsureApplied(factory)` from a startup script

### No new tables for Aging or Profit Per Line
These features use existing tables (`Transactions`, `TransactionItems`, `Parts`, `Customers`).

---

## Screens Changed

| Screen | Change |
|--------|--------|
| Invoices (`invoices-view.js`) | Added "View Profit" button + expandable profit detail panel per invoice |
| Quotes (`quotes-view.js`) | **New screen** — full quote management |
| Customer Aging (`customer-aging-view.js`) | **New screen** — 30/60/90 day aging report |
| Screen Registry (`screen-registry.js`) | Registered 2 new screens |

---

## Remaining Ideas for Future Phases

| Feature | Priority | Notes |
|---------|----------|-------|
| WhatsApp quote sharing | High | Add a "Send Quote via WhatsApp" button using the existing communications API |
| Quote PDF export | High | Generate a printable/downloadable quote PDF |
| Quote expiry notifications | Medium | Auto-notify customer before quote expires |
| Customer aging alerts on dashboard | Medium | Add "Overdue receivables" card linking to the aging screen |
| Quote templates | Low | Pre-fill common quote structures for repeat parts |
| Invoice profit history chart | Low | Show margin trends over time per part category |
| Customer credit limit enforcement | Medium | Block invoices when customer balance exceeds limit |
| Supplier aging (payables) | Medium | Mirror the customer aging for supplier payments |
| RMA / Return workflow | High | Formal return merchandise authorization with credit notes |
| Payment gateway integration | High | Stripe/PayPal for web storefront checkout |
