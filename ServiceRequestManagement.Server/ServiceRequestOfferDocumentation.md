### AddServiceRequestOffer Method

This method is used to add a new service request offer to the system.

---

#### Method Signature

```csharp
 public Response AddServiceRequestOffer(ServiceRequestOffer serviceRequestOffer)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.AddServiceRequestOffer(serviceRequestOffer, connection);
            return response;
        }

```

### AddServiceRequestOffer Method with Transactional Logic

This method is responsible for adding a service request offer and its associated service offers into the database using a transactional approach.

---

#### Method Signature

```csharp
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

```

### UpdateServiceRequestOffer API Endpoint

This endpoint is responsible for updating an existing service request offer in the database.

---

#### Endpoint Details

- **Route**: `/UpdateServiceRequestOffer`
- **HTTP Method**: POST
- **Content-Type**: `application/json`

---

#### Method Signature

```csharp
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


```

### UpdateServiceRequestOffer Method

This method updates an existing service request offer and its related entries in the database.

---

#### Method Signature

```csharp
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
```

### GetAllServiceRequestOffers Method

This method retrieves all service request offers from the database.

---

#### Method Signature

```csharp
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

```

### GetAllServiceRequestOffers Method (DAL)

This method retrieves all service request offers from the database, along with their associated service offers.

---

#### Method Signature

```csharp
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

```

### GetUserServiceRequestOffers Method (Controller)

This method retrieves all service request offers associated with a specific user, based on the user's ID.

---

#### Method Signature

```csharp
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

```

### GetUserServiceRequestOffers Method (Data Access Layer)

This method retrieves all service request offers associated with a specific user, based on the user's ID, and returns the offers along with related service offer details. It handles database operations, manages exceptions, and returns appropriate status codes.

---

#### Method Signature

```csharp
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

```

### GetServiceRequestOffersForProviderManager Method (Controller)

This method retrieves all service request offers associated with a specific provider manager based on the provider manager's email.

---

#### Method Signature

```csharp
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

```

### GetServiceRequestOffersForProviderManager Method (Data Access Layer)

This method retrieves service request offers associated with a provider manager based on the provider manager's email and their department.

---

#### Method Signature

```csharp
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

```

### GetServiceRequestOfferDetails Method (Controller)

This method retrieves the details of a specific service request offer based on the service request offer ID.

---

#### Method Signature

```csharp
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
```

### GetServiceRequestOfferDetails Method (Data Access Layer)

This method retrieves the details of a specific service request offer based on the provided service request offer ID.

---

#### Method Signature

```csharp
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
```
