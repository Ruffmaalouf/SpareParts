namespace SpareParts.Api.Hosting;

public sealed record ServiceProfile(string ServiceName, IReadOnlyCollection<ServiceCapability> Capabilities);
