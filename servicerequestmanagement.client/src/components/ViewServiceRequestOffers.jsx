import React, { useEffect, useState, useContext } from "react";
import axios from "axios";
import { AuthContext } from "../AuthContext";

function ViewServiceRequestOffers() {
  const { userRole, userID, userEmail } = useContext(AuthContext);
  const [serviceRequestOffers, setServiceRequestOffers] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const fetchServiceRequestOffers = async () => {
      try {
        let response;
        if (userRole === "User") {
          // Fetch service request offers created by the user
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetUserServiceRequestOffers",
            { params: { userID: userID } }
          );
        } else if (userRole === "ProviderManager") {
          // Fetch service request offers for the Provider Manager based on their department
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetServiceRequestOffersForProviderManager",
            { params: { email: userEmail } }
          );
        }

        if (response.status === 200 && response.data.length > 0) {
          setServiceRequestOffers(response.data);
        } else {
          setErrorMessage("No service request offers found.");
        }
      } catch (error) {
        console.error("Error fetching service request offers:", error);
        setErrorMessage("An error occurred while fetching service request offers.");
      }
    };

    fetchServiceRequestOffers();
  }, [userRole, userID, userEmail]);

  const handleView = (serviceRequestOfferId) => {
    if(userRole==="User")
    {
      window.location.href = `/view-service-request-offer/${serviceRequestOfferId}`;
    }
    if(userRole==="ProviderManager")
    {
      window.location.href = `/view-service-request-offer-pm/${serviceRequestOfferId}`;
    }
    
  };

  return (
    <div className="container mt-5">
      <h2>View Service Request Offers</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {serviceRequestOffers.length > 0 ? (
        <div>
          {serviceRequestOffers.map((offer, index) => (
            <div key={offer.serviceRequestOfferId || index} className="card mb-3">
              <div className="card-body">
                <h5 className="card-title">{offer.project || "No Project Title"}</h5>
                <p className="card-text text-white">
                  <strong>Master Agreement:</strong> {offer.masterAgreementName || "N/A"}
                </p>
                <p className="card-text text-white">
                  <strong>Domain:</strong> {offer.domainName || "N/A"}
                </p>
                <p className="card-text text-white">
                  <strong>Task Description:</strong> {offer.taskDescription || "N/A"}
                </p>
                <p className="card-text text-white">
                  <strong>Number of Offers:</strong> {offer.numberOfOffers || "N/A"}
                </p>
                <div>
                  <button
                    className="btn btn-primary me-2"
                    onClick={() => handleView(offer.serviceRequestOfferId)}
                  >
                    View
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div>No service request offers found.</div>
      )}
    </div>
  );
}

export default ViewServiceRequestOffers;
