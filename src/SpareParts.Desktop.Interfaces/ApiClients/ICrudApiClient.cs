using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface ICrudApiClient
    {
        Task<List<T>> GetAllAsync<T>(string url);
        Task PostAsync(string url, object payload);
        Task<TResponse> PostAsync<TResponse>(string url, object payload)
            where TResponse : notnull;
        Task PutAsync(string url, object payload);
        Task DeleteAsync(string url);
    }
}
