namespace ServiceRequestManagement.Server.Models
{
    public class Evaluation
    {
        public int EvaluationID { get; set; }
        public Guid ServiceRequestId { get; set; }
        public int AgreementID { get; set; }
        public string? AgreementName { get; set; }
        public string? TaskDescription { get; set; }
        public string? Type { get; set; }
        public string? Project { get; set; }
        public string? ProviderID { get; set; }
        public string? ProviderName { get; set; }
        public int TimelinessScore { get; set; }
        public int QualityScore { get; set; }
        public decimal OverallScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
