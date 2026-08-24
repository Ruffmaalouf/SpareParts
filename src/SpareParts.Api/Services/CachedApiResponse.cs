namespace SpareParts.Api.Services;

/// <summary>
/// One cached composed-read-model response: the payload plus its weak ETag.
/// The ETag is an opaque content hash — it never encodes the tenant id or cache key
/// (Ignition Report 04 §09 zero-leakage rule).
/// </summary>
public sealed class CachedApiResponse<T>
{
    public CachedApiResponse(T payload, string etag)
    {
        Payload = payload;
        ETag = etag;
    }

    public T Payload { get; }
    public string ETag { get; }
}
