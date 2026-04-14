# .NET Core Backend Setup Guide

This document provides complete C# examples for building the backend API.

## Project Structure

```
GovFlowApi/
├── Controllers/
│   ├── CasesController.cs
│   ├── DashboardController.cs
│   ├── RulesController.cs
│   └── AuthController.cs
├── DTOs/
│   ├── ApiResponse.cs
│   ├── CaseDto.cs
│   ├── RuleDto.cs
│   └── AuthDto.cs
├── Models/
│   ├── Case.cs
│   ├── Rule.cs
│   └── User.cs
├── Services/
│   ├── ICaseService.cs
│   ├── CaseService.cs
│   └── IRuleService.cs
├── Data/
│   └── ApplicationDbContext.cs
└── Program.cs
```

## Complete Example: CasesController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GovFlowApi.DTOs;
using GovFlowApi.Services;

namespace GovFlowApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _caseService;
        private readonly ILogger<CasesController> _logger;

        public CasesController(ICaseService caseService, ILogger<CasesController> logger)
        {
            _caseService = caseService;
            _logger = logger;
        }

        /// <summary>
        /// Get all cases with filtering and pagination
        /// GET /api/cases?status=pending&pageNumber=1&pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<CaseDto>>> GetCases(
            [FromQuery] CaseFilterDto filters)
        {
            try
            {
                var result = await _caseService.GetCasesAsync(filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cases");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving cases",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get case by ID
        /// GET /api/cases/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CaseDetailDto>>> GetCaseById(string id)
        {
            try
            {
                var caseDetail = await _caseService.GetCaseByIdAsync(id);
                
                if (caseDetail == null)
                {
                    return NotFound(new ApiResponse<CaseDetailDto>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<CaseDetailDto>
                {
                    Data = caseDetail,
                    Success = true,
                    Message = "Case retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving case {CaseId}", id);
                return StatusCode(500, new ApiResponse<CaseDetailDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the case"
                });
            }
        }

        /// <summary>
        /// Create new case
        /// POST /api/cases
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CaseDto>>> CreateCase(
            [FromBody] CreateCaseDto dto)
        {
            try
            {
                var createdCase = await _caseService.CreateCaseAsync(dto);

                return CreatedAtAction(
                    nameof(GetCaseById),
                    new { id = createdCase.Id },
                    new ApiResponse<CaseDto>
                    {
                        Data = createdCase,
                        Success = true,
                        Message = "Case created successfully"
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating case");
                return StatusCode(500, new ApiResponse<CaseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the case"
                });
            }
        }

        /// <summary>
        /// Update case
        /// PUT /api/cases/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CaseDto>>> UpdateCase(
            string id,
            [FromBody] UpdateCaseDto dto)
        {
            try
            {
                var updatedCase = await _caseService.UpdateCaseAsync(id, dto);
                
                if (updatedCase == null)
                {
                    return NotFound(new ApiResponse<CaseDto>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<CaseDto>
                {
                    Data = updatedCase,
                    Success = true,
                    Message = "Case updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating case {CaseId}", id);
                return StatusCode(500, new ApiResponse<CaseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the case"
                });
            }
        }

        /// <summary>
        /// Approve case
        /// POST /api/cases/{id}/approve
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Administrator,Approver")]
        public async Task<ActionResult<ApiResponse<object>>> ApproveCase(
            string id,
            [FromBody] ApproveRequestDto dto)
        {
            try
            {
                var success = await _caseService.ApproveCaseAsync(id, dto.Notes);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Case approved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving case {CaseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while approving the case"
                });
            }
        }

        /// <summary>
        /// Reject case
        /// POST /api/cases/{id}/reject
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Administrator,Approver")]
        public async Task<ActionResult<ApiResponse<object>>> RejectCase(
            string id,
            [FromBody] RejectRequestDto dto)
        {
            try
            {
                var success = await _caseService.RejectCaseAsync(id, dto.Reason);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Case rejected successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting case {CaseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while rejecting the case"
                });
            }
        }

        /// <summary>
        /// Request additional information
        /// POST /api/cases/{id}/request-info
        /// </summary>
        [HttpPost("{id}/request-info")]
        public async Task<ActionResult<ApiResponse<object>>> RequestInfo(
            string id,
            [FromBody] RequestInfoDto dto)
        {
            try
            {
                var success = await _caseService.RequestInfoAsync(id, dto.Message);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Information requested successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting info for case {CaseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while requesting information"
                });
            }
        }

        /// <summary>
        /// Assign case
        /// POST /api/cases/{id}/assign
        /// </summary>
        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<ApiResponse<object>>> AssignCase(
            string id,
            [FromBody] AssignCaseDto dto)
        {
            try
            {
                var success = await _caseService.AssignCaseAsync(id, dto.AssignTo);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Case assigned successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning case {CaseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while assigning the case"
                });
            }
        }

        /// <summary>
        /// Delete case
        /// DELETE /api/cases/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCase(string id)
        {
            try
            {
                var success = await _caseService.DeleteCaseAsync(id);
                
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Case with ID {id} not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Case deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case {CaseId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the case"
                });
            }
        }
    }
}
```

## DTOs (Data Transfer Objects)

```csharp
// DTOs/ApiResponse.cs
namespace GovFlowApi.DTOs
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}

// DTOs/PaginatedResponse.cs
namespace GovFlowApi.DTOs
{
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

// DTOs/CaseDto.cs
namespace GovFlowApi.DTOs
{
    public class CaseDto
    {
        public string Id { get; set; }
        public string ApplicantName { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string AssignedTo { get; set; }
        public string CurrentStage { get; set; }
    }

    public class CaseDetailDto : CaseDto
    {
        public List<DocumentDto> Documents { get; set; }
        public List<TimelineEventDto> Timeline { get; set; }
        public List<RuleEngineOutputDto> RuleEngineOutput { get; set; }
        public CaseMetricsDto Metrics { get; set; }
    }

    public class CreateCaseDto
    {
        [Required]
        [StringLength(200)]
        public string ApplicantName { get; set; }
        
        [Required]
        public string ServiceType { get; set; }
        
        [Required]
        public string Priority { get; set; }
    }

    public class UpdateCaseDto
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public string AssignedTo { get; set; }
        public string Notes { get; set; }
    }

    public class CaseFilterDto
    {
        public string Status { get; set; }
        public string ServiceType { get; set; }
        public string Priority { get; set; }
        public string AssignedTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string SearchQuery { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ApproveRequestDto
    {
        public string Notes { get; set; }
    }

    public class RejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }

    public class RequestInfoDto
    {
        [Required]
        public string Message { get; set; }
    }

    public class AssignCaseDto
    {
        [Required]
        public string AssignTo { get; set; }
    }
}
```

## Program.cs Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using GovFlowApi.Data;
using GovFlowApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "GovFlow API", 
        Version = "v1",
        Description = "Government Services Workflow Orchestrator API"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey))
    };
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<IRuleService, RuleService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GovFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "GovFlowApi",
    "Audience": "GovFlowClient",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Database Models

```csharp
// Models/Case.cs
namespace GovFlowApi.Models
{
    public class Case
    {
        public string Id { get; set; }
        public string ApplicantName { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string AssignedTo { get; set; }
        public string CurrentStage { get; set; }
        
        // Navigation properties
        public List<Document> Documents { get; set; }
        public List<TimelineEvent> Timeline { get; set; }
    }
}
```

## Next Steps

1. **Create the .NET Core project** using the structure above
2. **Configure your database** connection string
3. **Run migrations** to create database schema
4. **Test endpoints** using Swagger UI at `https://localhost:5001/swagger`
5. **Update React app** `.env` file to point to your API
6. **Set VITE_USE_MOCK_DATA=false** to use real API

## Testing the Integration

```bash
# Terminal 1: Run .NET Core API
cd GovFlowApi
dotnet run

# Terminal 2: Run React app
npm run dev

# Access:
# - React App: http://localhost:5173
# - API Swagger: https://localhost:5001/swagger
```

The React frontend will now communicate with your .NET Core backend!
