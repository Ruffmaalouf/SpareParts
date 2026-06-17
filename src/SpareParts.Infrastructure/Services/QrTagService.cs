using System.ComponentModel.DataAnnotations;
using System.Text;
using Dapper;
using SpareParts.Domain.Qr;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class QrTagService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public QrTagService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public QrTagDto Generate(int partId)
    {
        if (partId <= 0)
            throw new ValidationException("A valid part must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var row = session.Connection.QuerySingleOrDefault<PartNameRow>(
            "SELECT Id, Name, InternalCode FROM dbo.Parts WHERE Id = @PartId AND TenantId = @TenantId;",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (row is null)
            throw new NotFoundException($"Part {partId} not found.");

        var publicUrl = $"https://sparepartsapp.com/parts/{partId}";
        var payload = $"part_id={partId}&name={Uri.EscapeDataString(row.Name)}&code={Uri.EscapeDataString(row.InternalCode ?? string.Empty)}&url={Uri.EscapeDataString(publicUrl)}";
        var qrDataUrl = GenerateSvgDataUrl(payload);

        return new QrTagDto
        {
            PartId = partId,
            PartName = row.Name,
            InternalCode = row.InternalCode ?? string.Empty,
            QrPayload = payload,
            QrCodeDataUrl = qrDataUrl,
            PublicUrl = publicUrl
        };
    }

    private static string GenerateSvgDataUrl(string payload)
    {
        var size = 200;
        var svg = new StringBuilder();
        svg.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{size}' height='{size}'>");
        svg.Append($"<rect width='{size}' height='{size}' fill='white'/>");
        svg.Append($"<text x='10' y='20' font-size='8' fill='black'>QR: {System.Security.SecurityElement.Escape(payload[..Math.Min(60, payload.Length)])}</text>");
        svg.Append("</svg>");
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg.ToString()));
        return $"data:image/svg+xml;base64,{base64}";
    }
}
