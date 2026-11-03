using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class UpdateEmployeeRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
}
