import React, { useEffect, useState, useContext } from "react";
import { useParams } from "react-router-dom";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function ViewServiceRequestOffer() {
  const { serviceRequestOfferId } = useParams();
  // const { requestID } = useParams();
  const { userRole, userID } = useContext(AuthContext);
  const [serviceRequestOffer, setServiceRequestOffer] = useState(null);
  const [offers, setOffers] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [cycleStatus, setCycleStatus] = useState("none");

  useEffect(() => {
    const fetchServiceRequestOffer = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestOfferDetails",
          { params: { serviceRequestOfferId } }
        );
        setServiceRequestOffer(response.data);
        setOffers(
          response.data.serviceOffers.map((offer) => ({
            ...offer,
            selection: "Not Selected",
            comment: "",
          }))
        );
      } catch (error) {
        console.error("Error fetching service request offer:", error);
        setErrorMessage("An error occurred while fetching service request offer.");
      }
    };

    fetchServiceRequestOffer();
  }, [serviceRequestOfferId]);

  const handleSelectionChange = (offerID, isSelected) => {
    setOffers((prevOffers) =>
      prevOffers.map((offer) =>
        offer.offerID === offerID
          ? { ...offer, selection: isSelected ? "Selected" : "Not Selected" }
          : offer
      )
    );

    // Update cycle status after selection change
    const selectedOffer = offers.some((offer) => offer.selection === "Selected");
    setCycleStatus(selectedOffer ? "none" : "cycle_two");
  };

  const handleCommentChange = (offerID, comment) => {
    setOffers((prevOffers) =>
      prevOffers.map((offer) =>
        offer.offerID === offerID ? { ...offer, comment } : offer
      )
    );
  };

  useEffect(() => {
    const anySelected = offers.some((offer) => offer.selection === "Selected");
    setCycleStatus(anySelected ? "none" : "cycle_two");
  }, [offers]);

  const handleSubmit = async () => {
    const updatedRequestOffer = {
      serviceRequestOfferId: serviceRequestOffer.serviceRequestOfferId,
      requestID: serviceRequestOffer.requestID,
      userID: userID,
      masterAgreementID: serviceRequestOffer.masterAgreementID,
      masterAgreementName: serviceRequestOffer.masterAgreementName,
      taskDescription: serviceRequestOffer.taskDescription,
      requestType: serviceRequestOffer.requestType,
      project: serviceRequestOffer.project,
      domainID: serviceRequestOffer.domainID,
      domainName: serviceRequestOffer.domainName,
      cycleStatus,
      numberOfSpecialists: serviceRequestOffer.numberOfSpecialists,
      numberOfOffers: serviceRequestOffer.numberOfOffers,
      serviceOffers: offers,
    };

    try {
      const response = await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/AddServiceRequestOfferSelection",
        updatedRequestOffer
      );

      if (response.status === 200) {
        setSuccessMessage("Service request offer submitted successfully.");
      } else {
        setErrorMessage(response.data.statusMessage || "Error submitting offer.");
      }
    } catch (error) {
      console.error("Error submitting service request offer:", error);
      setErrorMessage("An error occurred while submitting the service request offer.");
    }
  };

  return (
    <div className="container mt-5">
      <h2>Service Request Offer Details</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {serviceRequestOffer ? (
        <div className="row">
          <div className="col-md-7">
            <h4>{serviceRequestOffer.project}</h4>
            <p><strong>Task:</strong> {serviceRequestOffer.taskDescription}</p>
            <p><strong>Domain:</strong> {serviceRequestOffer.domainName}</p>
            <p><strong>Number of Specialists:</strong> {serviceRequestOffer.numberOfSpecialists}</p>
            <p><strong>Number of Offers:</strong> {serviceRequestOffer.numberOfOffers}</p>
          </div>
          <div className="col-md-5">
            {serviceRequestOffer.serviceOffers.map((offer) => (
              <div key={offer.offerID} className="card mb-3">
                <div className="card-body">
                  <p className="text-white"><strong>Provider:</strong> {offer.providerName}</p>
                  <p className="text-white"><strong>Employee ID:</strong> {offer.employeeID}</p>
                  <p className="text-white"><strong>Role:</strong> {offer.role}</p>
                  <p className="text-white"><strong>Level:</strong> {offer.level}</p>
                  <p className="text-white"><strong>Technology Level:</strong> {offer.technologyLevel}</p>
                  <p className="text-white"><strong>Location Type:</strong> {offer.locationType}</p>
                  <p className="text-white"><strong>Price:</strong> ${offer.price}</p>
                  <div className="mt-2">
                  <button
                      className={`btn ${offer.selection === "Selected" ? "btn-success" : "btn-outline-success"} me-2`}
                      onClick={() => handleSelectionChange(offer.offerID, true)}
                    >
                      {offer.selection === "Selected" ? "Selected" : "Select"}
                    </button>
                    <button
                      className={`btn ${offer.selection === "Not Selected" ? "btn-danger" : "btn-outline-danger"}`}
                      onClick={() => handleSelectionChange(offer.offerID, false)}
                    >
                      {offer.selection === "Not Selected" ? "Not Selected" : "Unselect"}
                    </button>
                  </div>
                  <input
                    type="text"
                    className="form-control mt-2"
                    placeholder="Enter comment"
                    value={offer.comment}
                    onChange={(e) => handleCommentChange(offer.offerID, e.target.value)}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div>Loading...</div>
      )}
      <button className="btn btn-primary mt-3" onClick={handleSubmit}>Submit</button>
    </div>
  );
}

export default ViewServiceRequestOffer;
