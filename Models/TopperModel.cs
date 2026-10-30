namespace EduConnect.Models
{
    public class TopperModel
    {
        public int TopperId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public decimal? Marks10 { get; set; }
        public decimal? Marks12 { get; set; }
        public string Marksheet10Url { get; set; } = string.Empty;
        public string Marksheet12Url { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? PricePerDay { get; set; }
        public decimal? PricePerMonth { get; set; }
        public decimal? PricePerYear { get; set; }
    }
}
