using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LeaveDesk.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaveDesk.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly string _fromAddress;

    public SmtpEmailService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _clientId = RequireConfig(configuration, "Email:OAuth:ClientId");
        _clientSecret = RequireConfig(configuration, "Email:OAuth:ClientSecret");
        _refreshToken = RequireConfig(configuration, "Email:OAuth:RefreshToken");
        _fromAddress = RequireConfig(configuration, "Email:FromAddress");
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // Notifications are a side effect of the business operation, not part of it: a transient
        // Gmail/network failure here must not fail the leave request, approval, etc. that triggered it.
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_fromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(new SaslMechanismOAuth2(_fromAddress, accessToken));
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("Email sent to {To} | {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} | {Subject}", to, subject);
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var http = _httpClientFactory.CreateClient();

        var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken,
            ["grant_type"] = "refresh_token"
        }));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Google OAuth token response did not include an access token.");
    }

    private static string RequireConfig(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"{key} is not configured.");

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
