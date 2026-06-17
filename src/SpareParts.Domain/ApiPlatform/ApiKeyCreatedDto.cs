namespace SpareParts.Domain.ApiPlatform;

public class ApiKeyCreatedDto : ApiKeyDto
{
    public string PlainKey { get; set; } = string.Empty;
}
