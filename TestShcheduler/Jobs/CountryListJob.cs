using Quartz;
using TestShcheduler.Services;

namespace TestShcheduler.Jobs;

[DisallowConcurrentExecution]
public sealed class CountryListJob(
    CountryDataService countryData,
    JobExecutionTracker tracker,
    ILogger<CountryListJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        int retryAttempt = context.RetryAttempt;
        string triggerSource = context.Trigger?.Key.Name ?? "manual";
        var countries = countryData.GetAll();

        logger.LogInformation(
            "CountryListJob started (retry attempt {RetryAttempt}, trigger: {Trigger})",
            retryAttempt,
            triggerSource);

        if (tracker.SimulateFailure)
        {
            string reason = retryAttempt > 0
                ? $"Simulated failure on retry #{retryAttempt} — upstream data source unavailable."
                : "Simulated failure — configured to fail for testing retry behavior.";

            logger.LogError("CountryListJob failed: {Reason}", reason);
            tracker.RecordFailure(retryAttempt, triggerSource, reason);
            throw new JobExecutionException(reason);
        }

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync($"=== Country List ({DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}) ===");

        var lines = new List<string>(countries.Count);
        foreach (var country in countries)
        {
            string line = $"{country.Code} | {country.Name} | Capital: {country.Capital} | Population: {country.Population:N0}";
            lines.Add(line);
            await Console.Out.WriteLineAsync(line);
            logger.LogInformation("Country: {Line}", line);
        }

        await Console.Out.WriteLineAsync("=== End Country List ===");
        await Console.Out.WriteLineAsync();

        string message = retryAttempt > 0
            ? $"Successfully displayed {countries.Count} countries after retry #{retryAttempt}."
            : $"Successfully displayed {countries.Count} countries.";

        tracker.RecordSuccess(retryAttempt, triggerSource, lines, message);
        logger.LogInformation("CountryListJob completed successfully");
    }
}
