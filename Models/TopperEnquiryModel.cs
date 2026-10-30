namespace EduConnect.Models
{
    public class TopperEnquiryModel
    {
        public int EnquiryId { get; set; }
        public int TopperId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentStandard { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}