using Quartz;
using TestShcheduler.Jobs;
using TestShcheduler.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CountryDataService>();
builder.Services.AddSingleton<JobExecutionTracker>();
builder.Services.AddSingleton<JobControlService>();

builder.AddQuartz(q =>
{
    q.UseInMemoryStore();

    q.AddJob<CountryListJob>(job => job
        .WithIdentity(JobControlService.JobKey)
        .StoreDurably());

    q.AddTrigger<CountryListJob>(trigger => trigger
        .WithIdentity(JobControlService.TriggerKey)
        .ForJob(JobControlService.JobKey)
        .StartNow()
        .WithSimpleSchedule(schedule => schedule
            .WithInterval(TimeSpan.FromMinutes(2))
            .RepeatForever())
        .WithRetryPolicy(RetryPolicy.Fixed(3, TimeSpan.FromSeconds(30))));
});

builder.Services.AddQuartzHttpApi(options => options.ApiPath = "/quartz-api");
builder.Services.AddQuartzDashboard(options =>
{
    options.ReadOnly = false;
    options.HistoryRetention = TimeSpan.FromHours(24);
});

builder.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapQuartzHttpApi().AllowAnonymous();
app.MapQuartzDashboard().AllowAnonymous();

app.MapGet("/api/countries", (CountryDataService countryData) =>
    Results.Ok(countryData.GetAll()));

app.MapGet("/api/status", async (JobControlService control, JobExecutionTracker tracker) =>
{
    TriggerStatusDto trigger = await control.GetTriggerStatusAsync();
    return Results.Ok(new
    {
        trigger,
        simulateFailure = tracker.SimulateFailure,
        latestExecution = tracker.GetLatest()
    });
});

app.MapGet("/api/executions", (JobExecutionTracker tracker, int? count) =>
    Results.Ok(tracker.GetRecent(count ?? 20)));

app.MapPost("/api/trigger", async (JobControlService control) =>
{
    await control.TriggerNowAsync();
    return Results.Ok(new { message = "Job triggered manually." });
});

app.MapPost("/api/simulate-failure", (JobExecutionTracker tracker, bool enabled) =>
{
    tracker.SimulateFailure = enabled;
    return Results.Ok(new
    {
        simulateFailure = tracker.SimulateFailure,
        message = enabled
            ? "Next run(s) will fail until you turn this off."
            : "Failure simulation disabled. Next run should succeed."
    });
});

app.MapPost("/api/retry-policy", async (JobControlService control, RetryPolicyRequest request) =>
{
    if (request.MaxAttempts < 0 || request.DelaySeconds < 1)
    {
        return Results.BadRequest(new { message = "MaxAttempts must be >= 0 and DelaySeconds must be >= 1." });
    }

    await control.UpdateRetryPolicyAsync(request.MaxAttempts, request.DelaySeconds);
    TriggerStatusDto trigger = await control.GetTriggerStatusAsync();

    return Results.Ok(new
    {
        message = $"Retry policy updated: {request.MaxAttempts} retries, {request.DelaySeconds}s apart.",
        trigger
    });
});

app.Run();

public sealed record RetryPolicyRequest(int MaxAttempts, int DelaySeconds);
