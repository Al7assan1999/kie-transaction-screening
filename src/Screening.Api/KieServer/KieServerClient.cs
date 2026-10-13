using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Screening.Api.Screening;

namespace Screening.Api.KieServer;

public class KieServerException(string message) : Exception(message);

/// <summary>
/// Thin client for the KIE Server REST API. Screening runs as one batch-execution call against a
/// stateless session: insert the facts, fire all rules, read back the ScreeningResult the rules filled in.
/// </summary>
public class KieServerClient(HttpClient http, IOptions<KieServerOptions> options)
{
    private const string Package = "com.alhassan.screening";
    private readonly KieServerOptions _options = options.Value;

    public static void Configure(HttpClient http, KieServerOptions options)
    {
        http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Add("X-KIE-ContentType", "JSON");
    }

    public async Task<ScreeningDecision> ScreenAsync(ScreeningRequest request, CancellationToken ct = default)
    {
        var command = BuildBatchCommand(request, _options);
        using var content = new StringContent(command.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"containers/instances/{_options.ContainerId}", content, ct);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new KieServerException($"KIE Server returned {(int)response.StatusCode}: {body}");

        return ParseDecision(body);
    }

    /// <summary>Returns the container status (e.g. STARTED), or null if it isn't deployed.</summary>
    public async Task<string?> GetContainerStatusAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"containers/{_options.ContainerId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        return json?["type"]?.GetValue<string>() == "SUCCESS"
            ? json["result"]?["kie-container"]?["status"]?.GetValue<string>()
            : null;
    }

    public static JsonObject BuildBatchCommand(ScreeningRequest r, KieServerOptions options) => new()
    {
        ["lookup"] = options.SessionName,
        ["commands"] = new JsonArray
        {
            Insert("Transaction", new JsonObject
            {
                ["id"] = r.Id,
                ["customerId"] = r.CustomerId,
                ["amount"] = r.Amount,
                ["currency"] = r.Currency,
                ["channel"] = r.Channel,
                ["country"] = r.Country,
                ["customerRiskLevel"] = r.CustomerRiskLevel,
                ["dailyTotal"] = r.DailyTotal,
                ["accountAgeDays"] = r.AccountAgeDays,
            }),
            Insert("ScreeningPolicy", new JsonObject
            {
                ["highRiskCountries"] = new JsonArray(options.HighRiskCountries.Select(c => (JsonNode)c).ToArray()),
            }),
            Insert("ScreeningResult", new JsonObject { ["transactionId"] = r.Id }, outIdentifier: "result"),
            new JsonObject { ["fire-all-rules"] = new JsonObject { ["out-identifier"] = "firedRules" } },
        },
    };

    public static ScreeningDecision ParseDecision(string responseBody)
    {
        var json = JsonNode.Parse(responseBody)
            ?? throw new KieServerException("Empty response from KIE Server.");

        if (json["type"]?.GetValue<string>() != "SUCCESS")
            throw new KieServerException(json["msg"]?.GetValue<string>() ?? "KIE Server reported a failure.");

        var result = json["result"]?["execution-results"]?["results"]?.AsArray()
            .FirstOrDefault(r => r?["key"]?.GetValue<string>() == "result")
            ?["value"]?[$"{Package}.ScreeningResult"]
            ?? throw new KieServerException("Response did not contain a ScreeningResult.");

        return new ScreeningDecision(
            result["transactionId"]!.GetValue<string>(),
            result["decision"]!.GetValue<string>(),
            result["riskScore"]!.GetValue<int>(),
            result["reasons"]?.AsArray().Select(x => x!.GetValue<string>()).ToList() ?? []);
    }

    private static JsonObject Insert(string type, JsonObject fact, string? outIdentifier = null)
    {
        var insert = new JsonObject { ["object"] = new JsonObject { [$"{Package}.{type}"] = fact } };
        if (outIdentifier is not null)
        {
            insert["out-identifier"] = outIdentifier;
            insert["return-object"] = true;
        }
        return new JsonObject { ["insert"] = insert };
    }
}
