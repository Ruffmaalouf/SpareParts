using Dapper;
using SpareParts.Domain.Communications;

namespace SpareParts.Infrastructure.Data
{
    public sealed class OutboundMessagesRepository
    {
        private readonly DbSession _session;

        public OutboundMessagesRepository(DbSession session)
        {
            _session = session;
        }

        public int Insert(OutboundMessageRecord record)
        {
            const string sql = @"INSERT INTO dbo.OutboundMessages
                (Direction, Channel, RecipientKind, RecipientId, RecipientName, RecipientPhone, TemplateKey,
                 ReferenceType, ReferenceId, Body, AttachmentCount, Status, Provider, ProviderMessageId,
                 ProviderStatus, ErrorMessage, CreatedAt, CreatedByUserId, SentAt)
                VALUES
                (@Direction, @Channel, @RecipientKind, @RecipientId, @RecipientName, @RecipientPhone, @TemplateKey,
                 @ReferenceType, @ReferenceId, @Body, @AttachmentCount, @Status, @Provider, @ProviderMessageId,
                 @ProviderStatus, @ErrorMessage, @CreatedAt, @CreatedByUserId, @SentAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return _session.Connection.ExecuteScalar<int>(sql, record, _session.Transaction);
        }

        public void UpdateDeliveryResult(
            int id,
            string status,
            string? provider,
            string? providerMessageId,
            string? providerStatus,
            string? errorMessage,
            DateTime? sentAt)
        {
            const string sql = @"UPDATE dbo.OutboundMessages
                                 SET Status = @Status,
                                     Provider = @Provider,
                                     ProviderMessageId = @ProviderMessageId,
                                     ProviderStatus = @ProviderStatus,
                                     ErrorMessage = @ErrorMessage,
                                     SentAt = @SentAt
                                 WHERE Id = @Id;";

            _session.Connection.Execute(sql, new
            {
                Id = id,
                Status = status,
                Provider = provider,
                ProviderMessageId = providerMessageId,
                ProviderStatus = providerStatus,
                ErrorMessage = errorMessage,
                SentAt = sentAt
            }, _session.Transaction);
        }

        public IReadOnlyList<OutboundMessageLogDto> GetRecent(int take)
        {
            const string sql = @"SELECT TOP (@Take)
                                        Id,
                                        Direction,
                                        Channel,
                                        RecipientKind,
                                        RecipientId,
                                        RecipientName,
                                        RecipientPhone,
                                        TemplateKey,
                                        ReferenceType,
                                        ReferenceId,
                                        Body,
                                        AttachmentCount,
                                        Status,
                                        Provider,
                                        ProviderMessageId,
                                        ProviderStatus,
                                        ErrorMessage,
                                        CreatedAt,
                                        SentAt
                                 FROM dbo.OutboundMessages
                                 ORDER BY CreatedAt DESC, Id DESC;";

            return _session.Connection.Query<OutboundMessageLogDto>(
                sql,
                new { Take = Math.Clamp(take, 1, 200) },
                _session.Transaction).ToList();
        }

        public IReadOnlyList<CommunicationConversationDto> GetConversations(string? search, int take)
        {
            const string sql = @"
WITH FilteredMessages AS
(
    SELECT *
    FROM dbo.OutboundMessages
    WHERE NULLIF(LTRIM(RTRIM(RecipientPhone)), N'') IS NOT NULL
      AND (@Search IS NULL
           OR RecipientName LIKE @Like
           OR RecipientPhone LIKE @Like
           OR Body LIKE @Like)
),
RankedMessages AS
(
    SELECT Id,
           Direction,
           RecipientKind,
           RecipientId,
           RecipientName,
           RecipientPhone,
           Body,
           Status,
           CreatedAt,
           ROW_NUMBER() OVER (PARTITION BY RecipientPhone ORDER BY CreatedAt DESC, Id DESC) AS RowNumber,
           COUNT(1) OVER (PARTITION BY RecipientPhone) AS MessageCount,
           SUM(CASE WHEN Direction = N'Inbound' THEN 1 ELSE 0 END) OVER (PARTITION BY RecipientPhone) AS InboundCount,
           SUM(CASE WHEN Direction = N'Outbound' THEN 1 ELSE 0 END) OVER (PARTITION BY RecipientPhone) AS OutboundCount
    FROM FilteredMessages
)
SELECT TOP (@Take)
       RecipientName,
       RecipientPhone,
       RecipientKind,
       RecipientId,
       LEFT(Body, 180) AS LastMessagePreview,
       Direction AS LastDirection,
       Status AS LastStatus,
       CreatedAt AS LastMessageAt,
       MessageCount,
       InboundCount,
       OutboundCount,
       CAST(0 AS INT) AS UnreadCount
FROM RankedMessages
WHERE RowNumber = 1
ORDER BY CreatedAt DESC, Id DESC;";

            var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            return _session.Connection.Query<CommunicationConversationDto>(
                sql,
                new
                {
                    Search = trimmedSearch,
                    Like = trimmedSearch == null ? null : $"%{trimmedSearch}%",
                    Take = Math.Clamp(take, 1, 200)
                },
                _session.Transaction).ToList();
        }

        public IReadOnlyList<CommunicationConversationMessageDto> GetMessagesByPhone(string phone, int take)
        {
            const string sql = @"
SELECT *
FROM
(
    SELECT TOP (@Take)
           Id,
           Direction,
           Channel,
           RecipientKind,
           RecipientId,
           RecipientName,
           RecipientPhone,
           TemplateKey,
           ReferenceType,
           ReferenceId,
           Body,
           AttachmentCount,
           Status,
           Provider,
           ProviderMessageId,
           ProviderStatus,
           ErrorMessage,
           CreatedAt,
           SentAt
    FROM dbo.OutboundMessages
    WHERE RecipientPhone = @Phone
    ORDER BY CreatedAt DESC, Id DESC
) recent
ORDER BY CreatedAt, Id;";

            return _session.Connection.Query<CommunicationConversationMessageDto>(
                sql,
                new
                {
                    Phone = phone,
                    Take = Math.Clamp(take, 1, 500)
                },
                _session.Transaction).ToList();
        }
    }
}
