## `AddServiceRequestOfferSelection`

The `AddServiceRequestOfferSelection` method is used to add a service request offer selection to the database. The data is provided through a `ServiceRequestOfferSelection` object passed in the request body. The method utilizes a SQL database to insert the offer selection data and returns a success or error message based on the outcome.

### Syntax

```csharp
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
```
## `AddServiceRequestOfferSelection`

The `AddServiceRequestOfferSelection` method is responsible for adding a service request offer selection, including the main service request offer details and associated service offers, into the database. It performs two main tasks: inserts data into the `ServiceRequestOfferSelection` table and inserts associated offer details into the `ServiceOfferSelection` table.

### Syntax

```csharp
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
```

## `GetServiceRequestOfferSelection`

The `GetServiceRequestOfferSelection` method is responsible for retrieving the details of a specific service request offer selection from the database based on the provided `ServiceRequestOfferId`. The method returns the main details of the service request offer and associated selected service offers if found.

### Syntax

```csharp
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
```
## `GetServiceRequestOfferSelection`

The `GetServiceRequestOfferSelection` method retrieves the details of a specific service request offer selection, including any associated selected service offers, from the database based on the provided `serviceRequestOfferId`.

This method fetches data from the `ServiceRequestOfferSelection` table and uses the given `ServiceRequestOfferId` to retrieve the relevant service request offer and its associated service offers from the `ServiceOfferSelection` table. If no offers are selected, the method returns a `null` value.

### Syntax

```csharp
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
```
## `GetAllServiceRequestOfferSelections`

The `GetAllServiceRequestOfferSelections` method retrieves a list of all service request offer selections from the database. It fetches all the service request offers and their associated selected service offers if any. This API is typically used to get a complete overview of all available service request offer selections in the system.

If no selections are found, the method returns a message indicating that no records exist.

### Syntax

```csharp
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
```
## `GetAllServiceRequestOfferSelections`

The `GetAllServiceRequestOfferSelections` method retrieves all the service request offer selections and their corresponding service offers from the database. This method fetches detailed data for all service requests and associated offers.

The data is returned in the form of a list of `ServiceRequestOfferSelection` objects, where each object contains the request details and its related service offers, including selected and non-selected offers.

### Syntax

```csharp
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
```