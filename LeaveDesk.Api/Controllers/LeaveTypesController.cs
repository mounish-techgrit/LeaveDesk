using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;

    public LeaveTypesController(ILeaveTypeService leaveTypeService)
    {
        _leaveTypeService = leaveTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var leaveTypes = await _leaveTypeService.GetAllAsync();
        return Ok(leaveTypes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var leaveType = await _leaveTypeService.GetByIdAsync(id);
        return leaveType is null ? NotFound() : Ok(leaveType);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Create(LeaveTypeRequestDto request)
    {
        var result = await _leaveTypeService.CreateAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Update(int id, LeaveTypeRequestDto request)
    {
        var result = await _leaveTypeService.UpdateAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _leaveTypeService.DeleteAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
