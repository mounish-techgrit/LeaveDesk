namespace LeaveDesk.Domain.Entities;

public class LeaveBalanceAdjustment
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int? AdjustedByEmployeeId { get; set; }
    public Employee? AdjustedByEmployee { get; set; }

    public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
}
