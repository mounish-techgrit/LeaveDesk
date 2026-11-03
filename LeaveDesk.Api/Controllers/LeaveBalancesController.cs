using System.Security.Claims;
using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeService _employeeService;

    public LeaveBalancesController(ILeaveBalanceService leaveBalanceService, IEmployeeService employeeService)
    {
        _leaveBalanceService = leaveBalanceService;
        _employeeService = employeeService;
    }

    private string CallerUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("me")]
    public async Task<IActionResult> GetMyBalances([FromQuery] int? year)
    {
        var me = await _employeeService.GetByApplicationUserIdAsync(CallerUserId);
        if (me is null)
        {
            return NotFound("Your account is not yet linked to an employee profile. Contact HR.");
        }

        var balances = await _leaveBalanceService.GetForEmployeeAsync(me.Id, year);
        return Ok(balances);
    }

    [HttpGet("{employeeId:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> GetForEmployee(int employeeId, [FromQuery] int? year)
    {
        var balances = await _leaveBalanceService.GetForEmployeeAsync(employeeId, year);
        return Ok(balances);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Adjust(AdjustBalanceRequestDto request)
    {
        // HR may act here without a linked Employee profile of their own (e.g. the seeded admin account),
        // so the "adjusted by" audit field is best-effort and may be null.
        var me = await _employeeService.GetByApplicationUserIdAsync(CallerUserId);

        var result = await _leaveBalanceService.AdjustAsync(request.EmployeeId, request.LeaveTypeId, request.Year, request.AdjustmentAmount, request.Reason, me?.Id);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
