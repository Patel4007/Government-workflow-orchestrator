using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentServices.WorkflowApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly GovernmentWorkflowStore _store;

    public AuthController(GovernmentWorkflowStore store)
    {
        _store = store;
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<AuthResponseDto>> Login([FromBody] LoginDto login)
    {
        var response = _store.Authenticate(login);
        return Ok(new ApiResponse<AuthResponseDto>
        {
            Data = response,
            Message = "Login successful."
        });
    }

    [HttpPost("refresh")]
    public ActionResult<ApiResponse<AuthResponseDto>> Refresh([FromBody] RefreshTokenDto _)
    {
        var response = _store.RefreshToken();
        return Ok(new ApiResponse<AuthResponseDto>
        {
            Data = response,
            Message = "Token refreshed."
        });
    }

    [HttpGet("user")]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        return Ok(new ApiResponse<UserDto>
        {
            Data = _store.GetCurrentUser()
        });
    }

    [HttpPost("logout")]
    public ActionResult<ApiResponse<object>> Logout()
    {
        return Ok(new ApiResponse<object>
        {
            Data = new { },
            Message = "Logged out."
        });
    }
}
