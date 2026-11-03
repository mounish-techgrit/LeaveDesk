namespace LeaveDesk.Application.DTOs;

public class ServiceResult
{
    public bool Succeeded { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string error) => new() { Succeeded = false, Errors = [error] };
}

public class ServiceResult<T>
{
    public bool Succeeded { get; private init; }
    public T? Data { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static ServiceResult<T> Failure(string error) => new() { Succeeded = false, Errors = [error] };
}
