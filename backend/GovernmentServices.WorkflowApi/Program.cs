using GovernmentServices.WorkflowApi.Configuration;
using GovernmentServices.WorkflowApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Server:Url"] ?? "http://localhost:5050");

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.Configure<VerificationOptions>(builder.Configuration.GetSection(VerificationOptions.SectionName));
builder.Services.AddSingleton<IConfidenceCalibrationService, PlattScalingConfidenceCalibrationService>();
builder.Services.AddSingleton<ILogisticRegressionModelService, LogisticRegressionModelService>();
builder.Services.AddSingleton<IAftSurvivalModelService, AftSurvivalModelService>();
builder.Services.AddSingleton<IVerificationBotService, VerificationBotService>();
builder.Services.AddSingleton<GovernmentWorkflowStore>();

var app = builder.Build();

app.UseCors();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "GovernmentServices.WorkflowApi"
}));

app.MapControllers();

app.Run();
