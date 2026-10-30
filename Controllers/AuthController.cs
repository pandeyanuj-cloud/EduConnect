using Microsoft.AspNetCore.Mvc;
using EduConnect.Models;
using EduConnect.Repositories;
using EduConnect.Services;
using System.Text.RegularExpressions;


namespace EduConnect.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly PasswordResetService _passwordResetService;

        public AuthController(UserRepository repo, PasswordResetService passwordResetService)
        {
            _repo = repo;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("signup")]
        public IActionResult Signup([FromBody] SignupRequest request)
        {
            var email = request.Email?.Trim();
            var fullName = request.FullName?.Trim();
            var phone = request.Phone?.Trim();
            var role = NormalizeRole(request.Role);

            if (string.IsNullOrWhiteSpace(fullName) || !Regex.IsMatch(fullName, @"^[a-zA-Z\s]+$"))
                return BadRequest(new { message = "Name can only contain letters and spaces" });

            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[a-zA-Z0-9]+@[a-zA-Z0-9.]+\.[a-zA-Z]{2,}$"))
                return BadRequest(new { message = "Invalid email format" });

            if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^[0-9]{10}$"))
                return BadRequest(new { message = "Phone number must be exactly 10 digits" });

            if (string.IsNullOrWhiteSpace(role))
                return BadRequest(new { message = "Please select your account type" });

            if (_repo.EmailExists(email))
                return BadRequest(new { message = "Email already registered" });

            var user = new UserModel
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone = phone,
                Role = role
            };

            _repo.CreateUser(user);
            Response.Cookies.Append("eduConnectUserId", user.UserId.ToString(), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = Request.IsHttps });
            return Ok(new { message = "Account created", role = user.Role, fullName = user.FullName, email = user.Email, userId = user.UserId });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var email = request.Email?.Trim();
            var password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required" });

            var user = _repo.GetUserByEmail(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            var role = NormalizeRole(user.Role);
            Response.Cookies.Append("eduConnectUserId", user.UserId.ToString(), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = Request.IsHttps });
            return Ok(new { message = "Login successful", role = role, fullName = user.FullName, email = user.Email, userId = user.UserId });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("eduConnectUserId");
            return Ok(new { message = "Logged out." });
        }

        [HttpPost("forgot-password/request-otp")]
        public async Task<IActionResult> RequestPasswordResetOtp([FromBody] ForgotPasswordRequest request)
        {
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[a-zA-Z0-9]+@[a-zA-Z0-9.]+\.[a-zA-Z]{2,}$"))
                return BadRequest(new { message = "Invalid email format" });

            var user = _repo.GetUserByEmail(email);
            if (user == null)
                return NotFound(new { message = "No account found with that email." });

            try
            {
                await _passwordResetService.RequestOtpAsync(user);
                return Ok(new { message = "OTP sent to your email." });
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(500, new { message = exception.Message });
            }
        }

        [HttpPost("forgot-password/verify-otp")]
        public IActionResult VerifyPasswordResetOtp([FromBody] VerifyOtpRequest request)
        {
            var email = request.Email?.Trim() ?? string.Empty;
            if (_passwordResetService.VerifyOtp(email, request.Otp?.Trim() ?? string.Empty, out var resetToken))
                return Ok(new { resetToken });

            return BadRequest(new { message = "Invalid or expired OTP." });
        }

        [HttpPost("forgot-password/reset")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword != request.RewritePassword)
                return BadRequest(new { message = "Passwords do not match." });

            if (!_passwordResetService.TryUseResetToken(request.Email?.Trim() ?? string.Empty, request.ResetToken ?? string.Empty, out var userId))
                return BadRequest(new { message = "Your password reset session has expired. Please request a new OTP." });

            if (!_repo.UpdatePassword(userId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword)))
                return BadRequest(new { message = "Unable to reset password." });

            return Ok(new { message = "Password reset successfully. Please login with your new password." });
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;

            var normalized = role.Trim();
            if (normalized.Equals("student", StringComparison.OrdinalIgnoreCase)) return "Student";
            if (normalized.Equals("topper", StringComparison.OrdinalIgnoreCase)) return "Topper";
            if (normalized.Equals("classowner", StringComparison.OrdinalIgnoreCase) || normalized.Equals("class owner", StringComparison.OrdinalIgnoreCase)) return "ClassOwner";

            return normalized;
        }
    }

    public class SignupRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string RewritePassword { get; set; } = string.Empty;
    }
}