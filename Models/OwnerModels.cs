namespace EduConnect.Models
{
    public class OwnerClassRequest
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
        public string ClassTimings { get; set; } = string.Empty;
    }

    public class OwnerEnquiryModel
    {
        public int BookingId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;
        public string StudentStandard { get; set; } = string.Empty;
        public string StudentBoard { get; set; } = string.Empty;
        public string YearlyBudget { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime EnquiryDate { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class OwnerReviewModel
    {
        public int ReviewId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
