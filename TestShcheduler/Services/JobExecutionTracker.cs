using System.Collections.Concurrent;
using TestShcheduler.Models;

namespace TestShcheduler.Services;

public sealed class JobExecutionTracker
{
    private const int MaxRecords = 50;
    private readonly ConcurrentQueue<JobExecutionRecord> records = new();

    public bool SimulateFailure { get; set; }

    public void RecordSuccess(
        int retryAttempt,
        string triggerSource,
        IReadOnlyList<string> countriesShown,
        string message)
    {
        Add(new JobExecutionRecord(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Success: true,
            message,
            retryAttempt,
            triggerSource,
            countriesShown));
    }

    public void RecordFailure(
        int retryAttempt,
        string triggerSource,
        string reason)
    {
        Add(new JobExecutionRecord(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Success: false,
            reason,
            retryAttempt,
            triggerSource,
            []));
    }

    public IReadOnlyList<JobExecutionRecord> GetRecent(int count = 20)
    {
        return records.Reverse().Take(count).ToList();
    }

    public JobExecutionRecord? GetLatest()
    {
        return records.LastOrDefault();
    }

    private void Add(JobExecutionRecord record)
    {
        records.Enqueue(record);

        while (records.Count > MaxRecords && records.TryDequeue(out _))
        {
        }
    }
}
