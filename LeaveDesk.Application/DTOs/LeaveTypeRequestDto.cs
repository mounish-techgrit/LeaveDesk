using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class LeaveTypeRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 365)]
    public int YearlyQuota { get; set; }

    public bool AllowExceedingBalance { get; set; }

    [Range(0, 365)]
    public int MaxPastDaysAllowed { get; set; }

    [Range(0, 365)]
    public int MaxCarryForwardDays { get; set; }
}
