namespace LeaveDesk.Application.DTOs;

public class DecideLeaveRequestDto
{
    public bool Approve { get; set; }
    public string? Comment { get; set; }
}
