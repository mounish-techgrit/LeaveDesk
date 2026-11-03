using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class ForgotPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
