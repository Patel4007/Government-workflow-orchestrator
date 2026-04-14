# Government Services Workflow Orchestrator

A modern, professional React web application for managing government service workflows with full support for .NET Core backend integration.

## 🏗️ Architecture

This is a **frontend-only React application** designed to integrate seamlessly with a **.NET Core Web API backend**.

```
┌─────────────────────────────────┐
│   React Frontend (This App)     │
│   - TypeScript                   │
│   - Tailwind CSS                 │
│   - React Router                 │
└─────────────┬───────────────────┘
              │ HTTP/REST API
              │ (JSON)
┌─────────────▼───────────────────┐
│   .NET Core Backend API         │
│   - ASP.NET Core Web API        │
│   - Entity Framework Core       │
│   - SQL Server / PostgreSQL     │
└─────────────────────────────────┘
```

## 🚀 Quick Start

### 1. Frontend Setup (This Application)

```bash
# Install dependencies
npm install

# Create environment file
cp env.example .env

# Configure your .NET Core API URL
# Edit .env file:
VITE_API_BASE_URL=https://localhost:5001/api
VITE_USE_MOCK_DATA=false

# Run development server
npm run dev
```

### 2. Backend Setup (.NET Core API)

You need to create a separate .NET Core Web API project. Here's the basic structure:

```bash
# Create new .NET Core Web API project
dotnet new webapi -n GovFlowApi
cd GovFlowApi

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# Run the API
dotnet run
```

## 📡 API Integration

### API Services

All API calls are abstracted into service classes located in `/src/app/services/`:

- **`case.service.ts`** - Case management operations
- **`dashboard.service.ts`** - Dashboard metrics and activity
- **`rule.service.ts`** - Rule engine configuration
- **`auth.service.ts`** - Authentication and authorization

### Example: Using API Services in Components

```typescript
import { useApi } from '../hooks/useApi';
import { caseService } from '../services/case.service';

function MyCases() {
  const { data, loading, error, refetch } = useApi(
    () => caseService.getCases(),
    { immediate: true }
  );

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return <div>{/* Render cases */}</div>;
}
```

## 🔌 .NET Core API Endpoints

Your .NET Core API should implement these endpoints:

### Cases Controller (`/api/cases`)

```csharp
[ApiController]
[Route("api/cases")]
public class CasesController : ControllerBase
{
    // GET /api/cases
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CaseDto>>> GetCases(
        [FromQuery] CaseFilterDto filters)
    {
        // Implementation
    }

    // GET /api/cases/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CaseDetailDto>>> GetCaseById(string id)
    {
        // Implementation
    }

    // POST /api/cases
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CaseDto>>> CreateCase(
        [FromBody] CreateCaseDto dto)
    {
        // Implementation
    }

    // PUT /api/cases/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CaseDto>>> UpdateCase(
        string id, [FromBody] UpdateCaseDto dto)
    {
        // Implementation
    }

    // POST /api/cases/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveCase(
        string id, [FromBody] ApproveRequestDto dto)
    {
        // Implementation
    }

    // POST /api/cases/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectCase(
        string id, [FromBody] RejectRequestDto dto)
    {
        // Implementation
    }
}
```

### Dashboard Controller (`/api/dashboard`)

```csharp
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    // GET /api/dashboard/metrics
    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<DashboardMetricsDto>>> GetMetrics()
    {
        // Implementation
    }

    // GET /api/dashboard/activity
    [HttpGet("activity")]
    public async Task<ActionResult<ApiResponse<List<ActivityDto>>>> GetRecentActivity()
    {
        // Implementation
    }
}
```

### Rules Controller (`/api/rules`)

```csharp
[ApiController]
[Route("api/rules")]
public class RulesController : ControllerBase
{
    // GET /api/rules
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RuleDto>>>> GetRules()
    {
        // Implementation
    }

    // POST /api/rules/{id}/toggle
    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<ApiResponse>> ToggleRule(string id)
    {
        // Implementation
    }
}
```

### Authentication Controller (`/api/auth`)

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto dto)
    {
        // Implementation: Generate JWT token
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenDto dto)
    {
        // Implementation: Refresh JWT token
    }

    // GET /api/auth/user
    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        // Implementation: Return current authenticated user
    }
}
```

## 📦 DTO Models (.NET Core)

Create these C# classes to match the TypeScript interfaces:

```csharp
// DTOs/ApiResponse.cs
public class ApiResponse<T>
{
    public T Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
}

// DTOs/PaginatedResponse.cs
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// DTOs/CaseDto.cs
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

// DTOs/CreateCaseDto.cs
public class CreateCaseDto
{
    [Required]
    public string ApplicantName { get; set; }
    
    [Required]
    public string ServiceType { get; set; }
    
    [Required]
    public string Priority { get; set; }
}

// Add more DTOs as needed...
```

## 🔐 Authentication & Authorization

### JWT Token Flow

1. User logs in via `/api/auth/login`
2. Backend returns JWT token and refresh token
3. Frontend stores tokens in localStorage
4. All subsequent API calls include `Authorization: Bearer <token>` header
5. On token expiry, frontend automatically refreshes using refresh token

### Frontend Token Management

The `ApiClient` class automatically handles:
- Adding JWT tokens to requests
- Detecting 401 Unauthorized responses
- Refreshing expired tokens
- Redirecting to login on refresh failure

```typescript
// Automatic token handling - no manual work needed!
const cases = await caseService.getCases();
```

## 🔧 CORS Configuration (.NET Core)

Add CORS policy to your .NET Core API:

```csharp
// Program.cs or Startup.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// ...

app.UseCors("AllowReactApp");
```

## 🧪 Development Mode (Mock Data)

The application includes **mock data mode** for frontend development without a backend:

```env
# .env file
VITE_USE_MOCK_DATA=true
```

When `VITE_USE_MOCK_DATA=true`, all API services return mock data instead of making real HTTP requests.

## 📝 Custom Hooks

### `useApi` Hook

Handles API calls with loading and error states:

```typescript
const { data, loading, error, refetch } = useApi(
  () => caseService.getCases(),
  { immediate: true }
);
```

### `useMutation` Hook

Handles POST/PUT/DELETE operations:

```typescript
const { mutate, loading, error } = useMutation(
  (id: string) => caseService.approveCase(id)
);

await mutate('CASE-2024-001');
```

## 🎨 Features

### ✅ Implemented
- Dashboard with metrics and charts
- Case management with filtering and search
- Workflow pipeline visualization
- Rule engine configuration
- Document management
- Timeline and activity tracking
- Real-time notifications
- API service layer with TypeScript
- Automatic authentication handling
- Loading and error states
- Mock data mode for development

### 🔜 Ready for Backend Integration
- JWT authentication
- Role-based access control
- File upload for documents
- Real-time WebSocket updates
- Advanced filtering and pagination
- Export functionality

## 📚 Tech Stack

### Frontend (This App)
- **React 18.3** - UI framework
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **React Router** - Navigation
- **Recharts** - Data visualization
- **Radix UI** - Component primitives

### Backend (Your .NET Core API)
- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core** - ORM
- **SQL Server / PostgreSQL** - Database
- **JWT Authentication** - Security
- **Swagger** - API documentation

## 🚦 API Response Format

All API responses follow this standard format:

```json
{
  "data": { /* actual data */ },
  "success": true,
  "message": "Operation completed successfully",
  "errors": []
}
```

Error responses:

```json
{
  "data": null,
  "success": false,
  "message": "An error occurred",
  "errors": [
    "Validation error: ApplicantName is required",
    "Validation error: ServiceType is invalid"
  ]
}
```

## 🔍 Type Safety

All DTOs have matching TypeScript interfaces in `/src/app/types/api.types.ts`. Keep these in sync with your C# models for full type safety.

## 📖 Additional Resources

- [React Documentation](https://react.dev)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [JWT Authentication in .NET](https://jwt.io)

## 🤝 Contributing

When adding new API endpoints:

1. Add endpoint to `/src/app/config/api.config.ts`
2. Create DTO interface in `/src/app/types/api.types.ts`
3. Add service method in appropriate service file
4. Implement corresponding C# controller action
5. Test with both mock data and real API

## 📄 License

MIT
