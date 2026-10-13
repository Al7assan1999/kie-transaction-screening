using Microsoft.Extensions.Options;
using Screening.Api.KieServer;
using Screening.Api.Screening;

namespace Screening.Tests;

/// <summary>Skipped unless KIE_SERVER_URL is set, so `dotnet test` works without a running server.</summary>
public sealed class KieServerFactAttribute : FactAttribute
{
    public KieServerFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KIE_SERVER_URL")))
            Skip = "Set KIE_SERVER_URL to run against a real KIE Server (see CI workflow).";
    }
}

/// <summary>Runs the deployed Drools rules on a real KIE Server (started by the CI workflow).</summary>
public class KieServerIntegrationTests
{
    private static KieServerClient CreateClient()
    {
        var options = new KieServerOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("KIE_SERVER_URL")!,
            Username = "kieserver",
            Password = "kieserver1!",
            HighRiskCountries = ["AA"],
        };
        var http = new HttpClient();
        KieServerClient.Configure(http, options);
        return new KieServerClient(http, Options.Create(options));
    }

    private static ScreeningRequest Ordinary => new("IT-1", "C1", 250m, "USD", "ONLINE", "US", "LOW", 250m, 365);

    [KieServerFact]
    public async Task Container_is_started() =>
        Assert.Equal("STARTED", await CreateClient().GetContainerStatusAsync());

    [KieServerFact]
    public async Task Ordinary_transaction_is_approved()
    {
        var decision = await CreateClient().ScreenAsync(Ordinary);

        Assert.Equal("IT-1", decision.TransactionId);
        Assert.Equal("APPROVE", decision.Decision);
        Assert.Empty(decision.Reasons);
    }

    [KieServerFact]
    public async Task Policy_list_from_the_api_drives_country_review()
    {
        var decision = await CreateClient().ScreenAsync(Ordinary with { Country = "AA" });

        Assert.Equal("REVIEW", decision.Decision);
        Assert.Contains(decision.Reasons, r => r.Contains("AA"));
    }

    [KieServerFact]
    public async Task Reject_outranks_review()
    {
        var decision = await CreateClient().ScreenAsync(Ordinary with
        {
            Amount = 12000m, Channel = "CASH", CustomerRiskLevel = "HIGH", DailyTotal = 60000m,
        });

        Assert.Equal("REJECT", decision.Decision);
        Assert.Equal(90, decision.RiskScore);
    }
}
