using System.Security.Claims;
using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _employeeService.GetByApplicationUserIdAsync(callerUserId);
        return profile is null
            ? NotFound("Your account is not yet linked to an employee profile. Contact HR.")
            : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(UpdateMyProfileRequestDto request)
    {
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _employeeService.UpdateMyProfileAsync(callerUserId, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!User.IsInRole("HRAdmin"))
        {
            var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var myProfile = await _employeeService.GetByApplicationUserIdAsync(callerUserId);
            if (myProfile is null || myProfile.Id != id)
            {
                return Forbid();
            }
        }

        var employee = await _employeeService.GetByIdAsync(id);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Create(CreateEmployeeRequestDto request)
    {
        var result = await _employeeService.CreateAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequestDto request)
    {
        var result = await _employeeService.UpdateAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:int}/role")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> AssignRole(int id, AssignRoleRequestDto request)
    {
        var result = await _employeeService.AssignRoleAsync(id, request.Role);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _employeeService.DeleteAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
