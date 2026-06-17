namespace SpareParts.Domain.Qr;

public class QrTagDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
}
