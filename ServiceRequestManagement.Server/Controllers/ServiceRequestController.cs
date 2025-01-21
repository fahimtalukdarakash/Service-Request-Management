using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestManagement.Server.Models;
using Microsoft.Data.SqlClient;
namespace ServiceRequestManagement.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ServiceRequestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //[HttpPost]
        //[Route("AddServiceRequest")]
        ////[Consumes("multipart/form-data")]
        //public Response AddServiceRequest([FromForm] ServiceRequest serviceRequest)
        //{
        //    Response response = new Response();
        //    SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        //    Dal dal = new Dal();
        //    response = dal.AddServiceRequest(serviceRequest, connection);
        //    return response;
        //}
        [HttpPost]
        [Route("AddServiceRequest")]
        public Response AddServiceRequest(ServiceRequest serviceRequest)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.AddServiceRequest(serviceRequest, connection);
            return response;
        }

        [HttpGet]
        [Route("ServiceRequestList")]
        public IActionResult ServiceRequestList()
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ServiceRequestList(connection);
            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequests);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }

        [HttpPost]
        [Route("ServiceRequestApproval")]
        public Response ServiceRequestApproval([FromBody] Guid requestID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ServiceRequestApproval(requestID, connection);
            return response;
        }


        [HttpGet]
        [Route("ViewServiceRequests")]
        public Response ViewServiceRequests(string userRole, Guid userID, string department = null)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ViewServiceRequests(userRole, userID, department, connection);
            return response;
        }

        [HttpGet]
        [Route("GetUserServiceRequests")]
        public Response GetUserServiceRequests(Guid userID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetUserServiceRequests(userID, connection);
            return response;
        }

        [HttpGet]
        [Route("GetServiceRequestsForProviderManager")]
        public Response GetServiceRequestsForProviderManager(string email)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetServiceRequestsForProviderManager(email, connection);
            return response;
        }
        [HttpGet]
        [Route("GetServiceRequestDetails")]
        public Response GetServiceRequestDetails(Guid requestID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetServiceRequestDetails(requestID, connection);
            return response;
        }

        [HttpGet]
        [Route("GetMessages")]
        public Response GetMessages(Guid requestID)
        {
            Console.WriteLine($"Received ServiceRequestID: {requestID}");
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetMessages(requestID, connection);
            return response;
        }


        [HttpPost]
        [Route("SendMessage")]
        public Response SendMessage(Message message)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.SendMessage(message, connection);
            return response;
        }

    }
}
