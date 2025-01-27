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
        public IActionResult GetServiceRequestDetails(Guid requestID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetServiceRequestDetails(requestID, connection);
            if (response.StatusCode == 200)
            {
                return Ok(response.serviceRequest);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
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

        [HttpPost]
        [Route("AddServiceRequestOffer")]
        public Response AddServiceRequestOffer(ServiceRequestOffer serviceRequestOffer)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.AddServiceRequestOffer(serviceRequestOffer, connection);
            return response;
        }
        [HttpPost]
        [Route("UpdateServiceRequestOffer")]
        public IActionResult UpdateServiceRequestOffer([FromBody] ServiceRequestOffer serviceRequestOffer)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.UpdateServiceRequestOffer(serviceRequestOffer, connection);

            if (response.StatusCode == 200)
            {
                return Ok(new { message = response.StatusMessage });
            }
            else
            {
                return StatusCode(500, new { message = response.StatusMessage });
            }
        }

        [HttpGet]
        [Route("GetAllServiceRequestOffers")]
        public IActionResult GetAllServiceRequestOffers()
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetAllServiceRequestOffers(connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOffer);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }
        [HttpGet]
        [Route("GetUserServiceRequestOffers")]
        public IActionResult GetUserServiceRequestOffers(Guid userID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetUserServiceRequestOffers(userID, connection);
            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOffer);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }

        [HttpGet]
        [Route("GetServiceRequestOffersForProviderManager")]
        public IActionResult GetServiceRequestOffersForProviderManager(string email)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetServiceRequestOffersForProviderManager(email, connection);
            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOffer);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }
        [HttpGet]
        [Route("GetServiceRequestOfferDetails")]
        public IActionResult GetServiceRequestOfferDetails(int serviceRequestOfferId)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            Response response = dal.GetServiceRequestOfferDetails(serviceRequestOfferId, connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.serviceRequestOffer);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }
        [HttpPost]
        [Route("AddServiceRequestOfferSelection")]
        public IActionResult AddServiceRequestOfferSelection([FromBody] ServiceRequestOfferSelection offerSelection)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            Response response = dal.AddServiceRequestOfferSelection(offerSelection, connection);

            if (response.StatusCode == 200)
            {
                return Ok(new { message = response.StatusMessage });
            }
            else
            {
                return BadRequest(new { message = response.StatusMessage });
            }
        }
        [HttpGet]
        [Route("GetServiceRequestOfferSelection")]
        public IActionResult GetServiceRequestOfferSelection(int serviceRequestOfferId)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.GetServiceRequestOfferSelection(serviceRequestOfferId, connection);

            if (response != null)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(new { message = "No selected offers found for the given ServiceRequestOfferId." });
            }
        }

        [HttpGet]
        [Route("GetAllServiceRequestOfferSelections")]
        public IActionResult GetAllServiceRequestOfferSelections()
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.GetAllServiceRequestOfferSelections(connection);

            if (response != null && response.Count > 0)
            {
                return Ok(response);
            }
            else
            {
                return NotFound(new { message = "No service request offer selections found." });
            }
        }
        [HttpPost]
        [Route("CreateOrder")]
        public IActionResult CreateOrder(ServiceRequestOfferSelection serviceRequestOffer)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.CreateOrder(serviceRequestOffer, connection);

            if (response.StatusCode == 200)
            {
                return Ok(new { message = response.StatusMessage });
            }
            else
            {
                return StatusCode(500, new { message = response.StatusMessage });
            }
        }
        [HttpGet]
        [Route("GetAllOrders")]
        public IActionResult GetAllOrders()
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.GetAllOrders(connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOrders);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }

        [HttpGet]
        [Route("GetSingleOrder")]
        public IActionResult GetSingleOrder(int serviceRequestOfferId)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetSingleOrder(serviceRequestOfferId, connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.serviceRequestOrder);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }

        [HttpGet]
        [Route("GetUserOrders")]
        public IActionResult GetUserOrders(Guid userID)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.GetUserOrders(userID, connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOrders);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }

        [HttpGet]
        [Route("GetProviderManagerOrders")]
        public IActionResult GetProviderManagerOrders(string email)
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            var response = dal.GetProviderManagerOrders(email, connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.listServiceRequestOrders);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }


        [HttpGet]
        [Route("GetMessages2")]
        public Response GetMessages2(int serviceRequestOfferId)
        {
            Console.WriteLine($"Received ServiceRequestID: {serviceRequestOfferId}");
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetMessages2(serviceRequestOfferId, connection);
            return response;
        }


        [HttpPost]
        [Route("SendMessage2")]
        public Response SendMessage2(Message2 message)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.SendMessage2(message, connection);
            return response;
        }

        [HttpPost]
        [Route("AddEvaluation")]
        public IActionResult AddEvaluation([FromBody] Evaluation evaluation)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.AddEvaluation(evaluation, connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.StatusMessage);
            }
            else
            {
                return BadRequest(response.StatusMessage);
            }
        }

        [HttpGet]
        [Route("GetAllEvaluations")]
        public IActionResult GetAllEvaluations()
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            Response response = dal.GetAllEvaluations(connection);

            if (response.StatusCode == 200)
            {
                return Ok(response.listEvaluations);
            }
            else
            {
                return NotFound(new { message = response.StatusMessage });
            }
        }
    }
}
