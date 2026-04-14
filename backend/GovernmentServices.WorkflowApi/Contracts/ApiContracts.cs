namespace GovernmentServices.WorkflowApi.Contracts;

public sealed class ApiResponse<T>
{
    public required T Data { get; init; }
    public bool Success { get; init; } = true;
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
}

public sealed class PaginatedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

public class CaseDto
{
    public required string Id { get; init; }
    public required string ApplicantName { get; init; }
    public required string ServiceType { get; init; }
    public required string Status { get; init; }
    public required string Priority { get; init; }
    public required string SubmittedDate { get; init; }
    public required string LastUpdated { get; init; }
    public string? AssignedTo { get; init; }
    public required string CurrentStage { get; init; }
}

public sealed class CaseDetailDto : CaseDto
{
    public required IReadOnlyList<DocumentDto> Documents { get; init; }
    public required IReadOnlyList<TimelineEventDto> Timeline { get; init; }
    public required IReadOnlyList<RuleEngineOutputDto> RuleEngineOutput { get; init; }
    public required CaseMetricsDto Metrics { get; init; }
    public CaseStatisticalInsightsDto? StatisticalInsights { get; init; }
}

public sealed class CaseStatisticalInsightsDto
{
    public required string GeneratedAt { get; init; }
    public required ConfidenceCalibrationDto ConfidenceCalibration { get; init; }
    public required LogisticRegressionPredictionDto LogisticRegression { get; init; }
    public required AftSurvivalPredictionDto AftSurvival { get; init; }
}

public sealed class ConfidenceCalibrationDto
{
    public required string ModelName { get; init; }
    public required string Method { get; init; }
    public double RawConfidenceScore { get; init; }
    public double CalibratedConfidenceScore { get; init; }
    public required string ReliabilityBand { get; init; }
    public required IReadOnlyList<string> Drivers { get; init; }
}

public sealed class LogisticRegressionPredictionDto
{
    public required string ModelName { get; init; }
    public double ManualReviewProbability { get; init; }
    public double ApprovalProbability { get; init; }
    public bool RecommendManualReview { get; init; }
    public bool RecommendAutoApproval { get; init; }
    public required string RecommendedDisposition { get; init; }
    public required IReadOnlyList<string> Drivers { get; init; }
}

public sealed class AftSurvivalPredictionDto
{
    public required string ModelName { get; init; }
    public required string Distribution { get; init; }
    public double MedianCompletionDays { get; init; }
    public double ExpectedCompletionDays { get; init; }
    public double PredictedRemainingDays { get; init; }
    public double SlaBreachProbability { get; init; }
    public required string RiskBand { get; init; }
    public required IReadOnlyList<string> Drivers { get; init; }
}

public sealed class CreateCaseDto
{
    public required string ApplicantName { get; init; }
    public required string ServiceType { get; init; }
    public required string Priority { get; init; }
    public IReadOnlyList<CaseDocumentUploadDto>? Documents { get; init; }
}

public sealed class CaseDocumentUploadDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public long Size { get; init; }
}

public sealed class UpdateCaseDto
{
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? AssignedTo { get; init; }
    public string? Notes { get; init; }
}

public sealed class CaseFilterDto
{
    public string? Status { get; init; }
    public string? ServiceType { get; init; }
    public string? Priority { get; init; }
    public string? AssignedTo { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }
    public string? SearchQuery { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}

public sealed class ApproveCaseDto
{
    public string? Notes { get; init; }
}

public sealed class RejectCaseDto
{
    public required string Reason { get; init; }
}

public sealed class RequestInfoDto
{
    public required string Message { get; init; }
}

public sealed class AssignCaseDto
{
    public required string AssignTo { get; init; }
}

public sealed class WorkflowDto
{
    public required string Id { get; init; }
    public required string CaseId { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<WorkflowStepDto> Steps { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public sealed class WorkflowStepDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Stage { get; init; }
    public required string Status { get; init; }
    public string? AssignedTo { get; init; }
    public string? CompletedAt { get; init; }
    public string? Description { get; init; }
    public int Order { get; init; }
}

public sealed class AdvanceWorkflowDto
{
    public string? Notes { get; init; }
}

public sealed class TimelineEventDto
{
    public required string Id { get; init; }
    public required string Timestamp { get; init; }
    public required string Action { get; init; }
    public required string User { get; init; }
    public required string Details { get; init; }
    public Dictionary<string, object?>? Metadata { get; init; }
}

public sealed class DocumentDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string UploadedAt { get; init; }
    public required string Status { get; init; }
    public string? Url { get; init; }
    public long? Size { get; init; }
}

public sealed class RuleDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool Enabled { get; init; }
    public int Priority { get; init; }
    public required IReadOnlyList<RuleConditionDto> Conditions { get; init; }
    public required IReadOnlyList<RuleActionDto> Actions { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

public sealed class RuleConditionDto
{
    public required string Id { get; init; }
    public required string Field { get; init; }
    public required string Operator { get; init; }
    public required string Value { get; init; }
}

public sealed class RuleActionDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Target { get; init; }
    public Dictionary<string, object?>? Parameters { get; init; }
}

public sealed class RuleEngineOutputDto
{
    public required string RuleId { get; init; }
    public required string RuleName { get; init; }
    public bool Matched { get; init; }
    public required IReadOnlyList<string> ExecutedActions { get; init; }
    public required string Timestamp { get; init; }
}

public sealed class CaseMetricsDto
{
    public int TimeElapsed { get; init; }
    public required string SlaStatus { get; init; }
    public double AutomationScore { get; init; }
    public int? DaysRemaining { get; init; }
}

public sealed class DashboardMetricsDto
{
    public int TotalWorkflows { get; init; }
    public int ActiveCases { get; init; }
    public int PendingApprovals { get; init; }
    public int CompletedProcesses { get; init; }
    public double AvgProcessingTime { get; init; }
    public double SuccessRate { get; init; }
    public required IReadOnlyList<ProcessingDataPointDto> ProcessingData { get; init; }
    public required IReadOnlyList<StatusDistributionDto> StatusDistribution { get; init; }
}

public sealed class ProcessingDataPointDto
{
    public required string Month { get; init; }
    public int Cases { get; init; }
}

public sealed class StatusDistributionDto
{
    public required string Name { get; init; }
    public int Value { get; init; }
    public required string Color { get; init; }
}

public sealed class ActivityDto
{
    public required string Id { get; init; }
    public required string CaseId { get; init; }
    public required string Action { get; init; }
    public required string User { get; init; }
    public required string Timestamp { get; init; }
}

public sealed class LoginDto
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed class RefreshTokenDto
{
    public required string RefreshToken { get; init; }
}

public sealed class AuthResponseDto
{
    public required string Token { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public required UserDto User { get; init; }
}

public sealed class UserDto
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
    public required IReadOnlyList<string> Permissions { get; init; }
}

public sealed class RuleTestResultDto
{
    public bool Matched { get; init; }
    public required IReadOnlyList<string> ExecutedActions { get; init; }
    public required string Summary { get; init; }
}
