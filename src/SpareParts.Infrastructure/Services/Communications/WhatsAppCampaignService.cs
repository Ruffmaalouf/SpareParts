using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Services
{
    public sealed class WhatsAppCampaignService
    {
        private const int DefaultMaxRecipients = 50;
        private const int AbsoluteMaxRecipients = 200;

        private readonly ISqlConnectionFactory _factory;
        private readonly CommunicationsService _communicationsService;

        public WhatsAppCampaignService(
            ISqlConnectionFactory factory,
            CommunicationsService communicationsService)
        {
            _factory = factory;
            _communicationsService = communicationsService;
        }

        public IReadOnlyList<WhatsAppCampaignAssetDto> GetAssets()
        {
            using var conn = _factory.CreateConnection();
            var parts = conn.Query<WhatsAppCampaignAssetDto>(
                """
SELECT TOP (180)
    AssetType = N'Part',
    p.Id,
    Title = CONCAT(COALESCE(NULLIF(LTRIM(RTRIM(p.InternalCode)), N''), CONCAT(N'Part ', p.Id)), N' - ', p.Name),
    Subtitle = CONCAT(
        N'OEM ', COALESCE(NULLIF(LTRIM(RTRIM(p.OEMNumber)), N''), N'-'),
        N' · ',
        COALESCE(NULLIF(LTRIM(RTRIM(p.Condition)), N''), N'Part'),
        N' · Stock ',
        CONVERT(NVARCHAR(30), CONVERT(DECIMAL(19, 0), ISNULL(stock.AvailableQuantity, 0)))
    ),
    Price = p.SalePrice,
    Currency = p.Currency,
    ImageCount = 0,
    IsSelected = CAST(0 AS BIT)
FROM dbo.Parts p
OUTER APPLY
(
    SELECT AvailableQuantity = ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0)
    FROM dbo.Stock s
    WHERE s.PartId = p.Id
) stock
WHERE p.IsActive = 1
ORDER BY
    CASE WHEN ISNULL(stock.AvailableQuantity, 0) > 0 THEN 0 ELSE 1 END,
    p.Name,
    p.Id;
""").ToList();

            var cars = conn.Query<WhatsAppCampaignAssetDto>(
                """
SELECT TOP (90)
    AssetType = N'Car',
    uc.Id,
    Title = CONCAT(
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
        END,
        N' ',
        uc.ModelYear
    ),
    Subtitle = CONCAT(
        COALESCE(NULLIF(LTRIM(RTRIM(loc.Name)), N''), NULLIF(LTRIM(RTRIM(uc.Location)), N''), N'No location'),
        N' · ',
        CASE WHEN uc.IsReceived = 1 THEN N'Received' ELSE N'Incoming' END,
        N' · ',
        ISNULL(images.ImageCount, 0),
        N' image(s)'
    ),
    Price = uc.Price,
    Currency = uc.PriceCurrency,
    ImageCount = ISNULL(images.ImageCount, 0),
    IsSelected = CAST(0 AS BIT)
FROM dbo.UsedCars uc
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
OUTER APPLY
(
    SELECT ImageCount = COUNT(1)
    FROM dbo.usedcar_images image
    WHERE image.UsedCarId = uc.Id
) images
ORDER BY uc.CreatedAt DESC, uc.Id DESC;
""").ToList();

            return parts.Concat(cars).ToList();
        }

        public WhatsAppCampaignPreviewResponse Preview(WhatsAppCampaignPreviewRequest request)
        {
            var build = BuildCampaign(request);
            return new WhatsAppCampaignPreviewResponse
            {
                Segment = build.Segment,
                Language = build.Language,
                MessageBody = build.MessageBody,
                RecipientCount = build.Recipients.Count,
                AttachmentCount = build.AttachmentCount,
                Recipients = build.Recipients,
                Assets = build.Assets
            };
        }

        public async Task<WhatsAppCampaignSendResponse> SendAsync(
            SendWhatsAppCampaignRequest request,
            int userId,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ValidationException("WhatsApp campaign request is required.");
            }

            var build = BuildCampaign(request);
            if (build.Recipients.Count == 0)
            {
                throw new ValidationException("No customers matched this campaign segment.");
            }

            var body = string.IsNullOrWhiteSpace(request.MessageBodyOverride)
                ? build.MessageBody
                : request.MessageBodyOverride.Trim();

            using var conn = _factory.CreateConnection();
            var attachments = build.IncludeImages
                ? LoadAttachments(conn, build.UsedCarIds)
                : new List<CommunicationAttachmentDto>();
            var campaignName = NormalizeCampaignName(request.Name, build.Segment);
            var campaignId = InsertCampaign(
                conn,
                campaignName,
                build,
                body,
                attachments.Count,
                userId);

            var sentCount = 0;
            var failedCount = 0;
            var preparedCount = 0;

            foreach (var recipient in build.Recipients)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var status = "Failed";
                string? errorMessage = null;
                int? messageId = null;
                DateTime? sentAt = null;

                try
                {
                    var response = await _communicationsService.SendAsync(
                        new SendBusinessMessageRequest
                        {
                            Channel = CommunicationChannel.WhatsApp,
                            TemplateKey = CommunicationTemplateKey.FreeText,
                            RecipientKind = CommunicationRecipientKind.Manual,
                            RecipientNameOverride = recipient.Name,
                            RecipientPhoneOverride = recipient.Phone,
                            MessageBody = Personalize(body, recipient.Name),
                            CampaignId = campaignId,
                            Attachments = attachments
                        },
                        userId,
                        cancellationToken);

                    messageId = response.MessageId;
                    status = string.IsNullOrWhiteSpace(response.Status) ? "Prepared" : response.Status.Trim();
                    errorMessage = response.ErrorMessage;
                    if (IsPreparedStatus(status))
                    {
                        preparedCount++;
                    }
                    else if (IsFailedStatus(status))
                    {
                        failedCount++;
                    }
                    else
                    {
                        sentCount++;
                        sentAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failedCount++;
                    status = "Failed";
                    errorMessage = ex.Message;
                }

                InsertCampaignRecipient(
                    conn,
                    campaignId,
                    recipient,
                    messageId,
                    status,
                    errorMessage,
                    sentAt);
            }

            var campaignStatus = ResolveCampaignStatus(sentCount, failedCount, preparedCount, build.Recipients.Count);
            UpdateCampaignCounts(conn, campaignId, sentCount, failedCount, preparedCount, campaignStatus);

            return new WhatsAppCampaignSendResponse
            {
                CampaignId = campaignId,
                Name = campaignName,
                Status = campaignStatus,
                RecipientCount = build.Recipients.Count,
                SentCount = sentCount,
                FailedCount = failedCount,
                PreparedCount = preparedCount,
                AttachmentCount = attachments.Count,
                MessageBody = body
            };
        }

        public IReadOnlyList<WhatsAppCampaignSummaryDto> GetRecent(int take)
        {
            using var conn = _factory.CreateConnection();
            return conn.Query<WhatsAppCampaignSummaryDto>(
                """
SELECT TOP (@Take)
    c.Id,
    c.Name,
    c.Segment,
    c.Language,
    c.MessageBody,
    c.RecipientCount,
    c.SentCount,
    c.FailedCount,
    c.PreparedCount,
    c.AttachmentCount,
    c.Status,
    c.CreatedAt,
    ReplyCount = ISNULL(replies.ReplyCount, 0),
    SalesCount = ISNULL(sales.SalesCount, 0),
    SalesAmount = ISNULL(sales.SalesAmount, 0)
FROM dbo.WhatsAppCampaigns c
OUTER APPLY
(
    SELECT ReplyCount = COUNT(DISTINCT inbound.Id)
    FROM dbo.WhatsAppCampaignRecipients r
    INNER JOIN dbo.OutboundMessages inbound
        ON inbound.Direction = N'Inbound'
       AND inbound.RecipientPhone = r.RecipientPhone
       AND inbound.CreatedAt >= c.CreatedAt
    WHERE r.CampaignId = c.Id
) replies
OUTER APPLY
(
    SELECT
        SalesCount = COUNT(DISTINCT t.Id),
        SalesAmount = SUM(CASE WHEN t.IsReturn = 1 THEN -ABS(ISNULL(t.TotalAmount, 0)) ELSE ISNULL(t.TotalAmount, 0) END)
    FROM dbo.WhatsAppCampaignRecipients r
    INNER JOIN dbo.Transactions t
        ON t.CustomerId = r.CustomerId
       AND t.TransactionDate >= c.CreatedAt
    INNER JOIN dbo.TransactionTypes tt
        ON tt.Id = t.TransactionTypeId
       AND tt.TypeKey = @SaleType
    WHERE r.CampaignId = c.Id
) sales
ORDER BY c.CreatedAt DESC, c.Id DESC;
""",
                new
                {
                    Take = Math.Clamp(take, 1, 100),
                    SaleType = TransactionTypeKeys.Sale
                }).ToList();
        }

        private CampaignBuild BuildCampaign(WhatsAppCampaignPreviewRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("WhatsApp campaign request is required.");
            }

            var segment = NormalizeSegment(request.Segment);
            var language = NormalizeLanguage(request.Language);
            var maxRecipients = Math.Clamp(
                request.MaxRecipients <= 0 ? DefaultMaxRecipients : request.MaxRecipients,
                1,
                AbsoluteMaxRecipients);
            var partIds = NormalizeIds(request.PartIds);
            var usedCarIds = NormalizeIds(request.UsedCarIds);
            var customerIds = NormalizeIds(request.CustomerIds);

            using var conn = _factory.CreateConnection();
            var recipients = LoadRecipients(conn, segment, customerIds, maxRecipients);
            var assets = LoadSelectedAssets(conn, partIds, usedCarIds);
            var attachmentCount = request.IncludeImages ? CountAttachments(conn, usedCarIds) : 0;
            var body = BuildMessage(language, assets, request.Note, request.IncludeImages && attachmentCount > 0);

            return new CampaignBuild(
                segment,
                language,
                request.IncludeImages,
                partIds,
                usedCarIds,
                recipients,
                assets,
                attachmentCount,
                body);
        }

        private static IReadOnlyList<WhatsAppCampaignRecipientDto> LoadRecipients(
            IDbConnection conn,
            string segment,
            IReadOnlyCollection<int> customerIds,
            int maxRecipients)
        {
            var rows = string.Equals(segment, WhatsAppCampaignSegments.WaitingForParts, StringComparison.Ordinal)
                ? LoadWaitingRecipients(conn, customerIds, maxRecipients)
                : LoadCustomerRecipients(conn, segment, customerIds, maxRecipients);

            var deduped = new Dictionary<string, WhatsAppCampaignRecipientDto>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                var phone = NormalizePhone(row.Phone);
                if (string.IsNullOrWhiteSpace(phone) || deduped.ContainsKey(phone))
                {
                    continue;
                }

                row.Phone = phone;
                row.Name = string.IsNullOrWhiteSpace(row.Name) ? "Customer" : row.Name.Trim();
                deduped[phone] = row;
            }

            return deduped.Values.Take(maxRecipients).ToList();
        }

        private static IReadOnlyList<WhatsAppCampaignRecipientDto> LoadCustomerRecipients(
            IDbConnection conn,
            string segment,
            IReadOnlyCollection<int> customerIds,
            int maxRecipients)
        {
            return conn.Query<WhatsAppCampaignRecipientDto>(
                """
WITH SaleSummary AS
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
        END)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE t.CustomerId IS NOT NULL
    GROUP BY t.CustomerId
),
ContactSummary AS
(
    SELECT
        om.RecipientId AS CustomerId,
        LastInboundAt = MAX(CASE WHEN om.Direction = N'Inbound' THEN om.CreatedAt END),
        LastOutboundAt = MAX(CASE WHEN om.Direction = N'Outbound' THEN om.CreatedAt END)
    FROM dbo.OutboundMessages om
    WHERE om.RecipientKind = N'Customer'
      AND om.RecipientId IS NOT NULL
    GROUP BY om.RecipientId
)
SELECT TOP (@MaxRecipients)
    CustomerId = c.Id,
    c.Name,
    c.Phone,
    s.LastSaleAt,
    SaleCount = ISNULL(s.SaleCount, 0),
    TotalSales = ISNULL(s.TotalSales, 0),
    OutstandingBalance = ISNULL(c.OpeningBalance, 0) + ISNULL(s.OutstandingBalance, 0),
    contact.LastInboundAt,
    contact.LastOutboundAt
FROM dbo.Customers c
LEFT JOIN SaleSummary s ON s.CustomerId = c.Id
LEFT JOIN ContactSummary contact ON contact.CustomerId = c.Id
WHERE NULLIF(LTRIM(RTRIM(c.Phone)), N'') IS NOT NULL
  AND (@HasCustomerIds = 0 OR c.Id IN @CustomerIds)
  AND
  (
      @Segment = @AllCustomers
      OR (@Segment = @ActiveCustomers AND s.LastSaleAt >= DATEADD(DAY, -180, SYSUTCDATETIME()))
      OR (@Segment = @InactiveCustomers AND (s.LastSaleAt IS NULL OR s.LastSaleAt < DATEADD(DAY, -180, SYSUTCDATETIME())))
      OR (@Segment = @UnpaidCustomers AND (ISNULL(c.OpeningBalance, 0) + ISNULL(s.OutstandingBalance, 0)) > 0)
      OR (@Segment = @RecentBuyers AND s.LastSaleAt >= DATEADD(DAY, -30, SYSUTCDATETIME()))
  )
ORDER BY
    CASE WHEN @Segment = @UnpaidCustomers THEN ISNULL(c.OpeningBalance, 0) + ISNULL(s.OutstandingBalance, 0) ELSE 0 END DESC,
    s.LastSaleAt DESC,
    c.Name;
""",
                new
                {
                    MaxRecipients = maxRecipients * 2,
                    SaleType = TransactionTypeKeys.Sale,
                    Segment = segment,
                    AllCustomers = WhatsAppCampaignSegments.AllCustomers,
                    ActiveCustomers = WhatsAppCampaignSegments.ActiveCustomers,
                    InactiveCustomers = WhatsAppCampaignSegments.InactiveCustomers,
                    UnpaidCustomers = WhatsAppCampaignSegments.UnpaidCustomers,
                    RecentBuyers = WhatsAppCampaignSegments.RecentBuyers,
                    HasCustomerIds = customerIds.Count > 0,
                    CustomerIds = customerIds.Count == 0 ? new[] { 0 } : customerIds
                }).ToList();
        }

        private static IReadOnlyList<WhatsAppCampaignRecipientDto> LoadWaitingRecipients(
            IDbConnection conn,
            IReadOnlyCollection<int> customerIds,
            int maxRecipients)
        {
            return conn.Query<WhatsAppCampaignRecipientDto>(
                """
WITH Waiting AS
(
    SELECT
        pr.CustomerId,
        CustomerName = MAX(pr.CustomerName),
        CustomerPhone = MAX(pr.CustomerPhone),
        LastRequestAt = MAX(pr.CreatedAt),
        RequestCount = COUNT(1)
    FROM dbo.PartRequests pr
    WHERE pr.Status IN (N'Open', N'Contacted')
    GROUP BY pr.CustomerId, COALESCE(CONVERT(NVARCHAR(20), pr.CustomerId), UPPER(LTRIM(RTRIM(pr.CustomerName))) + N'|' + ISNULL(pr.CustomerPhone, N''))
),
SaleSummary AS
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
        END)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE t.CustomerId IS NOT NULL
    GROUP BY t.CustomerId
)
SELECT TOP (@MaxRecipients)
    CustomerId = COALESCE(c.Id, w.CustomerId),
    Name = COALESCE(NULLIF(LTRIM(RTRIM(c.Name)), N''), NULLIF(LTRIM(RTRIM(w.CustomerName)), N''), N'Customer'),
    Phone = COALESCE(NULLIF(LTRIM(RTRIM(c.Phone)), N''), NULLIF(LTRIM(RTRIM(w.CustomerPhone)), N'')),
    s.LastSaleAt,
    SaleCount = ISNULL(s.SaleCount, 0),
    TotalSales = ISNULL(s.TotalSales, 0),
    OutstandingBalance = ISNULL(c.OpeningBalance, 0) + ISNULL(s.OutstandingBalance, 0),
    LastInboundAt = phoneContact.LastInboundAt,
    LastOutboundAt = phoneContact.LastOutboundAt
FROM Waiting w
LEFT JOIN dbo.Customers c ON c.Id = w.CustomerId
LEFT JOIN SaleSummary s ON s.CustomerId = COALESCE(c.Id, w.CustomerId)
OUTER APPLY
(
    SELECT
        LastInboundAt = MAX(CASE WHEN om.Direction = N'Inbound' THEN om.CreatedAt END),
        LastOutboundAt = MAX(CASE WHEN om.Direction = N'Outbound' THEN om.CreatedAt END)
    FROM dbo.OutboundMessages om
    WHERE om.RecipientPhone = COALESCE(NULLIF(LTRIM(RTRIM(c.Phone)), N''), NULLIF(LTRIM(RTRIM(w.CustomerPhone)), N''))
) phoneContact
WHERE COALESCE(NULLIF(LTRIM(RTRIM(c.Phone)), N''), NULLIF(LTRIM(RTRIM(w.CustomerPhone)), N'')) IS NOT NULL
  AND (@HasCustomerIds = 0 OR COALESCE(c.Id, w.CustomerId) IN @CustomerIds)
ORDER BY w.LastRequestAt DESC;
""",
                new
                {
                    MaxRecipients = maxRecipients * 2,
                    SaleType = TransactionTypeKeys.Sale,
                    HasCustomerIds = customerIds.Count > 0,
                    CustomerIds = customerIds.Count == 0 ? new[] { 0 } : customerIds
                }).ToList();
        }

        private static IReadOnlyList<WhatsAppCampaignAssetDto> LoadSelectedAssets(
            IDbConnection conn,
            IReadOnlyCollection<int> partIds,
            IReadOnlyCollection<int> usedCarIds)
        {
            var parts = partIds.Count == 0
                ? new List<WhatsAppCampaignAssetDto>()
                : conn.Query<WhatsAppCampaignAssetDto>(
                    """
SELECT
    AssetType = N'Part',
    p.Id,
    Title = CONCAT(COALESCE(NULLIF(LTRIM(RTRIM(p.InternalCode)), N''), CONCAT(N'Part ', p.Id)), N' - ', p.Name),
    Subtitle = CONCAT(
        N'OEM ', COALESCE(NULLIF(LTRIM(RTRIM(p.OEMNumber)), N''), N'-'),
        N' · ',
        COALESCE(NULLIF(LTRIM(RTRIM(p.Condition)), N''), N'Part')
    ),
    Price = p.SalePrice,
    Currency = p.Currency,
    ImageCount = 0,
    IsSelected = CAST(1 AS BIT)
FROM dbo.Parts p
INNER JOIN @Ids ids ON ids.Id = p.Id
ORDER BY p.Name, p.Id;
""",
                    new { Ids = ToIntIdList(partIds).AsTableValuedParameter("dbo.IntIdList") }).ToList();

            var cars = usedCarIds.Count == 0
                ? new List<WhatsAppCampaignAssetDto>()
                : conn.Query<WhatsAppCampaignAssetDto>(
                    """
SELECT
    AssetType = N'Car',
    uc.Id,
    Title = CONCAT(
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
        END,
        N' ',
        uc.ModelYear
    ),
    Subtitle = CONCAT(
        COALESCE(NULLIF(LTRIM(RTRIM(loc.Name)), N''), NULLIF(LTRIM(RTRIM(uc.Location)), N''), N'No location'),
        N' · ',
        CASE WHEN uc.IsReceived = 1 THEN N'Received' ELSE N'Incoming' END
    ),
    Price = uc.Price,
    Currency = uc.PriceCurrency,
    ImageCount = ISNULL(images.ImageCount, 0),
    IsSelected = CAST(1 AS BIT)
FROM dbo.UsedCars uc
INNER JOIN @Ids ids ON ids.Id = uc.Id
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
OUTER APPLY
(
    SELECT ImageCount = COUNT(1)
    FROM dbo.usedcar_images image
    WHERE image.UsedCarId = uc.Id
) images
ORDER BY uc.ModelYear DESC, uc.Id DESC;
""",
                    new { Ids = ToIntIdList(usedCarIds).AsTableValuedParameter("dbo.IntIdList") }).ToList();

            return parts.Concat(cars).ToList();
        }

        private static int CountAttachments(IDbConnection conn, IReadOnlyCollection<int> usedCarIds)
        {
            if (usedCarIds.Count == 0)
            {
                return 0;
            }

            return conn.ExecuteScalar<int>(
                """
SELECT COUNT(1)
FROM
(
    SELECT TOP (6) image.ImageId
    FROM dbo.usedcar_images image
    INNER JOIN @UsedCarIds ids ON ids.Id = image.UsedCarId
    ORDER BY image.CreatedAt DESC, image.ImageId DESC
) selected;
""",
                new { UsedCarIds = ToIntIdList(usedCarIds).AsTableValuedParameter("dbo.IntIdList") });
        }

        private static List<CommunicationAttachmentDto> LoadAttachments(
            IDbConnection conn,
            IReadOnlyCollection<int> usedCarIds)
        {
            if (usedCarIds.Count == 0)
            {
                return new List<CommunicationAttachmentDto>();
            }

            var rows = conn.Query<CampaignImageRow>(
                """
SELECT TOP (6)
    image.ImageId,
    image.UsedCarId,
    image.ImageMimeType,
    image.ImageData
FROM dbo.usedcar_images image
INNER JOIN @UsedCarIds ids ON ids.Id = image.UsedCarId
ORDER BY image.CreatedAt DESC, image.ImageId DESC;
""",
                new { UsedCarIds = ToIntIdList(usedCarIds).AsTableValuedParameter("dbo.IntIdList") });

            return rows.Select(row => new CommunicationAttachmentDto
            {
                SourceId = row.ImageId,
                FileName = $"campaign-car-{row.UsedCarId}-{row.ImageId}{ResolveExtension(row.ImageMimeType)}",
                MimeType = row.ImageMimeType,
                Base64Content = Convert.ToBase64String(row.ImageData)
            }).ToList();
        }

        private static int InsertCampaign(
            IDbConnection conn,
            string name,
            CampaignBuild build,
            string body,
            int attachmentCount,
            int userId)
        {
            return conn.ExecuteScalar<int>(
                """
INSERT INTO dbo.WhatsAppCampaigns
    (Name, Segment, Language, MessageBody, SelectedPartIds, SelectedUsedCarIds, RecipientCount, SentCount, FailedCount, PreparedCount, AttachmentCount, Status, CreatedByUserId)
VALUES
    (@Name, @Segment, @Language, @MessageBody, @SelectedPartIds, @SelectedUsedCarIds, @RecipientCount, 0, 0, 0, @AttachmentCount, N'Sending', @UserId);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
                new
                {
                    Name = name,
                    build.Segment,
                    build.Language,
                    MessageBody = body,
                    SelectedPartIds = string.Join(",", build.PartIds),
                    SelectedUsedCarIds = string.Join(",", build.UsedCarIds),
                    RecipientCount = build.Recipients.Count,
                    AttachmentCount = attachmentCount,
                    UserId = userId
                });
        }

        private static void InsertCampaignRecipient(
            IDbConnection conn,
            int campaignId,
            WhatsAppCampaignRecipientDto recipient,
            int? messageId,
            string status,
            string? errorMessage,
            DateTime? sentAt)
        {
            conn.Execute(
                """
INSERT INTO dbo.WhatsAppCampaignRecipients
    (CampaignId, CustomerId, RecipientName, RecipientPhone, MessageId, Status, ErrorMessage, SentAt)
VALUES
    (@CampaignId, @CustomerId, @RecipientName, @RecipientPhone, @MessageId, @Status, @ErrorMessage, @SentAt);
""",
                new
                {
                    CampaignId = campaignId,
                    recipient.CustomerId,
                    RecipientName = recipient.Name,
                    RecipientPhone = recipient.Phone,
                    MessageId = messageId,
                    Status = status,
                    ErrorMessage = Truncate(errorMessage, 1000),
                    SentAt = sentAt
                });
        }

        private static void UpdateCampaignCounts(
            IDbConnection conn,
            int campaignId,
            int sentCount,
            int failedCount,
            int preparedCount,
            string status)
        {
            conn.Execute(
                """
UPDATE dbo.WhatsAppCampaigns
SET SentCount = @SentCount,
    FailedCount = @FailedCount,
    PreparedCount = @PreparedCount,
    Status = @Status
WHERE Id = @CampaignId;
""",
                new
                {
                    CampaignId = campaignId,
                    SentCount = sentCount,
                    FailedCount = failedCount,
                    PreparedCount = preparedCount,
                    Status = status
                });
        }

        private static string BuildMessage(
            string language,
            IReadOnlyList<WhatsAppCampaignAssetDto> assets,
            string? note,
            bool hasImages)
        {
            var lines = BuildAssetLines(assets).ToList();
            if (lines.Count == 0)
            {
                lines.Add("New stock and offers are available now.");
            }

            var noteLine = string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();
            var imageLine = hasImages ? "Photos are attached for quick review." : string.Empty;

            var english = string.Join(Environment.NewLine, new[]
            {
                "Hello {CustomerName},",
                "New availability from Maalouf Auto Parts:",
                string.Join(Environment.NewLine, lines.Select((line, index) => $"{index + 1}. {line}")),
                imageLine,
                noteLine,
                "Reply here and we will reserve it or send details."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            var arabic = string.Join(Environment.NewLine, new[]
            {
                "مرحبا {CustomerName}،",
                "وصلت خيارات جديدة من Maalouf Auto Parts:",
                string.Join(Environment.NewLine, lines.Select((line, index) => $"{index + 1}. {line}")),
                hasImages ? "الصور مرفقة للمراجعة السريعة." : string.Empty,
                noteLine,
                "إذا مهتم، رد على هذه الرسالة لنحجز لك أو نرسل التفاصيل."
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

            return language switch
            {
                "Arabic" => arabic,
                "English" => english,
                _ => $"{arabic}{Environment.NewLine}{Environment.NewLine}{english}"
            };
        }

        private static IEnumerable<string> BuildAssetLines(IReadOnlyList<WhatsAppCampaignAssetDto> assets)
        {
            foreach (var asset in assets.Take(8))
            {
                var price = asset.Price.HasValue
                    ? $" · {Money(asset.Price.Value, asset.Currency)}"
                    : string.Empty;
                var imageHint = string.Equals(asset.AssetType, "Car", StringComparison.OrdinalIgnoreCase) && asset.ImageCount > 0
                    ? $" · {asset.ImageCount:N0} photo(s)"
                    : string.Empty;

                yield return $"{asset.Title} · {asset.Subtitle}{price}{imageHint}";
            }
        }

        private static string Personalize(string body, string name)
        {
            var safeName = string.IsNullOrWhiteSpace(name) ? "Customer" : name.Trim();
            return body.Replace("{CustomerName}", safeName, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeCampaignName(string? name, string segment)
        {
            var normalized = string.IsNullOrWhiteSpace(name)
                ? $"WhatsApp {SegmentLabel(segment)} {DateTime.UtcNow:yyyy-MM-dd HHmm}"
                : name.Trim();

            return normalized.Length <= 160 ? normalized : normalized[..160];
        }

        private static string SegmentLabel(string segment)
            => segment switch
            {
                WhatsAppCampaignSegments.ActiveCustomers => "active customers",
                WhatsAppCampaignSegments.InactiveCustomers => "inactive customers",
                WhatsAppCampaignSegments.UnpaidCustomers => "unpaid customers",
                WhatsAppCampaignSegments.RecentBuyers => "recent buyers",
                WhatsAppCampaignSegments.WaitingForParts => "waiting customers",
                _ => "customers"
            };

        private static string NormalizeSegment(string? segment)
        {
            var value = string.IsNullOrWhiteSpace(segment) ? WhatsAppCampaignSegments.AllCustomers : segment.Trim();
            return value switch
            {
                WhatsAppCampaignSegments.ActiveCustomers => WhatsAppCampaignSegments.ActiveCustomers,
                WhatsAppCampaignSegments.InactiveCustomers => WhatsAppCampaignSegments.InactiveCustomers,
                WhatsAppCampaignSegments.UnpaidCustomers => WhatsAppCampaignSegments.UnpaidCustomers,
                WhatsAppCampaignSegments.RecentBuyers => WhatsAppCampaignSegments.RecentBuyers,
                WhatsAppCampaignSegments.WaitingForParts => WhatsAppCampaignSegments.WaitingForParts,
                _ => WhatsAppCampaignSegments.AllCustomers
            };
        }

        private static string NormalizeLanguage(string? language)
        {
            var value = string.IsNullOrWhiteSpace(language) ? "ArabicEnglish" : language.Trim();
            return value switch
            {
                "Arabic" => "Arabic",
                "English" => "English",
                _ => "ArabicEnglish"
            };
        }

        private static IReadOnlyList<int> NormalizeIds(IEnumerable<int>? ids)
            => (ids ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .Take(100)
                .ToList();

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

        private static string Money(decimal amount, string? currencyCode)
            => $"{(string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant())} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

        private static bool IsPreparedStatus(string status)
            => string.Equals(status, "Prepared", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Preview", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Sending", StringComparison.OrdinalIgnoreCase);

        private static bool IsFailedStatus(string status)
            => string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase);

        private static string ResolveCampaignStatus(int sentCount, int failedCount, int preparedCount, int total)
        {
            if (failedCount >= total)
            {
                return "Failed";
            }

            if (failedCount > 0)
            {
                return "CompletedWithErrors";
            }

            if (sentCount > 0)
            {
                return "Sent";
            }

            return preparedCount > 0 ? "Prepared" : "Completed";
        }

        private static string ResolveExtension(string mimeType)
            => mimeType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".png"
            };

        private static DataTable ToIntIdList(IEnumerable<int> ids)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));

            foreach (var id in ids.Distinct())
            {
                table.Rows.Add(id);
            }

            return table;
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

    }
}
