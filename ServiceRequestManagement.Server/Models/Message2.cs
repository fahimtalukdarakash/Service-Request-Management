using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Message2
{
    [Key]
    public Guid MessageID { get; set; } // Unique message ID

    [Required]
    public int ServiceRequestOfferId { get; set; } // Links to a specific service request

    [Required]
    public Guid SenderID { get; set; } // ID of the sender (User or ProviderManager)

    [Required]
    [MaxLength(50)]
    public string? SenderRole { get; set; } // Role of the sender ('User' or 'ProviderManager')

    [Required]
    [MaxLength] // For NVARCHAR(MAX)
    public string? MessageContent { get; set; } // Message text

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.Now; // Time the message was sent
}