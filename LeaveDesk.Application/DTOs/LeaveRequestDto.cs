using LeaveDesk.Domain.Enums;

namespace LeaveDesk.Application.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveDayType DayType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal NumberOfDays { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public int? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public string? ApproverComment { get; set; }
    public DateTime? ActionedAt { get; set; }
}
