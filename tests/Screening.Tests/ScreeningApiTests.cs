using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Screening.Api.KieServer;
using Screening.Api.Screening;

namespace Screening.Tests;

/// <summary>Runs the real API with KIE Server replaced by a canned HTTP handler.</summary>
public class ScreeningApiTests
{
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> kie, List<HttpRequestMessage>? captured = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddHttpClient<KieServerClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(req =>
                {
                    captured?.Add(req);
                    return kie(req);
                }))))
        .CreateClient();

    [Fact]
    public async Task Screening_returns_the_rules_decision()
    {
        var captured = new List<HttpRequestMessage>();
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(KieServerClientTests.SampleResponse) }, captured);

        var response = await client.PostAsJsonAsync("/api/screenings", KieServerClientTests.Request);

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<ScreeningDecision>();
        Assert.Equal("REVIEW", decision!.Decision);

        var call = Assert.Single(captured);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.EndsWith("/containers/instances/transaction-rules", call.RequestUri!.AbsolutePath);
        Assert.Equal("Basic", call.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Invalid_request_is_rejected_without_calling_kie()
    {
        var captured = new List<HttpRequestMessage>();
        var client = CreateClient(_ => throw new InvalidOperationException("should not be called"), captured);

        var response = await client.PostAsJsonAsync("/api/screenings", KieServerClientTests.Request with { Currency = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(captured);
    }

    [Fact]
    public async Task Kie_server_outage_returns_503()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("boom") });

        var response = await client.PostAsJsonAsync("/api/screenings", KieServerClientTests.Request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(request));
    }
}
