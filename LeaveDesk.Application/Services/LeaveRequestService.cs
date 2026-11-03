using System.Text;
using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using LeaveDesk.Domain.Entities;
using LeaveDesk.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly IRepository<LeaveRequest> _leaveRequestRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<LeaveType> _leaveTypeRepository;
    private readonly IRepository<Holiday> _holidayRepository;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmailService _emailService;
    private readonly ILogger<LeaveRequestService> _logger;

    public LeaveRequestService(
        IRepository<LeaveRequest> leaveRequestRepository,
        IRepository<Employee> employeeRepository,
        IRepository<LeaveType> leaveTypeRepository,
        IRepository<Holiday> holidayRepository,
        ILeaveBalanceService leaveBalanceService,
        IEmailService emailService,
        ILogger<LeaveRequestService> logger)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _holidayRepository = holidayRepository;
        _leaveBalanceService = leaveBalanceService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetMyRequestsAsync(string applicationUserId)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return [];
        }

        var requests = (await _leaveRequestRepository.GetAllAsync())
            .Where(r => r.EmployeeId == employee.Id)
            .OrderByDescending(r => r.RequestedAt);

        return await ToDtosAsync(requests);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetTeamPendingAsync(string applicationUserId)
    {
        var manager = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (manager is null)
        {
            return [];
        }

        var requests = (await _leaveRequestRepository.GetAllAsync())
            .Where(r => r.ApproverId == manager.Id && r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt);

        return await ToDtosAsync(requests);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetTeamCalendarAsync(string applicationUserId, DateOnly from, DateOnly to)
    {
        var manager = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (manager is null)
        {
            return [];
        }

        var directReportIds = (await _employeeRepository.GetAllAsync())
            .Where(e => e.ManagerId == manager.Id)
            .Select(e => e.Id)
            .ToHashSet();

        var requests = (await _leaveRequestRepository.GetAllAsync())
            .Where(r => directReportIds.Contains(r.EmployeeId)
                && r.Status == LeaveRequestStatus.Approved
                && r.StartDate <= to && r.EndDate >= from)
            .OrderBy(r => r.StartDate);

        return await ToDtosAsync(requests);
    }

    public async Task<PagedResult<LeaveRequestDto>> SearchAsync(LeaveRequestQueryDto query)
    {
        var filtered = (await FilterAsync(query)).OrderByDescending(r => r.RequestedAt).ToList();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize);

        return new PagedResult<LeaveRequestDto>
        {
            Items = await ToDtosAsync(pageItems),
            TotalCount = filtered.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<byte[]> ExportCsvAsync(LeaveRequestQueryDto query)
    {
        var filtered = (await FilterAsync(query)).OrderBy(r => r.StartDate).ToList();
        var dtos = await ToDtosAsync(filtered);

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeName,LeaveType,StartDate,EndDate,DayType,NumberOfDays,Status,RequestedAt,ApproverName,ActionedAt");
        foreach (var dto in dtos)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(dto.EmployeeName),
                CsvEscape(dto.LeaveTypeName),
                dto.StartDate.ToString("yyyy-MM-dd"),
                dto.EndDate.ToString("yyyy-MM-dd"),
                dto.DayType,
                dto.NumberOfDays,
                dto.Status,
                dto.RequestedAt.ToString("yyyy-MM-dd HH:mm"),
                CsvEscape(dto.ApproverName ?? string.Empty),
                dto.ActionedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(int id)
    {
        var request = await _leaveRequestRepository.GetByIdAsync(id);
        return request is null ? null : await ToDtoAsync(request);
    }

    public async Task<ServiceResult<LeaveRequestDto>> CreateAsync(string applicationUserId, CreateLeaveRequestDto request)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Your account is not yet linked to an employee profile.");
        }

        if (!employee.IsActive)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Deactivated employees cannot apply for leave.");
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(request.LeaveTypeId);
        if (leaveType is null)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Leave type not found.");
        }

        var (ok, error, days) = await ValidateAsync(employee, leaveType, request, null);
        if (!ok)
        {
            return ServiceResult<LeaveRequestDto>.Failure(error!);
        }

        var reserveResult = await _leaveBalanceService.ReserveDaysAsync(employee.Id, leaveType.Id, request.StartDate.Year, days, leaveType.AllowExceedingBalance);
        if (!reserveResult.Succeeded)
        {
            return ServiceResult<LeaveRequestDto>.Failure(reserveResult.Errors.First());
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DayType = request.DayType,
            Reason = request.Reason,
            NumberOfDays = days,
            Status = LeaveRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            ApproverId = employee.ManagerId
        };

        await _leaveRequestRepository.AddAsync(leaveRequest);
        _logger.LogInformation(
            "Leave request {RequestId} created by employee {EmployeeId} ({Days} day(s) of {LeaveType}).",
            leaveRequest.Id, employee.Id, days, leaveType.Name);

        if (employee.ManagerId.HasValue)
        {
            var manager = await _employeeRepository.GetByIdAsync(employee.ManagerId.Value);
            if (manager is not null)
            {
                await _emailService.SendAsync(
                    manager.Email,
                    "LeaveDesk: New leave request to review",
                    $"{employee.FullName} submitted a {leaveType.Name} request for {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd} ({days} day(s)). Reason: {request.Reason}");
            }
        }

        return ServiceResult<LeaveRequestDto>.Success(await ToDtoAsync(leaveRequest));
    }

    public async Task<ServiceResult<LeaveRequestDto>> UpdateAsync(string applicationUserId, int id, CreateLeaveRequestDto request)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Your account is not yet linked to an employee profile.");
        }

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest is null || leaveRequest.EmployeeId != employee.Id)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Only pending requests can be edited.");
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(request.LeaveTypeId);
        if (leaveType is null)
        {
            return ServiceResult<LeaveRequestDto>.Failure("Leave type not found.");
        }

        var (ok, error, days) = await ValidateAsync(employee, leaveType, request, id);
        if (!ok)
        {
            return ServiceResult<LeaveRequestDto>.Failure(error!);
        }

        var sameBucket = leaveRequest.LeaveTypeId == leaveType.Id && leaveRequest.StartDate.Year == request.StartDate.Year;
        var currentRemaining = await _leaveBalanceService.GetRemainingDaysAsync(employee.Id, leaveType.Id, request.StartDate.Year);
        var availableForNew = sameBucket ? currentRemaining + leaveRequest.NumberOfDays : currentRemaining;

        if (!leaveType.AllowExceedingBalance && days > availableForNew)
        {
            return ServiceResult<LeaveRequestDto>.Failure("This change exceeds the available leave balance.");
        }

        await _leaveBalanceService.RestoreDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year, leaveRequest.NumberOfDays);
        await _leaveBalanceService.ReserveDaysAsync(employee.Id, leaveType.Id, request.StartDate.Year, days, true);

        leaveRequest.LeaveTypeId = leaveType.Id;
        leaveRequest.StartDate = request.StartDate;
        leaveRequest.EndDate = request.EndDate;
        leaveRequest.DayType = request.DayType;
        leaveRequest.Reason = request.Reason;
        leaveRequest.NumberOfDays = days;

        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        return ServiceResult<LeaveRequestDto>.Success(await ToDtoAsync(leaveRequest));
    }

    public async Task<ServiceResult> WithdrawAsync(string applicationUserId, int id)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return ServiceResult.Failure("Your account is not yet linked to an employee profile.");
        }

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest is null || leaveRequest.EmployeeId != employee.Id)
        {
            return ServiceResult.Failure("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return ServiceResult.Failure("Only pending requests can be withdrawn.");
        }

        leaveRequest.Status = LeaveRequestStatus.Withdrawn;
        leaveRequest.ActionedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        await _leaveBalanceService.RestoreDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year, leaveRequest.NumberOfDays);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelAsync(string applicationUserId, int id)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);
        if (employee is null)
        {
            return ServiceResult.Failure("Your account is not yet linked to an employee profile.");
        }

        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest is null || leaveRequest.EmployeeId != employee.Id)
        {
            return ServiceResult.Failure("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Approved)
        {
            return ServiceResult.Failure("Only approved requests can be cancelled.");
        }

        if (leaveRequest.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ServiceResult.Failure("This leave has already started and can no longer be cancelled.");
        }

        leaveRequest.Status = LeaveRequestStatus.Cancelled;
        leaveRequest.ActionedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        await _leaveBalanceService.RestoreDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year, leaveRequest.NumberOfDays);

        _logger.LogInformation("Leave request {RequestId} cancelled by employee {EmployeeId}.", leaveRequest.Id, employee.Id);

        await _emailService.SendAsync(
            employee.Email,
            "LeaveDesk: Your leave request was cancelled",
            $"Your {leaveRequest.NumberOfDays} day(s) request ({leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}) has been cancelled.");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DecideAsync(string applicationUserId, bool callerIsHrAdmin, int id, bool approve, string? comment)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id);
        if (leaveRequest is null)
        {
            return ServiceResult.Failure("Leave request not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return ServiceResult.Failure("This request has already been actioned.");
        }

        // HR acts as the fallback approver for manager-less employees, and may not have
        // (or need) a linked Employee profile of their own, so resolution here is optional.
        var caller = await _employeeRepository.FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);

        if (caller is not null && leaveRequest.EmployeeId == caller.Id)
        {
            return ServiceResult.Failure("You cannot act on your own leave request.");
        }

        var isDesignatedApprover = caller is not null && leaveRequest.ApproverId == caller.Id;
        var isHrFallback = leaveRequest.ApproverId is null && callerIsHrAdmin;

        if (!isDesignatedApprover && !isHrFallback)
        {
            return ServiceResult.Failure("You are not authorized to act on this request.");
        }

        leaveRequest.Status = approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        leaveRequest.ApproverComment = comment;
        leaveRequest.ActionedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(leaveRequest);

        _logger.LogInformation(
            "Leave request {RequestId} {Status} by {Actor}.",
            leaveRequest.Id, leaveRequest.Status, caller?.Id.ToString() ?? "HR (fallback)");

        if (!approve)
        {
            await _leaveBalanceService.RestoreDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year, leaveRequest.NumberOfDays);
        }

        var requester = await _employeeRepository.GetByIdAsync(leaveRequest.EmployeeId);
        if (requester is not null)
        {
            var commentLine = string.IsNullOrWhiteSpace(comment) ? string.Empty : $" Comment: {comment}";
            await _emailService.SendAsync(
                requester.Email,
                $"LeaveDesk: Your leave request was {leaveRequest.Status}",
                $"Your {leaveRequest.NumberOfDays} day(s) request ({leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}) was {leaveRequest.Status}.{commentLine}");
        }

        return ServiceResult.Success();
    }

    public async Task CancelAllPendingForEmployeeAsync(int employeeId)
    {
        var pendingRequests = (await _leaveRequestRepository.GetAllAsync())
            .Where(r => r.EmployeeId == employeeId && r.Status == LeaveRequestStatus.Pending)
            .ToList();

        foreach (var leaveRequest in pendingRequests)
        {
            leaveRequest.Status = LeaveRequestStatus.Cancelled;
            leaveRequest.ApproverComment = "Auto-cancelled: employee was deactivated.";
            leaveRequest.ActionedAt = DateTime.UtcNow;
            await _leaveRequestRepository.UpdateAsync(leaveRequest);

            await _leaveBalanceService.RestoreDaysAsync(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year, leaveRequest.NumberOfDays);
        }
    }

    private async Task<IEnumerable<LeaveRequest>> FilterAsync(LeaveRequestQueryDto query)
    {
        var requests = await _leaveRequestRepository.GetAllAsync();
        var employees = (await _employeeRepository.GetAllAsync()).ToDictionary(e => e.Id);

        return requests.Where(r =>
            (!query.EmployeeId.HasValue || r.EmployeeId == query.EmployeeId.Value) &&
            (!query.LeaveTypeId.HasValue || r.LeaveTypeId == query.LeaveTypeId.Value) &&
            (!query.Status.HasValue || r.Status == query.Status.Value) &&
            (!query.FromDate.HasValue || r.EndDate >= query.FromDate.Value) &&
            (!query.ToDate.HasValue || r.StartDate <= query.ToDate.Value) &&
            (!query.DepartmentId.HasValue || (employees.TryGetValue(r.EmployeeId, out var emp) && emp.DepartmentId == query.DepartmentId.Value)));
    }

    private static string CsvEscape(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n'))
        {
            text = "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }

    private async Task<(bool ok, string? error, decimal days)> ValidateAsync(Employee employee, LeaveType leaveType, CreateLeaveRequestDto request, int? excludingRequestId)
    {
        if (request.EndDate < request.StartDate)
        {
            return (false, "End date cannot be before start date.", 0);
        }

        if (request.DayType == LeaveDayType.HalfDay && request.StartDate != request.EndDate)
        {
            return (false, "A half-day request must have the same start and end date.", 0);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.StartDate < today)
        {
            var daysInPast = today.DayNumber - request.StartDate.DayNumber;
            if (daysInPast > leaveType.MaxPastDaysAllowed)
            {
                return (false, $"{leaveType.Name} leave cannot be backdated more than {leaveType.MaxPastDaysAllowed} day(s).", 0);
            }
        }

        var overlapping = await _leaveRequestRepository.ExistsAsync(r =>
            r.EmployeeId == employee.Id &&
            (!excludingRequestId.HasValue || r.Id != excludingRequestId.Value) &&
            (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved) &&
            r.StartDate <= request.EndDate && r.EndDate >= request.StartDate);

        if (overlapping)
        {
            return (false, "This request overlaps an existing pending or approved leave request.", 0);
        }

        var holidays = (await _holidayRepository.GetAllAsync())
            .Select(h => h.Date)
            .Where(d => d >= request.StartDate && d <= request.EndDate)
            .ToHashSet();

        decimal days;
        if (request.DayType == LeaveDayType.HalfDay)
        {
            var isWorkingDay = request.StartDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(request.StartDate);
            days = isWorkingDay ? 0.5m : 0m;
        }
        else
        {
            days = 0;
            for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                if (holidays.Contains(date))
                {
                    continue;
                }

                days++;
            }
        }

        if (days <= 0)
        {
            return (false, "The selected date range contains no working days.", 0);
        }

        return (true, null, days);
    }

    private static LeaveRequestDto MapToDto(LeaveRequest r, Employee? employee, LeaveType? leaveType, Employee? approver) => new()
    {
        Id = r.Id,
        EmployeeId = r.EmployeeId,
        EmployeeName = employee?.FullName ?? string.Empty,
        LeaveTypeId = r.LeaveTypeId,
        LeaveTypeName = leaveType?.Name ?? string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        DayType = r.DayType,
        Reason = r.Reason,
        NumberOfDays = r.NumberOfDays,
        Status = r.Status,
        RequestedAt = r.RequestedAt,
        ApproverId = r.ApproverId,
        ApproverName = approver?.FullName,
        ApproverComment = r.ApproverComment,
        ActionedAt = r.ActionedAt
    };

    private async Task<LeaveRequestDto> ToDtoAsync(LeaveRequest r)
    {
        var employee = await _employeeRepository.GetByIdAsync(r.EmployeeId);
        var leaveType = await _leaveTypeRepository.GetByIdAsync(r.LeaveTypeId);
        var approver = r.ApproverId.HasValue ? await _employeeRepository.GetByIdAsync(r.ApproverId.Value) : null;
        return MapToDto(r, employee, leaveType, approver);
    }

    private async Task<IEnumerable<LeaveRequestDto>> ToDtosAsync(IEnumerable<LeaveRequest> requests)
    {
        var employees = (await _employeeRepository.GetAllAsync()).ToDictionary(e => e.Id);
        var leaveTypes = (await _leaveTypeRepository.GetAllAsync()).ToDictionary(lt => lt.Id);

        return requests.Select(r =>
        {
            employees.TryGetValue(r.EmployeeId, out var employee);
            leaveTypes.TryGetValue(r.LeaveTypeId, out var leaveType);
            Employee? approver = r.ApproverId.HasValue && employees.TryGetValue(r.ApproverId.Value, out var a) ? a : null;
            return MapToDto(r, employee, leaveType, approver);
        }).ToList();
    }
}
