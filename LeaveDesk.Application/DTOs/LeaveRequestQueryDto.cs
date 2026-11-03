using LeaveDesk.Domain.Enums;

namespace LeaveDesk.Application.DTOs;

public class LeaveRequestQueryDto
{
    public int? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? LeaveTypeId { get; set; }
    public LeaveRequestStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
