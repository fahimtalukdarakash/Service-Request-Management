import React, { useEffect, useState, useContext } from "react";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function ViewServiceRequests() {
  const { userRole, userID, userEmail } = useContext(AuthContext);
  const [serviceRequests, setServiceRequests] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const fetchServiceRequests = async () => {
      try {
        let response;
        if (userRole === "User") {
          // Fetch requests created by the user
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetUserServiceRequests",
            { params: { userID: userID } }
          );
        } else if (userRole === "ProviderManager") {
          // Fetch requests for the Provider Manager based on their department
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestsForProviderManager",
            { params: { email: userEmail } }
          );
        }

        if (response.data.statusCode === 200) {
          setServiceRequests(response.data.listServiceRequests);
        } else {
          setErrorMessage(response.data.statusMessage);
        }
      } catch (error) {
        console.error("Error fetching service requests:", error);
        setErrorMessage("An error occurred while fetching service requests.");
      }
    };

    fetchServiceRequests();
  }, [userRole, userID, userEmail]);

  const handleView = (requestID) => {
    window.location.href = `/view-service-request/${requestID}`;
  };

  const handleUpdate = (requestID) => {
    window.location.href = `/update-service-request/${requestID}`;
  };

  const handleDelete = async (requestID) => {
    try {
      const response = await axios.delete(
        `https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/DeleteServiceRequest`,
        { params: { requestID: requestID } }
      );

      if (response.data.statusCode === 200) {
        setServiceRequests((prevRequests) =>
          prevRequests.filter((request) => request.requestID !== requestID)
        );
        alert("Service request deleted successfully.");
      } else {
        alert(response.data.statusMessage);
      }
    } catch (error) {
      console.error("Error deleting service request:", error);
      alert("An error occurred while deleting the service request.");
    }
  };

  return (
    <div className="container mt-5">
      <h2>View Service Requests</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {serviceRequests.length > 0 ? (
        <div>
          {serviceRequests.map((request, index) => (
            <div key={request.requestID || index} className="card mb-3">
              <div className="card-body">
                <h5 className="card-title">{request.project || "No Project Title"}</h5>
                <p className="card-text text-white">
                  <strong>Approval Status:</strong>{" "}
                  {request.isApproved === 1 ? "Approved" : "Not Approved"}
                </p>
                <p className="card-text text-white">
                  <strong>Domain:</strong> {request.selectedDomainName || "N/A"}
                </p>
                <p className="card-text text-white">
                  <strong>Task Description:</strong> {request.taskDescription || "N/A"}
                </p>
                <div>
                  <button
                    className="btn btn-primary me-2"
                    onClick={() => handleView(request.requestID)}
                  >
                    View
                  </button>
                  {userRole === "User" && (
                    <button
                      className="btn btn-secondary me-2"
                      onClick={() => handleUpdate(request.requestID)}
                    >
                      Update
                    </button>
                  )}
                  {userRole === "ProviderManager" && (
                    <button
                      className="btn btn-danger"
                      onClick={() => handleDelete(request.requestID)}
                    >
                      Delete
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div>No service requests found.</div>
      )}
    </div>
  );
}

export default ViewServiceRequests;
