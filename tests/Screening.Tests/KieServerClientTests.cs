using System.Text.Json.Nodes;
using Screening.Api.KieServer;
using Screening.Api.Screening;

namespace Screening.Tests;

public class KieServerClientTests
{
    internal static readonly ScreeningRequest Request = new("T1", "C1", 12000m, "USD", "CASH", "AA", "HIGH", 12000m, 400);

    internal const string SampleResponse = """
        {
          "type" : "SUCCESS",
          "msg" : "Container transaction-rules successfully called.",
          "result" : {
            "execution-results" : {
              "results" : [ {
                "value" : { "com.alhassan.screening.ScreeningResult" : {
                  "transactionId" : "T1", "decision" : "REVIEW", "riskScore" : 70,
                  "reasons" : [ "Counterparty country AA is high risk", "High-risk customer sending more than 10,000", "Cash transaction at or above the 10,000 reporting threshold" ]
                } },
                "key" : "result"
              }, { "value" : 3, "key" : "firedRules" } ],
              "facts" : [ { "value" : { "org.drools.core.common.DefaultFactHandle" : { "external-form" : "0:3:1:1:3:DEFAULT:NON_TRAIT:com.alhassan.screening.ScreeningResult" } }, "key" : "result" } ]
            }
          }
        }
        """;

    [Fact]
    public void Batch_command_inserts_facts_and_fires_rules()
    {
        var options = new KieServerOptions { HighRiskCountries = ["AA", "BB"] };

        var cmd = KieServerClient.BuildBatchCommand(Request, options);

        Assert.Equal("screeningSession", cmd["lookup"]!.GetValue<string>());
        var commands = cmd["commands"]!.AsArray();
        Assert.Equal(4, commands.Count);

        var tx = commands[0]!["insert"]!["object"]!["com.alhassan.screening.Transaction"]!;
        Assert.Equal(12000m, tx["amount"]!.GetValue<decimal>());
        Assert.Equal("CASH", tx["channel"]!.GetValue<string>());

        var policy = commands[1]!["insert"]!["object"]!["com.alhassan.screening.ScreeningPolicy"]!;
        Assert.Equal(["AA", "BB"], policy["highRiskCountries"]!.AsArray().Select(c => c!.GetValue<string>()));

        var result = commands[2]!["insert"]!;
        Assert.Equal("result", result["out-identifier"]!.GetValue<string>());
        Assert.True(result["return-object"]!.GetValue<bool>());

        Assert.NotNull(commands[3]!["fire-all-rules"]);
    }

    [Fact]
    public void Parses_screening_result_from_execution_results()
    {
        var decision = KieServerClient.ParseDecision(SampleResponse);

        Assert.Equal("T1", decision.TransactionId);
        Assert.Equal("REVIEW", decision.Decision);
        Assert.Equal(70, decision.RiskScore);
        Assert.Equal(3, decision.Reasons.Count);
    }

    [Fact]
    public void Failure_response_raises_with_server_message()
    {
        var body = new JsonObject { ["type"] = "FAILURE", ["msg"] = "Container transaction-rules is not instantiated." }.ToJsonString();

        var e = Assert.Throws<KieServerException>(() => KieServerClient.ParseDecision(body));
        Assert.Contains("not instantiated", e.Message);
    }

    [Theory]
    [InlineData("Currency", "US")]
    [InlineData("Channel", "WIRE")]
    [InlineData("DailyTotal", "100")]
    public void Validation_catches_bad_input(string field, string value)
    {
        var request = field switch
        {
            "Currency" => Request with { Currency = value },
            "Channel" => Request with { Channel = value },
            _ => Request with { DailyTotal = decimal.Parse(value) },
        };

        Assert.Contains(field, request.Validate().Keys);
    }

    [Fact]
    public void Valid_request_has_no_errors() => Assert.Empty(Request.Validate());
}
