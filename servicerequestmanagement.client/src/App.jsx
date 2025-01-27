import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import { AuthProvider } from './AuthContext';
import Home from './components/Home';
import Registration from './components/Registration';
import Navbar from './components/Navbar';
import Login from './components/Login';
import AddServiceRequest from './components/AddServiceRequest';
import ViewServiceRequests from './components/ViewServiceRequests';
import ViewServiceRequest from './components/ViewServiceRequest';
import UpdateServiceRequest from './components/UpdateServiceRequest';
import ViewServiceRequestOffers from './components/ViewServiceRequestOffers';
import ViewServiceRequestOffer from './components/ViewServiceRequestOffer';
import ViewServiceRequestOfferPM from './components/ViewServiceRequestOfferPM';
import OrderList from './components/OrderList';
import EvaluateOrder from './components/EvaluateOrder';
import Particle from './Particle';
function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-container">
          {/* Navbar */}
          <Navbar />
          <Particle/>
          {/* Main Content */}
          <div className="content-wrapper">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/register" element={<Registration />} />
              <Route path="/login" element={<Login />} />
              <Route path="/add-service-request" element={<AddServiceRequest />} />
              <Route path="/view-service-requests" element={<ViewServiceRequests />} />
              <Route path="/view-service-request/:requestID" element={<ViewServiceRequest />} />
              <Route path="/update-service-request/:requestID" element={<UpdateServiceRequest />} />
              <Route path="/service-requests-with-bidding" element={<ViewServiceRequestOffers/>} />
              <Route path="/view-service-request-offer/:serviceRequestOfferId" element={<ViewServiceRequestOffer/>} />
              <Route path="/view-service-request-offer-pm/:serviceRequestOfferId" element={<ViewServiceRequestOfferPM/>} />
              <Route path="/order-list" element={<OrderList />} />
              <Route path="/evaluate-order/:serviceRequestOfferId" element={<EvaluateOrder />} />
            </Routes>
          </div>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
