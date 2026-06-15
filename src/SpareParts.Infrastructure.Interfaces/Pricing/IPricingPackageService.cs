using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IPricingPackageService
    {
        IReadOnlyList<PricingPackageDto> GetAll(bool activeOnly = true);

        PricingPackageDto GetByCode(string code);

        PricingPackageDto GetById(int id);

        int Create(UpsertPricingPackageRequest request, int? userId);

        void Update(int id, UpsertPricingPackageRequest request, int? userId);

        void SetActive(int id, bool isActive, int? userId);
    }
}
