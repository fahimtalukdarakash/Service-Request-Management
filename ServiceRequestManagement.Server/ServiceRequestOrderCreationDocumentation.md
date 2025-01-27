# CreateOrder API Method

This API method is designed to create an order based on the `ServiceRequestOfferSelection` provided. It handles the logic of calling a Data Access Layer (DAL) method to perform the order creation in the database and then returns the appropriate response.

## Method Definition

```csharp
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
```
# `CreateOrder` Method

This method is part of the DAL (Data Access Layer) used to create an order by inserting data into two database tables: `ServiceRequestCreateOrder` and `ServiceRequestCreateOrderOffers`. The method takes care of the SQL queries to insert data and ensures the process is handled within a transaction to maintain integrity.

## Method Definition

```csharp
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
```
# `GetAllOrders` API Method

The `GetAllOrders` API method retrieves all the service request orders from the database by calling the Data Access Layer (DAL). It returns either a list of service request orders on success or an error message if no orders are found or if there’s an issue retrieving the data.

## Method Definition

```csharp
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
```
# `GetAllOrders` Method

The `GetAllOrders` method retrieves all service request orders from the database. It performs a SQL query to fetch all rows from the `ServiceRequestCreateOrder` table, maps each row to an `ServiceRequestOfferSelection` object, and returns a list of these orders as part of the response. The method also handles exceptions and provides appropriate status codes for different outcomes.

## Method Definition

```csharp
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
```
# `GetSingleOrder` API Method

The `GetSingleOrder` API method retrieves a specific service request order by its `serviceRequestOfferId`. It queries the database, calls the Data Access Layer (DAL) to fetch the order details, and returns either the details of the service request order or an error message if not found.

## Method Definition

```csharp
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
```
# `GetSingleOrder` Method

The `GetSingleOrder` method is responsible for retrieving a specific service request order from the `ServiceRequestCreateOrder` table in the database based on the given `serviceRequestOfferId`. It executes a SQL query to fetch the order, maps the result, and returns the order details or an appropriate error message.

## Method Definition

```csharp
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
```
# `GetUserOrders` API Method

The `GetUserOrders` API method retrieves all service request orders for a specific user, identified by their `userID`. The method queries the database to fetch the orders associated with that user and returns them as a JSON response. If no orders are found, it returns an error message.

## Method Definition

```csharp
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
```
# `GetUserOrders` API Method

The `GetUserOrders` API method retrieves a list of service request orders associated with a specific user, identified by their `userID`. It fetches orders from the `ServiceRequestCreateOrder` table in the database and returns them as a JSON response. If no orders are found for the user, it returns a "not found" error message.

## Method Definition

```csharp
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
        } public Response GetUserOrders(Guid userID, SqlConnection connection)
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
```
# `GetProviderManagerOrders` API Method

The `GetProviderManagerOrders` API method retrieves a list of service request orders that are associated with a specific provider manager, identified by their email address. It queries the `ServiceRequestCreateOrder` table in the database and returns the list of orders in JSON format. If no orders are found for the given email, a "not found" response is returned.

## Method Definition

```csharp
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
```
# `GetProviderManagerOrders` API Method

The `GetProviderManagerOrders` method retrieves a list of service request orders associated with a provider manager, based on the department of the provider manager, identified by their email. The department (DomainName) is first fetched using the provider manager's email, and then it queries the `ServiceRequestCreateOrder` table using the department as a filter to retrieve the service request orders.

This method returns a list of orders, or a "not found" message if no matching orders are found for the department. In case of an error, an appropriate error message will be returned.

## Method Definition

```csharp
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
```