namespace Screening.Api.Screening;

public record ScreeningRequest(
    string Id,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Channel,
    string Country,
    string CustomerRiskLevel,
    decimal DailyTotal,
    int AccountAgeDays)
{
    private static readonly string[] Channels = ["ONLINE", "BRANCH", "ATM", "CASH"];
    private static readonly string[] RiskLevels = ["LOW", "MEDIUM", "HIGH"];

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Id)) errors[nameof(Id)] = ["Required."];
        if (string.IsNullOrWhiteSpace(CustomerId)) errors[nameof(CustomerId)] = ["Required."];
        if (Currency is not { Length: 3 }) errors[nameof(Currency)] = ["Must be a 3-letter ISO code."];
        if (Country is not { Length: 2 }) errors[nameof(Country)] = ["Must be a 2-letter ISO code."];
        if (!Channels.Contains(Channel)) errors[nameof(Channel)] = [$"Must be one of {string.Join(", ", Channels)}."];
        if (!RiskLevels.Contains(CustomerRiskLevel)) errors[nameof(CustomerRiskLevel)] = [$"Must be one of {string.Join(", ", RiskLevels)}."];
        if (DailyTotal < Amount) errors[nameof(DailyTotal)] = ["Must include this transaction's amount."];
        if (AccountAgeDays < 0) errors[nameof(AccountAgeDays)] = ["Cannot be negative."];
        return errors;
    }
}

public record ScreeningDecision(string TransactionId, string Decision, int RiskScore, IReadOnlyList<string> Reasons);
