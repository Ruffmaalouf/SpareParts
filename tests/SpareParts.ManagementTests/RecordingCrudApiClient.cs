using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.ManagementTests;

internal sealed class RecordingCrudApiClient : ICrudApiClient
{
    public string? LastMethod { get; private set; }
    public string? LastUrl { get; private set; }
    public object? LastPayload { get; private set; }

    public Task<List<T>> GetAllAsync<T>(string url)
        => Task.FromResult(new List<T>());

    public Task PostAsync(string url, object payload)
    {
        LastMethod = "POST";
        LastUrl = url;
        LastPayload = payload;
        return Task.CompletedTask;
    }

    public Task<TResponse> PostAsync<TResponse>(string url, object payload)
        where TResponse : notnull
    {
        LastMethod = "POST";
        LastUrl = url;
        LastPayload = payload;
        return Task.FromResult(default(TResponse)!);
    }

    public Task PutAsync(string url, object payload)
    {
        LastMethod = "PUT";
        LastUrl = url;
        LastPayload = payload;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string url)
    {
        LastMethod = "DELETE";
        LastUrl = url;
        LastPayload = null;
        return Task.CompletedTask;
    }
}
