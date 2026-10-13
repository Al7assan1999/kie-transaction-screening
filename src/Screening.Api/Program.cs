using Microsoft.Extensions.Options;
using Screening.Api.KieServer;
using Screening.Api.Screening;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<KieServerOptions>()
    .BindConfiguration(KieServerOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<KieServerClient>((sp, http) =>
    KieServerClient.Configure(http, sp.GetRequiredService<IOptions<KieServerOptions>>().Value));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();

app.MapPost("/api/screenings", async (ScreeningRequest request, KieServerClient kie, ILogger<Program> logger, CancellationToken ct) =>
{
    var errors = request.Validate();
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    try
    {
        return Results.Ok(await kie.ScreenAsync(request, ct));
    }
    catch (Exception e) when (e is KieServerException or HttpRequestException)
    {
        logger.LogError(e, "Screening failed for transaction {TransactionId}", request.Id);
        return Results.Problem("The rules engine is unavailable. Try again shortly.", statusCode: 503);
    }
})
.WithSummary("Screen a transaction against the deployed Drools rules");

app.MapGet("/api/rules/status", async (KieServerClient kie, CancellationToken ct) =>
{
    var status = await kie.GetContainerStatusAsync(ct);
    return status == "STARTED"
        ? Results.Ok(new { container = "STARTED" })
        : Results.Problem($"Rules container is not running (status: {status ?? "not deployed"}).", statusCode: 503);
})
.WithSummary("Check that the rules container is deployed and started on KIE Server");

app.Run();

public partial class Program;
