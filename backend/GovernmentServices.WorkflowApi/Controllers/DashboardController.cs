using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentServices.WorkflowApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly GovernmentWorkflowStore _store;

    public DashboardController(GovernmentWorkflowStore store)
    {
        _store = store;
    }

    [HttpGet("metrics")]
    public ActionResult<ApiResponse<DashboardMetricsDto>> GetMetrics()
    {
        return Ok(_store.GetDashboardMetrics());
    }

    [HttpGet("activity")]
    public ActionResult<ApiResponse<IReadOnlyList<ActivityDto>>> GetActivity()
    {
        return Ok(_store.GetRecentActivity());
    }
}
