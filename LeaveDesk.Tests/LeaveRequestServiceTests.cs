using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Application.Services;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Domain.Enums;
using LeaveDesk.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LeaveDesk.Tests;

public class LeaveRequestServiceTests
{
    private readonly List<LeaveRequest> _leaveRequests = [];
    private readonly List<Employee> _employees = [];
    private readonly List<LeaveType> _leaveTypes = [];
    private readonly List<Holiday> _holidays = [];

    private readonly Mock<IRepository<LeaveRequest>> _leaveRequestRepo = new();
    private readonly Mock<IRepository<Employee>> _employeeRepo = new();
    private readonly Mock<IRepository<LeaveType>> _leaveTypeRepo = new();
    private readonly Mock<IRepository<Holiday>> _holidayRepo = new();
    private readonly Mock<ILeaveBalanceService> _balanceService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private readonly LeaveType _annual = new() { Id = 1, Name = "Annual", YearlyQuota = 20, MaxPastDaysAllowed = 0, MaxCarryForwardDays = 10 };
    private readonly Employee _manager = new() { Id = 1, FullName = "Manager One", Email = "manager@test.local", ApplicationUserId = "mgr-user", DepartmentId = 1, IsActive = true };
    private readonly Employee _employee = new() { Id = 2, FullName = "Employee One", Email = "employee@test.local", ApplicationUserId = "emp-user", DepartmentId = 1, ManagerId = 1, IsActive = true };

    private static DateOnly NextMonday(DateOnly from)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday;
        return from.AddDays(daysUntilMonday);
    }

    private LeaveRequestService CreateService()
    {
        _leaveTypes.Add(_annual);
        _employees.Add(_manager);
        _employees.Add(_employee);

        _leaveRequestRepo.SetupInMemory(_leaveRequests, r => r.Id);
        _employeeRepo.SetupInMemory(_employees, e => e.Id);
        _leaveTypeRepo.SetupInMemory(_leaveTypes, lt => lt.Id);
        _holidayRepo.SetupInMemory(_holidays, h => h.Id);

        _balanceService
            .Setup(b => b.ReserveDaysAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<bool>()))
            .ReturnsAsync(ServiceResult.Success());

        return new LeaveRequestService(
            _leaveRequestRepo.Object,
            _employeeRepo.Object,
            _leaveTypeRepo.Object,
            _holidayRepo.Object,
            _balanceService.Object,
            _emailService.Object,
            NullLogger<LeaveRequestService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_RoutesApproverToRequesterManager()
    {
        var service = CreateService();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        var result = await service.CreateAsync(_employee.ApplicationUserId, new CreateLeaveRequestDto
        {
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday,
            DayType = LeaveDayType.FullDay,
            Reason = "Test"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(_manager.Id, result.Data!.ApproverId);
    }

    [Fact]
    public async Task CreateAsync_ExcludesWeekendsAndHolidaysFromDayCount()
    {
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));
        var friday = monday.AddDays(4);
        var tuesday = monday.AddDays(1);
        _holidays.Add(new Holiday { Id = 1, Date = tuesday, Name = "Test Holiday" });

        var service = CreateService();

        var result = await service.CreateAsync(_employee.ApplicationUserId, new CreateLeaveRequestDto
        {
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = friday,
            DayType = LeaveDayType.FullDay,
            Reason = "Test"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(4m, result.Data!.NumberOfDays);
    }

    [Fact]
    public async Task CreateAsync_RejectsHalfDaySpanningMultipleDates()
    {
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));
        var service = CreateService();

        var result = await service.CreateAsync(_employee.ApplicationUserId, new CreateLeaveRequestDto
        {
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday.AddDays(1),
            DayType = LeaveDayType.HalfDay,
            Reason = "Test"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("same start and end date", result.Errors.First());
    }

    [Fact]
    public async Task CreateAsync_RejectsOverlappingRequest()
    {
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));
        var service = CreateService();

        _leaveRequests.Add(new LeaveRequest
        {
            Id = 1,
            EmployeeId = _employee.Id,
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday.AddDays(4),
            Status = LeaveRequestStatus.Approved,
            NumberOfDays = 5
        });

        var result = await service.CreateAsync(_employee.ApplicationUserId, new CreateLeaveRequestDto
        {
            LeaveTypeId = _annual.Id,
            StartDate = monday.AddDays(2),
            EndDate = monday.AddDays(3),
            DayType = LeaveDayType.FullDay,
            Reason = "Test"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("overlaps", result.Errors.First());
    }

    [Fact]
    public async Task CreateAsync_RejectsBackdatedLeave_WhenPastLeaveTypesLimit()
    {
        var service = CreateService();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await service.CreateAsync(_employee.ApplicationUserId, new CreateLeaveRequestDto
        {
            LeaveTypeId = _annual.Id, // MaxPastDaysAllowed = 0
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(-1),
            DayType = LeaveDayType.FullDay,
            Reason = "Test"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("backdated", result.Errors.First());
    }

    [Fact]
    public async Task DecideAsync_RejectsWhenCallerIsNeitherApproverNorHrFallback()
    {
        var service = CreateService();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        var stranger = new Employee { Id = 3, FullName = "Stranger", Email = "s@test.local", ApplicationUserId = "stranger-user", DepartmentId = 1, IsActive = true };
        _employees.Add(stranger);

        _leaveRequests.Add(new LeaveRequest
        {
            Id = 1,
            EmployeeId = _employee.Id,
            ApproverId = _manager.Id,
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday,
            NumberOfDays = 1,
            Status = LeaveRequestStatus.Pending
        });

        var result = await service.DecideAsync(stranger.ApplicationUserId, callerIsHrAdmin: false, id: 1, approve: true, comment: null);

        Assert.False(result.Succeeded);
        Assert.Contains("not authorized", result.Errors.First());
    }

    [Fact]
    public async Task DecideAsync_AllowsHrFallback_EvenWithoutLinkedEmployeeProfile()
    {
        // Regression test for a real bug found during manual testing: the seeded HRAdmin
        // account has no linked Employee row, but must still be able to act as the fallback
        // approver for a manager-less employee's request.
        var service = CreateService();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        _leaveRequests.Add(new LeaveRequest
        {
            Id = 1,
            EmployeeId = _employee.Id,
            ApproverId = null,
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday,
            NumberOfDays = 1,
            Status = LeaveRequestStatus.Pending
        });

        var result = await service.DecideAsync("unlinked-hr-user-id", callerIsHrAdmin: true, id: 1, approve: true, comment: "Approved by HR");

        Assert.True(result.Succeeded);
        Assert.Equal(LeaveRequestStatus.Approved, _leaveRequests.Single().Status);
    }

    [Fact]
    public async Task DecideAsync_RejectsActingOnOwnRequest()
    {
        // Contrived data state (ApproverId never naturally equals the requester's own id in
        // production, since it's always set to the requester's manager) exercising the
        // defensive self-approval guard directly.
        var service = CreateService();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        _leaveRequests.Add(new LeaveRequest
        {
            Id = 1,
            EmployeeId = _manager.Id,
            ApproverId = _manager.Id,
            LeaveTypeId = _annual.Id,
            StartDate = monday,
            EndDate = monday,
            NumberOfDays = 1,
            Status = LeaveRequestStatus.Pending
        });

        var result = await service.DecideAsync(_manager.ApplicationUserId, callerIsHrAdmin: false, id: 1, approve: true, comment: null);

        Assert.False(result.Succeeded);
        Assert.Contains("own leave request", result.Errors.First());
    }
}
