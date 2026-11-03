using System.ComponentModel.DataAnnotations;
using LeaveDesk.Domain.Enums;

namespace LeaveDesk.Application.DTOs;

public class CreateLeaveRequestDto
{
    public int LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public LeaveDayType DayType { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
