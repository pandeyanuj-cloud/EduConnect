using EduConnect.Models;
using EduConnect.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Controllers
{
    [ApiController]
    [Route("api/topper")]
    public class TopperController : ControllerBase
    {
        private readonly TopperRepository _repo;
        private readonly UserRepository _users;
        private readonly IWebHostEnvironment _environment;

        public TopperController(TopperRepository repo, UserRepository users, IWebHostEnvironment environment)
        {
            _repo = repo;
            _users = users;
            _environment = environment;
        }

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = GetTopperUserId();
            if (userId <= 0) return Unauthorized(new { message = "Please log in as a topper." });
            return Ok(_repo.GetProfileForUser(userId));
        }

        [HttpPost("profile")]
        public IActionResult SaveProfile([FromBody] TopperModel request)
        {
            var userId = GetTopperUserId();
            if (userId <= 0) return Unauthorized(new { message = "Please log in as a topper." });
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Standard)) return BadRequest(new { message = "Name and standard are required." });
            if (request.Marks10 is < 0 or > 100 || request.Marks12 is < 0 or > 100) return BadRequest(new { message = "Marks must be between 0 and 100." });
            request.Name = request.Name.Trim();
            request.Standard = request.Standard.Trim();
            return Ok(_repo.Save(userId, request));
        }

        [HttpPost("files")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (GetTopperUserId() <= 0) return Unauthorized(new { message = "Please log in as a topper." });
            if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Choose a file smaller than 5 MB." });
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp" and not ".pdf") return BadRequest(new { message = "Only JPG, PNG, WEBP, and PDF files are supported." });
            var directory = Path.Combine(_environment.WebRootPath, "images");
            Directory.CreateDirectory(directory);
            var fileName = $"topper-{Guid.NewGuid():N}{extension}";
            await using var stream = System.IO.File.Create(Path.Combine(directory, fileName));
            await file.CopyToAsync(stream);
            return Ok(new { fileUrl = $"/images/{fileName}" });
        }

        [HttpGet("enquiries")]
        public IActionResult GetEnquiries()
        {
            var userId = GetTopperUserId();
            return userId <= 0 ? Unauthorized(new { message = "Please log in as a topper." }) : Ok(_repo.GetEnquiries(userId));
        }

        private int GetTopperUserId()
        {
            if (!int.TryParse(Request.Cookies["eduConnectUserId"], out var userId)) return 0;
            var user = _users.GetUserById(userId);
            return user != null && user.Role.Equals("Topper", StringComparison.OrdinalIgnoreCase) ? userId : 0;
        }
    }
}