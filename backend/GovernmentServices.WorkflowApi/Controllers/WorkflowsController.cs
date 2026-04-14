using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentServices.WorkflowApi.Controllers;

[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly GovernmentWorkflowStore _store;

    public WorkflowsController(GovernmentWorkflowStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<ApiResponse<IReadOnlyList<WorkflowDto>>> GetWorkflows()
    {
        return Ok(_store.GetWorkflows());
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<WorkflowDto>> GetWorkflow(string id)
    {
        var workflow = _store.GetWorkflow(id);
        if (workflow is null)
        {
            return NotFound(new ApiResponse<WorkflowDto>
            {
                Data = null!,
                Success = false,
                Message = "Workflow not found."
            });
        }

        return Ok(new ApiResponse<WorkflowDto>
        {
            Data = workflow
        });
    }

    [HttpGet("{id}/steps")]
    public ActionResult<ApiResponse<IReadOnlyList<WorkflowStepDto>>> GetWorkflowSteps(string id)
    {
        var workflow = _store.GetWorkflow(id);
        if (workflow is null)
        {
            return NotFound(new ApiResponse<IReadOnlyList<WorkflowStepDto>>
            {
                Data = Array.Empty<WorkflowStepDto>(),
                Success = false,
                Message = "Workflow not found."
            });
        }

        return Ok(new ApiResponse<IReadOnlyList<WorkflowStepDto>>
        {
            Data = workflow.Steps
        });
    }

    [HttpPost("{id}/advance")]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> AdvanceWorkflow(
        string id,
        [FromBody] AdvanceWorkflowDto request,
        CancellationToken cancellationToken)
    {
        var workflow = await _store.AdvanceWorkflowAsync(id, request.Notes, cancellationToken);
        if (workflow is null)
        {
            return NotFound(new ApiResponse<WorkflowDto>
            {
                Data = null!,
                Success = false,
                Message = "Workflow not found."
            });
        }

        return Ok(new ApiResponse<WorkflowDto>
        {
            Data = workflow,
            Message = "Workflow advanced successfully."
        });
    }
}
