namespace RepoAllocation.Api.Models;

public sealed class AllocationRequest
{
    public decimal CashAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateOnly SettlementDate { get; set; }

    public DateOnly RepoEndDate { get; set; }

    public decimal MarginPercent { get; set; }

    public List<SecurityInput> Securities { get; set; } = new();
}

public sealed class SecurityInput
{
    public string SecurityId { get; set; } = string.Empty;

    public decimal NominalAvailable { get; set; }

    public decimal Price { get; set; }

    public decimal HaircutPercent { get; set; }

    public decimal FundingRate { get; set; }

    public DateOnly NextCouponDate { get; set; }
}