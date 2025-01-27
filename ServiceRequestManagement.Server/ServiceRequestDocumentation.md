## AddServiceRequest Method

The `AddServiceRequest` method handles the addition of a new service request to the system.

### Syntax

```csharp
 public Response AddServiceRequest(ServiceRequest serviceRequest)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.AddServiceRequest(serviceRequest, connection);
            return response;
        }
```

## AddServiceRequest (with SQL Transaction)

The `AddServiceRequest` method inserts a new service request into the database, including its associated role-specific details. It uses a SQL transaction to ensure atomicity and handle exceptions gracefully.

### Syntax

```csharp
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
```

## ServiceRequestList

The `ServiceRequestList` method retrieves a list of all service requests from the database. If the operation is successful, it returns the list of service requests; otherwise, it returns an error message.

### Syntax

```csharp
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
```

## `ServiceRequestList`

The `ServiceRequestList` method retrieves all service requests from the database. It returns a list of service requests if successful; otherwise, it returns an error message.

### Syntax

```csharp
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
```

## `ServiceRequestApproval`

The `ServiceRequestApproval` method processes the approval of a service request. It accepts a `requestID` to identify the service request that needs to be approved and updates the request status accordingly.

### Syntax

```csharp
public Response ServiceRequestApproval([FromBody] Guid requestID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ServiceRequestApproval(requestID, connection);
            return response;
        }
```

## `ServiceRequestApproval`

The `ServiceRequestApproval` method approves a given service request by updating its status to "approved" in the database. The method identifies the service request through its unique `requestID`.

### Syntax

```csharp
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
```

## `ViewServiceRequests`

The `ViewServiceRequests` method allows users with different roles (e.g., Admin, Manager) to view service requests based on their role and user ID. Additionally, it can filter requests by department if provided.

### Syntax

```csharp
public Response ViewServiceRequests(string userRole, Guid userID, string department = null)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ViewServiceRequests(userRole, userID, department, connection);
            return response;
        }
```

## `ViewServiceRequests`

The `ViewServiceRequests` method is designed to allow users to view service requests based on their roles and user details. Depending on the user's role (User or ProviderManager) and the optional department parameter, it fetches different sets of service requests from the database.

### Syntax

```csharp
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
```

## `GetUserServiceRequests`

The `GetUserServiceRequests` method is designed to fetch and return all service requests associated with a specific user based on their `userID`. This method helps in retrieving the service requests a user has created in the system.

### Syntax

```csharp
 public Response GetUserServiceRequests(Guid userID)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetUserServiceRequests(userID, connection);
            return response;
        }
```

## `GetUserServiceRequests`

The `GetUserServiceRequests` method retrieves all service requests associated with a specific user from the database. The user is identified by the provided `userID`, and the service requests are filtered by this ID.

### Syntax

```csharp
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
```

## `GetServiceRequestsForProviderManager`

The `GetServiceRequestsForProviderManager` method retrieves all service requests associated with a specific provider manager identified by their email. The method filters service requests based on the provider manager's role, and these requests are returned to help the provider manager track, review, and manage service tasks.

### Syntax

```csharp
public Response GetServiceRequestsForProviderManager(string email)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.GetServiceRequestsForProviderManager(email, connection);
            return response;
        }
```

## `GetServiceRequestsForProviderManager`

The `GetServiceRequestsForProviderManager` method retrieves service requests for a specific provider manager by using their email address. The method first fetches the provider manager's department from the `ProviderManager` table using their email, then queries the `ServiceRequests` table to retrieve the service requests where the `DomainName` matches the department of the provider manager.

### Syntax

```csharp
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
```

## `GetServiceRequestDetails`

The `GetServiceRequestDetails` method retrieves detailed information about a specific service request using its unique `RequestID`. It queries the database to fetch the service request details and returns the result based on the success or failure of the operation.

### Syntax

```csharp
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
```

## `GetServiceRequestDetails`

The `GetServiceRequestDetails` method retrieves detailed information about a specific service request, including related `RoleSpecific` details and external domain data via an API. It utilizes a SQL database to fetch information and an external HTTP API to get the domain ID based on the `MasterAgreementID`. If the service request exists, it returns the detailed data; otherwise, it provides an error response.

### Syntax

```csharp
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
```
