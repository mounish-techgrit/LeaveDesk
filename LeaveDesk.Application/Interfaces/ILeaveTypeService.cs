using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface ILeaveTypeService
{
    Task<IEnumerable<LeaveTypeDto>> GetAllAsync();
    Task<LeaveTypeDto?> GetByIdAsync(int id);
    Task<ServiceResult<LeaveTypeDto>> CreateAsync(LeaveTypeRequestDto request);
    Task<ServiceResult<LeaveTypeDto>> UpdateAsync(int id, LeaveTypeRequestDto request);
    Task<ServiceResult> DeleteAsync(int id);
}
