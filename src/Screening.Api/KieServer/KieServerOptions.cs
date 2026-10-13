using System.ComponentModel.DataAnnotations;

namespace Screening.Api.KieServer;

public class KieServerOptions
{
    public const string Section = "KieServer";

    /// <summary>e.g. http://localhost:8180/kie-server/services/rest/server</summary>
    [Required, Url] public string BaseUrl { get; set; } = "";
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    [Required] public string ContainerId { get; set; } = "transaction-rules";
    [Required] public string SessionName { get; set; } = "screeningSession";

    /// <summary>Sent with every request as a ScreeningPolicy fact, so it can change without redeploying rules.</summary>
    public List<string> HighRiskCountries { get; set; } = [];
}
