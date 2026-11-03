namespace LeaveDesk.Application.Interfaces;

public interface IReminderService
{
    Task SendPendingApprovalRemindersAsync();
}
