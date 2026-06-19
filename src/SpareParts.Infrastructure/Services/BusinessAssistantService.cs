using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    public sealed class BusinessAssistantService
    {
        private const string ActionCreateReport = "create_report";
        private const string ActionCreateUnpaidCustomersReport = "create_unpaid_customers_report";
        private const string ActionFindUnpaidCustomers = "find_unpaid_customers";
        private const string ActionSendPaymentReminder = "send_payment_reminder";
        private const string ActionDraftWhatsAppReminders = "draft_whatsapp_reminders";
        private const string ActionOpenCustomer = "open_customer";
        private const string ActionDraftPurchaseOrder = "draft_purchase_order";
        private const string ActionShowLosingParts = "show_losing_parts";
        private const string ActionShowSlowMovingParts = "show_slow_moving_parts";
        private const string ActionShowLosingCars = "show_losing_cars";
        private const string ActionCampaignDeadStockClearance = "campaign_dead_stock_clearance";
        private const string ActionCampaignBackInStock = "campaign_back_in_stock";
        private const string ActionCampaignUnpaidReminders = "campaign_unpaid_reminders";
        private const string ActionCampaignSeasonalServiceParts = "campaign_seasonal_service_parts";
        private const string ActionOpenContacts = "open_contacts";
        private const string ActionOpenPurchaseParts = "open_purchase_parts";
        private const string ActionOpenWhatsAppCampaigns = "open_whatsapp_campaigns";

        private static readonly string[] DefaultSuggestions =
        [
            "Create a report",
            "Draft purchase order",
            "Open customer",
            "Send unpaid reminder",
            "Find unpaid customers",
            "Dead stock clearance campaign",
            "Parts back in stock campaign",
            "Unpaid reminders campaign",
            "Seasonal service parts campaign",
            "Show slow-moving parts",
            "Show slow-moving BMW parts over $100 bought before March",
            "How much do I owe supplier <name>?"
        ];

        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingService _accountingService;
        private readonly ITenantContext _tenantContext;

        public BusinessAssistantService(ISqlConnectionFactory factory, AccountingService accountingService, ITenantContext tenantContext)
        {
            _factory = factory;
            _accountingService = accountingService;
            _tenantContext = tenantContext;
        }

        public BusinessAssistantResponseDto Ask(string? question)
        {
            var cleanQuestion = (question ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanQuestion))
            {
                return Unsupported("Ask a business question and I will look it up from the current company data.");
            }

            var normalized = NormalizeForMatch(cleanQuestion);

            var asksForReport = ContainsAny(normalized, "create", "build", "make", "run")
                && ContainsAny(normalized, "report", "reports");

            if (ContainsAny(normalized, "draft", "create", "prepare", "build")
                && (ContainsAll(normalized, "purchase", "order") || ContainsAny(normalized, "po", "reorder", "restock")))
            {
                return BuildDraftPurchaseOrder();
            }

            if (ContainsAny(normalized, "campaign", "campaigns", "clearance", "promote", "promotion", "blast"))
            {
                if (ContainsAny(normalized, "dead", "stale", "slow", "dormant", "clearance"))
                {
                    return BuildDeadStockClearanceCampaign();
                }

                if (ContainsAll(normalized, "back", "stock") || ContainsAny(normalized, "restocked", "arrived", "available"))
                {
                    return BuildBackInStockCampaign();
                }

                if (ContainsAny(normalized, "unpaid", "outstanding", "receivable", "debt", "reminder", "reminders"))
                {
                    return BuildUnpaidReminderCampaign();
                }

                if (ContainsAny(normalized, "seasonal", "service", "maintenance", "winter", "summer"))
                {
                    return BuildSeasonalServicePartsCampaign();
                }
            }

            if (asksForReport
                && ContainsAny(normalized, "slow", "stale", "dead")
                && ContainsAny(normalized, "part", "parts", "moving", "stock"))
            {
                return BuildSlowMovingParts(cleanQuestion);
            }

            if (asksForReport
                && ContainsAny(normalized, "unpaid", "outstanding", "receivable", "receivables", "debt", "debts")
                && ContainsAny(normalized, "customer", "customers", "client", "clients"))
            {
                return BuildUnpaidCustomers(asReport: true);
            }

            if (asksForReport
                && ContainsAny(normalized, "losing", "loss", "unprofitable")
                && ContainsAny(normalized, "part", "parts", "stock"))
            {
                return BuildLosingParts();
            }

            if (asksForReport
                && ContainsAny(normalized, "losing", "loss", "unprofitable")
                && ContainsAny(normalized, "car", "cars"))
            {
                return BuildLosingCars();
            }

            if (asksForReport)
            {
                return BuildReportLauncher();
            }

            if (ContainsAny(normalized, "send", "draft", "write", "prepare")
                && ContainsAny(normalized, "reminder", "reminders")
                && ContainsAny(normalized, "customer", "customers", "client", "clients", "whatsapp", "message", "messages"))
            {
                return BuildWhatsAppReminderDrafts(intent: "send_payment_reminder");
            }

            if (ContainsAny(normalized, "draft", "write", "prepare") && ContainsAny(normalized, "whatsapp", "message", "messages", "reminder", "reminders"))
            {
                return BuildWhatsAppReminderDrafts();
            }

            if (ContainsAny(normalized, "open", "show", "find") && ContainsAny(normalized, "customer", "customers", "client", "clients"))
            {
                return BuildOpenCustomer(cleanQuestion);
            }

            if (ContainsAny(normalized, "unpaid", "outstanding", "receivable", "receivables", "debt", "debts") && ContainsAny(normalized, "customer", "customers", "client", "clients"))
            {
                return BuildUnpaidCustomers();
            }

            if (ContainsAny(normalized, "losing", "loss", "unprofitable") && ContainsAny(normalized, "part", "parts", "stock"))
            {
                return BuildLosingParts();
            }

            if (ContainsAll(normalized, "trial", "balance"))
            {
                return BuildTrialBalance(cleanQuestion);
            }

            if (ContainsAny(normalized, "slow", "stale", "dead") && ContainsAny(normalized, "part", "parts", "moving", "stock"))
            {
                return BuildSlowMovingParts(cleanQuestion);
            }

            if (ContainsAny(normalized, "losing", "loss", "unprofitable") && ContainsAny(normalized, "car", "cars"))
            {
                return BuildLosingCars();
            }

            if (ContainsAll(normalized, "supplier", "most") && ContainsAny(normalized, "paid", "pay", "payment", "payments"))
            {
                return BuildTopPaidSupplier(cleanQuestion);
            }

            if (ContainsAny(normalized, "owe", "owed", "owing", "payable") && ContainsAny(normalized, "supplier", "suppliers"))
            {
                return BuildSupplierBalance(cleanQuestion);
            }

            return Unsupported("I can create quick reports, send reminder drafts, open customer summaries, draft purchase orders, suggest WhatsApp campaigns, answer supplier balances, and build natural-language stock reports.");
        }

        public BusinessAssistantResponseDto RunAction(BusinessAssistantActionRequest? request)
        {
            var actionId = (request?.ActionId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return Unsupported("Choose an assistant action to run.");
            }

            return actionId switch
            {
                ActionCreateReport => BuildReportLauncher(),
                ActionCreateUnpaidCustomersReport => BuildUnpaidCustomers(asReport: true),
                ActionFindUnpaidCustomers => BuildUnpaidCustomers(),
                ActionSendPaymentReminder => BuildWhatsAppReminderDrafts(GetPayloadInt(request, "customerId"), "send_payment_reminder"),
                ActionDraftWhatsAppReminders => BuildWhatsAppReminderDrafts(),
                ActionOpenCustomer => BuildOpenCustomer(GetPayloadInt(request, "customerId")),
                ActionDraftPurchaseOrder => BuildDraftPurchaseOrder(),
                ActionShowLosingParts => BuildLosingParts(),
                ActionShowSlowMovingParts => BuildSlowMovingParts(),
                ActionShowLosingCars => BuildLosingCars(),
                ActionCampaignDeadStockClearance => BuildDeadStockClearanceCampaign(),
                ActionCampaignBackInStock => BuildBackInStockCampaign(),
                ActionCampaignUnpaidReminders => BuildUnpaidReminderCampaign(),
                ActionCampaignSeasonalServiceParts => BuildSeasonalServicePartsCampaign(),
                ActionOpenContacts => BuildNavigationHint("open_contacts", "Open the Contacts workspace to review customer records.", "contacts"),
                ActionOpenPurchaseParts => BuildNavigationHint("open_purchase_parts", "Open Part Purchases to turn the draft into a purchase invoice.", "purchase-parts"),
                ActionOpenWhatsAppCampaigns => BuildNavigationHint("open_whatsapp_campaigns", "Open the WhatsApp campaign builder to preview or send this campaign.", "whatsapp"),
                _ => Unsupported($"I do not recognize the action '{actionId}'.")
            };
        }

        private BusinessAssistantResponseDto BuildReportLauncher()
            => BuildResponse(
                "action_report_launcher",
                true,
                "Choose the report or operating action you want me to run.",
                new[]
                {
                    new BusinessAssistantInsightDto
                    {
                        Label = "Unpaid customers",
                        Value = "Report",
                        Detail = "Creates a quick receivables report grouped by customer.",
                        Severity = "Warning"
                    },
                    new BusinessAssistantInsightDto
                    {
                        Label = "Parts losing money",
                        Value = "Report",
                        Detail = "Finds parts where sold revenue is below estimated cost.",
                        Severity = "Warning"
                    },
                    new BusinessAssistantInsightDto
                    {
                        Label = "WhatsApp reminders",
                        Value = "Draft",
                        Detail = "Prepares reminder text for the oldest unpaid customer invoices.",
                        Severity = "Neutral"
                    }
                },
                ReportActions());

        private BusinessAssistantResponseDto BuildOpenCustomer(string question)
        {
            using var connection = _factory.CreateConnection();
            var rows = LoadCustomerAssistantRows(connection, null, 250, _tenantContext.TenantId);
            var customer = FindCustomer(question, rows) ?? rows
                .OrderByDescending(row => row.OpenBalance)
                .ThenByDescending(row => row.LastSaleAt)
                .FirstOrDefault();

            if (customer == null)
            {
                return BuildResponse(
                    "open_customer",
                    true,
                    "I could not find any customer records to open.",
                    [],
                    OpenCustomerActions());
            }

            return BuildOpenCustomerResponse(customer);
        }

        private BusinessAssistantResponseDto BuildOpenCustomer(int? customerId)
        {
            if (customerId == null)
            {
                return BuildOpenCustomer(string.Empty);
            }

            using var connection = _factory.CreateConnection();
            var customer = LoadCustomerAssistantRows(connection, customerId, 1, _tenantContext.TenantId).FirstOrDefault();
            if (customer == null)
            {
                return BuildResponse(
                    "open_customer",
                    true,
                    $"I could not find customer #{customerId.Value}.",
                    [],
                    OpenCustomerActions());
            }

            return BuildOpenCustomerResponse(customer);
        }

        private BusinessAssistantResponseDto BuildOpenCustomerResponse(CustomerAssistantRow customer)
        {
            var balance = customer.OpenBalance;
            var answer = balance > 0
                ? $"Opened {customer.CustomerName}: open balance {Money(balance, "USD")}, phone {FormatPhone(customer.Phone)}."
                : $"Opened {customer.CustomerName}: no open customer balance found, phone {FormatPhone(customer.Phone)}.";

            return BuildResponse(
                "open_customer",
                true,
                answer,
                new[]
                {
                    new BusinessAssistantInsightDto
                    {
                        Label = "Contact",
                        Value = FormatPhone(customer.Phone),
                        Detail = customer.CustomerId > 0 ? $"Customer ID #{customer.CustomerId}." : "Customer record.",
                        Severity = string.IsNullOrWhiteSpace(customer.Phone) ? "Warning" : "Good"
                    },
                    new BusinessAssistantInsightDto
                    {
                        Label = "Open balance",
                        Value = Money(balance, "USD"),
                        Detail = $"{customer.OpenInvoiceCount:N0} open invoice(s). Opening balance {Money(customer.OpeningBalance, "USD")}.",
                        Severity = balance > 0 ? "Warning" : "Good"
                    },
                    new BusinessAssistantInsightDto
                    {
                        Label = "Sales activity",
                        Value = $"{customer.SaleCount:N0} sale(s)",
                        Detail = $"Total sales {Money(customer.TotalSales, "USD")}. Last sale: {FormatShortDate(customer.LastSaleAt)}.",
                        Severity = "Neutral"
                    }
                },
                OpenCustomerActions(customer.CustomerId, customer.CustomerName));
        }

        private BusinessAssistantResponseDto BuildDraftPurchaseOrder()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<PurchaseOrderDraftRow>(
                @"WITH StockByPart AS
                  (
                      SELECT PartId,
                             SUM(ISNULL(Quantity, 0)) AS OnHand,
                             SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                      FROM dbo.Stock
                      WHERE (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  )
                  SELECT TOP (10)
                         p.Id AS PartId,
                         p.InternalCode,
                         p.Name AS PartName,
                         ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                         p.CostPrice,
                         p.SalePrice,
                         p.MinStock,
                         ISNULL(st.OnHand, 0) AS OnHand,
                         ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
                         CASE
                             WHEN p.MinStock - ISNULL(st.AvailableQuantity, 0) > 0 THEN p.MinStock - ISNULL(st.AvailableQuantity, 0)
                             ELSE 1
                         END AS SuggestedQuantity
                  FROM dbo.Parts p
                  LEFT JOIN StockByPart st ON st.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND p.MinStock > 0
                    AND ISNULL(st.AvailableQuantity, 0) <= p.MinStock
                    AND (@TenantId = 0 OR p.TenantId = @TenantId)
                  ORDER BY
                    p.MinStock - ISNULL(st.AvailableQuantity, 0) DESC,
                    ISNULL(st.AvailableQuantity, 0),
                    p.Name;",
                new { TenantId = _tenantContext.TenantId }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "draft_purchase_order",
                    true,
                    "No low-stock parts currently need a purchase order draft.",
                    [],
                    PurchaseOrderActions());
            }

            var totalUnits = rows.Sum(row => row.SuggestedQuantity);
            var estimatedCost = rows.Sum(row => row.SuggestedQuantity * row.CostPrice);
            return BuildResponse(
                "draft_purchase_order",
                true,
                $"Drafted a purchase order shortlist with {rows.Count:N0} low-stock part(s), {totalUnits:N0} suggested unit(s), estimated at {Money(estimatedCost, rows[0].Currency)}.",
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = PartLabel(row.InternalCode, row.PartName),
                    Value = $"{row.SuggestedQuantity:N0} unit(s)",
                    Detail = $"Available {row.AvailableQuantity:N0}, min stock {row.MinStock:N0}. Cost {Money(row.CostPrice, row.Currency)}, sale {Money(row.SalePrice, row.Currency)}.",
                    Severity = row.AvailableQuantity <= 0 ? "Warning" : "Neutral"
                }),
                PurchaseOrderActions());
        }

        private BusinessAssistantResponseDto BuildDeadStockClearanceCampaign()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<CampaignPartRow>(
                @"WITH SalesByPart AS
                  (
                      SELECT ti.PartId,
                             MAX(t.TransactionDate) AS LastSoldAt,
                             SUM(CASE WHEN t.TransactionDate >= @RecentCutoff THEN ABS(ISNULL(ti.Quantity, 0)) ELSE 0 END) AS SoldQuantityLast90
                      FROM dbo.TransactionItems ti
                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      WHERE tt.TypeKey = @SaleType
                        AND ti.PartId IS NOT NULL
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                      GROUP BY ti.PartId
                  ),
                  StockByPart AS
                  (
                      SELECT PartId,
                             SUM(ISNULL(Quantity, 0)) AS OnHand,
                             SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                      FROM dbo.Stock
                      WHERE (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  ),
                  ReceiptByPart AS
                  (
                      SELECT PartId,
                             MAX(CreatedAt) AS LastReceivedAt
                      FROM dbo.StockMovements
                      WHERE Quantity > 0
                        AND (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  )
                  SELECT TOP (8)
                         p.Id AS PartId,
                         p.InternalCode,
                         p.Name AS PartName,
                         p.OEMNumber AS OemNumber,
                         ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                         p.SalePrice,
                         ISNULL(st.OnHand, 0) AS OnHand,
                         ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
                         ISNULL(s.SoldQuantityLast90, 0) AS SoldQuantityLast90,
                         s.LastSoldAt,
                         r.LastReceivedAt,
                         DATEDIFF(DAY, COALESCE(s.LastSoldAt, r.LastReceivedAt, p.CreatedAt), @Today) AS AgeDays
                  FROM dbo.Parts p
                  LEFT JOIN StockByPart st ON st.PartId = p.Id
                  LEFT JOIN SalesByPart s ON s.PartId = p.Id
                  LEFT JOIN ReceiptByPart r ON r.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND ISNULL(st.OnHand, 0) > 0
                    AND DATEDIFF(DAY, COALESCE(s.LastSoldAt, r.LastReceivedAt, p.CreatedAt), @Today) >= 90
                    AND (@TenantId = 0 OR p.TenantId = @TenantId)
                  ORDER BY AgeDays DESC, ISNULL(st.OnHand, 0) DESC, p.Name;",
                new
                {
                    Today = DateTime.Today,
                    RecentCutoff = DateTime.Today.AddDays(-90),
                    SaleType = TransactionTypeKeys.Sale,
                    TenantId = _tenantContext.TenantId
                }).ToList();

            return BuildCampaignResponse(
                "campaign_dead_stock_clearance",
                "Dead stock clearance",
                rows,
                "Prepared a dead stock clearance campaign draft.",
                row => $"Dormant {row.AgeDays:N0} day(s), {row.OnHand:N0} on hand, {row.SoldQuantityLast90:N0} sold in 90 days. Feature OEM {FormatOem(row.OemNumber)} at {Money(row.SalePrice, row.Currency)}.",
                "Warning",
                ActionCampaignDeadStockClearance);
        }

        private BusinessAssistantResponseDto BuildBackInStockCampaign()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<CampaignPartRow>(
                @"WITH StockByPart AS
                  (
                      SELECT PartId,
                             SUM(ISNULL(Quantity, 0)) AS OnHand,
                             SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                      FROM dbo.Stock
                      WHERE (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  ),
                  ReceiptByPart AS
                  (
                      SELECT PartId,
                             MAX(CreatedAt) AS LastReceivedAt
                      FROM dbo.StockMovements
                      WHERE Quantity > 0
                        AND (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  )
                  SELECT TOP (8)
                         p.Id AS PartId,
                         p.InternalCode,
                         p.Name AS PartName,
                         p.OEMNumber AS OemNumber,
                         ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                         p.SalePrice,
                         ISNULL(st.OnHand, 0) AS OnHand,
                         ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
                         CAST(0 AS DECIMAL(19, 4)) AS SoldQuantityLast90,
                         CAST(NULL AS DATETIME2) AS LastSoldAt,
                         r.LastReceivedAt,
                         DATEDIFF(DAY, r.LastReceivedAt, @Today) AS AgeDays
                  FROM dbo.Parts p
                  INNER JOIN ReceiptByPart r ON r.PartId = p.Id
                  LEFT JOIN StockByPart st ON st.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND ISNULL(st.AvailableQuantity, 0) > 0
                    AND r.LastReceivedAt >= @Since
                    AND (@TenantId = 0 OR p.TenantId = @TenantId)
                  ORDER BY r.LastReceivedAt DESC, ISNULL(st.AvailableQuantity, 0) DESC, p.Name;",
                new
                {
                    Today = DateTime.Today,
                    Since = DateTime.Today.AddDays(-30),
                    TenantId = _tenantContext.TenantId
                }).ToList();

            return BuildCampaignResponse(
                "campaign_back_in_stock",
                "Parts back in stock",
                rows,
                "Prepared a parts back in stock campaign draft.",
                row => $"Received {FormatShortDate(row.LastReceivedAt)}, available {row.AvailableQuantity:N0}. Promote {PartLabel(row.InternalCode, row.PartName)} at {Money(row.SalePrice, row.Currency)}.",
                "Good",
                ActionCampaignBackInStock);
        }

        private BusinessAssistantResponseDto BuildUnpaidReminderCampaign()
        {
            var response = BuildWhatsAppReminderDrafts(intent: "campaign_unpaid_reminders");
            response.Answer = response.Insights.Count == 0
                ? "No unpaid customers were found for an unpaid reminders campaign."
                : $"Prepared an unpaid reminders campaign draft with {response.Insights.Count:N0} reminder message(s).";
            response.Actions = CampaignActions(ActionCampaignUnpaidReminders, "Unpaid reminders").ToList();
            return response;
        }

        private BusinessAssistantResponseDto BuildSeasonalServicePartsCampaign()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<CampaignPartRow>(
                @"WITH StockByPart AS
                  (
                      SELECT PartId,
                             SUM(ISNULL(Quantity, 0)) AS OnHand,
                             SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                      FROM dbo.Stock
                      WHERE (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  )
                  SELECT TOP (8)
                         p.Id AS PartId,
                         p.InternalCode,
                         p.Name AS PartName,
                         p.OEMNumber AS OemNumber,
                         ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                         p.SalePrice,
                         ISNULL(st.OnHand, 0) AS OnHand,
                         ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
                         CAST(0 AS DECIMAL(19, 4)) AS SoldQuantityLast90,
                         CAST(NULL AS DATETIME2) AS LastSoldAt,
                         CAST(NULL AS DATETIME2) AS LastReceivedAt,
                         0 AS AgeDays
                  FROM dbo.Parts p
                  LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId AND (@TenantId = 0 OR c.TenantId = @TenantId)
                  LEFT JOIN StockByPart st ON st.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND ISNULL(st.AvailableQuantity, 0) > 0
                    AND (@TenantId = 0 OR p.TenantId = @TenantId)
                    AND
                    (
                        LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%oil%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%filter%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%brake%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%battery%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%wiper%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%spark%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%coolant%'
                        OR LOWER(CONCAT(p.Name, N' ', p.OEMNumber, N' ', p.Notes, N' ', c.Name)) LIKE N'%ac%'
                    )
                  ORDER BY ISNULL(st.AvailableQuantity, 0) DESC, p.Name;",
                new { TenantId = _tenantContext.TenantId }).ToList();

            return BuildCampaignResponse(
                "campaign_seasonal_service_parts",
                "Seasonal service parts",
                rows,
                "Prepared a seasonal service parts campaign draft.",
                row => $"Available {row.AvailableQuantity:N0}, OEM {FormatOem(row.OemNumber)}, price {Money(row.SalePrice, row.Currency)}. Use this for maintenance/service reminders.",
                "Good",
                ActionCampaignSeasonalServiceParts);
        }

        private BusinessAssistantResponseDto BuildCampaignResponse(
            string intent,
            string campaignName,
            IReadOnlyList<CampaignPartRow> rows,
            string answer,
            Func<CampaignPartRow, string> detailFactory,
            string severity,
            string refreshActionId)
        {
            if (rows.Count == 0)
            {
                return BuildResponse(
                    intent,
                    true,
                    $"No candidates were found for a {campaignName.ToLowerInvariant()} campaign.",
                    [],
                    CampaignActions(refreshActionId, campaignName));
            }

            return BuildResponse(
                intent,
                true,
                $"{answer} I found {rows.Count:N0} candidate part(s) to feature.",
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = PartLabel(row.InternalCode, row.PartName),
                    Value = $"{row.AvailableQuantity:N0} available",
                    Detail = detailFactory(row),
                    Severity = severity
                }),
                CampaignActions(refreshActionId, campaignName));
        }

        private BusinessAssistantResponseDto BuildUnpaidCustomers(bool asReport = false)
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<UnpaidCustomerRow>(
                @"WITH OpenSales AS
                  (
                      SELECT
                          t.ReferenceId AS SalesInvoiceId,
                          t.TransactionNumber,
                          t.TransactionDate,
                          c.Id AS CustomerId,
                          COALESCE(NULLIF(c.Name, N''), N'Unassigned customer') AS CustomerName,
                          c.Phone,
                          ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CurrencyCode,
                          COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0) AS DisplayTotalAmount,
                          COALESCE(NULLIF(t.PaidCounterAmount, 0), t.PaidAmount, 0) AS DisplayPaidAmount
                      FROM dbo.Transactions t
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      LEFT JOIN dbo.Customers c ON c.Id = t.CustomerId AND (@TenantId = 0 OR c.TenantId = @TenantId)
                      WHERE tt.TypeKey = @SaleType
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                  )
                  SELECT TOP (10)
                         CustomerId,
                         CustomerName,
                         Phone,
                         CurrencyCode,
                         SUM(DisplayTotalAmount - DisplayPaidAmount) AS RemainingAmount,
                         SUM(DisplayTotalAmount) AS TotalAmount,
                         SUM(DisplayPaidAmount) AS PaidAmount,
                         COUNT(1) AS InvoiceCount,
                         MIN(TransactionDate) AS OldestInvoiceDate,
                         MAX(TransactionDate) AS LastInvoiceDate
                  FROM OpenSales
                  WHERE DisplayTotalAmount > DisplayPaidAmount
                  GROUP BY CustomerId, CustomerName, Phone, CurrencyCode
                  ORDER BY RemainingAmount DESC, OldestInvoiceDate;",
                new { SaleType = TransactionTypeKeys.Sale, TenantId = _tenantContext.TenantId }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "unpaid_customers",
                    true,
                    "No unpaid customer invoices were found.",
                    [],
                    CustomerDebtActions());
            }

            var currencies = rows.Select(row => NormalizeCurrency(row.CurrencyCode)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var totalText = currencies.Count == 1
                ? Money(rows.Sum(row => row.RemainingAmount), currencies[0])
                : $"{rows.Sum(row => row.RemainingAmount).ToString("N2", CultureInfo.CurrentCulture)} mixed currency";
            var answer = asReport
                ? $"Created unpaid customers report with {rows.Count:N0} customer balance row(s), totaling {totalText}."
                : $"I found {rows.Count:N0} customer balance row(s) with unpaid sales, totaling {totalText}.";

            return BuildResponse(
                asReport ? "unpaid_customers_report" : "unpaid_customers",
                true,
                answer,
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = row.CustomerName,
                    Value = Money(row.RemainingAmount, row.CurrencyCode),
                    Detail = $"{row.InvoiceCount:N0} unpaid invoice(s). Oldest: {FormatShortDate(row.OldestInvoiceDate)}. Phone: {FormatPhone(row.Phone)}.",
                    Severity = "Warning"
                }),
                CustomerDebtActions(rows[0].CustomerId, rows[0].CustomerName));
        }

        private BusinessAssistantResponseDto BuildWhatsAppReminderDrafts(int? customerId = null, string intent = "draft_whatsapp_reminders")
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<PaymentReminderDraftRow>(
                @"WITH OpenSales AS
                  (
                      SELECT
                          t.ReferenceId AS SalesInvoiceId,
                          t.TransactionNumber,
                          t.TransactionDate,
                          c.Id AS CustomerId,
                          COALESCE(NULLIF(c.Name, N''), N'Customer') AS CustomerName,
                          c.Phone,
                          ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CurrencyCode,
                          COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0) AS DisplayTotalAmount,
                          COALESCE(NULLIF(t.PaidCounterAmount, 0), t.PaidAmount, 0) AS DisplayPaidAmount
                      FROM dbo.Transactions t
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      LEFT JOIN dbo.Customers c ON c.Id = t.CustomerId AND (@TenantId = 0 OR c.TenantId = @TenantId)
                      WHERE tt.TypeKey = @SaleType
                        AND (@CustomerId IS NULL OR c.Id = @CustomerId)
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                  )
                  SELECT TOP (6)
                         SalesInvoiceId,
                         TransactionNumber,
                         TransactionDate,
                         CustomerId,
                         CustomerName,
                         Phone,
                         CurrencyCode,
                         DisplayTotalAmount AS TotalAmount,
                         DisplayPaidAmount AS PaidAmount,
                         DisplayTotalAmount - DisplayPaidAmount AS RemainingAmount
                  FROM OpenSales
                  WHERE DisplayTotalAmount > DisplayPaidAmount
                  ORDER BY TransactionDate, RemainingAmount DESC;",
                new { SaleType = TransactionTypeKeys.Sale, CustomerId = customerId, TenantId = _tenantContext.TenantId }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    intent,
                    true,
                    customerId == null
                        ? "No unpaid customer invoices were found, so there are no reminders to draft."
                        : "No unpaid invoices were found for that customer, so there is no reminder to send.",
                    [],
                    CustomerDebtActions());
            }

            var missingPhones = rows.Count(row => string.IsNullOrWhiteSpace(row.Phone));
            var answer = missingPhones == 0
                ? $"Prepared {rows.Count:N0} WhatsApp payment reminder message(s)."
                : $"Prepared {rows.Count:N0} WhatsApp payment reminder message(s); {missingPhones:N0} need a customer phone number before sending.";

            return BuildResponse(
                intent,
                true,
                answer,
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = $"{row.CustomerName} - {row.TransactionNumber}",
                    Value = FormatPhone(row.Phone),
                    Detail = BuildReminderBody(row),
                    Severity = string.IsNullOrWhiteSpace(row.Phone) ? "Warning" : "Good"
                }),
                CustomerDebtActions(rows[0].CustomerId, rows[0].CustomerName));
        }

        private BusinessAssistantResponseDto BuildLosingParts()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<LosingPartRow>(
                @"WITH LineRows AS
                  (
                      SELECT
                          p.Id AS PartId,
                          p.InternalCode,
                          p.Name AS PartName,
                          ISNULL(NULLIF(MAX(COALESCE(NULLIF(ti.CurrencyCode, N''), NULLIF(t.CounterCurrencyCode, N''))), N''), N'USD') AS CurrencyCode,
                          SUM(ABS(ISNULL(ti.Quantity, 0))) AS QuantitySold,
                          SUM(CASE
                              WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
                              ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
                          END) AS Revenue,
                          SUM(CASE
                              WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
                              ELSE COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
                          END) AS Cost,
                          MAX(t.TransactionDate) AS LastSoldAt
                      FROM dbo.Transactions t
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
                      INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
                      INNER JOIN dbo.Parts p ON p.Id = ti.PartId AND (@TenantId = 0 OR p.TenantId = @TenantId)
                      OUTER APPLY
                      (
                          SELECT SUM(ABS(CAST(sm.Quantity AS DECIMAL(19, 4))) * sm.UnitCost) AS MovementCost
                          FROM dbo.StockMovements sm
                          WHERE sm.ReferenceType = @SaleReferenceType
                            AND sm.ReferenceId = t.ReferenceId
                            AND sm.PartId = ti.PartId
                            AND (@TenantId = 0 OR sm.TenantId = @TenantId)
                      ) m
                      WHERE ti.PartId IS NOT NULL
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                      GROUP BY p.Id, p.InternalCode, p.Name
                  )
                  SELECT TOP (10)
                         PartId,
                         InternalCode,
                         PartName,
                         CurrencyCode,
                         QuantitySold,
                         Revenue,
                         Cost,
                         Revenue - Cost AS Profit,
                         LastSoldAt
                  FROM LineRows
                  WHERE Revenue - Cost < 0
                  ORDER BY Profit, PartName;",
                new
                {
                    SaleType = TransactionTypeKeys.Sale,
                    SaleReferenceType = "Sale",
                    TenantId = _tenantContext.TenantId
                }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "losing_parts",
                    true,
                    "No parts currently show a negative sales margin.",
                    [],
                    ProfitActions());
            }

            var largestLoss = rows[0];
            return BuildResponse(
                "losing_parts",
                true,
                $"I found {rows.Count:N0} part(s) with negative margin. The largest loss is {PartLabel(largestLoss)} at {Money(largestLoss.Profit, largestLoss.CurrencyCode)}.",
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = PartLabel(row),
                    Value = Money(row.Profit, row.CurrencyCode),
                    Detail = $"Revenue {Money(row.Revenue, row.CurrencyCode)} vs cost {Money(row.Cost, row.CurrencyCode)} across {row.QuantitySold:N0} sold. Last sale: {FormatShortDate(row.LastSoldAt)}.",
                    Severity = "Warning"
                }),
                ProfitActions());
        }

        private BusinessAssistantResponseDto BuildSupplierBalance(string question)
        {
            using var connection = _factory.CreateConnection();
            var suppliers = connection.Query<SupplierAssistantRow>(
                @"SELECT s.Id,
                         s.Name,
                         s.OpeningBalance,
                         s.AccountId,
                         a.Code AS AccountCode,
                         a.Name AS AccountName
                  FROM dbo.Suppliers s
                  LEFT JOIN dbo.Accounts a ON a.Id = s.AccountId AND (@TenantId = 0 OR a.TenantId = @TenantId)
                  WHERE (@TenantId = 0 OR s.TenantId = @TenantId)
                  ORDER BY s.Name;",
                new { TenantId = _tenantContext.TenantId }).ToList();

            var supplier = FindSupplier(question, suppliers);
            if (supplier == null)
            {
                var openSuppliers = GetTopOpenSuppliers(connection, _tenantContext.TenantId);
                if (openSuppliers.Count == 0)
                {
                    return BuildResponse(
                        "supplier_balance",
                        true,
                        "I could not match a supplier name, and there are no open supplier balances to suggest.",
                        []);
                }

                return BuildResponse(
                    "supplier_balance",
                    true,
                    "I could not match a supplier name. The largest open supplier balances are listed below.",
                    openSuppliers.Select(item => new BusinessAssistantInsightDto
                    {
                        Label = item.SupplierName,
                        Value = Money(item.OpenBalance, "USD"),
                        Detail = $"{item.InvoiceCount} open purchase transaction(s).",
                        Severity = item.OpenBalance > 0 ? "Warning" : "Neutral"
                    }));
            }

            var ledger = GetSupplierLedgerBalance(connection, supplier.AccountId, _tenantContext.TenantId);
            var invoiceBalance = GetSupplierOpenInvoiceBalance(connection, supplier.Id, _tenantContext.TenantId);
            var useLedger = ledger.LineCount > 0;
            var useOpeningBalance = !useLedger && invoiceBalance.InvoiceCount == 0 && supplier.OpeningBalance != 0;
            var balance = useLedger
                ? ledger.Balance
                : useOpeningBalance
                    ? supplier.OpeningBalance
                    : invoiceBalance.OpenBalance;
            var source = useLedger
                ? $"supplier ledger account {supplier.AccountCode}"
                : useOpeningBalance
                    ? "the supplier opening balance"
                    : "open purchase invoices";

            var answer = balance switch
            {
                > 0 => $"You owe {supplier.Name} {Money(balance, "USD")} based on {source}.",
                < 0 => $"{supplier.Name} has a credit balance of {Money(Math.Abs(balance), "USD")} based on {source}.",
                _ => $"There is no open amount for {supplier.Name} based on {source}."
            };

            return BuildResponse(
                "supplier_balance",
                true,
                answer,
                new[]
                {
                    new BusinessAssistantInsightDto
                    {
                        Label = supplier.Name,
                        Value = Money(balance, "USD"),
                        Detail = useLedger
                            ? $"{ledger.LineCount} journal line(s) in {supplier.AccountName}."
                            : useOpeningBalance
                                ? "No purchase transactions found; using the supplier opening balance."
                                : $"{invoiceBalance.InvoiceCount} purchase transaction(s) checked.",
                        Severity = balance > 0 ? "Warning" : "Good"
                    },
                    new BusinessAssistantInsightDto
                    {
                        Label = "Invoice open balance",
                        Value = Money(invoiceBalance.OpenBalance, "USD"),
                        Detail = "Total purchases less paid amounts for this supplier.",
                        Severity = invoiceBalance.OpenBalance > 0 ? "Warning" : "Neutral"
                    }
                });
        }

        private BusinessAssistantResponseDto BuildTopPaidSupplier(string question)
        {
            var range = ResolveDateRange(question);
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<TopPaidSupplierRow>(
                @"SELECT TOP (5)
                         s.Id AS SupplierId,
                         s.Name AS SupplierName,
                         SUM(ISNULL(t.PaidAmount, 0)) AS PaidAmount,
                         SUM(ISNULL(t.TotalBaseAmount, t.TotalAmount)) AS PurchasedAmount,
                         COUNT(1) AS TransactionCount
                  FROM dbo.Transactions t
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                  INNER JOIN dbo.Suppliers s ON s.Id = t.SupplierId AND (@TenantId = 0 OR s.TenantId = @TenantId)
                  WHERE tt.TypeKey IN @TypeKeys
                    AND (@DateFrom IS NULL OR t.TransactionDate >= @DateFrom)
                    AND (@DateTo IS NULL OR t.TransactionDate < DATEADD(DAY, 1, @DateTo))
                    AND (@TenantId = 0 OR t.TenantId = @TenantId)
                  GROUP BY s.Id, s.Name
                  HAVING SUM(ISNULL(t.PaidAmount, 0)) > 0
                  ORDER BY PaidAmount DESC, SupplierName;",
                new
                {
                    TypeKeys = new[] { TransactionTypeKeys.Purchase, TransactionTypeKeys.UsedCarPurchase },
                    range.DateFrom,
                    range.DateTo,
                    TenantId = _tenantContext.TenantId
                }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "top_paid_supplier",
                    true,
                    $"No supplier payments were found for {range.Label}.",
                    []);
            }

            var top = rows[0];
            return BuildResponse(
                "top_paid_supplier",
                true,
                $"{top.SupplierName} is the supplier you paid most for {range.Label}: {Money(top.PaidAmount, "USD")}.",
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = row.SupplierName,
                    Value = Money(row.PaidAmount, "USD"),
                    Detail = $"{row.TransactionCount} purchase transaction(s), {Money(row.PurchasedAmount, "USD")} purchased.",
                    Severity = row.SupplierId == top.SupplierId ? "Good" : "Neutral"
                }));
        }

        private BusinessAssistantResponseDto BuildLosingCars()
        {
            using var connection = _factory.CreateConnection();
            var rows = connection.Query<LosingCarRow>(
                @"WITH SalesByCar AS
                  (
                      SELECT p.UsedCarId,
                             SUM(ISNULL(ti.LineTotal, 0)) AS SalesRevenue,
                             SUM(ISNULL(ti.Quantity, 0) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0)) AS EstimatedSoldCost,
                             COUNT(DISTINCT t.Id) AS SaleCount,
                             MAX(t.TransactionDate) AS LastSaleAt
                      FROM dbo.TransactionItems ti
                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      INNER JOIN dbo.Parts p ON p.Id = ti.PartId AND (@TenantId = 0 OR p.TenantId = @TenantId)
                      WHERE tt.TypeKey = @SaleType
                        AND p.UsedCarId IS NOT NULL
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                        AND (@TenantId = 0 OR ti.TenantId = @TenantId)
                      GROUP BY p.UsedCarId
                  )
                  SELECT TOP (8)
                         uc.Id AS UsedCarId,
                         CASE
                             WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                                 CASE
                                     WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                                     ELSE cm.Name + N' (' + cm.BodyType + N')'
                                 END
                             ELSE
                                 CASE
                                     WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                                     ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                                 END
                         END + N' ' + CONVERT(NVARCHAR(4), uc.ModelYear) AS Car,
                         ISNULL(NULLIF(uc.BaseCurrencyCode, N''), N'USD') AS CurrencyCode,
                         ISNULL(uc.GrandTotalBase, 0) AS LoadedCost,
                         ISNULL(s.SalesRevenue, 0) AS SalesRevenue,
                         ISNULL(s.EstimatedSoldCost, 0) AS EstimatedSoldCost,
                         ISNULL(s.SaleCount, 0) AS SaleCount,
                         s.LastSaleAt,
                         ISNULL(s.SalesRevenue, 0) - ISNULL(uc.GrandTotalBase, 0) AS RealizedMargin
                  FROM dbo.UsedCars uc
                  INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId AND (@TenantId = 0 OR cm.TenantId = @TenantId)
                  INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId AND (@TenantId = 0 OR cb.TenantId = @TenantId)
                  LEFT JOIN SalesByCar s ON s.UsedCarId = uc.Id
                  WHERE ISNULL(s.SalesRevenue, 0) - ISNULL(uc.GrandTotalBase, 0) < 0
                    AND (@TenantId = 0 OR uc.TenantId = @TenantId)
                  ORDER BY RealizedMargin, uc.Id DESC;",
                new { SaleType = TransactionTypeKeys.Sale, TenantId = _tenantContext.TenantId }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "losing_cars",
                    true,
                    "No used cars currently show a negative realized margin.",
                    []);
            }

            return BuildResponse(
                "losing_cars",
                true,
                $"I found {rows.Count} used car(s) with negative realized margin. The largest gap is {rows[0].Car} at {Money(rows[0].RealizedMargin, rows[0].CurrencyCode)}.",
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = row.Car,
                    Value = Money(row.RealizedMargin, row.CurrencyCode),
                    Detail = $"Sales {Money(row.SalesRevenue, row.CurrencyCode)} vs loaded cost {Money(row.LoadedCost, row.CurrencyCode)}. {row.SaleCount} sale(s).",
                    Severity = "Warning"
                }));
        }

        private BusinessAssistantResponseDto BuildSlowMovingParts(string question = "")
        {
            var today = DateTime.Today;
            var cutoff = today.AddDays(-90);

            using var connection = _factory.CreateConnection();
            var filter = ExtractSlowMovingPartFilter(connection, question, _tenantContext.TenantId);
            var rows = connection.Query<SlowMovingPartRow>(
                @"WITH SalesByPart AS
                  (
                      SELECT ti.PartId,
                             MAX(t.TransactionDate) AS LastSoldAt,
                             SUM(ISNULL(ti.Quantity, 0)) AS SoldQuantityAllTime,
                             SUM(CASE WHEN t.TransactionDate >= @Cutoff THEN ISNULL(ti.Quantity, 0) ELSE 0 END) AS SoldQuantityLast90
                      FROM dbo.TransactionItems ti
                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      WHERE tt.TypeKey = @SaleType
                        AND ti.PartId IS NOT NULL
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                      GROUP BY ti.PartId
                  ),
                  StockByPart AS
                  (
                      SELECT PartId,
                             SUM(ISNULL(Quantity, 0)) AS OnHand
                      FROM dbo.Stock
                      WHERE (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  ),
                  ReceiptByPart AS
                  (
                      SELECT PartId,
                             MAX(CreatedAt) AS LastReceivedAt
                      FROM dbo.StockMovements
                      WHERE Quantity > 0
                        AND (@TenantId = 0 OR TenantId = @TenantId)
                      GROUP BY PartId
                  )
                  SELECT TOP (8)
                         p.Id AS PartId,
                         p.InternalCode,
                         p.Name AS PartName,
                         ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                         p.SalePrice,
                         b.Name AS PartBrandName,
                         cb.Name AS CarBrandName,
                         ISNULL(st.OnHand, 0) AS OnHand,
                         ISNULL(s.SoldQuantityLast90, 0) AS SoldQuantityLast90,
                         s.SoldQuantityAllTime,
                         s.LastSoldAt,
                         r.LastReceivedAt,
                         CASE WHEN s.LastSoldAt IS NULL THEN NULL ELSE DATEDIFF(DAY, s.LastSoldAt, @Today) END AS DaysSinceLastSale
                  FROM dbo.Parts p
                  LEFT JOIN dbo.Brands b ON b.Id = p.BrandId AND (@TenantId = 0 OR b.TenantId = @TenantId)
                  LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId AND (@TenantId = 0 OR uc.TenantId = @TenantId)
                  LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId AND (@TenantId = 0 OR cm.TenantId = @TenantId)
                  LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId AND (@TenantId = 0 OR cb.TenantId = @TenantId)
                  LEFT JOIN StockByPart st ON st.PartId = p.Id
                  LEFT JOIN SalesByPart s ON s.PartId = p.Id
                  LEFT JOIN ReceiptByPart r ON r.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND ISNULL(st.OnHand, 0) > 0
                    AND (@TenantId = 0 OR p.TenantId = @TenantId)
                    AND (@BrandTerm = N''
                         OR UPPER(ISNULL(b.Name, N'')) LIKE @BrandLike
                         OR UPPER(ISNULL(cb.Name, N'')) LIKE @BrandLike
                         OR UPPER(ISNULL(p.Name, N'')) LIKE @BrandLike
                         OR UPPER(ISNULL(p.Notes, N'')) LIKE @BrandLike)
                    AND (@MinSalePrice IS NULL OR p.SalePrice > @MinSalePrice)
                    AND (@PurchasedBefore IS NULL OR r.LastReceivedAt < @PurchasedBefore)
                  ORDER BY
                    CASE WHEN ISNULL(s.SoldQuantityLast90, 0) = 0 THEN 0 ELSE 1 END,
                    ISNULL(s.SoldQuantityLast90, 0),
                    CASE WHEN s.LastSoldAt IS NULL THEN 0 ELSE 1 END,
                    DaysSinceLastSale DESC,
                    ISNULL(st.OnHand, 0) DESC,
                    p.Name;",
                new
                {
                    SaleType = TransactionTypeKeys.Sale,
                    Cutoff = cutoff,
                    Today = today,
                    filter.BrandTerm,
                    BrandLike = string.IsNullOrWhiteSpace(filter.BrandTerm)
                        ? string.Empty
                        : $"%{filter.BrandTerm.Trim().ToUpperInvariant()}%",
                    filter.MinSalePrice,
                    filter.PurchasedBefore,
                    TenantId = _tenantContext.TenantId
                }).ToList();

            if (rows.Count == 0)
            {
                return BuildResponse(
                    "slow_moving_parts",
                    true,
                    filter.HasFilters
                        ? $"I did not find any on-hand slow-moving parts matching {filter.Summary.ToLowerInvariant()}."
                        : "I did not find any on-hand parts to classify as slow-moving.",
                    [],
                    SlowMovingActions());
            }

            var zeroSalesCount = rows.Count(row => row.SoldQuantityLast90 <= 0);
            var answer = zeroSalesCount > 0
                ? $"I found {zeroSalesCount} on-hand part(s) in the top results with no sales in the last 90 days."
                : "All top on-hand parts have moved in the last 90 days, but these are the slowest movers.";
            if (filter.HasFilters)
            {
                answer += $" Filters: {filter.Summary}.";
            }

            return BuildResponse(
                "slow_moving_parts",
                true,
                answer,
                rows.Select(row => new BusinessAssistantInsightDto
                {
                    Label = string.IsNullOrWhiteSpace(row.InternalCode)
                        ? row.PartName
                        : $"{row.InternalCode} - {row.PartName}",
                    Value = $"{row.OnHand:N0} on hand",
                    Detail = $"{row.SoldQuantityLast90:N0} sold in 90 days. Last sale: {FormatLastSale(row.LastSoldAt, row.DaysSinceLastSale)}. Last receipt: {FormatShortDate(row.LastReceivedAt)}. Sale price {Money(row.SalePrice, row.Currency)}.",
                    Severity = row.SoldQuantityLast90 <= 0 ? "Warning" : "Neutral"
                }),
                SlowMovingActions());
        }

        private BusinessAssistantResponseDto BuildTrialBalance(string question)
        {
            var range = ResolveDateRange(question);
            var report = _accountingService.GetTrialBalance(range.DateFrom, range.DateTo);
            var difference = report.TotalDebit - report.TotalCredit;
            var balanceState = Math.Abs(difference) < 0.01m
                ? "balanced"
                : $"out by {Money(Math.Abs(difference), report.BaseCurrencyCode)}";

            if (report.Rows.Count == 0)
            {
                return BuildResponse(
                    "trial_balance",
                    true,
                    $"No trial balance activity was found for {range.Label}.",
                    []);
            }

            var insights = report.Rows
                .OrderByDescending(row => Math.Max(row.DebitBalance, row.CreditBalance))
                .ThenBy(row => row.AccountCode)
                .Take(8)
                .Select(row => new BusinessAssistantInsightDto
                {
                    Label = row.AccountDisplay,
                    Value = row.DebitBalance >= row.CreditBalance
                        ? $"Dr {Money(row.DebitBalance, row.BaseCurrencyCode)}"
                        : $"Cr {Money(row.CreditBalance, row.BaseCurrencyCode)}",
                    Detail = $"{row.AccountType}: debits {Money(row.TotalDebit, row.BaseCurrencyCode)}, credits {Money(row.TotalCredit, row.BaseCurrencyCode)}.",
                    Severity = "Neutral"
                });

            return BuildResponse(
                "trial_balance",
                true,
                $"Trial balance for {range.Label} is {balanceState}: debits {Money(report.TotalDebit, report.BaseCurrencyCode)}, credits {Money(report.TotalCredit, report.BaseCurrencyCode)}.",
                insights);
        }

        private static IReadOnlyList<OpenSupplierRow> GetTopOpenSuppliers(System.Data.IDbConnection connection, int tenantId)
            => connection.Query<OpenSupplierRow>(
                @"SELECT TOP (5)
                         s.Id AS SupplierId,
                         s.Name AS SupplierName,
                         SUM(ISNULL(t.TotalBaseAmount, t.TotalAmount) - ISNULL(t.PaidAmount, 0)) AS OpenBalance,
                         COUNT(1) AS InvoiceCount
                  FROM dbo.Transactions t
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                  INNER JOIN dbo.Suppliers s ON s.Id = t.SupplierId AND (@TenantId = 0 OR s.TenantId = @TenantId)
                  WHERE tt.TypeKey IN @TypeKeys
                    AND (@TenantId = 0 OR t.TenantId = @TenantId)
                  GROUP BY s.Id, s.Name
                  HAVING SUM(ISNULL(t.TotalBaseAmount, t.TotalAmount) - ISNULL(t.PaidAmount, 0)) <> 0
                  ORDER BY OpenBalance DESC, SupplierName;",
                new
                {
                    TypeKeys = new[] { TransactionTypeKeys.Purchase, TransactionTypeKeys.UsedCarPurchase },
                    TenantId = tenantId
                }).ToList();

        private static SupplierBalanceRow GetSupplierLedgerBalance(System.Data.IDbConnection connection, int? accountId, int tenantId)
        {
            if (accountId == null)
            {
                return new SupplierBalanceRow();
            }

            return connection.QuerySingle<SupplierBalanceRow>(
                @"SELECT ISNULL(SUM(jl.Credit - jl.Debit), 0) AS Balance,
                         COUNT(jl.Id) AS LineCount
                  FROM dbo.JournalLines jl
                  WHERE jl.AccountId = @AccountId
                    AND (@TenantId = 0 OR jl.TenantId = @TenantId);",
                new { AccountId = accountId.Value, TenantId = tenantId });
        }

        private static OpenSupplierRow GetSupplierOpenInvoiceBalance(System.Data.IDbConnection connection, int supplierId, int tenantId)
            => connection.QuerySingle<OpenSupplierRow>(
                @"SELECT @SupplierId AS SupplierId,
                         CAST(N'' AS NVARCHAR(255)) AS SupplierName,
                         ISNULL(SUM(ISNULL(t.TotalBaseAmount, t.TotalAmount) - ISNULL(t.PaidAmount, 0)), 0) AS OpenBalance,
                         COUNT(t.Id) AS InvoiceCount
                  FROM dbo.Transactions t
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                  WHERE tt.TypeKey IN @TypeKeys
                    AND t.SupplierId = @SupplierId
                    AND (@TenantId = 0 OR t.TenantId = @TenantId);",
                new
                {
                    SupplierId = supplierId,
                    TypeKeys = new[] { TransactionTypeKeys.Purchase, TransactionTypeKeys.UsedCarPurchase },
                    TenantId = tenantId
                });

        private static SupplierAssistantRow? FindSupplier(string question, IReadOnlyList<SupplierAssistantRow> suppliers)
        {
            var normalizedQuestion = NormalizeForMatch(question);
            var directMatch = suppliers
                .Where(supplier => !string.IsNullOrWhiteSpace(supplier.Name))
                .Select(supplier => new
                {
                    Supplier = supplier,
                    NormalizedName = NormalizeForMatch(supplier.Name)
                })
                .Where(item => item.NormalizedName.Length > 0 && normalizedQuestion.Contains(item.NormalizedName, StringComparison.Ordinal))
                .OrderByDescending(item => item.NormalizedName.Length)
                .Select(item => item.Supplier)
                .FirstOrDefault();

            if (directMatch != null)
            {
                return directMatch;
            }

            var supplierTerm = ExtractSupplierTerm(question);
            if (string.IsNullOrWhiteSpace(supplierTerm))
            {
                return null;
            }

            var normalizedTerm = NormalizeForMatch(supplierTerm);
            return suppliers
                .Where(supplier => !string.IsNullOrWhiteSpace(supplier.Name))
                .Select(supplier => new
                {
                    Supplier = supplier,
                    NormalizedName = NormalizeForMatch(supplier.Name)
                })
                .Where(item => item.NormalizedName.Contains(normalizedTerm, StringComparison.Ordinal)
                               || normalizedTerm.Contains(item.NormalizedName, StringComparison.Ordinal))
                .OrderBy(item => Math.Abs(item.NormalizedName.Length - normalizedTerm.Length))
                .ThenBy(item => item.Supplier.Name)
                .Select(item => item.Supplier)
                .FirstOrDefault();
        }

        private static string ExtractSupplierTerm(string question)
        {
            var match = Regex.Match(question, @"\bsupplier\s+(?<term>.+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            var term = match.Groups["term"].Value.Trim();
            term = Regex.Replace(term, @"\b(for|please|now|today)\b", string.Empty, RegexOptions.IgnoreCase);
            return term.Trim(' ', '?', '.', ',', ':', ';', '"', '\'');
        }

        private static IReadOnlyList<CustomerAssistantRow> LoadCustomerAssistantRows(System.Data.IDbConnection connection, int? customerId, int take, int tenantId)
            => connection.Query<CustomerAssistantRow>(
                @"WITH SaleSummary AS
                  (
                      SELECT
                          t.CustomerId,
                          LastSaleAt = MAX(t.TransactionDate),
                          SaleCount = COUNT(1),
                          TotalSales = SUM(CASE WHEN t.IsReturn = 1 THEN -ABS(ISNULL(t.TotalAmount, 0)) ELSE ISNULL(t.TotalAmount, 0) END),
                          OutstandingBalance = SUM(CASE WHEN t.IsReturn = 1 THEN 0 ELSE
                              CASE WHEN ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0) > 0
                                   THEN ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0)
                                   ELSE 0
                              END
                          END),
                          OpenInvoiceCount = SUM(CASE WHEN t.IsReturn = 0 AND ISNULL(t.TotalAmount, 0) > ISNULL(t.PaidAmount, 0) THEN 1 ELSE 0 END)
                      FROM dbo.Transactions t
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
                      WHERE t.CustomerId IS NOT NULL
                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                      GROUP BY t.CustomerId
                  )
                  SELECT TOP (@Take)
                         CustomerId = c.Id,
                         CustomerName = COALESCE(NULLIF(c.Name, N''), CONCAT(N'Customer ', c.Id)),
                         c.Phone,
                         OpeningBalance = ISNULL(c.OpeningBalance, 0),
                         OpenBalance = ISNULL(c.OpeningBalance, 0) + ISNULL(s.OutstandingBalance, 0),
                         OpenInvoiceCount = ISNULL(s.OpenInvoiceCount, 0),
                         SaleCount = ISNULL(s.SaleCount, 0),
                         TotalSales = ISNULL(s.TotalSales, 0),
                         s.LastSaleAt
                  FROM dbo.Customers c
                  LEFT JOIN SaleSummary s ON s.CustomerId = c.Id
                  WHERE (@CustomerId IS NULL OR c.Id = @CustomerId)
                    AND (@TenantId = 0 OR c.TenantId = @TenantId)
                  ORDER BY OpenBalance DESC, s.LastSaleAt DESC, c.Name;",
                new
                {
                    Take = Math.Clamp(take, 1, 500),
                    CustomerId = customerId,
                    SaleType = TransactionTypeKeys.Sale,
                    TenantId = tenantId
                }).ToList();

        private static CustomerAssistantRow? FindCustomer(string question, IReadOnlyList<CustomerAssistantRow> customers)
        {
            var normalizedQuestion = NormalizeForMatch(question);
            var directMatch = customers
                .Where(customer => !string.IsNullOrWhiteSpace(customer.CustomerName))
                .Select(customer => new
                {
                    Customer = customer,
                    NormalizedName = NormalizeForMatch(customer.CustomerName)
                })
                .Where(item => item.NormalizedName.Length > 0 && normalizedQuestion.Contains(item.NormalizedName, StringComparison.Ordinal))
                .OrderByDescending(item => item.NormalizedName.Length)
                .Select(item => item.Customer)
                .FirstOrDefault();

            if (directMatch != null)
            {
                return directMatch;
            }

            var customerTerm = ExtractCustomerTerm(question);
            if (string.IsNullOrWhiteSpace(customerTerm))
            {
                return null;
            }

            var normalizedTerm = NormalizeForMatch(customerTerm);
            return customers
                .Where(customer => !string.IsNullOrWhiteSpace(customer.CustomerName))
                .Select(customer => new
                {
                    Customer = customer,
                    NormalizedName = NormalizeForMatch(customer.CustomerName)
                })
                .Where(item => item.NormalizedName.Contains(normalizedTerm, StringComparison.Ordinal)
                               || normalizedTerm.Contains(item.NormalizedName, StringComparison.Ordinal))
                .OrderBy(item => Math.Abs(item.NormalizedName.Length - normalizedTerm.Length))
                .ThenByDescending(item => item.Customer.OpenBalance)
                .Select(item => item.Customer)
                .FirstOrDefault();
        }

        private static string ExtractCustomerTerm(string question)
        {
            var term = Regex.Replace(question, @"\b(open|show|find|customer|customers|client|clients|profile|account|please|now|today)\b", " ", RegexOptions.IgnoreCase);
            term = Regex.Replace(term, @"\s+", " ");
            return term.Trim(' ', '?', '.', ',', ':', ';', '"', '\'');
        }

        private static SlowMovingPartFilter ExtractSlowMovingPartFilter(System.Data.IDbConnection connection, string question, int tenantId)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new SlowMovingPartFilter(string.Empty, null, null);
            }

            return new SlowMovingPartFilter(
                ExtractKnownBrandTerm(connection, question, tenantId),
                ExtractMinimumAmount(question),
                ExtractBeforeMonthDate(question));
        }

        private static string ExtractKnownBrandTerm(System.Data.IDbConnection connection, string question, int tenantId)
        {
            var normalizedQuestion = NormalizeForMatch(question);
            var brands = connection.Query<string>(
                    @"SELECT Name FROM dbo.Brands WHERE NULLIF(LTRIM(RTRIM(Name)), N'') IS NOT NULL AND (@TenantId = 0 OR TenantId = @TenantId)
                      UNION
                      SELECT Name FROM dbo.CarBrands WHERE NULLIF(LTRIM(RTRIM(Name)), N'') IS NOT NULL AND (@TenantId = 0 OR TenantId = @TenantId);",
                    new { TenantId = tenantId })
                .Concat(CommonVehicleBrands)
                .Where(brand => !string.IsNullOrWhiteSpace(brand))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(brand => new
                {
                    Name = brand.Trim(),
                    NormalizedName = NormalizeForMatch(brand)
                })
                .Where(item => item.NormalizedName.Length >= 2 && normalizedQuestion.Contains(item.NormalizedName, StringComparison.Ordinal))
                .OrderByDescending(item => item.NormalizedName.Length)
                .FirstOrDefault();

            return brands?.Name ?? string.Empty;
        }

        private static decimal? ExtractMinimumAmount(string question)
        {
            var match = Regex.Match(question, @"(?:(?:over|above|more than|greater than)\s*\$?\s*|>\s*\$?\s*)(?<amount>[\d,]+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return decimal.TryParse(match.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                ? amount
                : null;
        }

        private static DateTime? ExtractBeforeMonthDate(string question)
        {
            var dateMatch = Regex.Match(question, @"\bbefore\s+(?<date>(?:19|20)\d{2}-\d{1,2}-\d{1,2})\b", RegexOptions.IgnoreCase);
            if (dateMatch.Success
                && DateTime.TryParse(dateMatch.Groups["date"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exactDate))
            {
                return exactDate.Date;
            }

            var yearMatch = Regex.Match(question, @"\b(19|20)\d{2}\b");
            var year = yearMatch.Success
                ? int.Parse(yearMatch.Value, CultureInfo.InvariantCulture)
                : DateTime.Today.Year;

            for (var i = 0; i < MonthNames.Length; i++)
            {
                if (Regex.IsMatch(question, $@"\bbefore\b[^.?,;]*\b{MonthNames[i].Pattern}\b", RegexOptions.IgnoreCase))
                {
                    return new DateTime(year, i + 1, 1);
                }
            }

            return null;
        }

        private static int? GetPayloadInt(BusinessAssistantActionRequest? request, string key)
        {
            if (request?.Payload == null
                || !request.Payload.TryGetValue(key, out var value)
                || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return null;
            }

            return id;
        }

        private static BusinessAssistantResponseDto BuildNavigationHint(string intent, string answer, string target)
            => BuildResponse(
                intent,
                true,
                answer,
                new[]
                {
                    new BusinessAssistantInsightDto
                    {
                        Label = "Workspace",
                        Value = target,
                        Detail = "The web and mobile shells open this action directly; desktop keeps this confirmation in the assistant thread.",
                        Severity = "Neutral"
                    }
                },
                new[]
                {
                    CreateAction(
                        intent,
                        "Open workspace",
                        answer,
                        "Navigate",
                        "Good",
                        target)
                });

        private static BusinessAssistantResponseDto BuildResponse(
            string intent,
            bool isSupported,
            string answer,
            IEnumerable<BusinessAssistantInsightDto> insights,
            IEnumerable<BusinessAssistantActionDto>? actions = null)
            => new()
            {
                Intent = intent,
                IsSupported = isSupported,
                Answer = answer,
                Insights = insights.ToList(),
                Actions = (actions ?? DefaultActions()).ToList(),
                Suggestions = DefaultSuggestions.ToList()
            };

        private static BusinessAssistantResponseDto Unsupported(string answer)
            => BuildResponse("unknown", false, answer, []);

        private static IEnumerable<BusinessAssistantActionDto> DefaultActions()
            =>
            [
                CreateAction(
                    ActionCreateReport,
                    "Create report",
                    "Pick a quick operational report to run.",
                    "Report",
                    "Neutral"),
                CreateAction(
                    ActionSendPaymentReminder,
                    "Send reminder",
                    "Prepare a payment reminder from unpaid invoices.",
                    "Draft",
                    "Good",
                    "whatsapp"),
                CreateAction(
                    ActionOpenCustomer,
                    "Open customer",
                    "Open the top customer summary from live balances.",
                    "Open",
                    "Neutral",
                    "contacts"),
                CreateAction(
                    ActionDraftPurchaseOrder,
                    "Draft purchase order",
                    "Build a low-stock purchase shortlist.",
                    "Draft",
                    "Good",
                    "purchase-parts"),
                CreateAction(
                    ActionFindUnpaidCustomers,
                    "Find unpaid customers",
                    "List customer balances from unpaid sales.",
                    "Query",
                    "Warning"),
                CreateAction(
                    ActionCampaignDeadStockClearance,
                    "Dead stock clearance",
                    "Suggest a campaign for dormant on-hand parts.",
                    "Campaign",
                    "Warning",
                    "whatsapp"),
                CreateAction(
                    ActionCampaignBackInStock,
                    "Back in stock campaign",
                    "Feature recently received available parts.",
                    "Campaign",
                    "Good",
                    "whatsapp"),
                CreateAction(
                    ActionCampaignUnpaidReminders,
                    "Unpaid reminders",
                    "Prepare a receivables reminder campaign.",
                    "Campaign",
                    "Warning",
                    "whatsapp"),
                CreateAction(
                    ActionCampaignSeasonalServiceParts,
                    "Seasonal service parts",
                    "Promote maintenance parts customers may need now.",
                    "Campaign",
                    "Good",
                    "whatsapp")
            ];

        private static IEnumerable<BusinessAssistantActionDto> ReportActions()
            =>
            [
                CreateAction(
                    ActionCreateUnpaidCustomersReport,
                    "Create unpaid customer report",
                    "Generate the receivables report now.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionShowLosingParts,
                    "Show parts losing money",
                    "Run the negative-margin parts report.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionShowSlowMovingParts,
                    "Show slow-moving parts",
                    "Find on-hand parts with weak recent movement.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionShowLosingCars,
                    "Show cars losing money",
                    "Run the used-car margin report.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionDraftPurchaseOrder,
                    "Draft purchase order",
                    "Build a low-stock purchase shortlist.",
                    "Draft",
                    "Good",
                    "purchase-parts"),
                CreateAction(
                    ActionCampaignDeadStockClearance,
                    "Dead stock campaign",
                    "Prepare a clearance campaign from dormant stock.",
                    "Campaign",
                    "Warning",
                    "whatsapp")
            ];

        private static IEnumerable<BusinessAssistantActionDto> CustomerDebtActions(int? customerId = null, string customerName = "")
            =>
            [
                CreateAction(
                    ActionSendPaymentReminder,
                    "Send reminder",
                    string.IsNullOrWhiteSpace(customerName)
                        ? "Prepare reminder text for unpaid customer invoices."
                        : $"Prepare reminder text for {customerName}.",
                    "Draft",
                    "Good",
                    "whatsapp",
                    customerId == null ? null : new Dictionary<string, string> { ["customerId"] = customerId.Value.ToString(CultureInfo.InvariantCulture) }),
                CreateAction(
                    ActionOpenCustomer,
                    "Open customer",
                    string.IsNullOrWhiteSpace(customerName)
                        ? "Open the largest unpaid customer summary."
                        : $"Open {customerName}.",
                    "Open",
                    "Neutral",
                    "contacts",
                    customerId == null ? null : new Dictionary<string, string> { ["customerId"] = customerId.Value.ToString(CultureInfo.InvariantCulture) }),
                CreateAction(
                    ActionCreateUnpaidCustomersReport,
                    "Create report",
                    "Regenerate this as a report-style view.",
                    "Report",
                    "Neutral"),
                CreateAction(
                    ActionShowLosingParts,
                    "Check losing parts",
                    "Look for inventory sales that are below cost.",
                    "Report",
                    "Warning")
            ];

        private static IEnumerable<BusinessAssistantActionDto> OpenCustomerActions(int? customerId = null, string customerName = "")
            =>
            [
                CreateAction(
                    ActionOpenContacts,
                    "Open contacts",
                    "Open the contacts workspace.",
                    "Navigate",
                    "Neutral",
                    "contacts"),
                CreateAction(
                    ActionSendPaymentReminder,
                    "Send reminder",
                    string.IsNullOrWhiteSpace(customerName)
                        ? "Prepare a payment reminder if the customer has an open balance."
                        : $"Prepare a payment reminder for {customerName}.",
                    "Draft",
                    "Good",
                    "whatsapp",
                    customerId == null ? null : new Dictionary<string, string> { ["customerId"] = customerId.Value.ToString(CultureInfo.InvariantCulture) }),
                CreateAction(
                    ActionFindUnpaidCustomers,
                    "Find unpaid customers",
                    "Review all customer receivables.",
                    "Query",
                    "Warning")
            ];

        private static IEnumerable<BusinessAssistantActionDto> PurchaseOrderActions()
            =>
            [
                CreateAction(
                    ActionOpenPurchaseParts,
                    "Open purchases",
                    "Open Part Purchases to create the supplier purchase invoice.",
                    "Navigate",
                    "Good",
                    "purchase-parts"),
                CreateAction(
                    ActionDraftPurchaseOrder,
                    "Refresh draft",
                    "Rebuild the low-stock purchase shortlist.",
                    "Draft",
                    "Good",
                    "purchase-parts"),
                CreateAction(
                    ActionShowSlowMovingParts,
                    "Check slow movers",
                    "Review stock that may not need replenishment.",
                    "Report",
                    "Warning")
            ];

        private static IEnumerable<BusinessAssistantActionDto> SlowMovingActions()
            =>
            [
                CreateAction(
                    ActionShowSlowMovingParts,
                    "Refresh slow movers",
                    "Run the slow-moving parts report again.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionCampaignDeadStockClearance,
                    "Dead stock campaign",
                    "Turn dormant stock into a clearance campaign draft.",
                    "Campaign",
                    "Warning",
                    "whatsapp"),
                CreateAction(
                    ActionDraftPurchaseOrder,
                    "Draft purchase order",
                    "Check low-stock replenishment separately.",
                    "Draft",
                    "Good",
                    "purchase-parts")
            ];

        private static IEnumerable<BusinessAssistantActionDto> CampaignActions(string refreshActionId, string campaignName)
            =>
            [
                CreateAction(
                    refreshActionId,
                    "Refresh campaign",
                    $"Rebuild the {campaignName.ToLowerInvariant()} campaign suggestions.",
                    "Campaign",
                    "Neutral",
                    "whatsapp"),
                CreateAction(
                    ActionOpenWhatsAppCampaigns,
                    "Open campaign builder",
                    "Open WhatsApp to preview recipients, edit copy, and send.",
                    "Navigate",
                    "Good",
                    "whatsapp"),
                CreateAction(
                    ActionCampaignUnpaidReminders,
                    "Unpaid reminders",
                    "Prepare a receivables reminder campaign.",
                    "Campaign",
                    "Warning",
                    "whatsapp")
            ];

        private static IEnumerable<BusinessAssistantActionDto> ProfitActions()
            =>
            [
                CreateAction(
                    ActionShowLosingParts,
                    "Refresh losing parts",
                    "Run the negative-margin parts report again.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionShowLosingCars,
                    "Show cars losing money",
                    "Check used-car realized margin.",
                    "Report",
                    "Warning"),
                CreateAction(
                    ActionFindUnpaidCustomers,
                    "Find unpaid customers",
                    "Move from margin review to receivables.",
                    "Query",
                    "Neutral")
            ];

        private static BusinessAssistantActionDto CreateAction(
            string id,
            string label,
            string description,
            string kind,
            string tone,
            string target = "",
            IReadOnlyDictionary<string, string>? payload = null)
            => new()
            {
                Id = id,
                Label = label,
                Description = description,
                Kind = kind,
                Target = target,
                Tone = tone,
                Payload = payload?.ToDictionary(item => item.Key, item => item.Value) ?? new Dictionary<string, string>()
            };

        private static DateRange ResolveDateRange(string question)
        {
            var normalized = NormalizeForMatch(question);
            var today = DateTime.Today;

            if (ContainsAll(normalized, "last", "month"))
            {
                var lastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                return MonthRange(lastMonth.Year, lastMonth.Month);
            }

            if (ContainsAll(normalized, "this", "month"))
            {
                return MonthRange(today.Year, today.Month);
            }

            var yearMatch = Regex.Match(question, @"\b(19|20)\d{2}\b");
            var year = yearMatch.Success
                ? int.Parse(yearMatch.Value, CultureInfo.InvariantCulture)
                : today.Year;

            for (var i = 0; i < MonthNames.Length; i++)
            {
                if (Regex.IsMatch(question, $@"\b{MonthNames[i].Pattern}\b", RegexOptions.IgnoreCase))
                {
                    return MonthRange(year, i + 1);
                }
            }

            return new DateRange(null, null, "all time");
        }

        private static DateRange MonthRange(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            return new DateRange(firstDay, lastDay, firstDay.ToString("MMMM yyyy", CultureInfo.CurrentCulture));
        }

        private static string NormalizeForMatch(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var ch in value.ToLowerInvariant())
            {
                builder.Append(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ');
            }

            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        private static bool ContainsAll(string normalizedValue, params string[] words)
            => words.All(word => ContainsWord(normalizedValue, word));

        private static bool ContainsAny(string normalizedValue, params string[] words)
            => words.Any(word => ContainsWord(normalizedValue, word));

        private static bool ContainsWord(string normalizedValue, string word)
            => normalizedValue.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(word, StringComparer.Ordinal);

        private static string Money(decimal amount, string? currencyCode)
            => $"{amount.ToString("N2", CultureInfo.CurrentCulture)} {NormalizeCurrency(currencyCode)}";

        private static string NormalizeCurrency(string? currencyCode)
            => string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();

        private static string FormatPhone(string? phone)
            => string.IsNullOrWhiteSpace(phone) ? "No phone" : phone.Trim();

        private static string FormatShortDate(DateTime? value)
            => value == null ? "n/a" : value.Value.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);

        private static string FormatLastSale(DateTime? lastSoldAt, int? daysSinceLastSale)
            => lastSoldAt == null
                ? "never"
                : $"{lastSoldAt.Value:yyyy-MM-dd} ({daysSinceLastSale:N0} days ago)";

        private static string BuildReminderBody(PaymentReminderDraftRow row)
            => $"Hello {row.CustomerName}, reminder for invoice {row.TransactionNumber} dated {FormatShortDate(row.TransactionDate)}. Outstanding balance: {Money(row.RemainingAmount, row.CurrencyCode)}. Please settle it when convenient. Thank you.";

        private static string PartLabel(LosingPartRow row)
            => string.IsNullOrWhiteSpace(row.InternalCode)
                ? row.PartName
                : $"{row.InternalCode} - {row.PartName}";

        private static string PartLabel(string? internalCode, string partName)
            => string.IsNullOrWhiteSpace(internalCode)
                ? partName
                : $"{internalCode} - {partName}";

        private static string FormatOem(string? oemNumber)
            => string.IsNullOrWhiteSpace(oemNumber) ? "n/a" : oemNumber.Trim();

        private static readonly (string Pattern, string Name)[] MonthNames =
        [
            ("jan(?:uary)?", "January"),
            ("feb(?:ruary)?", "February"),
            ("mar(?:ch)?", "March"),
            ("apr(?:il)?", "April"),
            ("may", "May"),
            ("jun(?:e)?", "June"),
            ("jul(?:y)?", "July"),
            ("aug(?:ust)?", "August"),
            ("sep(?:tember)?", "September"),
            ("oct(?:ober)?", "October"),
            ("nov(?:ember)?", "November"),
            ("dec(?:ember)?", "December")
        ];

        private static readonly string[] CommonVehicleBrands =
        [
            "BMW",
            "Mercedes",
            "Mercedes-Benz",
            "Audi",
            "Volkswagen",
            "Porsche",
            "Toyota",
            "Lexus",
            "Nissan",
            "Infiniti",
            "Honda",
            "Hyundai",
            "Kia",
            "Ford",
            "Chevrolet",
            "Jeep",
            "Land Rover",
            "Range Rover",
            "Mini",
            "Volvo",
            "Mazda",
            "Mitsubishi",
            "Renault",
            "Peugeot"
        ];











    }
}
