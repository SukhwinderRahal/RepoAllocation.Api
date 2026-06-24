using RepoAllocation.Api.Models;

namespace RepoAllocation.Api.Application;

public sealed class SampleSecurityProvider
{
    public IReadOnlyList<SecurityInput> GetSecurities(int count)
    {
        var safeCount = Math.Clamp(count, 1, 1000);

        return Enumerable.Range(1, safeCount)
            .Select(index => new SecurityInput
            {
                SecurityId = $"SEC{index:0000}",
                NominalAvailable = 100_000m + (index % 10) * 25_000m,
                Price = 0.95m + (index % 11) * 0.01m,
                HaircutPercent = 1m + (index % 8),
                FundingRate = 0.005m + (index % 20) * 0.001m,
                NextCouponDate = new DateOnly(2030, 1, 1).AddDays(index % 365)
            })
            .ToList();
    }
}
