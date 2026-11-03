using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;

namespace LeaveDesk.Application.Services;

public class HolidayService : IHolidayService
{
    private readonly IRepository<Holiday> _repository;

    public HolidayService(IRepository<Holiday> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<HolidayDto>> GetAllAsync()
    {
        var holidays = await _repository.GetAllAsync();
        return holidays.Select(ToDto);
    }

    public async Task<HolidayDto?> GetByIdAsync(int id)
    {
        var holiday = await _repository.GetByIdAsync(id);
        return holiday is null ? null : ToDto(holiday);
    }

    public async Task<ServiceResult<HolidayDto>> CreateAsync(HolidayRequestDto request)
    {
        if (await _repository.ExistsAsync(h => h.Date == request.Date))
        {
            return ServiceResult<HolidayDto>.Failure("A holiday is already recorded for this date.");
        }

        var holiday = new Holiday { Date = request.Date, Name = request.Name };
        await _repository.AddAsync(holiday);

        return ServiceResult<HolidayDto>.Success(ToDto(holiday));
    }

    public async Task<ServiceResult<HolidayDto>> UpdateAsync(int id, HolidayRequestDto request)
    {
        var holiday = await _repository.GetByIdAsync(id);
        if (holiday is null)
        {
            return ServiceResult<HolidayDto>.Failure("Holiday not found.");
        }

        if (await _repository.ExistsAsync(h => h.Date == request.Date && h.Id != id))
        {
            return ServiceResult<HolidayDto>.Failure("A holiday is already recorded for this date.");
        }

        holiday.Date = request.Date;
        holiday.Name = request.Name;

        await _repository.UpdateAsync(holiday);

        return ServiceResult<HolidayDto>.Success(ToDto(holiday));
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var holiday = await _repository.GetByIdAsync(id);
        if (holiday is null)
        {
            return ServiceResult.Failure("Holiday not found.");
        }

        await _repository.DeleteAsync(holiday);
        return ServiceResult.Success();
    }

    private static HolidayDto ToDto(Holiday holiday) => new()
    {
        Id = holiday.Id,
        Date = holiday.Date,
        Name = holiday.Name
    };
}
