namespace EduConnect.Models
{
    public class StudentProfileModel
    {
        public int ProfileId { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public string Board { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Subjects { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public string PreferredPriceRange { get; set; } = string.Empty;
        public decimal? CustomPrice { get; set; }
        public string LearningMode { get; set; } = "Both";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
