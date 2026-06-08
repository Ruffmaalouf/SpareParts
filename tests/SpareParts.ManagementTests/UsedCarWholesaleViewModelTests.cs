using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class UsedCarWholesaleViewModelTests
{
    [Fact]
    public void Repair_items_reduce_projected_profit_unless_car_is_for_parts()
    {
        var sut = new UsedCarWholesaleViewModel(new RecordingCrudApiClient());
        sut.AvailableCars.Add(new UsedCarDto
        {
            Id = 12,
            Car = "BMW 320i",
            ModelYear = 2018,
            FullCostBase = 8200m,
            BaseCurrencyCode = "USD"
        });
        sut.SelectedUsedCarId = 12;
        sut.SalePrice = 10000m;
        sut.RepairDescription = "Windshield";
        sut.RepairPrice = 350m;

        sut.AddRepairItemCommand.Execute(null);

        Assert.Single(sut.RepairItems);
        Assert.Equal(350m, sut.RepairTotalAmount);
        Assert.Equal(1450m, sut.ProjectedProfitLoss);

        sut.IsForParts = true;

        Assert.False(sut.IsRepairListEnabled);
        Assert.Equal(1800m, sut.ProjectedProfitLoss);
    }

    [Fact]
    public async Task LoadAsync_OnlyShowsUnsoldCarsAsAvailable()
    {
        var api = new SeededCrudApiClient
        {
            UsedCars =
            [
                new UsedCarDto { Id = 1, Car = "BMW 320i", ModelYear = 2018, BaseCurrencyCode = "USD" },
                new UsedCarDto { Id = 2, Car = "Audi A4", ModelYear = 2017, BaseCurrencyCode = "USD", IsWholesaleSold = true }
            ]
        };
        var sut = new UsedCarWholesaleViewModel(api);

        await sut.LoadAsync();

        Assert.Single(sut.AvailableCars);
        Assert.Equal(1, sut.AvailableCars[0].Id);
        Assert.Equal(1, sut.SelectedUsedCarId);
    }

    private sealed class SeededCrudApiClient : ICrudApiClient
    {
        public List<UsedCarDto> UsedCars { get; init; } = [];

        public Task<List<T>> GetAllAsync<T>(string url)
        {
            if (typeof(T) == typeof(UsedCarDto))
            {
                return Task.FromResult(UsedCars.Cast<T>().ToList());
            }

            if (typeof(T) == typeof(CustomerDto))
            {
                return Task.FromResult(new List<T>());
            }

            if (typeof(T) == typeof(UsedCarWholesaleSaleDto))
            {
                return Task.FromResult(new List<T>());
            }

            if (typeof(T) == typeof(AppConstantDto))
            {
                return Task.FromResult(new List<T>());
            }

            return Task.FromResult(new List<T>());
        }

        public Task<T> GetAsync<T>(string url) where T : notnull
            => Task.FromResult(default(T)!);

        public Task PostAsync(string url, object payload) => Task.CompletedTask;

        public Task<TResponse> PostAsync<TResponse>(string url, object payload)
            where TResponse : notnull
            => Task.FromResult(default(TResponse)!);

        public Task PutAsync(string url, object payload) => Task.CompletedTask;

        public Task DeleteAsync(string url) => Task.CompletedTask;
    }
}
