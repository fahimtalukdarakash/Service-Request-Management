import React, { useContext } from 'react';
import { AuthContext } from '../AuthContext';

function Home() {
  const { isLoggedIn, userName, userRole, userID } = useContext(AuthContext);

  return (
    <div className="text-center">
      <h1>Welcome to Service Management</h1>
      <p>Manage your services efficiently with our platform.</p>
      {isLoggedIn && (
        <div>
          <h2>Hello, {userName}!</h2>
          <p>Your role: {userRole}</p>
        </div>
      )}
    </div>
  );
}

export default Home;
