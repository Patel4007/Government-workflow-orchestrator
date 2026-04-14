using GovernmentServices.WorkflowApi.Contracts;

namespace GovernmentServices.WorkflowApi.Services;

public sealed class VerificationCalibrationInput
{
    public int DocumentCount { get; init; }
    public int VerifiedDocumentCount { get; init; }
    public int PendingDocumentCount { get; init; }
    public int RejectedDocumentCount { get; init; }
    public bool UsedFallbackVerification { get; init; }

    public double VerifiedDocumentRatio => DocumentCount == 0 ? 0 : VerifiedDocumentCount / (double)DocumentCount;
    public double PendingDocumentRatio => DocumentCount == 0 ? 0 : PendingDocumentCount / (double)DocumentCount;
    public double RejectedDocumentRatio => DocumentCount == 0 ? 0 : RejectedDocumentCount / (double)DocumentCount;
}

public sealed class StatisticalFeatureVector
{
    public required string CaseId { get; init; }
    public required string ServiceType { get; init; }
    public required string Priority { get; init; }
    public required string CurrentStage { get; init; }
    public required string Status { get; init; }
    public double ElapsedDays { get; init; }
    public int DocumentCount { get; init; }
    public int VerifiedDocumentCount { get; init; }
    public int PendingDocumentCount { get; init; }
    public int RejectedDocumentCount { get; init; }
    public bool IsAssigned { get; init; }
    public bool UsedFallbackVerification { get; init; }
    public double RawVerificationConfidence { get; init; }
    public double CalibratedVerificationConfidence { get; init; }

    public double VerifiedDocumentRatio => DocumentCount == 0 ? 0 : VerifiedDocumentCount / (double)DocumentCount;
    public double PendingDocumentRatio => DocumentCount == 0 ? 0 : PendingDocumentCount / (double)DocumentCount;
    public double RejectedDocumentRatio => DocumentCount == 0 ? 0 : RejectedDocumentCount / (double)DocumentCount;
}

public interface IConfidenceCalibrationService
{
    ConfidenceCalibrationDto Calibrate(double rawConfidenceScore, VerificationCalibrationInput input);
}

public interface ILogisticRegressionModelService
{
    LogisticRegressionPredictionDto Predict(StatisticalFeatureVector features);
}

public interface IAftSurvivalModelService
{
    AftSurvivalPredictionDto Predict(StatisticalFeatureVector features);
}

public sealed class PlattScalingConfidenceCalibrationService : IConfidenceCalibrationService
{
    private const string ModelName = "platt-calibration";

    public ConfidenceCalibrationDto Calibrate(double rawConfidenceScore, VerificationCalibrationInput input)
    {
        var clampedRaw = StatisticalMath.ClampProbability(rawConfidenceScore);
        var plattScaled = StatisticalMath.Sigmoid(-0.45 + (1.35 * StatisticalMath.Logit(clampedRaw)));

        var penalty = 0.0;
        var bonus = 0.0;
        var drivers = new List<string>();

        if (input.UsedFallbackVerification)
        {
            penalty += 0.08;
            drivers.Add("Fallback verification lowers confidence reliability.");
        }

        if (input.PendingDocumentRatio > 0)
        {
            penalty += 0.07 * input.PendingDocumentRatio;
            drivers.Add("Pending documents reduce calibrated confidence.");
        }

        if (input.RejectedDocumentRatio > 0)
        {
            penalty += 0.1 * input.RejectedDocumentRatio;
            drivers.Add("Rejected documents heavily reduce confidence.");
        }

        if (input.VerifiedDocumentRatio > 0)
        {
            bonus += 0.05 * input.VerifiedDocumentRatio;
            drivers.Add("Verified documents increase confidence.");
        }

        if (input.DocumentCount <= 1)
        {
            penalty += 0.03;
            drivers.Add("Thin document evidence makes confidence less stable.");
        }

        var calibrated = StatisticalMath.ClampProbability(plattScaled - penalty + bonus);
        var reliabilityBand = calibrated switch
        {
            >= 0.85 => "high",
            >= 0.65 => "moderate",
            _ => "low"
        };

        if (drivers.Count == 0)
        {
            drivers.Add("Calibration stayed close to the raw verifier confidence.");
        }

        return new ConfidenceCalibrationDto
        {
            ModelName = ModelName,
            Method = "Platt scaling",
            RawConfidenceScore = Math.Round(clampedRaw, 4),
            CalibratedConfidenceScore = Math.Round(calibrated, 4),
            ReliabilityBand = reliabilityBand,
            Drivers = drivers
        };
    }
}

public sealed class LogisticRegressionModelService : ILogisticRegressionModelService
{
    private const string ModelName = "logistic-regression";

    public LogisticRegressionPredictionDto Predict(StatisticalFeatureVector features)
    {
        var manualReviewLogit = -1.15
            + (2.05 * (1 - features.CalibratedVerificationConfidence))
            + (1.2 * features.PendingDocumentRatio)
            + (1.55 * features.RejectedDocumentRatio)
            + StatisticalMath.PriorityWeight(features.Priority, highWeight: 0.35, criticalWeight: 0.6)
            + StatisticalMath.StageWeight(features.CurrentStage, submission: 0.2, verification: 0.35, review: 0.55, approval: -0.2, completion: -0.8)
            + StatisticalMath.ServiceWeight(features.ServiceType, tax: 0.2, benefit: 0.15, verification: 0.05, license: -0.05)
            + (features.UsedFallbackVerification ? 0.2 : 0)
            - (0.4 * features.VerifiedDocumentRatio);

        var approvalLogit = -0.55
            + (2.25 * features.CalibratedVerificationConfidence)
            + (0.75 * features.VerifiedDocumentRatio)
            - (1.0 * features.PendingDocumentRatio)
            - (1.4 * features.RejectedDocumentRatio)
            - StatisticalMath.PriorityWeight(features.Priority, highWeight: 0.1, criticalWeight: 0.25)
            + StatisticalMath.StageWeight(features.CurrentStage, submission: -0.35, verification: -0.2, review: 0.1, approval: 0.35, completion: 0.65)
            + StatisticalMath.ServiceWeight(features.ServiceType, tax: -0.05, benefit: -0.1, verification: 0.1, license: 0.15)
            - (features.UsedFallbackVerification ? 0.12 : 0);

        var manualReviewProbability = StatisticalMath.Sigmoid(manualReviewLogit);
        var approvalProbability = StatisticalMath.Sigmoid(approvalLogit);
        var recommendManualReview = manualReviewProbability >= 0.55;
        var recommendAutoApproval = approvalProbability >= 0.7 && manualReviewProbability < 0.35;

        var drivers = new List<string>();
        if (features.CalibratedVerificationConfidence < 0.7)
        {
            drivers.Add("Lower calibrated verification confidence increases manual-review odds.");
        }
        if (features.PendingDocumentRatio > 0)
        {
            drivers.Add("Pending documents push the case toward human review.");
        }
        if (features.Priority is "high" or "critical")
        {
            drivers.Add("Higher-priority cases are triaged more conservatively.");
        }
        if (features.VerifiedDocumentRatio >= 0.8)
        {
            drivers.Add("Strong document verification support raises approval likelihood.");
        }
        if (features.CurrentStage == "approval")
        {
            drivers.Add("Cases already at approval stage receive an approval boost.");
        }

        if (drivers.Count == 0)
        {
            drivers.Add("Case signals are near the logistic model baseline.");
        }

        var disposition = recommendManualReview
            ? "manual-review"
            : recommendAutoApproval
                ? "auto-approval-candidate"
                : "continue-standard-processing";

        return new LogisticRegressionPredictionDto
        {
            ModelName = ModelName,
            ManualReviewProbability = Math.Round(manualReviewProbability, 4),
            ApprovalProbability = Math.Round(approvalProbability, 4),
            RecommendManualReview = recommendManualReview,
            RecommendAutoApproval = recommendAutoApproval,
            RecommendedDisposition = disposition,
            Drivers = drivers
        };
    }
}

public sealed class AftSurvivalModelService : IAftSurvivalModelService
{
    private const string ModelName = "aft-survival";
    private const double SlaDays = 30.0;
    private const double Sigma = 0.45;

    public AftSurvivalPredictionDto Predict(StatisticalFeatureVector features)
    {
        var mu = Math.Log(12.0)
            + (0.22 * Math.Log(1 + Math.Max(features.ElapsedDays, 0)))
            + StatisticalMath.ServiceWeight(features.ServiceType, tax: 0.18, benefit: 0.12, verification: -0.05, license: -0.08)
            + StatisticalMath.PriorityWeight(features.Priority, highWeight: 0.08, criticalWeight: 0.16)
            + StatisticalMath.StageWeight(features.CurrentStage, submission: 0.5, verification: 0.3, review: 0.18, approval: -0.05, completion: -0.45)
            + (0.22 * features.PendingDocumentRatio)
            + (0.35 * features.RejectedDocumentRatio)
            + (0.04 * Math.Min(features.DocumentCount, 5))
            - (0.5 * features.CalibratedVerificationConfidence)
            - (features.IsAssigned ? 0.08 : 0.0);

        var medianCompletionDays = Math.Exp(mu);
        var expectedCompletionDays = Math.Exp(mu + ((Sigma * Sigma) / 2));
        var predictedRemainingDays = Math.Max(medianCompletionDays - features.ElapsedDays, 0);
        var slaBreachProbability = 1 - StatisticalMath.NormalCdf((Math.Log(SlaDays) - mu) / Sigma);

        var drivers = new List<string>();
        if (features.CurrentStage is "submission" or "verification")
        {
            drivers.Add("Earlier workflow stages extend the predicted completion time.");
        }
        if (features.PendingDocumentRatio > 0)
        {
            drivers.Add("Pending documents lengthen the survival estimate.");
        }
        if (features.CalibratedVerificationConfidence >= 0.8)
        {
            drivers.Add("High calibrated confidence shortens the projected timeline.");
        }
        if (!features.IsAssigned)
        {
            drivers.Add("Unassigned cases are predicted to close more slowly.");
        }

        if (drivers.Count == 0)
        {
            drivers.Add("The case is tracking close to the AFT model baseline.");
        }

        var riskBand = slaBreachProbability switch
        {
            >= 0.6 => "high",
            >= 0.3 => "moderate",
            _ => "low"
        };

        return new AftSurvivalPredictionDto
        {
            ModelName = ModelName,
            Distribution = "log-normal AFT",
            MedianCompletionDays = Math.Round(medianCompletionDays, 2),
            ExpectedCompletionDays = Math.Round(expectedCompletionDays, 2),
            PredictedRemainingDays = Math.Round(predictedRemainingDays, 2),
            SlaBreachProbability = Math.Round(slaBreachProbability, 4),
            RiskBand = riskBand,
            Drivers = drivers
        };
    }
}

internal static class StatisticalMath
{
    public static double Sigmoid(double value) => 1 / (1 + Math.Exp(-value));

    public static double ClampProbability(double value) => Math.Clamp(value, 0.01, 0.99);

    public static double Logit(double probability)
    {
        var clamped = ClampProbability(probability);
        return Math.Log(clamped / (1 - clamped));
    }

    public static double NormalCdf(double value) => 0.5 * (1 + Erf(value / Math.Sqrt(2)));

    private static double Erf(double value)
    {
        var sign = Math.Sign(value);
        var absolute = Math.Abs(value);
        const double a = 0.147;
        var inner = 1 - Math.Exp((-absolute * absolute) * ((4 / Math.PI) + (a * absolute * absolute)) / (1 + (a * absolute * absolute)));
        return sign * Math.Sqrt(inner);
    }

    public static double PriorityWeight(string priority, double highWeight, double criticalWeight) =>
        priority switch
        {
            "critical" => criticalWeight,
            "high" => highWeight,
            "medium" => 0,
            "low" => -0.05,
            _ => 0
        };

    public static double ServiceWeight(string serviceType, double tax, double benefit, double verification, double license) =>
        serviceType switch
        {
            "tax-filing" => tax,
            "benefit-approval" => benefit,
            "document-verification" => verification,
            "license-renewal" => license,
            _ => 0
        };

    public static double StageWeight(string stage, double submission, double verification, double review, double approval, double completion) =>
        stage switch
        {
            "submission" => submission,
            "verification" => verification,
            "review" => review,
            "approval" => approval,
            "completion" => completion,
            _ => 0
        };
}
