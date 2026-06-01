#nullable enable

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SecureChat.Domain.Interfaces.Services;

namespace SecureChat.Infrastructure.Services;

/// <summary>
/// Provides SMTP email delivery using MailKit.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendOtpAsync(string email, string otpCode, string displayName)
    {
        _logger.LogInformation("Sending OTP email to {Email}.", email);

        if (!IsConfigured() && ShouldLogOtpWhenSmtpMissing())
        {
            _logger.LogWarning("SMTP is not configured. Development OTP for {Email}: {OtpCode}", email, otpCode);
            return;
        }

        var body = $"""
            <!doctype html>
            <html>
            <body style="margin:0;background:#f5f7fb;font-family:Segoe UI,Arial,sans-serif;color:#172033;">
              <div style="max-width:560px;margin:32px auto;background:#ffffff;border:1px solid #e6eaf2;border-radius:8px;overflow:hidden;">
                <div style="padding:24px 28px;background:#172033;color:#ffffff;">
                  <h1 style="margin:0;font-size:22px;">SecureChat verification</h1>
                </div>
                <div style="padding:28px;">
                  <p style="font-size:16px;margin:0 0 16px;">Hello {System.Net.WebUtility.HtmlEncode(displayName)},</p>
                  <p style="font-size:15px;line-height:1.5;margin:0 0 20px;">Use this verification code to continue your SecureChat registration.</p>
                  <div style="font-size:34px;letter-spacing:8px;font-weight:700;text-align:center;padding:18px;background:#eef3ff;border-radius:8px;color:#2347a6;">{System.Net.WebUtility.HtmlEncode(otpCode)}</div>
                  <p style="font-size:13px;color:#667085;margin:20px 0 0;">This code expires soon. If you did not request it, ignore this email.</p>
                </div>
              </div>
            </body>
            </html>
            """;

        await SendEmailAsync(email, "Your SecureChat verification code", body);
    }

    /// <inheritdoc />
    public async Task SendSecurityAlertAsync(string email, string eventType, string deviceName, string ipAddress)
    {
        _logger.LogInformation("Sending security alert {EventType} to {Email}.", eventType, email);

        var body = $"""
            <!doctype html>
            <html>
            <body style="margin:0;background:#f5f7fb;font-family:Segoe UI,Arial,sans-serif;color:#172033;">
              <div style="max-width:560px;margin:32px auto;background:#ffffff;border:1px solid #e6eaf2;border-radius:8px;overflow:hidden;">
                <div style="padding:24px 28px;background:#7a1f1f;color:#ffffff;">
                  <h1 style="margin:0;font-size:22px;">SecureChat security alert</h1>
                </div>
                <div style="padding:28px;">
                  <p style="font-size:15px;line-height:1.5;margin:0 0 16px;">A security event was recorded for your account.</p>
                  <p><strong>Event:</strong> {System.Net.WebUtility.HtmlEncode(eventType)}</p>
                  <p><strong>Device:</strong> {System.Net.WebUtility.HtmlEncode(deviceName)}</p>
                  <p><strong>IP address:</strong> {System.Net.WebUtility.HtmlEncode(ipAddress)}</p>
                  <p style="font-size:13px;color:#667085;margin:20px 0 0;">If this was not you, revoke the device immediately from SecureChat.</p>
                </div>
              </div>
            </body>
            </html>
            """;

        await SendEmailAsync(email, "SecureChat security alert", body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("SMTP is not configured. Skipping email '{Subject}' to {Email}.", subject, toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(GetRequiredConfig("Email:FromName"), GetRequiredConfig("Email:FromEmail")));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(GetRequiredConfig("Email:Host"), GetRequiredInt("Email:Port"), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(GetRequiredConfig("Email:Username"), GetRequiredConfig("Email:Password"));
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string GetRequiredConfig(string key)
    {
        return _configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Email:Host"])
            && !string.IsNullOrWhiteSpace(_configuration["Email:Username"])
            && !string.IsNullOrWhiteSpace(_configuration["Email:Password"])
            && !string.IsNullOrWhiteSpace(_configuration["Email:FromEmail"]);
    }

    private bool ShouldLogOtpWhenSmtpMissing()
    {
        return bool.TryParse(_configuration["Email:LogOtpWhenSmtpMissing"], out var value) && value;
    }

    private int GetRequiredInt(string key)
    {
        return int.TryParse(GetRequiredConfig(key), out var value)
            ? value
            : throw new InvalidOperationException($"Configuration value '{key}' must be an integer.");
    }
}
