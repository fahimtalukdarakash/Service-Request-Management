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
                            IsApproved = row["IsApproved"] != DBNull.Value ? Convert.ToInt32(row["IsApproved"]) : 0,
                            MasterAgreementID = row["MasterAgreementID"] != DBNull.Value ? Convert.ToInt32(row["MasterAgreementID"]) : 0,
                            MasterAgreementName = row["MasterAgreementName"]?.ToString(),
                            SelectedDomainName = row["DomainName"]?.ToString(),
                            numberOfSpecialists = row["numberOfSpecialists"] != DBNull.Value ? Convert.ToInt32(row["numberOfSpecialists"]) : (int?)null,
                            numberOfOffers = row["numberOfOffers"] != DBNull.Value ? Convert.ToInt32(row["numberOfOffers"]) : (int?)null,
                            cycleStatus = cycle,
                            RoleSpecific = new List<RoleSpecific>() // Initialize role-specific list
                        };

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

    }
}
