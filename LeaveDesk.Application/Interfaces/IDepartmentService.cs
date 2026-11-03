using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<ServiceResult<DepartmentDto>> CreateAsync(DepartmentRequestDto request);
    Task<ServiceResult<DepartmentDto>> UpdateAsync(int id, DepartmentRequestDto request);
    Task<ServiceResult> DeleteAsync(int id);
}
