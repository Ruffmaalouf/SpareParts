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
    public interface IApiClient :
        IApiSessionClient,
        IAuthApiClient,
        IUserApiClient,
        IRoleApiClient,
        ICustomerApiClient,
        IWarehouseApiClient,
        ICarCatalogApiClient,
        IPartsApiClient,
        ISalesApiClient,
        ICrudApiClient
    {
    }
}
