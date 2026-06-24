using Microsoft.AspNetCore.Mvc;
using RepoAllocation.Api.Application;
using RepoAllocation.Api.Domain;
using RepoAllocation.Api.Models;

namespace RepoAllocation.Api.Controllers;

[ApiController]
[Route("api/allocation")]
public sealed class AllocationController : ControllerBase
{
    private readonly AllocationApplicationService _allocationApplicationService;

    public AllocationController(AllocationApplicationService allocationApplicationService)
    {
        _allocationApplicationService = allocationApplicationService;
    }

    [HttpPost("suggest")]
    public ActionResult<AllocationResponse> Suggest(AllocationRequest request)
    {
        try
        {
            var response = _allocationApplicationService.SuggestAllocation(request);
            return Ok(response);
        }
        catch (AllocationValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("sample-securities")]
    public ActionResult<IReadOnlyList<SecurityInput>> GetSampleSecurities([FromQuery] int count = 1000)
    {
        var securities = _allocationApplicationService.GetSampleSecurities(count);
        return Ok(securities);
    }
}