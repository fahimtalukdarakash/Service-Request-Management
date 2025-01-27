import React, { useEffect, useState, useContext } from "react";
import axios from "axios";
import { AuthContext } from "../AuthContext";
import { useNavigate } from "react-router-dom";

function OrderList() {
  const { userRole, userEmail, userID } = useContext(AuthContext);
  const [orders, setOrders] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    const fetchOrders = async () => {
      try {
        let response;
        if (userRole === "User") {
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetUserOrders",
            { params: { userID: userID } }
          );
        } else if (userRole === "ProviderManager") {
          response = await axios.get(
            "https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/ServiceRequest/GetProviderManagerOrders",
            { params: { email: userEmail } }
          );
        }

        if (response.status === 200) {
          setOrders(response.data);
        } else {
          setErrorMessage(response.data.message || "No orders found.");
        }
      } catch (error) {
        console.error("Error fetching orders:", error);
        setErrorMessage("An error occurred while fetching the orders.");
      }
    };

    fetchOrders();
  }, [userRole, userEmail, userID]);

  const handleEvaluate = (orderID) => {
    navigate(`/evaluate-order/${orderID}`);
  };

  return (
    <div className="container mt-5">
      <h2>{userRole === "User" ? "Your Orders" : "All Orders"}</h2>
      {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
      {orders.length > 0 ? (
        <div>
          {orders.map((order, index) => (
            <div key={order.serviceRequestOfferId || index} className="card mb-3">
              <div className="card-body">
                <h5 className="card-title">{order.project || "No Project Title"}</h5>
                <p className="text-white"><strong>Task Description:</strong> {order.taskDescription || "N/A"}</p>
                <p className="text-white"><strong>Cycle Status:</strong> {order.cycleStatus}</p>
                <p className="text-white"><strong>Domain:</strong> {order.domainName}</p>
                <p className="text-white"><strong>Number of Specialists:</strong> {order.numberOfSpecialists}</p>
                <p className="text-white"><strong>Number of Offers:</strong> {order.numberOfOffers}</p>

                <h6>Offers:</h6>
                {order.serviceOffers && order.serviceOffers.length > 0 ? (
                  order.serviceOffers.map((offer) => (
                    <div key={offer.offerID} className="mb-2 p-2 border rounded bg-light">
                      <p><strong>Provider:</strong> {offer.providerName}</p>
                      <p><strong>Role:</strong> {offer.role}</p>
                      <p><strong>Price:</strong> {offer.price}</p>
                      <p><strong>Selection:</strong> {offer.selection}</p>
                      <p><strong>Comment:</strong> {offer.comment || "No comments"}</p>
                    </div>
                  ))
                ) : (
                  <p>No offers available</p>
                )}

                {userRole === "User" && (
                  <button
                    className="btn btn-primary"
                    onClick={() => handleEvaluate(order.serviceRequestOfferId)}
                  >
                    Evaluate
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div>No orders found.</div>
      )}
    </div>
  );
}

export default OrderList;
