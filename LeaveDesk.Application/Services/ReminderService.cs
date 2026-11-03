using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Application.Services;

public class ReminderService : IReminderService
{
    private readonly IRepository<LeaveRequest> _leaveRequestRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Holiday> _holidayRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IRepository<LeaveRequest> leaveRequestRepository,
        IRepository<Employee> employeeRepository,
        IRepository<Holiday> holidayRepository,
        IEmailService emailService,
        ILogger<ReminderService> logger)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _holidayRepository = holidayRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendPendingApprovalRemindersAsync()
    {
        var pending = (await _leaveRequestRepository.GetAllAsync())
            .Where(r => r.Status == LeaveRequestStatus.Pending)
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        var holidays = (await _holidayRepository.GetAllAsync()).Select(h => h.Date).ToHashSet();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdue = pending
            .Where(r => CountWorkingDays(DateOnly.FromDateTime(r.RequestedAt), today, holidays) > 2)
            .ToList();

        if (overdue.Count == 0)
        {
            return;
        }

        var employees = (await _employeeRepository.GetAllAsync()).ToDictionary(e => e.Id);

        foreach (var group in overdue.GroupBy(r => r.ApproverId))
        {
            if (group.Key is null || !employees.TryGetValue(group.Key.Value, out var approver))
            {
                // Manager-less requests have no single approver to remind; HR already sees
                // them in the company-wide view.
                continue;
            }

            var lines = group.Select(r =>
            {
                employees.TryGetValue(r.EmployeeId, out var requester);
                return $"- {requester?.FullName ?? "Unknown"}: {r.NumberOfDays} day(s), requested {r.RequestedAt:yyyy-MM-dd}";
            });

            var body = "The following leave requests have been pending for more than 2 working days:\n" + string.Join("\n", lines);
            await _emailService.SendAsync(approver.Email, "LeaveDesk: Pending approvals reminder", body);
        }

        _logger.LogInformation("Pending-approval reminders sent for {Count} overdue request(s).", overdue.Count);
    }

    private static int CountWorkingDays(DateOnly from, DateOnly to, HashSet<DateOnly> holidays)
    {
        var count = 0;
        for (var date = from.AddDays(1); date <= to; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            if (holidays.Contains(date))
            {
                continue;
            }

            count++;
        }

        return count;
    }
}
