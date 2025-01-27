using ServiceRequestManagement.Server.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestManagement.Server.Models
{
    public class ServiceRequestOffer
    {
        public int ServiceRequestOfferId { get; set; }
        public Guid RequestID { get; set; }
        public Guid UserID { get; set; }
        public int MasterAgreementID { get; set; }
        public string? MasterAgreementName { get; set; }
        public string? TaskDescription { get; set; }
        public string? RequestType { get; set; }
        public string? Project { get; set; }
        public int DomainID { get; set; }
        public string? DomainName { get; set; }
        public string? CycleStatus { get; set; }
        public int NumberOfSpecialists { get; set; }
        public int NumberOfOffers { get; set; }
        public List<ServiceOffer>? ServiceOffers { get; set; }
    }

    public class ServiceOffer
    {
        public int OfferID { get; set; }
        public int ServiceRequestOfferId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderID { get; set; }
        public string? EmployeeID { get; set; }
        public string? Role { get; set; }
        public string? Level { get; set; }
        public string? TechnologyLevel { get; set; }
        public decimal Price { get; set; }
    }

}
