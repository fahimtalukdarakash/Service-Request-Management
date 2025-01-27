import React, { useState, useEffect } from "react";
import axios from "axios";
import { useParams } from "react-router-dom";

function EvaluateOrder() {
  const { serviceRequestOfferId } = useParams();
  const [orderDetails, setOrderDetails] = useState(null);
  const [providers, setProviders] = useState([]);
  const [selectedProvider, setSelectedProvider] = useState("");
  const [timelinessScore, setTimelinessScore] = useState("");
  const [qualityScore, setQualityScore] = useState("");
  const [overallScore, setOverallScore] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const fetchOrderDetails = async () => {
      try {
        const response = await axios.get(
          "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetSingleOrder",
          { params: { serviceRequestOfferId: serviceRequestOfferId } }
        );

        if (response.status === 200) {
          setOrderDetails(response.data);
          // Extract unique provider names from service offers
          const uniqueProviders = Array.from(
            new Map(response.data.serviceOffers.map((offer) => [offer.providerName, offer])).values()
          );
          setProviders(uniqueProviders);
        } else {
          setErrorMessage("No order details found.");
        }
      } catch (error) {
        console.error("Error fetching order details:", error);
        setErrorMessage("An error occurred while fetching order details.");
      }
    };

    fetchOrderDetails();
  }, [serviceRequestOfferId]);

  const handleSubmit = async () => {
    if (!selectedProvider || !timelinessScore || !qualityScore || !overallScore) {
      setErrorMessage("Please fill all fields before submitting.");
      return;
    }

    // Get provider details
    const providerDetails = providers.find((p) => p.providerName === selectedProvider);

    const evaluationData = {
      serviceRequestId: orderDetails.requestID,
      agreementId: orderDetails.masterAgreementID,
      agreementName: orderDetails.masterAgreementName,
      taskDescription: orderDetails.taskDescription,
      type: orderDetails.requestType,
      project: orderDetails.project,
      providerId: providerDetails.providerID,
      providerName: selectedProvider,
      timelinessScore: parseFloat(timelinessScore),
      qualityScore: parseFloat(qualityScore),
      overallScore: parseFloat(overallScore),
    };

    try {
      const response = await axios.post(
        "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/AddEvaluation",
        evaluationData
      );

      if (response.status === 200) {
        setSuccessMessage("Evaluation submitted successfully.");
        setErrorMessage("");
      } else {
        setErrorMessage("Failed to submit evaluation.");
      }
    } catch (error) {
      console.error("Error submitting evaluation:", error);
      setErrorMessage("An error occurred while submitting evaluation.");
    }
  };

  return (
    <div className="container mt-5">
      <h2>Evaluate Order</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {successMessage && <div className="alert alert-success">{successMessage}</div>}

      {orderDetails ? (
        <div className="card p-4">
          <h4 className="text-white">Project: {orderDetails.project}</h4>
          <p className="text-white"><strong>Task Description:</strong> {orderDetails.taskDescription}</p>
          <p className="text-white"><strong>Agreement:</strong> {orderDetails.masterAgreementName}</p>

          <div className="mb-3">
            <label className="text-white">Provider Name</label>
            <select
              className="form-control"
              value={selectedProvider}
              onChange={(e) => setSelectedProvider(e.target.value)}
            >
              <option value="">Select Provider</option>
              {providers.map((provider) => (
                <option key={provider.providerID} value={provider.providerName}>
                  {provider.providerName}
                </option>
              ))}
            </select>
          </div>

          <div className="mb-3">
            <label className="text-white">Timeliness Score</label>
            <input
              type="number"
              className="form-control"
              value={timelinessScore}
              onChange={(e) => setTimelinessScore(e.target.value)}
              placeholder="Enter timeliness score (1-10)"
            />
          </div>

          <div className="mb-3">
            <label className="text-white">Quality Score</label>
            <input
              type="number"
              className="form-control"
              value={qualityScore}
              onChange={(e) => setQualityScore(e.target.value)}
              placeholder="Enter quality score (1-10)"
            />
          </div>

          <div className="mb-3">
            <label className="text-white">Overall Score</label>
            <input
              type="number"
              className="form-control"
              value={overallScore}
              onChange={(e) => setOverallScore(e.target.value)}
              placeholder="Enter overall score (1-10)"
            />
          </div>

          <button className="btn btn-primary" onClick={handleSubmit}>
            Submit Evaluation
          </button>
        </div>
      ) : (
        <div>Loading...</div>
      )}
    </div>
  );
}

export default EvaluateOrder;
