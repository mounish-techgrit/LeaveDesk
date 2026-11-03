namespace LeaveDesk.Application.DTOs;

public class AuthResult
{
    public bool Succeeded { get; private init; }
    public AuthResponseDto? Data { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static AuthResult Success(AuthResponseDto data) => new() { Succeeded = true, Data = data };
    public static AuthResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
}
