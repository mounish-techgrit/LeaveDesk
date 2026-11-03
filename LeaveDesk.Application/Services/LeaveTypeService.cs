using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;

namespace LeaveDesk.Application.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly IRepository<LeaveType> _repository;

    public LeaveTypeService(IRepository<LeaveType> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LeaveTypeDto>> GetAllAsync()
    {
        var leaveTypes = await _repository.GetAllAsync();
        return leaveTypes.Select(ToDto);
    }

    public async Task<LeaveTypeDto?> GetByIdAsync(int id)
    {
        var leaveType = await _repository.GetByIdAsync(id);
        return leaveType is null ? null : ToDto(leaveType);
    }

    public async Task<ServiceResult<LeaveTypeDto>> CreateAsync(LeaveTypeRequestDto request)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return ServiceResult<LeaveTypeDto>.Failure(validationError);
        }

        if (await _repository.ExistsAsync(lt => lt.Name == request.Name))
        {
            return ServiceResult<LeaveTypeDto>.Failure("A leave type with this name already exists.");
        }

        var leaveType = new LeaveType
        {
            Name = request.Name,
            YearlyQuota = request.YearlyQuota,
            AllowExceedingBalance = request.AllowExceedingBalance,
            MaxPastDaysAllowed = request.MaxPastDaysAllowed,
            MaxCarryForwardDays = request.MaxCarryForwardDays
        };

        await _repository.AddAsync(leaveType);

        return ServiceResult<LeaveTypeDto>.Success(ToDto(leaveType));
    }

    public async Task<ServiceResult<LeaveTypeDto>> UpdateAsync(int id, LeaveTypeRequestDto request)
    {
        var leaveType = await _repository.GetByIdAsync(id);
        if (leaveType is null)
        {
            return ServiceResult<LeaveTypeDto>.Failure("Leave type not found.");
        }

        var validationError = Validate(request);
        if (validationError is not null)
        {
            return ServiceResult<LeaveTypeDto>.Failure(validationError);
        }

        if (await _repository.ExistsAsync(lt => lt.Name == request.Name && lt.Id != id))
        {
            return ServiceResult<LeaveTypeDto>.Failure("A leave type with this name already exists.");
        }

        leaveType.Name = request.Name;
        leaveType.YearlyQuota = request.YearlyQuota;
        leaveType.AllowExceedingBalance = request.AllowExceedingBalance;
        leaveType.MaxPastDaysAllowed = request.MaxPastDaysAllowed;
        leaveType.MaxCarryForwardDays = request.MaxCarryForwardDays;

        await _repository.UpdateAsync(leaveType);

        return ServiceResult<LeaveTypeDto>.Success(ToDto(leaveType));
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var leaveType = await _repository.GetByIdAsync(id);
        if (leaveType is null)
        {
            return ServiceResult.Failure("Leave type not found.");
        }

        try
        {
            await _repository.DeleteAsync(leaveType);
            return ServiceResult.Success();
        }
        catch (RepositoryDeleteConflictException)
        {
            return ServiceResult.Failure("Cannot delete this leave type because leave requests or balances reference it.");
        }
    }

    private static string? Validate(LeaveTypeRequestDto request)
    {
        if (!request.AllowExceedingBalance && request.YearlyQuota <= 0)
        {
            return "Yearly quota must be greater than zero.";
        }

        if (request.YearlyQuota < 0)
        {
            return "Yearly quota cannot be negative.";
        }

        if (request.MaxCarryForwardDays < 0)
        {
            return "Max carry forward days cannot be negative.";
        }

        if (!request.AllowExceedingBalance && request.MaxCarryForwardDays > request.YearlyQuota)
        {
            return "Max carry forward days cannot exceed the yearly quota.";
        }

        if (request.MaxPastDaysAllowed < 0)
        {
            return "Max past days allowed cannot be negative.";
        }

        return null;
    }

    private static LeaveTypeDto ToDto(LeaveType leaveType) => new()
    {
        Id = leaveType.Id,
        Name = leaveType.Name,
        YearlyQuota = leaveType.YearlyQuota,
        AllowExceedingBalance = leaveType.AllowExceedingBalance,
        MaxPastDaysAllowed = leaveType.MaxPastDaysAllowed,
        MaxCarryForwardDays = leaveType.MaxCarryForwardDays
    };
}
