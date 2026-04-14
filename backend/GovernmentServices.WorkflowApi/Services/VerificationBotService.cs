using System.Diagnostics;
using System.Text.Json;
using GovernmentServices.WorkflowApi.Configuration;
using GovernmentServices.WorkflowApi.Models;
using Microsoft.Extensions.Options;

namespace GovernmentServices.WorkflowApi.Services;

public interface IVerificationBotService
{
    Task<VerificationResult> VerifyAsync(
        string caseId,
        string applicantName,
        string serviceType,
        IReadOnlyList<DocumentRecord> documents,
        CancellationToken cancellationToken);
}

public sealed class VerificationBotService : IVerificationBotService
{
    private readonly VerificationOptions _options;
    private readonly IConfidenceCalibrationService _confidenceCalibrationService;
    private readonly ILogger<VerificationBotService> _logger;

    public VerificationBotService(
        IOptions<VerificationOptions> options,
        IConfidenceCalibrationService confidenceCalibrationService,
        ILogger<VerificationBotService> logger)
    {
        _options = options.Value;
        _confidenceCalibrationService = confidenceCalibrationService;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyAsync(
        string caseId,
        string applicantName,
        string serviceType,
        IReadOnlyList<DocumentRecord> documents,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || documents.Count == 0)
        {
            return BuildFallback(caseId, serviceType, documents, "Verification bot disabled or no documents provided.");
        }

        var request = new
        {
            caseId,
            applicantName,
            serviceType,
            documents = documents.Select(document => new
            {
                document.Name,
                document.Type,
                document.Status,
                document.Size
            })
        };

        try
        {
            var payload = JsonSerializer.Serialize(request);
            var scriptPath = Path.IsPathRooted(_options.ScriptPath)
                ? _options.ScriptPath
                : Path.Combine(AppContext.BaseDirectory, _options.ScriptPath);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.PythonExecutable,
                    Arguments = $"\"{scriptPath}\" --model \"{_options.ModelId}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardInput.WriteAsync(payload);
            process.StandardInput.Close();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(timeoutCts.Token);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Qwen verification script exited with {ExitCode}: {Error}", process.ExitCode, error);
                return BuildFallback(caseId, serviceType, documents, "Qwen verification fallback used because the Python model process failed.");
            }

            var response = JsonSerializer.Deserialize<QwenVerificationResponse>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response is null)
            {
                _logger.LogWarning("Unable to deserialize Qwen verification response for case {CaseId}. Output: {Output}", caseId, output);
                return BuildFallback(caseId, serviceType, documents, "Qwen verification fallback used because the model response was invalid.");
            }

            var calibration = _confidenceCalibrationService.Calibrate(
                response.ConfidenceScore,
                BuildCalibrationInput(documents, usedFallbackVerification: false));
            var passed = response.Passed && calibration.CalibratedConfidenceScore >= 0.6;

            return new VerificationResult
            {
                Passed = passed,
                ConfidenceScore = calibration.CalibratedConfidenceScore,
                RawConfidenceScore = calibration.RawConfidenceScore,
                Summary = response.Summary,
                Model = response.Model ?? _options.ModelId,
                Findings = response.Findings?.Where(finding => !string.IsNullOrWhiteSpace(finding)).ToArray() ?? Array.Empty<string>(),
                Calibration = calibration,
                UsedFallback = false
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Falling back to heuristic verification for case {CaseId}", caseId);
            return BuildFallback(caseId, serviceType, documents, "Qwen verification fallback used because local dependencies or the model are not ready yet.");
        }
    }

    private VerificationResult BuildFallback(
        string caseId,
        string serviceType,
        IReadOnlyList<DocumentRecord> documents,
        string summary)
    {
        var findings = new List<string>();
        var verifiedDocuments = 0;

        foreach (var document in documents)
        {
            var looksSupported = document.Type.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                || document.Type.Contains("image", StringComparison.OrdinalIgnoreCase);
            var seemsOfficial = document.Name.Contains("tax", StringComparison.OrdinalIgnoreCase)
                || document.Name.Contains("license", StringComparison.OrdinalIgnoreCase)
                || document.Name.Contains("identity", StringComparison.OrdinalIgnoreCase)
                || document.Name.Contains("benefit", StringComparison.OrdinalIgnoreCase)
                || document.Name.Contains("verification", StringComparison.OrdinalIgnoreCase)
                || document.Name.Contains("w-2", StringComparison.OrdinalIgnoreCase);

            if (looksSupported && seemsOfficial)
            {
                verifiedDocuments++;
                findings.Add($"{document.Name} looks valid for {serviceType}.");
                continue;
            }

            findings.Add($"{document.Name} needs manual review.");
        }

        var confidence = documents.Count == 0 ? 0.0 : Math.Round((double)verifiedDocuments / documents.Count, 2);
        var calibration = _confidenceCalibrationService.Calibrate(
            confidence,
            BuildCalibrationInput(documents, usedFallbackVerification: true));
        var passed = calibration.CalibratedConfidenceScore >= 0.75;

        return new VerificationResult
        {
            Passed = passed,
            ConfidenceScore = calibration.CalibratedConfidenceScore,
            RawConfidenceScore = calibration.RawConfidenceScore,
            Summary = summary,
            Model = $"{_options.ModelId} (fallback heuristics)",
            Findings = findings.Count == 0
                ? new[] { $"Case {caseId} has no uploaded documents yet." }
                : findings,
            Calibration = calibration,
            UsedFallback = true
        };
    }

    private static VerificationCalibrationInput BuildCalibrationInput(
        IReadOnlyList<DocumentRecord> documents,
        bool usedFallbackVerification)
    {
        return new VerificationCalibrationInput
        {
            DocumentCount = documents.Count,
            VerifiedDocumentCount = documents.Count(document => document.Status.Equals("verified", StringComparison.OrdinalIgnoreCase)),
            PendingDocumentCount = documents.Count(document => document.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)),
            RejectedDocumentCount = documents.Count(document => document.Status.Equals("rejected", StringComparison.OrdinalIgnoreCase)),
            UsedFallbackVerification = usedFallbackVerification
        };
    }

    private sealed class QwenVerificationResponse
    {
        public bool Passed { get; init; }
        public double ConfidenceScore { get; init; }
        public string Summary { get; init; } = string.Empty;
        public string? Model { get; init; }
        public string[]? Findings { get; init; }
    }
}
