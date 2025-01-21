import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes, Link } from 'react-router-dom';
import axios from 'axios';
import 'bootstrap/dist/css/bootstrap.min.css';

function Registration() {
  const [formData, setFormData] = useState({
      name: '',
      email: '',
      password: '',
      role: ''
  });
  const [errors, setErrors] = useState({});

  const validate = () => {
      const errors = {};
      if (!formData.name) errors.name = 'Name is required';
      if (!formData.email) {
          errors.email = 'Email is required';
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
          errors.email = 'Invalid email format';
      }
      if (!formData.password) errors.password = 'Password is required';
      if (!formData.role) errors.role = 'Role is required';
      setErrors(errors);
      return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
      e.preventDefault();
      console.log(formData.name,formData.email,formData.role);
      if (!validate()) return;

      try {
          const response = await axios.post('https://servicerequestapi-d0g3ezftcggucbev.germanywestcentral-01.azurewebsites.net/api/Registration/Registration', formData);
          alert(response.data.StatusMessage || "Registration successful!");

          // Clear the form fields after successful registration
          setFormData({
            name: '',
            email: '',
            password: '',
            role: ''
        });
        setErrors({}); // Clear errors if any were displayed
      } catch (error) {
          alert('Registration failed');
      }
  };

  const handleChange = (e) => {
      setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  return (
      <div className="card bg-secondary text-light p-4">
          <h2 className="card-title">Register</h2>
          <form onSubmit={handleSubmit}>
              <div className="mb-3">
                  <label htmlFor="name" className="form-label">Name</label>
                  <input
                      type="text"
                      className={`form-control ${errors.name ? 'is-invalid' : ''}`}
                      id="name"
                      name="name"
                      value={formData.name}
                      onChange={handleChange}
                  />
                  {errors.name && <div className="invalid-feedback">{errors.name}</div>}
              </div>
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
              <div className="mb-3">
                  <label htmlFor="role" className="form-label">Role</label>
                  <select
                      className={`form-control ${errors.role ? 'is-invalid' : ''}`}
                      id="role"
                      name="role"
                      value={formData.role}
                      onChange={handleChange}
                  >
                      <option value="">Select Role</option>
                      <option value="User">User</option>
                      <option value="ProviderManager">Provider Manager</option>
                      <option value="ProviderAdmin">Provider Admin</option>
                      <option value="Provider">Provider</option>
                  </select>
                  {errors.role && <div className="invalid-feedback">{errors.role}</div>}
              </div>
              <button type="submit" className="btn btn-primary">Register</button>
          </form>
      </div>
  );
}

export default Registration;