namespace LeaveDesk.Application.DTOs;

public class LeaveTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int YearlyQuota { get; set; }
    public bool AllowExceedingBalance { get; set; }
    public int MaxPastDaysAllowed { get; set; }
    public int MaxCarryForwardDays { get; set; }
}
