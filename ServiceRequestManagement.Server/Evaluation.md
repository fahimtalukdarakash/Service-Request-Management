# `AddEvaluation` API Method

The `AddEvaluation` method handles the process of adding a new evaluation to the system. It accepts an evaluation object as input and stores the evaluation in the database. Upon successful insertion, a success message is returned. If an error occurs, a bad request status with an error message is returned.

## Method Definition

```csharp
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
```
# `AddEvaluation` API Method

The `AddEvaluation` method handles adding a new evaluation into the system. The evaluation details are provided as an `Evaluation` object, which is inserted into the `ServiceRequestEvaluations` table of the database. After insertion, a response message is returned indicating the success or failure of the operation.

## Method Definition

```csharp
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
```
# `GetAllEvaluations` API Method

The `GetAllEvaluations` method retrieves all evaluations from the database. It communicates with the data access layer (DAL) to fetch the evaluations and returns the list of evaluations in a response. If successful, a status code `200 OK` is returned along with the evaluations list. If no evaluations are found, a `404 Not Found` status code is returned.

## Method Definition

```csharp
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
```
# `GetAllEvaluations` API Method

The `GetAllEvaluations` method retrieves all evaluation records from the `ServiceRequestEvaluations` table. It fetches the data, processes it, and returns it as a list of evaluation objects. If the evaluations are successfully fetched, the method returns a `200 OK` status along with the evaluations list. If no evaluations are found, it returns a `404 Not Found` status code with a corresponding message.

## Method Definition

```csharp
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

```