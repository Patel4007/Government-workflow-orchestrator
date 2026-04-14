using GovernmentServices.WorkflowApi.Contracts;

namespace GovernmentServices.WorkflowApi.Models;

internal sealed class CaseRecord
{
    public required string Id { get; init; }
    public required string ApplicantName { get; set; }
    public required string ServiceType { get; set; }
    public required string Status { get; set; }
    public required string Priority { get; set; }
    public required DateTimeOffset SubmittedDate { get; init; }
    public required DateTimeOffset LastUpdated { get; set; }
    public string? AssignedTo { get; set; }
    public required string CurrentStage { get; set; }
    public required List<DocumentRecord> Documents { get; init; }
    public required List<TimelineEventRecord> Timeline { get; init; }
    public required List<RuleEngineOutputRecord> RuleEngineOutputs { get; init; }
    public required CaseMetricsRecord Metrics { get; set; }
    public required string WorkflowId { get; init; }
    public CaseStatisticalInsightsDto? StatisticalInsights { get; set; }
}

internal sealed class WorkflowRecord
{
    public required string Id { get; init; }
    public required string CaseId { get; init; }
    public required string Name { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required List<WorkflowStepRecord> Steps { get; init; }
}

internal sealed class WorkflowStepRecord
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Stage { get; init; }
    public required string Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Description { get; init; }
    public int Order { get; init; }
}

internal sealed class TimelineEventRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Action { get; init; }
    public required string User { get; init; }
    public required string Details { get; init; }
}

public sealed class DocumentRecord
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public required string Status { get; set; }
    public string? Url { get; init; }
    public long? Size { get; init; }
}

internal sealed class RuleRecord
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool Enabled { get; set; }
    public required int Priority { get; set; }
    public required List<RuleConditionRecord> Conditions { get; init; }
    public required List<RuleActionRecord> Actions { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class RuleConditionRecord
{
    public required string Id { get; init; }
    public required string Field { get; set; }
    public required string Operator { get; set; }
    public required string Value { get; set; }
}

internal sealed class RuleActionRecord
{
    public required string Id { get; init; }
    public required string Type { get; set; }
    public required string Target { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}

internal sealed class RuleEngineOutputRecord
{
    public required string RuleId { get; init; }
    public required string RuleName { get; init; }
    public required bool Matched { get; set; }
    public required List<string> ExecutedActions { get; init; }
    public required DateTimeOffset Timestamp { get; set; }
}

internal sealed class CaseMetricsRecord
{
    public required int TimeElapsed { get; set; }
    public required string SlaStatus { get; set; }
    public required double AutomationScore { get; set; }
    public int? DaysRemaining { get; set; }
}

internal sealed class ActivityRecord
{
    public required string Id { get; init; }
    public required string CaseId { get; init; }
    public required string Action { get; init; }
    public required string User { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class VerificationResult
{
    public required bool Passed { get; init; }
    public required double ConfidenceScore { get; init; }
    public required double RawConfidenceScore { get; init; }
    public required string Summary { get; init; }
    public required string Model { get; init; }
    public required IReadOnlyList<string> Findings { get; init; }
    public required ConfidenceCalibrationDto Calibration { get; init; }
    public bool UsedFallback { get; init; }
}
