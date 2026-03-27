using RestSharp;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IRestClientFactory
    {
        RestClient Create(string baseUrl);
    }
}
