import React, { useState, useEffect, useContext } from "react";
import axios from "axios";
import { useParams } from "react-router-dom";
import { AuthContext } from "../AuthContext";

function ViewServiceRequestOfferPM() {
  const { serviceRequestOfferId } = useParams();
  const { userRole, userID } = useContext(AuthContext);
  const [serviceRequestOffer, setServiceRequestOffer] = useState(null);
  const [message, setMessage] = useState("");
  const [messages, setMessages] = useState([]);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const fetchServiceRequestOffer = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestOfferSelection",
          { params: { serviceRequestOfferId: serviceRequestOfferId } }
        );
        setServiceRequestOffer(response.data);
      } catch (error) {
        setErrorMessage("An error occurred while fetching service request offer.");
      }
    };

    const fetchMessages = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetMessages2",
          { params: { serviceRequestOfferId } }
        );

        if (response.data.statusCode === 200) {
          setMessages(response.data.listMessages);
        } else {
          setErrorMessage(response.data.statusMessage);
        }
      } catch (error) {
        setErrorMessage("An error occurred while fetching messages.");
      }
    };

    fetchServiceRequestOffer();
    fetchMessages();
  }, [serviceRequestOfferId]);

  const handleSendMessage = async () => {
    if (!message.trim()) {
      setErrorMessage("Message cannot be empty.");
      return;
    }

    try {
      const payload = {
        serviceRequestOfferId: serviceRequestOfferId,
        senderID: userID,
        senderRole: userRole,
        messageContent: message,
      };

      await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/SendMessage2",
        payload
      );

      setMessages((prevMessages) => [...prevMessages, payload]);
      setMessage("");
      setSuccessMessage("Message sent successfully!");
    } catch (error) {
      setErrorMessage("An error occurred while sending the message.");
    }
  };

  const handleCreateOrder = async () => {
    try {
      await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/CreateOrder",
        serviceRequestOffer
      );
      setSuccessMessage("Order created successfully.");
    } catch (error) {
      setErrorMessage("An error occurred while creating the order.");
    }
  };

  return (
    <div className="container mt-5">
      <h2>Service Request Offer Details</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {successMessage && <div className="alert alert-success">{successMessage}</div>}
      {serviceRequestOffer ? (
        <div>
          <h4>Project: {serviceRequestOffer.project}</h4>
          <p><strong>Task Description:</strong> {serviceRequestOffer.taskDescription}</p>
          <p><strong>Request Type:</strong> {serviceRequestOffer.requestType}</p>
          <p><strong>Domain:</strong> {serviceRequestOffer.domainName}</p>
          <p><strong>Cycle Status:</strong> {serviceRequestOffer.cycleStatus}</p>

          <h4>Offers</h4>
          {serviceRequestOffer.serviceOffers.map((offer, index) => (
            <div key={index} className="card mb-3">
              <div className="card-body">
                <p><strong>Provider:</strong> {offer.providerName}</p>
                <p><strong>Role:</strong> {offer.role}</p>
                <p><strong>Price:</strong> ${offer.price}</p>
                <p><strong>Selection:</strong> {offer.selection}</p>
                <p><strong>Comment:</strong> {offer.comment}</p>
              </div>
            </div>
          ))}
          <div className="mt-4">
            <button className="btn btn-success me-3" onClick={handleCreateOrder}>
              Create Order
            </button>
          </div>

          <h4>Messages</h4>
          <div className="card bg-light p-3">
            {messages.length > 0 ? (
              messages.map((msg, index) => (
                <div key={index}>
                  <strong>{msg.senderRole}:</strong> {msg.messageContent}
                </div>
              ))
            ) : (
              <p>No messages yet.</p>
            )}
          </div>

          <div className="mt-3">
            <input
              type="text"
              className="form-control"
              placeholder="Enter your message"
              value={message}
              onChange={(e) => setMessage(e.target.value)}
            />
            <button className="btn btn-primary mt-2" onClick={handleSendMessage}>
              Send Message
            </button>
          </div>

          
        </div>
      ) : (
        <div>Loading...</div>
      )}
    </div>
  );
}

export default ViewServiceRequestOfferPM;
