import React, { useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import 'bootstrap/dist/css/bootstrap.min.css';
import { AuthContext } from '../AuthContext';

function Login() {
  const { login } = useContext(AuthContext); // Use the login function from context
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const navigate = useNavigate();

  const validate = () => {
    const errors = {};
    if (!formData.email) {
      errors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Invalid email format';
    }
    if (!formData.password) errors.password = 'Password is required';
    setErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      const response = await axios.post(
        'https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/Registration/Login',
        formData
      );
      console.log('Backend response:', response.data);

      if (response.data.statusCode === 200) {
        const userName = response.data.registration.name; // Adjust to match backend key
        const userRole = response.data.registration.role; // Adjust to match backend key
        const userID = response.data.registration.id;
        const userEmail = response.data.registration.email;
        login(userName, userEmail, userRole, userID);
        navigate('/');
      } else {
        alert(response.data.StatusMessage || 'Invalid Credentials');
      }
    } catch (error) {
      console.error(error);
      alert('Login failed. Please try again.');
    }
  };

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  return (
    <div className="card bg-secondary text-light p-4">
      <h2 className="card-title">Login</h2>
      <form onSubmit={handleSubmit}>
        <div className="mb-3">
          <label htmlFor="email" className="form-label">Email</label>
          <input
            type="email"
            className={`form-control ${errors.email ? 'is-invalid' : ''}`}
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
          />
          {errors.email && <div className="invalid-feedback">{errors.email}</div>}
        </div>
        <div className="mb-3">
          <label htmlFor="password" className="form-label">Password</label>
          <input
            type="password"
            className={`form-control ${errors.password ? 'is-invalid' : ''}`}
            id="password"
            name="password"
            value={formData.password}
            onChange={handleChange}
          />
          {errors.password && <div className="invalid-feedback">{errors.password}</div>}
        </div>
        <button type="submit" className="btn btn-primary">Login</button>
      </form>
    </div>
  );
}

export default Login;
