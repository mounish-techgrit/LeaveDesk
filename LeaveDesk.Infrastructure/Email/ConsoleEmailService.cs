using LeaveDesk.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Infrastructure.Email;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation("EMAIL -> {To} | {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
