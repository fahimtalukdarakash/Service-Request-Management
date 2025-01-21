using ServiceRequestManagement.Server.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class ServiceRequest
{
    public Guid RequestID { get; set; }
    public Guid UserID { get; set; }
    public int MasterAgreementID { get; set; }
    public string? MasterAgreementName { get; set; }
    public string? TaskDescription { get; set; }
    public string? RequestType { get; set; }
    public string? Project { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? TotalManDays { get; set; }
    public string? Location { get; set; }
    public string? ProviderManagerInfo { get; set; }
    public string? Consumer { get; set; }
    public string? Representatives { get; set; }
    public string? cycleStatus { get; set; } // Added Cycle field
    public string? SelectedDomainName { get; set; }
    public int? numberOfSpecialists { get; set; }
    public int? numberOfOffers { get; set; }
    public int IsApproved { get; set; }
    public List<RoleSpecific>? RoleSpecific { get; set; } // Updated to reflect RoleSpecific entries
}

public class RoleSpecific
{
    public Guid RoleID { get; set; } = Guid.NewGuid(); // Auto-generated unique ID
    public Guid RequestID { get; set; }               // Foreign key for ServiceRequestList
    public Guid UserID { get; set; }                  // User who created the role-specific data
    public string Role { get; set; }                 // Role name
    public string Level { get; set; }                // Role level (e.g., Junior, Intermediate, etc.)
    public string TechnologyLevel { get; set; }      // Technology level (e.g., Common, Advanced)
    public string LocationType { get; set; }         // Location type (e.g., Onsite, Hybrid, Remote)
    public int NumberOfEmployee { get; set; }        // Number of employees
}
