import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import { AuthContext } from '../AuthContext';

function SideNavbar() {
  const { userRole, isLoggedIn, userName } = useContext(AuthContext);

  if (!isLoggedIn) {
    return null; // Do not render if the user is not logged in
  }

  // Render role-specific buttons
  const renderButtons = () => {
    switch (userRole) {
      case 'User':
        return (
          <>
            <Link to="/add-service-request" className="btn btn-primary mb-2">
              Add a Service Request
            </Link>
            <Link to="/view-service-requests" className="btn btn-primary mb-2">
              View Service Request Lists
            </Link>
            <Link to="/approved-service-requests" className="btn btn-primary mb-2">
              Approved Service Request Lists
            </Link>
            <Link to="/not-approved-service-requests" className="btn btn-primary mb-2">
              Not Approved Service Request Lists
            </Link>
            <Link to="/service-requests-with-bidding" className="btn btn-primary mb-2">
              Service Request List With Bidding
            </Link>
          </>
        );
      case 'ProviderManager':
        return (
          <>
            <Link to="/not-approved-service-requests" className="btn btn-primary mb-2">
              View Service Request List (Not Approved)
            </Link>
            <Link to="/approved-service-requests" className="btn btn-primary mb-2">
              Approved Service Request Lists
            </Link>
            <Link to="/service-requests-with-bidding" className="btn btn-primary mb-2">
              Service Request List With Bidding
            </Link>
          </>
        );
      case 'ProviderAdmin':
      case 'Provider':
        return (
          <Link to="/service-requests-with-bidding" className="btn btn-primary mb-2">
            View Service Request List With Bidding
          </Link>
        );
      default:
        return null;
    }
  };

  return (
    <div className="d-flex flex-column p-3 bg-light min-vh-100" style={{ width: '250px' }}>
      <h5>Welcome, {userName}</h5>
      <hr />
      {renderButtons()}
    </div>
  );
}

export default SideNavbar;
