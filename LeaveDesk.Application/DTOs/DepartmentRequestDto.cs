using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class DepartmentRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
