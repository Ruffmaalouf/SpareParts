using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Services
{
    internal sealed record CampaignBuild(
        string Segment,
        string Language,
        bool IncludeImages,
        IReadOnlyList<int> PartIds,
        IReadOnlyList<int> UsedCarIds,
        IReadOnlyList<WhatsAppCampaignRecipientDto> Recipients,
        IReadOnlyList<WhatsAppCampaignAssetDto> Assets,
        int AttachmentCount,
        string MessageBody);
}
