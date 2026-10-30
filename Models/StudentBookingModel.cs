namespace EduConnect.Models
{
    public class StudentBookingModel
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = "Class";
        public int TargetId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string ContactInfo { get; set; } = string.Empty;
    }
}
