namespace EduConnect.Models
{
    public class SavedClassModel
    {
        public int SavedId { get; set; }
        public int UserId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Mode { get; set; } = "Both";
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MaterialProvided { get; set; } = string.Empty;
    }
}
