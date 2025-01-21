namespace ServiceRequestManagement.Server.Models
{
    public class Registration
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public int IsActive { get; set; }
        public int IsApproved { get; set; }
    }
}
