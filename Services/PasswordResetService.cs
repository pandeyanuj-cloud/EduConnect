using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using EduConnect.Models;

namespace EduConnect.Services;

public sealed class PasswordResetService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, PasswordResetAttempt> _attempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly IConfiguration _configuration;

    public PasswordResetService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RequestOtpAsync(UserModel user)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        await SendOtpEmailAsync(user.Email, otp);
        _attempts[user.Email] = new PasswordResetAttempt(user.UserId, Hash(otp), DateTimeOffset.UtcNow.Add(OtpLifetime));
    }

    public bool VerifyOtp(string email, string otp, out string resetToken)
    {
        resetToken = string.Empty;
        if (!_attempts.TryGetValue(email, out var attempt) || attempt.ExpiresAt < DateTimeOffset.UtcNow || !CryptographicEquals(attempt.OtpHash, Hash(otp)))
            return false;

        resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _attempts[email] = attempt with { ResetToken = resetToken, ResetTokenExpiresAt = DateTimeOffset.UtcNow.Add(OtpLifetime) };
        return true;
    }

    public bool TryUseResetToken(string email, string resetToken, out int userId)
    {
        userId = 0;
        if (!_attempts.TryGetValue(email, out var attempt) || attempt.ResetToken != resetToken || attempt.ResetTokenExpiresAt < DateTimeOffset.UtcNow)
            return false;

        userId = attempt.UserId;
        _attempts.TryRemove(email, out _);
        return true;
    }

    private async Task SendOtpEmailAsync(string email, string otp)
    {
        var settings = _configuration.GetSection("Smtp");
        var host = settings["Host"];
        var from = settings["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP email settings are not configured.");

        using var message = new MailMessage(from, email)
        {
            Subject = "EduConnect password reset OTP",
            Body = $"Your EduConnect password reset OTP is {otp}. It expires in 10 minutes.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, settings.GetValue<int>("Port", 587))
        {
            EnableSsl = settings.GetValue("EnableSsl", true)
        };
        var username = settings["Username"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, settings["Password"]);

        await client.SendMailAsync(message);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool CryptographicEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));

    private sealed record PasswordResetAttempt(int UserId, string OtpHash, DateTimeOffset ExpiresAt)
    {
        public string? ResetToken { get; init; }
        public DateTimeOffset ResetTokenExpiresAt { get; init; }
    }
}