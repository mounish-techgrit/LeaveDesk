using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto request);
    Task<AuthResult> LoginAsync(LoginRequestDto request);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequestDto request);
}
