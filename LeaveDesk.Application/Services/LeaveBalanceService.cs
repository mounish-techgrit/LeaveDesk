using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;

namespace LeaveDesk.Application.Services;

public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly IRepository<LeaveBalance> _balanceRepository;
    private readonly IRepository<LeaveType> _leaveTypeRepository;
    private readonly IRepository<LeaveBalanceAdjustment> _adjustmentRepository;

    public LeaveBalanceService(
        IRepository<LeaveBalance> balanceRepository,
        IRepository<LeaveType> leaveTypeRepository,
        IRepository<LeaveBalanceAdjustment> adjustmentRepository)
    {
        _balanceRepository = balanceRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _adjustmentRepository = adjustmentRepository;
    }

    public async Task<IEnumerable<LeaveBalanceDto>> GetForEmployeeAsync(int employeeId, int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var leaveTypes = await _leaveTypeRepository.GetAllAsync();

        var dtos = new List<LeaveBalanceDto>();
        foreach (var leaveType in leaveTypes)
        {
            var balance = await EnsureBalanceAsync(employeeId, leaveType.Id, targetYear);
            dtos.Add(ToDto(balance, leaveType.Name));
        }

        return dtos;
    }

    public async Task<decimal> GetRemainingDaysAsync(int employeeId, int leaveTypeId, int year)
    {
        var balance = await EnsureBalanceAsync(employeeId, leaveTypeId, year);
        return balance.RemainingDays;
    }

    public async Task<ServiceResult> ReserveDaysAsync(int employeeId, int leaveTypeId, int year, decimal days, bool allowExceedingBalance)
    {
        var balance = await EnsureBalanceAsync(employeeId, leaveTypeId, year);

        if (!allowExceedingBalance && days > balance.RemainingDays)
        {
            return ServiceResult.Failure("This request exceeds the available leave balance.");
        }

        balance.UsedDays += days;
        await _balanceRepository.UpdateAsync(balance);

        return ServiceResult.Success();
    }

    public async Task RestoreDaysAsync(int employeeId, int leaveTypeId, int year, decimal days)
    {
        var balance = await EnsureBalanceAsync(employeeId, leaveTypeId, year);
        balance.UsedDays -= days;
        if (balance.UsedDays < 0)
        {
            balance.UsedDays = 0;
        }

        await _balanceRepository.UpdateAsync(balance);
    }

    public async Task<ServiceResult<LeaveBalanceDto>> AdjustAsync(int employeeId, int leaveTypeId, int year, decimal amount, string reason, int? adjustedByEmployeeId)
    {
        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId);
        if (leaveType is null)
        {
            return ServiceResult<LeaveBalanceDto>.Failure("Leave type not found.");
        }

        var balance = await EnsureBalanceAsync(employeeId, leaveTypeId, year);
        var newAllocated = balance.AllocatedDays + amount;
        if (newAllocated < 0)
        {
            return ServiceResult<LeaveBalanceDto>.Failure("This adjustment would make the allocated balance negative.");
        }

        balance.AllocatedDays = newAllocated;
        await _balanceRepository.UpdateAsync(balance);

        var adjustment = new LeaveBalanceAdjustment
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            AdjustmentAmount = amount,
            Reason = reason,
            AdjustedByEmployeeId = adjustedByEmployeeId,
            AdjustedAt = DateTime.UtcNow
        };
        await _adjustmentRepository.AddAsync(adjustment);

        return ServiceResult<LeaveBalanceDto>.Success(ToDto(balance, leaveType.Name));
    }

    private async Task<LeaveBalance> EnsureBalanceAsync(int employeeId, int leaveTypeId, int year)
    {
        var existing = await _balanceRepository.FirstOrDefaultAsync(b =>
            b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year);
        if (existing is not null)
        {
            return existing;
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveTypeId)
            ?? throw new InvalidOperationException("Leave type not found while initializing balance.");

        var previousYear = await _balanceRepository.FirstOrDefaultAsync(b =>
            b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year - 1);

        var carriedForward = previousYear is null
            ? 0
            : Math.Max(0, Math.Min(previousYear.RemainingDays, leaveType.MaxCarryForwardDays));

        var balance = new LeaveBalance
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            AllocatedDays = leaveType.YearlyQuota,
            UsedDays = 0,
            CarriedForwardDays = carriedForward
        };

        await _balanceRepository.AddAsync(balance);
        return balance;
    }

    private static LeaveBalanceDto ToDto(LeaveBalance balance, string leaveTypeName) => new()
    {
        Id = balance.Id,
        EmployeeId = balance.EmployeeId,
        LeaveTypeId = balance.LeaveTypeId,
        LeaveTypeName = leaveTypeName,
        Year = balance.Year,
        AllocatedDays = balance.AllocatedDays,
        UsedDays = balance.UsedDays,
        CarriedForwardDays = balance.CarriedForwardDays,
        RemainingDays = balance.RemainingDays
    };
}
