using RepoAllocation.Api.Domain;
using RepoAllocation.Api.Models;

namespace RepoAllocation.Api.Application;

public sealed class AllocationApplicationService
{
    private readonly AllocationEngine _allocationEngine;
    private readonly SampleSecurityProvider _sampleSecurityProvider;

    public AllocationApplicationService(
        AllocationEngine allocationEngine,
        SampleSecurityProvider sampleSecurityProvider)
    {
        _allocationEngine = allocationEngine;
        _sampleSecurityProvider = sampleSecurityProvider;
    }

    public AllocationResponse SuggestAllocation(AllocationRequest request)
    {
        return _allocationEngine.SuggestAllocation(request);
    }

    public IReadOnlyList<SecurityInput> GetSampleSecurities(int count)
    {
        return _sampleSecurityProvider.GetSecurities(count);
    }
}