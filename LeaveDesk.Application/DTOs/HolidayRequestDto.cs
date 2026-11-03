using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class HolidayRequestDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
