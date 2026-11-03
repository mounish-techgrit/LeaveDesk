using LeaveDesk.Application.Interfaces;
using LeaveDesk.Application.Services;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Tests.TestHelpers;
using Moq;

namespace LeaveDesk.Tests;

public class LeaveBalanceServiceTests
{
    private readonly List<LeaveBalance> _balances = [];
    private readonly List<LeaveType> _leaveTypes = [];
    private readonly List<LeaveBalanceAdjustment> _adjustments = [];

    private readonly Mock<IRepository<LeaveBalance>> _balanceRepo = new();
    private readonly Mock<IRepository<LeaveType>> _leaveTypeRepo = new();
    private readonly Mock<IRepository<LeaveBalanceAdjustment>> _adjustmentRepo = new();

    private readonly LeaveType _earned = new() { Id = 1, Name = "Earned", YearlyQuota = 15, MaxCarryForwardDays = 10 };

    private LeaveBalanceService CreateService()
    {
        _leaveTypes.Add(_earned);

        _balanceRepo.SetupInMemory(_balances, b => b.Id);
        _leaveTypeRepo.SetupInMemory(_leaveTypes, lt => lt.Id);
        _adjustmentRepo.SetupInMemory(_adjustments, a => a.Id);

        return new LeaveBalanceService(_balanceRepo.Object, _leaveTypeRepo.Object, _adjustmentRepo.Object);
    }

    [Fact]
    public async Task EnsureBalance_CarriesForwardPreviousYearRemaining_CappedAtMaxCarryForward()
    {
        _balances.Add(new LeaveBalance
        {
            Id = 1,
            EmployeeId = 1,
            LeaveTypeId = _earned.Id,
            Year = 2026,
            AllocatedDays = 15,
            UsedDays = 0,
            CarriedForwardDays = 0
        });

        var service = CreateService();

        var remaining = await service.GetRemainingDaysAsync(1, _earned.Id, 2027);

        // 15 remaining in 2026, capped at MaxCarryForwardDays (10), plus the fresh 15-day 2027 quota = 25.
        Assert.Equal(25m, remaining);
    }

    [Fact]
    public async Task ReserveDaysAsync_FailsWhenExceedingBalance_UnlessAllowed()
    {
        var service = CreateService();

        var blocked = await service.ReserveDaysAsync(1, _earned.Id, 2027, 20, allowExceedingBalance: false);
        Assert.False(blocked.Succeeded);

        var allowed = await service.ReserveDaysAsync(1, _earned.Id, 2027, 20, allowExceedingBalance: true);
        Assert.True(allowed.Succeeded);
    }

    [Fact]
    public async Task RestoreDaysAsync_NeverDropsUsedDaysBelowZero()
    {
        var service = CreateService();
        await service.ReserveDaysAsync(1, _earned.Id, 2027, 5, allowExceedingBalance: false);

        await service.RestoreDaysAsync(1, _earned.Id, 2027, 100);

        var remaining = await service.GetRemainingDaysAsync(1, _earned.Id, 2027);
        Assert.Equal(15m, remaining);
    }

    [Fact]
    public async Task AdjustAsync_RejectsAdjustmentThatWouldMakeAllocationNegative()
    {
        var service = CreateService();

        var result = await service.AdjustAsync(1, _earned.Id, 2027, -100, "oops", adjustedByEmployeeId: null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdjustAsync_RecordsAuditTrail_EvenWithoutAnAdjustingEmployee()
    {
        // Regression test: the seeded HRAdmin account has no linked Employee row.
        var service = CreateService();

        var result = await service.AdjustAsync(1, _earned.Id, 2027, 3, "Overtime comp", adjustedByEmployeeId: null);

        Assert.True(result.Succeeded);
        var adjustment = Assert.Single(_adjustments);
        Assert.Null(adjustment.AdjustedByEmployeeId);
        Assert.Equal(3, adjustment.AdjustmentAmount);
    }
}
