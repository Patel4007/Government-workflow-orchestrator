using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentServices.WorkflowApi.Controllers;

[ApiController]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly GovernmentWorkflowStore _store;

    public CasesController(GovernmentWorkflowStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<PaginatedResponse<CaseDto>> GetCases([FromQuery] CaseFilterDto filters)
    {
        return Ok(_store.GetCases(filters));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CaseDetailDto>>> GetCase(string id, CancellationToken cancellationToken)
    {
        var result = await _store.GetCaseAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound(new ApiResponse<CaseDetailDto>
            {
                Data = null!,
                Success = false,
                Message = "Case not found.",
                Errors = new[] { $"No case exists for id '{id}'." }
            });
        }

        return Ok(new ApiResponse<CaseDetailDto>
        {
            Data = result
        });
    }

    [HttpPost]
    public ActionResult<ApiResponse<CaseDto>> CreateCase([FromBody] CreateCaseDto request)
    {
        var created = _store.CreateCase(request);
        return CreatedAtAction(nameof(GetCase), new { id = created.Id }, new ApiResponse<CaseDto>
        {
            Data = created,
            Message = "Case created successfully."
        });
    }

    [HttpPut("{id}")]
    public ActionResult<ApiResponse<CaseDto>> UpdateCase(string id, [FromBody] UpdateCaseDto request)
    {
        var updated = _store.UpdateCase(id, request);
        if (updated is null)
        {
            return NotFound(new ApiResponse<CaseDto>
            {
                Data = null!,
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<CaseDto>
        {
            Data = updated,
            Message = "Case updated successfully."
        });
    }

    [HttpPost("{id}/approve")]
    public ActionResult<ApiResponse<object>> ApproveCase(string id, [FromBody] ApproveCaseDto request)
    {
        if (!_store.ApproveCase(id, request.Notes))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Case approved successfully."
        });
    }

    [HttpPost("{id}/reject")]
    public ActionResult<ApiResponse<object>> RejectCase(string id, [FromBody] RejectCaseDto request)
    {
        if (!_store.RejectCase(id, request.Reason))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Case rejected successfully."
        });
    }

    [HttpPost("{id}/request-info")]
    public ActionResult<ApiResponse<object>> RequestInformation(string id, [FromBody] RequestInfoDto request)
    {
        if (!_store.RequestInformation(id, request.Message))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Additional information requested."
        });
    }

    [HttpPost("{id}/assign")]
    public ActionResult<ApiResponse<object>> AssignCase(string id, [FromBody] AssignCaseDto request)
    {
        if (!_store.AssignCase(id, request.AssignTo))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Case assigned successfully."
        });
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<object>> DeleteCase(string id)
    {
        if (!_store.DeleteCase(id))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Case not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Case deleted successfully."
        });
    }
}
