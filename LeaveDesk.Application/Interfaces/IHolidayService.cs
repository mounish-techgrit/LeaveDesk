using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface IHolidayService
{
    Task<IEnumerable<HolidayDto>> GetAllAsync();
    Task<HolidayDto?> GetByIdAsync(int id);
    Task<ServiceResult<HolidayDto>> CreateAsync(HolidayRequestDto request);
    Task<ServiceResult<HolidayDto>> UpdateAsync(int id, HolidayRequestDto request);
    Task<ServiceResult> DeleteAsync(int id);
}
