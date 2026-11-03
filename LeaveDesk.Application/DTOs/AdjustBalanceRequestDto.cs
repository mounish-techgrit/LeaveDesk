using System.ComponentModel.DataAnnotations;

namespace LeaveDesk.Application.DTOs;

public class AdjustBalanceRequestDto
{
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    public decimal AdjustmentAmount { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
