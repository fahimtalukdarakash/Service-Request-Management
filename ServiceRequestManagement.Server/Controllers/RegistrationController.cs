using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestManagement.Server.Models;
using Microsoft.Data.SqlClient;

namespace ServiceRequestManagement.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost]
        [Route("Registration")]

        public async Task<Response> Registration(Registration registration)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();

            // Use 'await' to wait for the asynchronous operation
            response = await dal.Registration(registration, connection);

            return response;
        }
        [HttpPost]
        [Route("Login")]
        public Response Login(Registration registration)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.Login(registration, connection);
            return response;
        }
        [HttpPost]
        [Route("UserApproval")]
        public Response UserApproval(Registration registration)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response  = dal.UserApproval(registration, connection);
            return response;
        }

        [HttpPost]
        [Route("ProviderManagerRegistration")]

        public Response ProviderManagerRegistration(ProviderManager providerManager)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ProviderManagerRegistration(providerManager, connection);
            return response;
        }

    }
}
