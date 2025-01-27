using Microsoft.Data;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
namespace ServiceRequestManagement.Server.Models
{
    public class Dal
    {
        public async  Task<Response> Registration(Registration registration, SqlConnection connection)
        {
            Response response = new Response();

            // If the role is ProviderManager, validate with the external API
            if (registration.Role == "ProviderManager")
            {
                // Step 1: Fetch data from the ProviderManager API
                string apiUrl = "https://677e874894bde1c1252c5143.mockapi.io/api/v1/ProviderManagerList";
                HttpClient client = new HttpClient();
                HttpResponseMessage apiResponse = await client.GetAsync(apiUrl);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    response.StatusCode = 500;
                    response.StatusMessage = "Error fetching Provider Manager data from API.";
                    return response;
                }

                // Step 2: Parse the API response
                string apiResponseContent = await apiResponse.Content.ReadAsStringAsync();
                var providerManagers = JsonConvert.DeserializeObject<List<ProviderManager>>(apiResponseContent);

                // Step 3: Check if the name and email exist in the API data
                var matchingProvider = providerManagers.FirstOrDefault(pm =>
                    pm.Email == registration.Email && pm.Name == registration.Name);

                if (matchingProvider == null)
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Provider Manager data not found in the API.";
                    return response;
                }

                // Step 4: Save data to the ProviderManager table
                SqlCommand providerManagerCmd = new SqlCommand(
                    "INSERT INTO ProviderManager(Name, Email, Password, Department, CreatedAt) " +
                    "VALUES(@Name, @Email, @Password, @Department, @CreatedAt)",
                    connection);
                providerManagerCmd.Parameters.AddWithValue("@Name", matchingProvider.Name);
                providerManagerCmd.Parameters.AddWithValue("@Email", matchingProvider.Email);
                providerManagerCmd.Parameters.AddWithValue("@Password", registration.Password);
                providerManagerCmd.Parameters.AddWithValue("@Department", matchingProvider.Department);
                providerManagerCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                connection.Open();
                int providerInsertResult = providerManagerCmd.ExecuteNonQuery();
                connection.Close();

                if (providerInsertResult <= 0)
                {
                    response.StatusCode = 500;
                    response.StatusMessage = "Error saving data to the Provider Manager table.";
                    return response;
                }
            }

            // Step 5: Save data to the Registration table
            SqlCommand cmd = new SqlCommand(
                "INSERT INTO Registration(Name, Email, Password, Role, IsActive, IsApproved) " +
                "VALUES(@Name, @Email, @Password, @Role, @IsActive, @IsApproved)",
                connection);

            cmd.Parameters.AddWithValue("@Name", registration.Name);
            cmd.Parameters.AddWithValue("@Email", registration.Email);
            cmd.Parameters.AddWithValue("@Password", registration.Password);
            cmd.Parameters.AddWithValue("@Role", registration.Role);
            cmd.Parameters.AddWithValue("@IsActive", true);
            cmd.Parameters.AddWithValue("@IsApproved", false);

            connection.Open();
            int registrationInsertResult = cmd.ExecuteNonQuery();
            connection.Close();

            if (registrationInsertResult > 0)
            {
                response.StatusCode = 200;
                response.StatusMessage = "Registration Successful";
            }
            else
            {
                response.StatusCode = 404;
                response.StatusMessage = "Registration Failed";
            }

            return response;
        }

        public Response Login(Registration registration, SqlConnection connection)
        {
            Response response = new Response();

            // Step 1: Check the Registration Table
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT * FROM Registration WHERE Email = '" + registration.Email + "' AND Password = '" + registration.Password + "'",
                connection
            );
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            if (dataTable.Rows.Count > 0)
            {
                // User found in Registration table
                response.StatusCode = 200;
                response.StatusMessage = "Login Successful";

                Registration reg = new Registration
                {
                    Id = Guid.Parse(dataTable.Rows[0]["Id"].ToString()),
                    Name = dataTable.Rows[0]["Name"].ToString(),
                    Email = dataTable.Rows[0]["Email"].ToString(),
                    Role = dataTable.Rows[0]["Role"].ToString()
                };

                response.Registration = reg;
            }
            else
            {
                // Step 2: Check the ProviderManager Table
                SqlDataAdapter providerAdapter = new SqlDataAdapter(
                    "SELECT * FROM ProviderManager WHERE Email = '" + registration.Email + "' AND Password = '" + registration.Password + "'",
                    connection
                );
                DataTable providerTable = new DataTable();
                providerAdapter.Fill(providerTable);

                if (providerTable.Rows.Count > 0)
                {
                    // User found in ProviderManager table
                    response.StatusCode = 200;
                    response.StatusMessage = "Login Successful";

                    Registration reg = new Registration
                    {
                        Id = Guid.Empty, // Assuming the ID in ProviderManager is also a GUID
                        Name = providerTable.Rows[0]["Name"].ToString(),
                        Email = providerTable.Rows[0]["Email"].ToString(),
                        Role = "ProviderManager" // Explicitly set the role as "ProviderManager"
                    };

                    response.Registration = reg;
                }
                else
                {
                    // Credentials not found in either table
                    response.StatusCode = 404;
                    response.StatusMessage = "Login Failed";
                    response.Registration = null;
                }
            }

            return response;
        }

        public Response UserApproval(Registration registration, SqlConnection connection)
        {
            Response response = new Response();
            SqlCommand cmd = new SqlCommand("UPDATE Registration SET IsApproved = 1 WHERE Id = '" + registration.Id + "' AND IsActive = 1", connection);
            connection.Open();
            int i = cmd.ExecuteNonQuery();
            connection.Close();
            if (i > 0)
            {
                response.StatusCode = 200;
                response.StatusMessage = "User Approved";
            }
            else
            {
                response.StatusCode = 404;
                response.StatusMessage = "User Aprroval Failed";
            }
            return response;
        }

        public Response AddServiceRequest(ServiceRequest serviceRequest, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                // Use a transaction to ensure atomicity
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Insert into ServiceRequests and get the newly generated RequestID
                    string serviceRequestQuery = @"
                INSERT INTO ServiceRequests (
                    UserID, TaskDescription, RequestType, Project, StartDate, EndDate, 
                    TotalManDays, Location, ProviderManagerInfo, Consumer, Representatives, 
                    Cycle, MasterAgreementID, MasterAgreementName, DomainName, numberOfSpecialists, numberOfOffers
                )
                OUTPUT INSERTED.RequestID
                VALUES (
                    @UserID, @TaskDescription, @RequestType, @Project, @StartDate, @EndDate, 
                    @TotalManDays, @Location, @ProviderManagerInfo, @Consumer, @Representatives, 
                    @cycleStatus, @MasterAgreementID, @MasterAgreementName, @SelectedDomainName, @numberOfSpecialists, @numberOfOffers
                )";

                    SqlCommand serviceRequestCmd = new SqlCommand(serviceRequestQuery, connection, transaction);
                    serviceRequestCmd.Parameters.AddWithValue("@UserID", serviceRequest.UserID);
                    serviceRequestCmd.Parameters.AddWithValue("@TaskDescription", serviceRequest.TaskDescription ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@RequestType", serviceRequest.RequestType ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@Project", serviceRequest.Project ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@StartDate", serviceRequest.StartDate ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@EndDate", serviceRequest.EndDate ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@TotalManDays", serviceRequest.TotalManDays ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@Location", serviceRequest.Location ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@ProviderManagerInfo", serviceRequest.ProviderManagerInfo ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@Consumer", serviceRequest.Consumer ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@Representatives", serviceRequest.Representatives ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@cycleStatus", serviceRequest.cycleStatus ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@MasterAgreementID", serviceRequest.MasterAgreementID);
                    serviceRequestCmd.Parameters.AddWithValue("@MasterAgreementName", serviceRequest.MasterAgreementName ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@SelectedDomainName", serviceRequest.SelectedDomainName ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@numberOfSpecialists", serviceRequest.numberOfSpecialists ?? (object)DBNull.Value);
                    serviceRequestCmd.Parameters.AddWithValue("@numberOfOffers", serviceRequest.numberOfOffers ?? (object)DBNull.Value);

                    Guid newRequestID = (Guid)serviceRequestCmd.ExecuteScalar();

                    // Insert role-specific details
                    if (serviceRequest.RoleSpecific != null && serviceRequest.RoleSpecific.Count > 0)
                    {
                        foreach (var role in serviceRequest.RoleSpecific)
                        {
                            string roleSpecificQuery = @"
                        INSERT INTO RoleSpecific (
                            RequestID, UserID, Role, Level, TechnologyLevel, LocationType, NumberOfEmployee
                        )
                        VALUES (
                            @RequestID, @UserID, @Role, @Level, @TechnologyLevel, @LocationType, @NumberOfEmployee
                        )";

                            SqlCommand roleSpecificCmd = new SqlCommand(roleSpecificQuery, connection, transaction);
                            roleSpecificCmd.Parameters.AddWithValue("@RequestID", newRequestID);
                            roleSpecificCmd.Parameters.AddWithValue("@UserID", serviceRequest.UserID);
                            roleSpecificCmd.Parameters.AddWithValue("@Role", role.Role ?? (object)DBNull.Value);
                            roleSpecificCmd.Parameters.AddWithValue("@Level", role.Level ?? (object)DBNull.Value);
                            roleSpecificCmd.Parameters.AddWithValue("@TechnologyLevel", role.TechnologyLevel ?? (object)DBNull.Value);
                            roleSpecificCmd.Parameters.AddWithValue("@LocationType", role.LocationType ?? (object)DBNull.Value);
                            roleSpecificCmd.Parameters.AddWithValue("@NumberOfEmployee", role.NumberOfEmployee);

                            roleSpecificCmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    response.StatusCode = 200;
                    response.StatusMessage = "Service Request and related RoleSpecific entries added successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.StatusCode = 500;
                    response.StatusMessage = $"An error occurred: {ex.Message}";
                }
                finally
                {
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        public Response ServiceRequestList(SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                // Fetch all service requests
                string query = "SELECT * FROM ServiceRequests";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string cycle;
                        ServiceRequest serviceRequest = new ServiceRequest();
                        cycle = row["Cycle"]?.ToString();
                        if (cycle == "1")
                        {
                            cycle = "Cycle1";
                        }
                        else
                        {
                            cycle = "Cycle2";
                        }
                        ServiceRequest request = new ServiceRequest
                        {
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            StartDate = row["StartDate"] != DBNull.Value ? DateTime.Parse(row["StartDate"].ToString()) : (DateTime?)null,
                            EndDate = row["EndDate"] != DBNull.Value ? DateTime.Parse(row["EndDate"].ToString()) : (DateTime?)null,
                            TotalManDays = row["TotalManDays"] != DBNull.Value ? Convert.ToInt32(row["TotalManDays"]) : (int?)null,
                            Location = row["Location"]?.ToString(),
                            ProviderManagerInfo = row["ProviderManagerInfo"]?.ToString(),
                            Consumer = row["Consumer"]?.ToString(),
                            Representatives = row["Representatives"]?.ToString(),
                            IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            SelectedDomainName = row["DomainName"]?.ToString(),
                            DomainName = null,
                            DomainID = null,
                            numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                            numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                            cycleStatus = cycle,
                            RoleSpecific = new List<RoleSpecific>() // Initialize role-specific list
                        };
                        // Call external API to get domain ID based on Master Agreement ID
                        string masterAgreementAPIUrl = $"https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/{request.MasterAgreementID}";
                        using (HttpClient httpClient = new HttpClient())
                        {
                            var responseTask = httpClient.GetAsync(masterAgreementAPIUrl);
                            responseTask.Wait();
                            var apiResponse = responseTask.Result;

                            if (apiResponse.IsSuccessStatusCode)
                            {
                                var result = apiResponse.Content.ReadAsStringAsync().Result;
                                var masterAgreementData = JsonConvert.DeserializeObject<List<MasterAgreementDomain>>(result);

                                // Find matching domain name and get domain ID
                                var matchingDomain = masterAgreementData.FirstOrDefault(d => d.DomainName == request.SelectedDomainName);
                                if (matchingDomain != null)
                                {
                                    request.DomainID = matchingDomain.DomainID;
                                    request.DomainName = matchingDomain.DomainName;
                                }
                            }
                        }

                        // Fetch RoleSpecific details for this request
                        string roleQuery = "SELECT * FROM RoleSpecific WHERE RequestID = @RequestID";
                        using (SqlCommand roleCmd = new SqlCommand(roleQuery, connection))
                        {
                            roleCmd.Parameters.AddWithValue("@RequestID", request.RequestID);
                            using (SqlDataAdapter roleAdapter = new SqlDataAdapter(roleCmd))
                            {
                                DataTable roleTable = new DataTable();
                                roleAdapter.Fill(roleTable);

                                foreach (DataRow roleRow in roleTable.Rows)
                                {
                                    RoleSpecific roleDetail = new RoleSpecific
                                    {
                                        RoleID = roleRow["RoleID"] != DBNull.Value ? Guid.Parse(roleRow["RoleID"].ToString()) : Guid.Empty,
                                        RequestID = roleRow["RequestID"] != DBNull.Value ? Guid.Parse(roleRow["RequestID"].ToString()) : Guid.Empty,
                                        UserID = roleRow["UserID"] != DBNull.Value ? Guid.Parse(roleRow["UserID"].ToString()) : Guid.Empty,
                                        DomainId = request.DomainID,
                                        DomainName = request.DomainName,
                                        Role = roleRow["Role"]?.ToString(),
                                        Level = roleRow["Level"]?.ToString(),
                                        TechnologyLevel = roleRow["TechnologyLevel"]?.ToString(),
                                        LocationType = roleRow["LocationType"]?.ToString(),
                                        NumberOfEmployee = roleRow["NumberOfEmployee"] != DBNull.Value ? Convert.ToInt32(roleRow["NumberOfEmployee"]) : 0
                                    };

                                    request.RoleSpecific.Add(roleDetail);
                                }
                            }
                        }

                        serviceRequests.Add(request);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service requests retrieved successfully.";
                    response.listServiceRequests = serviceRequests;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service requests found.";
                    response.listServiceRequests = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response ServiceRequestApproval(Guid requestID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE ServiceRequests SET IsApproved = 1 WHERE RequestID = @RequestID", connection))
                {
                    cmd.Parameters.AddWithValue("@RequestID", requestID);

                    connection.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    connection.Close();

                    if (rowsAffected > 0)
                    {
                        response.StatusCode = 200;
                        response.StatusMessage = "Service Request Approved";
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.StatusMessage = "Service Request Approval Failed";
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }

            return response;
        }


        public Response ProviderManagerRegistration(ProviderManager providerManager, SqlConnection connection)
        {
            Response response = new Response();

            try
            {
                // Open the connection
                connection.Open();

                // Check if the user exists and is approved as a Provider Manager
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Registration WHERE ID = @ID AND IsApproved = 1 AND Role = 'ProviderManager'",
                    connection
                );

                checkCmd.Parameters.AddWithValue("@ID", providerManager.ID);

                int exists = (int)checkCmd.ExecuteScalar();

                if (exists > 0)
                {
                    // Insert into ProviderManager table
                    SqlCommand providerManagerCmd = new SqlCommand(
                        "INSERT INTO ProviderManager(ID, Department, Email, Password, CreatedAt) " +
                        "VALUES(@ID, @Department, @Email, @Password, @CreatedAt)",
                        connection
                    );

                    providerManagerCmd.Parameters.AddWithValue("@ID", providerManager.ID);
                    providerManagerCmd.Parameters.AddWithValue("@Department", providerManager.Department);
                    providerManagerCmd.Parameters.AddWithValue("@Email", providerManager.Email);
                    providerManagerCmd.Parameters.AddWithValue("@Password", providerManager.Password);
                    providerManagerCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    int pmResult = providerManagerCmd.ExecuteNonQuery();

                    if (pmResult > 0)
                    {
                        response.StatusCode = 200;
                        response.StatusMessage = "Provider Manager Registration Successful";
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.StatusMessage = "Failed to Register Provider Manager";
                    }
                }
                else
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "User not approved or does not exist as a Provider Manager in the Registration table";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response ViewServiceRequests(string userRole, Guid userID, string department, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query;
                if (userRole == "User")
                {
                    // Fetch only the service requests created by the user
                    query = "SELECT * FROM ServiceRequests WHERE UserID = @UserID";
                }
                else if (userRole == "ProviderManager" && department != null)
                {
                    // Fetch only the service requests where SelectedDomainName matches the department
                    query = "SELECT * FROM ServiceRequests WHERE SelectedDomainName = @Department";
                }
                else
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "Invalid role or missing department for provider manager.";
                    return response;
                }

                SqlCommand cmd = new SqlCommand(query, connection);
                if (userRole == "User")
                {
                    cmd.Parameters.AddWithValue("@UserID", userID);
                }
                else if (userRole == "ProviderManager")
                {
                    cmd.Parameters.AddWithValue("@Department", department);
                }

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string cycle;
                        ServiceRequest serviceRequest = new ServiceRequest();
                        cycle = row["Cycle"]?.ToString();
                        if (cycle == "1")
                        {
                            cycle = "cycle_one";
                        }
                        else
                        {
                            cycle = "cycle_two";
                        }
                        ServiceRequest request = new ServiceRequest
                        {
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            StartDate = row["StartDate"] != DBNull.Value ? DateTime.Parse(row["StartDate"].ToString()) : (DateTime?)null,
                            EndDate = row["EndDate"] != DBNull.Value ? DateTime.Parse(row["EndDate"].ToString()) : (DateTime?)null,
                            TotalManDays = row["TotalManDays"] != DBNull.Value ? Convert.ToInt32(row["TotalManDays"]) : (int?)null,
                            Location = row["Location"]?.ToString(),
                            ProviderManagerInfo = row["ProviderManagerInfo"]?.ToString(),
                            Consumer = row["Consumer"]?.ToString(),
                            Representatives = row["Representatives"]?.ToString(),
                            cycleStatus = cycle,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            SelectedDomainName = row["DomainName"]?.ToString(),
                            numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                            numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                            IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                            RoleSpecific = new List<RoleSpecific>()
                        };

                        // Fetch RoleSpecific details for this request
                        string roleSpecificQuery = "SELECT * FROM RoleSpecific WHERE RequestID = @RequestID";
                        SqlCommand roleCmd = new SqlCommand(roleSpecificQuery, connection);
                        roleCmd.Parameters.AddWithValue("@RequestID", request.RequestID);

                        SqlDataAdapter roleAdapter = new SqlDataAdapter(roleCmd);
                        DataTable roleTable = new DataTable();
                        roleAdapter.Fill(roleTable);

                        foreach (DataRow roleRow in roleTable.Rows)
                        {
                            RoleSpecific roleSpecific = new RoleSpecific
                            {
                                Role = roleRow["Role"]?.ToString(),
                                Level = roleRow["Level"]?.ToString(),
                                TechnologyLevel = roleRow["TechnologyLevel"]?.ToString(),
                                LocationType = roleRow["LocationType"]?.ToString(),
                                NumberOfEmployee = roleRow["NumberOfEmployee"] != DBNull.Value ? Convert.ToInt32(roleRow["NumberOfEmployee"]) : 0
                            };
                            request.RoleSpecific.Add(roleSpecific);
                        }

                        serviceRequests.Add(request);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service requests retrieved successfully.";
                    response.listServiceRequests = serviceRequests;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service requests found.";
                    response.listServiceRequests = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetUserServiceRequests(Guid userID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query = "SELECT * FROM ServiceRequests WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userID);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string cycle;
                        ServiceRequest serviceRequest = new ServiceRequest();
                        cycle = row["Cycle"]?.ToString();
                        if (cycle == "1")
                        {
                            cycle = "cycle_one";
                        }
                        else
                        {
                            cycle = "cycle_two";
                        }
                        ServiceRequest request = new ServiceRequest
                        {
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            StartDate = row["StartDate"] != DBNull.Value ? DateTime.Parse(row["StartDate"].ToString()) : (DateTime?)null,
                            EndDate = row["EndDate"] != DBNull.Value ? DateTime.Parse(row["EndDate"].ToString()) : (DateTime?)null,
                            TotalManDays = row["TotalManDays"] != DBNull.Value ? Convert.ToInt32(row["TotalManDays"]) : (int?)null,
                            Location = row["Location"]?.ToString(),
                            ProviderManagerInfo = row["ProviderManagerInfo"]?.ToString(),
                            Consumer = row["Consumer"]?.ToString(),
                            Representatives = row["Representatives"]?.ToString(),
                            cycleStatus = cycle,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            SelectedDomainName = row["DomainName"]?.ToString(),
                            numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                            numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                            IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                            RoleSpecific = new List<RoleSpecific>()
                        };

                        // Fetch RoleSpecific details for this request
                        string roleSpecificQuery = "SELECT * FROM RoleSpecific WHERE RequestID = @RequestID";
                        SqlCommand roleCmd = new SqlCommand(roleSpecificQuery, connection);
                        roleCmd.Parameters.AddWithValue("@RequestID", request.RequestID);

                        SqlDataAdapter roleAdapter = new SqlDataAdapter(roleCmd);
                        DataTable roleTable = new DataTable();
                        roleAdapter.Fill(roleTable);

                        foreach (DataRow roleRow in roleTable.Rows)
                        {
                            RoleSpecific roleSpecific = new RoleSpecific
                            {
                                Role = roleRow["Role"]?.ToString(),
                                Level = roleRow["Level"]?.ToString(),
                                TechnologyLevel = roleRow["TechnologyLevel"]?.ToString(),
                                LocationType = roleRow["LocationType"]?.ToString(),
                                NumberOfEmployee = roleRow["NumberOfEmployee"] != DBNull.Value ? Convert.ToInt32(roleRow["NumberOfEmployee"]) : 0
                            };
                            request.RoleSpecific.Add(roleSpecific);
                        }

                        serviceRequests.Add(request);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "User service requests retrieved successfully.";
                    response.listServiceRequests = serviceRequests;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service requests found for the user.";
                    response.listServiceRequests = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetServiceRequestsForProviderManager(string email, SqlConnection connection)
        {
            Response response = new Response();

            try
            {
                connection.Open();

                // Fetch the Department for the logged-in ProviderManager using their email
                SqlCommand departmentCmd = new SqlCommand("SELECT Department FROM ProviderManager WHERE Email = @Email", connection);
                departmentCmd.Parameters.AddWithValue("@Email", email);

                string department = departmentCmd.ExecuteScalar()?.ToString();
                connection.Close();

                if (string.IsNullOrEmpty(department))
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Provider Manager's department not found.";
                    return response;
                }

                connection.Open();
                // Fetch Service Requests where SelectedDomainName matches the ProviderManager's Department
                string query = "SELECT * FROM ServiceRequests WHERE DomainName = @SelectedDomainName";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SelectedDomainName", department);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequest> serviceRequests = new List<ServiceRequest>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        string cycle;
                        ServiceRequest serviceRequest = new ServiceRequest();
                        cycle = row["Cycle"]?.ToString();
                        if (cycle == "1")
                        {
                            cycle = "cycle_one";
                        }
                        else
                        {
                            cycle = "cycle_two";
                        }
                        ServiceRequest request = new ServiceRequest
                        {
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            StartDate = row["StartDate"] != DBNull.Value ? DateTime.Parse(row["StartDate"].ToString()) : (DateTime?)null,
                            EndDate = row["EndDate"] != DBNull.Value ? DateTime.Parse(row["EndDate"].ToString()) : (DateTime?)null,
                            TotalManDays = row["TotalManDays"] != DBNull.Value ? Convert.ToInt32(row["TotalManDays"]) : (int?)null,
                            Location = row["Location"]?.ToString(),
                            ProviderManagerInfo = row["ProviderManagerInfo"]?.ToString(),
                            Consumer = row["Consumer"]?.ToString(),
                            Representatives = row["Representatives"]?.ToString(),
                            cycleStatus = cycle,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            SelectedDomainName = row["DomainName"]?.ToString(),
                            numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                            numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                            IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                            RoleSpecific = new List<RoleSpecific>()
                        };

                        // Fetch RoleSpecific details for this request
                        string roleSpecificQuery = "SELECT * FROM RoleSpecific WHERE RequestID = @RequestID";
                        SqlCommand roleCmd = new SqlCommand(roleSpecificQuery, connection);
                        roleCmd.Parameters.AddWithValue("@RequestID", request.RequestID);

                        SqlDataAdapter roleAdapter = new SqlDataAdapter(roleCmd);
                        DataTable roleTable = new DataTable();
                        roleAdapter.Fill(roleTable);

                        foreach (DataRow roleRow in roleTable.Rows)
                        {
                            RoleSpecific roleSpecific = new RoleSpecific
                            {
                                Role = roleRow["Role"]?.ToString(),
                                Level = roleRow["Level"]?.ToString(),
                                TechnologyLevel = roleRow["TechnologyLevel"]?.ToString(),
                                LocationType = roleRow["LocationType"]?.ToString(),
                                NumberOfEmployee = roleRow["NumberOfEmployee"] != DBNull.Value ? Convert.ToInt32(roleRow["NumberOfEmployee"]) : 0
                            };
                            request.RoleSpecific.Add(roleSpecific);
                        }

                        serviceRequests.Add(request);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service requests retrieved successfully.";
                    response.listServiceRequests = serviceRequests;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service requests found for the specified role domain.";
                    response.listServiceRequests = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetServiceRequestDetails(Guid requestID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query = "SELECT * FROM ServiceRequests WHERE RequestID = @RequestID";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@RequestID", requestID);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    DataRow row = dataTable.Rows[0];
                    string cycle;
                    ServiceRequest serviceRequest = new ServiceRequest();
                    cycle = row["Cycle"]?.ToString();
                    if (cycle == "1")
                    {
                        cycle = "cycle_one";
                    }
                    else
                    {
                        cycle = "cycle_two";
                    }
                    ServiceRequest request = new ServiceRequest
                    {
                        RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                        UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                        TaskDescription = row["TaskDescription"]?.ToString(),
                        RequestType = row["RequestType"]?.ToString(),
                        Project = row["Project"]?.ToString(),
                        StartDate = row["StartDate"] != DBNull.Value ? DateTime.Parse(row["StartDate"].ToString()) : (DateTime?)null,
                        EndDate = row["EndDate"] != DBNull.Value ? DateTime.Parse(row["EndDate"].ToString()) : (DateTime?)null,
                        TotalManDays = row["TotalManDays"] != DBNull.Value ? Convert.ToInt32(row["TotalManDays"]) : (int?)null,
                        Location = row["Location"]?.ToString(),
                        ProviderManagerInfo = row["ProviderManagerInfo"]?.ToString(),
                        Consumer = row["Consumer"]?.ToString(),
                        Representatives = row["Representatives"]?.ToString(),
                        cycleStatus = cycle,
                        MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                        MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                        SelectedDomainName = row["DomainName"]?.ToString(),
                        DomainName = null,
                        DomainID = null,
                        numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                        numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                        IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                        RoleSpecific = new List<RoleSpecific>()
                    };
                    // Call external API to get domain ID based on Master Agreement ID
                    string masterAgreementAPIUrl = $"https://agiledev-contractandprovidermana-production.up.railway.app/master-agreements/established-agreements/{request.MasterAgreementID}";
                    using (HttpClient httpClient = new HttpClient())
                    {
                        var responseTask = httpClient.GetAsync(masterAgreementAPIUrl);
                        responseTask.Wait();
                        var apiResponse = responseTask.Result;

                        if (apiResponse.IsSuccessStatusCode)
                        {
                            var result = apiResponse.Content.ReadAsStringAsync().Result;
                            var masterAgreementData = JsonConvert.DeserializeObject<List<MasterAgreementDomain>>(result);

                            // Find matching domain name and get domain ID
                            var matchingDomain = masterAgreementData.FirstOrDefault(d => d.DomainName == request.SelectedDomainName);
                            if (matchingDomain != null)
                            {
                                request.DomainID = matchingDomain.DomainID;
                                request.DomainName = matchingDomain.DomainName;
                            }
                        }
                    }

                    // Fetch RoleSpecific details for this request
                    string roleSpecificQuery = "SELECT * FROM RoleSpecific WHERE RequestID = @RequestID";
                    SqlCommand roleCmd = new SqlCommand(roleSpecificQuery, connection);
                    roleCmd.Parameters.AddWithValue("@RequestID", request.RequestID);

                    SqlDataAdapter roleAdapter = new SqlDataAdapter(roleCmd);
                    DataTable roleTable = new DataTable();
                    roleAdapter.Fill(roleTable);

                    foreach (DataRow roleRow in roleTable.Rows)
                    {
                        RoleSpecific roleSpecific = new RoleSpecific
                        {
                            DomainId = request.DomainID,
                            DomainName = request.DomainName,
                            Role = roleRow["Role"]?.ToString(),
                            Level = roleRow["Level"]?.ToString(),
                            TechnologyLevel = roleRow["TechnologyLevel"]?.ToString(),
                            LocationType = roleRow["LocationType"]?.ToString(),
                            NumberOfEmployee = roleRow["NumberOfEmployee"] != DBNull.Value ? Convert.ToInt32(roleRow["NumberOfEmployee"]) : 0
                        };
                        request.RoleSpecific.Add(roleSpecific);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service Request details retrieved successfully.";
                    response.serviceRequest = request;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Service Request not found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetMessages(Guid serviceRequestID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM Messages WHERE ServiceRequestID = @ServiceRequestID ORDER BY Timestamp ASC", connection);
                adapter.SelectCommand.Parameters.AddWithValue("@ServiceRequestID", serviceRequestID);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<Message> messages = new List<Message>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        Message message = new Message
                        {
                            MessageID = Guid.Parse(row["MessageID"].ToString()),
                            ServiceRequestID = Guid.Parse(row["ServiceRequestID"].ToString()),
                            SenderID = Guid.Parse(row["SenderID"].ToString()),
                            SenderRole = row["SenderRole"].ToString(),
                            MessageContent = row["MessageContent"].ToString(),
                            Timestamp = DateTime.Parse(row["Timestamp"].ToString())
                        };
                        messages.Add(message);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Messages retrieved successfully.";
                    response.listMessages = messages;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No messages found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }

            return response;
        }

        public Response SendMessage(Message message, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Messages (ServiceRequestID, SenderID, SenderRole, MessageContent) " +
                    "VALUES (@ServiceRequestID, @SenderID, @SenderRole, @MessageContent)", connection);

                cmd.Parameters.AddWithValue("@ServiceRequestID", message.ServiceRequestID);
                cmd.Parameters.AddWithValue("@SenderID", message.SenderID);
                cmd.Parameters.AddWithValue("@SenderRole", message.SenderRole);
                cmd.Parameters.AddWithValue("@MessageContent", message.MessageContent);

                connection.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();

                if (rowsAffected > 0)
                {
                    response.StatusCode = 200;
                    response.StatusMessage = "Message sent successfully.";
                }
                else
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "Failed to send message.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }

            return response;
        }

        public Response AddServiceRequestOffer(ServiceRequestOffer serviceRequestOffer, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Insert into ServiceRequestOffers and get newly generated ID
                    string offerQuery = @"
                INSERT INTO ServiceRequestOffers (
                    RequestID, UserID, MasterAgreementID, MasterAgreementName, 
                    TaskDescription, RequestType, Project, DomainID, DomainName, 
                    CycleStatus, NumberOfSpecialists, NumberOfOffers
                ) 
                OUTPUT INSERTED.ServiceRequestOfferId
                VALUES (
                    @RequestID, @UserID, @MasterAgreementID, @MasterAgreementName, 
                    @TaskDescription, @RequestType, @Project, @DomainID, @DomainName, 
                    @CycleStatus, @NumberOfSpecialists, @NumberOfOffers
                )";

                    SqlCommand offerCmd = new SqlCommand(offerQuery, connection, transaction);
                    offerCmd.Parameters.AddWithValue("@RequestID", serviceRequestOffer.RequestID);
                    offerCmd.Parameters.AddWithValue("@UserID", serviceRequestOffer.UserID);
                    offerCmd.Parameters.AddWithValue("@MasterAgreementID", serviceRequestOffer.MasterAgreementID);
                    offerCmd.Parameters.AddWithValue("@MasterAgreementName", serviceRequestOffer.MasterAgreementName);
                    offerCmd.Parameters.AddWithValue("@TaskDescription", serviceRequestOffer.TaskDescription ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@RequestType", serviceRequestOffer.RequestType ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@Project", serviceRequestOffer.Project ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@DomainID", serviceRequestOffer.DomainID);
                    offerCmd.Parameters.AddWithValue("@DomainName", serviceRequestOffer.DomainName);
                    offerCmd.Parameters.AddWithValue("@CycleStatus", serviceRequestOffer.CycleStatus ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@NumberOfSpecialists", serviceRequestOffer.NumberOfSpecialists);
                    offerCmd.Parameters.AddWithValue("@NumberOfOffers", serviceRequestOffer.NumberOfOffers);

                    int newOfferId = (int)offerCmd.ExecuteScalar();

                    // Insert into ServiceOffers table
                    foreach (var offer in serviceRequestOffer.ServiceOffers)
                    {
                        string serviceOfferQuery = @"
                    INSERT INTO ServiceOffers (
                        ServiceRequestOfferId, ProviderName, ProviderID, EmployeeID, 
                        Role, Level, TechnologyLevel, Price
                    ) 
                    VALUES (
                        @ServiceRequestOfferId, @ProviderName, @ProviderID, @EmployeeID, 
                        @Role, @Level, @TechnologyLevel, @Price
                    )";

                        SqlCommand serviceOfferCmd = new SqlCommand(serviceOfferQuery, connection, transaction);
                        serviceOfferCmd.Parameters.AddWithValue("@ServiceRequestOfferId", newOfferId);
                        serviceOfferCmd.Parameters.AddWithValue("@ProviderName", offer.ProviderName);
                        serviceOfferCmd.Parameters.AddWithValue("@ProviderID", offer.ProviderID);
                        serviceOfferCmd.Parameters.AddWithValue("@EmployeeID", offer.EmployeeID);
                        serviceOfferCmd.Parameters.AddWithValue("@Role", offer.Role ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@Level", offer.Level ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@TechnologyLevel", offer.TechnologyLevel ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@Price", offer.Price);

                        serviceOfferCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    response.StatusCode = 200;
                    response.StatusMessage = "Service request offer added successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.StatusCode = 500;
                    response.StatusMessage = $"An error occurred: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response UpdateServiceRequestOffer(ServiceRequestOffer serviceRequestOffer, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Update ServiceRequestOffers table
                    string offerQuery = @"
            UPDATE ServiceRequestOffers 
            SET 
                RequestID = @RequestID, 
                UserID = @UserID, 
                MasterAgreementID = @MasterAgreementID, 
                MasterAgreementName = @MasterAgreementName, 
                TaskDescription = @TaskDescription, 
                RequestType = @RequestType, 
                Project = @Project, 
                DomainID = @DomainID, 
                DomainName = @DomainName, 
                CycleStatus = @CycleStatus, 
                NumberOfSpecialists = @NumberOfSpecialists, 
                NumberOfOffers = @NumberOfOffers
            WHERE ServiceRequestOfferId = @ServiceRequestOfferId";

                    SqlCommand offerCmd = new SqlCommand(offerQuery, connection, transaction);
                    offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                    offerCmd.Parameters.AddWithValue("@RequestID", serviceRequestOffer.RequestID);
                    offerCmd.Parameters.AddWithValue("@UserID", serviceRequestOffer.UserID);
                    offerCmd.Parameters.AddWithValue("@MasterAgreementID", serviceRequestOffer.MasterAgreementID);
                    offerCmd.Parameters.AddWithValue("@MasterAgreementName", serviceRequestOffer.MasterAgreementName);
                    offerCmd.Parameters.AddWithValue("@TaskDescription", serviceRequestOffer.TaskDescription ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@RequestType", serviceRequestOffer.RequestType ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@Project", serviceRequestOffer.Project ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@DomainID", serviceRequestOffer.DomainID);
                    offerCmd.Parameters.AddWithValue("@DomainName", serviceRequestOffer.DomainName);
                    offerCmd.Parameters.AddWithValue("@CycleStatus", serviceRequestOffer.CycleStatus ?? (object)DBNull.Value);
                    offerCmd.Parameters.AddWithValue("@NumberOfSpecialists", serviceRequestOffer.NumberOfSpecialists);
                    offerCmd.Parameters.AddWithValue("@NumberOfOffers", serviceRequestOffer.NumberOfOffers);

                    offerCmd.ExecuteNonQuery();

                    // First, delete related entries in ServiceOfferSelection table
                    string deleteOfferSelectionQuery = "DELETE FROM ServiceOfferSelection WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                    SqlCommand deleteOfferSelectionCmd = new SqlCommand(deleteOfferSelectionQuery, connection, transaction);
                    deleteOfferSelectionCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                    deleteOfferSelectionCmd.ExecuteNonQuery();

                    // Second, delete related entries in ServiceRequestOfferSelection table
                    string deleteRequestOfferSelectionQuery = "DELETE FROM ServiceRequestOfferSelection WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                    SqlCommand deleteRequestOfferSelectionCmd = new SqlCommand(deleteRequestOfferSelectionQuery, connection, transaction);
                    deleteRequestOfferSelectionCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                    deleteRequestOfferSelectionCmd.ExecuteNonQuery();

                    // Then, delete entries in ServiceOffers table
                    string deleteOffersQuery = "DELETE FROM ServiceOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                    SqlCommand deleteCmd = new SqlCommand(deleteOffersQuery, connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                    deleteCmd.ExecuteNonQuery();

                    // Insert updated ServiceOffers entries
                    foreach (var offer in serviceRequestOffer.ServiceOffers)
                    {
                        string serviceOfferQuery = @"
                INSERT INTO ServiceOffers (
                    ServiceRequestOfferId, ProviderName, ProviderID, EmployeeID, 
                    Role, Level, TechnologyLevel, Price
                ) 
                VALUES (
                    @ServiceRequestOfferId, @ProviderName, @ProviderID, @EmployeeID, 
                    @Role, @Level, @TechnologyLevel, @Price
                )";

                        SqlCommand serviceOfferCmd = new SqlCommand(serviceOfferQuery, connection, transaction);
                        serviceOfferCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                        serviceOfferCmd.Parameters.AddWithValue("@ProviderName", offer.ProviderName);
                        serviceOfferCmd.Parameters.AddWithValue("@ProviderID", offer.ProviderID);
                        serviceOfferCmd.Parameters.AddWithValue("@EmployeeID", offer.EmployeeID);
                        serviceOfferCmd.Parameters.AddWithValue("@Role", offer.Role ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@Level", offer.Level ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@TechnologyLevel", offer.TechnologyLevel ?? (object)DBNull.Value);
                        serviceOfferCmd.Parameters.AddWithValue("@Price", offer.Price);

                        serviceOfferCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    response.StatusCode = 200;
                    response.StatusMessage = "Service request offer updated successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.StatusCode = 500;
                    response.StatusMessage = $"An error occurred: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }
        public Response GetAllServiceRequestOffers(SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                // Fetch all service request offers
                string query = "SELECT * FROM ServiceRequestOffers";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOffer> serviceRequestOffers = new List<ServiceRequestOffer>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOffer offer = new ServiceRequestOffer
                        {
                            ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            DomainID = row["DomainID"] != DBNull.Value ? Convert.ToInt32(row["DomainID"]) : 0,
                            DomainName = row["DomainName"]?.ToString(),
                            CycleStatus = row["CycleStatus"]?.ToString(),
                            NumberOfSpecialists = row["NumberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["NumberOfSpecialists"]) : 0,
                            NumberOfOffers = row["NumberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["NumberOfOffers"]) : 0,
                            ServiceOffers = new List<ServiceOffer>()
                        };

                        // Fetch related ServiceOffers for this request offer
                        string offerQuery = "SELECT * FROM ServiceOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                        using (SqlCommand offerCmd = new SqlCommand(offerQuery, connection))
                        {
                            offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offer.ServiceRequestOfferId);
                            using (SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd))
                            {
                                DataTable offerTable = new DataTable();
                                offerAdapter.Fill(offerTable);

                                foreach (DataRow offerRow in offerTable.Rows)
                                {
                                    ServiceOffer serviceOffer = new ServiceOffer
                                    {
                                        OfferID = Convert.ToInt32(offerRow["OfferID"]),
                                        ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                                        ProviderName = offerRow["ProviderName"]?.ToString(),
                                        ProviderID = offerRow["ProviderID"]?.ToString(),
                                        EmployeeID = offerRow["EmployeeID"]?.ToString(),
                                        Role = offerRow["Role"]?.ToString(),
                                        Level = offerRow["Level"]?.ToString(),
                                        TechnologyLevel = offerRow["TechnologyLevel"]?.ToString(),
                                        Price = offerRow["Price"] != DBNull.Value ? Convert.ToDecimal(offerRow["Price"]) : 0
                                    };

                                    offer.ServiceOffers.Add(serviceOffer);
                                }
                            }
                        }

                        serviceRequestOffers.Add(offer);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service request offers retrieved successfully.";
                    response.listServiceRequestOffer = serviceRequestOffers;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service request offers found.";
                    response.listServiceRequestOffer = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetUserServiceRequestOffers(Guid userID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query = "SELECT * FROM ServiceRequestOffers WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userID);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOffer> serviceRequestOffers = new List<ServiceRequestOffer>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOffer offer = new ServiceRequestOffer
                        {
                            ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            DomainID = row["DomainID"] != DBNull.Value ? Convert.ToInt32(row["DomainID"]) : 0,
                            DomainName = row["DomainName"]?.ToString(),
                            CycleStatus = row["CycleStatus"]?.ToString(),
                            NumberOfSpecialists = row["NumberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["NumberOfSpecialists"]) : 0,
                            NumberOfOffers = row["NumberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["NumberOfOffers"]) : 0,
                            ServiceOffers = new List<ServiceOffer>()
                        };

                        // Fetch related ServiceOffers for this service request offer
                        string offerQuery = "SELECT * FROM ServiceOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                        using (SqlCommand offerCmd = new SqlCommand(offerQuery, connection))
                        {
                            offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offer.ServiceRequestOfferId);
                            using (SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd))
                            {
                                DataTable offerTable = new DataTable();
                                offerAdapter.Fill(offerTable);

                                foreach (DataRow offerRow in offerTable.Rows)
                                {
                                    ServiceOffer serviceOffer = new ServiceOffer
                                    {
                                        OfferID = Convert.ToInt32(offerRow["OfferID"]),
                                        ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                                        ProviderName = offerRow["ProviderName"]?.ToString(),
                                        ProviderID = offerRow["ProviderID"]?.ToString(),
                                        EmployeeID = offerRow["EmployeeID"]?.ToString(),
                                        Role = offerRow["Role"]?.ToString(),
                                        Level = offerRow["Level"]?.ToString(),
                                        TechnologyLevel = offerRow["TechnologyLevel"]?.ToString(),
                                        Price = offerRow["Price"] != DBNull.Value ? Convert.ToDecimal(offerRow["Price"]) : 0
                                    };

                                    offer.ServiceOffers.Add(serviceOffer);
                                }
                            }
                        }

                        serviceRequestOffers.Add(offer);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "User service request offers retrieved successfully.";
                    response.listServiceRequestOffer = serviceRequestOffers;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service request offers found for the user.";
                    response.listServiceRequestOffer = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetServiceRequestOffersForProviderManager(string email, SqlConnection connection)
        {
            Response response = new Response();

            try
            {
                connection.Open();

                // Fetch the Department for the logged-in ProviderManager using their email
                SqlCommand departmentCmd = new SqlCommand("SELECT Department FROM ProviderManager WHERE Email = @Email", connection);
                departmentCmd.Parameters.AddWithValue("@Email", email);

                string department = departmentCmd.ExecuteScalar()?.ToString();
                connection.Close();

                if (string.IsNullOrEmpty(department))
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Provider Manager's department not found.";
                    return response;
                }

                connection.Open();
                // Fetch Service Request Offers where DomainName matches the ProviderManager's Department
                string query = "SELECT * FROM ServiceRequestOffers WHERE DomainName = @DomainName";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@DomainName", department);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOffer> serviceRequestOffers = new List<ServiceRequestOffer>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOffer offer = new ServiceRequestOffer
                        {
                            ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                            RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                            UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            TaskDescription = row["TaskDescription"]?.ToString(),
                            RequestType = row["RequestType"]?.ToString(),
                            Project = row["Project"]?.ToString(),
                            DomainID = row["DomainID"] != DBNull.Value ? Convert.ToInt32(row["DomainID"]) : 0,
                            DomainName = row["DomainName"]?.ToString(),
                            CycleStatus = row["CycleStatus"]?.ToString(),
                            NumberOfSpecialists = row["NumberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["NumberOfSpecialists"]) : 0,
                            NumberOfOffers = row["NumberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["NumberOfOffers"]) : 0,
                            ServiceOffers = new List<ServiceOffer>()
                        };

                        // Fetch related ServiceOffers for this request offer
                        string offerQuery = "SELECT * FROM ServiceOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                        using (SqlCommand offerCmd = new SqlCommand(offerQuery, connection))
                        {
                            offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offer.ServiceRequestOfferId);
                            using (SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd))
                            {
                                DataTable offerTable = new DataTable();
                                offerAdapter.Fill(offerTable);

                                foreach (DataRow offerRow in offerTable.Rows)
                                {
                                    ServiceOffer serviceOffer = new ServiceOffer
                                    {
                                        OfferID = Convert.ToInt32(offerRow["OfferID"]),
                                        ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                                        ProviderName = offerRow["ProviderName"]?.ToString(),
                                        ProviderID = offerRow["ProviderID"]?.ToString(),
                                        EmployeeID = offerRow["EmployeeID"]?.ToString(),
                                        Role = offerRow["Role"]?.ToString(),
                                        Level = offerRow["Level"]?.ToString(),
                                        TechnologyLevel = offerRow["TechnologyLevel"]?.ToString(),
                                        Price = offerRow["Price"] != DBNull.Value ? Convert.ToDecimal(offerRow["Price"]) : 0
                                    };

                                    offer.ServiceOffers.Add(serviceOffer);
                                }
                            }
                        }

                        serviceRequestOffers.Add(offer);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service request offers retrieved successfully.";
                    response.listServiceRequestOffer = serviceRequestOffers;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service request offers found for the specified role domain.";
                    response.listServiceRequestOffer = null;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetServiceRequestOfferDetails(int serviceRequestOfferId, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query = "SELECT * FROM ServiceRequestOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    DataRow row = dataTable.Rows[0];

                    ServiceRequestOffer offer = new ServiceRequestOffer
                    {
                        ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                        RequestID = row["RequestID"] != DBNull.Value ? Guid.Parse(row["RequestID"].ToString()) : Guid.Empty,
                        UserID = row["UserID"] != DBNull.Value ? Guid.Parse(row["UserID"].ToString()) : Guid.Empty,
                        MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                        MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                        TaskDescription = row["TaskDescription"]?.ToString(),
                        RequestType = row["RequestType"]?.ToString(),
                        Project = row["Project"]?.ToString(),
                        DomainID = row["DomainID"] != DBNull.Value ? Convert.ToInt32(row["DomainID"]) : 0,
                        DomainName = row["DomainName"]?.ToString(),
                        CycleStatus = row["CycleStatus"]?.ToString(),
                        NumberOfSpecialists = row["NumberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["NumberOfSpecialists"]) : 0,
                        NumberOfOffers = row["NumberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["NumberOfOffers"]) : 0,
                        ServiceOffers = new List<ServiceOffer>()
                    };

                    // Fetch related ServiceOffers for this service request offer
                    string offerQuery = "SELECT * FROM ServiceOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                    using (SqlCommand offerCmd = new SqlCommand(offerQuery, connection))
                    {
                        offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);
                        using (SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd))
                        {
                            DataTable offerTable = new DataTable();
                            offerAdapter.Fill(offerTable);

                            foreach (DataRow offerRow in offerTable.Rows)
                            {
                                ServiceOffer serviceOffer = new ServiceOffer
                                {
                                    OfferID = Convert.ToInt32(offerRow["OfferID"]),
                                    ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                                    ProviderName = offerRow["ProviderName"]?.ToString(),
                                    ProviderID = offerRow["ProviderID"]?.ToString(),
                                    EmployeeID = offerRow["EmployeeID"]?.ToString(),
                                    Role = offerRow["Role"]?.ToString(),
                                    Level = offerRow["Level"]?.ToString(),
                                    TechnologyLevel = offerRow["TechnologyLevel"]?.ToString(),
                                    Price = offerRow["Price"] != DBNull.Value ? Convert.ToDecimal(offerRow["Price"]) : 0
                                };

                                offer.ServiceOffers.Add(serviceOffer);
                            }
                        }
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Service Request Offer details retrieved successfully.";
                    response.serviceRequestOffer = offer;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Service Request Offer not found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response AddServiceRequestOfferSelection(ServiceRequestOfferSelection offerSelection, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                // Insert into ServiceRequestOfferSelection table
                string requestQuery = @"
            INSERT INTO ServiceRequestOfferSelection 
            (ServiceRequestOfferId, RequestID, UserID, MasterAgreementID, MasterAgreementName, TaskDescription, RequestType, Project, DomainID, DomainName, CycleStatus, NumberOfSpecialists, NumberOfOffers)
            VALUES 
            (@ServiceRequestOfferId, @RequestID, @UserID, @MasterAgreementID, @MasterAgreementName, @TaskDescription, @RequestType, @Project, @DomainID, @DomainName, @CycleStatus, @NumberOfSpecialists, @NumberOfOffers)";

                SqlCommand requestCmd = new SqlCommand(requestQuery, connection);
                requestCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offerSelection.ServiceRequestOfferId);
                requestCmd.Parameters.AddWithValue("@RequestID", offerSelection.RequestID);
                requestCmd.Parameters.AddWithValue("@UserID", offerSelection.UserID);
                requestCmd.Parameters.AddWithValue("@MasterAgreementID", offerSelection.MasterAgreementID);
                requestCmd.Parameters.AddWithValue("@MasterAgreementName", offerSelection.MasterAgreementName);
                requestCmd.Parameters.AddWithValue("@TaskDescription", offerSelection.TaskDescription);
                requestCmd.Parameters.AddWithValue("@RequestType", offerSelection.RequestType);
                requestCmd.Parameters.AddWithValue("@Project", offerSelection.Project);
                requestCmd.Parameters.AddWithValue("@DomainID", offerSelection.DomainID);
                requestCmd.Parameters.AddWithValue("@DomainName", offerSelection.DomainName);
                requestCmd.Parameters.AddWithValue("@CycleStatus", offerSelection.CycleStatus);
                requestCmd.Parameters.AddWithValue("@NumberOfSpecialists", offerSelection.NumberOfSpecialists);
                requestCmd.Parameters.AddWithValue("@NumberOfOffers", offerSelection.NumberOfOffers);

                requestCmd.ExecuteNonQuery();

                // Insert into ServiceOfferSelection table
                foreach (var offer in offerSelection.ServiceOffers)
                {
                    string offerQuery = @"
                INSERT INTO ServiceOfferSelection 
                (OfferID, ServiceRequestOfferId, ProviderName, ProviderID, EmployeeID, Role, Level, TechnologyLevel, Price, Selection, Comment)
                VALUES 
                (@OfferID, @ServiceRequestOfferId, @ProviderName, @ProviderID, @EmployeeID, @Role, @Level, @TechnologyLevel, @Price, @Selection, @Comment)";

                    SqlCommand offerCmd = new SqlCommand(offerQuery, connection);
                    offerCmd.Parameters.AddWithValue("@OfferID", offer.OfferID);
                    offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offer.ServiceRequestOfferId);
                    offerCmd.Parameters.AddWithValue("@ProviderName", offer.ProviderName);
                    offerCmd.Parameters.AddWithValue("@ProviderID", offer.ProviderID);
                    offerCmd.Parameters.AddWithValue("@EmployeeID", offer.EmployeeID);
                    offerCmd.Parameters.AddWithValue("@Role", offer.Role);
                    offerCmd.Parameters.AddWithValue("@Level", offer.Level);
                    offerCmd.Parameters.AddWithValue("@TechnologyLevel", offer.TechnologyLevel);
                    offerCmd.Parameters.AddWithValue("@Price", offer.Price);
                    offerCmd.Parameters.AddWithValue("@Selection", offer.Selection);
                    offerCmd.Parameters.AddWithValue("@Comment", offer.Comment);

                    offerCmd.ExecuteNonQuery();
                }

                response.StatusCode = 200;
                response.StatusMessage = "Service request offer selection added successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public ServiceRequestOfferSelection GetServiceRequestOfferSelection(int serviceRequestOfferId, SqlConnection connection)
        {
            ServiceRequestOfferSelection offerSelection = null;
            try
            {
                connection.Open();

                string query = @"
        SELECT * 
        FROM ServiceRequestOfferSelection 
        WHERE ServiceRequestOfferId = @ServiceRequestOfferId";

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    offerSelection = new ServiceRequestOfferSelection
                    {
                        ServiceRequestOfferId = Convert.ToInt32(reader["ServiceRequestOfferId"]),
                        RequestID = Guid.Parse(reader["RequestID"].ToString()),
                        UserID = Guid.Parse(reader["UserID"].ToString()),
                        MasterAgreementID = Convert.ToInt32(reader["MasterAgreementID"]),
                        MasterAgreementName = reader["MasterAgreementName"].ToString(),
                        TaskDescription = reader["TaskDescription"].ToString(),
                        RequestType = reader["RequestType"].ToString(),
                        Project = reader["Project"].ToString(),
                        DomainID = Convert.ToInt32(reader["DomainID"]),
                        DomainName = reader["DomainName"].ToString(),
                        CycleStatus = reader["CycleStatus"].ToString(),
                        NumberOfSpecialists = Convert.ToInt32(reader["NumberOfSpecialists"]),
                        NumberOfOffers = Convert.ToInt32(reader["NumberOfOffers"]),
                        ServiceOffers = new List<ServiceOfferSelection>()
                    };
                }
                reader.Close();

                if (offerSelection != null)
                {
                    string offerQuery = @"
            SELECT * 
            FROM ServiceOfferSelection 
            WHERE ServiceRequestOfferId = @ServiceRequestOfferId AND Selection = 'Selected'";

                    SqlCommand offerCmd = new SqlCommand(offerQuery, connection);
                    offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);

                    SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd);
                    DataTable offerTable = new DataTable();
                    offerAdapter.Fill(offerTable);

                    foreach (DataRow row in offerTable.Rows)
                    {
                        ServiceOfferSelection offer = new ServiceOfferSelection
                        {
                            OfferID = Convert.ToInt32(row["OfferID"]),
                            ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                            ProviderName = row["ProviderName"].ToString(),
                            ProviderID = row["ProviderID"].ToString(),
                            EmployeeID = row["EmployeeID"].ToString(),
                            Role = row["Role"].ToString(),
                            Level = row["Level"].ToString(),
                            TechnologyLevel = row["TechnologyLevel"].ToString(),
                            Price = Convert.ToDecimal(row["Price"]),
                            Selection = row["Selection"].ToString(),
                            Comment = row["Comment"].ToString()
                        };
                        offerSelection.ServiceOffers.Add(offer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return offerSelection;
        }

        public List<ServiceRequestOfferSelection> GetAllServiceRequestOfferSelections(SqlConnection connection)
        {
            List<ServiceRequestOfferSelection> offerSelections = new List<ServiceRequestOfferSelection>();

            try
            {
                connection.Open();

                string query = "SELECT * FROM ServiceRequestOfferSelection";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    ServiceRequestOfferSelection offerSelection = new ServiceRequestOfferSelection
                    {
                        ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                        RequestID = Guid.Parse(row["RequestID"].ToString()),
                        UserID = Guid.Parse(row["UserID"].ToString()),
                        MasterAgreementID = Convert.ToInt32(row["MasterAgreementID"]),
                        MasterAgreementName = row["MasterAgreementName"].ToString(),
                        TaskDescription = row["TaskDescription"].ToString(),
                        RequestType = row["RequestType"].ToString(),
                        Project = row["Project"].ToString(),
                        DomainID = Convert.ToInt32(row["DomainID"]),
                        DomainName = row["DomainName"].ToString(),
                        CycleStatus = row["CycleStatus"].ToString(),
                        NumberOfSpecialists = Convert.ToInt32(row["NumberOfSpecialists"]),
                        NumberOfOffers = Convert.ToInt32(row["NumberOfOffers"]),
                        ServiceOffers = new List<ServiceOfferSelection>()
                    };

                    // Get corresponding service offers (both selected and not selected)
                    string offerQuery = "SELECT * FROM ServiceOfferSelection WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                    SqlCommand offerCmd = new SqlCommand(offerQuery, connection);
                    offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", offerSelection.ServiceRequestOfferId);

                    SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd);
                    DataTable offerTable = new DataTable();
                    offerAdapter.Fill(offerTable);

                    foreach (DataRow offerRow in offerTable.Rows)
                    {
                        ServiceOfferSelection offer = new ServiceOfferSelection
                        {
                            OfferID = Convert.ToInt32(offerRow["OfferID"]),
                            ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                            ProviderName = offerRow["ProviderName"].ToString(),
                            ProviderID = offerRow["ProviderID"].ToString(),
                            EmployeeID = offerRow["EmployeeID"].ToString(),
                            Role = offerRow["Role"].ToString(),
                            Level = offerRow["Level"].ToString(),
                            TechnologyLevel = offerRow["TechnologyLevel"].ToString(),
                            Price = Convert.ToDecimal(offerRow["Price"]),
                            Selection = offerRow["Selection"].ToString(),
                            Comment = offerRow["Comment"].ToString()
                        };
                        offerSelection.ServiceOffers.Add(offer);
                    }

                    offerSelections.Add(offerSelection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return offerSelections;
        }

        public Response CreateOrder(ServiceRequestOfferSelection serviceRequestOffer, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Insert into ServiceRequestCreateOrder table
                    string insertOrderQuery = @"
            INSERT INTO ServiceRequestCreateOrder 
            (ServiceRequestOfferId, RequestID, UserID, MasterAgreementID, MasterAgreementName, 
             TaskDescription, RequestType, Project, DomainID, DomainName, CycleStatus, NumberOfSpecialists, NumberOfOffers)
            VALUES 
            (@ServiceRequestOfferId, @RequestID, @UserID, @MasterAgreementID, @MasterAgreementName, 
             @TaskDescription, @RequestType, @Project, @DomainID, @DomainName, @CycleStatus, @NumberOfSpecialists, @NumberOfOffers)";

                    SqlCommand orderCmd = new SqlCommand(insertOrderQuery, connection, transaction);
                    orderCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                    orderCmd.Parameters.AddWithValue("@RequestID", serviceRequestOffer.RequestID);
                    orderCmd.Parameters.AddWithValue("@UserID", serviceRequestOffer.UserID);
                    orderCmd.Parameters.AddWithValue("@MasterAgreementID", serviceRequestOffer.MasterAgreementID);
                    orderCmd.Parameters.AddWithValue("@MasterAgreementName", serviceRequestOffer.MasterAgreementName);
                    orderCmd.Parameters.AddWithValue("@TaskDescription", serviceRequestOffer.TaskDescription);
                    orderCmd.Parameters.AddWithValue("@RequestType", serviceRequestOffer.RequestType);
                    orderCmd.Parameters.AddWithValue("@Project", serviceRequestOffer.Project);
                    orderCmd.Parameters.AddWithValue("@DomainID", serviceRequestOffer.DomainID);
                    orderCmd.Parameters.AddWithValue("@DomainName", serviceRequestOffer.DomainName);
                    orderCmd.Parameters.AddWithValue("@CycleStatus", serviceRequestOffer.CycleStatus);
                    orderCmd.Parameters.AddWithValue("@NumberOfSpecialists", serviceRequestOffer.NumberOfSpecialists);
                    orderCmd.Parameters.AddWithValue("@NumberOfOffers", serviceRequestOffer.NumberOfOffers);

                    orderCmd.ExecuteNonQuery();

                    // Insert into ServiceRequestCreateOrderOffers table
                    foreach (var offer in serviceRequestOffer.ServiceOffers)
                    {
                        string insertOfferQuery = @"
                INSERT INTO ServiceRequestCreateOrderOffers 
                (OfferID, ServiceRequestOfferId, ProviderName, ProviderID, EmployeeID, Role, Level, TechnologyLevel, Price, Selection, Comment)
                VALUES 
                (@OfferID, @ServiceRequestOfferId, @ProviderName, @ProviderID, @EmployeeID, @Role, @Level, @TechnologyLevel, @Price, @Selection, @Comment)";

                        SqlCommand offerCmd = new SqlCommand(insertOfferQuery, connection, transaction);
                        offerCmd.Parameters.AddWithValue("@OfferID", offer.OfferID);
                        offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOffer.ServiceRequestOfferId);
                        offerCmd.Parameters.AddWithValue("@ProviderName", offer.ProviderName);
                        offerCmd.Parameters.AddWithValue("@ProviderID", offer.ProviderID);
                        offerCmd.Parameters.AddWithValue("@EmployeeID", offer.EmployeeID);
                        offerCmd.Parameters.AddWithValue("@Role", offer.Role);
                        offerCmd.Parameters.AddWithValue("@Level", offer.Level);
                        offerCmd.Parameters.AddWithValue("@TechnologyLevel", offer.TechnologyLevel);
                        offerCmd.Parameters.AddWithValue("@Price", offer.Price);
                        offerCmd.Parameters.AddWithValue("@Selection", offer.Selection);
                        offerCmd.Parameters.AddWithValue("@Comment", offer.Comment);

                        offerCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    response.StatusCode = 200;
                    response.StatusMessage = "Order created successfully.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.StatusCode = 500;
                    response.StatusMessage = "An error occurred: " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        // Get all orders
        public Response GetAllOrders(SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                string query = "SELECT * FROM ServiceRequestCreateOrder";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOfferSelection> orders = new List<ServiceRequestOfferSelection>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOfferSelection order = MapOrder(row, connection);
                        orders.Add(order);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "All service request orders retrieved successfully.";
                    response.listServiceRequestOrders = orders;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No service request orders found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        public Response GetSingleOrder(int serviceRequestOfferId, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                string query = "SELECT * FROM ServiceRequestCreateOrder WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    DataRow row = dataTable.Rows[0];
                    ServiceRequestOfferSelection order = MapOrder(row, connection);

                    response.StatusCode = 200;
                    response.StatusMessage = "Service request order retrieved successfully.";
                    response.serviceRequestOrder = order;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Service request order not found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        // Get orders for a specific user
        public Response GetUserOrders(Guid userID, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                string query = "SELECT * FROM ServiceRequestCreateOrder WHERE UserID = @UserID";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserID", userID);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOfferSelection> orders = new List<ServiceRequestOfferSelection>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOfferSelection order = MapOrder(row, connection);
                        orders.Add(order);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "User service request orders retrieved successfully.";
                    response.listServiceRequestOrders = orders;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No orders found for the user.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        // Get orders for a provider manager based on domain
        public Response GetProviderManagerOrders(string email, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                // Fetch the Department (DomainName) for the logged-in ProviderManager using their email
                string departmentQuery = "SELECT Department FROM ProviderManager WHERE Email = @Email";
                SqlCommand departmentCmd = new SqlCommand(departmentQuery, connection);
                departmentCmd.Parameters.AddWithValue("@Email", email);

                string? department = departmentCmd.ExecuteScalar()?.ToString();
                connection.Close();

                if (string.IsNullOrEmpty(department))
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "Provider Manager's department not found.";
                    return response;
                }

                connection.Open();
                string query = "SELECT * FROM ServiceRequestCreateOrder WHERE DomainName = @DomainName";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@DomainName", department);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<ServiceRequestOfferSelection> orders = new List<ServiceRequestOfferSelection>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        ServiceRequestOfferSelection order = MapOrder(row, connection);
                        orders.Add(order);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Provider manager service request orders retrieved successfully.";
                    response.listServiceRequestOrders = orders;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No orders found for the provider manager.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }

        // Helper function to map order and fetch related offers
        private ServiceRequestOfferSelection MapOrder(DataRow row, SqlConnection connection)
        {
            ServiceRequestOfferSelection order = new ServiceRequestOfferSelection
            {
                ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                RequestID = Guid.Parse(row["RequestID"].ToString()),
                UserID = Guid.Parse(row["UserID"].ToString()),
                MasterAgreementID = Convert.ToInt32(row["MasterAgreementID"]),
                MasterAgreementName = row["MasterAgreementName"].ToString(),
                TaskDescription = row["TaskDescription"].ToString(),
                RequestType = row["RequestType"].ToString(),
                Project = row["Project"].ToString(),
                DomainID = Convert.ToInt32(row["DomainID"]),
                DomainName = row["DomainName"].ToString(),
                CycleStatus = row["CycleStatus"].ToString(),
                NumberOfSpecialists = Convert.ToInt32(row["NumberOfSpecialists"]),
                NumberOfOffers = Convert.ToInt32(row["NumberOfOffers"]),
                ServiceOffers = new List<ServiceOfferSelection>()
            };

            string offerQuery = "SELECT * FROM ServiceRequestCreateOrderOffers WHERE ServiceRequestOfferId = @ServiceRequestOfferId";
            SqlCommand offerCmd = new SqlCommand(offerQuery, connection);
            offerCmd.Parameters.AddWithValue("@ServiceRequestOfferId", order.ServiceRequestOfferId);

            SqlDataAdapter offerAdapter = new SqlDataAdapter(offerCmd);
            DataTable offerTable = new DataTable();
            offerAdapter.Fill(offerTable);

            foreach (DataRow offerRow in offerTable.Rows)
            {
                ServiceOfferSelection offer = new ServiceOfferSelection
                {
                    OfferID = Convert.ToInt32(offerRow["OfferID"]),
                    ServiceRequestOfferId = Convert.ToInt32(offerRow["ServiceRequestOfferId"]),
                    ProviderName = offerRow["ProviderName"].ToString(),
                    ProviderID = offerRow["ProviderID"].ToString(),
                    EmployeeID = offerRow["EmployeeID"].ToString(),
                    Role = offerRow["Role"].ToString(),
                    Level = offerRow["Level"].ToString(),
                    TechnologyLevel = offerRow["TechnologyLevel"].ToString(),
                    Price = Convert.ToDecimal(offerRow["Price"]),
                    Selection = offerRow["Selection"].ToString(),
                    Comment = offerRow["Comment"].ToString()
                };
                order.ServiceOffers.Add(offer);
            }

            return order;
        }

        public Response GetMessages2(int serviceRequestOfferId, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM Messages2 WHERE ServiceRequestOfferId = @ServiceRequestOfferId ORDER BY Timestamp ASC", connection);
                adapter.SelectCommand.Parameters.AddWithValue("@ServiceRequestOfferId", serviceRequestOfferId);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<Message2> messages = new List<Message2>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        Message2 message = new Message2
                        {
                            MessageID = Guid.Parse(row["MessageID"].ToString()),
                            ServiceRequestOfferId = Convert.ToInt32(row["ServiceRequestOfferId"]),
                            SenderID = Guid.Parse(row["SenderID"].ToString()),
                            SenderRole = row["SenderRole"].ToString(),
                            MessageContent = row["MessageContent"].ToString(),
                            Timestamp = DateTime.Parse(row["Timestamp"].ToString())
                        };
                        messages.Add(message);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Messages retrieved successfully.";
                    response.listMessages2 = messages;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No messages found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }

            return response;
        }
        public Response SendMessage2(Message2 message, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Messages2 (ServiceRequestOfferId, SenderID, SenderRole, MessageContent) " +
                    "VALUES (@ServiceRequestOfferId, @SenderID, @SenderRole, @MessageContent)", connection);

                cmd.Parameters.AddWithValue("@ServiceRequestOfferId", message.ServiceRequestOfferId);
                cmd.Parameters.AddWithValue("@SenderID", message.SenderID);
                cmd.Parameters.AddWithValue("@SenderRole", message.SenderRole);
                cmd.Parameters.AddWithValue("@MessageContent", message.MessageContent);

                connection.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();

                if (rowsAffected > 0)
                {
                    response.StatusCode = 200;
                    response.StatusMessage = "Message sent successfully.";
                }
                else
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "Failed to send message.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }

            return response;
        }

        public Response AddEvaluation(Evaluation evaluation, SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();

                string query = @"
            INSERT INTO ServiceRequestEvaluations 
            (ServiceRequestId, AgreementID, AgreementName, TaskDescription, Type, Project, ProviderID, ProviderName, TimelinessScore, QualityScore, OverallScore)
            VALUES 
            (@ServiceRequestId, @AgreementID, @AgreementName, @TaskDescription, @Type, @Project, @ProviderID, @ProviderName, @TimelinessScore, @QualityScore, @OverallScore)";

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ServiceRequestId", evaluation.ServiceRequestId);
                cmd.Parameters.AddWithValue("@AgreementID", evaluation.AgreementID);
                cmd.Parameters.AddWithValue("@AgreementName", evaluation.AgreementName);
                cmd.Parameters.AddWithValue("@TaskDescription", evaluation.TaskDescription);
                cmd.Parameters.AddWithValue("@Type", evaluation.Type);
                cmd.Parameters.AddWithValue("@Project", evaluation.Project);
                cmd.Parameters.AddWithValue("@ProviderID", evaluation.ProviderID);
                cmd.Parameters.AddWithValue("@ProviderName", evaluation.ProviderName);
                cmd.Parameters.AddWithValue("@TimelinessScore", evaluation.TimelinessScore);
                cmd.Parameters.AddWithValue("@QualityScore", evaluation.QualityScore);
                cmd.Parameters.AddWithValue("@OverallScore", evaluation.OverallScore);

                cmd.ExecuteNonQuery();

                response.StatusCode = 200;
                response.StatusMessage = "Evaluation added successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "Error: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }
            return response;
        }

        public Response GetAllEvaluations(SqlConnection connection)
        {
            Response response = new Response();
            try
            {
                connection.Open();
                string query = "SELECT * FROM ServiceRequestEvaluations";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    List<Evaluation> evaluations = new List<Evaluation>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        Evaluation evaluation = new Evaluation
                        {
                            EvaluationID = Convert.ToInt32(row["EvaluationID"]),
                            ServiceRequestId = Guid.Parse(row["ServiceRequestId"].ToString()),
                            AgreementID = Convert.ToInt32(row["AgreementID"]),
                            AgreementName = row["AgreementName"].ToString(),
                            TaskDescription = row["TaskDescription"].ToString(),
                            Type = row["Type"].ToString(),
                            Project = row["Project"].ToString(),
                            ProviderID = row["ProviderID"].ToString(),
                            ProviderName = row["ProviderName"].ToString(),
                            TimelinessScore = Convert.ToInt32(row["TimelinessScore"]),
                            QualityScore = Convert.ToInt32(row["QualityScore"]),
                            OverallScore = Convert.ToDecimal(row["OverallScore"]),
                            CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                        };

                        evaluations.Add(evaluation);
                    }

                    response.StatusCode = 200;
                    response.StatusMessage = "Evaluations retrieved successfully.";
                    response.listEvaluations = evaluations;
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusMessage = "No evaluations found.";
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = "An error occurred: " + ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return response;
        }
    }
}
