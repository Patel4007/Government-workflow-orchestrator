using GovernmentServices.WorkflowApi.Contracts;
using GovernmentServices.WorkflowApi.Models;

namespace GovernmentServices.WorkflowApi.Services;

public sealed class GovernmentWorkflowStore
{
    private const int DefaultPageSize = 10;
    private readonly object _syncRoot = new();
    private readonly IVerificationBotService _verificationBotService;
    private readonly ILogisticRegressionModelService _logisticRegressionModelService;
    private readonly IAftSurvivalModelService _aftSurvivalModelService;
    private readonly List<CaseRecord> _cases;
    private readonly List<WorkflowRecord> _workflows;
    private readonly List<RuleRecord> _rules;
    private readonly List<ActivityRecord> _activities;
    private readonly UserDto _demoUser;

    public GovernmentWorkflowStore(
        IVerificationBotService verificationBotService,
        ILogisticRegressionModelService logisticRegressionModelService,
        IAftSurvivalModelService aftSurvivalModelService)
    {
        _verificationBotService = verificationBotService;
        _logisticRegressionModelService = logisticRegressionModelService;
        _aftSurvivalModelService = aftSurvivalModelService;
        _demoUser = new UserDto
        {
            Id = "user-001",
            Username = "admin",
            Email = "admin@govservices.gov",
            FullName = "Admin User",
            Role = "Administrator",
            Permissions = new[]
            {
                "cases:read",
                "cases:write",
                "rules:manage",
                "users:manage"
            }
        };

        (_cases, _workflows, _rules, _activities) = SeedData();
    }

    public UserDto GetCurrentUser() => _demoUser;

    public AuthResponseDto Authenticate(LoginDto login)
    {
        var username = string.IsNullOrWhiteSpace(login.Username) ? _demoUser.Username : login.Username;
        return BuildAuthResponse(username);
    }

    public AuthResponseDto RefreshToken() => BuildAuthResponse(_demoUser.Username);

    public PaginatedResponse<CaseDto> GetCases(CaseFilterDto? filters)
    {
        lock (_syncRoot)
        {
            IEnumerable<CaseRecord> query = _cases.OrderByDescending(record => record.LastUpdated);

            if (!string.IsNullOrWhiteSpace(filters?.Status))
            {
                query = query.Where(record => record.Status.Equals(filters.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filters?.ServiceType))
            {
                query = query.Where(record => record.ServiceType.Equals(filters.ServiceType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filters?.Priority))
            {
                query = query.Where(record => record.Priority.Equals(filters.Priority, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filters?.AssignedTo))
            {
                query = query.Where(record => string.Equals(record.AssignedTo, filters.AssignedTo, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filters?.SearchQuery))
            {
                query = query.Where(record =>
                    record.Id.Contains(filters.SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    record.ApplicantName.Contains(filters.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            var pageNumber = Math.Max(filters?.PageNumber ?? 1, 1);
            var pageSize = Math.Max(filters?.PageSize ?? DefaultPageSize, 1);
            var totalCount = query.Count();
            var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize), 1);
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapCase)
                .ToArray();

            return new PaginatedResponse<CaseDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
    }

    public async Task<CaseDetailDto?> GetCaseAsync(string id, CancellationToken cancellationToken)
    {
        CaseRecord? record;
        lock (_syncRoot)
        {
            record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        if (record is null)
        {
            return null;
        }

        await EnsureCaseAnalysisAsync(record, cancellationToken);

        lock (_syncRoot)
        {
            return MapCaseDetail(record);
        }
    }

    public CaseDto CreateCase(CreateCaseDto request)
    {
        lock (_syncRoot)
        {
            var nextIndex = _cases.Count + 1;
            var now = DateTimeOffset.UtcNow;
            var caseId = $"CASE-{now:yyyy}-{nextIndex:000}";
            var workflowId = $"workflow-{nextIndex:000}";
            var documents = (request.Documents ?? Array.Empty<CaseDocumentUploadDto>())
                .Select((document, index) => new DocumentRecord
                {
                    Id = $"doc-{nextIndex:000}-{index + 1}",
                    Name = document.Name,
                    Type = document.Type,
                    Size = document.Size,
                    UploadedAt = now,
                    Status = "pending",
                    Url = null
                })
                .ToList();

            var createdCase = new CaseRecord
            {
                Id = caseId,
                ApplicantName = request.ApplicantName,
                ServiceType = request.ServiceType,
                Status = "pending",
                Priority = request.Priority,
                SubmittedDate = now,
                LastUpdated = now,
                AssignedTo = null,
                CurrentStage = "submission",
                Documents = documents,
                Timeline = new List<TimelineEventRecord>
                {
                    BuildTimelineEvent("Case Created", request.ApplicantName, $"New {request.ServiceType} case created.")
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now, now, 0.3),
                WorkflowId = workflowId
            };

            _cases.Add(createdCase);
            _workflows.Add(BuildWorkflowForCase(createdCase));
            AddActivity(caseId, "New Case Submitted", request.ApplicantName, now);

            return MapCase(createdCase);
        }
    }

    public CaseDto? UpdateCase(string id, UpdateCaseDto request)
    {
        lock (_syncRoot)
        {
            var record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (record is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                record.Status = request.Status;
            }

            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                record.Priority = request.Priority;
            }

            if (!string.IsNullOrWhiteSpace(request.AssignedTo))
            {
                record.AssignedTo = request.AssignedTo;
            }

            record.LastUpdated = DateTimeOffset.UtcNow;
            record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, record.Metrics.AutomationScore);

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                record.Timeline.Insert(0, BuildTimelineEvent("Case Updated", _demoUser.FullName, request.Notes));
            }

            record.StatisticalInsights = null;
            AddActivity(record.Id, "Case Updated", _demoUser.FullName, record.LastUpdated);
            return MapCase(record);
        }
    }

    public bool DeleteCase(string id)
    {
        lock (_syncRoot)
        {
            var removed = _cases.RemoveAll(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) > 0;
            _workflows.RemoveAll(item => item.CaseId.Equals(id, StringComparison.OrdinalIgnoreCase));
            _activities.RemoveAll(item => item.CaseId.Equals(id, StringComparison.OrdinalIgnoreCase));
            return removed;
        }
    }

    public bool ApproveCase(string id, string? notes)
    {
        lock (_syncRoot)
        {
            var record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (record is null)
            {
                return false;
            }

            record.Status = "approved";
            record.CurrentStage = "approval";
            record.LastUpdated = DateTimeOffset.UtcNow;
            record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, 0.94);
            record.Timeline.Insert(0, BuildTimelineEvent("Case Approved", _demoUser.FullName, notes ?? "Case approved."));
            record.StatisticalInsights = null;
            AddActivity(record.Id, "Approved", _demoUser.FullName, record.LastUpdated);
            UpdateWorkflowStatus(record.WorkflowId, "approved", "approval");
            return true;
        }
    }

    public bool RejectCase(string id, string reason)
    {
        lock (_syncRoot)
        {
            var record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (record is null)
            {
                return false;
            }

            record.Status = "rejected";
            record.LastUpdated = DateTimeOffset.UtcNow;
            record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, record.Metrics.AutomationScore);
            record.Timeline.Insert(0, BuildTimelineEvent("Case Rejected", _demoUser.FullName, reason));
            record.StatisticalInsights = null;
            AddActivity(record.Id, "Rejected", _demoUser.FullName, record.LastUpdated);
            UpdateWorkflowStatus(record.WorkflowId, "rejected", record.CurrentStage);
            return true;
        }
    }

    public bool RequestInformation(string id, string message)
    {
        lock (_syncRoot)
        {
            var record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (record is null)
            {
                return false;
            }

            record.Status = "pending";
            record.LastUpdated = DateTimeOffset.UtcNow;
            record.Timeline.Insert(0, BuildTimelineEvent("Additional Information Requested", _demoUser.FullName, message));
            record.StatisticalInsights = null;
            AddActivity(record.Id, "Documents Requested", _demoUser.FullName, record.LastUpdated);
            return true;
        }
    }

    public bool AssignCase(string id, string assignTo)
    {
        lock (_syncRoot)
        {
            var record = _cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (record is null)
            {
                return false;
            }

            record.AssignedTo = assignTo;
            record.LastUpdated = DateTimeOffset.UtcNow;
            record.Timeline.Insert(0, BuildTimelineEvent("Case Assigned", "System", $"Case assigned to {assignTo}."));
            record.StatisticalInsights = null;
            AddActivity(record.Id, "Case Assigned", "System", record.LastUpdated);

            var currentStep = _workflows
                .First(item => item.Id == record.WorkflowId)
                .Steps
                .FirstOrDefault(item => item.Status == "in-progress");

            if (currentStep is not null)
            {
                currentStep.AssignedTo = assignTo;
            }

            return true;
        }
    }

    public ApiResponse<DashboardMetricsDto> GetDashboardMetrics()
    {
        lock (_syncRoot)
        {
            var totalWorkflows = _workflows.Count;
            var activeCases = _cases.Count(item => item.Status == "in-progress");
            var pendingApprovals = _cases.Count(item => item.CurrentStage == "approval" && item.Status != "approved");
            var completed = _cases.Count(item => item.Status == "completed" || item.Status == "approved");
            var avgProcessingTime = Math.Round(_cases.Average(item => item.Metrics.TimeElapsed), 1);
            var successRate = _cases.Count == 0
                ? 0
                : Math.Round((_cases.Count(item => item.Status is "completed" or "approved") / (double)_cases.Count) * 100, 1);

            var statusDistribution = _cases
                .GroupBy(item => item.Status)
                .Select(group => new StatusDistributionDto
                {
                    Name = ToDisplayLabel(group.Key),
                    Value = group.Count(),
                    Color = group.Key switch
                    {
                        "completed" or "approved" => "#10b981",
                        "in-progress" => "#3b82f6",
                        "pending" => "#f59e0b",
                        "rejected" => "#ef4444",
                        _ => "#6b7280"
                    }
                })
                .ToArray();

            var processingData = _cases
                .GroupBy(item => item.SubmittedDate.ToString("MMM"))
                .OrderBy(group => DateTime.ParseExact(group.Key, "MMM", null))
                .Select(group => new ProcessingDataPointDto
                {
                    Month = group.Key,
                    Cases = group.Count()
                })
                .ToArray();

            return new ApiResponse<DashboardMetricsDto>
            {
                Data = new DashboardMetricsDto
                {
                    TotalWorkflows = totalWorkflows,
                    ActiveCases = activeCases,
                    PendingApprovals = pendingApprovals,
                    CompletedProcesses = completed,
                    AvgProcessingTime = avgProcessingTime,
                    SuccessRate = successRate,
                    ProcessingData = processingData.Length == 0
                        ? new[]
                        {
                            new ProcessingDataPointDto { Month = "Jan", Cases = 0 }
                        }
                        : processingData,
                    StatusDistribution = statusDistribution.Length == 0
                        ? new[]
                        {
                            new StatusDistributionDto { Name = "Pending", Value = 0, Color = "#f59e0b" }
                        }
                        : statusDistribution
                }
            };
        }
    }

    public ApiResponse<IReadOnlyList<ActivityDto>> GetRecentActivity()
    {
        lock (_syncRoot)
        {
            var activities = _activities
                .OrderByDescending(item => item.Timestamp)
                .Take(8)
                .Select(item => new ActivityDto
                {
                    Id = item.Id,
                    CaseId = item.CaseId,
                    Action = item.Action,
                    User = item.User,
                    Timestamp = ToRelativeTime(item.Timestamp)
                })
                .ToArray();

            return new ApiResponse<IReadOnlyList<ActivityDto>>
            {
                Data = activities
            };
        }
    }

    public ApiResponse<IReadOnlyList<RuleDto>> GetRules()
    {
        lock (_syncRoot)
        {
            return new ApiResponse<IReadOnlyList<RuleDto>>
            {
                Data = _rules.OrderBy(item => item.Priority).Select(MapRule).ToArray()
            };
        }
    }

    public RuleDto? GetRule(string id)
    {
        lock (_syncRoot)
        {
            var rule = _rules.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return rule is null ? null : MapRule(rule);
        }
    }

    public RuleDto CreateRule(RuleDto request)
    {
        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var rule = new RuleRecord
            {
                Id = string.IsNullOrWhiteSpace(request.Id) ? $"rule-{_rules.Count + 1}" : request.Id,
                Name = request.Name,
                Description = request.Description,
                Enabled = request.Enabled,
                Priority = request.Priority,
                CreatedAt = now,
                UpdatedAt = now,
                Conditions = request.Conditions.Select(MapRuleCondition).ToList(),
                Actions = request.Actions.Select(MapRuleAction).ToList()
            };

            _rules.Add(rule);
            return MapRule(rule);
        }
    }

    public RuleDto? UpdateRule(string id, RuleDto request)
    {
        lock (_syncRoot)
        {
            var rule = _rules.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
            {
                return null;
            }

            rule.Name = request.Name;
            rule.Description = request.Description;
            rule.Enabled = request.Enabled;
            rule.Priority = request.Priority;
            rule.UpdatedAt = DateTimeOffset.UtcNow;

            rule.Conditions.Clear();
            rule.Conditions.AddRange(request.Conditions.Select(MapRuleCondition));
            rule.Actions.Clear();
            rule.Actions.AddRange(request.Actions.Select(MapRuleAction));

            return MapRule(rule);
        }
    }

    public RuleDto? ToggleRule(string id)
    {
        lock (_syncRoot)
        {
            var rule = _rules.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
            {
                return null;
            }

            rule.Enabled = !rule.Enabled;
            rule.UpdatedAt = DateTimeOffset.UtcNow;
            return MapRule(rule);
        }
    }

    public bool DeleteRule(string id)
    {
        lock (_syncRoot)
        {
            return _rules.RemoveAll(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) > 0;
        }
    }

    public RuleTestResultDto? TestRule(string id)
    {
        lock (_syncRoot)
        {
            var rule = _rules.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
            {
                return null;
            }

            var sampleCase = _cases.OrderByDescending(item => item.LastUpdated).First();
            var matched = RuleMatchesCase(rule, sampleCase, 0.9);
            return new RuleTestResultDto
            {
                Matched = matched,
                ExecutedActions = matched
                    ? rule.Actions.Select(item => $"{ToDisplayLabel(item.Type)} -> {item.Target}").ToArray()
                    : new[] { "No actions executed." },
                Summary = matched
                    ? $"Rule '{rule.Name}' matched sample case {sampleCase.Id}."
                    : $"Rule '{rule.Name}' did not match sample case {sampleCase.Id}."
            };
        }
    }

    public ApiResponse<IReadOnlyList<WorkflowDto>> GetWorkflows()
    {
        lock (_syncRoot)
        {
            return new ApiResponse<IReadOnlyList<WorkflowDto>>
            {
                Data = _workflows
                    .OrderByDescending(item => item.UpdatedAt)
                    .Select(MapWorkflow)
                    .ToArray()
            };
        }
    }

    public WorkflowDto? GetWorkflow(string id)
    {
        lock (_syncRoot)
        {
            var workflow = _workflows.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return workflow is null ? null : MapWorkflow(workflow);
        }
    }

    public async Task<WorkflowDto?> AdvanceWorkflowAsync(string id, string? notes, CancellationToken cancellationToken)
    {
        WorkflowRecord? workflow;
        CaseRecord? record;

        lock (_syncRoot)
        {
            workflow = _workflows.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            record = workflow is null ? null : _cases.FirstOrDefault(item => item.Id == workflow.CaseId);
        }

        if (workflow is null || record is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        lock (_syncRoot)
        {
            var currentStep = workflow.Steps.FirstOrDefault(item => item.Status == "in-progress")
                ?? workflow.Steps.FirstOrDefault(item => item.Status == "pending");

            if (currentStep is null)
            {
                return MapWorkflow(workflow);
            }

            currentStep.Status = "completed";
            currentStep.CompletedAt = now;
            workflow.UpdatedAt = now;
            record.LastUpdated = now;
            record.CurrentStage = currentStep.Stage;
            record.Status = currentStep.Stage is "completion" ? "completed" : "in-progress";

            var nextStep = workflow.Steps
                .Where(item => item.Order > currentStep.Order)
                .OrderBy(item => item.Order)
                .FirstOrDefault();

            if (nextStep is null)
            {
                workflow.Status = "completed";
                record.Status = "completed";
                record.CurrentStage = "completion";
            }
            else
            {
                nextStep.Status = "in-progress";
                record.CurrentStage = nextStep.Stage;
                workflow.Status = nextStep.Status;
            }

            record.Timeline.Insert(0, BuildTimelineEvent("Workflow Advanced", _demoUser.FullName, notes ?? $"Advanced past {currentStep.Name}."));
            record.StatisticalInsights = null;
            AddActivity(record.Id, "Workflow Advanced", _demoUser.FullName, now);
        }

        if (record.CurrentStage == "verification")
        {
            await EnsureCaseAnalysisAsync(record, cancellationToken);

            lock (_syncRoot)
            {
                var verificationStep = workflow.Steps.FirstOrDefault(item => item.Stage == "verification");
                if (verificationStep is not null && record.RuleEngineOutputs.Any(item => item.RuleId == "verification-bot" && item.Matched))
                {
                    verificationStep.Status = "completed";
                    verificationStep.CompletedAt = DateTimeOffset.UtcNow;

                    var nextPending = workflow.Steps
                        .Where(item => item.Order > verificationStep.Order)
                        .OrderBy(item => item.Order)
                        .FirstOrDefault(item => item.Status == "pending");

                    if (nextPending is not null)
                    {
                        nextPending.Status = "in-progress";
                        record.CurrentStage = nextPending.Stage;
                        record.Status = "in-progress";
                    }
                }
            }
        }

        lock (_syncRoot)
        {
            workflow.UpdatedAt = DateTimeOffset.UtcNow;
            record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, record.Metrics.AutomationScore);
            return MapWorkflow(workflow);
        }
    }

    private async Task EnsureCaseAnalysisAsync(CaseRecord record, CancellationToken cancellationToken)
    {
        VerificationResult verification;

        lock (_syncRoot)
        {
            if (record.RuleEngineOutputs.Any(item => item.RuleId == "verification-bot")
                && record.Documents.All(item => item.Status != "pending")
                && record.StatisticalInsights is not null)
            {
                record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, record.Metrics.AutomationScore);
                return;
            }
        }

        verification = await _verificationBotService.VerifyAsync(
            record.Id,
            record.ApplicantName,
            record.ServiceType,
            record.Documents,
            cancellationToken);

        lock (_syncRoot)
        {
            foreach (var document in record.Documents)
            {
                document.Status = verification.Passed ? "verified" : document.Status == "pending" ? "pending" : document.Status;
            }

            record.RuleEngineOutputs.RemoveAll(item =>
                item.RuleId == "verification-bot"
                || item.RuleId == "confidence-calibration-model"
                || item.RuleId == "logistic-regression-model"
                || item.RuleId == "aft-survival-model"
                || item.RuleId.StartsWith("rule-", StringComparison.OrdinalIgnoreCase));
            record.RuleEngineOutputs.Add(new RuleEngineOutputRecord
            {
                RuleId = "verification-bot",
                RuleName = "Qwen Verification Bot",
                Matched = verification.Passed,
                ExecutedActions = new List<string>(
                    new[]
                    {
                        verification.Summary,
                        $"Raw confidence score: {Math.Round(verification.RawConfidenceScore * 100)}%",
                        $"Calibrated confidence score: {Math.Round(verification.ConfidenceScore * 100)}%",
                        $"Model: {verification.Model}"
                    }
                    .Concat(verification.Calibration.Drivers.Select(driver => $"Calibration: {driver}"))
                    .Concat(verification.Findings)
                ),
                Timestamp = DateTimeOffset.UtcNow
            });

            record.RuleEngineOutputs.Add(new RuleEngineOutputRecord
            {
                RuleId = "confidence-calibration-model",
                RuleName = "Confidence Calibration Model",
                Matched = verification.ConfidenceScore >= 0.65,
                ExecutedActions = new List<string>(
                    new[]
                    {
                        $"Method: {verification.Calibration.Method}",
                        $"Reliability band: {verification.Calibration.ReliabilityBand}",
                        $"Raw verifier confidence: {Math.Round(verification.Calibration.RawConfidenceScore * 100)}%",
                        $"Calibrated verifier confidence: {Math.Round(verification.Calibration.CalibratedConfidenceScore * 100)}%"
                    }.Concat(verification.Calibration.Drivers)
                ),
                Timestamp = DateTimeOffset.UtcNow
            });

            foreach (var rule in _rules.Where(item => item.Enabled).OrderBy(item => item.Priority))
            {
                var matched = RuleMatchesCase(rule, record, verification.ConfidenceScore);
                if (!matched)
                {
                    continue;
                }

                record.RuleEngineOutputs.Add(new RuleEngineOutputRecord
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Matched = true,
                    ExecutedActions = rule.Actions.Select(item => $"{ToDisplayLabel(item.Type)} -> {item.Target}").ToList(),
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            UpdateStatisticalInsights(record, verification);
            record.Timeline.Insert(0, BuildTimelineEvent(
                "Documents Verified",
                verification.UsedFallback ? "Verification Fallback" : "Verification Bot",
                $"{verification.Summary} Confidence {Math.Round(verification.ConfidenceScore * 100)}%."));
            AddActivity(record.Id, "Verification Completed", verification.UsedFallback ? "Verification Fallback" : "Verification Bot", DateTimeOffset.UtcNow);
        }
    }

    private void UpdateStatisticalInsights(CaseRecord record, VerificationResult verification)
    {
        var features = BuildFeatureVector(record, verification);
        var logisticPrediction = _logisticRegressionModelService.Predict(features);
        var aftPrediction = _aftSurvivalModelService.Predict(features);

        record.StatisticalInsights = new CaseStatisticalInsightsDto
        {
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O"),
            ConfidenceCalibration = verification.Calibration,
            LogisticRegression = logisticPrediction,
            AftSurvival = aftPrediction
        };

        record.RuleEngineOutputs.Add(new RuleEngineOutputRecord
        {
            RuleId = "logistic-regression-model",
            RuleName = "Logistic Regression Triage Model",
            Matched = logisticPrediction.RecommendManualReview || logisticPrediction.RecommendAutoApproval,
            ExecutedActions = new List<string>(
                new[]
                {
                    $"Manual review probability: {Math.Round(logisticPrediction.ManualReviewProbability * 100)}%",
                    $"Approval probability: {Math.Round(logisticPrediction.ApprovalProbability * 100)}%",
                    $"Recommended disposition: {logisticPrediction.RecommendedDisposition}"
                }.Concat(logisticPrediction.Drivers)
            ),
            Timestamp = DateTimeOffset.UtcNow
        });

        record.RuleEngineOutputs.Add(new RuleEngineOutputRecord
        {
            RuleId = "aft-survival-model",
            RuleName = "AFT Survival Model",
            Matched = aftPrediction.SlaBreachProbability >= 0.3,
            ExecutedActions = new List<string>(
                new[]
                {
                    $"Median completion time: {aftPrediction.MedianCompletionDays} days",
                    $"Expected completion time: {aftPrediction.ExpectedCompletionDays} days",
                    $"Predicted remaining time: {aftPrediction.PredictedRemainingDays} days",
                    $"SLA breach probability: {Math.Round(aftPrediction.SlaBreachProbability * 100)}%"
                }.Concat(aftPrediction.Drivers)
            ),
            Timestamp = DateTimeOffset.UtcNow
        });

        record.Metrics = BuildMetrics(record.SubmittedDate, record.LastUpdated, verification.ConfidenceScore);
        record.Metrics.SlaStatus = aftPrediction.SlaBreachProbability switch
        {
            >= 0.6 => "High Risk of Breach",
            >= 0.3 => "At Risk of Breach",
            _ => "Within SLA"
        };
    }

    private static StatisticalFeatureVector BuildFeatureVector(CaseRecord record, VerificationResult verification)
    {
        return new StatisticalFeatureVector
        {
            CaseId = record.Id,
            ServiceType = record.ServiceType,
            Priority = record.Priority,
            CurrentStage = record.CurrentStage,
            Status = record.Status,
            ElapsedDays = Math.Max((DateTimeOffset.UtcNow - record.SubmittedDate).TotalDays, 0),
            DocumentCount = record.Documents.Count,
            VerifiedDocumentCount = record.Documents.Count(document => document.Status.Equals("verified", StringComparison.OrdinalIgnoreCase)),
            PendingDocumentCount = record.Documents.Count(document => document.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)),
            RejectedDocumentCount = record.Documents.Count(document => document.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase)),
            IsAssigned = !string.IsNullOrWhiteSpace(record.AssignedTo),
            UsedFallbackVerification = verification.UsedFallback,
            RawVerificationConfidence = verification.RawConfidenceScore,
            CalibratedVerificationConfidence = verification.ConfidenceScore
        };
    }

    private static bool RuleMatchesCase(RuleRecord rule, CaseRecord record, double documentConfidence)
    {
        return rule.Conditions.All(condition =>
        {
            var fieldValue = condition.Field switch
            {
                "serviceType" => record.ServiceType,
                "documentConfidence" => documentConfidence.ToString("0.00"),
                "hasChanges" => "false",
                "income" => record.Priority is "high" or "critical" ? "175000" : "65000",
                "veteranStatus" => record.ServiceType == "benefit-approval" ? "true" : "false",
                _ => string.Empty
            };

            return condition.Operator switch
            {
                "equals" => string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                "greaterThan" => decimal.TryParse(fieldValue, out var currentValue) &&
                                 decimal.TryParse(condition.Value, out var expectedMin) &&
                                 currentValue > expectedMin,
                "lessThan" => decimal.TryParse(fieldValue, out var currentNumeric) &&
                              decimal.TryParse(condition.Value, out var expectedMax) &&
                              currentNumeric < expectedMax,
                _ => false
            };
        });
    }

    private void UpdateWorkflowStatus(string workflowId, string status, string stage)
    {
        var workflow = _workflows.FirstOrDefault(item => item.Id == workflowId);
        if (workflow is null)
        {
            return;
        }

        workflow.Status = status;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var step in workflow.Steps)
        {
            if (step.Stage == stage)
            {
                step.Status = status == "approved" ? "approved" : status == "rejected" ? "rejected" : step.Status;
                step.CompletedAt ??= DateTimeOffset.UtcNow;
            }
        }
    }

    private void AddActivity(string caseId, string action, string user, DateTimeOffset timestamp)
    {
        _activities.Add(new ActivityRecord
        {
            Id = $"act-{Guid.NewGuid():N}"[..10],
            CaseId = caseId,
            Action = action,
            User = user,
            Timestamp = timestamp
        });
    }

    private AuthResponseDto BuildAuthResponse(string username)
    {
        return new AuthResponseDto
        {
            Token = $"demo-jwt-{Guid.NewGuid():N}",
            RefreshToken = $"demo-refresh-{Guid.NewGuid():N}",
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = _demoUser.Id,
                Username = username,
                Email = _demoUser.Email,
                FullName = _demoUser.FullName,
                Role = _demoUser.Role,
                Permissions = _demoUser.Permissions
            }
        };
    }

    private static CaseDto MapCase(CaseRecord record) =>
        new()
        {
            Id = record.Id,
            ApplicantName = record.ApplicantName,
            ServiceType = record.ServiceType,
            Status = record.Status,
            Priority = record.Priority,
            SubmittedDate = record.SubmittedDate.ToString("O"),
            LastUpdated = record.LastUpdated.ToString("O"),
            AssignedTo = record.AssignedTo,
            CurrentStage = record.CurrentStage
        };

    private static CaseDetailDto MapCaseDetail(CaseRecord record) =>
        new()
        {
            Id = record.Id,
            ApplicantName = record.ApplicantName,
            ServiceType = record.ServiceType,
            Status = record.Status,
            Priority = record.Priority,
            SubmittedDate = record.SubmittedDate.ToString("O"),
            LastUpdated = record.LastUpdated.ToString("O"),
            AssignedTo = record.AssignedTo,
            CurrentStage = record.CurrentStage,
            Documents = record.Documents.Select(MapDocument).ToArray(),
            Timeline = record.Timeline.OrderByDescending(item => item.Timestamp).Select(MapTimeline).ToArray(),
            RuleEngineOutput = record.RuleEngineOutputs.OrderByDescending(item => item.Timestamp).Select(MapRuleEngineOutput).ToArray(),
            Metrics = new CaseMetricsDto
            {
                TimeElapsed = record.Metrics.TimeElapsed,
                SlaStatus = record.Metrics.SlaStatus,
                AutomationScore = record.Metrics.AutomationScore,
                DaysRemaining = record.Metrics.DaysRemaining
            },
            StatisticalInsights = record.StatisticalInsights
        };

    private static DocumentDto MapDocument(DocumentRecord record) =>
        new()
        {
            Id = record.Id,
            Name = record.Name,
            Type = record.Type,
            UploadedAt = record.UploadedAt.ToString("O"),
            Status = record.Status,
            Url = record.Url,
            Size = record.Size
        };

    private static TimelineEventDto MapTimeline(TimelineEventRecord record) =>
        new()
        {
            Id = record.Id,
            Timestamp = record.Timestamp.ToString("O"),
            Action = record.Action,
            User = record.User,
            Details = record.Details
        };

    private static RuleEngineOutputDto MapRuleEngineOutput(RuleEngineOutputRecord record) =>
        new()
        {
            RuleId = record.RuleId,
            RuleName = record.RuleName,
            Matched = record.Matched,
            ExecutedActions = record.ExecutedActions.ToArray(),
            Timestamp = record.Timestamp.ToString("O")
        };

    private static WorkflowDto MapWorkflow(WorkflowRecord record) =>
        new()
        {
            Id = record.Id,
            CaseId = record.CaseId,
            Name = record.Name,
            Status = record.Status,
            CreatedAt = record.CreatedAt.ToString("O"),
            UpdatedAt = record.UpdatedAt.ToString("O"),
            Steps = record.Steps.OrderBy(item => item.Order).Select(MapWorkflowStep).ToArray()
        };

    private static WorkflowStepDto MapWorkflowStep(WorkflowStepRecord record) =>
        new()
        {
            Id = record.Id,
            Name = record.Name,
            Stage = record.Stage,
            Status = record.Status,
            AssignedTo = record.AssignedTo,
            CompletedAt = record.CompletedAt?.ToString("O"),
            Description = record.Description,
            Order = record.Order
        };

    private static RuleDto MapRule(RuleRecord record) =>
        new()
        {
            Id = record.Id,
            Name = record.Name,
            Description = record.Description,
            Enabled = record.Enabled,
            Priority = record.Priority,
            Conditions = record.Conditions.Select(condition => new RuleConditionDto
            {
                Id = condition.Id,
                Field = condition.Field,
                Operator = condition.Operator,
                Value = condition.Value
            }).ToArray(),
            Actions = record.Actions.Select(action => new RuleActionDto
            {
                Id = action.Id,
                Type = action.Type,
                Target = action.Target,
                Parameters = action.Parameters
            }).ToArray(),
            CreatedAt = record.CreatedAt.ToString("O"),
            UpdatedAt = record.UpdatedAt.ToString("O")
        };

    private static RuleConditionRecord MapRuleCondition(RuleConditionDto request) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(request.Id) ? $"cond-{Guid.NewGuid():N}"[..10] : request.Id,
            Field = request.Field,
            Operator = request.Operator,
            Value = request.Value
        };

    private static RuleActionRecord MapRuleAction(RuleActionDto request) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(request.Id) ? $"act-{Guid.NewGuid():N}"[..10] : request.Id,
            Type = request.Type,
            Target = request.Target,
            Parameters = request.Parameters
        };

    private static CaseMetricsRecord BuildMetrics(DateTimeOffset submittedDate, DateTimeOffset now, double automationScore)
    {
        var elapsedDays = Math.Max((int)Math.Ceiling((now - submittedDate).TotalDays), 0);
        var daysRemaining = Math.Max(30 - elapsedDays, 0);
        var normalizedAutomationScore = automationScore <= 1 ? automationScore * 100 : automationScore;
        return new CaseMetricsRecord
        {
            TimeElapsed = elapsedDays,
            SlaStatus = daysRemaining > 0 ? "Within SLA" : "SLA Breached",
            AutomationScore = Math.Round(normalizedAutomationScore, 1),
            DaysRemaining = daysRemaining
        };
    }

    private static TimelineEventRecord BuildTimelineEvent(string action, string user, string details) =>
        new()
        {
            Id = $"evt-{Guid.NewGuid():N}"[..10],
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            User = user,
            Details = details
        };

    private static string ToRelativeTime(DateTimeOffset timestamp)
    {
        var delta = DateTimeOffset.UtcNow - timestamp;
        if (delta.TotalMinutes < 1)
        {
            return "just now";
        }

        if (delta.TotalHours < 1)
        {
            return $"{Math.Max((int)delta.TotalMinutes, 1)} minutes ago";
        }

        if (delta.TotalDays < 1)
        {
            return $"{Math.Max((int)delta.TotalHours, 1)} hours ago";
        }

        return $"{Math.Max((int)delta.TotalDays, 1)} days ago";
    }

    private static string ToDisplayLabel(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static (List<CaseRecord> Cases, List<WorkflowRecord> Workflows, List<RuleRecord> Rules, List<ActivityRecord> Activities) SeedData()
    {
        var now = DateTimeOffset.UtcNow;
        var seedCases = new List<CaseRecord>
        {
            new()
            {
                Id = "CASE-2024-001",
                ApplicantName = "Sarah Johnson",
                ServiceType = "tax-filing",
                Status = "in-progress",
                Priority = "high",
                SubmittedDate = now.AddDays(-24),
                LastUpdated = now.AddMinutes(-5),
                AssignedTo = "Tax Review Team",
                CurrentStage = "review",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-1", Name = "Tax Form 1040.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-1), Status = "verified", Size = 481231 },
                    new() { Id = "doc-2", Name = "W-2 Form 2023.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-1), Status = "verified", Size = 292312 },
                    new() { Id = "doc-3", Name = "Schedule C.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-1), Status = "pending", Size = 187221 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-1", Timestamp = now.AddMinutes(-5), Action = "Case Assigned", User = "System", Details = "Case automatically assigned to Tax Review Team based on workload." },
                    new() { Id = "evt-2", Timestamp = now.AddHours(-2), Action = "Documents Uploaded", User = "Sarah Johnson", Details = "Uploaded tax filing documents for the current submission." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-24), now, 0.76),
                WorkflowId = "workflow-001"
            },
            new()
            {
                Id = "CASE-2024-002",
                ApplicantName = "Michael Chen",
                ServiceType = "benefit-approval",
                Status = "pending",
                Priority = "critical",
                SubmittedDate = now.AddDays(-3),
                LastUpdated = now.AddHours(-1),
                AssignedTo = "Benefits Processing",
                CurrentStage = "verification",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-4", Name = "Benefit Eligibility.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-2), Status = "pending", Size = 210002 },
                    new() { Id = "doc-5", Name = "Identity Verification.jpg", Type = "image/jpeg", UploadedAt = now.AddDays(-2), Status = "pending", Size = 145223 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-3", Timestamp = now.AddHours(-1), Action = "Additional Information Requested", User = "Emily Davis", Details = "Need updated veteran status evidence." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-3), now, 0.65),
                WorkflowId = "workflow-002"
            },
            new()
            {
                Id = "CASE-2024-003",
                ApplicantName = "Emily Rodriguez",
                ServiceType = "document-verification",
                Status = "approved",
                Priority = "medium",
                SubmittedDate = now.AddDays(-12),
                LastUpdated = now.AddHours(-6),
                AssignedTo = "Document Services",
                CurrentStage = "completion",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-6", Name = "Identity Verification.jpg", Type = "image/jpeg", UploadedAt = now.AddDays(-11), Status = "verified", Size = 125331 },
                    new() { Id = "doc-7", Name = "Address Verification.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-11), Status = "verified", Size = 332181 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-4", Timestamp = now.AddHours(-6), Action = "Documents Verified", User = "Verification Bot", Details = "All required documents verified successfully." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-12), now, 0.92),
                WorkflowId = "workflow-003"
            },
            new()
            {
                Id = "CASE-2024-004",
                ApplicantName = "David Williams",
                ServiceType = "license-renewal",
                Status = "rejected",
                Priority = "low",
                SubmittedDate = now.AddDays(-6),
                LastUpdated = now.AddDays(-3),
                AssignedTo = "Licensing Department",
                CurrentStage = "review",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-8", Name = "License Renewal Application.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-5), Status = "rejected", Size = 197663 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-5", Timestamp = now.AddDays(-3), Action = "Case Rejected", User = "Licensing Officer", Details = "Application contained incomplete supporting documentation." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-6), now, 0.22),
                WorkflowId = "workflow-004"
            },
            new()
            {
                Id = "CASE-2024-005",
                ApplicantName = "Jessica Martinez",
                ServiceType = "tax-filing",
                Status = "completed",
                Priority = "medium",
                SubmittedDate = now.AddDays(-28),
                LastUpdated = now.AddDays(-15),
                AssignedTo = "Tax Review Team",
                CurrentStage = "completion",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-9", Name = "Tax Filing Packet.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-27), Status = "verified", Size = 390002 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-6", Timestamp = now.AddDays(-15), Action = "Case Completed", User = "System", Details = "Tax filing completed and archived." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-28), now, 0.88),
                WorkflowId = "workflow-005"
            },
            new()
            {
                Id = "CASE-2024-006",
                ApplicantName = "Robert Anderson",
                ServiceType = "benefit-approval",
                Status = "in-progress",
                Priority = "high",
                SubmittedDate = now.AddDays(-7),
                LastUpdated = now.AddMinutes(-15),
                AssignedTo = "Benefits Processing",
                CurrentStage = "approval",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-10", Name = "Benefit Claim.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-6), Status = "verified", Size = 223331 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-7", Timestamp = now.AddMinutes(-15), Action = "Review Completed", User = "John Smith", Details = "Manual review completed and ready for approval." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-7), now, 0.71),
                WorkflowId = "workflow-006"
            },
            new()
            {
                Id = "CASE-2024-007",
                ApplicantName = "Amanda Thompson",
                ServiceType = "document-verification",
                Status = "pending",
                Priority = "medium",
                SubmittedDate = now.AddHours(-3),
                LastUpdated = now.AddHours(-3),
                AssignedTo = null,
                CurrentStage = "submission",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-11", Name = "Identity Verification.jpg", Type = "image/jpeg", UploadedAt = now.AddHours(-3), Status = "pending", Size = 83312 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-8", Timestamp = now.AddHours(-3), Action = "Case Created", User = "Amanda Thompson", Details = "New document verification request submitted." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddHours(-3), now, 0.5),
                WorkflowId = "workflow-007"
            },
            new()
            {
                Id = "CASE-2024-008",
                ApplicantName = "James Wilson",
                ServiceType = "license-renewal",
                Status = "in-progress",
                Priority = "low",
                SubmittedDate = now.AddDays(-9),
                LastUpdated = now.AddHours(-2),
                AssignedTo = "Licensing Department",
                CurrentStage = "verification",
                Documents = new List<DocumentRecord>
                {
                    new() { Id = "doc-12", Name = "License Renewal Application.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-8), Status = "pending", Size = 201231 },
                    new() { Id = "doc-13", Name = "Address Verification.pdf", Type = "application/pdf", UploadedAt = now.AddDays(-8), Status = "pending", Size = 132211 }
                },
                Timeline = new List<TimelineEventRecord>
                {
                    new() { Id = "evt-9", Timestamp = now.AddHours(-2), Action = "Verification Started", User = "Verification Bot", Details = "Automated verification kicked off for renewal documents." }
                },
                RuleEngineOutputs = new List<RuleEngineOutputRecord>(),
                Metrics = BuildMetrics(now.AddDays(-9), now, 0.58),
                WorkflowId = "workflow-008"
            }
        };

        var workflows = seedCases.Select(BuildWorkflowForCase).ToList();

        var rules = new List<RuleRecord>
        {
            new()
            {
                Id = "rule-1",
                Name = "High Priority Tax Cases",
                Description = "Route tax cases with income above threshold to senior review.",
                Enabled = true,
                Priority = 1,
                Conditions = new List<RuleConditionRecord>
                {
                    new() { Id = "cond-1", Field = "serviceType", Operator = "equals", Value = "tax-filing" },
                    new() { Id = "cond-2", Field = "income", Operator = "greaterThan", Value = "150000" }
                },
                Actions = new List<RuleActionRecord>
                {
                    new() { Id = "act-1", Type = "assign", Target = "Senior Tax Review Team" },
                    new() { Id = "act-2", Type = "setPriority", Target = "high" }
                },
                CreatedAt = now.AddMonths(-4),
                UpdatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = "rule-2",
                Name = "Fast Track Benefits",
                Description = "Escalate benefit approvals for veteran applicants.",
                Enabled = true,
                Priority = 2,
                Conditions = new List<RuleConditionRecord>
                {
                    new() { Id = "cond-3", Field = "serviceType", Operator = "equals", Value = "benefit-approval" },
                    new() { Id = "cond-4", Field = "veteranStatus", Operator = "equals", Value = "true" }
                },
                Actions = new List<RuleActionRecord>
                {
                    new() { Id = "act-3", Type = "setPriority", Target = "critical" },
                    new() { Id = "act-4", Type = "skipStage", Target = "verification" }
                },
                CreatedAt = now.AddMonths(-3),
                UpdatedAt = now.AddDays(-4)
            },
            new()
            {
                Id = "rule-3",
                Name = "Document Verification Threshold",
                Description = "Require manual review when confidence drops below 85%.",
                Enabled = true,
                Priority = 3,
                Conditions = new List<RuleConditionRecord>
                {
                    new() { Id = "cond-5", Field = "documentConfidence", Operator = "lessThan", Value = "0.85" }
                },
                Actions = new List<RuleActionRecord>
                {
                    new() { Id = "act-5", Type = "assign", Target = "Manual Review Team" },
                    new() { Id = "act-6", Type = "addFlag", Target = "requires-manual-review" }
                },
                CreatedAt = now.AddMonths(-2),
                UpdatedAt = now.AddHours(-9)
            },
            new()
            {
                Id = "rule-4",
                Name = "Auto-Approve Simple Cases",
                Description = "Automatically approve license renewals with no changes.",
                Enabled = false,
                Priority = 4,
                Conditions = new List<RuleConditionRecord>
                {
                    new() { Id = "cond-6", Field = "serviceType", Operator = "equals", Value = "license-renewal" },
                    new() { Id = "cond-7", Field = "hasChanges", Operator = "equals", Value = "false" }
                },
                Actions = new List<RuleActionRecord>
                {
                    new() { Id = "act-7", Type = "approve", Target = "automatic" }
                },
                CreatedAt = now.AddMonths(-1),
                UpdatedAt = now.AddDays(-1)
            }
        };

        var activities = new List<ActivityRecord>
        {
            new() { Id = "act-1", CaseId = "CASE-2024-001", Action = "Review Completed", User = "John Smith", Timestamp = now.AddMinutes(-5) },
            new() { Id = "act-2", CaseId = "CASE-2024-006", Action = "Approved", User = "System", Timestamp = now.AddMinutes(-15) },
            new() { Id = "act-3", CaseId = "CASE-2024-002", Action = "Documents Requested", User = "Emily Davis", Timestamp = now.AddHours(-1) },
            new() { Id = "act-4", CaseId = "CASE-2024-008", Action = "Verification Completed", User = "Verification Bot", Timestamp = now.AddHours(-2) },
            new() { Id = "act-5", CaseId = "CASE-2024-007", Action = "New Case Submitted", User = "Amanda Thompson", Timestamp = now.AddHours(-3) }
        };

        return (seedCases, workflows, rules, activities);
    }

    private static WorkflowRecord BuildWorkflowForCase(CaseRecord record)
    {
        var steps = new List<WorkflowStepRecord>
        {
            new() { Id = $"{record.WorkflowId}-step-1", Name = "Submission", Stage = "submission", Status = "completed", CompletedAt = record.SubmittedDate.AddHours(1), Description = "Initial application submitted.", Order = 1 },
            new() { Id = $"{record.WorkflowId}-step-2", Name = "Document Verification", Stage = "verification", Status = "pending", AssignedTo = "Verification Bot", Description = "Automated document checks and classification.", Order = 2 },
            new() { Id = $"{record.WorkflowId}-step-3", Name = "Manual Review", Stage = "review", Status = "pending", AssignedTo = record.AssignedTo, Description = "Human review of the service request.", Order = 3 },
            new() { Id = $"{record.WorkflowId}-step-4", Name = "Approval", Stage = "approval", Status = "pending", AssignedTo = record.AssignedTo, Description = "Final case approval or rejection.", Order = 4 },
            new() { Id = $"{record.WorkflowId}-step-5", Name = "Completion", Stage = "completion", Status = "pending", Description = "Citizen notification and archival.", Order = 5 }
        };

        foreach (var step in steps)
        {
            if (step.Stage == record.CurrentStage)
            {
                step.Status = record.Status is "approved" ? "approved" : record.Status is "rejected" ? "rejected" : record.Status == "completed" && step.Stage == "completion" ? "completed" : "in-progress";
                if (record.Status is "approved" or "completed" or "rejected")
                {
                    step.CompletedAt ??= record.LastUpdated;
                }
            }
            else if (step.Order < steps.First(item => item.Stage == record.CurrentStage).Order)
            {
                step.Status = "completed";
                step.CompletedAt ??= record.SubmittedDate.AddHours(step.Order);
            }
        }

        if (record.Status == "completed")
        {
            foreach (var step in steps.Where(item => item.Order <= 5))
            {
                step.Status = "completed";
                step.CompletedAt ??= record.LastUpdated;
            }
        }

        if (record.Status == "approved")
        {
            steps[1].Status = "completed";
            steps[1].CompletedAt ??= record.LastUpdated.AddDays(-2);
            steps[2].Status = "completed";
            steps[2].CompletedAt ??= record.LastUpdated.AddDays(-1);
            steps[3].Status = "approved";
            steps[3].CompletedAt ??= record.LastUpdated;
            steps[4].Status = "in-progress";
        }

        if (record.Status == "rejected")
        {
            steps[1].Status = "completed";
            steps[1].CompletedAt ??= record.LastUpdated.AddDays(-1);
            steps[2].Status = "rejected";
            steps[2].CompletedAt ??= record.LastUpdated;
        }

        return new WorkflowRecord
        {
            Id = record.WorkflowId,
            CaseId = record.Id,
            Name = $"{ToDisplayLabel(record.ServiceType)} Workflow - {record.Id}",
            Status = record.Status,
            CreatedAt = record.SubmittedDate,
            UpdatedAt = record.LastUpdated,
            Steps = steps
        };
    }
}
