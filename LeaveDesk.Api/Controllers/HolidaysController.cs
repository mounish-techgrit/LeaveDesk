using LeaveDesk.Application.DTOs;
using LeaveDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidaysController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var holidays = await _holidayService.GetAllAsync();
        return Ok(holidays);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var holiday = await _holidayService.GetByIdAsync(id);
        return holiday is null ? NotFound() : Ok(holiday);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Create(HolidayRequestDto request)
    {
        var result = await _holidayService.CreateAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Update(int id, HolidayRequestDto request)
    {
        var result = await _holidayService.UpdateAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HRAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _holidayService.DeleteAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
