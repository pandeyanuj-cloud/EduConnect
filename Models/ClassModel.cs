namespace EduConnect.Models
{
    public class ClassModel
    {
        public int ClassId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Mode { get; set; } = "Both";
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MaterialProvided { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ClassTimings { get; set; } = string.Empty;
        public string TeachingExperience { get; set; } = string.Empty;
        public string StudyMaterial { get; set; } = string.Empty;
        public string TestsProvided { get; set; } = string.Empty;
        public string Facilities { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string BatchSize { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
    }
}