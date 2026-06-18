using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CommunicationsService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingService _accountingService;
        private readonly CustomersService _customersService;
        private readonly ICommunicationDeliveryClient _deliveryClient;
        private readonly ITenantContext _tenantContext;

        public CommunicationsService(
            ISqlConnectionFactory factory,
            AccountingService accountingService,
            CustomersService customersService,
            ICommunicationDeliveryClient deliveryClient,
            ITenantContext tenantContext)
        {
            _factory = factory;
            _accountingService = accountingService;
            _customersService = customersService;
            _deliveryClient = deliveryClient;
            _tenantContext = tenantContext;
        }

        public async Task<SendBusinessMessageResponse> SendAsync(
            SendBusinessMessageRequest request,
            int userId,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ValidationException("Communication request is required.");
            }

            var prepared = Prepare(request);
            var messageId = InsertInitialLog(prepared, userId, request.PreviewOnly ? "Preview" : "Sending");

            if (request.PreviewOnly)
            {
                return ToResponse(messageId, prepared, new CommunicationDeliveryResult
                {
                    Status = "Preview",
                    Provider = "Preview"
                });
            }

            var result = await _deliveryClient.SendAsync(prepared, cancellationToken);
            UpdateDeliveryLog(messageId, result);
            return ToResponse(messageId, prepared, result);
        }

        public IReadOnlyList<OutboundMessageLogDto> GetRecent(int take)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var messages = new OutboundMessagesRepository(session).GetRecent(take);
            session.Commit();
            return messages;
        }

        public IReadOnlyList<CommunicationConversationDto> GetConversations(string? search, int take)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var conversations = new OutboundMessagesRepository(session).GetConversations(search, take);
            session.Commit();
            return conversations;
        }

        public IReadOnlyList<CommunicationConversationMessageDto> GetConversationMessages(string phone, int take)
        {
            var normalizedPhone = NormalizePhone(phone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                throw new ValidationException("Conversation phone is required.");
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var messages = new OutboundMessagesRepository(session).GetMessagesByPhone(normalizedPhone, take);
            session.Commit();
            return messages;
        }

        public CommunicationConversationMessageDto RecordInbound(RecordInboundMessageRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("Inbound message request is required.");
            }

            var body = (request.Body ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ValidationException("Inbound message body is required.");
            }

            var normalizedPhone = NormalizePhone(request.SenderPhone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                throw new ValidationException("Inbound sender phone is required.");
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var recipient = ResolveContactByPhone(session, normalizedPhone, request.SenderName);
            var createdAt = request.ReceivedAt?.ToUniversalTime() ?? DateTime.UtcNow;
            var record = new OutboundMessageRecord
            {
                Direction = "Inbound",
                Channel = request.Channel.ToString(),
                RecipientKind = recipient.Kind,
                RecipientId = recipient.Id,
                RecipientName = recipient.Name,
                RecipientPhone = normalizedPhone,
                TemplateKey = "Inbound",
                ReferenceType = "Conversation",
                ReferenceId = null,
                Body = body,
                AttachmentCount = 0,
                Status = "Received",
                Provider = string.IsNullOrWhiteSpace(request.Provider) ? "Webhook" : request.Provider.Trim(),
                ProviderMessageId = request.ProviderMessageId,
                ProviderStatus = request.ProviderStatus,
                CreatedAt = createdAt
            };

            var id = new OutboundMessagesRepository(session).Insert(record);
            session.Commit();
            return ToConversationMessageDto(id, record);
        }

        private CommunicationPreparedMessage Prepare(SendBusinessMessageRequest request)
            => request.TemplateKey switch
            {
                CommunicationTemplateKey.SalesInvoice => PrepareSalesInvoice(request),
                CommunicationTemplateKey.PaymentReminder => PreparePaymentReminder(request),
                CommunicationTemplateKey.PurchaseInvoice => PreparePurchaseInvoice(request),
                CommunicationTemplateKey.Statement => PrepareStatement(request),
                CommunicationTemplateKey.PartAvailability => PreparePartAvailability(request),
                CommunicationTemplateKey.UsedCarImages => PrepareUsedCarImages(request),
                CommunicationTemplateKey.FreeText => PrepareFreeText(request),
                CommunicationTemplateKey.AccountBalanceReminder => PrepareAccountBalanceReminder(request),
                _ => throw new ValidationException($"Unsupported communication template '{request.TemplateKey}'.")
            };

        private CommunicationPreparedMessage PrepareSalesInvoice(SendBusinessMessageRequest request)
        {
            var invoiceId = RequireId(request.SalesInvoiceId, "Sales invoice");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var invoice = new SalesRepository(session).GetInvoiceById(invoiceId)
                ?? throw new NotFoundException("Sales invoice not found.");
            var customer = ResolveCustomer(session, invoice.CustomerId);
            var recipient = ResolveRecipient(session, request, customer);
            var balance = invoice.TotalAmount - invoice.PaidAmount;
            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Invoice {invoice.InvoiceNumber} dated {invoice.InvoiceDate:yyyy-MM-dd} is ready.",
                $"Total: {Money(invoice.TotalAmount)} | Paid: {Money(invoice.PaidAmount)} | Balance: {Money(balance)}.",
                FormatLines(invoice.Items.Select(item => $"{item.Quantity} x {item.Description} @ {Money(item.UnitPrice)}")),
                AppendNote(request.Note),
                "Thank you."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "SalesInvoice", invoice.InvoiceId, body);
        }

        private CommunicationPreparedMessage PreparePaymentReminder(SendBusinessMessageRequest request)
        {
            var invoiceId = RequireId(request.SalesInvoiceId, "Sales invoice");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var invoice = new SalesRepository(session).GetInvoiceById(invoiceId)
                ?? throw new NotFoundException("Sales invoice not found.");
            var customer = ResolveCustomer(session, invoice.CustomerId);
            var recipient = ResolveRecipient(session, request, customer);
            var balance = invoice.TotalAmount - invoice.PaidAmount;
            if (balance <= 0)
            {
                throw new ValidationException("Selected invoice has no outstanding balance.");
            }

            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Friendly payment reminder for invoice {invoice.InvoiceNumber}.",
                $"Outstanding balance: {Money(balance)} from total {Money(invoice.TotalAmount)}.",
                AppendNote(request.Note),
                "Please settle it when convenient. Thank you."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "SalesInvoice", invoice.InvoiceId, body);
        }

        private CommunicationPreparedMessage PrepareAccountBalanceReminder(SendBusinessMessageRequest request)
        {
            var customerId = RequireId(request.RecipientId, "Customer");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var recipient = ResolveCustomer(session, customerId);
            session.Commit();

            var aging = _customersService.GetAging().FirstOrDefault(row => row.CustomerId == customerId);
            if (aging is null || aging.TotalBalance <= 0)
            {
                throw new ValidationException("Customer has no outstanding balance.");
            }

            var buckets = new List<string>();
            if (aging.Current > 0) buckets.Add($"Current: {Money(aging.Current)}");
            if (aging.Days1To30 > 0) buckets.Add($"1-30 days: {Money(aging.Days1To30)}");
            if (aging.Days31To60 > 0) buckets.Add($"31-60 days: {Money(aging.Days31To60)}");
            if (aging.Days61To90 > 0) buckets.Add($"61-90 days: {Money(aging.Days61To90)}");
            if (aging.Over90Days > 0) buckets.Add($"90+ days: {Money(aging.Over90Days)}");

            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"This is a friendly reminder that your account has an outstanding balance of {Money(aging.TotalBalance)}.",
                buckets.Count > 0 ? "Breakdown: " + string.Join(", ", buckets) : string.Empty,
                AppendNote(request.Note),
                "Please settle it when convenient. Thank you."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            return CreatePrepared(request, recipient, "Customer", recipient.Id, body);
        }

        private CommunicationPreparedMessage PreparePurchaseInvoice(SendBusinessMessageRequest request)
        {
            var purchaseId = RequireId(request.PurchaseInvoiceId, "Purchase invoice");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var purchase = new PurchasesRepository(session).GetInvoiceById(purchaseId)
                ?? throw new NotFoundException("Purchase invoice not found.");
            var supplier = ResolveSupplier(session, purchase.SupplierId);
            var recipient = ResolveRecipient(session, request, supplier);
            var balance = purchase.TotalAmount - purchase.PaidAmount;
            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Purchase invoice {purchase.PurchaseNumber} dated {purchase.PurchaseDate:yyyy-MM-dd}.",
                $"Total: {Money(purchase.TotalAmount)} | Paid: {Money(purchase.PaidAmount)} | Balance: {Money(balance)}.",
                FormatLines(purchase.Items.Select(item => $"{item.Quantity} x {item.Description} @ {Money(item.UnitCost)}")),
                AppendNote(request.Note),
                "Thank you."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "PurchaseInvoice", purchase.PurchaseId, body);
        }

        private CommunicationPreparedMessage PrepareStatement(SendBusinessMessageRequest request)
        {
            var accountId = RequireId(request.AccountId, "Account");
            var statement = _accountingService.GetStatementOfAccount(
                accountId,
                request.StatementDateFrom,
                request.StatementDateTo);

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var recipient = ResolveRecipient(session, request, null);
            var period = FormatPeriod(statement.DateFrom, statement.DateTo);
            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Statement of account for {statement.AccountDisplay} {period}.",
                $"Opening: {Money(statement.OpeningBalance, statement.BaseCurrencyCode)} | Debit: {Money(statement.TotalDebit, statement.BaseCurrencyCode)} | Credit: {Money(statement.TotalCredit, statement.BaseCurrencyCode)}.",
                $"Closing balance: {Money(statement.ClosingBalance, statement.BaseCurrencyCode)}.",
                AppendNote(request.Note),
                "Please contact us if you need any detail."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "Statement", statement.AccountId, body);
        }

        private CommunicationPreparedMessage PreparePartAvailability(SendBusinessMessageRequest request)
        {
            var partId = RequireId(request.PartId, "Part");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var part = new PartsRepository(session).GetByIds([partId]).GetValueOrDefault(partId)
                ?? throw new NotFoundException("Part not found.");
            var recipient = ResolveRecipient(session, request, null);
            var available = ResolveAvailableQuantity(session, partId, request.WarehouseId);
            var price = request.QuotedPrice ?? part.SalePrice;
            var availabilityText = available <= 0 ? "currently out of stock" : $"available quantity: {available:N0}";
            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Part available update: {part.Name}.",
                $"Code: {part.InternalCode}. OEM: {part.OEMNumber ?? "N/A"}.",
                $"{availabilityText}. Price: {Money(price, part.Currency)}.",
                AppendNote(request.Note),
                "Tell us if you want us to reserve it."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "Part", partId, body);
        }

        private CommunicationPreparedMessage PrepareUsedCarImages(SendBusinessMessageRequest request)
        {
            var usedCarId = RequireId(request.UsedCarId, "Used car");

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var usedCar = session.Connection.QuerySingleOrDefault<UsedCarMessageRow>(
                @"SELECT TOP (1)
                         uc.Id,
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
                         END AS Car,
                         uc.ModelYear,
                         uc.PriceCurrency,
                         uc.Price
                  FROM dbo.UsedCars uc
                  INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
                  INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
                  WHERE uc.Id = @UsedCarId AND (@TenantId = 0 OR uc.TenantId = @TenantId);",
                new { UsedCarId = usedCarId, TenantId = session.TenantId },
                session.Transaction)
                ?? throw new NotFoundException("Used car not found.");

            var recipient = ResolveRecipient(session, request, null);
            var attachments = LoadUsedCarImageAttachments(session, usedCarId, request.UsedCarImageIds);
            var body = string.Join(Environment.NewLine, new[]
            {
                $"Hello {recipient.Name},",
                $"Sharing car images for {usedCar.Car} {usedCar.ModelYear}.",
                $"Asking price: {Money(usedCar.Price, usedCar.PriceCurrency)}.",
                attachments.Count == 0 ? "No saved images are attached yet." : $"{attachments.Count:N0} image(s) attached.",
                AppendNote(request.Note)
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            session.Commit();
            return CreatePrepared(request, recipient, "UsedCar", usedCarId, body, attachments);
        }

        private CommunicationPreparedMessage PrepareFreeText(SendBusinessMessageRequest request)
        {
            var body = (request.MessageBody ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ValidationException("Message body is required.");
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var recipient = ResolveRecipient(session, request, null);
            session.Commit();

            var referenceType = request.CampaignId is > 0 ? "WhatsAppCampaign" : "Conversation";
            var referenceId = request.CampaignId is > 0 ? request.CampaignId : null;
            return CreatePrepared(request, recipient, referenceType, referenceId, body, request.Attachments);
        }

        private int InsertInitialLog(CommunicationPreparedMessage prepared, int userId, string status)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var id = new OutboundMessagesRepository(session).Insert(new OutboundMessageRecord
            {
                Direction = "Outbound",
                Channel = prepared.Channel,
                RecipientKind = prepared.RecipientKind,
                RecipientId = prepared.RecipientId,
                RecipientName = prepared.RecipientName,
                RecipientPhone = prepared.RecipientPhone,
                TemplateKey = prepared.TemplateKey,
                ReferenceType = prepared.ReferenceType,
                ReferenceId = prepared.ReferenceId,
                Body = prepared.Body,
                AttachmentCount = prepared.Attachments.Count,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            session.Commit();
            return id;
        }

        private void UpdateDeliveryLog(int messageId, CommunicationDeliveryResult result)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            new OutboundMessagesRepository(session).UpdateDeliveryResult(
                messageId,
                result.Status,
                result.Provider,
                result.ProviderMessageId,
                result.ProviderStatus,
                result.ErrorMessage,
                result.SentAt);
            session.Commit();
        }

        private static CommunicationPreparedMessage CreatePrepared(
            SendBusinessMessageRequest request,
            ResolvedRecipient recipient,
            string referenceType,
            int? referenceId,
            string body,
            List<CommunicationAttachmentDto>? attachments = null)
        {
            var normalizedPhone = NormalizePhone(recipient.Phone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                throw new ValidationException($"{recipient.Kind} '{recipient.Name}' does not have a phone number.");
            }

            return new CommunicationPreparedMessage
            {
                Channel = request.Channel.ToString(),
                RecipientKind = recipient.Kind,
                RecipientId = recipient.Id,
                RecipientName = recipient.Name,
                RecipientPhone = normalizedPhone,
                TemplateKey = request.TemplateKey.ToString(),
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Body = body,
                Attachments = attachments ?? new List<CommunicationAttachmentDto>()
            };
        }

        private ResolvedRecipient ResolveRecipient(
            DbSession session,
            SendBusinessMessageRequest request,
            ResolvedRecipient? defaultRecipient)
        {
            if (!string.IsNullOrWhiteSpace(request.RecipientPhoneOverride))
            {
                return new ResolvedRecipient(
                    "Manual",
                    null,
                    string.IsNullOrWhiteSpace(request.RecipientNameOverride)
                        ? "Customer"
                        : request.RecipientNameOverride.Trim(),
                    request.RecipientPhoneOverride);
            }

            return request.RecipientKind switch
            {
                CommunicationRecipientKind.Customer when request.RecipientId is int id => ResolveCustomer(session, id),
                CommunicationRecipientKind.Customer when defaultRecipient?.Kind == "Customer" => defaultRecipient,
                CommunicationRecipientKind.Customer => throw new ValidationException("Customer is required."),
                CommunicationRecipientKind.Supplier when request.RecipientId is int id => ResolveSupplier(session, id),
                CommunicationRecipientKind.Supplier when defaultRecipient?.Kind == "Supplier" => defaultRecipient,
                CommunicationRecipientKind.Supplier => throw new ValidationException("Supplier is required."),
                CommunicationRecipientKind.Manual => throw new ValidationException("Manual recipient phone is required."),
                _ => throw new ValidationException($"Unsupported recipient kind '{request.RecipientKind}'.")
            };
        }

        private static ResolvedRecipient ResolveContactByPhone(DbSession session, string normalizedPhone, string? fallbackName)
        {
            var customer = new CustomersRepository(session)
                .GetAll()
                .FirstOrDefault(item => PhoneMatches(item.Phone, normalizedPhone));
            if (customer != null)
            {
                return new ResolvedRecipient("Customer", customer.Id, customer.Name, customer.Phone);
            }

            var supplier = new SuppliersRepository(session)
                .GetAll()
                .FirstOrDefault(item => PhoneMatches(item.Phone, normalizedPhone));
            if (supplier != null)
            {
                return new ResolvedRecipient("Supplier", supplier.Id, supplier.Name, supplier.Phone);
            }

            var name = string.IsNullOrWhiteSpace(fallbackName) ? normalizedPhone : fallbackName.Trim();
            return new ResolvedRecipient("Manual", null, name, normalizedPhone);
        }

        private static ResolvedRecipient ResolveCustomer(DbSession session, int? customerId)
        {
            if (customerId is not > 0)
            {
                throw new ValidationException("Customer is required.");
            }

            var customer = new CustomersRepository(session).GetById(customerId.Value)
                ?? throw new NotFoundException("Customer not found.");
            return new ResolvedRecipient("Customer", customer.Id, customer.Name, customer.Phone);
        }

        private static ResolvedRecipient ResolveSupplier(DbSession session, int supplierId)
        {
            if (supplierId <= 0)
            {
                throw new ValidationException("Supplier is required.");
            }

            var supplier = new SuppliersRepository(session).GetById(supplierId)
                ?? throw new NotFoundException("Supplier not found.");
            return new ResolvedRecipient("Supplier", supplier.Id, supplier.Name, supplier.Phone);
        }

        private static int ResolveAvailableQuantity(DbSession session, int partId, int? warehouseId)
        {
            var sql = warehouseId is > 0
                ? @"SELECT ISNULL(SUM(Quantity - ReservedQuantity), 0)
                    FROM dbo.Stock
                    WHERE PartId = @PartId
                      AND WarehouseId = @WarehouseId;"
                : @"SELECT ISNULL(SUM(Quantity - ReservedQuantity), 0)
                    FROM dbo.Stock
                    WHERE PartId = @PartId;";

            return session.Connection.ExecuteScalar<int>(
                sql,
                new { PartId = partId, WarehouseId = warehouseId },
                session.Transaction);
        }

        private static List<CommunicationAttachmentDto> LoadUsedCarImageAttachments(
            DbSession session,
            int usedCarId,
            IReadOnlyCollection<int> requestedImageIds)
        {
            var rows = session.Connection.Query<UsedCarImageAttachmentRow>(
                @"SELECT TOP (10)
                         ImageId,
                         ImageMimeType,
                         ImageData
                  FROM dbo.usedcar_images
                  WHERE UsedCarId = @UsedCarId
                    AND (@HasRequestedIds = 0 OR ImageId IN @ImageIds)
                  ORDER BY CreatedAt DESC, ImageId DESC;",
                new
                {
                    UsedCarId = usedCarId,
                    HasRequestedIds = requestedImageIds.Count > 0,
                    ImageIds = requestedImageIds.Count == 0 ? new[] { 0 } : requestedImageIds
                },
                session.Transaction);

            return rows.Select(row => new CommunicationAttachmentDto
            {
                SourceId = row.ImageId,
                FileName = $"used-car-{usedCarId}-{row.ImageId}{ResolveExtension(row.ImageMimeType)}",
                MimeType = row.ImageMimeType,
                Base64Content = Convert.ToBase64String(row.ImageData)
            }).ToList();
        }

        private static string FormatLines(IEnumerable<string> lines)
        {
            var selected = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Take(5)
                .ToList();

            return selected.Count == 0
                ? string.Empty
                : "Items: " + string.Join("; ", selected);
        }

        private static string AppendNote(string? note)
            => string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();

        private static int RequireId(int? id, string label)
        {
            if (id is > 0)
            {
                return id.Value;
            }

            throw new ValidationException($"{label} is required.");
        }

        private static string Money(decimal amount, string currencyCode = "USD")
            => $"{(string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant())} {amount:N2}";

        private static string FormatPeriod(DateTime? from, DateTime? to)
        {
            if (from == null && to == null)
            {
                return "for all dates";
            }

            if (from != null && to != null)
            {
                return $"from {from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
            }

            return from != null
                ? $"from {from.Value:yyyy-MM-dd}"
                : $"up to {to!.Value:yyyy-MM-dd}";
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var trimmed = phone.Trim();
            var chars = trimmed
                .Where((ch, index) => char.IsDigit(ch) || (ch == '+' && index == 0))
                .ToArray();
            var normalized = new string(chars);
            return normalized.Any(char.IsDigit) ? normalized : string.Empty;
        }

        private static bool PhoneMatches(string? candidatePhone, string normalizedPhone)
        {
            var candidate = NormalizePhone(candidatePhone);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            if (string.Equals(candidate, normalizedPhone, StringComparison.Ordinal))
            {
                return true;
            }

            var candidateDigits = DigitsOnly(candidate);
            var phoneDigits = DigitsOnly(normalizedPhone);
            if (candidateDigits.Length < 7 || phoneDigits.Length < 7)
            {
                return false;
            }

            var candidateTail = candidateDigits[^7..];
            var phoneTail = phoneDigits[^7..];
            return string.Equals(candidateTail, phoneTail, StringComparison.Ordinal);
        }

        private static string DigitsOnly(string value)
            => new(value.Where(char.IsDigit).ToArray());

        private static string ResolveExtension(string mimeType)
            => mimeType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".png"
            };

        private static SendBusinessMessageResponse ToResponse(
            int messageId,
            CommunicationPreparedMessage prepared,
            CommunicationDeliveryResult result)
            => new()
            {
                MessageId = messageId,
                Status = result.Status,
                Channel = prepared.Channel,
                TemplateKey = prepared.TemplateKey,
                RecipientName = prepared.RecipientName,
                RecipientPhone = prepared.RecipientPhone,
                Body = prepared.Body,
                AttachmentCount = prepared.Attachments.Count,
                Provider = result.Provider,
                ProviderMessageId = result.ProviderMessageId,
                ProviderStatus = result.ProviderStatus,
                ErrorMessage = result.ErrorMessage
            };

        private static CommunicationConversationMessageDto ToConversationMessageDto(
            int id,
            OutboundMessageRecord record)
            => new()
            {
                Id = id,
                Direction = record.Direction,
                Channel = record.Channel,
                RecipientKind = record.RecipientKind,
                RecipientId = record.RecipientId,
                RecipientName = record.RecipientName,
                RecipientPhone = record.RecipientPhone,
                TemplateKey = record.TemplateKey,
                ReferenceType = record.ReferenceType,
                ReferenceId = record.ReferenceId,
                Body = record.Body,
                AttachmentCount = record.AttachmentCount,
                Status = record.Status,
                Provider = record.Provider,
                ProviderMessageId = record.ProviderMessageId,
                ProviderStatus = record.ProviderStatus,
                ErrorMessage = record.ErrorMessage,
                CreatedAt = record.CreatedAt,
                SentAt = record.SentAt
            };

    }
}
