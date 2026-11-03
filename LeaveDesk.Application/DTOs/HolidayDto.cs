namespace LeaveDesk.Application.DTOs;

public class HolidayDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}
