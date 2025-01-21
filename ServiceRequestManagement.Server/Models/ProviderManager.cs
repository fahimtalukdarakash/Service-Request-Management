namespace ServiceRequestManagement.Server.Models
{
    public class ProviderManager
    {
        public int ID { get; set; } // Foreign Key to Registration table
        public string? Name { get; set; }
        public string? Department { get; set; } // Department of the provider manager
        public string? Email { get; set; } // Email of the provider manager
        public string? Password { get; set; } // Password of the provider manager
        public DateTime CreatedAt { get; set; } // Timestamp of creation
    }
}
