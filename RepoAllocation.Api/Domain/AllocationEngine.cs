using RepoAllocation.Api.Models;

namespace RepoAllocation.Api.Domain;

public sealed class AllocationEngine
{
    public AllocationResponse SuggestAllocation(AllocationRequest request)
    {
        Validate(request);

        var requiredCollateralValue =
            request.CashAmount * (1 + request.MarginPercent / 100m);

        var eligibleSecurities = request.Securities
            .Where(security => IsEligible(security, request.SettlementDate))
            .Select(security => new
            {
                Security = security,
                Score = CalculateScore(security, request.SettlementDate)
            })
            .OrderBy(item => item.Score)
            .ThenBy(item => item.Security.SecurityId)
            .ToList();

        var response = new AllocationResponse
        {
            RequiredCollateralValue = Math.Round(requiredCollateralValue, 2)
        };

        var remainingCollateralValue = requiredCollateralValue;

        foreach (var item in eligibleSecurities)
        {
            if (remainingCollateralValue <= 0)
            {
                break;
            }

            var security = item.Security;

            var availableCollateralValue = CalculateCollateralValue(
                security.NominalAvailable,
                security.Price,
                security.HaircutPercent);

            var collateralValueToUse = Math.Min(
                remainingCollateralValue,
                availableCollateralValue);

            var collateralValuePerNominal =
                security.Price * (1 - security.HaircutPercent / 100m);

            var nominalAllocated = collateralValueToUse / collateralValuePerNominal;

            response.Allocations.Add(new AllocationRow
            {
                SecurityId = security.SecurityId,
                NominalAllocated = Math.Round(nominalAllocated, 2),
                CollateralValue = Math.Round(collateralValueToUse, 2),
                Score = Math.Round(item.Score, 4),
                Reason = BuildReason(security, request.SettlementDate)
            });

            response.AllocatedCollateralValue += collateralValueToUse;
            remainingCollateralValue -= collateralValueToUse;
        }

        response.AllocatedCollateralValue =
            Math.Round(response.AllocatedCollateralValue, 2);

        response.Shortfall =
            Math.Round(Math.Max(0, response.RequiredCollateralValue - response.AllocatedCollateralValue), 2);

        return response;
    }

    private static void Validate(AllocationRequest request)
    {
        if (request.CashAmount < 0)
        {
            throw new AllocationValidationException("Cash amount cannot be negative.");
        }

        if (request.SettlementDate > request.RepoEndDate)
        {
            throw new AllocationValidationException("Settlement date cannot be after repo end date.");
        }

        if (request.Securities.Count == 0)
        {
            throw new AllocationValidationException("Securities list cannot be empty.");
        }

        foreach (var security in request.Securities)
        {
            if (string.IsNullOrWhiteSpace(security.SecurityId))
            {
                throw new AllocationValidationException("SecurityId is required.");
            }

            if (security.NominalAvailable < 0)
            {
                throw new AllocationValidationException("Nominal available cannot be negative.");
            }

            if (security.Price < 0)
            {
                throw new AllocationValidationException("Price cannot be negative.");
            }

            if (security.HaircutPercent < 0)
            {
                throw new AllocationValidationException("Haircut cannot be negative.");
            }
        }
    }

    private static bool IsEligible(SecurityInput security, DateOnly settlementDate)
    {
        var daysToCoupon = security.NextCouponDate.DayNumber - settlementDate.DayNumber;

        return security.NominalAvailable > 0
            && security.Price > 0
            && security.HaircutPercent >= 0
            && daysToCoupon > 3;
    }

    private static decimal CalculateScore(SecurityInput security, DateOnly settlementDate)
    {
        var daysToCoupon = security.NextCouponDate.DayNumber - settlementDate.DayNumber;

        var fundingRateScore = security.FundingRate;
        var haircutPenalty = security.HaircutPercent / 100m;
        var couponPenalty = daysToCoupon <= 30
            ? (30 - daysToCoupon) / 100m
            : 0m;

        return fundingRateScore + haircutPenalty + couponPenalty;
    }

    private static decimal CalculateCollateralValue(
        decimal nominal,
        decimal price,
        decimal haircutPercent)
    {
        // Price is represented as a multiplier:
        // 1.00 = par, 0.98 = 98%, 1.01 = 101%.
        return nominal * price * (1 - haircutPercent / 100m);
    }

    private static string BuildReason(SecurityInput security, DateOnly settlementDate)
    {
        var daysToCoupon = security.NextCouponDate.DayNumber - settlementDate.DayNumber;

        return $"Selected with funding rate {security.FundingRate}, haircut {security.HaircutPercent}%, and {daysToCoupon} days to coupon.";
    }
}