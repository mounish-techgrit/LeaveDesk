using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LeaveDesk.Infrastructure.Identity;

public class UserAccountService : IUserAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> FindUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? new List<string>() : await _userManager.GetRolesAsync(user);
    }

    public async Task<ServiceResult> SetRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Failure("User account not found.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            return ServiceResult.Failure(string.Join(" ", addResult.Errors.Select(e => e.Description)));
        }

        return ServiceResult.Success();
    }
}
