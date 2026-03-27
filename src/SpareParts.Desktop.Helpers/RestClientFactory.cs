using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class RestClientFactory : IRestClientFactory
    {
        public RestClient Create(string baseUrl)
            => new(new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false
            });
    }
}
