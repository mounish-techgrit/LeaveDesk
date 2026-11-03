using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class CreateEmployeeRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}
