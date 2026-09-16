using Quartz;
using TestShcheduler.Jobs;

namespace TestShcheduler.Services;

public sealed class JobControlService(ISchedulerFactory schedulerFactory)
{
    public const string JobGroup = "countries";
    public const string TriggerName = "country-list-trigger";
    public const string JobName = "country-list-job";

    public static JobKey JobKey { get; } = new(JobName, JobGroup);
    public static TriggerKey TriggerKey { get; } = new(TriggerName, JobGroup);

    public async Task TriggerNowAsync(CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.TriggerJob(JobKey, data: null, cancellationToken);
    }

    public async Task UpdateRetryPolicyAsync(int maxAttempts, int delaySeconds, CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        RetryPolicy policy = RetryPolicy.Fixed(maxAttempts, TimeSpan.FromSeconds(delaySeconds));

        await scheduler.UpdateTriggerDetails(
            TriggerKey,
            new TriggerDetailsUpdate().WithRetryPolicy(policy),
            cancellationToken);
    }

    public async Task<TriggerStatusDto> GetTriggerStatusAsync(CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        ITrigger? trigger = await scheduler.GetTrigger(TriggerKey, cancellationToken);

        if (trigger is null)
        {
            return new TriggerStatusDto(
                Exists: false,
                State: "Missing",
                NextFireTimeUtc: null,
                PreviousFireTimeUtc: null,
                RetryPolicy: "Not configured");
        }

        TriggerState state = await scheduler.GetTriggerState(TriggerKey, cancellationToken);
        string retryPolicy = trigger.RetryPolicy?.ToString() ?? "None";

        return new TriggerStatusDto(
            Exists: true,
            State: state.ToString(),
            NextFireTimeUtc: trigger.NextFireTimeUtc,
            PreviousFireTimeUtc: trigger.PreviousFireTimeUtc,
            RetryPolicy: retryPolicy);
    }
}

public sealed record TriggerStatusDto(
    bool Exists,
    string State,
    DateTimeOffset? NextFireTimeUtc,
    DateTimeOffset? PreviousFireTimeUtc,
    string RetryPolicy);
