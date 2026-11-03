using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext context,
        JwtTokenGenerator tokenGenerator,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _tokenGenerator = tokenGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return AuthResult.Failure(["Email is already registered."]);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.Failure(createResult.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, "Employee");
        _logger.LogInformation("New account registered for {Email}.", request.Email);

        var response = await BuildAuthResponseAsync(user);
        return AuthResult.Success(response);
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResult.Failure(["Invalid email or password."]);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login blocked: {Email} is locked out after repeated failed attempts.", request.Email);
            return AuthResult.Failure(["This account is locked due to multiple failed login attempts. Try again later."]);
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login attempt for {Email}.", request.Email);
            return AuthResult.Failure(["Invalid email or password."]);
        }

        var linkedEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
        if (linkedEmployee is not null && !linkedEmployee.IsActive)
        {
            _logger.LogWarning("Login blocked: {Email} belongs to a deactivated employee.", request.Email);
            return AuthResult.Failure(["This account has been deactivated. Contact HR."]);
        }

        var response = await BuildAuthResponseAsync(user, linkedEmployee);
        return AuthResult.Success(response);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Deliberately silent: do not reveal whether an email is registered.
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var body = $"Use this token to reset your password: {token}\n\n" +
                   "(In a real deployment this token would be embedded in a link to a reset-password page.)";

        await _emailService.SendAsync(user.Email!, "LeaveDesk: Reset your password", body);
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return ServiceResult.Failure("Invalid request.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return ServiceResult.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Password reset completed for {Email}.", request.Email);
        return ServiceResult.Success();
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user, Employee? employee = null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        employee ??= await _context.Employees.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

        var (token, expiresAt) = _tokenGenerator.GenerateToken(user.Id, user.Email!, roles, employee?.Id);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            Roles = roles
        };
    }
}
