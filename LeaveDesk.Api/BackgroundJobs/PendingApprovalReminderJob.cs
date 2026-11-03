using LeaveDesk.Application.Interfaces;

namespace LeaveDesk.Api.BackgroundJobs;

public class PendingApprovalReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingApprovalReminderJob> _logger;
    private readonly TimeSpan _interval;

    public PendingApprovalReminderJob(IServiceScopeFactory scopeFactory, ILogger<PendingApprovalReminderJob> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var hours = configuration.GetValue<double?>("ReminderJob:IntervalHours") ?? 24;
        _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        // Runs once immediately on startup (so it's demoable without waiting a full interval),
        // then repeats every _interval.
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
                await reminderService.SendPendingApprovalRemindersAsync();
                _logger.LogInformation("Pending-approval reminder job ran at {Time}.", DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending-approval reminder job failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
