using EduConnect.Models;
using EduConnect.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Controllers
{
    [ApiController]
    [Route("api/owner")]
    public class OwnerController : ControllerBase
    {
        private readonly OwnerRepository _ownerRepository;
        private readonly UserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;

        public OwnerController(OwnerRepository ownerRepository, UserRepository userRepository, IWebHostEnvironment environment)
        {
            _ownerRepository = ownerRepository;
            _userRepository = userRepository;
            _environment = environment;
        }

        [HttpGet("classes")]
        public IActionResult GetClasses()
        {
            var ownerId = GetOwnerId();
            return ownerId <= 0 ? Unauthorized(new { message = "Please log in as a class owner." }) : Ok(_ownerRepository.GetClasses(ownerId));
        }

        [HttpPost("classes")]
        public IActionResult SaveClass([FromBody] OwnerClassRequest request)
        {
            var ownerId = GetOwnerId();
            if (ownerId <= 0) return Unauthorized(new { message = "Please log in as a class owner." });
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Standard))
                return BadRequest(new { message = "Subject, location, and standard are required." });
            if (request.Fee < 0 || request.Rating < 0 || request.Rating > 5)
                return BadRequest(new { message = "Fee must be positive and rating must be between 0 and 5." });
            if ((request.ContactNumber?.Length ?? 0) > 20 || (request.ClassTimings?.Length ?? 0) > 200)
                return BadRequest(new { message = "Contact number must be 20 characters or fewer and timings must be 200 characters or fewer." });

            var classItem = new ClassModel
            {
                ClassId = request.ClassId,
                Subject = request.Subject.Trim(),
                Fee = request.Fee,
                Location = request.Location.Trim(),
                Standard = request.Standard.Trim(),
                Rating = request.Rating,
                Mode = string.IsNullOrWhiteSpace(request.Mode) ? "Both" : request.Mode.Trim(),
                ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                MaterialProvided = request.MaterialProvided?.Trim() ?? string.Empty,
                ContactNumber = request.ContactNumber?.Trim() ?? string.Empty,
                ClassTimings = request.ClassTimings?.Trim() ?? string.Empty
            };

            var saved = request.ClassId > 0
                ? _ownerRepository.UpdateClass(ownerId, classItem)
                : _ownerRepository.CreateClass(ownerId, classItem);

            return saved == null ? NotFound(new { message = "Class not found or does not belong to this owner." }) : Ok(saved);
        }

        [HttpDelete("classes/{classId:int}")]
        public IActionResult DeleteClass(int classId)
        {
            var ownerId = GetOwnerId();
            if (ownerId <= 0) return Unauthorized(new { message = "Please log in as a class owner." });
            return _ownerRepository.DeleteClass(ownerId, classId)
                ? Ok(new { message = "Class deleted." })
                : NotFound(new { message = "Class not found or does not belong to this owner." });
        }

        [HttpPost("images")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (GetOwnerId() <= 0) return Unauthorized(new { message = "Please log in as a class owner." });
            if (image == null || image.Length == 0) return BadRequest(new { message = "Please choose an image." });
            if (image.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Image must be 5 MB or smaller." });

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (extension is not ".jpg" and not ".jpeg" and not ".png")
                return BadRequest(new { message = "Only JPG and PNG images are supported." });

            var imageDirectory = Path.Combine(_environment.WebRootPath, "images");
            Directory.CreateDirectory(imageDirectory);
            var fileName = $"class-{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(imageDirectory, fileName);
            await using var stream = System.IO.File.Create(filePath);
            await image.CopyToAsync(stream);
            return Ok(new { imageUrl = $"/images/{fileName}" });
        }

        [HttpGet("enquiries")]
        public IActionResult GetEnquiries()
        {
            var ownerId = GetOwnerId();
            return ownerId <= 0 ? Unauthorized(new { message = "Please log in as a class owner." }) : Ok(_ownerRepository.GetEnquiries(ownerId));
        }

        [HttpGet("reviews")]
        public IActionResult GetReviews()
        {
            var ownerId = GetOwnerId();
            return ownerId <= 0 ? Unauthorized(new { message = "Please log in as a class owner." }) : Ok(_ownerRepository.GetReviews(ownerId));
        }

        private int GetOwnerId()
        {
            if (!int.TryParse(Request.Cookies["eduConnectUserId"], out var userId)) return 0;
            var user = _userRepository.GetUserById(userId);
            return user != null && user.Role.Equals("ClassOwner", StringComparison.OrdinalIgnoreCase) ? userId : 0;
        }
    }
}