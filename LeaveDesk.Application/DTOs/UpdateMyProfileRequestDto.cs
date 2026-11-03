using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class UpdateMyProfileRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }
}
