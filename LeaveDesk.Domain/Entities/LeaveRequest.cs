using LeaveDesk.Domain.Enums;

namespace LeaveDesk.Domain.Entities;

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveDayType DayType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal NumberOfDays { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public int? ApproverId { get; set; }
    public Employee? Approver { get; set; }
    public string? ApproverComment { get; set; }
    public DateTime? ActionedAt { get; set; }
}
