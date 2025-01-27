## `Registration` Method

The `Registration` method is an asynchronous function that handles the registration of a new user or entity. It calls the `Dal.Registration` method to insert the registration data into the database and returns a `Response` object containing the result of the operation.

### Method Signature
```csharp
public async Task<Response> Registration(Registration registration)
{
    Response response = new Response();
    SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
    Dal dal = new Dal();

    // Use 'await' to wait for the asynchronous operation
    response = await dal.Registration(registration, connection);

    return response;
}
```

## `Dal.Registration` Method

The `Registration` method is an asynchronous function that handles the registration of a user or entity into the system. The method checks if the user's role is "ProviderManager" and fetches data from an external API before inserting the user data into the database. It returns a `Response` object containing the result of the operation.

### Method Signature
```csharp
public async Task<Response> Registration(Registration registration, SqlConnection connection)
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
```
## `Login` Method

The `Login` method handles user login by validating credentials against the data in the database. It returns a `Response` object indicating the success or failure of the login attempt.

### Method Signature
```csharp
public Response Login(Registration registration)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.Login(registration, connection);
            return response;
        }

```
## ` Dal Login` Method

The `Login` method validates user login credentials by checking against the `Registration` and `ProviderManager` tables. It returns a `Response` object indicating whether the login was successful and includes the user details if successful.

### Method Signature
```csharp
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

```
## `ProviderManagerRegistration` Method

The `ProviderManagerRegistration` method facilitates the registration of a `ProviderManager`. It establishes a database connection, invokes the `Dal` layer to perform the registration, and returns a response indicating the operation's success or failure.

### Method Signature
```csharp
 public Response ProviderManagerRegistration(ProviderManager providerManager)
        {
            Response response = new Response();
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            Dal dal = new Dal();
            response = dal.ProviderManagerRegistration(providerManager, connection);
            return response;
        }

```

## ` Dal ProviderManagerRegistration` Method

The `ProviderManagerRegistration` method handles the registration of a `ProviderManager` by validating the user's approval status, inserting details into the `ProviderManager` table, and returning a response indicating the outcome.

### Method Signature
```csharp
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

```





