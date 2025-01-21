import React, { useState, useEffect, useContext } from "react";
import { useParams } from "react-router-dom";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function ViewServiceRequest() {
  const { requestID } = useParams(); // Get requestID from URL params
  const { userRole, userID } = useContext(AuthContext);
  const [serviceRequest, setServiceRequest] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  // States for messaging
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState("");

  useEffect(() => {
    const fetchServiceRequest = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestDetails",
          { params: { requestID } }
        );

        if (response.data.statusCode === 200) {
          setServiceRequest(response.data.serviceRequest);
        } else {
          setErrorMessage(response.data.statusMessage);
        }
      } catch (error) {
        console.error("Error fetching service request:", error);
        setErrorMessage("An error occurred while fetching service request.");
      }
    };

    const fetchMessages = async () => {
      try {
        const response = await axios.get("https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetMessages", {
          params: { requestID }
        });

        if (response.data.statusCode === 200) {
          setMessages(response.data.listMessages);
        } else {
          setErrorMessage(response.data.statusMessage);
        }
      } catch (error) {
        console.error("Error fetching messages:", error);
        setErrorMessage("An error occurred while fetching messages.");
      }
    };

    fetchServiceRequest();
    fetchMessages();
  }, [requestID]);

  const handleApprove = async () => {
    try {
      const response = await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/ServiceRequestApproval",
        JSON.stringify(requestID),
        {
          headers: {
            "Content-Type": "application/json",
          },
        }
      );

      if (response.data.statusCode === 200) {
        setSuccessMessage("Service Request Approved Successfully!");
        setServiceRequest((prev) => ({
          ...prev,
          isApproved: 1,
        }));
      } else {
        setErrorMessage(response.data.statusMessage);
      }
    } catch (error) {
      console.error("Error approving service request:", error);
      setErrorMessage("An error occurred while approving the service request.");
    }
  };

  const handleSendMessage = async () => {
    if (!newMessage.trim()) {
      setErrorMessage("Message cannot be empty.");
      return;
    }

    try {
      const payload = {
        serviceRequestID: requestID,
        senderID: userID,
        senderRole: userRole,
        messageContent: newMessage,
      };

      const response = await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/SendMessage",
        payload
      );

      if (response.data.statusCode === 200) {
        setSuccessMessage("Message sent successfully!");
        setMessages((prevMessages) => [...prevMessages, payload]);
        setNewMessage("");
      } else {
        setErrorMessage(response.data.statusMessage || "Failed to send message.");
      }
    } catch (error) {
      console.error("Error sending message:", error);
      setErrorMessage("An error occurred while sending the message.");
    }
  };

  return (
    <div className="container mt-5">
      <h2>Service Request Details</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {successMessage && <div className="alert alert-success">{successMessage}</div>}
      {serviceRequest ? (
        <div className="card bg-dark text-light p-4">
          <h4 className="card-title">{serviceRequest.project || "No Project Title"}</h4>
          <p><strong>Task Description:</strong> {serviceRequest.taskDescription || "N/A"}</p>
          <p><strong>Request Type:</strong> {serviceRequest.requestType || "N/A"}</p>
          <p><strong>Start Date:</strong> {serviceRequest.startDate || "N/A"}</p>
          <p><strong>End Date:</strong> {serviceRequest.endDate || "N/A"}</p>
          <p><strong>Total Man Days:</strong> {serviceRequest.totalManDays || "N/A"}</p>
          <p><strong>Location:</strong> {serviceRequest.location || "N/A"}</p>
          <p><strong>Provider Manager Info:</strong> {serviceRequest.providerManagerInfo || "N/A"}</p>
          <p><strong>Consumer:</strong> {serviceRequest.consumer || "N/A"}</p>
          <p><strong>Representatives:</strong> {serviceRequest.representatives || "N/A"}</p>
          <p><strong>Cycle:</strong> {serviceRequest.cycle || "N/A"}</p>
          <p><strong>Master Agreement ID:</strong> {serviceRequest.masterAgreementID || "N/A"}</p>
          <p><strong>Master Agreement Name:</strong> {serviceRequest.masterAgreementName || "N/A"}</p>
          <p><strong>Selected Domain Name:</strong> {serviceRequest.selectedDomainName || "N/A"}</p>
          <p><strong>Approval Status:</strong> {serviceRequest.isApproved === 1 ? "Approved" : "Not Approved"}</p>

          <h5 className="mt-4">Role Specific Details</h5>
          {serviceRequest.roleSpecific && serviceRequest.roleSpecific.length > 0 ? (
            <ul>
              {serviceRequest.roleSpecific.map((role, index) => (
                <li key={index}>
                  <strong>Role:</strong> {role.role || "N/A"}, <strong>Level:</strong> {role.level || "N/A"}, <strong>Technology Level:</strong> {role.technologyLevel || "N/A"}, <strong>Location Type:</strong> {role.locationType || "N/A"}, <strong>Number of Employees:</strong> {role.numberOfEmployee || "N/A"}
                </li>
              ))}
            </ul>
          ) : (
            <p>No Role Specific Details Available.</p>
          )}

          {userRole === "ProviderManager" && (
            <button
              className="btn btn-primary mt-3"
              onClick={handleApprove}
              disabled={serviceRequest.isApproved === 1}
            >
              {serviceRequest.isApproved === 1 ? "Approved" : "Approve"}
            </button>
          )}
        </div>
      ) : (
        <div>Loading...</div>
      )}

      <h3 className="mt-4">Messages</h3>
      <div className="card bg-light p-3">
        {messages && messages.length > 0 ? (
          messages.map((msg, index) => (
            <div key={index}>
              <strong>
                {msg.senderRole ? (msg.senderRole === userRole ? "You" : msg.senderRole) : "Unknown"}:
              </strong>{" "}
              {msg.messageContent || "No Message Text"}
            </div>
          ))
        ) : (
          <p>No messages yet.</p>
        )}
      </div>

      <div className="mt-3">
        <textarea
          className="form-control"
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Type your message here..."
        ></textarea>
        <button className="btn btn-primary mt-2" onClick={handleSendMessage}>
          Send
        </button>
      </div>
    </div>
  );
}

export default ViewServiceRequest;
