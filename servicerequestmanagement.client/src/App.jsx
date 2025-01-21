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
            </Routes>
          </div>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
