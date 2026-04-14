namespace GovernmentServices.WorkflowApi.Configuration;

public sealed class VerificationOptions
{
    public const string SectionName = "Verification";

    public bool Enabled { get; init; } = true;
    public string PythonExecutable { get; init; } = "python3";
    public string ScriptPath { get; init; } = "ml/verify_case.py";
    public string ModelId { get; init; } = "Qwen/Qwen2.5-0.5B-Instruct";
    public int TimeoutSeconds { get; init; } = 180;
}
