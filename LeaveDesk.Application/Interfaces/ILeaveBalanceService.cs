using LeaveDesk.Application.DTOs;

namespace LeaveDesk.Application.Interfaces;

public interface ILeaveBalanceService
{
    Task<IEnumerable<LeaveBalanceDto>> GetForEmployeeAsync(int employeeId, int? year);
    Task<decimal> GetRemainingDaysAsync(int employeeId, int leaveTypeId, int year);
    Task<ServiceResult> ReserveDaysAsync(int employeeId, int leaveTypeId, int year, decimal days, bool allowExceedingBalance);
    Task RestoreDaysAsync(int employeeId, int leaveTypeId, int year, decimal days);
    Task<ServiceResult<LeaveBalanceDto>> AdjustAsync(int employeeId, int leaveTypeId, int year, decimal amount, string reason, int? adjustedByEmployeeId);
}
