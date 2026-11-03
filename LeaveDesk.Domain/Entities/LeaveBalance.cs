namespace LeaveDesk.Domain.Entities;

public class LeaveBalance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }
    public decimal AllocatedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal CarriedForwardDays { get; set; }

    public decimal RemainingDays => AllocatedDays + CarriedForwardDays - UsedDays;
}
