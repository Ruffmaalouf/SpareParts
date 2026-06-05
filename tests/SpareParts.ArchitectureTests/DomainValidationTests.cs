using System.ComponentModel.DataAnnotations;
using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;

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
}
