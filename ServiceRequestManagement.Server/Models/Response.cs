namespace ServiceRequestManagement.Server.Models
{
    public class Response
    {
        public int StatusCode { get; set; }
        public string? StatusMessage { get; set; }
        public List<Registration>? listRegistration { get; set; }
        public Registration? Registration { get; set; }
        public List<ServiceRequest>? listServiceRequests { get; set; }
        public ServiceRequest? serviceRequest { get; set; }
        public List<Message>? listMessages { get; set; }

        public List<ServiceRequestOffer>? listServiceRequestOffer { get; set; }

        public ServiceRequestOffer? serviceRequestOffer { get; set; }

        public List<ServiceRequestOfferSelection>? listServiceRequestOrders { get; set; }

        public ServiceRequestOfferSelection? serviceRequestOrder { get; set; }
        public List<Message2>? listMessages2 { get; set; }

        public List<Evaluation>? listEvaluations { get; set; }
    }
}
