using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<IEnumerable<LeaveRequestDto>> GetMyRequestsAsync(string applicationUserId);
    Task<IEnumerable<LeaveRequestDto>> GetTeamPendingAsync(string applicationUserId);
    Task<IEnumerable<LeaveRequestDto>> GetTeamCalendarAsync(string applicationUserId, DateOnly from, DateOnly to);
    Task<PagedResult<LeaveRequestDto>> SearchAsync(LeaveRequestQueryDto query);
    Task<byte[]> ExportCsvAsync(LeaveRequestQueryDto query);
    Task<LeaveRequestDto?> GetByIdAsync(int id);
    Task<ServiceResult<LeaveRequestDto>> CreateAsync(string applicationUserId, CreateLeaveRequestDto request);
    Task<ServiceResult<LeaveRequestDto>> UpdateAsync(string applicationUserId, int id, CreateLeaveRequestDto request);
    Task<ServiceResult> WithdrawAsync(string applicationUserId, int id);
    Task<ServiceResult> CancelAsync(string applicationUserId, int id);
    Task<ServiceResult> DecideAsync(string applicationUserId, bool callerIsHrAdmin, int id, bool approve, string? comment);
    Task CancelAllPendingForEmployeeAsync(int employeeId);
}
