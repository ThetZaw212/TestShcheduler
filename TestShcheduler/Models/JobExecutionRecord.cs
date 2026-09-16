namespace TestShcheduler.Models;

public sealed record JobExecutionRecord(
    Guid Id,
    DateTimeOffset ExecutedAt,
    bool Success,
    string Message,
    int RetryAttempt,
    string TriggerSource,
    IReadOnlyList<string> CountriesShown);
