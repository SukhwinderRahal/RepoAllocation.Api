using RepoAllocation.Api.Domain;
using RepoAllocation.Api.Models;
using Xunit;

namespace RepoAllocation.Tests;

public sealed class AllocationServiceTests
{
    [Fact]
    public void SuggestAllocation_UsesSingleSecurity_WhenSingleSecurityCoversRequirement()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 200m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.Equal(110m, result.RequiredCollateralValue);
        Assert.Single(result.Allocations);
        Assert.Equal("SEC1", result.Allocations[0].SecurityId);
        Assert.True(result.AllocatedCollateralValue >= 110m);
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public void SuggestAllocation_UsesMultipleSecurities_WhenOneSecurityIsNotEnough()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 150m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1)),
                CreateSecurity(
                    securityId: "SEC2",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 2.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.Equal(165m, result.RequiredCollateralValue);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Contains(result.Allocations, allocation => allocation.SecurityId == "SEC1");
        Assert.Contains(result.Allocations, allocation => allocation.SecurityId == "SEC2");
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public void SuggestAllocation_PartiallyAllocatesLastSecurity_WhenLastSecurityIsOnlyPartlyNeeded()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 150m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1)),
                CreateSecurity(
                    securityId: "SEC2",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 2.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        var sec1Allocation = result.Allocations.Single(allocation => allocation.SecurityId == "SEC1");
        var sec2Allocation = result.Allocations.Single(allocation => allocation.SecurityId == "SEC2");

        Assert.Equal(100m, sec1Allocation.NominalAllocated);
        Assert.True(sec2Allocation.NominalAllocated < 100m);
        Assert.Equal(65m, sec2Allocation.NominalAllocated);
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public void SuggestAllocation_RanksSecurityWithFarCouponBeforeSecurityWithNearCoupon()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 150m,
            marginPercent: 0m,
            settlementDate: new DateOnly(2026, 1, 10),
            securities:
            [
                CreateSecurity(
                    securityId: "SEC_NEAR",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2026, 1, 15)),
                CreateSecurity(
                    securityId: "SEC_FAR",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2026, 3, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.Equal("SEC_FAR", result.Allocations[0].SecurityId);
        Assert.Equal("SEC_NEAR", result.Allocations[1].SecurityId);
    }

    [Fact]
    public void SuggestAllocation_ExcludesSecurity_WhenCouponIsThreeDaysOrLessAway()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            settlementDate: new DateOnly(2026, 1, 10),
            securities:
            [
                CreateSecurity(
                    securityId: "SEC_TOO_CLOSE",
                    nominalAvailable: 200m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2026, 1, 12))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.DoesNotContain(result.Allocations, allocation => allocation.SecurityId == "SEC_TOO_CLOSE");
        Assert.Empty(result.Allocations);
        Assert.True(result.Shortfall > 0);
    }

    [Fact]
    public void SuggestAllocation_RanksLowHaircutSecurityBeforeHighHaircutSecurity()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 150m,
            marginPercent: 0m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC_HIGH_HC",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.10m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1)),
                CreateSecurity(
                    securityId: "SEC_LOW_HC",
                    nominalAvailable: 100m,
                    price: 1.00m,
                    haircutPercent: 0.01m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.Equal("SEC_LOW_HC", result.Allocations[0].SecurityId);
        Assert.Equal("SEC_HIGH_HC", result.Allocations[1].SecurityId);
    }

    [Fact]
    public void SuggestAllocation_ComputesShortfall_WhenCollateralIsInsufficient()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 10m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var result = engine.SuggestAllocation(request);

        Assert.Equal(110m, result.RequiredCollateralValue);
        Assert.True(result.AllocatedCollateralValue < result.RequiredCollateralValue);
        Assert.Equal(10m, result.AllocatedCollateralValue);
        Assert.Equal(100m, result.Shortfall);
    }

    [Fact]
    public void SuggestAllocation_Throws_WhenSecuritiesListIsEmpty()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            securities: []);

        var exception = Assert.Throws<AllocationValidationException>(
            () => engine.SuggestAllocation(request));

        Assert.Equal("Securities list cannot be empty.", exception.Message);
    }

    [Fact]
    public void SuggestAllocation_Throws_WhenSecurityIdIsMissing()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "",
                    nominalAvailable: 200m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var exception = Assert.Throws<AllocationValidationException>(
            () => engine.SuggestAllocation(request));

        Assert.Equal("SecurityId is required.", exception.Message);
    }

    [Fact]
    public void SuggestAllocation_Throws_WhenSettlementDateIsAfterRepoEndDate()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: 100m,
            marginPercent: 10m,
            settlementDate: new DateOnly(2026, 2, 10),
            repoEndDate: new DateOnly(2026, 1, 10),
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 200m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var exception = Assert.Throws<AllocationValidationException>(
            () => engine.SuggestAllocation(request));

        Assert.Equal("Settlement date cannot be after repo end date.", exception.Message);
    }

    [Fact]
    public void SuggestAllocation_Throws_WhenCashAmountIsNegative()
    {
        var engine = new AllocationEngine();

        var request = CreateRequest(
            cashAmount: -100m,
            marginPercent: 10m,
            securities:
            [
                CreateSecurity(
                    securityId: "SEC1",
                    nominalAvailable: 200m,
                    price: 1.00m,
                    haircutPercent: 0.00m,
                    fundingRate: 1.00m,
                    nextCouponDate: new DateOnly(2030, 1, 1))
            ]);

        var exception = Assert.Throws<AllocationValidationException>(
            () => engine.SuggestAllocation(request));

        Assert.Equal("Cash amount cannot be negative.", exception.Message);
    }

    private static AllocationRequest CreateRequest(
        decimal cashAmount,
        decimal marginPercent,
        IReadOnlyList<SecurityInput> securities,
        DateOnly? settlementDate = null,
        DateOnly? repoEndDate = null)
    {
        return new AllocationRequest
        {
            CashAmount = cashAmount,
            Currency = "EUR",
            SettlementDate = settlementDate ?? new DateOnly(2026, 1, 10),
            RepoEndDate = repoEndDate ?? new DateOnly(2026, 2, 10),
            MarginPercent = marginPercent,
            Securities = securities.ToList()
        };
    }

    private static SecurityInput CreateSecurity(
        string securityId,
        decimal nominalAvailable,
        decimal price,
        decimal haircutPercent,
        decimal fundingRate,
        DateOnly nextCouponDate)
    {
        return new SecurityInput
        {
            SecurityId = securityId,
            NominalAvailable = nominalAvailable,
            Price = price,
            HaircutPercent = haircutPercent,
            FundingRate = fundingRate,
            NextCouponDate = nextCouponDate
        };
    }
}