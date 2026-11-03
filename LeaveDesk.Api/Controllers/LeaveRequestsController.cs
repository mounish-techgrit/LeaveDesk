using System.Security.Claims;
using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService, IEmployeeService employeeService)
    {
        _leaveRequestService = leaveRequestService;
        _employeeService = employeeService;
    }

    private string CallerUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests()
    {
        var requests = await _leaveRequestService.GetMyRequestsAsync(CallerUserId);
        return Ok(requests);
    }

    [HttpGet("team-pending")]
    public async Task<IActionResult> GetTeamPending()
    {
        var requests = await _leaveRequestService.GetTeamPendingAsync(CallerUserId);
        return Ok(requests);
    }

    [HttpGet("team-calendar")]
    public async Task<IActionResult> GetTeamCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (to < from)
        {
            return BadRequest(new[] { "'to' date must not be before 'from' date." });
        }

        var requests = await _leaveRequestService.GetTeamCalendarAsync(CallerUserId, from, to);
        return Ok(requests);
    }

    [HttpGet]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] LeaveRequestQueryDto query)
    {
        var result = await _leaveRequestService.SearchAsync(query);
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Export([FromQuery] LeaveRequestQueryDto query)
    {
        var csvBytes = await _leaveRequestService.ExportCsvAsync(query);
        return File(csvBytes, "text/csv", "leave-requests.csv");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _leaveRequestService.GetByIdAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("HRAdmin"))
        {
            var me = await _employeeService.GetByApplicationUserIdAsync(CallerUserId);
            if (me is null || (request.EmployeeId != me.Id && request.ApproverId != me.Id))
            {
                return Forbid();
            }
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestDto request)
    {
        var result = await _leaveRequestService.CreateAsync(CallerUserId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateLeaveRequestDto request)
    {
        var result = await _leaveRequestService.UpdateAsync(CallerUserId, id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _leaveRequestService.WithdrawAsync(CallerUserId, id);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _leaveRequestService.CancelAsync(CallerUserId, id);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpPost("{id:int}/decide")]
    public async Task<IActionResult> Decide(int id, DecideLeaveRequestDto request)
    {
        var result = await _leaveRequestService.DecideAsync(CallerUserId, User.IsInRole("HRAdmin"), id, request.Approve, request.Comment);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
