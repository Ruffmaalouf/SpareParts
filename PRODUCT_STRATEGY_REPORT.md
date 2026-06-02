# SpareParts — Product Strategy Report
### Senior Product Strategist, UX Expert, Software Architect & Growth Consultant Analysis

> **Date:** June 2026  
> **App:** SpareParts ERP — Automotive Aftermarket Operations Platform  
> **Stack:** .NET 8 backend, WPF Desktop (Windows), React Web, React Native Mobile, SQL Server, OpenAI, SignalR, WhatsApp  
> **Analyzed by:** Deep codebase analysis across 40+ controllers, 100+ domain entities, all frontend clients

---

## Executive Briefing (Brutally Honest)

SpareParts is already **far more sophisticated than most competitors** in the automotive aftermarket ERP space. You have features that enterprise-grade systems charge $50K/year for:

- AI-powered NLQ business assistant
- Visual part recognition via computer vision
- Accounting journal auto-posting per transaction
- Multi-warehouse, multi-currency, multi-client support
- Real-time Owner Cockpit dashboard
- Custom ad-hoc report builder
- WhatsApp/SMS campaign engine
- Loyalty program, warranty claims, dead stock intelligence
- Used car teardown & donor car hotspot analysis

**The problem is not missing features. The problem is:**

1. **Discoverability** — most powerful features are hidden behind menus; users don't know they exist
2. **Completion gaps** — several workflows start strong then break at a critical step (e.g., no return/credit note after a sale, no payment gateway for the storefront, no quote-to-invoice flow)
3. **Trust signals** — enterprise clients need 2FA, MFA, IP restriction, compliance exports, and data masking before they'll sign contracts
4. **Client-facing zero** — customers interact with nothing except a basic storefront; no portal, no payment link, no order tracking, no self-service
5. **Supplier blindspot** — suppliers are data entries, not participants; no portal, no PO confirmation, no quote submission
6. **Demo-ability** — the best features (AI, Growth Intelligence) are buried and hard to show in a 10-minute sales demo

Fix those five problems and you can charge 3–5× more and close enterprise clients.

---

## Part I: Feature Deep Dives

---

### F-01: Customer Self-Service Portal

**Problem it solves:** Customers call/WhatsApp staff to check if their invoice is paid, what their balance is, whether their part request is ready, or to download a receipt. This creates 10–30 manual interruptions per day per staff member.

**Why users will love it:** Customers get 24/7 access. Staff reclaim hours. The portal feels premium — like dealing with a professional business, not a WhatsApp-only shop.

**Business value:**
- Reduces inbound customer calls by 60–80%
- Increases customer trust and retention
- Enables online payment collection (removes cash dependency)
- Creates upsell surface (show loyalty points, promotions, substitute parts)

**Priority:** Must-have  
**Development difficulty:** Medium  
**Suggested UI placement:** Standalone portal subdomain (`portal.yourdomain.com`) linked from invoice WhatsApp messages; or tab within existing web React client for logged-in customers.

**Backend/API impact:**
- New `CustomerPortalController` or extend existing `WebCatalogController`
- New endpoints: `GET /portal/invoices`, `GET /portal/balance`, `GET /portal/requests`, `GET /portal/loyalty`, `POST /portal/pay`
- Customer JWT token with restricted scope (can only see own data)
- Restrict queries to `WHERE CustomerId = tokenSubject`

**Database impact:**
- New `CustomerPortalSessions` table (or reuse `Users` with `WebAppUser` role — already exists in the codebase)
- Add `CustomerPortalAccessEnabled` flag to `Customers` table
- Add `InvoiceDownloadLog` for audit

**Security considerations:**
- Customer token must be strictly scoped — never return data of other customers
- Rate-limit invoice download endpoint (prevent data scraping)
- Mask partial payment details to prevent oversharing
- All portal actions write to `ActivityLog`

**Acceptance criteria:**
- [ ] Customer logs in with phone/email (or existing Google/Facebook OAuth — already wired)
- [ ] Sees list of own invoices with payment status
- [ ] Can download PDF receipt
- [ ] Can see balance and statement of account
- [ ] Can see loyalty points balance and history
- [ ] Can view part request status (pending / reserved / ready)
- [ ] Can make online payment (when payment gateway integrated — F-02)
- [ ] Admin can disable portal access per customer

**Example user story:**  
*"As a garage owner who buys BMW parts weekly, I want to log into a portal and see all my unpaid invoices and download PDFs without calling the shop, so I can process my own accounts efficiently."*

**Demo script (how to sell this):**
> "Right now, your staff probably gets 20 calls a day asking 'is my invoice ready?', 'what's my balance?', 'can you send me a receipt?'. With the Customer Portal, every one of those calls disappears. Your customer gets a link in their WhatsApp payment reminder — they click it, log in with Google, see everything. They can pay online right there. Your team stops being a call center and starts selling."

---

### F-02: Online Payment Gateway Integration

**Problem it solves:** The web storefront exists but has no payment collection. Customers complete checkout and then what — call to pay cash? This breaks the entire e-commerce flow. All outstanding invoices require manual collection with no digital option.

**Why users will love it:** Customers pay at midnight from their phone. Staff stop chasing payments. Cash flow improves immediately.

**Business value:**
- Converts storefront from a catalog to a revenue channel
- Reduces Days Sales Outstanding (DSO) by 40–60%
- Enables partial payment collection digitally
- Payment confirmations auto-mark invoices as Paid in the system

**Priority:** Must-have  
**Development difficulty:** Medium  
**Suggested UI placement:**
- Storefront checkout (already exists — add payment step)
- Customer portal "Pay Now" button on unpaid invoices
- WhatsApp message deep link: "Pay your invoice of $320" → portal payment page

**Backend/API impact:**
- New `PaymentsController` with `/initiate`, `/webhook`, `/status` endpoints
- Webhook handler from payment provider updates `SalesInvoice.PaymentStatus`
- Auto-post journal entry on successful payment (accounting integration already exists — extend it)
- Support Stripe, PayPal, local gateways (abstract via `IPaymentGateway` interface)

**Database impact:**
- New `PaymentTransactions` table: `InvoiceId`, `Amount`, `Provider`, `ExternalTransactionId`, `Status`, `CreatedAt`, `ConfirmedAt`
- Add `OnlinePaymentEnabled` flag to `AppConstants`

**Security considerations:**
- Never store card data — use tokenized provider references only
- Webhook signature validation (Stripe webhooks use HMAC — verify before processing)
- Idempotency keys on payment initiation to prevent double-charging
- All payment events logged in `ActivityLog`

**Acceptance criteria:**
- [ ] Storefront checkout has a "Pay Online" button
- [ ] Clicking redirects to payment provider hosted page (or embedded Stripe Elements)
- [ ] Successful payment updates invoice status to `Paid` automatically
- [ ] Failed payment shows clear error; invoice remains `Pending`
- [ ] Payment confirmation WhatsApp/email sent automatically
- [ ] Owner cockpit shows online vs. cash collection breakdown

**Example user story:**  
*"As a shop owner, I want customers who order online to be able to pay immediately by card, so I don't have to chase payment and can ship same-day."*

**Demo script:**
> "Watch this — customer browses your catalog, adds BMW M3 brake pads to cart, checks out. System shows the total. They click 'Pay Now', enter their card, done. Your system receives the payment confirmation in real time, invoice flips to 'Paid', your warehouse gets a pick notification, and the customer gets a WhatsApp confirmation — all within 30 seconds of them clicking buy. Zero manual steps."

---

### F-03: Quote / Estimate Workflow (Pre-Invoice)

**Problem it solves:** Sales staff currently create invoices for parts that might not be confirmed. If a customer rejects the price, the invoice sits as unpaid clutter. There's no formal quotation stage — no quote number, no expiry, no conversion to invoice.

**Why users will love it:** Professional quotes build trust. Converting a confirmed quote to an invoice with one click saves time. Expired quotes auto-close, keeping the pipeline clean.

**Business value:**
- Closes more deals (customer sees a professional document, not a WhatsApp message)
- Reduces invoice clutter from unconfirmed sales
- Tracks win/loss rates on quotes (business intelligence)
- Required by many corporate procurement departments

**Priority:** Must-have  
**Development difficulty:** Medium  
**Suggested UI placement:** New tab in Sales module: "Quotes" (mirroring the Invoices tab). Add "Convert to Invoice" button on confirmed quotes.

**Backend/API impact:**
- New `QuotesController` with CRUD, `convert-to-invoice`, `expire` endpoints
- Existing `SalesInvoice` model extended with `DocumentType` enum: `Quote | Invoice`
- Alternatively, separate `SalesQuote` entity with `Items`, `ExpiryDate`, `Status` (Draft → Sent → Accepted → Converted / Expired / Rejected)
- Background job: auto-expire quotes past `ExpiryDate`

**Database impact:**
- New `SalesQuotes` table or `DocumentType` column on `SalesInvoices`
- New `QuoteStatusHistory` for audit trail
- Add `ConvertedInvoiceId` foreign key on quote

**Security considerations:**
- Quotes viewable in customer portal (read-only)
- Only Manager/Operator role can convert quote to invoice
- Quote PDF should not show internal cost data

**Acceptance criteria:**
- [ ] Create quote with line items, expiry date, notes
- [ ] Send quote via WhatsApp/email directly from app
- [ ] Customer can accept/reject via portal or reply link
- [ ] One-click "Convert to Invoice" on accepted quotes
- [ ] Quote number sequence separate from invoice sequence
- [ ] Expired quotes auto-close; notification sent to sales rep
- [ ] Quote win/loss report in Owner Cockpit

**Example user story:**  
*"As a salesperson, when a garage asks for a price on 5 parts, I want to send them a formal quote with a 48-hour expiry so they take it seriously and I can track whether they accepted or went elsewhere."*

**Demo script:**
> "Customer sends a WhatsApp asking for a price on a BMW differential seal kit. Salesperson opens Quotes, adds the parts, sets a 48-hour expiry, hits Send — customer gets a WhatsApp with a link to a professional PDF quote with your logo. Customer approves with one click. Sales rep gets notified, hits 'Convert to Invoice', done. And at the end of the month you can see your quote win rate — 70% accepted, 30% lost — and you know exactly where you're losing business."

---

### F-04: Returns, Refunds & Credit Notes

**Problem it solves:** Customers return parts (wrong part, defective, customer ordered wrong). Currently there's no formal return workflow — staff either delete the invoice (destroying audit trail) or manually adjust it (no paper trail). This is an accounting nightmare.

**Why users will love it:** Clean, auditable process. Customer gets a credit note. Stock is automatically restored. Accounting entries reverse correctly.

**Business value:**
- Accounting compliance (can't delete invoices — must reverse them)
- Stock integrity restored automatically on return
- Tracks return reasons for supplier chargeback claims
- Required for VAT/tax audits in most jurisdictions

**Priority:** Must-have  
**Development difficulty:** Medium  
**Suggested UI placement:** "Return" button on completed SalesInvoice detail. Returns module under Sales menu.

**Backend/API impact:**
- New `ReturnsController` with `POST /returns/from-invoice/{id}`, `GET /returns`
- Return processing: (1) creates `CreditNote` linked to original invoice, (2) creates reverse `StockMovement` restoring quantity to warehouse, (3) posts reverse accounting journal entry, (4) marks items as returned
- Partial return support (return 2 of 5 items)

**Database impact:**
- New `CreditNotes` table: `OriginalInvoiceId`, `CustomerId`, `TotalAmount`, `Status`, `Reason`
- New `CreditNoteItems` table
- Add `ReturnedQuantity` on `SalesInvoiceItems`
- `ReturnReasons` lookup table (Defective, Wrong Part, Customer Error, Duplicate Order)

**Security considerations:**
- Returns require Manager approval above threshold amount (configurable in `AppConstants`)
- Return action logged in `ActivityLog` with reason
- Cannot return an item already returned
- Credit note PDF excludes internal cost data

**Acceptance criteria:**
- [ ] Open any paid/partial sales invoice and click "Return"
- [ ] Select which items to return (full or partial quantities)
- [ ] Select return reason from dropdown
- [ ] System creates credit note, reverses stock, posts reverse journal
- [ ] Customer sees credit note in portal
- [ ] Return report in sales analytics
- [ ] Returns above configurable threshold require manager approval

**Example user story:**  
*"As a shop manager, when a customer brings back a wrong-fitment part, I want to process a return that automatically restores stock and creates a credit note, without me having to manually adjust anything or call the accountant."*

---

### F-05: Physical Stocktake / Inventory Count Module

**Problem it solves:** Periodic physical stock counts are done on paper or Excel, then manually entered. Discrepancies are discovered late. Stock on-hand in the system diverges from reality over time, causing missed sales and phantom stock.

**Why users will love it:** Warehouse staff count with tablets or phones (mobile app — already exists). System instantly highlights discrepancies. Adjustments are posted automatically with reason codes.

**Business value:**
- Eliminates phantom stock (selling parts you don't have)
- Required for financial audit compliance
- Reduces stockout/overstock by 30–50%
- Real-time variance report shows shrinkage

**Priority:** Must-have  
**Development difficulty:** Medium  
**Suggested UI placement:** New "Stocktake" module under Inventory menu. Mobile app gets dedicated "Count" screen.

**Backend/API impact:**
- New `StocktakeController` with: `POST /stocktakes` (create count session), `PUT /stocktakes/{id}/items` (submit counted quantities), `POST /stocktakes/{id}/finalize` (apply adjustments)
- Finalization generates `StockMovement` records with `MovementType = Adjustment` and reason
- Optional: background job to lock stock editing during active stocktake

**Database impact:**
- New `Stocktakes` table: `WarehouseId`, `StartedAt`, `FinalizedAt`, `Status`, `CreatedByUserId`
- New `StocktakeItems` table: `PartId`, `ExpectedQty`, `CountedQty`, `Variance`, `AdjustmentReason`

**Security considerations:**
- Only Manager role can finalize stocktake (posts adjustments)
- Operators can submit counts but not finalize
- All adjustments logged with user, timestamp, reason

**Acceptance criteria:**
- [ ] Manager creates stocktake session for one or all warehouses
- [ ] System generates count sheet with expected quantities (hidden or shown — configurable)
- [ ] Staff count via mobile app (scan barcode → enter quantity)
- [ ] Real-time variance highlights show discrepancies
- [ ] Manager reviews, approves, and finalizes
- [ ] Finalization posts stock adjustment entries
- [ ] Stocktake report: variance per item, total shrinkage value, date/time

**Example user story:**  
*"As a warehouse manager doing a monthly count, I want my team to scan parts and enter quantities on their phones, so the system automatically shows me what's missing and I can post adjustments in one click instead of spending a week on spreadsheets."*

---

### F-06: Two-Factor Authentication (2FA / MFA)

**Problem it solves:** Username/password only is insufficient for a system holding financial records, customer data, and inventory. One compromised credential = full access. Enterprise clients will not sign contracts without MFA.

**Why users will love it:** Owners sleep better at night. IT departments approve procurement. Insurance companies may reduce premiums.

**Business value:**
- Enables sales to enterprise and government clients
- Reduces insider threat and credential-stuffing risk
- Compliance requirement for ISO 27001, SOC 2, GDPR
- Differentiates from competitors who don't offer it

**Priority:** Must-have  
**Development difficulty:** Low–Medium  
**Suggested UI placement:** User Profile → Security Settings → Enable 2FA. Admin → Users → Force 2FA toggle.

**Backend/API impact:**
- New `TwoFactorController` with: `POST /2fa/setup` (generate TOTP QR), `POST /2fa/verify`, `POST /2fa/disable`
- Extend `AuthController.Login` to return `RequiresMfa: true` on first factor success
- Support TOTP (Google Authenticator, Authy) as primary method
- Support WhatsApp OTP as fallback (already have WhatsApp integration — free to use)
- Add `Users.MfaEnabled`, `Users.MfaSecret` columns

**Database impact:**
- Add `MfaEnabled` (bool), `MfaSecret` (encrypted), `MfaBackupCodes` (hashed) to `Users`
- Add `MfaEnforced` to `Roles` (force all users in this role to enable MFA)

**Security considerations:**
- MFA secrets stored encrypted at rest (AES-256)
- Backup codes hashed (bcrypt), one-time use
- Brute-force protection: lock account after 5 failed MFA attempts
- Admin can force-reset MFA for a user (with audit log)
- WhatsApp OTP expires in 5 minutes

**Acceptance criteria:**
- [ ] User can enable TOTP-based 2FA from profile settings
- [ ] After password login, user prompted for 6-digit TOTP code
- [ ] Failed TOTP after 5 attempts locks account for 15 minutes
- [ ] Admin can enforce MFA for specific roles
- [ ] Backup codes downloadable at setup time
- [ ] WhatsApp OTP fallback when TOTP not available
- [ ] Login history shows 2FA method used

**Example user story:**  
*"As a business owner, I want to require all managers and accountants to use 2FA so that if someone's laptop is stolen or password is leaked, no one can access our financial records."*

---

### F-07: Scheduled & Automated Payment Reminders

**Problem it solves:** WhatsApp campaign templates exist, but there's no automated scheduling engine. Staff manually decide when to send reminders and to whom. Overdue invoices accumulate silently.

**Why users will love it:** Staff don't have to think about it. Reminders go out automatically at the right time. Customers pay faster.

**Business value:**
- Reduce average Days to Payment by 15–25 days
- Reduces bad debt (customers who forget to pay because no one reminded them)
- Frees 1–2 hours/day per staff member from manual follow-up
- Collections increase 20–40% in first month

**Priority:** Must-have  
**Development difficulty:** Low  
**Suggested UI placement:** New "Automations" section under Communications. Per-rule configuration: trigger, delay, message template, channels (WhatsApp/SMS/email).

**Backend/API impact:**
- New `AutomationRulesController`: CRUD for rules
- Background job (`IHostedService` or Hangfire/Quartz): runs on configurable schedule (e.g., every morning at 8am), evaluates rules, sends messages
- Rule conditions: invoice age > N days, payment status = Pending/Partial, customer tier = X
- Reuses existing `CommunicationsController` for actual message sending

**Database impact:**
- New `AutomationRules` table: `TriggerType`, `ConditionJson`, `DelayDays`, `TemplateId`, `Channel`, `IsActive`
- New `AutomationExecutionLog` table: `RuleId`, `CustomerId`, `InvoiceId`, `SentAt`, `Status`
- Prevent duplicate sends (check execution log before sending)

**Security considerations:**
- Only Admin/Manager can create/modify automation rules
- Unsubscribe flag on customer (opt-out of automated messages)
- Rate-limit per customer (max 1 reminder per 24 hours per invoice)

**Acceptance criteria:**
- [ ] Create rule: "If invoice is 7 days overdue, send WhatsApp reminder using template X"
- [ ] Create rule: "If invoice is 30 days overdue, send SMS reminder + assign to manager for follow-up"
- [ ] Each rule can be enabled/disabled without deleting
- [ ] Execution log shows every message sent, to whom, when, result
- [ ] Customer can be excluded from automation (VIP, dispute, etc.)
- [ ] Owner cockpit widget: "X reminders sent this week, $Y collected from reminded customers"

**Example user story:**  
*"As a manager, I want the system to automatically send a WhatsApp message 7 days after an invoice is unpaid, without me having to check the overdue list every morning."*

---

### F-08: Barcode & QR Label Printing

**Problem it solves:** Parts are received and stored but not labeled. Staff search visually for parts, causing picking errors and time waste. No barcode scanning at receiving because there's nothing to scan.

**Why users will love it:** Warehouse ops become 3× faster. Scan to find, scan to pick, scan to receive. Works with any $50 Bluetooth scanner.

**Business value:**
- Reduces picking errors by 80%
- Speeds up receiving workflow by 60%
- Enables future mobile-first warehouse operations
- Required for any client with multiple warehouse locations

**Priority:** Should-have  
**Development difficulty:** Low  
**Suggested UI placement:** Part detail page → "Print Label" button. Inventory receiving → "Print Labels for Received Parts". Bulk label printing from filtered part list.

**Backend/API impact:**
- New `LabelsController`: `GET /labels/part/{id}` (generate PDF label), `POST /labels/batch` (batch print)
- Label includes: Part code, barcode (Code128 or QR), name, category, location, price (optional)
- Use ZXing.NET for barcode generation (already .NET ecosystem)
- Return PDF (via iTextSharp/QuestPDF) or PNG for direct printing

**Database impact:**
- No schema changes — reads from existing `Parts`, `Locations` tables
- Optional: `LabelPrintLog` for audit (who printed what, when)

**Security considerations:**
- Labels should not include cost prices (only sale price or no price) — configurable
- Batch print limited to 500 labels per request to prevent abuse

**Acceptance criteria:**
- [ ] Single-part label printable from part detail in WPF and web
- [ ] Batch label printing from filtered part list
- [ ] Label includes: internal code, name, category, warehouse location, barcode, optional price
- [ ] Supports A4 sheet templates (e.g., 2×5 labels per sheet) and direct thermal label printer output (ZPL)
- [ ] QR label option that links to part's web catalog page

**Example user story:**  
*"As a warehouse worker, I want to print barcode labels for all newly received parts so I can scan them during picking instead of searching by memory."*

---

### F-09: Supplier Invoice OCR (AI-Powered Auto-Fill)

**Problem it solves:** When a purchase invoice arrives by email or photo, staff manually read and type every line item into the system. This takes 10–30 minutes per invoice and introduces errors. With 20+ supplier invoices/week, that's hours of wasted data entry.

**Why users will love it:** Upload or photograph the supplier invoice — system reads it and pre-fills the purchase order. Staff review and confirm in 30 seconds.

**Business value:**
- Saves 30+ minutes per purchase invoice
- Eliminates transcription errors (wrong prices, quantities, part codes)
- Speeds up supplier invoice processing by 5–10×
- Premium differentiator — almost no competitors in the automotive aftermarket have this

**Priority:** Should-have  
**Development difficulty:** Medium  
**Suggested UI placement:** Purchase Invoice creation screen → "Import from Document" button. Drag-and-drop invoice image/PDF.

**Backend/API impact:**
- Extend `PurchasesController` with `POST /purchases/import-from-document`
- Use OpenAI GPT-4o Vision (already integrated) to extract: supplier name, invoice date, invoice number, line items (part code, description, quantity, unit price)
- Match extracted part codes/descriptions to existing `Parts` catalog using fuzzy search
- Return structured preview for user review before creating invoice

**Database impact:**
- Add `DocumentImportSource` column to `PurchaseInvoices` (Manual | OCR | EDI)
- New `ImportedDocuments` table for storing original uploaded files and extraction results

**Security considerations:**
- Uploaded documents scanned for malicious content (MIME type validation, file size limit)
- OCR results shown for review — never auto-create invoices without human approval
- Documents stored in secure blob storage, not public URL

**Acceptance criteria:**
- [ ] Upload PDF or image of supplier invoice
- [ ] System extracts supplier, date, invoice number, line items using AI
- [ ] Extracted line items shown side-by-side with catalog matches (with confidence scores)
- [ ] User can correct mismatched parts before confirming
- [ ] Confirmed items create standard purchase invoice
- [ ] Failed extraction falls back to manual entry
- [ ] Extraction accuracy tracked (correct matches / total lines)

**Example user story:**  
*"As a purchasing manager, when I receive a PDF invoice from BMW Parts GmbH, I want to upload it and have the system extract all 40 line items automatically, so I only spend 1 minute reviewing instead of 30 minutes typing."*

**Demo script:**
> "Here's a supplier invoice we just received by email — it's a 3-page PDF with 40 parts. Watch what happens when I drag it in. [Drag PDF onto screen.] The AI reads the document — extracts the supplier name, date, and every line item. It matches each part code to our catalog, flags the two it couldn't match, and shows me the confidence score for each one. I correct those two mismatches, hit Confirm, and the purchase invoice is created. Forty items, 30 seconds."

---

### F-10: Vehicle Compatibility / OEM Fitment Integration

**Problem it solves:** Staff must memorize or manually check whether a part fits a specific car model. Customers ask "does this fit a 2019 BMW 330i?" and the answer is guesswork or a phone call. Wrong parts get sold, leading to returns and angry customers.

**Why users will love it:** Sales staff answer fitment questions instantly. Web storefront lets customers filter by their car. Reduces returns by 50%+.

**Business value:**
- Reduces wrong-part returns
- Increases web storefront conversion (customers can self-service fitment check)
- Premium feature that large parts retailers (AutoZone, etc.) pay millions for
- Customer filters their car model in storefront → only sees compatible parts

**Priority:** Should-have  
**Development difficulty:** Medium–High  
**Suggested UI placement:** Part detail → "Compatible Vehicles" tab. Storefront → "Shop by Vehicle" filter. POS → "Check Fitment" button when adding line item.

**Backend/API impact:**
- New `FitmentController` with `GET /fitment/part/{partId}`, `GET /fitment/vehicle/{carModelId}`
- Integrate TecDoc, ACES/PIES, or free equivalents (OpenParts API) for OEM fitment data
- Alternatively: import fitment CSV from supplier catalog (many suppliers include this data)
- `PartSubstitutes` entity already exists — extend it with fitment data

**Database impact:**
- New `PartFitments` table: `PartId`, `CarBrandId`, `CarModelId`, `YearFrom`, `YearTo`, `Notes`
- Already have `CarBrands` and `CarModels` tables — leverage them

**Security considerations:**
- Fitment data is read-only for all roles (no security concerns beyond normal data access)
- Bulk import of fitment data requires Manager role

**Acceptance criteria:**
- [ ] Part detail shows list of compatible vehicles (year range, model)
- [ ] Storefront "Shop by Car" filter: Select Brand → Model → Year → see only compatible parts
- [ ] POS: when adding part to invoice, system warns if customer's car (if recorded on profile) is incompatible
- [ ] Fitment data importable via CSV or TecDoc integration
- [ ] Web search returns fitment-aware results ("brake pads for 2019 BMW 330i")

**Example user story:**  
*"As a customer buying from the online store, I want to enter my car model and year so the website only shows me parts that actually fit my car, so I don't accidentally order something wrong."*

---

### F-11: Predictive Demand Forecasting (AI)

**Problem it solves:** Reorder rules currently use static min/max thresholds. They don't account for seasonality, historical sales velocity changes, or upcoming events (e.g., "summer tyre season"). Stock-outs and overstock happen because the rules don't adapt.

**Why users will love it:** The system tells you what to order before you run out — and doesn't tell you to reorder parts that historically sit for 6 months.

**Business value:**
- Reduces stockouts (lost sales) by 30–50%
- Reduces excess inventory (cash locked up in slow parts) by 20–30%
- Automatic PO generation suggestions reduce purchasing workload

**Priority:** Should-have  
**Development difficulty:** Medium–High  
**Suggested UI placement:** New "Forecasting" tab in Inventory module. Owner Cockpit widget: "Parts predicted to stock out this week". Purchasing module: AI-suggested POs.

**Backend/API impact:**
- Extend `ReorderController` with `GET /reorder/ai-forecast`
- Use sales velocity (last 30/90/180 days), seasonality (same period last year), and current stock level
- OpenAI API (already integrated) can power this with structured prompts + your historical data
- Alternatively, use ML.NET for on-device time-series forecasting (no API cost)
- Output: ranked list of parts to reorder, suggested quantities, suggested suppliers

**Database impact:**
- New `ForecastSnapshots` table: `PartId`, `ForecastDate`, `PredictedDemand`, `ConfidenceScore`, `Method`
- Historical data already exists in `StockMovements` — no new tracking needed

**Security considerations:**
- Forecast data is internal only — not exposed to customers
- Manager role required to approve AI-generated purchase orders

**Acceptance criteria:**
- [ ] Weekly forecast report: "These 15 parts will stock out within 14 days at current velocity"
- [ ] Confidence score per prediction (high/medium/low)
- [ ] One-click "Create Purchase Order" from forecast recommendation
- [ ] Seasonal adjustment visible (e.g., "This part sells 3× more in winter")
- [ ] Owner Cockpit shows forecast accuracy over time (trust calibration)
- [ ] Forecasts update automatically after each sale

**Example user story:**  
*"As a purchasing manager, I want the system to tell me every Monday which parts I need to reorder this week based on predicted demand, so I can review and confirm a purchase order without manually analyzing every SKU."*

---

### F-12: Cash Flow Forecast Dashboard

**Problem it solves:** The Owner Cockpit shows today's cash balance but not where cash will be in 30/60/90 days. Business owners make investment decisions blind — they don't know if they can afford to buy a $20K donor car next week.

**Why users will love it:** Owners can plan with confidence. They see upcoming supplier payments vs. expected customer collections. Prevents cash crisis surprises.

**Business value:**
- Prevents cash shortfalls (knowing 30 days ahead)
- Helps owners decide when to buy stock, donor cars, and equipment
- Extremely rare in competitors — major differentiator
- Investors and bank managers love this for lending decisions

**Priority:** Should-have  
**Development difficulty:** Medium  
**Suggested UI placement:** Owner Cockpit → new "Cash Flow" tab. 30/60/90 day waterfall chart.

**Backend/API impact:**
- New `CashFlowController`: `GET /cashflow/forecast?days=90`
- Inputs: current cash balance, unpaid customer invoices (expected inflows by due date), unpaid supplier invoices (expected outflows), recurring fixed costs (configurable), scheduled purchases
- Output: day-by-day or week-by-week projected balance with confidence bands

**Database impact:**
- New `RecurringExpenses` table for fixed costs (rent, salaries, utilities)
- Use existing `SalesInvoices` (payment due dates) and `PurchaseInvoices` for projection inputs

**Security considerations:**
- Cash flow data is Owner/Manager only — never visible to operators
- Projections are estimates — clearly labeled as such in UI

**Acceptance criteria:**
- [ ] Waterfall chart showing projected cash balance over 30/60/90 days
- [ ] Breakdown: inflows from customer collections, outflows from supplier payments
- [ ] Manual input for expected large expenses (configurable)
- [ ] "Danger zone" indicator when projected balance goes below minimum threshold
- [ ] Downloadable as PDF for bank/investor presentations

**Example user story:**  
*"As a business owner, I want to see whether I'll have enough cash to buy a €15,000 donor car in 3 weeks, based on what customers owe me and what I owe suppliers."*

---

### F-13: Warranty & Returns Analytics Dashboard

**Problem it solves:** Warranty claims exist in the database but there's no aggregated view. Business owners don't know which suppliers consistently send defective parts, which part categories have the highest return rates, or how much warranty costs them annually.

**Why users will love it:** Suddenly you can charge defective parts back to suppliers with data. You stop ordering from bad suppliers. You discontinue high-return part lines.

**Business value:**
- Supplier chargeback recovery: reclaim 10–30% of warranty costs
- Reduce return rate by eliminating bad suppliers/parts
- Supports purchasing decisions with quality data

**Priority:** Should-have  
**Development difficulty:** Low  
**Suggested UI placement:** Reports → "Quality & Returns" dashboard tab.

**Backend/API impact:**
- Extend `ReportBuilderController` or new `WarrantyController` endpoint
- Aggregate: claims by supplier, by part, by category, by customer, by time period
- Calculate: warranty cost as % of sales revenue, average claim resolution time

**Database impact:**
- No new tables — reads from existing `WarrantyClaims`, `SalesInvoices`, `Parts`, `Suppliers`
- Materialized view or scheduled aggregation for performance

**Acceptance criteria:**
- [ ] Top 10 suppliers by warranty claims volume and value
- [ ] Top 10 parts by return rate (returns / sales)
- [ ] Warranty cost trend (monthly, quarterly)
- [ ] Claim resolution time (days open vs. closed)
- [ ] Export to Excel for supplier chargeback documentation

---

### F-14: Advanced Role & Permission Management UI

**Problem it solves:** RBAC exists in the code and database, but the UI for managing roles is likely a basic list. Admins can't quickly audit who has access to what. Permissions drift over time — people get access they shouldn't have.

**Why users will love it:** Admins see a permission matrix — user vs. feature — in one screen. Spot over-privileged users instantly. Enterprise compliance teams demand this.

**Business value:**
- Security compliance (SOC 2, ISO 27001)
- Prevents insider threat via accidental over-permissioning
- Required talking point for enterprise sales

**Priority:** Should-have  
**Development difficulty:** Low–Medium  
**Suggested UI placement:** Admin → Security → Permission Matrix (grid view: users on Y axis, features on X axis, checkmarks)

**Backend/API impact:**
- New endpoint: `GET /roles/permission-matrix` — returns denormalized view of user → feature access
- No new data model — read from existing `Roles`, `UserRoles`, `RoleMenuAccess`

**Acceptance criteria:**
- [ ] Permission matrix grid: rows = users, columns = modules/features, cells = has access (green) / no access (grey)
- [ ] Click a cell to grant/revoke access with reason field
- [ ] Filter by user, role, or module
- [ ] Export permission matrix to Excel for compliance audit
- [ ] "Last permission change" audit trail per user

---

### F-15: Supplier Performance Dashboard

**Problem it solves:** Suppliers are evaluated by gut feel. No data on who delivers on time, who has price stability, who sends defective goods most often. Good and bad suppliers look the same in the system.

**Why users will love it:** Purchasing managers make better decisions. Bad suppliers are replaced. Price negotiations use real data.

**Business value:**
- Better supplier mix → lower costs, fewer stockouts, fewer returns
- Data-backed negotiation leverage

**Priority:** Should-have  
**Development difficulty:** Low  
**Suggested UI placement:** Purchases → Suppliers → Supplier Scorecard tab.

**Backend/API impact:**
- New `SupplierScorecardController`: aggregate metrics per supplier
- Metrics: average delivery time (PO date → stock arrival), price variance over time, return/defect rate, order fulfillment rate

**Database impact:**
- Add `ExpectedDeliveryDate` to `PurchaseInvoices` (for on-time tracking)
- All other data already available in existing tables

**Acceptance criteria:**
- [ ] Scorecard per supplier: delivery score (A–F), price stability score, quality score
- [ ] Trend charts (is supplier getting better or worse?)
- [ ] Side-by-side supplier comparison for same part category
- [ ] Auto-highlight suppliers below threshold with warning badge

---

### F-16: Intelligent Search (Semantic/NLQ Search)

**Problem it solves:** Current search likely uses exact SQL LIKE matching. Searching "bmw front absorber" won't find a part named "BMW Shock Absorber Front Left". Typos break search. No OEM number lookup across suppliers.

**Why users will love it:** Sales staff find parts in 2 seconds instead of 30. Customers on the storefront find what they need without knowing exact part names.

**Business value:**
- Faster sales → more invoices per hour per salesperson
- Higher storefront conversion rate (customers find products)
- Reduces "sorry, we don't have it" mistakes when the part exists under a different name

**Priority:** Should-have  
**Development difficulty:** Medium  
**Suggested UI placement:** Top search bar (already exists) → upgrade to semantic search. Storefront search → upgrade.

**Backend/API impact:**
- Extend `SearchController` with vector embedding search
- Use OpenAI `text-embedding-3-small` (already have OpenAI key) to embed part names, descriptions, OEM codes
- Store embeddings in database or use pgvector / SQL Server full-text search as simpler alternative
- Simple quick win: SQL Server full-text search with synonym maps (low effort, good improvement)

**Acceptance criteria:**
- [ ] Search "shock absorber bmw" finds "BMW Front Shock Absorber Left" and "BMW Rear Strut Assembly"
- [ ] OEM number lookup works even with partial match
- [ ] Typo tolerance (e.g., "absrober" → results)
- [ ] Search results ranked by: stock availability first, then match score
- [ ] "Did you mean?" suggestion on near-misses

---

### F-17: Email Integration (Invoice Delivery, Quotes, Notifications)

**Problem it solves:** The system has WhatsApp and SMS but no email. Many customers — especially corporate garage fleets and foreign clients — prefer email for formal documents. Invoices, quotes, and statements must be emailable.

**Why users will love it:** One click to send invoice to customer's inbox. Auto-send statement of account monthly. Corporate clients can process invoices directly from email.

**Business value:**
- Required for corporate/B2B clients
- Reduces printing costs
- Professional impression

**Priority:** Should-have  
**Development difficulty:** Low  
**Suggested UI placement:** Invoice → "Send by Email" button. Customer profile → email field (if not present). Automated reports → email delivery schedule.

**Backend/API impact:**
- New `EmailService` implementing `INotificationService`
- Use SMTP or SendGrid/Mailgun API
- Email templates for: invoice, quote, statement of account, payment reminder, password reset
- Reuse existing PDF generation for attachments

**Database impact:**
- Add `Email` field to `Customers` (if not present)
- New `EmailDeliveryLog` table: mirrors `OutboundMessageLogs` but for email

**Acceptance criteria:**
- [ ] "Send Invoice by Email" button on invoice detail
- [ ] Monthly statement of account auto-emailed to customer
- [ ] Email confirmation on successful online payment
- [ ] Password reset flow uses email (not just WhatsApp OTP)
- [ ] Email delivery status visible (sent/bounced/opened if using SendGrid)

---

### F-18: System Health & Monitoring Dashboard (Admin)

**Problem it solves:** When the app is slow or errors occur, admins have no visibility inside the application. They have to check server logs externally. No visibility into API response times, error rates, queue depths, or background job status.

**Why users will love it:** Admin can see "everything is green" or diagnose issues before users complain.

**Business value:**
- Reduces downtime (catch problems proactively)
- Required for SLA guarantees when selling to enterprise
- Proves professionalism to IT evaluators

**Priority:** Nice-to-have  
**Development difficulty:** Low–Medium  
**Suggested UI placement:** Admin menu → System Health (owner/admin only)

**Backend/API impact:**
- New `HealthController` with `/health`, `/health/details` endpoints
- Report: database connection status, last successful background job run, error count last 24h, API latency percentiles (p50/p95/p99), WhatsApp webhook status, OpenAI API quota remaining, pending automation jobs count

**Database impact:**
- New `SystemHealthLog` table: `Timestamp`, `Component`, `Status`, `LatencyMs`, `ErrorMessage`
- Leverage existing `ActivityLog` for error rate calculation

**Acceptance criteria:**
- [ ] Green/yellow/red status per component (DB, API, WhatsApp, AI, background jobs)
- [ ] Last 24h error count with drill-down to error log
- [ ] Background job execution history (last run, success/failure, duration)
- [ ] Alert: email/WhatsApp to admin if any component turns red

---

### F-19: Mobile-First Warehouse Operations

**Problem it solves:** The mobile app exists but is a mirror of the web dashboard. Warehouse staff need a purpose-built receive → put-away → pick → pack flow optimized for one-handed use with barcode scanning.

**Why users will love it:** Warehouse workers stop using paper. Pick accuracy increases. Receiving takes minutes instead of hours.

**Business value:**
- Reduces warehouse errors by 70%
- Speeds up order fulfillment
- Enables multi-warehouse without adding headcount

**Priority:** Should-have  
**Development difficulty:** Medium  
**Suggested UI placement:** Mobile app → dedicated "Warehouse" role home screen with large-button task tiles: Receive, Put Away, Pick, Count

**Backend/API impact:**
- New `WarehouseOperationsController` with mobile-optimized endpoints (minimal data transfer)
- `POST /warehouse/receive/{purchaseId}` — scan-to-receive
- `POST /warehouse/pick/{invoiceId}` — scan-to-pick
- `POST /warehouse/transfer` — scan-to-transfer between locations

**Acceptance criteria:**
- [ ] Receive purchase invoice items by scanning barcodes on mobile
- [ ] System confirms each scanned item against expected PO contents
- [ ] Pick list generated for warehouse staff from sales order
- [ ] Scan to confirm each picked item
- [ ] Discrepancies flagged for manager review
- [ ] Works offline with sync-on-reconnect (critical for poor-signal warehouses)

---

### F-20: Bulk Price Update Tool

**Problem it solves:** When a supplier raises prices by 5%, the purchasing manager must update 200+ part prices one by one. Or when a promotion applies to a category, each part must be edited individually. This takes hours and introduces errors.

**Why users will love it:** Category price updates take 30 seconds. Supplier cost changes propagate automatically with configurable margin rules.

**Business value:**
- Saves 2–4 hours per price update event
- Reduces pricing errors
- Enables rapid promotional pricing

**Priority:** Should-have  
**Development difficulty:** Low  
**Suggested UI placement:** Inventory → Parts → "Bulk Price Update" action (above the table)

**Backend/API impact:**
- New endpoint: `POST /parts/bulk-price-update` with filter criteria (brand, category, supplier) and adjustment type (% increase, fixed amount, set price, set margin)
- Preview mode: show before/after prices without committing
- Requires confirmation step before applying

**Database impact:**
- Add to existing `Parts` update logic
- Log price changes to new `PartPriceHistory` table (if not existing)

**Acceptance criteria:**
- [ ] Filter parts by brand, category, or supplier
- [ ] Select adjustment type: +X%, -X%, set margin, set specific price
- [ ] Preview shows: current price, new price, margin impact for each affected part
- [ ] Confirm applies changes in bulk
- [ ] Full audit log: who changed what, when, by how much

---

## Part II: Summary Matrices & Roadmaps

---

## A: Top 10 Irresistible Features

> These are the features that make clients sign contracts and users refuse to switch.

| # | Feature | Why It's Irresistible |
|---|---------|----------------------|
| 1 | **Customer Self-Service Portal** (F-01) | Clients immediately understand the value: "my customers stop calling me" |
| 2 | **Online Payment Gateway** (F-02) | Direct revenue impact — cash flow improvement is measurable within 30 days |
| 3 | **Supplier Invoice OCR** (F-09) | "Your system reads invoices" is a showstopper demo moment |
| 4 | **Automated Payment Reminders** (F-07) | Owners see collections improve in week 1 — most powerful quick ROI |
| 5 | **Quote → Invoice Workflow** (F-03) | Professionalism upgrade — closes deals with corporate clients |
| 6 | **Predictive Demand Forecasting** (F-11) | "The system tells me what to order before I run out" = magic |
| 7 | **Cash Flow Forecast Dashboard** (F-12) | Owners make better decisions — unique in the market |
| 8 | **Vehicle Compatibility Filter** (F-10) | Web storefront filter by car model drives conversion rates dramatically |
| 9 | **2FA / MFA** (F-06) | Enterprise clients cannot proceed without it — a gate-opener |
| 10 | **Returns & Credit Notes** (F-04) | Accounting compliance — required for any audited business |

---

## B: Top 10 Quick Wins (Low Effort, High Visibility)

> Build these first to show momentum and improve daily life immediately.

| # | Feature | Effort | Impact | Time to Build |
|---|---------|--------|--------|---------------|
| 1 | **Automated Payment Reminders** (F-07) | Low | High | 3–5 days |
| 2 | **Barcode/QR Label Printing** (F-08) | Low | High | 2–3 days |
| 3 | **Bulk Price Update Tool** (F-20) | Low | Medium | 2–3 days |
| 4 | **Email Integration** (F-17) | Low | Medium | 3–4 days |
| 5 | **Warranty & Returns Dashboard** (F-13) | Low | Medium | 2–3 days |
| 6 | **Supplier Performance Dashboard** (F-15) | Low | Medium | 3–4 days |
| 7 | **2FA (TOTP)** (F-06) | Low–Med | High | 4–5 days |
| 8 | **Permission Matrix UI** (F-14) | Low–Med | Medium | 3–4 days |
| 9 | **System Health Dashboard** (F-18) | Low | Medium | 2–3 days |
| 10 | **Physical Stocktake Module** (F-05) | Medium | High | 5–7 days |

---

## C: Top 10 AI Features

> Listed in recommended build order.

| # | Feature | AI Method | Business Impact |
|---|---------|-----------|----------------|
| 1 | **Supplier Invoice OCR** (F-09) | GPT-4o Vision (existing API) | Save 30+ min/invoice |
| 2 | **Predictive Demand Forecasting** (F-11) | Time-series via GPT or ML.NET | Reduce stockouts 30–50% |
| 3 | **Automated Payment Reminders** (F-07) | Rules engine + AI-personalized message tone | Reduce DSO 15–25 days |
| 4 | **Semantic Search Upgrade** (F-16) | Text embeddings (OpenAI) or SQL FTS | 3× faster part lookup |
| 5 | **Cash Flow AI Narrative** (F-12) | GPT summary of cash forecast ("You'll have a shortfall on Day 22 unless...") | Executive decision support |
| 6 | **Customer Churn Prediction** | ML: days since last order, payment delays, WhatsApp response rate | Proactive retention actions |
| 7 | **Dynamic Pricing Suggestions** | GPT analyzes velocity, competitor pricing, margin rules | Optimize revenue per part |
| 8 | **Warranty Pattern Detection** | Anomaly detection: spike in returns for specific part/supplier | Catch defect batches early |
| 9 | **Smart Part Substitute Suggestion** | Embedding similarity: when a part is out of stock, AI recommends best substitute | Never lose a sale to stockout |
| 10 | **AI-Generated WhatsApp Campaigns** | GPT drafts personalized campaign messages based on customer segments | Higher campaign engagement |

> **Note:** GPT-4o is already integrated via OpenAI key. Features 1, 3, 5, 7, 9, 10 can reuse the existing `BusinessAssistantController` infrastructure with minimal new work.

---

## D: Top 10 Dashboard & Reporting Features

| # | Dashboard | Who Uses It | Key Metrics | Missing Today? |
|---|-----------|-------------|-------------|---------------|
| 1 | **Cash Flow Forecast** | Owner | 30/60/90-day cash projection, inflows vs. outflows | Yes |
| 2 | **Sales Velocity Heatmap** | Manager | Which parts/categories sell fastest, slowest, by time of day | Partial |
| 3 | **Supplier Scorecard** | Purchasing Mgr | Delivery time, price trend, defect rate per supplier | Yes |
| 4 | **Customer Lifetime Value** | Sales Mgr | Total spend per customer, first order to now, loyalty tier | Partial |
| 5 | **Inventory Aging** | Warehouse Mgr | How long each part has sat (0–30, 30–90, 90–180, 180+ days) | Partial (dead stock) |
| 6 | **Warranty & Returns** | Quality/Finance | Return rate by part/supplier, cost of warranty | Yes |
| 7 | **WhatsApp Campaign Performance** | Marketing | Delivery rate, read rate, reply rate, conversions | Partial |
| 8 | **Daily Operations** | Supervisor | Open orders, pending POs, low stock alerts, overdue invoices, today's pickings | Yes |
| 9 | **Tax & Compliance** | Accountant | VAT collected by period, tax payable summary, deductible purchases | Yes |
| 10 | **Used Car ROI** | Owner | Acquisition cost vs. parts recovered value vs. wholesale price per donor car | Partial |

---

## E: Top 10 Admin Productivity Features

| # | Feature | Time Saved | Effort |
|---|---------|-----------|--------|
| 1 | **Bulk Price Update** (F-20) | 2–4 hrs/event | Low |
| 2 | **Supplier Invoice OCR** (F-09) | 30 min/invoice | Medium |
| 3 | **Automated Payment Reminders** (F-07) | 1–2 hrs/day | Low |
| 4 | **Physical Stocktake Module** (F-05) | 1–2 days/stocktake | Medium |
| 5 | **Barcode Label Printing** (F-08) | 30–60 min/receiving | Low |
| 6 | **Permission Matrix UI** (F-14) | 30 min per user audit | Low |
| 7 | **Quote to Invoice** (F-03) | 5 min/quote | Medium |
| 8 | **Bulk Import from Supplier CSV** | 1–3 hrs/new catalog | Medium |
| 9 | **Saved Search Filters with Pinning** | 2–5 min/day (×10 users) | Low |
| 10 | **System Health Dashboard** (F-18) | 30–60 min/incident | Low |

---

## F: Features to Avoid (Not Worth the Effort)

These look attractive but have poor ROI for your specific business and market:

| Feature | Why to Avoid |
|---------|-------------|
| **Full ERP Accounting Module (AR/AP aging automation)** | Your accounting already posts journals correctly. Building a full accounting module competes with QuickBooks/Sage which clients already use. Integrate instead. |
| **Social Media Marketing Integration** | Publishing to Instagram/Facebook from the app sounds good in demos but nobody actually uses it. Complex to maintain, low adoption. |
| **Customer-Facing Live Chat (separate from WhatsApp)** | You already have WhatsApp integration. Adding a second live chat channel fragments communication. Double the maintenance cost, half the usage. |
| **Custom Mobile App Themes** | You already have 6 themes on desktop. Adding theme customization for mobile sounds fun but users don't care. Zero business value. |
| **Built-in Shipping Carrier Integration (DHL/FedEx API)** | Your market is local automotive — customers collect in person or use local delivery. Complex API integrations for a feature most clients won't use. |
| **Subscription Billing / Recurring Invoices** | Your business model is transactional (parts sales). Recurring billing adds complexity for minimal applicable scenarios. |
| **Multi-Language UI** | Unless you're actively selling to non-Arabic/non-English markets, this is expensive to maintain and rarely earns its cost. Wait until a specific client demands it. |
| **Blockchain Audit Trail** | Marketing buzzword. Your existing `ActivityLog` + database transactions are legally sufficient and technically superior. Blockchain adds cost and complexity with zero additional legal or compliance value. |
| **AI Chatbot for Internal Help Desk** | You already have the Business Assistant (NLQ). Don't build a second chatbot for internal Q&A — it will overlap and confuse users. |
| **Full-Featured HR/Payroll Module** | Out of scope for an automotive ERP. Even if clients ask for it, they're better served integrating with a dedicated HR system via API. |

---

## G: 30-Day Implementation Roadmap

**Goal:** Ship 5 high-impact quick wins. Demonstrate momentum. Improve daily operations immediately.

### Week 1 (Days 1–7): Foundation & Security
- **Day 1–2:** Email integration (`EmailService`, invoice/quote/reminder templates)
- **Day 3–4:** Barcode/QR label printing (single + batch, A4 sheet templates)
- **Day 5–7:** 2FA implementation (TOTP setup flow, login enforcement, backup codes)

**Deliverable:** Security upgrade + two warehouse productivity tools deployed.

### Week 2 (Days 8–14): Automation
- **Day 8–10:** Automated payment reminders engine (rules CRUD + background scheduler)
- **Day 11–12:** Bulk price update tool
- **Day 13–14:** Supplier performance scorecard dashboard

**Deliverable:** Collections automation live; purchasing team gets data-driven supplier scoring.

### Week 3 (Days 15–21): Analytics & Admin
- **Day 15–16:** Warranty & returns analytics dashboard
- **Day 17–18:** Permission matrix UI upgrade (admin security view)
- **Day 19–21:** System health monitoring dashboard

**Deliverable:** Admin team has full visibility into security, quality, and system status.

### Week 4 (Days 22–30): Client-Facing
- **Day 22–25:** Physical stocktake module (with mobile app support)
- **Day 26–28:** Customer portal — Phase 1 (invoice list, PDF download, balance view)
- **Day 29–30:** Polish, test, deploy, demo prep

**Deliverable:** Customer-facing upgrade shipped. Ready for enterprise client demos.

**30-Day KPIs to measure:**
- Collections: % change in average days to payment
- Support calls: % reduction in "what's my balance?" calls
- Warehouse: picking error rate before/after barcode labels
- Security: 2FA adoption rate among manager/owner roles

---

## H: 90-Day Implementation Roadmap

**Goal:** Transform SpareParts from a great internal tool into a full-stack enterprise platform.

### Days 31–45: Revenue & Compliance Workflows
- Returns / Credit Notes workflow (F-04)
- Quote / Estimate workflow (F-03)
- VAT/Tax compliance report module
- Email invoice delivery automation

### Days 46–60: AI Features Batch 1
- Supplier Invoice OCR auto-fill (F-09)
- Semantic/NLQ search upgrade (F-16)
- Predictive demand forecasting — Phase 1 (velocity-based, no ML)
- AI-generated WhatsApp campaign drafts

### Days 61–75: Client & Supplier Expansion
- Customer portal — Phase 2 (online payment, part request tracking, loyalty points)
- Online payment gateway integration (F-02) — Stripe integration
- Supplier performance dashboard V2 (drill-down per PO)
- Bulk import from supplier catalog CSV

### Days 76–90: Advanced Operations
- Vehicle compatibility / OEM fitment (F-10) — CSV import path first
- Cash flow forecast dashboard (F-12)
- Mobile warehouse operations (receive/pick screens, F-19)
- Inventory aging dashboard

**90-Day KPIs:**
- Online payment adoption: % of invoices paid digitally
- Quote win rate tracked and visible (goal: baseline established)
- Stockout rate (goal: 20% reduction)
- Supplier invoice processing time (goal: 70% reduction with OCR)
- Customer portal activation rate (goal: 40% of customers activated)

---

## I: Feature Priority Matrix (Impact vs. Effort)

```
HIGH IMPACT
│
│  [Customer Portal]    [Payment Gateway]    [Predictive Forecast]
│  [Automated Reminders][Quote Workflow]     [Vehicle Compatibility]
│  [Supplier OCR]       [Returns Workflow]   [Cash Flow Dashboard]
│
│  [2FA]               [Stocktake Module]   [Mobile Warehouse Ops]
│  [Barcode Labels]    [Bulk Price Update]
│  [Email Integration] [Supplier Scorecard]
│  [Warranty Dashboard][Permission Matrix]  [Semantic Search]
│
│  [System Health]     [Used Car ROI Dash]  [Customer Churn AI]
│  [Label Templates]   [Dynamic Pricing AI]
│
└──────────────────────────────────────────────────────────────────
  LOW EFFORT          MEDIUM EFFORT         HIGH EFFORT

ZONE GUIDE:
┌─────────────────────────────────────┐
│ TOP-LEFT = QUICK WINS (Do First)    │  High Impact + Low Effort
│ TOP-RIGHT = STRATEGIC BETS          │  High Impact + High Effort (plan carefully)
│ BOTTOM-LEFT = FILL-INS              │  Low Impact + Low Effort (fill sprint gaps)
│ BOTTOM-RIGHT = AVOID                │  Low Impact + High Effort (never build these)
└─────────────────────────────────────┘
```

### Priority Tiers:

**Tier 1 — Do Immediately (High Impact + Low Effort):**
- Automated payment reminders
- Barcode label printing
- Email integration
- 2FA/MFA
- Warranty analytics dashboard
- Bulk price update
- Supplier scorecard
- Permission matrix UI

**Tier 2 — Plan for Next Sprint (High Impact + Medium Effort):**
- Physical stocktake module
- Quote/estimate workflow
- Returns & credit notes
- Customer portal Phase 1
- Cash flow forecast dashboard
- Semantic search upgrade

**Tier 3 — Strategic Investments (High Impact + High Effort):**
- Online payment gateway
- Supplier invoice OCR
- Vehicle compatibility / fitment
- Predictive demand forecasting
- Mobile warehouse operations
- Customer portal Phase 2 (payments)

**Tier 4 — Nice When Free (Low Impact + Low Effort):**
- System health dashboard
- Additional label templates
- Used car ROI drill-down
- More Owner Cockpit widgets

**Never Build:**
- See Section F above

---

## J: Final MVP Upgrade Package

### "Enterprise-Ready SpareParts" — The 10-Feature Package That Changes Everything

This is the minimum viable upgrade that makes SpareParts look **modern, smart, secure, and enterprise-ready** in a client demo and due diligence review.

---

**Package Name:** *SpareParts Enterprise Upgrade v2*

| # | Feature | Category | Days | Demo Impact |
|---|---------|----------|------|-------------|
| 1 | Two-Factor Authentication | Security | 5 | "We take security seriously" |
| 2 | Automated Payment Reminders | Automation | 5 | "Watch collections improve immediately" |
| 3 | Customer Self-Service Portal (Phase 1) | Client-Facing | 8 | "Your customers get 24/7 self-service" |
| 4 | Quote → Invoice Workflow | Workflow | 6 | "From quote to paid invoice in 3 clicks" |
| 5 | Returns & Credit Notes | Compliance | 6 | "Full audit trail on every transaction" |
| 6 | Barcode Label Printing | Operations | 3 | "Every part has a scannable identity" |
| 7 | Supplier Invoice OCR | AI | 8 | "Our AI reads your supplier invoices" |
| 8 | Cash Flow Forecast Dashboard | Analytics | 7 | "See your cash 90 days into the future" |
| 9 | Permission Matrix UI | Security | 4 | "Know exactly who has access to what" |
| 10 | Automated Email Delivery | Communication | 4 | "Invoices, quotes, and statements by email" |

**Total: ~56 development days (11 weeks) for one developer, ~6 weeks for two developers.**

---

### What This Package Achieves:

**Security posture:** 2FA + permission matrix + full audit logs = passes enterprise IT security review

**Compliance posture:** Returns/credit notes + journal posting already there = clean audit trail, VAT-ready

**Client experience:** Portal + email + WhatsApp + payment reminders = 10× better than any competitor in the automotive aftermarket

**Operational excellence:** Barcode labels + OCR + stocktake = warehouse errors drop 70%

**Financial clarity:** Cash flow forecast + warranty analytics + supplier scorecard = management dashboards that impress investors and bank managers

**Sales demo script for the full package:**
> *"In the morning, your supplier invoices arrive by email. You drag them in, the AI reads them and fills the purchase order. At 9am, the system automatically sent WhatsApp reminders to all customers with overdue invoices. By 10am, two customers already paid online through the customer portal. Your warehouse team is scanning barcodes to receive stock — no paperwork. Your accountant is looking at the cash flow forecast: you'll have enough to buy that donor car next week. And when your IT manager asks about security, you show them the permission matrix and the 2FA enforcement policy. That's SpareParts."*

---

## Appendix: What's Already Excellent (Don't Break It)

Before building anything new, be careful not to disturb these existing strengths:

| Existing Strength | Why It's Special |
|------------------|-----------------|
| **Accounting auto-posting** | Every sale/purchase auto-posts double-entry journals. Most competitors don't have this. Guard this carefully — it's a major differentiator. |
| **Multi-warehouse + multi-currency** | Already works. Don't introduce complexity that breaks this. |
| **WPF desktop themes** | Quirky but genuinely memorable. The AMG/M-Sport themes make demos fun. Keep them. |
| **Dead stock + Growth Intelligence** | Very few systems tell you how to *unlock* value. This is a strategic differentiator — invest in making it more visible, not replacing it. |
| **Visual part recognition (CV)** | The "photo → find part" feature is a showstopper. Make sure it's featured prominently in demos. |
| **Report builder** | An ad-hoc SQL query builder for non-technical users is incredibly powerful. Many ERP systems charge $10K/year for this alone. |
| **Used car teardown intelligence** | Donor car hotspot analysis is genuinely unique in the market. No competitor has this for automotive aftermarket at this price point. |
| **WhatsApp campaigns** | Native WhatsApp integration is a massive advantage in Middle Eastern and emerging markets. Don't dilute it by adding competing chat channels. |

---

*End of SpareParts Product Strategy Report*
*Analyzed from 40+ API controllers, 100+ domain entities, 3 frontend clients (WPF, React, React Native), full database schema*
