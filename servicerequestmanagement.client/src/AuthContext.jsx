import React, { createContext, useState } from 'react';

// Create a context for authentication
export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [isLoggedIn, setIsLoggedIn] = useState(sessionStorage.getItem('isLoggedIn') === 'true');
  const [userName, setUserName] = useState(sessionStorage.getItem('userName') || '');
  const [userEmail, setUserEmail] = useState(sessionStorage.getItem('userEmail') || '');
  const [userRole, setUserRole] = useState(sessionStorage.getItem('userRole') || '');
  const [userID, setUserID] = useState(sessionStorage.getItem('userID') || '')

  const login = (name, email, role,ID) => {
    setIsLoggedIn(true);
    setUserName(name);
    setUserEmail(email);
    setUserRole(role);
    setUserID(ID);
    sessionStorage.setItem('isLoggedIn', 'true');
    sessionStorage.setItem('userName', name);
    sessionStorage.setItem('userEmail', email);
    sessionStorage.setItem('userRole', role);
    sessionStorage.setItem('userID', ID);
  };

  const logout = () => {
    setIsLoggedIn(false);
    setUserName('');
    setUserRole('');
    setUserID('');
    sessionStorage.clear(); // Clear all session data
  };

  return (
    <AuthContext.Provider value={{ isLoggedIn, userName, userEmail, userRole, userID, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
