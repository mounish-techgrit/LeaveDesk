using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class AssignRoleRequestDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
