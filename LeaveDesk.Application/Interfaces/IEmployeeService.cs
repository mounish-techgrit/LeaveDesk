using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto?> GetByApplicationUserIdAsync(string applicationUserId);
    Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeRequestDto request);
    Task<ServiceResult<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequestDto request);
    Task<ServiceResult<EmployeeDto>> UpdateMyProfileAsync(string applicationUserId, UpdateMyProfileRequestDto request);
    Task<ServiceResult> AssignRoleAsync(int id, string role);
    Task<ServiceResult> DeleteAsync(int id);
}
