namespace RepoAllocation.Api.Models;

public sealed class AllocationResponse
{
    public decimal RequiredCollateralValue { get; set; }

    public decimal AllocatedCollateralValue { get; set; }

    public decimal Shortfall { get; set; }

    public List<AllocationRow> Allocations { get; set; } = new();
}

public sealed class AllocationRow
{
    public string SecurityId { get; set; } = string.Empty;

    public decimal NominalAllocated { get; set; }

    public decimal CollateralValue { get; set; }

    public decimal Score { get; set; }

    public string Reason { get; set; } = string.Empty;
}
