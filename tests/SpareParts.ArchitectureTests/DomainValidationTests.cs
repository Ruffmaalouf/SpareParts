using System.ComponentModel.DataAnnotations;
using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;

namespace SpareParts.ArchitectureTests;

public class DomainValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreatePartRequest_RequiredFields_FailWithoutNameAndCode()
    {
        var request = new CreatePartRequest { Name = "", InternalCode = "", CategoryId = 0 };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreatePartRequest.Name)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreatePartRequest.InternalCode)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreatePartRequest.CategoryId)));
    }

    [Fact]
    public void CreatePartRequest_DefaultCurrency_IsUsd()
    {
        var request = new CreatePartRequest();
        Assert.Equal(PartDefaults.Currency, request.Currency);
    }

    [Fact]
    public void CreatePartRequest_DefaultPricingStatus_IsManual()
    {
        var request = new CreatePartRequest();
        Assert.Equal(PartPricingStatus.Manual, request.PricingStatus);
    }

    [Fact]
    public void CreateCustomerRequest_RequiredName_FailsWhenEmpty()
    {
        var request = new CreateCustomerRequest { Name = "" };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateCustomerRequest.Name)));
    }

    [Fact]
    public void CreateCustomerRequest_ValidRequest_PassesValidation()
    {
        var request = new CreateCustomerRequest { Name = "Valid Customer" };
        var errors = Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateSupplierRequest_RequiredName_FailsWhenEmpty()
    {
        var request = new CreateSupplierRequest { Name = "" };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateSupplierRequest.Name)));
    }

    [Fact]
    public void CreateBrandRequest_RequiredName_FailsWhenEmpty()
    {
        var request = new CreateBrandRequest { Name = "" };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateBrandRequest.Name)));
    }

    [Fact]
    public void CreateCategoryRequest_RequiredName_FailsWhenEmpty()
    {
        var request = new CreateCategoryRequest { Name = "" };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateCategoryRequest.Name)));
    }

    [Fact]
    public void CreateUserRequest_RequiredFields_FailWhenMissing()
    {
        var request = new CreateUserRequest { Username = "", FullName = "", Password = "" };
        var errors = Validate(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateUserRequest.Username)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateUserRequest.FullName)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateUserRequest.Password)));
    }

    [Fact]
    public void PartPricingStatus_Constants_HaveExpectedValues()
    {
        Assert.Equal("Manual", PartPricingStatus.Manual);
        Assert.Equal("Calculated", PartPricingStatus.Calculated);
        Assert.Equal("Locked", PartPricingStatus.Locked);
    }

    [Fact]
    public void PartDefaults_Currency_IsUsd()
    {
        Assert.Equal("USD", PartDefaults.Currency);
    }

    [Fact]
    public void SalesProfitHistoryBuilder_ReturnsOneBucketPerMonth_InChronologicalOrder()
    {
        var asOf = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var points = SalesProfitHistoryBuilder.Build([], months: 3, asOf: asOf);

        Assert.Equal(["2026-04", "2026-05", "2026-06"], points.Select(p => p.Period).ToArray());
        Assert.All(points, p =>
        {
            Assert.Equal(0, p.InvoiceCount);
            Assert.Equal(0m, p.Revenue);
            Assert.Equal(0m, p.Profit);
            Assert.Equal(0m, p.MarginPercent);
        });
    }

    [Fact]
    public void SalesProfitHistoryBuilder_AggregatesRevenueCostAndProfit_PerMonth()
    {
        var asOf = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        SalesProfitHistoryRawLine[] lines =
        [
            new(TransactionId: 1, TransactionDate: new DateTime(2026, 5, 10), Quantity: 2, UnitPrice: 100m, CostPrice: 60m),
            new(TransactionId: 1, TransactionDate: new DateTime(2026, 5, 20), Quantity: 1, UnitPrice: 50m, CostPrice: 20m),
            new(TransactionId: 2, TransactionDate: new DateTime(2026, 6, 1), Quantity: 3, UnitPrice: 10m, CostPrice: 4m),
            new(TransactionId: 99, TransactionDate: new DateTime(2025, 1, 1), Quantity: 5, UnitPrice: 1000m, CostPrice: 1m)
        ];

        var points = SalesProfitHistoryBuilder.Build(lines, months: 2, asOf: asOf);

        Assert.Equal(["2026-05", "2026-06"], points.Select(p => p.Period).ToArray());

        var may = points[0];
        Assert.Equal(1, may.InvoiceCount);
        Assert.Equal(250m, may.Revenue);
        Assert.Equal(140m, may.Cost);
        Assert.Equal(110m, may.Profit);
        Assert.Equal(44m, may.MarginPercent);

        var june = points[1];
        Assert.Equal(1, june.InvoiceCount);
        Assert.Equal(30m, june.Revenue);
        Assert.Equal(12m, june.Cost);
        Assert.Equal(18m, june.Profit);
        Assert.Equal(60m, june.MarginPercent);
    }
}
