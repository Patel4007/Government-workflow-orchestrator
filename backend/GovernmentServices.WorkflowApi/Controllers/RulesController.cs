using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentServices.WorkflowApi.Controllers;

[ApiController]
[Route("api/rules")]
public sealed class RulesController : ControllerBase
{
    private readonly GovernmentWorkflowStore _store;

    public RulesController(GovernmentWorkflowStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<ApiResponse<IReadOnlyList<RuleDto>>> GetRules()
    {
        return Ok(_store.GetRules());
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<RuleDto>> GetRule(string id)
    {
        var rule = _store.GetRule(id);
        if (rule is null)
        {
            return NotFound(new ApiResponse<RuleDto>
            {
                Data = null!,
                Success = false,
                Message = "Rule not found."
            });
        }

        return Ok(new ApiResponse<RuleDto>
        {
            Data = rule
        });
    }

    [HttpPost]
    public ActionResult<ApiResponse<RuleDto>> CreateRule([FromBody] RuleDto request)
    {
        var created = _store.CreateRule(request);
        return CreatedAtAction(nameof(GetRule), new { id = created.Id }, new ApiResponse<RuleDto>
        {
            Data = created,
            Message = "Rule created successfully."
        });
    }

    [HttpPut("{id}")]
    public ActionResult<ApiResponse<RuleDto>> UpdateRule(string id, [FromBody] RuleDto request)
    {
        var updated = _store.UpdateRule(id, request);
        if (updated is null)
        {
            return NotFound(new ApiResponse<RuleDto>
            {
                Data = null!,
                Success = false,
                Message = "Rule not found."
            });
        }

        return Ok(new ApiResponse<RuleDto>
        {
            Data = updated,
            Message = "Rule updated successfully."
        });
    }

    [HttpPost("{id}/toggle")]
    public ActionResult<ApiResponse<RuleDto>> ToggleRule(string id)
    {
        var toggled = _store.ToggleRule(id);
        if (toggled is null)
        {
            return NotFound(new ApiResponse<RuleDto>
            {
                Data = null!,
                Success = false,
                Message = "Rule not found."
            });
        }

        return Ok(new ApiResponse<RuleDto>
        {
            Data = toggled,
            Message = toggled.Enabled ? "Rule activated." : "Rule paused."
        });
    }

    [HttpPost("{id}/test")]
    public ActionResult<ApiResponse<RuleTestResultDto>> TestRule(string id)
    {
        var result = _store.TestRule(id);
        if (result is null)
        {
            return NotFound(new ApiResponse<RuleTestResultDto>
            {
                Data = null!,
                Success = false,
                Message = "Rule not found."
            });
        }

        return Ok(new ApiResponse<RuleTestResultDto>
        {
            Data = result,
            Message = "Rule test completed."
        });
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<object>> DeleteRule(string id)
    {
        if (!_store.DeleteRule(id))
        {
            return NotFound(new ApiResponse<object>
            {
                Data = new { },
                Success = false,
                Message = "Rule not found."
            });
        }

        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Rule deleted successfully."
        });
    }
}
