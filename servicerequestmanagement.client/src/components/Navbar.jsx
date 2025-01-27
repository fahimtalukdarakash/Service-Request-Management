import React, { useContext } from 'react';
import { Link } from 'react-router-dom';
import { AuthContext } from '../AuthContext';

function Navbar() {
  const { isLoggedIn, logout, userRole } = useContext(AuthContext);

  return (
    <nav className="navbar navbar-expand-lg navbar-dark">
      <div className="container-fluid">
        <Link className="navbar-brand" to="/">Service Management</Link>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarNav"
          aria-controls="navbarNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse" id="navbarNav">
          <ul className="navbar-nav ms-auto">
            {!isLoggedIn && (
              <>
                <li className="nav-item">
                  <Link className="nav-link" to="/login">Login</Link>
                </li>
                <li className="nav-item">
                  <Link className="nav-link" to="/register">Register</Link>
                </li>
              </>
            )}
            {isLoggedIn && (
              <>
                {/* Render role-specific links */}
                {userRole === 'User' && (
                  <>
                    <li className="nav-item">
                      <Link className="nav-link" to="/add-service-request">
                      Add a Service Request
                      </Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/view-service-requests">
                      View Service Requests
                      </Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/order-list">
                        Order List
                      </Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/service-requests-with-bidding">
                        Service Request List With Bidding
                      </Link>
                    </li>
                  </>
                )}
                {userRole === 'ProviderManager' && (
                  <>
                    <li className="nav-item">
                      <Link className="nav-link" to="/view-service-requests">
                      View Service Requests
                      </Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/order-list">Order List</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/approved-service-requests">Approved Requests</Link>
                    </li>
                    <li className="nav-item">
                      <Link className="nav-link" to="/service-requests-with-bidding">
                        Service Request List With Bidding
                      </Link>
                    </li>
                  </>
                )}
                {userRole === 'ProviderAdmin' && (
                  <li className="nav-item">
                    <Link className="nav-link" to="/service-requests-with-bidding">Service Requests With Bidding</Link>
                  </li>
                )}
                {userRole === 'Provider' && (
                  <li className="nav-item">
                    <Link className="nav-link" to="/service-requests-with-bidding">Service Requests With Bidding</Link>
                  </li>
                )}
                <li className="nav-item">
                  <button className="btn btn-danger" onClick={logout}>Logout</button>
                </li>
              </>
            )}
          </ul>
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
