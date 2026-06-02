namespace SpareParts.Domain.BusinessPartners;

public enum CustomerPriceTier
{
    Retail = 0,
    Wholesale = 1,
    VIP = 2
}

public class CustomerPriceTierDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomerPriceTier PriceTier { get; set; }
    public string PriceTierName => PriceTier.ToString();
}

public class UpdateCustomerPriceTierRequest
{
    public CustomerPriceTier PriceTier { get; set; }
}
