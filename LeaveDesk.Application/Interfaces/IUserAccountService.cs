using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface IUserAccountService
{
    Task<string?> FindUserIdByEmailAsync(string email);
    Task<IList<string>> GetRolesAsync(string userId);
    Task<ServiceResult> SetRoleAsync(string userId, string role);
}
