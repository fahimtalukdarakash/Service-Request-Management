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
    }
}
